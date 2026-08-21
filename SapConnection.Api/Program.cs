using Microsoft.Data.SqlClient;
using SapConnection;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var configPath = Path.Combine(AppContext.BaseDirectory, "config.json");

app.MapGet("/api/sap/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/sap/test", () => RunQuery(
    "SELECT @@VERSION AS SqlVersion, DB_NAME() AS CurrentDatabase, GETDATE() AS ServerTime"));

app.MapPost("/api/sap/query", (QueryRequest request) =>
{
    var query = request.Query?.Trim();
    if (string.IsNullOrWhiteSpace(query))
    {
        return Results.BadRequest(new { error = "El campo 'query' es obligatorio." });
    }

    if (!query.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = "Solo se permiten consultas SELECT a través de la API." });
    }

    return RunQuery(query);
});

app.Run();

IResult RunQuery(string query)
{
    SapConnectionConfig config;
    try
    {
        config = SapConnectionConfig.Load(configPath);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error de configuración: {ex.Message}", statusCode: 500);
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

    try
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = new SqlCommand(query, connection);
        using var reader = command.ExecuteReader();

        var columnNames = Enumerable.Range(0, reader.FieldCount)
            .Select(reader.GetName)
            .ToArray();

        var rows = new List<Dictionary<string, object?>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>();
            foreach (var name in columnNames)
            {
                var value = reader[name];
                row[name] = value is DBNull ? null : value;
            }
            rows.Add(row);
        }

        return Results.Ok(new { columns = columnNames, rowCount = rows.Count, rows });
    }
    catch (SqlException ex)
    {
        return Results.Problem($"Error al conectar o ejecutar la consulta: {ex.Message}", statusCode: 502);
    }
}

record QueryRequest(string? Query);
