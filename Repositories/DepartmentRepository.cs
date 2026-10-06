using HrPayrollSystem.Data;
using HrPayrollSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HrPayrollSystem.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly ApplicationDbContext _context;

    public DepartmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<Department>> GetAllAsync()
        => _context.Departments.OrderBy(d => d.Name).ToListAsync();

    public Task<Department?> GetByIdAsync(int id)
        => _context.Departments.FirstOrDefaultAsync(d => d.Id == id);

    public async Task<bool> NameExistsAsync(string name)
        => await _context.Departments.AnyAsync(d => d.Name == name);

    public async Task AddAsync(Department department)
    {
        _context.Departments.Add(department);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Department department)
    {
        _context.Departments.Update(department);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var dept = await _context.Departments.FindAsync(id);
        if (dept != null)
        {
            _context.Departments.Remove(dept);
            await _context.SaveChangesAsync();
        }
    }
}
