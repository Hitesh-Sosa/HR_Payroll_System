namespace HrPayrollSystem.Services;

public class PayrollInput
{
    public decimal GrossMonthlySalary { get; set; }
    public decimal Basic { get; set; }
    public decimal Hra { get; set; }
    public decimal Conveyance { get; set; }
    public decimal SpecialAllowance { get; set; }
    public int WorkingDays { get; set; }
    public decimal LopDays { get; set; }
}

public class PayrollResult
{
    public decimal PaidDays { get; set; }
    public decimal EarnedBasic { get; set; }
    public decimal EarnedHra { get; set; }
    public decimal EarnedConveyance { get; set; }
    public decimal EarnedSpecial { get; set; }
    public decimal EarnedGross { get; set; }
    public decimal PfDeduction { get; set; }
    public decimal ProfessionalTax { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
}

public static class PayrollCalculator
{
    public const decimal PfRate = 0.12m;               // PF = 12% of EARNED Basic
    public const decimal ProfessionalTaxMonthly = 200m; // flat slab for simplicity

    public static PayrollResult Calculate(PayrollInput input)
    {
        if (input.WorkingDays <= 0)
            throw new InvalidOperationException("Working days must be greater than zero.");

        if (input.LopDays < 0 || input.LopDays > input.WorkingDays)
            throw new InvalidOperationException("LOP days must be between 0 and the working days count.");

        // Pro-rata factor: paid fraction of the month
        var paidDays = input.WorkingDays - input.LopDays;
        var factor = paidDays / input.WorkingDays;

        var earnedBasic = R2(input.Basic * factor);
        var earnedHra = R2(input.Hra * factor);
        var earnedConveyance = R2(input.Conveyance * factor);
        var earnedSpecial = R2(input.SpecialAllowance * factor);
        var earnedGross = R2(earnedBasic + earnedHra + earnedConveyance + earnedSpecial);

        var pf = R2(earnedBasic * PfRate);
        var profTax = ProfessionalTaxMonthly;
        var totalDeductions = R2(pf + profTax);
        var netPay = R2(earnedGross - totalDeductions);

        return new PayrollResult
        {
            PaidDays = paidDays,
            EarnedBasic = earnedBasic,
            EarnedHra = earnedHra,
            EarnedConveyance = earnedConveyance,
            EarnedSpecial = earnedSpecial,
            EarnedGross = earnedGross,
            PfDeduction = pf,
            ProfessionalTax = profTax,
            TotalDeductions = totalDeductions,
            NetPay = netPay
        };
    }

    // Money rounds to 2 decimals, halves away from zero (banker-safe convention)
    private static decimal R2(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}