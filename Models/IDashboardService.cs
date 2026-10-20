using HrPayrollSystem.Models;

namespace HrPayrollSystem.Services;

public interface IDashboardService
{
    Task<AdminDashboardViewModel> GetAdminStatsAsync();
    Task<HrDashboardViewModel> GetHrStatsAsync();
    Task<ManagerDashboardViewModel> GetManagerStatsAsync(Employee manager);
    Task<EmployeeHomeViewModel> GetEmployeeHomeAsync(Employee employee);
}