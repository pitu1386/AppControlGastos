namespace AppControlGastos.Models;

public class SavingsJar
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Ahorro General"; // Vivienda, Viajes, Impuestos, Emergencia, Proyectos
    public decimal TargetAmount { get; set; } = 3000m;
    public decimal CurrentAmount { get; set; } = 1200m;
    public string Icon { get; set; } = "piggy-bank";
    public string Color { get; set; } = "emerald";

    public decimal ProgressPercentage => TargetAmount > 0 
        ? Math.Min(100m, Math.Round((CurrentAmount / TargetAmount) * 100m, 1)) 
        : 0m;
}

public class SavingsGoalConfig
{
    public decimal MonthlySavingsTarget { get; set; } = 500m;
    public List<SavingsJar> Jars { get; set; } = new();
}
