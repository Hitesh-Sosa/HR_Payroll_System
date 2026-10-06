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

    public PayrollController(
        IPayrollService payrollService,
        IEmployeeRepository employeeRepository)
    {
        _payrollService = payrollService;
        _employeeRepository = employeeRepository;
    }

    // GET /Payroll
    // Admin only — run payroll and view payroll history
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index()
    {
        var months = await _payrollService.GetMonthSummariesAsync();
        return View(months);
    }

    // POST /Payroll/Run
    // Admin only — run payroll for a month
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Run(int year, int month)
    {
        try
        {
            var summary = await _payrollService.RunPayrollAsync(year, month);

            TempData["Success"] =
                $"Payroll {month:00}/{year}: {summary.WorkingDays} working days. " +
                $"Generated {summary.Generated.Count} payslip(s)." +
                (summary.Skipped.Count > 0
                    ? $" Skipped {summary.Skipped.Count}: {string.Join(", ", summary.Skipped)}"
                    : "");

            return RedirectToAction(nameof(Month), new { year, month });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    // GET /Payroll/Month?year=2026&month=10
    // Admin only
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Month(int year, int month)
    {
        var payslips = await _payrollService.GetPayslipsForMonthAsync(year, month);

        ViewBag.Year = year;
        ViewBag.Month = month;

        return View(payslips);
    }

    // GET /Payroll/Details/5
    // Admin OR the owning employee
    [Authorize]
    public async Task<IActionResult> Details(int id)
    {
        var payslip = await _payrollService.GetPayslipAsync(id);

        if (payslip == null)
            return NotFound();

        // Admin can see every payslip
        if (!User.IsInRole("Admin"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Forbid();

            var employee = await _employeeRepository.GetByUserIdAsync(userId);

            // Employee can see only their own payslip
            if (employee == null || payslip.EmployeeId != employee.Id)
                return Forbid();
        }

        return View(payslip);
    }

    // GET /Payroll/MyPayslips
    // Logged-in employee can see their own payslips
    [Authorize]
    public async Task<IActionResult> MyPayslips()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Forbid();

        var employee = await _employeeRepository.GetByUserIdAsync(userId);

        if (employee == null)
        {
            TempData["Error"] =
                "No employee profile is linked to your account.";

            return RedirectToAction("Dashboard", "Employee");
        }

        var payslips =
            await _payrollService.GetMyPayslipsAsync(employee.Id);

        return View(payslips);
    }

    // GET /Payroll/DownloadPdf/5
    // Admin OR the owning employee
    [Authorize]
    public async Task<IActionResult> DownloadPdf(
        int id,
        [FromServices] PayslipPdfService pdfService)
    {
        var payslip = await _payrollService.GetPayslipAsync(id);

        if (payslip == null)
            return NotFound();

        // Admin can download any payslip
        if (!User.IsInRole("Admin"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Forbid();

            var employee =
                await _employeeRepository.GetByUserIdAsync(userId);

            // Employee can download only their own payslip
            if (employee == null || payslip.EmployeeId != employee.Id)
                return Forbid();
        }

        // Generate PDF
        var bytes = pdfService.Generate(payslip);

        var employeeCode =
            payslip.Employee?.EmployeeCode ?? "Employee";

        var fileName =
            $"Payslip_{employeeCode}_{payslip.Year}-{payslip.Month:D2}.pdf";

        return File(
            bytes,
            "application/pdf",
            fileName);
    }
}
