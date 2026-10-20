using HrPayrollSystem.Models;
using HrPayrollSystem.Repositories;
using HrPayrollSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HrPayrollSystem.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IEmployeeRepository _employeeRepository;

    public DashboardController(IDashboardService dashboardService, IEmployeeRepository employeeRepository)
    {
        _dashboardService = dashboardService;
        _employeeRepository = employeeRepository;
    }

    // SMART DOOR: /Dashboard → the right dashboard for your role
    public IActionResult Index()
    {
        if (User.IsInRole("Admin")) return RedirectToAction(nameof(Admin));
        if (User.IsInRole("HR")) return RedirectToAction(nameof(Hr));
        if (User.IsInRole("Manager")) return RedirectToAction(nameof(Manager));
        return RedirectToAction(nameof(Employee));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Admin()
    {
        var vm = await _dashboardService.GetAdminStatsAsync();
        return View(vm);
    }

    [Authorize(Roles = "HR")]
    public async Task<IActionResult> Hr()
    {
        var vm = await _dashboardService.GetHrStatsAsync();
        return View(vm);
    }

    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Manager()
    {
        var employee = await GetCurrentEmployeeAsync();
        if (employee == null) return RedirectToAction("Dashboard", "Employee");
        var vm = await _dashboardService.GetManagerStatsAsync(employee);
        return View(vm);
    }

    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> Employee()
    {
        var employee = await GetCurrentEmployeeAsync();
        if (employee == null) return RedirectToAction("Dashboard", "Employee");
        var vm = await _dashboardService.GetEmployeeHomeAsync(employee);
        return View(vm);
    }

    private async Task<Employee?> GetCurrentEmployeeAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return await _employeeRepository.GetByUserIdAsync(userId);
    }
}