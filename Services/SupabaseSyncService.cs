using System.Text.Json;
using AppControlGastos.Models;

namespace AppControlGastos.Services;

public enum SyncState
{
    LocalOnly,
    Syncing,
    Synced,
    Error
}

/// <summary>
/// Gestiona la sincronización de datos con el proyecto Supabase existente.
/// Utiliza las tablas con prefijo gastos_* para garantizar aislamiento total respecto a AppCobros.
/// </summary>
public class SupabaseSyncService
{
    private readonly SupabaseClientService _sb;
    private readonly SupabaseAuthService _auth;

    public SyncState State { get; private set; } = SyncState.LocalOnly;
    public DateTime? LastSyncTime { get; private set; }
    public string? LastError { get; private set; }

    public event Action? OnSyncStatusChanged;

    public SupabaseSyncService(SupabaseClientService sb, SupabaseAuthService auth)
    {
        _sb = sb;
        _auth = auth;
    }

    private void SetState(SyncState state, string? error = null)
    {
        State = state;
        LastError = error;
        if (state == SyncState.Synced)
        {
            LastSyncTime = DateTime.Now;
        }
        OnSyncStatusChanged?.Invoke();
    }

    /// <summary>
    /// Sube todo el estado actual a la tabla gastos_settings (en columna snapshot_json y valores directos).
    /// </summary>
    public async Task<(bool Success, string Message)> PushToCloudAsync(AppData data)
    {
        SetState(SyncState.Syncing);
        try
        {
            var snapshotJson = JsonSerializer.Serialize(data, SupabaseClientService.JsonOptions);

            var settingRow = new Dictionary<string, object?>
            {
                ["id"] = "current",
                ["bank_account_balance"] = data.BankAccountBalance,
                ["monthly_savings_target"] = data.SavingsConfig?.MonthlySavingsTarget ?? 500m,
                ["snapshot_json"] = JsonSerializer.Deserialize<JsonElement>(snapshotJson),
                ["updated_at"] = DateTime.UtcNow.ToString("o")
            };

            await _sb.UpsertRowAsync("gastos_settings", settingRow);

            SetState(SyncState.Synced);
            return (true, "Datos sincronizados correctamente con Supabase.");
        }
        catch (Exception ex)
        {
            SetState(SyncState.Error, ex.Message);
            return (false, $"Error al subir a la nube: {ex.Message}");
        }
    }

    /// <summary>
    /// Descarga el estado guardado en la nube desde gastos_settings.
    /// </summary>
    public async Task<(bool Success, AppData? Data, string Message)> PullFromCloudAsync()
    {
        SetState(SyncState.Syncing);
        try
        {
            var rows = await _sb.GetAsync<JsonElement>("gastos_settings", "id=eq.current&select=snapshot_json,bank_account_balance,updated_at");
            if (rows.Count == 0)
            {
                SetState(SyncState.Synced);
                return (true, null, "No hay datos previos en la nube todavía.");
            }

            var first = rows[0];
            if (first.TryGetProperty("snapshot_json", out var snapProp) && snapProp.ValueKind == JsonValueKind.Object)
            {
                var loaded = JsonSerializer.Deserialize<AppData>(snapProp.GetRawText(), SupabaseClientService.JsonOptions);
                if (loaded != null)
                {
                    SetState(SyncState.Synced);
                    return (true, loaded, "Datos descargados con éxito de Supabase.");
                }
            }

            SetState(SyncState.Synced);
            return (true, null, "La tabla gastos_settings está lista pero vacía.");
        }
        catch (Exception ex)
        {
            SetState(SyncState.Error, ex.Message);
            return (false, null, $"Error al leer de la nube: {ex.Message}");
        }
    }

    /// <summary>
    /// Prueba si la tabla gastos_settings responde en el proyecto Supabase.
    /// </summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync()
    {
        try
        {
            var rows = await _sb.GetAsync<JsonElement>("gastos_settings", "select=id&limit=1");
            return (true, "Conexión exitosa con el proyecto Supabase.");
        }
        catch (Exception ex)
        {
            return (false, $"No se pudo conectar con la tabla gastos_settings: {ex.Message}");
        }
    }
}
