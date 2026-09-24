namespace AppControlGastos.Models;

public class FamilyLoanRepayment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Date { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string Note { get; set; } = "Devolución / Reposición préstamo familiar";
}

public class FamilyLoan
{
    public decimal InitialAmount { get; set; } = 8000m;
    public List<FamilyLoanRepayment> Repayments { get; set; } = new();

    public decimal RepaidTotal => Repayments.Sum(r => r.Amount);
    public decimal PendingTotal => Math.Max(0m, InitialAmount - RepaidTotal);

    public decimal ProgressPercentage => InitialAmount > 0 
        ? Math.Min(100m, Math.Round((RepaidTotal / InitialAmount) * 100m, 1)) 
        : 0m;
}
