namespace AppControlGastos.Models;

public class WorkLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Date { get; set; } = DateTime.Today;
    public decimal Hours { get; set; } = 4m;
    public HourLogType Type { get; set; } = HourLogType.Facturable;
    public string Description { get; set; } = string.Empty;
}

public class Project
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Calle X — Interiorismo";
    public string Client { get; set; } = "Cliente ABC";
    public decimal HourlyRate { get; set; } = 35m;
    public decimal EstimatedHours { get; set; } = 100m;
    public decimal EstimatedBudget => Math.Round(EstimatedHours * HourlyRate, 2);
    public ProjectStatusType Status { get; set; } = ProjectStatusType.EnCurso;
    public DateTime? ExpectedCompletionDate { get; set; }

    public List<WorkLog> WorkLogs { get; set; } = new();

    // Horas registradas
    public decimal TotalHoursWorked => WorkLogs.Sum(w => w.Hours);
    public decimal BillableHoursWorked => WorkLogs.Where(w => w.Type == HourLogType.Facturable).Sum(w => w.Hours);
    public decimal NonBillableHoursWorked => WorkLogs.Where(w => w.Type != HourLogType.Facturable).Sum(w => w.Hours);

    // Valor generado según horas facturables
    public decimal ValueGenerated => Math.Round(BillableHoursWorked * HourlyRate, 2);

    // Horas restantes estimadas
    public decimal RemainingHours => Math.Max(0m, EstimatedHours - BillableHoursWorked);

    // Ingresos potenciales pendientes por facturar
    public decimal PotentialPendingIncome => Math.Round(RemainingHours * HourlyRate, 2);

    // Porcentaje de avance
    public decimal ProgressPercentage => EstimatedHours > 0 
        ? Math.Min(100m, Math.Round((BillableHoursWorked / EstimatedHours) * 100m, 1)) 
        : 0m;
}
