using System.Text.Json;
using Microsoft.JSInterop;
using AppControlGastos.Models;

namespace AppControlGastos.Services;

public class AppStateService
{
    private const string StorageKey = "app_control_gastos_v1";
    private readonly IJSRuntime _js;

    public AppData Data { get; private set; } = new();
    public bool IsInitialized { get; private set; } = false;

    public event Action? OnChange;

    public AppStateService(IJSRuntime js)
    {
        _js = js;
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    public async Task InitializeAsync()
    {
        if (IsInitialized) return;

        try
        {
            var json = await _js.InvokeAsync<string?>("financialApp.loadData", StorageKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var loaded = JsonSerializer.Deserialize<AppData>(json, options);
                if (loaded != null)
                {
                    Data = loaded;
                }
                else
                {
                    LoadDemoData(saveImmediately: false);
                }
            }
            else
            {
                LoadDemoData(saveImmediately: false);
                await SaveAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al inicializar estado: {ex.Message}");
            LoadDemoData(saveImmediately: false);
        }

        IsInitialized = true;
        NotifyStateChanged();
    }

    public async Task SaveAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(Data, new JsonSerializerOptions { WriteIndented = true });
            await _js.InvokeVoidAsync("financialApp.saveData", StorageKey, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al guardar estado: {ex.Message}");
        }
        NotifyStateChanged();
    }

    // ----------------------------------------------------
    // BANCO
    // ----------------------------------------------------
    public async Task SetBankAccountBalanceAsync(decimal balance)
    {
        Data.BankAccountBalance = balance;
        await SaveAsync();
    }

    // ----------------------------------------------------
    // FACTURAS
    // ----------------------------------------------------
    public async Task AddInvoiceAsync(Invoice invoice)
    {
        Data.Invoices.Insert(0, invoice);
        await SaveAsync();
    }

    public async Task UpdateInvoiceAsync(Invoice invoice)
    {
        var index = Data.Invoices.FindIndex(i => i.Id == invoice.Id);
        if (index >= 0)
        {
            Data.Invoices[index] = invoice;
            await SaveAsync();
        }
    }

    public async Task DeleteInvoiceAsync(string id)
    {
        Data.Invoices.RemoveAll(i => i.Id == id);
        await SaveAsync();
    }

    public async Task MarkInvoiceCollectedAsync(string id, DateTime? date = null, decimal? amount = null)
    {
        var inv = Data.Invoices.FirstOrDefault(i => i.Id == id);
        if (inv != null)
        {
            inv.IsCollected = true;
            inv.CollectedDate = date ?? DateTime.Today;
            inv.CollectedAmount = amount ?? inv.TotalInvoice;
            // Al cobrar se actualiza el saldo bancario si corresponde
            Data.BankAccountBalance += inv.CollectedAmount;
            await SaveAsync();
        }
    }

    // ----------------------------------------------------
    // GASTOS
    // ----------------------------------------------------
    public async Task AddExpenseAsync(Expense expense)
    {
        Data.Expenses.Insert(0, expense);
        // Si lo pagó el usuario, se descuenta de la cuenta bancaria
        if (expense.PaidBy == "Yo")
        {
            Data.BankAccountBalance -= expense.Amount;
        }
        await SaveAsync();
    }

    public async Task UpdateExpenseAsync(Expense expense)
    {
        var index = Data.Expenses.FindIndex(e => e.Id == expense.Id);
        if (index >= 0)
        {
            Data.Expenses[index] = expense;
            await SaveAsync();
        }
    }

    public async Task DeleteExpenseAsync(string id)
    {
        Data.Expenses.RemoveAll(e => e.Id == id);
        await SaveAsync();
    }

    // ----------------------------------------------------
    // GASTOS FIJOS
    // ----------------------------------------------------
    public async Task AddFixedExpenseAsync(FixedExpense fixedExpense)
    {
        Data.FixedExpenses.Add(fixedExpense);
        await SaveAsync();
    }

    public async Task UpdateFixedExpenseAsync(FixedExpense fixedExpense)
    {
        var index = Data.FixedExpenses.FindIndex(f => f.Id == fixedExpense.Id);
        if (index >= 0)
        {
            Data.FixedExpenses[index] = fixedExpense;
            await SaveAsync();
        }
    }

    public async Task DeleteFixedExpenseAsync(string id)
    {
        Data.FixedExpenses.RemoveAll(f => f.Id == id);
        await SaveAsync();
    }

    // ----------------------------------------------------
    // CUOTAS
    // ----------------------------------------------------
    public async Task AddInstallmentAsync(InstallmentPurchase item)
    {
        Data.Installments.Add(item);
        await SaveAsync();
    }

    public async Task UpdateInstallmentAsync(InstallmentPurchase item)
    {
        var index = Data.Installments.FindIndex(i => i.Id == item.Id);
        if (index >= 0)
        {
            Data.Installments[index] = item;
            await SaveAsync();
        }
    }

    public async Task DeleteInstallmentAsync(string id)
    {
        Data.Installments.RemoveAll(i => i.Id == id);
        await SaveAsync();
    }

    public async Task PayNextInstallmentAsync(string id)
    {
        var item = Data.Installments.FirstOrDefault(i => i.Id == id);
        if (item != null && item.PaidInstallments < item.TotalInstallments)
        {
            item.PaidInstallments++;
            item.NextDueDate = item.NextDueDate.AddMonths(1);
            
            // Generar gasto en el histórico de gastos
            var expense = new Expense
            {
                Concept = $"Cuota {item.PaidInstallments}/{item.TotalInstallments} - {item.Concept}",
                Category = item.Category,
                Amount = item.InstallmentAmount,
                Date = DateTime.Today,
                Scope = item.Scope,
                InstallmentPurchaseId = item.Id,
                PaidBy = "Yo"
            };
            Data.Expenses.Insert(0, expense);
            Data.BankAccountBalance -= item.InstallmentAmount;

            await SaveAsync();
        }
    }

    // ----------------------------------------------------
    // PRESUPUESTOS
    // ----------------------------------------------------
    public async Task AddOrUpdateBudgetAsync(string category, decimal limit)
    {
        var existing = Data.BudgetCategories.FirstOrDefault(b => b.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            existing.MonthlyLimit = limit;
        }
        else
        {
            Data.BudgetCategories.Add(new BudgetCategory { Category = category, MonthlyLimit = limit });
        }
        await SaveAsync();
    }

    public async Task DeleteBudgetAsync(string id)
    {
        Data.BudgetCategories.RemoveAll(b => b.Id == id);
        await SaveAsync();
    }

    // ----------------------------------------------------
    // FONDO DE LIQUIDEZ
    // ----------------------------------------------------
    public async Task UpdateLiquidityFundTargetAsync(decimal target)
    {
        Data.LiquidityFund.TargetAmount = target;
        await SaveAsync();
    }

    public async Task AddFundMovementAsync(FundMovementType type, decimal amount, string reason)
    {
        Data.LiquidityFund.Movements.Insert(0, new FundMovement
        {
            Date = DateTime.Today,
            Type = type,
            Amount = amount,
            Reason = reason
        });

        if (type == FundMovementType.RetiroPorTensionLiquidez)
        {
            // Sube la liquidez bancaria inmediata porque se trajo dinero del fondo
            Data.BankAccountBalance += amount;
            Data.LiquidityFund.CurrentBalance = Math.Max(0m, Data.LiquidityFund.CurrentBalance - amount);
        }
        else if (type == FundMovementType.Reposicion)
        {
            // Se devuelve dinero al fondo desde la cuenta bancaria
            Data.BankAccountBalance -= amount;
            Data.LiquidityFund.CurrentBalance += amount;
        }
        else if (type == FundMovementType.AporteExtraordinario)
        {
            Data.LiquidityFund.CurrentBalance += amount;
        }

        await SaveAsync();
    }

    // ----------------------------------------------------
    // PRÉSTAMO FAMILIAR (PADRES)
    // ----------------------------------------------------
    public async Task UpdateFamilyLoanInitialAsync(decimal amount)
    {
        Data.FamilyLoan.InitialAmount = amount;
        await SaveAsync();
    }

    public async Task AddFamilyLoanRepaymentAsync(decimal amount, string note)
    {
        Data.FamilyLoan.Repayments.Insert(0, new FamilyLoanRepayment
        {
            Date = DateTime.Today,
            Amount = amount,
            Note = note
        });
        Data.BankAccountBalance -= amount;
        await SaveAsync();
    }

    // ----------------------------------------------------
    // FONDOS DE FAMILIA / MULTIDIVISA
    // ----------------------------------------------------
    public async Task AddFamilyFundItemAsync(FamilyFundItem item)
    {
        Data.FamilyFundItems.Insert(0, item);
        if (item.Direction == DirectionType.RecibidoParaAdministrar)
        {
            Data.BankAccountBalance += item.EurEquivalent;
        }
        await SaveAsync();
    }

    public async Task UpdateFamilyFundItemAsync(FamilyFundItem item)
    {
        var index = Data.FamilyFundItems.FindIndex(f => f.Id == item.Id);
        if (index >= 0)
        {
            Data.FamilyFundItems[index] = item;
            await SaveAsync();
        }
    }

    public async Task RecordFamilyExpenseAsync(string id, decimal spentEur, string reason)
    {
        var item = Data.FamilyFundItems.FirstOrDefault(f => f.Id == id);
        if (item != null)
        {
            item.SpentEur += spentEur;
            if (item.SpentEur >= item.EurEquivalent)
            {
                item.IsSettled = true;
            }
            await SaveAsync();
        }
    }

    public async Task SettleFamilyItemAsync(string id)
    {
        var item = Data.FamilyFundItems.FirstOrDefault(f => f.Id == id);
        if (item != null)
        {
            item.IsSettled = true;
            await SaveAsync();
        }
    }

    public async Task DeleteFamilyFundItemAsync(string id)
    {
        Data.FamilyFundItems.RemoveAll(f => f.Id == id);
        await SaveAsync();
    }

    // ----------------------------------------------------
    // PAGOS A PAREJA (REGULARIZACIONES)
    // ----------------------------------------------------
    public async Task AddPartnerPaymentAsync(decimal amount, string concept, string fromWhom = "Yo")
    {
        Data.PartnerPayments.Insert(0, new PartnerPayment
        {
            Date = DateTime.Today,
            Amount = amount,
            Concept = concept,
            FromWhom = fromWhom
        });

        if (fromWhom == "Yo")
        {
            Data.BankAccountBalance -= amount;
        }
        else
        {
            Data.BankAccountBalance += amount;
        }

        await SaveAsync();
    }

    // ----------------------------------------------------
    // OBJETIVOS DE AHORRO
    // ----------------------------------------------------
    public async Task SetSavingsMonthlyTargetAsync(decimal target)
    {
        Data.SavingsConfig.MonthlySavingsTarget = target;
        await SaveAsync();
    }

    public async Task AddOrUpdateJarAsync(SavingsJar jar)
    {
        var index = Data.SavingsConfig.Jars.FindIndex(j => j.Id == jar.Id);
        if (index >= 0)
        {
            Data.SavingsConfig.Jars[index] = jar;
        }
        else
        {
            Data.SavingsConfig.Jars.Add(jar);
        }
        await SaveAsync();
    }

    public async Task DeleteJarAsync(string id)
    {
        Data.SavingsConfig.Jars.RemoveAll(j => j.Id == id);
        await SaveAsync();
    }

    // ----------------------------------------------------
    // PROYECTOS Y HORAS
    // ----------------------------------------------------
    public async Task AddProjectAsync(Project project)
    {
        Data.Projects.Insert(0, project);
        await SaveAsync();
    }

    public async Task UpdateProjectAsync(Project project)
    {
        var index = Data.Projects.FindIndex(p => p.Id == project.Id);
        if (index >= 0)
        {
            Data.Projects[index] = project;
            await SaveAsync();
        }
    }

    public async Task DeleteProjectAsync(string id)
    {
        Data.Projects.RemoveAll(p => p.Id == id);
        await SaveAsync();
    }

    public async Task AddWorkLogAsync(string projectId, decimal hours, HourLogType type, string description, DateTime? date = null)
    {
        var proj = Data.Projects.FirstOrDefault(p => p.Id == projectId);
        if (proj != null)
        {
            proj.WorkLogs.Insert(0, new WorkLog
            {
                Date = date ?? DateTime.Today,
                Hours = hours,
                Type = type,
                Description = description
            });
            await SaveAsync();
        }
    }

    // ----------------------------------------------------
    // BACKUP / EXPORT / IMPORT
    // ----------------------------------------------------
    public async Task DownloadBackupAsync()
    {
        var json = JsonSerializer.Serialize(Data, new JsonSerializerOptions { WriteIndented = true });
        string fileName = $"Backup_Finanzas_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        await _js.InvokeVoidAsync("financialApp.downloadJsonFile", fileName, json);
    }

    public async Task<bool> RestoreBackupAsync(string json)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var restored = JsonSerializer.Deserialize<AppData>(json, options);
            if (restored != null)
            {
                Data = restored;
                await SaveAsync();
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al restaurar backup: {ex.Message}");
        }
        return false;
    }

    public async Task ResetAllDataAsync()
    {
        Data = new AppData();
        await SaveAsync();
    }

    // ----------------------------------------------------
    // DATOS DE DEMOSTRACIÓN COMPLETOS Y REALISTAS
    // ----------------------------------------------------
    public void LoadDemoData(bool saveImmediately = true)
    {
        Data = new AppData
        {
            BankAccountBalance = 8500m
        };

        // 1. Facturas
        Data.Invoices = new List<Invoice>
        {
            new Invoice
            {
                Number = "FAC-2026-003",
                ClientName = "Studio Brussels BV",
                ClientCountry = "Bélgica",
                Type = InvoiceType.Intracomunitaria,
                IssueDate = DateTime.Today.AddDays(-20),
                ExpectedPaymentDate = DateTime.Today.AddDays(70), // cobro a 90 días
                BaseAmount = 4000m,
                ApplyVat = false,
                VatRate = 0,
                ApplyIrpfWithholding = false,
                MustReserveIrpfExternally = true,
                ExternalIrpfReserveRate = 20m, // Reserva 800€ para mod 130
                IsCollected = false,
                Notes = "Proyecto identidad corporativa Bruselas. Cobro previsto a 90 días."
            },
            new Invoice
            {
                Number = "FAC-2026-002",
                ClientName = "Diseño Interior Madrid SL",
                ClientCountry = "España",
                Type = InvoiceType.Nacional,
                IssueDate = DateTime.Today.AddDays(-10),
                ExpectedPaymentDate = DateTime.Today.AddDays(20), // 30 días
                BaseAmount = 2500m,
                ApplyVat = true,
                VatRate = 21m,
                ApplyIrpfWithholding = true,
                IrpfRate = 15m,
                IsCollected = false,
                Notes = "Planos y ejecución Calle Velázquez."
            },
            new Invoice
            {
                Number = "FAC-2026-001",
                ClientName = "Atelier Decoración BCN",
                ClientCountry = "España",
                Type = InvoiceType.Nacional,
                IssueDate = DateTime.Today.AddDays(-40),
                ExpectedPaymentDate = DateTime.Today.AddDays(-10),
                BaseAmount = 2850m,
                ApplyVat = true,
                VatRate = 21m,
                ApplyIrpfWithholding = true,
                IrpfRate = 15m,
                IsCollected = true,
                CollectedDate = DateTime.Today.AddDays(-5),
                CollectedAmount = 3021m,
                Notes = "Cobrada puntualmente."
            }
        };

        // 2. Gastos fijos
        Data.FixedExpenses = new List<FixedExpense>
        {
            new FixedExpense
            {
                Concept = "Cuota de Autónomos RETA",
                Category = "Autónomos",
                Amount = 320m,
                Frequency = FrequencyType.Mensual,
                DayOfMonth = 30,
                Scope = ExpenseScope.Profesional
            },
            new FixedExpense
            {
                Concept = "Gestoría & Asesoría Fiscal",
                Category = "Gestoría",
                Amount = 90m,
                Frequency = FrequencyType.Mensual,
                DayOfMonth = 5,
                Scope = ExpenseScope.Profesional
            },
            new FixedExpense
            {
                Concept = "Software Profesional (Adobe CC + Figma)",
                Category = "Software",
                Amount = 65m,
                Frequency = FrequencyType.Mensual,
                DayOfMonth = 15,
                Scope = ExpenseScope.Profesional
            },
            new FixedExpense
            {
                Concept = "Alquiler Piso Vivienda",
                Category = "Vivienda",
                Amount = 900m,
                Frequency = FrequencyType.Mensual,
                DayOfMonth = 1,
                Scope = ExpenseScope.Compartido
            },
            new FixedExpense
            {
                Concept = "Seguro Responsabilidad Civil",
                Category = "Otros",
                Amount = 360m,
                Frequency = FrequencyType.Anual,
                DayOfMonth = 15,
                Scope = ExpenseScope.Profesional
            }
        };

        // 3. Compras a plazos / cuotas
        Data.Installments = new List<InstallmentPurchase>
        {
            new InstallmentPurchase
            {
                Concept = "Viaje / Compra Argentina",
                Category = "Ocio",
                TotalAmount = 840m,
                TotalInstallments = 7,
                PaidInstallments = 3, // Cuota 3 de 7
                CustomInstallmentAmount = 120m,
                StartDate = DateTime.Today.AddMonths(-3),
                NextDueDate = DateTime.Today.AddDays(15),
                Scope = ExpenseScope.Personal,
                Notes = "Billetes y estancia en 7 pagos sin interés"
            }
        };

        // 4. Gastos recientes
        Data.Expenses = new List<Expense>
        {
            new Expense
            {
                Concept = "Compra Mercadona semanal",
                Category = "Alimentación",
                Amount = 140m,
                PaymentMethod = PaymentMethodType.Tarjeta,
                Scope = ExpenseScope.Compartido,
                SharedPercentageUser = 50m,
                PaidBy = "Yo",
                Date = DateTime.Today.AddDays(-2)
            },
            new Expense
            {
                Concept = "Cena restaurante viernes",
                Category = "Ocio",
                Amount = 60m,
                PaymentMethod = PaymentMethodType.Tarjeta,
                Scope = ExpenseScope.Compartido,
                SharedPercentageUser = 50m,
                PaidBy = "Pareja", // Pagó la pareja
                Date = DateTime.Today.AddDays(-4)
            },
            new Expense
            {
                Concept = "Abono Transportes Madrid",
                Category = "Transporte",
                Amount = 30m,
                PaymentMethod = PaymentMethodType.Tarjeta,
                Scope = ExpenseScope.Personal,
                Date = DateTime.Today.AddDays(-6)
            },
            new Expense
            {
                Concept = "Cuota Argentina 3/7",
                Category = "Ocio",
                Amount = 120m,
                PaymentMethod = PaymentMethodType.Domiciliacion,
                Scope = ExpenseScope.Personal,
                Date = DateTime.Today.AddDays(-12)
            }
        };

        // 5. Presupuestos mensuales
        Data.BudgetCategories = new List<BudgetCategory>
        {
            new BudgetCategory { Category = "Alimentación", MonthlyLimit = 400m, Icon = "cart" },
            new BudgetCategory { Category = "Vivienda", MonthlyLimit = 900m, Icon = "home" },
            new BudgetCategory { Category = "Ocio", MonthlyLimit = 150m, Icon = "cocktail" },
            new BudgetCategory { Category = "Transporte", MonthlyLimit = 100m, Icon = "car" },
            new BudgetCategory { Category = "Software", MonthlyLimit = 100m, Icon = "laptop" },
            new BudgetCategory { Category = "Material", MonthlyLimit = 150m, Icon = "box" }
        };

        // 6. Fondo de liquidez
        Data.LiquidityFund = new LiquidityFund
        {
            TargetAmount = 10000m,
            CurrentBalance = 4200m,
            Movements = new List<FundMovement>
            {
                new FundMovement
                {
                    Date = DateTime.Today.AddDays(-18),
                    Type = FundMovementType.RetiroPorTensionLiquidez,
                    Amount = 1000m,
                    Reason = "Adelanto para pago trimestral mientras llega cobro de Bélgica"
                }
            }
        };

        // 7. Préstamo padres
        Data.FamilyLoan = new FamilyLoan
        {
            InitialAmount = 8000m,
            Repayments = new List<FamilyLoanRepayment>
            {
                new FamilyLoanRepayment
                {
                    Date = DateTime.Today.AddMonths(-2),
                    Amount = 1000m,
                    Note = "Transferencia amortización préstamo"
                },
                new FamilyLoanRepayment
                {
                    Date = DateTime.Today.AddMonths(-1),
                    Amount = 1000m,
                    Note = "Transferencia amortización préstamo"
                }
            }
        };

        // 8. Fondos de familia / multidivisa
        Data.FamilyFundItems = new List<FamilyFundItem>
        {
            new FamilyFundItem
            {
                Person = "Mamá",
                Direction = DirectionType.RecibidoParaAdministrar,
                OriginalCurrency = CurrencyType.USD,
                OriginalAmount = 500m,
                ExchangeRate = 0.92m,
                ExchangeRateDate = DateTime.Today.AddDays(-15),
                Purpose = "Compra personal encargada en España",
                SpentEur = 320m, // gastado 320€ de 460€
                IsSettled = false,
                Notes = "Quedan 140€ por gastar o devolver"
            },
            new FamilyFundItem
            {
                Person = "Mamá",
                Direction = DirectionType.AdelantadoPorMi,
                OriginalCurrency = CurrencyType.USD,
                OriginalAmount = 450m,
                ExchangeRate = 0.92m,
                ExchangeRateDate = DateTime.Today.AddDays(-5),
                Purpose = "Compré algo para mi madre por adelantado",
                SpentEur = 450m,
                IsSettled = false,
                Notes = "Pendiente de recibir devolución acordada en USD"
            }
        };

        // 9. Pagos a pareja
        Data.PartnerPayments = new List<PartnerPayment>
        {
            new PartnerPayment
            {
                Date = DateTime.Today.AddMonths(-1),
                Amount = 400m,
                Concept = "Transferencia liquidación gastos agosto",
                FromWhom = "Yo"
            }
        };

        // 10. Objetivos de ahorro y Huchas
        Data.SavingsConfig = new SavingsGoalConfig
        {
            MonthlySavingsTarget = 500m,
            Jars = new List<SavingsJar>
            {
                new SavingsJar { Name = "Fondo Emergencia", TargetAmount = 6000m, CurrentAmount = 3500m, Color = "emerald", Icon = "shield" },
                new SavingsJar { Name = "Vivienda / Entrada", TargetAmount = 15000m, CurrentAmount = 4800m, Color = "blue", Icon = "home" },
                new SavingsJar { Name = "Viajes & Vacaciones", TargetAmount = 2000m, CurrentAmount = 850m, Color = "amber", Icon = "plane" },
                new SavingsJar { Name = "Reserva Impuestos Anual", TargetAmount = 4000m, CurrentAmount = 1800m, Color = "purple", Icon = "file-text" }
            }
        };

        // 11. Proyectos
        Data.Projects = new List<Project>
        {
            new Project
            {
                Name = "Calle X — Interiorismo",
                Client = "Interior Studio BCN",
                HourlyRate = 35m,
                EstimatedHours = 100m,
                Status = ProjectStatusType.EnCurso,
                WorkLogs = new List<WorkLog>
                {
                    new WorkLog { Date = DateTime.Today.AddDays(-3), Hours = 4m, Type = HourLogType.Facturable, Description = "Diseño de planos de iluminación" },
                    new WorkLog { Date = DateTime.Today.AddDays(-2), Hours = 6m, Type = HourLogType.Facturable, Description = "Modelado 3D de cocina y salón" },
                    new WorkLog { Date = DateTime.Today.AddDays(-1), Hours = 5m, Type = HourLogType.Facturable, Description = "Selección de materiales y mobiliario" },
                    new WorkLog { Date = DateTime.Today.AddDays(-1), Hours = 2m, Type = HourLogType.GestionYReuniones, Description = "Reunión con proveedores y emails" }
                }
            },
            new Project
            {
                Name = "Reforma Loft Bruselas",
                Client = "Studio Brussels BV",
                HourlyRate = 40m,
                EstimatedHours = 80m,
                Status = ProjectStatusType.EnCurso,
                WorkLogs = new List<WorkLog>
                {
                    new WorkLog { Date = DateTime.Today.AddDays(-10), Hours = 12m, Type = HourLogType.Facturable, Description = "Estudio previo de distribución" }
                }
            }
        };

        if (saveImmediately)
        {
            _ = SaveAsync();
        }
    }
}
