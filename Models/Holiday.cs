namespace HrPayrollSystem.Models;

public class Holiday
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;   // "Independence Day", "Diwali", "Christmas", "New Year's Day", ...
}