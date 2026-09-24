namespace AppControlGastos.Models;

public class Invoice
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Number { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientCountry { get; set; } = "España"; // España, Bélgica, etc.
    public InvoiceType Type { get; set; } = InvoiceType.Nacional;
    
    public DateTime IssueDate { get; set; } = DateTime.Today;
    public DateTime ExpectedPaymentDate { get; set; } = DateTime.Today.AddDays(30);
    
    public decimal BaseAmount { get; set; }
    
    // IRPF Retención en factura (habitual 15% o 7% para clientes en España)
    public decimal IrpfRate { get; set; } = 15;
    public bool ApplyIrpfWithholding { get; set; } = true;
    
    // Si es factura exterior (ej. Bélgica), no lleva retención en factura pero el autónomo DEBE reservar IRPF (ej. 20% para el Mod 130)
    public bool MustReserveIrpfExternally { get; set; } = false;
    public decimal ExternalIrpfReserveRate { get; set; } = 20;

    // IVA (21% nacional, 0% intracomunitaria con inversión del sujeto pasivo)
    public decimal VatRate { get; set; } = 21;
    public bool ApplyVat { get; set; } = true;

    // Estado del cobro
    public bool IsCollected { get; set; } = false;
    public DateTime? CollectedDate { get; set; }
    public decimal CollectedAmount { get; set; }

    public string Notes { get; set; } = string.Empty;

    // Propiedades calculadas
    public decimal VatAmount => (ApplyVat && VatRate > 0) ? Math.Round(BaseAmount * (VatRate / 100m), 2) : 0m;
    
    public decimal IrpfWithheldAmount => (ApplyIrpfWithholding && IrpfRate > 0) ? Math.Round(BaseAmount * (IrpfRate / 100m), 2) : 0m;
    
    public decimal IrpfReserveAmount => (MustReserveIrpfExternally && ExternalIrpfReserveRate > 0) 
        ? Math.Round(BaseAmount * (ExternalIrpfReserveRate / 100m), 2) 
        : 0m;

    // Total a cobrar al cliente en factura = Base + IVA - Retención
    public decimal TotalInvoice => Math.Round(BaseAmount + VatAmount - IrpfWithheldAmount, 2);

    // Saldo pendiente de cobro
    public decimal PendingToCollect => IsCollected ? 0m : Math.Max(0m, TotalInvoice - CollectedAmount);

    // Retraso / días
    public int DaysUntilPayment => (ExpectedPaymentDate.Date - DateTime.Today.Date).Days;
    public bool IsOverdue => !IsCollected && DateTime.Today.Date > ExpectedPaymentDate.Date;
}
