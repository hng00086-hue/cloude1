using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using SapConnection;

const string defaultQuery = "SELECT @@VERSION AS SqlVersion, DB_NAME() AS CurrentDatabase, GETDATE() AS ServerTime";

// Datos crudos (artículo x almacén) para armar el pivote en memoria: todos los
// artículos de inventario y todos los almacenes, sin restricciones de rango.
const string inventoryByWarehouseQuery = @"
SELECT
    T1.ItemCode,
    T1.ItemName,
    T1.InvntryUom,
    T0.WhsCode,
    T0.OnHand
FROM OITM T1
INNER JOIN OITW T0 ON T0.ItemCode = T1.ItemCode
WHERE T1.InvntItem = 'Y'
ORDER BY T1.ItemCode, T0.WhsCode";

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

var runInventoryReport = args.Length > 0 && args[0].Equals("inventario", StringComparison.OrdinalIgnoreCase);

// Cualquier otro argumento se usa como consulta SQL; sin argumentos se ejecuta
// una consulta de prueba.
var query = runInventoryReport
    ? inventoryByWarehouseQuery
    : args.Length > 0 ? string.Join(' ', args) : defaultQuery;

try
{
    using var connection = new SqlConnection(connectionString);
    Console.WriteLine($"Conectando a {config.Server}:{config.Port} (base '{config.Database}')...");
    connection.Open();
    Console.WriteLine("Conexión establecida correctamente.");
    Console.WriteLine();

    using var command = new SqlCommand(query, connection);
    using var reader = command.ExecuteReader();

    if (runInventoryReport)
    {
        PrintInventoryByWarehouse(reader);
    }
    else
    {
        PrintResults(reader);
    }
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

// Arma el mismo pivote que "Informe de stocks por almacén" de SAP Business One:
// una fila por artículo, una columna por almacén con la cantidad en stock, y una
// columna de Total de almacén con la suma. Los almacenes sin stock para un
// artículo se muestran en blanco, igual que en SAP.
static void PrintInventoryByWarehouse(IDataReader reader)
{
    var items = new List<(string ItemCode, string ItemName, string Uom)>();
    var itemIndex = new Dictionary<string, int>();
    var warehouses = new List<string>();
    var warehouseSeen = new HashSet<string>();
    var stock = new Dictionary<(string ItemCode, string WhsCode), decimal>();

    while (reader.Read())
    {
        var itemCode = reader.GetString(0);
        var itemName = reader.IsDBNull(1) ? "" : reader.GetString(1);
        var uom = reader.IsDBNull(2) ? "" : reader.GetString(2);
        var whsCode = reader.GetString(3);
        var onHand = reader.IsDBNull(4) ? 0m : Convert.ToDecimal(reader.GetValue(4));

        if (!itemIndex.ContainsKey(itemCode))
        {
            itemIndex[itemCode] = items.Count;
            items.Add((itemCode, itemName, uom));
        }

        if (warehouseSeen.Add(whsCode))
        {
            warehouses.Add(whsCode);
        }

        stock[(itemCode, whsCode)] = onHand;
    }

    warehouses.Sort(StringComparer.Ordinal);

    var headers = new List<string> { "Código de artículo", "Descripción", "UM", "Total de almacén" };
    headers.AddRange(warehouses);

    var rows = new List<string[]>();
    foreach (var item in items)
    {
        var total = warehouses
            .Select(w => stock.TryGetValue((item.ItemCode, w), out var qty) ? qty : 0m)
            .Sum();

        // Un artículo sin existencias en ningún almacén no aporta al reporte.
        if (total == 0m)
        {
            continue;
        }

        var row = new List<string>
        {
            item.ItemCode,
            item.ItemName,
            item.Uom,
            FormatQty(total)
        };

        foreach (var whs in warehouses)
        {
            var qty = stock.TryGetValue((item.ItemCode, whs), out var value) ? value : 0m;
            row.Add(qty == 0m ? "" : FormatQty(qty));
        }

        rows.Add(row.ToArray());
    }

    var widths = headers
        .Select((h, i) => Math.Max(h.Length, rows.Count == 0 ? 0 : rows.Max(r => r[i].Length)))
        .ToArray();

    Console.WriteLine(string.Join(" | ", headers.Select((h, i) => h.PadRight(widths[i]))));
    Console.WriteLine(new string('-', widths.Sum(w => w + 3)));

    foreach (var row in rows)
    {
        Console.WriteLine(string.Join(" | ", row.Select((v, i) => v.PadRight(widths[i]))));
    }

    Console.WriteLine();
    Console.WriteLine($"({rows.Count} artículo(s), {warehouses.Count} almacén(es))");
}

static string FormatQty(decimal value) =>
    value.ToString("#,##0.####", CultureInfo.InvariantCulture);
