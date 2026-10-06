namespace HrPayrollSystem.Models;

public class Attendance
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public DateOnly Date { get; set; }
    public TimeOnly? CheckIn { get; set; }
    public TimeOnly? CheckOut { get; set; }
    public decimal HoursWorked { get; set; }
    public AttendanceStatus Status { get; set; }
}