namespace AppControlGastos.Models;

public class PartnerPayment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Date { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string Concept { get; set; } = "Transferencia de regularización a pareja";
    public string FromWhom { get; set; } = "Yo"; // "Yo" transfirió a pareja, o "Pareja" transfirió a mí
}
