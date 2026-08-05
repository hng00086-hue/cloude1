using System.Text.Json;

namespace Produccion;

public class ProduccionConfig
{
    public string Server { get; set; } = "";
    public int Port { get; set; } = 1433;
    public string Database { get; set; } = "";
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
    public bool TrustServerCertificate { get; set; } = true;
    public int ConnectTimeoutSeconds { get; set; } = 10;

    public static ProduccionConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"No se encontró '{path}'. Copia 'config.example.json' a 'config.json' " +
                "y completa los datos de conexión a tu SAP.", path);
        }

        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<ProduccionConfig>(json, options)
               ?? throw new InvalidOperationException($"No se pudo interpretar '{path}'.");
    }
}
