-- ==========================================================
-- APP CONTROL GASTOS & CONTABILIDAD AUTÓNOMO
-- ESQUEMA COMPATIBLE PARA COMPARTIR PROYECTO SUPABASE CON APPCOBROS
--
-- IMPORTANTE:
-- - NO TOCA NI BORRA NINGUNA TABLA DE APPCOBROS (config, grupos, clientes, movimientos, papelera).
-- - Todas las tablas de esta app usan el prefijo "gastos_".
-- - Ejecutar en: Supabase Dashboard -> SQL Editor -> New Query -> Run
-- ==========================================================

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- 0. BORRADO EXCLUSIVO DE TABLAS DE APPCONTROLGASTOS (NUNCA DE OTRAS APPS)
DROP TABLE IF EXISTS public.gastos_work_logs CASCADE;
DROP TABLE IF EXISTS public.gastos_projects CASCADE;
DROP TABLE IF EXISTS public.gastos_savings_jars CASCADE;
DROP TABLE IF EXISTS public.gastos_partner_payments CASCADE;
DROP TABLE IF EXISTS public.gastos_family_fund_items CASCADE;
DROP TABLE IF EXISTS public.gastos_family_loan_repayments CASCADE;
DROP TABLE IF EXISTS public.gastos_family_loan CASCADE;
DROP TABLE IF EXISTS public.gastos_fund_movements CASCADE;
DROP TABLE IF EXISTS public.gastos_liquidity_fund CASCADE;
DROP TABLE IF EXISTS public.gastos_budget_categories CASCADE;
DROP TABLE IF EXISTS public.gastos_installment_purchases CASCADE;
DROP TABLE IF EXISTS public.gastos_fixed_expenses CASCADE;
DROP TABLE IF EXISTS public.gastos_expenses CASCADE;
DROP TABLE IF EXISTS public.gastos_invoices CASCADE;
DROP TABLE IF EXISTS public.gastos_settings CASCADE;

-- 1. CONFIGURACIÓN Y SALDO BANCARIO
CREATE TABLE public.gastos_settings (
    id TEXT PRIMARY KEY DEFAULT 'current',
    bank_account_balance NUMERIC(12, 2) NOT NULL DEFAULT 8500.00,
    monthly_savings_target NUMERIC(12, 2) NOT NULL DEFAULT 500.00,
    snapshot_json JSONB DEFAULT NULL,
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 2. FACTURAS E INGRESOS (NACIONALES, EXTERIOR/BÉLGICA, IRPF, 90 DÍAS)
CREATE TABLE public.gastos_invoices (
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
CREATE TABLE public.gastos_expenses (
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
CREATE TABLE public.gastos_fixed_expenses (
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
CREATE TABLE public.gastos_installment_purchases (
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

-- 6. TECHOS DE PRESUPUESTO MENSUAL POR CATEGORÍA
CREATE TABLE public.gastos_budget_categories (
    id TEXT PRIMARY KEY DEFAULT gen_random_uuid()::TEXT,
    category TEXT NOT NULL UNIQUE,
    monthly_limit NUMERIC(12, 2) NOT NULL DEFAULT 0.00
);

-- 7. FONDO DE LIQUIDEZ (COLCHÓN)
CREATE TABLE public.gastos_liquidity_fund (
    id TEXT PRIMARY KEY DEFAULT 'current',
    target_amount NUMERIC(12, 2) NOT NULL DEFAULT 10000.00,
    current_amount NUMERIC(12, 2) NOT NULL DEFAULT 4200.00,
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 8. MOVIMIENTOS DEL FONDO DE LIQUIDEZ
CREATE TABLE public.gastos_fund_movements (
    id TEXT PRIMARY KEY DEFAULT gen_random_uuid()::TEXT,
    date DATE NOT NULL DEFAULT CURRENT_DATE,
    type TEXT NOT NULL, -- 'RetiroUrgente' | 'Reposicion'
    amount NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    concept TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 9. PRÉSTAMO FAMILIAR (PADRES)
CREATE TABLE public.gastos_family_loan (
    id TEXT PRIMARY KEY DEFAULT 'current',
    initial_amount NUMERIC(12, 2) NOT NULL DEFAULT 8000.00,
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 10. REPOSICIONES AL PRÉSTAMO FAMILIAR
CREATE TABLE public.gastos_family_loan_repayments (
    id TEXT PRIMARY KEY DEFAULT gen_random_uuid()::TEXT,
    date DATE NOT NULL DEFAULT CURRENT_DATE,
    amount NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    concept TEXT NOT NULL DEFAULT 'Devolución préstamo familiar',
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 11. FONDOS DE FAMILIA / TERCEROS (MULTIDIVISA EUR/USD/ARS)
CREATE TABLE public.gastos_family_fund_items (
    id TEXT PRIMARY KEY DEFAULT gen_random_uuid()::TEXT,
    owner_name TEXT NOT NULL,
    currency TEXT NOT NULL DEFAULT 'EUR', -- 'EUR' | 'USD' | 'ARS'
    amount NUMERIC(14, 2) NOT NULL DEFAULT 0.00,
    exchange_rate_to_eur NUMERIC(14, 6) NOT NULL DEFAULT 1.000000,
    notes TEXT DEFAULT '',
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 12. LIQUIDACIONES / PAGOS DE AJUSTE CON PAREJA
CREATE TABLE public.gastos_partner_payments (
    id TEXT PRIMARY KEY DEFAULT gen_random_uuid()::TEXT,
    date DATE NOT NULL DEFAULT CURRENT_DATE,
    from_person TEXT NOT NULL, -- 'Yo' | 'Pareja'
    to_person TEXT NOT NULL,
    amount NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    notes TEXT DEFAULT ''
);

-- 13. METAS Y HUCHAS DE AHORRO
CREATE TABLE public.gastos_savings_jars (
    id TEXT PRIMARY KEY DEFAULT gen_random_uuid()::TEXT,
    name TEXT NOT NULL,
    target_amount NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    current_amount NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    icon TEXT NOT NULL DEFAULT 'piggy-bank'
);

-- 14. PROYECTOS Y CLIENTES
CREATE TABLE public.gastos_projects (
    id TEXT PRIMARY KEY DEFAULT gen_random_uuid()::TEXT,
    name TEXT NOT NULL,
    client_name TEXT NOT NULL,
    hourly_rate NUMERIC(10, 2) NOT NULL DEFAULT 40.00,
    agreed_fixed_price NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    billing_model TEXT NOT NULL DEFAULT 'PorHora', -- 'PorHora' | 'PrecioFijo' | 'Mixto'
    status TEXT NOT NULL DEFAULT 'Activo', -- 'Activo' | 'Pausado' | 'Completado' | 'Cancelado'
    direct_expenses NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 15. REGISTROS DE HORAS (WORK LOGS)
CREATE TABLE public.gastos_work_logs (
    id TEXT PRIMARY KEY DEFAULT gen_random_uuid()::TEXT,
    project_id TEXT NOT NULL REFERENCES public.gastos_projects(id) ON DELETE CASCADE,
    date DATE NOT NULL DEFAULT CURRENT_DATE,
    hours NUMERIC(6, 2) NOT NULL DEFAULT 0.00,
    description TEXT NOT NULL DEFAULT '',
    is_billable BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS gastos_work_logs_project_idx ON public.gastos_work_logs (project_id);

-- ==========================================================
-- ROW LEVEL SECURITY (RLS)
-- Permitimos acceso a authenticated (usuarios logueados) y a anon
-- para máxima flexibilidad y compatibilidad total.
-- ==========================================================
ALTER TABLE public.gastos_settings ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.gastos_invoices ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.gastos_expenses ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.gastos_fixed_expenses ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.gastos_installment_purchases ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.gastos_budget_categories ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.gastos_liquidity_fund ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.gastos_fund_movements ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.gastos_family_loan ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.gastos_family_loan_repayments ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.gastos_family_fund_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.gastos_partner_payments ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.gastos_savings_jars ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.gastos_projects ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.gastos_work_logs ENABLE ROW LEVEL SECURITY;

DO $$ 
DECLARE
    tbl text;
BEGIN
    FOR tbl IN 
        SELECT tablename FROM pg_tables 
        WHERE schemaname = 'public' AND tablename LIKE 'gastos_%'
    LOOP
        EXECUTE format('DROP POLICY IF EXISTS "%s_auth_all" ON public.%I;', tbl, tbl);
        EXECUTE format('CREATE POLICY "%s_auth_all" ON public.%I FOR ALL TO authenticated USING (true) WITH CHECK (true);', tbl, tbl);

        EXECUTE format('DROP POLICY IF EXISTS "%s_anon_all" ON public.%I;', tbl, tbl);
        EXECUTE format('CREATE POLICY "%s_anon_all" ON public.%I FOR ALL TO anon USING (true) WITH CHECK (true);', tbl, tbl);
    END LOOP;
END $$;

-- ==========================================================
-- REALTIME: sincronización en tiempo real
-- ==========================================================
DO $$
DECLARE
    tbl text;
BEGIN
    FOR tbl IN 
        SELECT tablename FROM pg_tables 
        WHERE schemaname = 'public' AND tablename LIKE 'gastos_%'
    LOOP
        BEGIN
            EXECUTE format('ALTER PUBLICATION supabase_realtime ADD TABLE public.%I;', tbl);
        EXCEPTION WHEN duplicate_object THEN
            -- ya estaba en la publicación, ignorar
        END;
    END LOOP;
END $$;

-- ==========================================================
-- CARGA DE DATOS INICIALES DEMO
-- ==========================================================
INSERT INTO public.gastos_settings (id, bank_account_balance, monthly_savings_target)
VALUES ('current', 8500.00, 500.00)
ON CONFLICT (id) DO UPDATE SET
    bank_account_balance = EXCLUDED.bank_account_balance,
    monthly_savings_target = EXCLUDED.monthly_savings_target;

INSERT INTO public.gastos_liquidity_fund (id, target_amount, current_amount)
VALUES ('current', 10000.00, 4200.00)
ON CONFLICT (id) DO UPDATE SET
    target_amount = EXCLUDED.target_amount,
    current_amount = EXCLUDED.current_amount;

INSERT INTO public.gastos_family_loan (id, initial_amount)
VALUES ('current', 8000.00)
ON CONFLICT (id) DO UPDATE SET
    initial_amount = EXCLUDED.initial_amount;

-- Facturas
INSERT INTO public.gastos_invoices (id, number, client_name, client_country, invoice_type, issue_date, expected_payment_date, base_amount, irpf_rate, apply_irpf_withholding, must_reserve_irpf_externally, external_irpf_reserve_rate, vat_rate, apply_vat, is_collected, collected_date, collected_amount, notes)
VALUES
('inv-001', 'FAC-2026-081', 'Acme Belgium BV', 'Bélgica', 'Intracomunitaria', '2026-07-20', '2026-10-18', 4000.00, 0.00, false, true, 20.00, 0.00, false, false, null, 0.00, 'Factura Bélgica a 90 días. Requiere reserva IRPF Modelo 130 (20%).'),
('inv-002', 'FAC-2026-088', 'Servicios Digitales Madrid SL', 'España', 'Nacional', '2026-08-15', '2026-10-15', 2500.00, 15.00, true, false, 0.00, 21.00, true, false, null, 0.00, 'Cliente nacional a 60 días con retención del 15% de IRPF.'),
('inv-003', 'FAC-2026-092', 'Nordic Solutions Oy', 'Finlandia', 'Intracomunitaria', '2026-08-20', '2026-11-20', 3200.00, 0.00, false, true, 20.00, 0.00, false, false, null, 0.00, 'Factura Finlandia a 90 días. Reserva 20% IRPF.'),
('inv-004', 'FAC-2026-075', 'Consulting BCN SA', 'España', 'Nacional', '2026-08-01', '2026-09-01', 2850.00, 15.00, true, false, 0.00, 21.00, true, true, '2026-09-05', 3021.00, 'Cobrada puntualmente.')
ON CONFLICT (id) DO NOTHING;

-- Gastos Fijos
INSERT INTO public.gastos_fixed_expenses (id, concept, category, amount, frequency, day_of_month, scope, is_active, notes)
VALUES
('fx-001', 'Cuota de Autónomos', 'Seguridad Social', 320.00, 'Mensual', 30, 'Profesional', true, 'Cargo último día del mes en cuenta.'),
('fx-002', 'Asesoría / Gestoría Fiscal', 'Gestoría', 90.00, 'Mensual', 5, 'Profesional', true, 'Pago recurrente día 5 de mes.'),
('fx-003', 'Suscripción Software y Servidores', 'Software', 65.00, 'Mensual', 15, 'Profesional', true, 'Cloud, GitHub Copilot y Figma.'),
('fx-004', 'Alquiler Vivienda y Despacho', 'Vivienda', 900.00, 'Mensual', 1, 'Compartido', true, '50% compartido con pareja.'),
('fx-005', 'Seguro de Responsabilidad Civil', 'Seguros', 360.00, 'Anual', 15, 'Profesional', true, 'Anual prorrateado 30 €/mes.')
ON CONFLICT (id) DO NOTHING;

-- Cuotas / Compras a plazos
INSERT INTO public.gastos_installment_purchases (id, concept, category, total_amount, total_installments, paid_installments, custom_installment_amount, start_date, next_due_date, scope, notes)
VALUES
('inst-001', 'Viaje Argentina', 'Viajes', 840.00, 7, 3, 120.00, '2026-07-10', '2026-10-10', 'Personal', 'Cuota actual: 3 de 7 pagadas. Quedan 4 cuotas de 120 € (480 € comprometidos).'),
('inst-002', 'MacBook Pro M-Series', 'Equipamiento', 2400.00, 12, 4, 200.00, '2026-05-15', '2026-10-15', 'Profesional', 'Herramienta de trabajo profesional. 4 de 12 cuotas pagadas.')
ON CONFLICT (id) DO NOTHING;

-- Techos de Presupuesto
INSERT INTO public.gastos_budget_categories (category, monthly_limit)
VALUES
('Alimentación', 400.00),
('Ocio', 150.00),
('Transporte', 100.00),
('Vivienda', 900.00),
('Compras', 150.00),
('Software', 100.00),
('Gestoría', 100.00),
('Equipamiento', 250.00)
ON CONFLICT (category) DO NOTHING;

-- Huchas de ahorro
INSERT INTO public.gastos_savings_jars (id, name, target_amount, current_amount, icon)
VALUES
('jar-1', 'Ahorro General', 5000.00, 1850.00, 'piggy-bank'),
('jar-2', 'Fondo de Emergencia', 6000.00, 2400.00, 'shield-alert'),
('jar-3', 'Viajes y Vacaciones', 2000.00, 950.00, 'plane'),
('jar-4', 'Proyectos Personales', 3000.00, 500.00, 'rocket'),
('jar-5', 'Reserva Impuestos Trimestral', 4000.00, 2600.00, 'landmark')
ON CONFLICT (id) DO NOTHING;

-- Reposiciones familiares
INSERT INTO public.gastos_family_loan_repayments (id, date, amount, concept)
VALUES
('flr-001', '2026-05-10', 1000.00, 'Devolución parcial cuota mayo'),
('flr-002', '2026-07-15', 500.00, 'Devolución parcial julio'),
('flr-003', '2026-08-20', 500.00, 'Devolución parcial agosto')
ON CONFLICT (id) DO NOTHING;

-- Fondos de Terceros / Familiares
INSERT INTO public.gastos_family_fund_items (id, owner_name, currency, amount, exchange_rate_to_eur, notes)
VALUES
('ffi-001', 'Padres (Reserva EUR)', 'EUR', 3500.00, 1.000000, 'Dinero guardado para transferencias en España.'),
('ffi-002', 'Familiar (Ahorros USD)', 'USD', 2000.00, 0.920000, 'Dólares en custodia (aprox. 1.840 €).'),
('ffi-003', 'Familiar (Cuenta ARS)', 'ARS', 450000.00, 0.000720, 'Pesos argentinos para pagos en origen (aprox. 324 €).')
ON CONFLICT (id) DO NOTHING;

-- Movimientos Fondo de Liquidez
INSERT INTO public.gastos_fund_movements (id, date, type, amount, concept)
VALUES
('fm-001', '2026-08-10', 'RetiroUrgente', 2300.00, 'Retiro temporal para cubrir pagos mientras se cobraba factura de cliente exterior'),
('fm-002', '2026-09-02', 'Reposicion', 500.00, 'Reposición parcial tras cobrar factura')
ON CONFLICT (id) DO NOTHING;

-- Proyectos
INSERT INTO public.gastos_projects (id, name, client_name, hourly_rate, agreed_fixed_price, billing_model, status, direct_expenses)
VALUES
('proj-1', 'Desarrollo API y Portal Web', 'Acme Belgium BV', 45.00, 0.00, 'PorHora', 'Activo', 120.00),
('proj-2', 'Mantenimiento Cloud y Soporte', 'Servicios Digitales Madrid SL', 40.00, 1500.00, 'Mixto', 'Activo', 0.00)
ON CONFLICT (id) DO NOTHING;
