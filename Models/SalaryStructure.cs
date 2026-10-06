namespace HrPayrollSystem.Models;

public class SalaryStructure
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }                
    public Employee? Employee { get; set; }

    public decimal Basic { get; set; }                 // ~40-50% of gross
    public decimal HRA { get; set; }                   // housing allowance
    public decimal Conveyance { get; set; }            // travel allowance
    public decimal SpecialAllowance { get; set; }      // the balancing remainder

    // Gross = Basic + HRA + Conveyance + SpecialAllowance
    // (payroll calculator will verify this matches Employee.GrossMonthlySalary)
}