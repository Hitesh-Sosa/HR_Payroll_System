using HrPayrollSystem.Models;
using HrPayrollSystem.Repositories;
using HrPayrollSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrPayrollSystem.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IEmployeeService _employeeService;

    public AdminController(IDepartmentRepository departmentRepository,
                           IEmployeeRepository employeeRepository,
                           IEmployeeService employeeService)
    {
        _departmentRepository = departmentRepository;
        _employeeRepository = employeeRepository;
        _employeeService = employeeService;
    }

    // ============ DASHBOARD ============
    public async Task<IActionResult> Index()
    {
        await PopulateDepartmentsAsync();   // ← THE FIX: view needs this!

        var employees = await _employeeRepository.GetAllAsync();
        return View(employees);
    }

    // ============ DEPARTMENTS ============
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDepartment(string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Department name is required.");

            if (await _departmentRepository.NameExistsAsync(name.Trim()))
                throw new InvalidOperationException($"Department '{name.Trim()}' already exists.");

            await _departmentRepository.AddAsync(new Department { Name = name.Trim() });
            TempData["Success"] = $"Department '{name.Trim()}' created.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        try
        {
            var employees = await _employeeRepository.GetAllAsync();
            if (employees.Any(e => e.DepartmentId == id && e.IsActive))
                throw new InvalidOperationException("Cannot delete: active employees exist in this department.");

            await _departmentRepository.DeleteAsync(id);
            TempData["Success"] = "Department deleted.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }

        return RedirectToAction(nameof(Index));
    }

    // ============ CREATE EMPLOYEE ============
    [HttpGet]
    public async Task<IActionResult> CreateEmployee()
    {
        await PopulateDepartmentsAsync();
        return View(new CreateEmployeeViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEmployee(CreateEmployeeViewModel form)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDepartmentsAsync();
            return View(form);
        }

        try
        {
            var employee = new Employee
            {
                EmployeeCode = form.EmployeeCode.Trim().ToUpper(),
                FullName = form.FullName.Trim(),
                Designation = form.Designation.Trim(),
                JoiningDate = form.JoiningDate!.Value,
                GrossMonthlySalary = form.GrossMonthlySalary!.Value,
                DepartmentId = form.DepartmentId,
                IsActive = true
            };

            if (form.LinkExistingUser)
            {
                await _employeeService.CreateEmployeeAsync(employee, form.SelectedUserId!);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(form.NewEmail) || string.IsNullOrWhiteSpace(form.NewPassword))
                    throw new InvalidOperationException("New email and password are required.");

                await _employeeService.CreateEmployeeWithNewAccountAsync(
                    employee, form.NewEmail.Trim(), form.NewPassword);
            }

            TempData["Success"] = $"Employee '{employee.FullName}' created with leave balances.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateDepartmentsAsync();
            return View(form);
        }
    }

    // ============ EDIT EMPLOYEE ============
    [HttpGet]
    public async Task<IActionResult> EditEmployee(int id)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);
        if (employee == null) return NotFound();

        await PopulateDepartmentsAsync();
        return View(employee);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEmployee(int id, Employee form)
    {
        if (id != form.Id) return NotFound();

        try
        {
            var employee = await _employeeRepository.GetByIdAsync(id);
            if (employee == null) return NotFound();

            employee.FullName = form.FullName.Trim();
            employee.Designation = form.Designation.Trim();
            employee.DepartmentId = form.DepartmentId;
            employee.GrossMonthlySalary = form.GrossMonthlySalary;
            employee.JoiningDate = form.JoiningDate;
            employee.IsActive = form.IsActive;

            await _employeeService.UpdateEmployeeAsync(employee);
            TempData["Success"] = "Employee updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateDepartmentsAsync();
            return View(form);
        }
    }

    // ============ DEACTIVATE ============
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        try
        {
            await _employeeService.DeactivateAsync(id);
            TempData["Success"] = "Employee deactivated (history preserved).";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    // ============ SALARY STRUCTURE ============
    [HttpGet]
    public async Task<IActionResult> Salary(int employeeId)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);
        if (employee == null) return NotFound();

        var existing = await _employeeService.GetSalaryStructureAsync(employeeId);
        if (existing != null)
        {
            TempData["Error"] = "Salary structure already exists for this employee.";
            return RedirectToAction(nameof(Index));
        }

        return View(new SalaryFormViewModel
        {
            EmployeeId = employee.Id,
            EmployeeName = employee.FullName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salary(SalaryFormViewModel form)
    {
        if (!ModelState.IsValid)
            return View(form);

        try
        {
            await _employeeService.AddSalaryStructureAsync(new SalaryStructure
            {
                EmployeeId = form.EmployeeId,
                Basic = form.Basic!.Value,
                HRA = form.HRA!.Value,
                Conveyance = form.Conveyance!.Value,
                SpecialAllowance = form.SpecialAllowance!.Value
            });
            TempData["Success"] = "Salary structure saved. Ready for payroll!";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(form);
        }
    }

    private async Task PopulateDepartmentsAsync()
    {
        ViewBag.Departments = await _departmentRepository.GetAllAsync();
        ViewBag.UnlinkedUsers = await _employeeService.GetUnlinkedUsersAsync();
    }
}
