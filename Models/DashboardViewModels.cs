using HrPayrollSystem.Models;

namespace HrPayrollSystem.Models;

public class AdminDashboardViewModel
{
    public int ActiveEmployees { get; set; }
    public int Departments { get; set; }
    public int PendingLeaves { get; set; }
    public decimal LatestMonthNetPay { get; set; }
    public string LatestMonthLabel { get; set; } = "—";

    public Dictionary<string, int> AttendanceSummary { get; set; } = new();
    public int UnlinkedUsersCount { get; set; }
    public List<string> UnlinkedEmails { get; set; } = new();
    public List<LeaveRequest> LatestLeaveRequests { get; set; } = new();
}

public class HrDashboardViewModel
{
    public int ActiveEmployees { get; set; }
    public int UnlinkedUsersCount { get; set; }
    public int PendingLeaves { get; set; }
    public int HolidaysThisMonth { get; set; }
    public Dictionary<string, int> AttendanceSummary { get; set; } = new();
    public List<LeaveRequest> PendingLeaveRequests { get; set; } = new();
}

public class TeamMemberToday
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Status { get; set; }
}

public class ManagerDashboardViewModel
{
    public string ManagerName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = "Not assigned";
    public bool HasDepartment { get; set; }

    public int TeamHeadcount { get; set; }
    public int PresentToday { get; set; }
    public int PendingLeaves { get; set; }

    public List<TeamMemberToday> TeamToday { get; set; } = new();
    public List<LeaveRequest> PendingTeamLeaves { get; set; } = new();

    public Attendance? MyToday { get; set; }
    public List<LeaveBalance> MyBalances { get; set; } = new();
}

public class EmployeeHomeViewModel
{
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = "—";

    public Attendance? Today { get; set; }
    public Dictionary<string, int> MonthSummary { get; set; } = new();
    public List<Attendance> MonthlyAttendance { get; set; } = new();
    public List<LeaveBalance> Balances { get; set; } = new();
    public Payslip? LatestPayslip { get; set; }
    public List<Holiday> UpcomingHolidays { get; set; } = new();
}