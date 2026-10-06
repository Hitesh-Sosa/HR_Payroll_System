namespace HrPayrollSystem.Models;

public class LeaveType
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;    
	public int AnnualQuota { get; set; }               
	public bool IsPaid { get; set; } = true;          

	public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
	public ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
}