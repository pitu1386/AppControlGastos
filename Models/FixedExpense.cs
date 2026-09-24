namespace AppControlGastos.Models;

public class FixedExpense
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Concept { get; set; } = string.Empty;
    public string Category { get; set; } = "Gastos Fijos";
    public decimal Amount { get; set; }
    public FrequencyType Frequency { get; set; } = FrequencyType.Mensual;
    public int DayOfMonth { get; set; } = 1;
    public ExpenseScope Scope { get; set; } = ExpenseScope.Profesional;
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = string.Empty;

    // Prorrateo para saber cuánto se debe reservar mensualmente
    public decimal MonthlyReserveAmount => Frequency switch
    {
        FrequencyType.Mensual => Amount,
        FrequencyType.Bimestral => Math.Round(Amount / 2m, 2),
        FrequencyType.Trimestral => Math.Round(Amount / 3m, 2),
        FrequencyType.Anual => Math.Round(Amount / 12m, 2),
        _ => Amount
    };

    public string FrequencyLabel => Frequency switch
    {
        FrequencyType.Mensual => "Mensual",
        FrequencyType.Bimestral => "Bimestral",
        FrequencyType.Trimestral => "Trimestral",
        FrequencyType.Anual => "Anual",
        _ => Frequency.ToString()
    };
}
