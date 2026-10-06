using HrPayrollSystem.Models;

namespace HrPayrollSystem.Repositories;

public interface IEmployeeRepository
{
    Task<List<Employee>> GetAllAsync();
    Task<List<Employee>> GetActiveAsync();
    Task<Employee?> GetByIdAsync(int id);
    Task<Employee?> GetByUserIdAsync(string applicationUserId);
    Task<bool> IsCodeTakenAsync(string employeeCode);
    Task AddAsync(Employee employee);
    Task UpdateAsync(Employee employee);
    Task DeleteAsync(int id);
}