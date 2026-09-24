namespace AppControlGastos.Services;

/// <summary>
/// Constantes globales de configuración, versión y conexión a Supabase.
/// Comparte el mismo proyecto de Supabase que AppCobros sin colisiones.
/// </summary>
public static class AppInfo
{
    public const string AppName = "Control de Gastos y Finanzas Autónomo";
    public const string Version = "1.0.0";
    
    // Conexión al proyecto Supabase existente del usuario
    public const string SupabaseUrl = "https://dsnsqrqoxddtvqxaevtx.supabase.co";
    public const string SupabaseAnonKey = "sb_publishable_XaOLEWhYuofmj2CxX8PXlg_HFf7xTat";
}
