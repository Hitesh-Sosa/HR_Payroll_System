namespace HrPayrollSystem.Models;

public class LeaveBalance
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public int LeaveTypeId { get; set; }
    public LeaveType? LeaveType { get; set; }

    public int Year { get; set; }                     
    public decimal RemainingDays { get; set; }          

    
}