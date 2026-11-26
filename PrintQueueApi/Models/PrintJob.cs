// Creado por mí — Equipo: Telpo TPS320
namespace PrintQueueApi.Models;

public enum PrintJobStatus
{
    Pending,
    Printing,
    Done,
    Failed,
    Cancelled
}

public enum TicketLineType
{
    /// <summary>Una línea de texto.</summary>
    Text,
    /// <summary>Una línea separadora (guiones a todo el ancho).</summary>
    Separator,
    /// <summary>Avanza papel N líneas (ver <see cref="TicketLine.Lines"/>).</summary>
    Feed
}

public enum TicketAlign
{
    Left,
    Center,
    Right
}

public enum TicketTextSize
{
    Normal,
    Large
}

public class TicketLine
{
    public TicketLineType Type { get; set; } = TicketLineType.Text;
    public string? Text { get; set; }
    public TicketAlign Align { get; set; } = TicketAlign.Left;
    public bool Bold { get; set; }
    public TicketTextSize Size { get; set; } = TicketTextSize.Normal;
    /// <summary>Solo para <see cref="TicketLineType.Feed"/>: cuántas líneas avanzar.</summary>
    public int Lines { get; set; } = 1;
}

public class PrintJob
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string DeviceId { get; init; } = "";
    public int Copies { get; init; } = 1;
    public List<TicketLine> Lines { get; init; } = new();
    public PrintJobStatus Status { get; set; } = PrintJobStatus.Pending;
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClaimedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public class CreatePrintJobRequest
{
    /// <summary>POS que debe imprimir. Si se omite se usa PrintQueue:DefaultDeviceId.</summary>
    public string? DeviceId { get; set; }
    public int Copies { get; set; } = 1;
    public List<TicketLine> Lines { get; set; } = new();
}

public class PrintFailureRequest
{
    public string? Error { get; set; }
}
