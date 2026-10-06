namespace HrPayrollSystem.Models;

public class LeaveRequest
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public int LeaveTypeId { get; set; }
    public LeaveType? LeaveType { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal NumberOfDays { get; set; }           

    public string? Reason { get; set; }                // employee's note

    public LeaveStatus Status { get; set; } = LeaveStatus.Pending; // By default
    public DateTime AppliedOn { get; set; } = DateTime.UtcNow;

    // Who approved/rejected (filled later by manager action)
    public string? ReviewedByUserId { get; set; }
    public DateTime? ReviewedOn { get; set; }
    public string? ReviewRemarks { get; set; }
}