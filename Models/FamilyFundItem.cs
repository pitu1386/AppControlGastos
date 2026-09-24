namespace AppControlGastos.Models;

public class FamilyFundItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Person { get; set; } = "Mamá";
    public DirectionType Direction { get; set; } = DirectionType.RecibidoParaAdministrar;
    
    // Divisa original
    public CurrencyType OriginalCurrency { get; set; } = CurrencyType.USD;
    public decimal OriginalAmount { get; set; } = 500m;
    
    // Tipo de cambio (1 Unidad de OriginalCurrency = X EUR)
    // Ejemplo: 1 USD = 0.92 EUR -> ExchangeRate = 0.92m
    // Si la moneda es EUR -> ExchangeRate = 1.0m
    public decimal ExchangeRate { get; set; } = 0.92m;
    public DateTime ExchangeRateDate { get; set; } = DateTime.Today;

    public string Purpose { get; set; } = "Compra personal / encargo"; // Para qué es
    public decimal SpentEur { get; set; } = 0m; // Cuánto se ha gastado en EUR

    // Si tenés que devolver dinero o si simplemente estás administrando ese importe
    public bool MustReturnSurplus { get; set; } = true;
    public bool IsSettled { get; set; } = false;
    public string Notes { get; set; } = string.Empty;

    // Equivalente inicial en EUR
    public decimal EurEquivalent => Math.Round(OriginalAmount * ExchangeRate, 2);

    // Saldo restante en EUR
    public decimal RemainingEur => Math.Max(0m, EurEquivalent - SpentEur);

    // Si es dinero recibido para administrar, representa "Dinero de terceros" que NO es propio.
    // Si es adelantado por mí, la familia me lo tiene que dar / devolver a mí.
    public decimal BalanceImpactThirdParty => Direction switch
    {
        DirectionType.RecibidoParaAdministrar => !IsSettled ? RemainingEur : 0m,
        _ => 0m
    };

    public decimal BalanceOwedToMe => Direction switch
    {
        DirectionType.AdelantadoPorMi => !IsSettled ? RemainingEur : 0m,
        _ => 0m
    };
}
