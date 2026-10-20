using HrPayrollSystem.Data;
using HrPayrollSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HrPayrollSystem.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    private static Dictionary<string, int> BuildSummary(IEnumerable<Attendance> rows)
    {
        var dict = new Dictionary<string, int>();
        foreach (AttendanceStatus s in Enum.GetValues(typeof(AttendanceStatus)))
            dict[s.ToString()] = 0;
        foreach (var r in rows)
            dict[r.Status.ToString()]++;
        return dict;
    }

    public async Task<AdminDashboardViewModel> GetAdminStatsAsync()
    {
        var now = DateTime.Now;
        var monthStart = new DateOnly(now.Year, now.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var vm = new AdminDashboardViewModel
        {
            ActiveEmployees = await _context.Employees.CountAsync(e => e.IsActive),
            Departments = await _context.Departments.CountAsync(),
            PendingLeaves = await _context.LeaveRequests.CountAsync(r => r.Status == LeaveStatus.Pending)
        };

        var latest = await _context.Payslips
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .FirstOrDefaultAsync();
        if (latest != null)
        {
            vm.LatestMonthNetPay = await _context.Payslips
                .Where(p => p.Year == latest.Year && p.Month == latest.Month)
                .SumAsync(p => p.NetPay);
            vm.LatestMonthLabel = new DateTime(latest.Year, latest.Month, 1).ToString("MMMM yyyy");
        }

        var rows = await _context.Attendances
            .Where(a => a.Date >= monthStart && a.Date <= monthEnd)
            .ToListAsync();
        vm.AttendanceSummary = BuildSummary(rows);

        var linkedIds = await _context.Employees.Where(e => e.IsActive)
            .Select(e => e.ApplicationUserId).ToListAsync();
        var unlinked = await _userManager.Users
            .Where(u => !linkedIds.Contains(u.Id)).OrderBy(u => u.Email).ToListAsync();
        vm.UnlinkedUsersCount = unlinked.Count;
        vm.UnlinkedEmails = unlinked.Select(u => u.Email ?? "").Take(5).ToList();

        vm.LatestLeaveRequests = await _context.LeaveRequests
            .Include(r => r.Employee).Include(r => r.LeaveType)
            .OrderByDescending(r => r.AppliedOn).Take(5).ToListAsync();

        return vm;
    }

    public async Task<HrDashboardViewModel> GetHrStatsAsync()
    {
        var now = DateTime.Now;
        var monthStart = new DateOnly(now.Year, now.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var vm = new HrDashboardViewModel
        {
            ActiveEmployees = await _context.Employees.CountAsync(e => e.IsActive),
            PendingLeaves = await _context.LeaveRequests.CountAsync(r => r.Status == LeaveStatus.Pending),
            HolidaysThisMonth = await _context.Holidays
                .CountAsync(h => h.Date.Year == now.Year && h.Date.Month == now.Month)
        };

        var linkedIds = await _context.Employees.Where(e => e.IsActive)
            .Select(e => e.ApplicationUserId).ToListAsync();
        vm.UnlinkedUsersCount = await _userManager.Users
            .CountAsync(u => !linkedIds.Contains(u.Id));

        var rows = await _context.Attendances
            .Where(a => a.Date >= monthStart && a.Date <= monthEnd)
            .ToListAsync();
        vm.AttendanceSummary = BuildSummary(rows);

        vm.PendingLeaveRequests = await _context.LeaveRequests
            .Include(r => r.Employee).Include(r => r.LeaveType)
            .Where(r => r.Status == LeaveStatus.Pending)
            .OrderBy(r => r.AppliedOn).Take(10).ToListAsync();

        return vm;
    }

    public async Task<ManagerDashboardViewModel> GetManagerStatsAsync(Employee manager)
    {
        var vm = new ManagerDashboardViewModel { ManagerName = manager.FullName, HasDepartment = manager.DepartmentId != null };
        var today = DateOnly.FromDateTime(DateTime.Now);

        vm.MyToday = await _context.Attendances
            .FirstOrDefaultAsync(a => a.EmployeeId == manager.Id && a.Date == today);
        vm.MyBalances = await _context.LeaveBalances.Include(b => b.LeaveType)
            .Where(b => b.EmployeeId == manager.Id && b.Year == DateTime.Now.Year)
            .OrderBy(b => b.LeaveTypeId).ToListAsync();

        if (!vm.HasDepartment) return vm;
        var deptId = manager.DepartmentId!.Value;

        vm.DepartmentName = (await _context.Departments.FirstOrDefaultAsync(d => d.Id == deptId))?.Name ?? "—";

        var team = await _context.Employees
            .Where(e => e.IsActive && e.DepartmentId == deptId && e.Id != manager.Id)
            .OrderBy(e => e.EmployeeCode).ToListAsync();
        vm.TeamHeadcount = team.Count;

        var teamIds = team.Select(t => t.Id).ToList();
        var todayRows = await _context.Attendances
            .Where(a => a.Date == today && teamIds.Contains(a.EmployeeId))
            .ToDictionaryAsync(a => a.EmployeeId, a => a.Status);
        vm.PresentToday = todayRows.Values.Count(s => s is AttendanceStatus.Present or AttendanceStatus.Late);

        vm.TeamToday = team.Select(t => new TeamMemberToday
        {
            Name = t.FullName,
            Code = t.EmployeeCode,
            Status = todayRows.TryGetValue(t.Id, out var st) ? st.ToString() : null
        }).ToList();

        vm.PendingTeamLeaves = await _context.LeaveRequests
            .Include(r => r.LeaveType).Include(r => r.Employee)
            .Where(r => r.Status == LeaveStatus.Pending && r.Employee!.DepartmentId == deptId)
            .OrderBy(r => r.AppliedOn).ToListAsync();
        vm.PendingLeaves = vm.PendingTeamLeaves.Count;

        return vm;
    }

    public async Task<EmployeeHomeViewModel> GetEmployeeHomeAsync(Employee employee)
    {
        var vm = new EmployeeHomeViewModel
        {
            EmployeeName = employee.FullName,
            EmployeeCode = employee.EmployeeCode,
            DepartmentName = employee.Department?.Name ?? "—"
        };

        var today = DateOnly.FromDateTime(DateTime.Now);
        var now = DateTime.Now;
        var monthStart = new DateOnly(now.Year, now.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        vm.Today = await _context.Attendances
            .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.Date == today);

        var rows = await _context.Attendances
            .Where(a => a.EmployeeId == employee.Id && a.Date >= monthStart && a.Date <= monthEnd)
            .OrderBy(a => a.Date).ToListAsync();
        vm.MonthlyAttendance = rows;
        vm.MonthSummary = BuildSummary(rows);

        vm.Balances = await _context.LeaveBalances.Include(b => b.LeaveType)
            .Where(b => b.EmployeeId == employee.Id && b.Year == now.Year)
            .OrderBy(b => b.LeaveTypeId).ToListAsync();

        vm.LatestPayslip = await _context.Payslips
            .Where(p => p.EmployeeId == employee.Id)
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .FirstOrDefaultAsync();

        vm.UpcomingHolidays = await _context.Holidays
            .Where(h => h.Date >= today).OrderBy(h => h.Date).Take(3).ToListAsync();

        return vm;
    }
}