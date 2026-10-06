namespace HrPayrollSystem.Models;

public class Employee
{
    public int Id { get; set; }

    // Links employee to their LOGIN account
    public string ApplicationUserId { get; set; } = string.Empty;

    public string EmployeeCode { get; set; } = string.Empty;   // EMP001
    public string FullName { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public DateTime JoiningDate { get; set; }
    public decimal GrossMonthlySalary { get; set; }
    public bool IsActive { get; set; } = true;

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}