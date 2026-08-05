using System.Data;
using Microsoft.Data.SqlClient;

namespace Produccion;

public partial class Form1 : Form
{
    private const string DefaultQuery =
        "SELECT TOP 100 DocNum, ItemCode, Quantity, DocStatus, PostDate " +
        "FROM OWOR ORDER BY DocEntry DESC";

    public Form1()
    {
        InitializeComponent();
        txtQuery.Text = DefaultQuery;
    }

    private void btnEjecutar_Click(object sender, EventArgs e)
    {
        var configPath = Path.Combine(Directory.GetCurrentDirectory(), "config.json");

        ProduccionConfig config;
        try
        {
            config = ProduccionConfig.Load(configPath);
        }
        catch (Exception ex)
        {
            MostrarError($"Error de configuración: {ex.Message}");
            return;
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

        btnEjecutar.Enabled = false;
        lblEstado.Text = $"Conectando a {config.Server}:{config.Port} (base '{config.Database}')...";
        Application.DoEvents();

        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            using var command = new SqlCommand(txtQuery.Text, connection);
            using var adapter = new SqlDataAdapter(command);
            var table = new DataTable();
            adapter.Fill(table);

            dgvResultados.DataSource = table;
            lblEstado.Text = $"Conexión establecida. {table.Rows.Count} fila(s) obtenida(s).";
        }
        catch (SqlException ex)
        {
            MostrarError($"Error al conectar o ejecutar la consulta: {ex.Message}");
        }
        finally
        {
            btnEjecutar.Enabled = true;
        }
    }

    private void MostrarError(string mensaje)
    {
        lblEstado.Text = mensaje;
        MessageBox.Show(mensaje, "Producción", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
