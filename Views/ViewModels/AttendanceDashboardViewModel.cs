using HrPayrollSystem.Models;

namespace HrPayrollSystem.Models.ViewModels;

public class AttendanceDashboardViewModel
{
    public int EmployeeId { get; set; }                    
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;

    public Attendance? TodayAttendance { get; set; }        
    public List<Attendance> MonthlyAttendance { get; set; } = new();

    public TimeOnly ServerTime { get; set; }              
}