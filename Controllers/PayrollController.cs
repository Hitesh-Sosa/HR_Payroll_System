using HrPayrollSystem.Models;
using HrPayrollSystem.Repositories;
using HrPayrollSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HrPayrollSystem.Controllers;

[Authorize]
public class PayrollController : Controller
{
    private readonly IPayrollService _payrollService;
    private readonly IEmployeeRepository _employeeRepository;

    public PayrollController(IPayrollService payrollService, IEmployeeRepository employeeRepository)
    {
        _payrollService = payrollService;
        _employeeRepository = employeeRepository;
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index()
    {
        var months = await _payrollService.GetMonthSummariesAsync();
        return View(months);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Run(int year, int month)
    {
        try
        {
            var summary = await _payrollService.RunPayrollAsync(year, month);
            TempData["Success"] =
                $"Payroll {month:00}/{year}: {summary.WorkingDays} working days. " +
                $"Generated {summary.Generated.Count} payslip(s)." +
                (summary.Skipped.Count > 0
                    ? $" Skipped {summary.Skipped.Count}: {string.Join(", ", summary.Skipped)}" : "");
            return RedirectToAction(nameof(Month), new { year, month });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Month(int year, int month)
    {
        var payslips = await _payrollService.GetPayslipsForMonthAsync(year, month);
        ViewBag.Year = year;
        ViewBag.Month = month;
        return View(payslips);
    }

    // Admin/HR: any payslip • everyone else: OWN payslip only
    public async Task<IActionResult> Details(int id)
    {
        var payslip = await _payrollService.GetPayslipAsync(id);
        if (payslip == null) return NotFound();

        if (!User.IsInRole("Admin") && !User.IsInRole("HR"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var employee = await _employeeRepository.GetByUserIdAsync(userId);
            if (employee == null || payslip.EmployeeId != employee.Id)
                return Forbid();
        }
        return View(payslip);
    }

    public async Task<IActionResult> DownloadPdf(int id, [FromServices] PayslipPdfService pdfService)
    {
        var payslip = await _payrollService.GetPayslipAsync(id);
        if (payslip == null) return NotFound();

        if (!User.IsInRole("Admin") && !User.IsInRole("HR"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var employee = await _employeeRepository.GetByUserIdAsync(userId);
            if (employee == null || payslip.EmployeeId != employee.Id)
                return Forbid();
        }

        var bytes = pdfService.Generate(payslip);
        var fileName = $"Payslip_{payslip.Employee?.EmployeeCode}_{payslip.Year}-{payslip.Month:D2}.pdf";
        return File(bytes, "application/pdf", fileName);
    }

    [Authorize]
    public async Task<IActionResult> MyPayslips()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var employee = await _employeeRepository.GetByUserIdAsync(userId);
        if (employee == null)
        {
            TempData["Error"] = "No employee profile is linked to your account.";
            return RedirectToAction("Dashboard", "Employee");
        }
        var payslips = await _payrollService.GetMyPayslipsAsync(employee.Id);
        return View(payslips);
    }
}