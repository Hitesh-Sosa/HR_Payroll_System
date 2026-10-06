using HrPayrollSystem.Models;
using HrPayrollSystem.Repositories;
using HrPayrollSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HrPayrollSystem.Controllers;

[Authorize]
public class LeaveController : Controller
{
    private readonly ILeaveService _leaveService;
    private readonly IEmployeeRepository _employeeRepository;

    public LeaveController(ILeaveService leaveService, IEmployeeRepository employeeRepository)
    {
        _leaveService = leaveService;
        _employeeRepository = employeeRepository;
    }

    // GET /Leave — balances + history + apply form
    public async Task<IActionResult> Index()
    {
        var employee = await GetCurrentEmployeeAsync();
        if (employee == null)
        {
            TempData["Error"] = "No employee profile is linked to your account.";
            return RedirectToAction("Dashboard", "Employee");
        }

        var vm = new LeavePageViewModel
        {
            Balances = await _leaveService.GetBalancesAsync(employee.Id),
            MyRequests = await _leaveService.GetMyRequestsAsync(employee.Id),
            LeaveTypes = await _leaveService.GetLeaveTypesAsync()
        };
        return View(vm);
    }

    // POST /Leave/Apply
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(ApplyLeaveViewModel form)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }

        var employee = await GetCurrentEmployeeAsync();
        if (employee == null)
        {
            TempData["Error"] = "No employee profile is linked to your account.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _leaveService.ApplyAsync(employee.Id, form.LeaveTypeId,
                form.StartDate!.Value, form.EndDate!.Value, form.Reason);
            TempData["Success"] = "Leave request submitted. Waiting for approval.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));   // PRG
    }

    // GET /Leave/Pending — approval queue (Manager/Admin only)
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Pending()
    {
        var requests = await _leaveService.GetPendingRequestsAsync();
        return View(requests);
    }

    // POST /Leave/Approve/5
    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? remarks)
    {
        try
        {
            await _leaveService.ApproveAsync(id, CurrentUserId(), remarks);
            TempData["Success"] = "Approved: balance deducted + attendance marked On Leave.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Pending));
    }

    // POST /Leave/Reject/5
    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? remarks)
    {
        try
        {
            await _leaveService.RejectAsync(id, CurrentUserId(), remarks);
            TempData["Success"] = "Leave rejected (no balance change).";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Pending));
    }

    private async Task<Employee?> GetCurrentEmployeeAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return await _employeeRepository.GetByUserIdAsync(userId);
    }

    private string CurrentUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}