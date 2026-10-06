using HrPayrollSystem.Data;
using HrPayrollSystem.Models;

namespace HrPayrollSystem.Services;

public interface IEmployeeService
{
    Task<List<ApplicationUser>> GetUnlinkedUsersAsync();
    Task CreateEmployeeAsync(Employee employee, string applicationUserId);
    Task CreateEmployeeWithNewAccountAsync(Employee employee, string email, string password);
    Task UpdateEmployeeAsync(Employee employee);
    Task DeactivateAsync(int employeeId);
    Task AddSalaryStructureAsync(SalaryStructure salary);
    Task<SalaryStructure?> GetSalaryStructureAsync(int employeeId);
}
