using HrPayrollSystem.Models;

namespace HrPayrollSystem.Services;

public interface ILeaveService
{
    Task<List<LeaveType>> GetLeaveTypesAsync();
    Task<List<LeaveBalance>> GetBalancesAsync(int employeeId);
    Task<List<LeaveRequest>> GetMyRequestsAsync(int employeeId);

    // departmentId: Managers see only their department; Admin/HR pass null (all)
    Task<List<LeaveRequest>> GetPendingRequestsAsync(int? departmentId = null);

    Task<LeaveRequest> ApplyAsync(int employeeId, int leaveTypeId, DateOnly startDate, DateOnly endDate, string? reason);

    // restrictToDepartmentId: managers can only review their own department
    Task ApproveAsync(int requestId, string reviewerUserId, string? remarks, int? restrictToDepartmentId = null);
    Task RejectAsync(int requestId, string reviewerUserId, string? remarks, int? restrictToDepartmentId = null);
}