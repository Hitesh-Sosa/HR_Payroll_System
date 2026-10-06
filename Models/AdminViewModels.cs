using System.ComponentModel.DataAnnotations;

namespace HrPayrollSystem.Models;

public class CreateEmployeeViewModel
{
    [Required, StringLength(10)]
    public string EmployeeCode { get; set; } = string.Empty;     // EMP002

    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Designation { get; set; } = string.Empty;

    [Required, DataType(DataType.Date)]
    public DateTime? JoiningDate { get; set; }

    [Required, Range(0, 99999999)]
    public decimal? GrossMonthlySalary { get; set; }

    [Required(ErrorMessage = "Please choose a department.")]
    public int? DepartmentId { get; set; }

    // Radio choice: link existing login OR create a new one
    public bool LinkExistingUser { get; set; } = true;

    [RequiredIfLinkExisting]
    public string? SelectedUserId { get; set; }

    [EmailAddress]
    public string? NewEmail { get; set; }

    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }
}

// Custom validation: SelectedUserId required only when linking an existing user
public class RequiredIfLinkExistingAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        var instance = (CreateEmployeeViewModel)context.ObjectInstance;
        if (instance.LinkExistingUser && string.IsNullOrWhiteSpace(instance.SelectedUserId))
            return new ValidationResult("Please select a user account to link.");

        return ValidationResult.Success;
    }
}

public class SalaryFormViewModel
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;

    [Required, Range(0, 99999999)]
    public decimal? Basic { get; set; }

    [Required, Range(0, 99999999)]
    public decimal? HRA { get; set; }

    [Required, Range(0, 99999999)]
    public decimal? Conveyance { get; set; }

    [Required, Range(0, 99999999)]
    public decimal? SpecialAllowance { get; set; }
}