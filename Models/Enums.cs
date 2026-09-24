namespace AppControlGastos.Models;

public enum ExpenseScope
{
    Profesional,
    Personal,
    Compartido
}

public enum InvoiceType
{
    Nacional,
    Intracomunitaria,
    Exterior
}

public enum FrequencyType
{
    Mensual,
    Bimestral,
    Trimestral,
    Anual
}

public enum PaymentMethodType
{
    Transferencia,
    Tarjeta,
    Domiciliacion,
    Efectivo,
    Bizum,
    Otro
}

public enum CurrencyType
{
    EUR,
    USD,
    ARS
}

public enum DirectionType
{
    RecibidoParaAdministrar,
    AdelantadoPorMi
}

public enum ProjectStatusType
{
    EnCurso,
    Facturado,
    Completado,
    Pausado
}

public enum HourLogType
{
    Facturable,
    GestionYReuniones,
    NoFacturable
}

public enum FundMovementType
{
    RetiroPorTensionLiquidez,
    Reposicion,
    AporteExtraordinario
}
