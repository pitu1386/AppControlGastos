namespace AppControlGastos.Models;

public class InstallmentPurchase
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Concept { get; set; } = string.Empty; // ej. "Viaje Argentina", "MacBook Pro"
    public string Category { get; set; } = "Compras";
    public decimal TotalAmount { get; set; }
    public int TotalInstallments { get; set; } = 1;
    public int PaidInstallments { get; set; } = 0;
    
    // Importe por cuota (si se calcula automáticamente o personalizado)
    public decimal CustomInstallmentAmount { get; set; } = 0;
    public decimal InstallmentAmount => CustomInstallmentAmount > 0 
        ? CustomInstallmentAmount 
        : (TotalInstallments > 0 ? Math.Round(TotalAmount / TotalInstallments, 2) : TotalAmount);

    public int RemainingInstallments => Math.Max(0, TotalInstallments - PaidInstallments);
    public decimal CommittedFutureAmount => Math.Round(RemainingInstallments * InstallmentAmount, 2);
    public decimal PaidAmount => Math.Round(PaidInstallments * InstallmentAmount, 2);

    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime NextDueDate { get; set; } = DateTime.Today;
    public ExpenseScope Scope { get; set; } = ExpenseScope.Personal;
    public string Notes { get; set; } = string.Empty;

    public bool IsCompleted => PaidInstallments >= TotalInstallments;

    // Próximas cuotas con fechas proyectadas
    public List<(int InstallmentNumber, DateTime DueDate, decimal Amount)> GetUpcomingInstallments()
    {
        var list = new List<(int, DateTime, decimal)>();
        for (int i = PaidInstallments + 1; i <= TotalInstallments; i++)
        {
            int offset = i - (PaidInstallments + 1);
            list.Add((i, NextDueDate.AddMonths(offset), InstallmentAmount));
        }
        return list;
    }
}
