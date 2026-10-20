using HrPayrollSystem.Data;
using HrPayrollSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HrPayrollSystem.Services;

public class LeaveService : ILeaveService
{
    private readonly ApplicationDbContext _context;

    public LeaveService(ApplicationDbContext context) { _context = context; }

    private static decimal CountWorkingDays(DateOnly start, DateOnly end)
    {
        decimal count = 0;
        for (var d = start; d <= end; d = d.AddDays(1))
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday) count++;
        return count;
    }

    public async Task<LeaveRequest> ApplyAsync(int employeeId, int leaveTypeId,
        DateOnly startDate, DateOnly endDate, string? reason)
    {
        if (endDate < startDate)
            throw new InvalidOperationException("End date cannot be before start date.");
        if (endDate < DateOnly.FromDateTime(DateTime.Now))
            throw new InvalidOperationException("You cannot apply for leave in the past.");

        var days = CountWorkingDays(startDate, endDate);
        if (days <= 0)
            throw new InvalidOperationException("The selected range contains no working days.");

        var hasOverlap = await _context.LeaveRequests.AnyAsync(r =>
            r.EmployeeId == employeeId &&
            (r.Status == LeaveStatus.Pending || r.Status == LeaveStatus.Approved) &&
            r.StartDate <= endDate && r.EndDate >= startDate);
        if (hasOverlap)
            throw new InvalidOperationException("You already have a leave request overlapping these dates.");

        var leaveType = await _context.LeaveTypes.FindAsync(leaveTypeId)
            ?? throw new InvalidOperationException("Invalid leave type selected.");

        var year = startDate.Year;
        var balance = await _context.LeaveBalances.FirstOrDefaultAsync(b =>
            b.EmployeeId == employeeId && b.LeaveTypeId == leaveTypeId && b.Year == year);
        if (balance == null)
            throw new InvalidOperationException($"No leave balance found for {leaveType.Name} in {year}.");
        if (balance.RemainingDays < days)
            throw new InvalidOperationException(
                $"Insufficient balance: you asked for {days:0.##} day(s) but only have {balance.RemainingDays:0.##} left of {leaveType.Name}.");

        var request = new LeaveRequest
        {
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            StartDate = startDate,
            EndDate = endDate,
            NumberOfDays = days,
            Reason = reason,
            Status = LeaveStatus.Pending,
            AppliedOn = DateTime.UtcNow
        };
        _context.LeaveRequests.Add(request);
        await _context.SaveChangesAsync();
        return request;
    }

    public async Task ApproveAsync(int requestId, string reviewerUserId, string? remarks, int? restrictToDepartmentId = null)
    {
        var request = await _context.LeaveRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == requestId)
            ?? throw new InvalidOperationException("Leave request not found.");

        if (request.Status != LeaveStatus.Pending)
            throw new InvalidOperationException("This request was already reviewed.");

        if (restrictToDepartmentId.HasValue &&
            request.Employee?.DepartmentId != restrictToDepartmentId.Value)
            throw new InvalidOperationException("This request belongs to another department.");

        var year = request.StartDate.Year;
        var balance = await _context.LeaveBalances.FirstOrDefaultAsync(b =>
            b.EmployeeId == request.EmployeeId && b.LeaveTypeId == request.LeaveTypeId && b.Year == year);
        if (balance == null) throw new InvalidOperationException("No leave balance record found.");
        if (balance.RemainingDays < request.NumberOfDays)
            throw new InvalidOperationException("Insufficient balance at approval time.");

        balance.RemainingDays -= request.NumberOfDays;
        request.Status = LeaveStatus.Approved;
        request.ReviewedByUserId = reviewerUserId;
        request.ReviewedOn = DateTime.UtcNow;
        request.ReviewRemarks = remarks;

        for (var d = request.StartDate; d <= request.EndDate; d = d.AddDays(1))
        {
            if (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            var exists = await _context.Attendances.AnyAsync(a => a.EmployeeId == request.EmployeeId && a.Date == d);
            if (!exists)
                _context.Attendances.Add(new Attendance
                { EmployeeId = request.EmployeeId, Date = d, Status = AttendanceStatus.OnLeave });
        }
        await _context.SaveChangesAsync();
    }

    public async Task RejectAsync(int requestId, string reviewerUserId, string? remarks, int? restrictToDepartmentId = null)
    {
        var request = await _context.LeaveRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == requestId)
            ?? throw new InvalidOperationException("Leave request not found.");

        if (request.Status != LeaveStatus.Pending)
            throw new InvalidOperationException("This request was already reviewed.");

        if (restrictToDepartmentId.HasValue &&
            request.Employee?.DepartmentId != restrictToDepartmentId.Value)
            throw new InvalidOperationException("This request belongs to another department.");

        request.Status = LeaveStatus.Rejected;
        request.ReviewedByUserId = reviewerUserId;
        request.ReviewedOn = DateTime.UtcNow;
        request.ReviewRemarks = remarks;
        await _context.SaveChangesAsync();
    }

    public Task<List<LeaveType>> GetLeaveTypesAsync()
        => _context.LeaveTypes.OrderBy(t => t.Id).ToListAsync();

    public Task<List<LeaveBalance>> GetBalancesAsync(int employeeId)
        => _context.LeaveBalances.Include(b => b.LeaveType)
            .Where(b => b.EmployeeId == employeeId).OrderBy(b => b.LeaveTypeId).ToListAsync();

    public Task<List<LeaveRequest>> GetMyRequestsAsync(int employeeId)
        => _context.LeaveRequests.Include(r => r.LeaveType)
            .Where(r => r.EmployeeId == employeeId).OrderByDescending(r => r.AppliedOn).ToListAsync();

    public async Task<List<LeaveRequest>> GetPendingRequestsAsync(int? departmentId = null)
    {
        var query = _context.LeaveRequests
            .Include(r => r.LeaveType).Include(r => r.Employee)
            .Where(r => r.Status == LeaveStatus.Pending);

        if (departmentId.HasValue)
            query = query.Where(r => r.Employee!.DepartmentId == departmentId.Value);

        return await query.OrderBy(r => r.AppliedOn).ToListAsync();
    }
}