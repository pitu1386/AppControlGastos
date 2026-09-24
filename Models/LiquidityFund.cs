namespace AppControlGastos.Models;

public class FundMovement
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Date { get; set; } = DateTime.Today;
    public FundMovementType Type { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty; // e.g. "Tensión cobro factura Bélgica", "Reposición tras cobro cliente X"
}

public class LiquidityFund
{
    public decimal TargetAmount { get; set; } = 10000m;
    public decimal CurrentBalance { get; set; } = 4200m;
    
    // Lista de movimientos (retiros por tensión vs reposiciones)
    public List<FundMovement> Movements { get; set; } = new();

    // Dinero retirado temporalmente pendiente de reponer
    public decimal UsedPendingReplacement => Movements
        .Where(m => m.Type == FundMovementType.RetiroPorTensionLiquidez)
        .Sum(m => m.Amount) 
        - Movements.Where(m => m.Type == FundMovementType.Reposicion).Sum(m => m.Amount);

    public decimal RealUsedPending => Math.Max(0m, UsedPendingReplacement);

    public decimal EffectiveAvailableInFund => Math.Max(0m, CurrentBalance - RealUsedPending);

    public decimal CompletionPercentage => TargetAmount > 0 
        ? Math.Min(100m, Math.Round((CurrentBalance / TargetAmount) * 100m, 1)) 
        : 0m;
}
