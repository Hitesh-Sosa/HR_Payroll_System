namespace HrPayrollSystem.Services;

public interface IEmailService
{
    Task SendPayslipEmailAsync(string toEmail, string employeeName, int year, int month, decimal netPay);
}