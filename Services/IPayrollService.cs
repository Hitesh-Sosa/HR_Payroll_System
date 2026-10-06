namespace HrPayrollSystem.Services;

public interface IPayrollService
{
    Task<PayrollRunSummary> RunPayrollAsync(int year, int month);
    Task<List<MonthSummary>> GetMonthSummariesAsync();
    Task<List<HrPayrollSystem.Models.Payslip>> GetPayslipsForMonthAsync(int year, int month);
    Task<HrPayrollSystem.Models.Payslip?> GetPayslipAsync(int id);
    Task<List<HrPayrollSystem.Models.Payslip>> GetMyPayslipsAsync(int employeeId);
}

public class PayrollRunSummary
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int WorkingDays { get; set; }
    public List<string> Generated { get; set; } = new();
    public List<string> Skipped { get; set; } = new();
}

public class MonthSummary
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int PayslipCount { get; set; }
    public decimal TotalNetPay { get; set; }
}