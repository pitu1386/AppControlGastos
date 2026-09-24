namespace AppControlGastos.Models;

public class BudgetCategory
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Category { get; set; } = string.Empty;
    public decimal MonthlyLimit { get; set; }
    public string Icon { get; set; } = "tag";
}
