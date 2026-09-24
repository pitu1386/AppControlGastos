-- ==========================================================
-- APP CONTROL GASTOS & CONTABILIDAD AUTÓNOMO
-- ESQUEMA COMPLETO DE BASE DE DATOS PARA SUPABASE
-- ==========================================================

-- Habilitar extensión pgcrypto para UUIDs
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Limpiar tablas si existen
DROP TABLE IF EXISTS public.work_logs CASCADE;
DROP TABLE IF EXISTS public.projects CASCADE;
DROP TABLE IF EXISTS public.savings_jars CASCADE;
DROP TABLE IF EXISTS public.partner_payments CASCADE;
DROP TABLE IF EXISTS public.family_fund_items CASCADE;
DROP TABLE IF EXISTS public.family_loan_repayments CASCADE;
DROP TABLE IF EXISTS public.family_loan CASCADE;
DROP TABLE IF EXISTS public.fund_movements CASCADE;
DROP TABLE IF EXISTS public.liquidity_fund CASCADE;
DROP TABLE IF EXISTS public.budget_categories CASCADE;
DROP TABLE IF EXISTS public.installment_purchases CASCADE;
DROP TABLE IF EXISTS public.fixed_expenses CASCADE;
DROP TABLE IF EXISTS public.expenses CASCADE;
DROP TABLE IF EXISTS public.invoices CASCADE;
DROP TABLE IF EXISTS public.app_settings CASCADE;

-- 1. CONFIGURACIÓN GENERAL Y SALDO BANCARIO
CREATE TABLE public.app_settings (
    id TEXT PRIMARY KEY DEFAULT 'config',
    bank_account_balance NUMERIC(12, 2) NOT NULL DEFAULT 8500.00,
    monthly_savings_target NUMERIC(12, 2) NOT NULL DEFAULT 500.00,
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 2. FACTURAS E INGRESOS (NACIONALES, BÉLGICA / EXTERIOR, IRPF, COBROS A 90 DÍAS)
CREATE TABLE public.invoices (
    id TEXT PRIMARY KEY DEFAULT gen_random_uuid()::TEXT,
    number TEXT NOT NULL,
    client_name TEXT NOT NULL,
    client_country TEXT NOT NULL DEFAULT 'España',
    invoice_type TEXT NOT NULL DEFAULT 'Nacional', -- 'Nacional' | 'Intracomunitaria' | 'Exterior'
    issue_date DATE NOT NULL DEFAULT CURRENT_DATE,
    expected_payment_date DATE NOT NULL DEFAULT (CURRENT_DATE + INTERVAL '30 days'),
    base_amount NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    irpf_rate NUMERIC(5, 2) NOT NULL DEFAULT 15.00,
    apply_irpf_withholding BOOLEAN NOT NULL DEFAULT TRUE,
    must_reserve_irpf_externally BOOLEAN NOT NULL DEFAULT FALSE,
    external_irpf_reserve_rate NUMERIC(5, 2) NOT NULL DEFAULT 20.00,
    vat_rate NUMERIC(5, 2) NOT NULL DEFAULT 21.00,
    apply_vat BOOLEAN NOT NULL DEFAULT TRUE,
    is_collected BOOLEAN NOT NULL DEFAULT FALSE,
    collected_date DATE,
    collected_amount NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    notes TEXT DEFAULT '',
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 3. GASTOS (PROFESIONAL, PERSONAL, COMPARTIDO)
CREATE TABLE public.expenses (
    id TEXT PRIMARY KEY DEFAULT gen_random_uuid()::TEXT,
    date DATE NOT NULL DEFAULT CURRENT_DATE,
    concept TEXT NOT NULL,
    category TEXT NOT NULL DEFAULT 'Otros',
    amount NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    payment_method TEXT NOT NULL DEFAULT 'Tarjeta',
    scope TEXT NOT NULL DEFAULT 'Profesional', -- 'Profesional' | 'Personal' | 'Compartido'
    shared_percentage_user NUMERIC(5, 2) NOT NULL DEFAULT 50.00,
    paid_by TEXT NOT NULL DEFAULT 'Yo', -- 'Yo' | 'Pareja'
    installment_purchase_id TEXT,
    notes TEXT DEFAULT '',
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 4. GASTOS FIJOS Y RESERVAS PERIÓDICAS
CREATE TABLE public.fixed_expenses (
    id TEXT PRIMARY KEY DEFAULT gen_random_uuid()::TEXT,
    concept TEXT NOT NULL,
    category TEXT NOT NULL DEFAULT 'Gastos Fijos',
    amount NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    frequency TEXT NOT NULL DEFAULT 'Mensual', -- 'Mensual' | 'Bimestral' | 'Trimestral' | 'Anual'
    day_of_month INTEGER NOT NULL DEFAULT 1,
    scope TEXT NOT NULL DEFAULT 'Profesional',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    notes TEXT DEFAULT ''
);

-- 5. COMPRAS EN CUOTAS Y GASTOS A PLAZOS
CREATE TABLE public.installment_purchases (
    id TEXT PRIMARY KEY DEFAULT gen_random_uuid()::TEXT,
    concept TEXT NOT NULL,
    category TEXT NOT NULL DEFAULT 'Compras',
    total_amount NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    total_installments INTEGER NOT NULL DEFAULT 1,
    paid_installments INTEGER NOT NULL DEFAULT 0,
    custom_installment_amount NUMERIC(12, 2) DEFAULT 0.00,
    start_date DATE NOT NULL DEFAULT CURRENT_DATE,
    next_due_date DATE NOT NULL DEFAULT CURRENT_DATE,
    scope TEXT NOT NULL DEFAULT 'Personal',
    notes TEXT DEFAULT ''
);

-- 6. TECHOS Y PRESUPUESTOS MENSUALES POR CATEGORÍA
CREATE TABLE public.budget_categories (
    id TEXT PRIMARY KEY DEFAULT gen_random_uuid()::TEXT,
    category TEXT UNIQUE NOT NULL,
    monthly_limit NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    icon TEXT DEFAULT 'tag'
);

-- 7. COLCHÓN DE LIQUIDEZ Y MOVIMIENTOS POR TENSIÓN
CREATE TABLE public.liquidity_fund (
    id TEXT PRIMARY KEY DEFAULT 'main',
    target_amount NUMERIC(12, 2) NOT NULL DEFAULT 10000.00,
    current_balance NUMERIC(12, 2) NOT NULL DEFAULT 4200.00
);

CREATE TABLE public.fund_movements (
    id TEXT PRIMARY KEY DEFAULT gen_random_uuid()::TEXT,
    date DATE NOT NULL DEFAULT CURRENT_DATE,
    movement_type TEXT NOT NULL, -- 'RetiroPorTensionLiquidez' | 'Reposicion' | 'AporteExtraordinario'
    amount NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    reason TEXT NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 8. PRÉSTAMO FAMILIAR DE PADRES
CREATE TABLE public.family_loan (
    id TEXT PRIMARY KEY DEFAULT 'main',
    initial_amount NUMERIC(12, 2) NOT NULL DEFAULT 8000.00
);

CREATE TABLE public.family_loan_repayments (
    id TEXT PRIMARY KEY DEFAULT gen_random_uuid()::TEXT,
    date DATE NOT NULL DEFAULT CURRENT_DATE,
    amount NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    note TEXT NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 9. FONDOS DE FAMILIA Y TERCEROS (MULTIDIVISA EUR / USD / ARS)
CREATE TABLE public.family_fund_items (
    id TEXT PRIMARY KEY DEFAULT gen_random_uuid()::TEXT,
    person TEXT NOT NULL DEFAULT 'Mamá',
    direction TEXT NOT NULL DEFAULT 'RecibidoParaAdministrar', -- 'RecibidoParaAdministrar' | 'AdelantadoPorMi'
    original_currency TEXT NOT NULL DEFAULT 'USD', -- 'EUR' | 'USD' | 'ARS'
    original_amount NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    exchange_rate NUMERIC(10, 4) NOT NULL DEFAULT 1.0000,
    exchange_rate_date DATE NOT NULL DEFAULT CURRENT_DATE,
    purpose TEXT NOT NULL DEFAULT '',
    spent_eur NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    must_return_surplus BOOLEAN NOT NULL DEFAULT TRUE,
    is_settled BOOLEAN NOT NULL DEFAULT FALSE,
    notes TEXT DEFAULT '',
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 10. TRANSFERENCIAS Y LIQUIDACIONES CON PAREJA
CREATE TABLE public.partner_payments (
    id TEXT PRIMARY KEY DEFAULT gen_random_uuid()::TEXT,
    date DATE NOT NULL DEFAULT CURRENT_DATE,
    amount NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    concept TEXT NOT NULL,
    from_whom TEXT NOT NULL DEFAULT 'Yo', -- 'Yo' | 'Pareja'
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 11. HUCHAS Y FONDOS DE AHORRO
CREATE TABLE public.savings_jars (
    id TEXT PRIMARY KEY DEFAULT gen_random_uuid()::TEXT,
    name TEXT NOT NULL,
    target_amount NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    current_amount NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    icon TEXT DEFAULT 'piggy-bank',
    color TEXT DEFAULT 'emerald'
);

-- 12. PROYECTOS Y PARTES DE HORAS
CREATE TABLE public.projects (
    id TEXT PRIMARY KEY DEFAULT gen_random_uuid()::TEXT,
    name TEXT NOT NULL,
    client TEXT NOT NULL,
    hourly_rate NUMERIC(10, 2) NOT NULL DEFAULT 35.00,
    estimated_hours NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
    status TEXT NOT NULL DEFAULT 'EnCurso', -- 'EnCurso' | 'Facturado' | 'Completado' | 'Pausado'
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE public.work_logs (
    id TEXT PRIMARY KEY DEFAULT gen_random_uuid()::TEXT,
    project_id TEXT NOT NULL REFERENCES public.projects(id) ON DELETE CASCADE,
    date DATE NOT NULL DEFAULT CURRENT_DATE,
    hours NUMERIC(6, 2) NOT NULL DEFAULT 0.00,
    log_type TEXT NOT NULL DEFAULT 'Facturable', -- 'Facturable' | 'GestionYReuniones' | 'NoFacturable'
    description TEXT DEFAULT '',
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- ==========================================================
-- INSERCIÓN DE DATOS REALISTAS INICIALES
-- ==========================================================

INSERT INTO public.app_settings (id, bank_account_balance, monthly_savings_target)
VALUES ('config', 8500.00, 500.00);

-- Facturas
INSERT INTO public.invoices (id, number, client_name, client_country, invoice_type, issue_date, expected_payment_date, base_amount, apply_vat, vat_rate, apply_irpf_withholding, must_reserve_irpf_externally, external_irpf_reserve_rate, is_collected, notes)
VALUES
  ('inv-001', 'FAC-2026-003', 'Studio Brussels BV', 'Bélgica', 'Intracomunitaria', CURRENT_DATE - 20, CURRENT_DATE + 70, 4000.00, FALSE, 0, FALSE, TRUE, 20.00, FALSE, 'Proyecto Bruselas a 90 días con reserva IRPF para Mod 130'),
  ('inv-002', 'FAC-2026-002', 'Diseño Interior Madrid SL', 'España', 'Nacional', CURRENT_DATE - 10, CURRENT_DATE + 20, 2500.00, TRUE, 21.00, TRUE, FALSE, 0, FALSE, 'Planos ejecución obra'),
  ('inv-003', 'FAC-2026-001', 'Atelier Decoración BCN', 'España', 'Nacional', CURRENT_DATE - 40, CURRENT_DATE - 10, 2850.00, TRUE, 21.00, TRUE, FALSE, 0, TRUE, 'Cobrada puntualmente');

-- Gastos fijos
INSERT INTO public.fixed_expenses (concept, category, amount, frequency, day_of_month, scope)
VALUES
  ('Cuota Autónomos RETA', 'Autónomos', 320.00, 'Mensual', 30, 'Profesional'),
  ('Gestoría Fiscal & Laboral', 'Gestoría', 90.00, 'Mensual', 5, 'Profesional'),
  ('Software Adobe CC + Figma', 'Software', 65.00, 'Mensual', 15, 'Profesional'),
  ('Alquiler Vivienda', 'Vivienda', 900.00, 'Mensual', 1, 'Compartido'),
  ('Seguro Responsabilidad Civil', 'Otros', 360.00, 'Anual', 15, 'Profesional');

-- Compras a cuotas
INSERT INTO public.installment_purchases (concept, category, total_amount, total_installments, paid_installments, custom_installment_amount, start_date, next_due_date, scope, notes)
VALUES
  ('Viaje / Compra Argentina', 'Ocio', 840.00, 7, 3, 120.00, CURRENT_DATE - INTERVAL '3 months', CURRENT_DATE + 15, 'Personal', 'Cuota 3 de 7 a 120 €/mes');

-- Presupuestos
INSERT INTO public.budget_categories (category, monthly_limit, icon)
VALUES
  ('Alimentación', 400.00, 'cart'),
  ('Vivienda', 900.00, 'home'),
  ('Ocio', 150.00, 'cocktail'),
  ('Transporte', 100.00, 'car'),
  ('Software', 100.00, 'laptop');

-- Colchón de liquidez
INSERT INTO public.liquidity_fund (id, target_amount, current_balance)
VALUES ('main', 10000.00, 4200.00);

INSERT INTO public.fund_movements (date, movement_type, amount, reason)
VALUES
  (CURRENT_DATE - 18, 'RetiroPorTensionLiquidez', 1000.00, 'Adelanto temporal por cobro pendiente de Bélgica a 90 días');

-- Préstamo familiar
INSERT INTO public.family_loan (id, initial_amount)
VALUES ('main', 8000.00);

INSERT INTO public.family_loan_repayments (date, amount, note)
VALUES
  (CURRENT_DATE - INTERVAL '2 months', 1000.00, 'Transferencia amortización préstamo padres'),
  (CURRENT_DATE - INTERVAL '1 month', 1000.00, 'Transferencia amortización préstamo padres');

-- Dinero familia multidivisa
INSERT INTO public.family_fund_items (person, direction, original_currency, original_amount, exchange_rate, purpose, spent_eur, is_settled)
VALUES
  ('Mamá', 'RecibidoParaAdministrar', 'USD', 500.00, 0.9200, 'Compra personal encargada en España', 320.00, FALSE),
  ('Mamá', 'AdelantadoPorMi', 'USD', 450.00, 0.9200, 'Compra adelantada para mamá pendiente de recibir', 450.00, FALSE);

-- Pareja
INSERT INTO public.partner_payments (date, amount, concept, from_whom)
VALUES
  (CURRENT_DATE - INTERVAL '1 month', 400.00, 'Transferencia regularización gastos compartidos', 'Yo');

-- Huchas
INSERT INTO public.savings_jars (name, target_amount, current_amount, color, icon)
VALUES
  ('Fondo Emergencia', 6000.00, 3500.00, 'emerald', 'shield'),
  ('Vivienda / Entrada', 15000.00, 4800.00, 'blue', 'home'),
  ('Viajes & Vacaciones', 2000.00, 850.00, 'amber', 'plane'),
  ('Reserva Impuestos Anual', 4000.00, 1800.00, 'purple', 'file-text');

-- Proyectos
INSERT INTO public.projects (id, name, client, hourly_rate, estimated_hours, status)
VALUES
  ('proj-001', 'Calle X — Interiorismo', 'Interior Studio BCN', 35.00, 100.00, 'EnCurso'),
  ('proj-002', 'Reforma Loft Bruselas', 'Studio Brussels BV', 40.00, 80.00, 'EnCurso');

INSERT INTO public.work_logs (project_id, date, hours, log_type, description)
VALUES
  ('proj-001', CURRENT_DATE - 3, 4.00, 'Facturable', 'Diseño de planos e iluminación'),
  ('proj-001', CURRENT_DATE - 2, 6.00, 'Facturable', 'Modelado 3D de cocina'),
  ('proj-001', CURRENT_DATE - 1, 5.00, 'Facturable', 'Selección de materiales y compras'),
  ('proj-001', CURRENT_DATE - 1, 2.00, 'GestionYReuniones', 'Reunión proveedores y llamadas');

-- Habilitar Row Level Security (RLS)
ALTER TABLE public.app_settings ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.invoices ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.expenses ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.fixed_expenses ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.installment_purchases ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.budget_categories ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.liquidity_fund ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.fund_movements ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.family_loan ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.family_loan_repayments ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.family_fund_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.partner_payments ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.savings_jars ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.projects ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.work_logs ENABLE ROW LEVEL SECURITY;

-- Políticas de acceso para clave anónima / autenticada
CREATE POLICY "Acceso total anon app_settings" ON public.app_settings FOR ALL USING (true);
CREATE POLICY "Acceso total anon invoices" ON public.invoices FOR ALL USING (true);
CREATE POLICY "Acceso total anon expenses" ON public.expenses FOR ALL USING (true);
CREATE POLICY "Acceso total anon fixed_expenses" ON public.fixed_expenses FOR ALL USING (true);
CREATE POLICY "Acceso total anon installment_purchases" ON public.installment_purchases FOR ALL USING (true);
CREATE POLICY "Acceso total anon budget_categories" ON public.budget_categories FOR ALL USING (true);
CREATE POLICY "Acceso total anon liquidity_fund" ON public.liquidity_fund FOR ALL USING (true);
CREATE POLICY "Acceso total anon fund_movements" ON public.fund_movements FOR ALL USING (true);
CREATE POLICY "Acceso total anon family_loan" ON public.family_loan FOR ALL USING (true);
CREATE POLICY "Acceso total anon family_loan_repayments" ON public.family_loan_repayments FOR ALL USING (true);
CREATE POLICY "Acceso total anon family_fund_items" ON public.family_fund_items FOR ALL USING (true);
CREATE POLICY "Acceso total anon partner_payments" ON public.partner_payments FOR ALL USING (true);
CREATE POLICY "Acceso total anon savings_jars" ON public.savings_jars FOR ALL USING (true);
CREATE POLICY "Acceso total anon projects" ON public.projects FOR ALL USING (true);
CREATE POLICY "Acceso total anon work_logs" ON public.work_logs FOR ALL USING (true);
