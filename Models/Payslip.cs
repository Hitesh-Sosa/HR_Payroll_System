namespace HrPayrollSystem.Models;

public class Payslip
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }

    public int WorkingDays { get; set; }
    public decimal LopDays { get; set; }          // loss of pay days
    public decimal PaidDays { get; set; }

    // Earnings (earned = component × paidDays/workingDays)
    public decimal EarnedBasic { get; set; }
    public decimal EarnedHra { get; set; }
    public decimal EarnedConveyance { get; set; }
    public decimal EarnedSpecial { get; set; }
    public decimal EarnedGross { get; set; }

    // Deductions
    public decimal PfDeduction { get; set; }
    public decimal ProfessionalTax { get; set; }
    public decimal TotalDeductions { get; set; }

    public decimal NetPay { get; set; }

    public DateTime GeneratedOn { get; set; } = DateTime.UtcNow;

    public ICollection<PayslipItem> Items { get; set; } = new List<PayslipItem>();
}

public class PayslipItem
{
    public int Id { get; set; }

    public int PayslipId { get; set; }
    public Payslip? Payslip { get; set; }

    public string Name { get; set; } = string.Empty;      // "Basic", "HRA", "PF"...
    public string Type { get; set; } = string.Empty;      // "Earning" / "Deduction"
    public decimal Amount { get; set; }
}