using HrPayrollSystem.Data;
using HrPayrollSystem.Models;
using HrPayrollSystem.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HrPayrollSystem.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public EmployeeService(IEmployeeRepository employeeRepository,
                           UserManager<ApplicationUser> userManager,
                           ApplicationDbContext context)
    {
        _employeeRepository = employeeRepository;
        _userManager = userManager;
        _context = context;
    }

    // Logins that exist but have NO employee profile yet
    public async Task<List<ApplicationUser>> GetUnlinkedUsersAsync()
    {
        var linkedUserIds = await _context.Employees
            .Where(e => e.IsActive)
            .Select(e => e.ApplicationUserId)
            .ToListAsync();

        return await _userManager.Users
            .Where(u => !linkedUserIds.Contains(u.Id))
            .OrderBy(u => u.Email)
            .ToListAsync();
    }

    public async Task CreateEmployeeAsync(Employee employee, string applicationUserId)
    {
        // RULE 1: the login account must exist
        var user = await _userManager.FindByIdAsync(applicationUserId)
            ?? throw new InvalidOperationException("Selected user account does not exist.");

        // RULE 2: that account must not already be linked to an active employee
        var alreadyLinked = await _context.Employees
            .AnyAsync(e => e.ApplicationUserId == applicationUserId && e.IsActive);

        if (alreadyLinked)
            throw new InvalidOperationException("That user account is already linked to an employee.");

        // RULE 3: unique employee code
        if (await _employeeRepository.IsCodeTakenAsync(employee.EmployeeCode))
            throw new InvalidOperationException($"Employee code '{employee.EmployeeCode}' is already in use.");

        employee.ApplicationUserId = user.Id;
        await _employeeRepository.AddAsync(employee);

        // RULE 4: auto-create leave balances for the new employee (current year)
        await CreateLeaveBalancesAsync(employee.Id);
    }

    public async Task CreateEmployeeWithNewAccountAsync(Employee employee, string email, string password)
    {
        // RULE 5: email must not already be registered
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing != null)
            throw new InvalidOperationException("A user account with this email already exists. Use 'Link existing user' instead.");

        // RULE 6: unique employee code
        if (await _employeeRepository.IsCodeTakenAsync(employee.EmployeeCode))
            throw new InvalidOperationException($"Employee code '{employee.EmployeeCode}' is already in use.");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Could not create account: {errors}");
        }

        employee.ApplicationUserId = user.Id;
        await _employeeRepository.AddAsync(employee);

        await CreateLeaveBalancesAsync(employee.Id);
    }

    public async Task UpdateEmployeeAsync(Employee employee)
    {
        if (await _employeeRepository.IsCodeTakenAsync(employee.EmployeeCode))
        {
            var existing = await _employeeRepository.GetByIdAsync(employee.Id);
            if (existing == null || existing.EmployeeCode != employee.EmployeeCode)
                throw new InvalidOperationException($"Employee code '{employee.EmployeeCode}' is already in use.");
        }

        await _employeeRepository.UpdateAsync(employee);
    }

    public async Task DeactivateAsync(int employeeId)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId)
            ?? throw new InvalidOperationException("Employee not found.");

        employee.IsActive = false;   // soft delete — history (attendance/payslips) stays intact
        await _employeeRepository.UpdateAsync(employee);
    }

    public async Task AddSalaryStructureAsync(SalaryStructure salary)
    {
        // RULE 7: one structure per employee (DB also enforces this)
        var exists = await _context.SalaryStructures
            .AnyAsync(s => s.EmployeeId == salary.EmployeeId);

        if (exists)
            throw new InvalidOperationException("This employee already has a salary structure. Use edit instead.");

        // RULE 8: components must add up to the gross salary
        var total = salary.Basic + salary.HRA + salary.Conveyance + salary.SpecialAllowance;
        var employee = await _employeeRepository.GetByIdAsync(salary.EmployeeId)
            ?? throw new InvalidOperationException("Employee not found.");

        if (total != employee.GrossMonthlySalary)
            throw new InvalidOperationException(
                $"Components total {total:N0} but gross salary is {employee.GrossMonthlySalary:N0}. They must match exactly.");

        _context.SalaryStructures.Add(salary);
        await _context.SaveChangesAsync();
    }

    public Task<SalaryStructure?> GetSalaryStructureAsync(int employeeId)
        => _context.SalaryStructures.FirstOrDefaultAsync(s => s.EmployeeId == employeeId);

    // Seed-style: full quota for every leave type, current year
    private async Task CreateLeaveBalancesAsync(int employeeId)
    {
        var leaveTypes = await _context.LeaveTypes.ToListAsync();
        var year = DateTime.Now.Year;

        foreach (var lt in leaveTypes)
        {
            _context.LeaveBalances.Add(new LeaveBalance
            {
                EmployeeId = employeeId,
                LeaveTypeId = lt.Id,
                Year = year,
                RemainingDays = lt.AnnualQuota
            });
        }
        await _context.SaveChangesAsync();
    }
}