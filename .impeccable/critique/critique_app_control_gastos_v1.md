---
target: "AppControlGastos Mobile PWA"
date: "2026-09-24"
total_score: 21
max_score: 40
na_heuristics: ""
p0_count: 1
p1_count: 1
p2_count: 1
p3_count: 1
method: "dual-agent (A: 875ab52a-e1eb-4b26-a648-41c125f0f0f5 · B: cf9895b9-3e21-4243-85be-7544d594f36d)"
---

# INFORME DE CRÍTICA DE DISEÑO & ERGONOMÍA MÓVIL
**Target:** FinanzasPro — PWA Móvil de Contabilidad Interna y Tesorería Autónomo  
**Método:** Dual-Agent (Assessment A: UX Design Review & Fiscal Domain · Assessment B: Mobile Ergonomics & Technical CSS Inspection)

## Design Health Score (Nielsen 10 Usability Heuristics)
Total: **21 / 40** (Aceptable - Requiere intervención prioritaria)

| # | Heurística | Puntuación | Hallazgo Clave |
|---|------------|------------|----------------|
| 1 | Visibilidad del estado del sistema | 2/4 | Falta feedback en operaciones críticas (sincronización, cobros). |
| 2 | Coincidencia sistema / mundo real | 4/4 | Sobresaliente. Habla el idioma del autónomo español (IRPF, Mod 130, Disponible Real). |
| 3 | Control y libertad del usuario | 2/4 | Borrado de registros sin confirmación modal ni opción de deshacer. |
| 4 | Consistencia y estándares | 2/4 | Mezcla de espaciados de escritorio (p-4) con móvil. Acceso duplicado a menú. |
| 5 | Prevención de errores | 1/4 | Modales con return silencioso al fallar validación de importe o concepto. |
| 6 | Reconocimiento antes que recuerdo | 3/4 | Etiquetas claras en navegación y cálculos fiscales automáticos. |
| 7 | Flexibilidad y eficiencia de uso | 2/4 | Faltan plantillas de facturación para clientes recurrentes. |
| 8 | Diseño estético y minimalista | 2/4 | Sobrecarga de tarjetas y colores en Home. Exceso de información en primer scroll. |
| 9 | Recuperación de errores | 1/4 | Mensajes de error ausentes en validación de formularios móviles. |
| 10 | Ayuda y documentación | 2/4 | Textos descriptivos buenos pero tooltips inaccesibles en pantallas táctiles. |

## Problemas Prioritarios Detectados y Resueltos
1. **[P0] Shell móvil y Menú inferior flotante roto**: Causa técnica identificada en el flexbox de `MainLayout` y `BottomNav`. Corregido fijando la altura a 100dvh, flex-shrink 0 en header y bottom nav, y scroll exclusivamente en el contenedor central.
2. **[P1] Modales de 650px incompatibles con teléfonos**: Transformados en Bottom Sheets táctiles con barra tirador y padding ergonómico.
3. **[P2] Tipografía desproporcionada (`display-6`) en cifras financieras**: Sustituida por tipografía fluida `clamp()` que no se corta en pantallas de 360px.
4. **[P3] Dianas táctiles inferiores a 44px**: Ampliados los botones de acción rápida, cobro y navegación a alturas ergonómicas accesibles.
