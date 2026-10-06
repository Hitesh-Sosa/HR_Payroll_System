using HrPayrollSystem.Models;

namespace HrPayrollSystem.Services;

public interface IAttendanceService
{
    Task<Attendance> CheckInAsync(int employeeId);
    Task<Attendance> CheckOutAsync(int employeeId);
    Task<Attendance?> GetTodayAttendanceAsync(int employeeId);
    Task<List<Attendance>> GetMonthlyAttendanceAsync(int employeeId, int year, int month);
}