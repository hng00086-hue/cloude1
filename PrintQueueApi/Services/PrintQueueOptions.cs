// Creado por mí — Equipo: Telpo TPS320
namespace PrintQueueApi.Services;

public class PrintQueueOptions
{
    public const string SectionName = "PrintQueue";

    /// <summary>
    /// Clave que el POS debe enviar en el encabezado X-Device-Key.
    /// Si está vacía no se valida (solo recomendable en pruebas).
    /// </summary>
    public string DeviceApiKey { get; set; } = "";

    public string DefaultDeviceId { get; set; } = "TPS320-01";

    /// <summary>
    /// Si el POS toma un ticket y no confirma en este tiempo (se apagó, perdió red...),
    /// el ticket vuelve a quedar pendiente.
    /// </summary>
    public int ClaimTimeoutSeconds { get; set; } = 60;

    /// <summary>Intentos antes de dejar un ticket como Failed.</summary>
    public int MaxAttempts { get; set; } = 3;
}
