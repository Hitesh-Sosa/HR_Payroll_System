using System.ComponentModel.DataAnnotations;

namespace HrPayrollSystem.Models;

// Shape of the Apply FORM
public class ApplyLeaveViewModel
{
    [Required(ErrorMessage = "Please choose a leave type.")]
    public int LeaveTypeId { get; set; }

    [Required(ErrorMessage = "Start date is required.")]
    public DateOnly? StartDate { get; set; }

    [Required(ErrorMessage = "End date is required.")]
    public DateOnly? EndDate { get; set; }

    public string? Reason { get; set; }
}

// Shape of the Index PAGE
public class LeavePageViewModel
{
    public List<LeaveBalance> Balances { get; set; } = new();
    public List<LeaveRequest> MyRequests { get; set; } = new();
    public List<LeaveType> LeaveTypes { get; set; } = new();
}
