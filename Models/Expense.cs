namespace AppControlGastos.Models;

public class Expense
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Date { get; set; } = DateTime.Today;
    public string Concept { get; set; } = string.Empty;
    public string Category { get; set; } = "Otros";
    public decimal Amount { get; set; }
    public PaymentMethodType PaymentMethod { get; set; } = PaymentMethodType.Tarjeta;
    
    // Clasificación clave: Profesional, Personal, Compartido
    public ExpenseScope Scope { get; set; } = ExpenseScope.Profesional;

    // En caso de ser compartido
    public decimal SharedPercentageUser { get; set; } = 50m; // % que paga el usuario
    public string PaidBy { get; set; } = "Yo"; // "Yo" o "Pareja"

    // Relación opcional con compras en cuotas
    public string? InstallmentPurchaseId { get; set; }

    public string Notes { get; set; } = string.Empty;

    // Cálculos para gastos compartidos
    public decimal UserShareAmount => Scope switch
    {
        ExpenseScope.Compartido => Math.Round(Amount * (SharedPercentageUser / 100m), 2),
        _ => Amount
    };

    public decimal PartnerShareAmount => Scope switch
    {
        ExpenseScope.Compartido => Math.Round(Amount - UserShareAmount, 2),
        _ => 0m
    };

    // Cuánto debe el usuario a la pareja por este gasto (+ si pagó la pareja, - si pagó el usuario y la pareja le debe)
    public decimal BalanceImpactWithPartner => Scope switch
    {
        ExpenseScope.Compartido when PaidBy == "Pareja" => UserShareAmount, // Yo le debo a mi pareja mi parte
        ExpenseScope.Compartido when PaidBy == "Yo" => -PartnerShareAmount, // Mi pareja me debe su parte a mí
        _ => 0m
    };
}
