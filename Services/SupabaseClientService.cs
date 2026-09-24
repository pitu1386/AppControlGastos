using System.Net;
using System.Text;
using System.Text.Json;

namespace AppControlGastos.Services;

/// <summary>
/// Cliente PostgREST de Supabase para AppControlGastos.
/// Consume la API REST de Supabase directamente sobre HttpClient sin dependencias pesadas.
/// </summary>
public class SupabaseClientService
{
    private static readonly string RestUrl = $"{AppInfo.SupabaseUrl}/rest/v1";

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    private readonly HttpClient _http;
    private readonly SupabaseAuthService _auth;

    public SupabaseClientService(HttpClient http, SupabaseAuthService auth)
    {
        _http = http;
        _auth = auth;
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string url)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Add("apikey", AppInfo.SupabaseAnonKey);
        var token = await _auth.GetAccessTokenAsync();
        req.Headers.Add("Authorization", $"Bearer {token ?? AppInfo.SupabaseAnonKey}");
        return req;
    }

    private async Task<string> SendAsync(HttpMethod method, string url, object? body = null, string? prefer = null)
    {
        HttpResponseMessage resp;
        try
        {
            using var req = await CreateRequestAsync(method, url);
            if (prefer != null) req.Headers.Add("Prefer", prefer);
            if (body != null)
            {
                req.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
            }
            resp = await _http.SendAsync(req);
        }
        catch (Exception ex)
        {
            throw new Exception("Sin conexión con Supabase. Verifica tu red.", ex);
        }

        using (resp)
        {
            var text = await resp.Content.ReadAsStringAsync();
            if (resp.IsSuccessStatusCode) return text;
            throw new Exception($"Error de Supabase ({(int)resp.StatusCode}): {text}");
        }
    }

    public async Task<List<T>> GetAsync<T>(string table, string query = "select=*")
    {
        var json = await SendAsync(HttpMethod.Get, $"{RestUrl}/{table}?{query}");
        return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();
    }

    public async Task UpsertAsync<T>(string table, IEnumerable<T> rows)
    {
        var list = rows.ToList();
        if (list.Count == 0) return;
        await SendAsync(HttpMethod.Post, $"{RestUrl}/{table}", list, "resolution=merge-duplicates,return=minimal");
    }

    public Task UpsertRowAsync<T>(string table, T row) => UpsertAsync(table, new List<T> { row });

    public async Task DeleteAsync(string table, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) throw new ArgumentException("El filtro DELETE no puede estar vacío.", nameof(filter));
        await SendAsync(HttpMethod.Delete, $"{RestUrl}/{table}?{filter}", null, "return=minimal");
    }

    public Task DeleteByIdAsync(string table, string id) => DeleteAsync(table, $"id=eq.{Uri.EscapeDataString(id)}");
}
