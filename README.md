# FinanzasPro - Contabilidad Interna y Gestión Financiera Personal y Profesional

Aplicación PWA en **.NET 10 (Blazor WebAssembly)** diseñada específicamente para autónomos y profesionales independientes (diseño/interiorismo/servicios) con clientes en España y el extranjero (Bélgica/UE), gastos compartidos en pareja, compras en cuotas, préstamos familiares multidivisa y control riguroso de tesorería y reservas fiscales.

---

## 🏛️ Filosofía: Los 5 Tipos de Dinero
El sistema separa tajantemente tu dinero en 5 naturalezas para que tener saldo en el banco nunca se confunda con poder gastarlo:

1. **Dinero Propio (Disponible Real 💚)**: Lo que verdaderamente te pertenece tras descontar reservas de IRPF/IVA, fijos, cuotas, deudas y reposición al fondo.
2. **Dinero Profesional 💼**: Facturación a clientes nacionales y de Bélgica/exterior, y gastos deducibles.
3. **Dinero de Terceros 👨‍👩‍👧**: Fondos familiares o encargos que administras en **EUR, USD o ARS** con tipo de cambio de referencia.
4. **Dinero Prestado / Deudas 🤝**: Préstamo de tus padres (8.000 € inicial con amortizaciones), retiros del colchón de liquidez y saldo pendiente con tu pareja.
5. **Dinero Reservado 🏦**: Retenciones de IRPF (15%) o reservas obligatorias para facturas exteriores sin retención (20% para el Mod 130), IVA, fijos inmediatos y cuotas a plazos.

---

## 📱 Diseño Mobile-First App Shell
Incluso en el navegador de escritorio (PC/Mac), la aplicación se muestra centrada en la pantalla con el formato y chasis de un **smartphone** (~500px, bordes redondeados y barra de navegación inferior táctil), con la misma experiencia que en el proyecto `AppPoblenou`.

---

## 🚀 Despliegue en GitHub Pages

Dispones de dos formas de publicar tu aplicación en GitHub Pages:

### Opción 1: Despliegue local rápido con PowerShell (`deploy.ps1`)
1. Crea tu repositorio en GitHub (por ejemplo `https://github.com/tu-usuario/AppControlGastos.git`).
2. Abre PowerShell en la carpeta del proyecto y ejecuta:
   ```powershell
   .\deploy.ps1 -RepoUrl "https://github.com/tu-usuario/AppControlGastos.git" -RepoName "AppControlGastos"
   ```
3. El script compila en Release, genera `.nojekyll`, copia `index.html` a `404.html`, ajusta el `<base href>` automáticamente y sube el resultado a la rama `gh-pages`.
4. En GitHub, entra en **Settings → Pages** y selecciona la rama `gh-pages` / `root`.
5. Tu app estará disponible en: `https://tu-usuario.github.io/AppControlGastos/`

### Opción 2: Despliegue automático por GitHub Actions
Al hacer push a la rama `main`, el workflow configurado en `.github/workflows/deploy.yml` compilará y publicará automáticamente la PWA en GitHub Pages.

---

## 🗄️ Base de Datos en Supabase

El esquema SQL completo con todas las tablas, relaciones, políticas RLS y los datos de ejemplo iniciales está listo en:
👉 `supabase/supabase_schema.sql`

### Cómo configurarlo:
1. Crea un proyecto en [Supabase](https://supabase.com).
2. Entra en **SQL Editor**.
3. Pega el contenido de [`supabase/supabase_schema.sql`](supabase/supabase_schema.sql) y pulsa **Run**.
4. Se crearán todas las tablas optimizadas (`invoices`, `expenses`, `fixed_expenses`, `installment_purchases`, `liquidity_fund`, `family_fund_items`, `projects`, etc.).

---

## 💻 Desarrollo Local
```bash
# Compilar
dotnet build

# Ejecutar localmente (disponible en PC y en el móvil por Wi-Fi)
dotnet run --launch-profile http
```
- URL local PC: `http://localhost:5156`
- URL móvil (red local): `http://192.168.0.14:5156`
