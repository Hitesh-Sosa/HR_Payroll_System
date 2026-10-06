using HrPayrollSystem.Models;

namespace HrPayrollSystem.Services;

public interface ILeaveService
{
    Task<List<LeaveType>> GetLeaveTypesAsync();
    Task<List<LeaveBalance>> GetBalancesAsync(int employeeId);
    Task<List<LeaveRequest>> GetMyRequestsAsync(int employeeId);
    Task<List<LeaveRequest>> GetPendingRequestsAsync();
    Task<LeaveRequest> ApplyAsync(int employeeId, int leaveTypeId, DateOnly startDate, DateOnly endDate, string? reason);
    Task ApproveAsync(int requestId, string reviewerUserId, string? remarks);
    Task RejectAsync(int requestId, string reviewerUserId, string? remarks);
}