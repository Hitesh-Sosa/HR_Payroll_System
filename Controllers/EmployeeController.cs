using HrPayrollSystem.Models;
using HrPayrollSystem.Models.ViewModels;
using HrPayrollSystem.Repositories;
using HrPayrollSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HrPayrollSystem.Controllers;

[Authorize]   // 🔒 any logged-in user can see THEIR OWN dashboard (but only their own!)
public class EmployeeController : Controller
{
    private readonly IAttendanceService _attendanceService;
    private readonly IEmployeeRepository _employeeRepository;

    public EmployeeController(IAttendanceService attendanceService,
                              IEmployeeRepository employeeRepository)
    {
        _attendanceService = attendanceService;
        _employeeRepository = employeeRepository;
    }

    
    public async Task<IActionResult> Dashboard()
    {
        // Claims = the identity card inside your login cookie.
        // NameIdentifier = the AspNetUsers.Id of the logged-in user.
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var employee = await _employeeRepository.GetByUserIdAsync(userId);

        if (employee == null)
        {
             
            return View(new AttendanceDashboardViewModel());   // EmployeeId = 0
        }

        var now = DateTime.Now;

        var today = await _attendanceService.GetTodayAttendanceAsync(employee.Id);
        var monthly = await _attendanceService.GetMonthlyAttendanceAsync(employee.Id, now.Year, now.Month);

        var vm = new AttendanceDashboardViewModel
        {
            EmployeeId = employee.Id,
            EmployeeName = employee.FullName,
            EmployeeCode = employee.EmployeeCode,
            DepartmentName = employee.Department?.Name ?? "—",
            TodayAttendance = today,
            MonthlyAttendance = monthly,
            ServerTime = TimeOnly.FromDateTime(now)
        };

        return View(vm);
    }

    // POST /Employee/CheckIn  (button click)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employee = await _employeeRepository.GetByUserIdAsync(userId);

        if (employee == null)
        {
            TempData["Error"] = "No employee profile is linked to your account.";
            return RedirectToAction(nameof(Dashboard));
        }

        try
        {
            await _attendanceService.CheckInAsync(employee.Id);
            TempData["Success"] = "Checked in successfully. Have a great day!";
        }
        catch (InvalidOperationException ex)
        {
            // Your service rules arrive here as FRIENDLY messages
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Dashboard));   // PRG: Post → Redirect → Get
    }

    // POST /Employee/CheckOut  (button click)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckOut()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employee = await _employeeRepository.GetByUserIdAsync(userId);

        if (employee == null)
        {
            TempData["Error"] = "No employee profile is linked to your account.";
            return RedirectToAction(nameof(Dashboard));
        }

        try
        {
            var attendance = await _attendanceService.CheckOutAsync(employee.Id);
            TempData["Success"] = $"Checked out successfully. Hours worked: {attendance.HoursWorked:0.00}.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Dashboard));
    }
}