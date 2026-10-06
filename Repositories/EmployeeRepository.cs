using HrPayrollSystem.Data;
using HrPayrollSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HrPayrollSystem.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly ApplicationDbContext _context;

    public EmployeeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<Employee>> GetAllAsync()
        => _context.Employees.Include(e => e.Department).OrderBy(e => e.EmployeeCode).ToListAsync();

    public Task<List<Employee>> GetActiveAsync()
        => _context.Employees.Include(e => e.Department)
            .Where(e => e.IsActive).OrderBy(e => e.EmployeeCode).ToListAsync();

    public Task<Employee?> GetByIdAsync(int id)
        => _context.Employees.Include(e => e.Department).FirstOrDefaultAsync(e => e.Id == id);

    public Task<Employee?> GetByUserIdAsync(string applicationUserId)
        => _context.Employees.Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.ApplicationUserId == applicationUserId);

    public async Task<bool> IsCodeTakenAsync(string employeeCode)
        => await _context.Employees.AnyAsync(e => e.EmployeeCode == employeeCode);

    public async Task AddAsync(Employee employee)
    {
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Employee employee)
    {
        _context.Employees.Update(employee);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee != null)
        {
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
        }
    }
}