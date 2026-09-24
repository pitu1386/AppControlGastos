namespace AppControlGastos.Models;

public class AppData
{
    public decimal BankAccountBalance { get; set; } = 8500m;
    
    public List<Invoice> Invoices { get; set; } = new();
    public List<Expense> Expenses { get; set; } = new();
    public List<FixedExpense> FixedExpenses { get; set; } = new();
    public List<InstallmentPurchase> Installments { get; set; } = new();
    public List<BudgetCategory> BudgetCategories { get; set; } = new();
    
    public LiquidityFund LiquidityFund { get; set; } = new();
    public FamilyLoan FamilyLoan { get; set; } = new();
    public List<FamilyFundItem> FamilyFundItems { get; set; } = new();
    public List<PartnerPayment> PartnerPayments { get; set; } = new();
    public SavingsGoalConfig SavingsConfig { get; set; } = new();
    public List<Project> Projects { get; set; } = new();

    // ==========================================
    // CÁLCULOS CLAVE DEL SISTEMA
    // ==========================================

    // 1. FACTURAS PENDIENTES DE COBRO
    public decimal TotalPendingInvoices => Invoices.Where(i => !i.IsCollected).Sum(i => i.PendingToCollect);

    // 2. GASTOS PRÓXIMOS / PENDIENTES
    // Próximos gastos fijos del mes activo + cuotas pendientes inmediatas
    public decimal ImmediateFixedExpenses => FixedExpenses.Where(f => f.IsActive).Sum(f => f.MonthlyReserveAmount);
    public decimal NextInstallmentsDueThisMonth => Installments.Where(i => !i.IsCompleted).Sum(i => i.InstallmentAmount);
    public decimal TotalPendingUpcomingPayments => ImmediateFixedExpenses + NextInstallmentsDueThisMonth;

    // 3. RESERVAS FISCALES Y FISCALIDAD
    // Reservas de IRPF (de facturas nacionales y exteriores pendientes o del mes) + IVA a ingresar
    public decimal TaxReservesRequired => Invoices
        .Where(i => !i.IsCollected || (i.IssueDate.Month == DateTime.Today.Month && i.IssueDate.Year == DateTime.Today.Year))
        .Sum(i => i.IrpfReserveAmount + i.VatAmount);

    // 4. DEUDA PENDIENTE CON PAREJA
    // Suma de la parte del usuario en gastos compartidos pagados por la pareja
    // MENOS la parte de la pareja en gastos compartidos pagados por el usuario
    // MENOS las transferencias de regularización que el usuario le ha hecho a la pareja
    public decimal CalculatePartnerDebt()
    {
        decimal userOwes = 0m;
        foreach (var exp in Expenses.Where(e => e.Scope == ExpenseScope.Compartido))
        {
            if (exp.PaidBy == "Pareja")
            {
                userOwes += exp.UserShareAmount; // Pareja pagó, yo debo mi parte
            }
            else if (exp.PaidBy == "Yo")
            {
                userOwes -= exp.PartnerShareAmount; // Yo pagué, pareja me debe su parte
            }
        }

        // Restar transferencias efectuadas por el usuario a la pareja
        decimal transfersFromUser = PartnerPayments.Where(p => p.FromWhom == "Yo").Sum(p => p.Amount);
        decimal transfersFromPartner = PartnerPayments.Where(p => p.FromWhom == "Pareja").Sum(p => p.Amount);

        return Math.Round(userOwes - transfersFromUser + transfersFromPartner, 2);
    }

    // 5. DINERO DE TERCEROS (FAMILIA / ENCARGOS)
    public decimal ThirdPartyMoney => FamilyFundItems
        .Where(f => f.Direction == DirectionType.RecibidoParaAdministrar && !f.IsSettled)
        .Sum(f => f.RemainingEur);

    // 6. DINERO QUE FAMILIA LE DEBE AL USUARIO
    public decimal MoneyFamilyOwesMe => FamilyFundItems
        .Where(f => f.Direction == DirectionType.AdelantadoPorMi && !f.IsSettled)
        .Sum(f => f.RemainingEur);

    // 7. DEUDA CON EL FONDO DE LIQUIDEZ (Retiros por tensión pendientes de reponer)
    public decimal FundDebtPending => LiquidityFund.RealUsedPending;

    // 8. DEUDA FAMILIAR CON PADRES (Préstamo)
    public decimal FamilyLoanDebt => FamilyLoan.PendingTotal;

    // 9. DINERO COMPROMETIDO EN CUOTAS FUTURAS TOTALES
    public decimal TotalCommittedFutureInstallments => Installments.Where(i => !i.IsCompleted).Sum(i => i.CommittedFutureAmount);

    // 10. DINERO REALMENTE DISPONIBLE (LA MÉTRICA DE ORO)
    // Saldo bancario 
    // - Dinero de Terceros (no es mío)
    // - Reservas fiscales (IRPF / IVA)
    // - Gastos fijos próximos de este ciclo
    // - Cuotas pendientes de este mes
    // - Reposición pendiente al fondo de liquidez
    // - Deuda pendiente con pareja (si es positiva, es decir, debo dinero)
    public decimal RealAvailableCash
    {
        get
        {
            decimal partnerDebt = Math.Max(0m, CalculatePartnerDebt());
            decimal totalDeductions = ThirdPartyMoney 
                                    + TaxReservesRequired 
                                    + ImmediateFixedExpenses 
                                    + NextInstallmentsDueThisMonth 
                                    + FundDebtPending 
                                    + partnerDebt;

            return Math.Round(BankAccountBalance - totalDeductions, 2);
        }
    }

    // 11. INGRESOS DEL MES ACTUAL
    public decimal CurrentMonthInvoiced => Invoices
        .Where(i => i.IssueDate.Month == DateTime.Today.Month && i.IssueDate.Year == DateTime.Today.Year)
        .Sum(i => i.TotalInvoice);

    public decimal CurrentMonthCollected => Invoices
        .Where(i => i.IsCollected && i.CollectedDate.HasValue && i.CollectedDate.Value.Month == DateTime.Today.Month && i.CollectedDate.Value.Year == DateTime.Today.Year)
        .Sum(i => i.CollectedAmount > 0 ? i.CollectedAmount : i.TotalInvoice);

    // 12. GASTOS DEL MES ACTUAL
    public decimal CurrentMonthProfessionalExpenses => Expenses
        .Where(e => e.Scope == ExpenseScope.Profesional && e.Date.Month == DateTime.Today.Month && e.Date.Year == DateTime.Today.Year)
        .Sum(e => e.Amount);

    public decimal CurrentMonthPersonalExpenses => Expenses
        .Where(e => e.Scope == ExpenseScope.Personal && e.Date.Month == DateTime.Today.Month && e.Date.Year == DateTime.Today.Year)
        .Sum(e => e.Amount);

    public decimal CurrentMonthSharedExpensesUserPortion => Expenses
        .Where(e => e.Scope == ExpenseScope.Compartido && e.Date.Month == DateTime.Today.Month && e.Date.Year == DateTime.Today.Year)
        .Sum(e => e.UserShareAmount);

    public decimal CurrentMonthTotalExpenses => CurrentMonthProfessionalExpenses + CurrentMonthPersonalExpenses + CurrentMonthSharedExpensesUserPortion;

    // 13. BENEFICIO DEL MES (Ingresos cobrados - gastos profesionales - impuestos/reservas)
    public decimal CurrentMonthProfit => Math.Round(CurrentMonthCollected - CurrentMonthProfessionalExpenses - TaxReservesRequired, 2);

    // 14. POTENCIAL DE INGRESOS EN PROYECTOS ACTIVOS
    public decimal ProjectsPotentialIncome => Projects.Where(p => p.Status == ProjectStatusType.EnCurso).Sum(p => p.PotentialPendingIncome);
}
