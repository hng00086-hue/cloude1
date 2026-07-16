using System.Data;
using Microsoft.Data.SqlClient;
using SapConnection;

const string defaultQuery = "SELECT @@VERSION AS SqlVersion, DB_NAME() AS CurrentDatabase, GETDATE() AS ServerTime";

var configPath = Path.Combine(Directory.GetCurrentDirectory(), "config.json");

SapConnectionConfig config;
try
{
    config = SapConnectionConfig.Load(configPath);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error de configuración: {ex.Message}");
    return 1;
}

var connectionString = new SqlConnectionStringBuilder
{
    DataSource = $"{config.Server},{config.Port}",
    InitialCatalog = config.Database,
    UserID = config.User,
    Password = config.Password,
    TrustServerCertificate = config.TrustServerCertificate,
    ConnectTimeout = config.ConnectTimeoutSeconds
}.ConnectionString;

// Cualquier argumento pasado al programa se usa como consulta SQL;
// si no se pasa ninguno, se ejecuta una consulta de prueba.
var query = args.Length > 0 ? string.Join(' ', args) : defaultQuery;

try
{
    using var connection = new SqlConnection(connectionString);
    Console.WriteLine($"Conectando a {config.Server}:{config.Port} (base '{config.Database}')...");
    connection.Open();
    Console.WriteLine("Conexión establecida correctamente.");
    Console.WriteLine();

    using var command = new SqlCommand(query, connection);
    using var reader = command.ExecuteReader();
    PrintResults(reader);
}
catch (SqlException ex)
{
    Console.Error.WriteLine($"Error al conectar o ejecutar la consulta: {ex.Message}");
    return 1;
}

return 0;

static void PrintResults(IDataReader reader)
{
    var columnNames = Enumerable.Range(0, reader.FieldCount)
        .Select(reader.GetName)
        .ToArray();

    Console.WriteLine(string.Join(" | ", columnNames));
    Console.WriteLine(new string('-', columnNames.Sum(c => c.Length + 3)));

    var rowCount = 0;
    while (reader.Read())
    {
        var values = Enumerable.Range(0, reader.FieldCount)
            .Select(i => reader.IsDBNull(i) ? "NULL" : reader.GetValue(i)?.ToString() ?? "");
        Console.WriteLine(string.Join(" | ", values));
        rowCount++;
    }

    Console.WriteLine();
    Console.WriteLine($"({rowCount} fila(s))");
}
