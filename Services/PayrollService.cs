using HrPayrollSystem.Data;
using HrPayrollSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HrPayrollSystem.Services;

public class PayrollService : IPayrollService
{
    private readonly ApplicationDbContext _context;

    public PayrollService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PayrollRunSummary> RunPayrollAsync(int year, int month)
    {
        // GUARD 1: sane month
        if (month < 1 || month > 12)
            throw new InvalidOperationException("Month must be between 1 and 12.");

        // GUARD 2: no future payroll
        var now = DateTime.Now;
        if (year > now.Year || (year == now.Year && month > now.Month))
            throw new InvalidOperationException("Cannot run payroll for a future month.");

        // GUARD 3: one run per month
        var alreadyRun = await _context.Payslips.AnyAsync(p => p.Year == year && p.Month == month);
        if (alreadyRun)
            throw new InvalidOperationException(
                $"Payroll for {month:00}/{year} was already generated. Each month can be processed once.");

        // Holidays for this month
        var holidaySet = (await _context.Holidays
            .Where(h => h.Date.Year == year && h.Date.Month == month)
            .Select(h => h.Date)
            .ToListAsync())
            .ToHashSet();

        // Working days = days − weekends − holidays
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var workingDays = 0;
        for (var d = 1; d <= daysInMonth; d++)
        {
            var date = new DateOnly(year, month, d);
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            if (holidaySet.Contains(date)) continue;
            workingDays++;
        }

        if (workingDays == 0)
            throw new InvalidOperationException("This month has no working days.");

        var summary = new PayrollRunSummary { Year = year, Month = month, WorkingDays = workingDays };

        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var today = DateOnly.FromDateTime(DateTime.Now);

        var employees = await _context.Employees
            .Where(e => e.IsActive)
            .ToListAsync();

        var structures = (await _context.SalaryStructures.ToListAsync())
            .ToDictionary(s => s.EmployeeId);

        foreach (var employee in employees)
        {
            if (!structures.TryGetValue(employee.Id, out var structure))
            {
                summary.Skipped.Add($"{employee.EmployeeCode} — no salary structure");
                continue;
            }

            var joiningDate = DateOnly.FromDateTime(employee.JoiningDate);

            var attendanceMap = (await _context.Attendances
                .Where(a => a.EmployeeId == employee.Id && a.Date >= monthStart && a.Date <= monthEnd)
                .ToListAsync())
                .ToDictionary(a => a.Date);

            // ===== LOP RULES =====
            decimal lop = 0;
            for (var d = 1; d <= daysInMonth; d++)
            {
                var date = new DateOnly(year, month, d);

                // FIX 1: future days in the current month are NOT absences yet
                if (date > today) continue;

                // FIX 2: days before the employee joined are NOT absences
                if (date < joiningDate) continue;

                if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
                if (holidaySet.Contains(date)) continue;

                if (!attendanceMap.TryGetValue(date, out var att))
                {
                    lop += 1;
                    continue;
                }

                switch (att.Status)
                {
                    case AttendanceStatus.HalfDay: lop += 0.5m; break;
                    case AttendanceStatus.Absent: lop += 1; break;
                    case AttendanceStatus.OnLeave: break;
                }
            }

            // Unpaid approved leave adds LOP — same clamps (today + joining date)
            lop += await CountUnpaidLeaveDaysAsync(employee.Id, monthStart, monthEnd, holidaySet, today, joiningDate);
            if (lop > workingDays) lop = workingDays;

            var result = PayrollCalculator.Calculate(new PayrollInput
            {
                GrossMonthlySalary = employee.GrossMonthlySalary,
                Basic = structure.Basic,
                Hra = structure.HRA,
                Conveyance = structure.Conveyance,
                SpecialAllowance = structure.SpecialAllowance,
                WorkingDays = workingDays,
                LopDays = lop
            });

            var payslip = new Payslip
            {
                EmployeeId = employee.Id,
                Year = year,
                Month = month,
                WorkingDays = workingDays,
                LopDays = lop,
                PaidDays = result.PaidDays,
                EarnedBasic = result.EarnedBasic,
                EarnedHra = result.EarnedHra,
                EarnedConveyance = result.EarnedConveyance,
                EarnedSpecial = result.EarnedSpecial,
                EarnedGross = result.EarnedGross,
                PfDeduction = result.PfDeduction,
                ProfessionalTax = result.ProfessionalTax,
                TotalDeductions = result.TotalDeductions,
                NetPay = result.NetPay,
                Items = new List<PayslipItem>
                {
                    new() { Name = "Basic",             Type = "Earning",   Amount = result.EarnedBasic },
                    new() { Name = "HRA",               Type = "Earning",   Amount = result.EarnedHra },
                    new() { Name = "Conveyance",        Type = "Earning",   Amount = result.EarnedConveyance },
                    new() { Name = "Special Allowance", Type = "Earning",   Amount = result.EarnedSpecial },
                    new() { Name = "Provident Fund",    Type = "Deduction", Amount = result.PfDeduction },
                    new() { Name = "Professional Tax",  Type = "Deduction", Amount = result.ProfessionalTax }
                }
            };

            _context.Payslips.Add(payslip);
            summary.Generated.Add($"{employee.EmployeeCode} — {employee.FullName}");
        }

        await _context.SaveChangesAsync();
        return summary;
    }

    private async Task<decimal> CountUnpaidLeaveDaysAsync(
        int employeeId, DateOnly monthStart, DateOnly monthEnd,
        HashSet<DateOnly> holidaySet, DateOnly today, DateOnly joiningDate)
    {
        var requests = await _context.LeaveRequests
            .Include(r => r.LeaveType)
            .Where(r => r.EmployeeId == employeeId
                     && r.Status == LeaveStatus.Approved
                     && r.LeaveType!.IsPaid == false
                     && r.StartDate <= monthEnd
                     && r.EndDate >= monthStart)
            .ToListAsync();

        decimal days = 0;
        foreach (var r in requests)
        {
            var start = r.StartDate < monthStart ? monthStart : r.StartDate;
            var end = r.EndDate > monthEnd ? monthEnd : r.EndDate;
            if (end > today) end = today;
            if (start < joiningDate) start = joiningDate;

            for (var d = start; d <= end; d = d.AddDays(1))
            {
                if (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
                if (holidaySet.Contains(d)) continue;
                days++;
            }
        }
        return days;
    }

    public Task<List<MonthSummary>> GetMonthSummariesAsync()
        => _context.Payslips
            .GroupBy(p => new { p.Year, p.Month })
            .Select(g => new MonthSummary
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                PayslipCount = g.Count(),
                TotalNetPay = g.Sum(p => p.NetPay)
            })
            .OrderByDescending(m => m.Year).ThenByDescending(m => m.Month)
            .ToListAsync();

    public Task<List<Payslip>> GetPayslipsForMonthAsync(int year, int month)
        => _context.Payslips
            .Include(p => p.Employee)
            .Where(p => p.Year == year && p.Month == month)
            .OrderBy(p => p.Employee!.EmployeeCode)
            .ToListAsync();

    public Task<Payslip?> GetPayslipAsync(int id)
        => _context.Payslips
            .Include(p => p.Employee).ThenInclude(e => e!.Department)
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id);

    public Task<List<Payslip>> GetMyPayslipsAsync(int employeeId)
        => _context.Payslips
            .Where(p => p.EmployeeId == employeeId)
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .ToListAsync();
}