// Creado por mí — Equipo: Telpo TPS320
using Microsoft.AspNetCore.Mvc;
using PrintQueueApi.Models;
using PrintQueueApi.Services;

namespace PrintQueueApi.Controllers;

[ApiController]
[Route("api/print-jobs")]
[Produces("application/json")]
public class PrintJobsController : ControllerBase
{
    private readonly PrintJobStore _store;

    public PrintJobsController(PrintJobStore store) => _store = store;

    /// <summary>Encola un ticket para que lo imprima el POS.</summary>
    [HttpPost]
    [ProducesResponseType<PrintJob>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<PrintJob> Create([FromBody] CreatePrintJobRequest request)
    {
        if (request.Lines.Count == 0)
            return BadRequest(new { error = "El ticket debe tener al menos una línea" });

        var job = _store.Enqueue(request);
        return CreatedAtAction(nameof(Get), new { id = job.Id }, job);
    }

    /// <summary>Encola un ticket de prueba (útil desde Swagger).</summary>
    [HttpPost("test")]
    [ProducesResponseType<PrintJob>(StatusCodes.Status201Created)]
    public ActionResult<PrintJob> CreateTest([FromQuery] string? deviceId)
    {
        var now = DateTime.Now;
        var job = _store.Enqueue(new CreatePrintJobRequest
        {
            DeviceId = deviceId,
            Lines =
            {
                new() { Text = "PRUEBA DE IMPRESION", Align = TicketAlign.Center, Bold = true, Size = TicketTextSize.Large },
                new() { Text = "Telpo TPS320", Align = TicketAlign.Center },
                new() { Type = TicketLineType.Separator },
                new() { Text = $"Fecha: {now:dd/MM/yyyy HH:mm:ss}" },
                new() { Text = "Articulo 1          $ 10.00" },
                new() { Text = "Articulo 2          $ 25.50" },
                new() { Type = TicketLineType.Separator },
                new() { Text = "TOTAL  $ 35.50", Align = TicketAlign.Right, Bold = true, Size = TicketTextSize.Large },
                new() { Text = "Gracias por su compra", Align = TicketAlign.Center },
                new() { Type = TicketLineType.Feed, Lines = 4 }
            }
        });
        return CreatedAtAction(nameof(Get), new { id = job.Id }, job);
    }

    /// <summary>Lista los tickets, opcionalmente filtrados por estado y POS.</summary>
    [HttpGet]
    public ActionResult<IReadOnlyList<PrintJob>> List([FromQuery] PrintJobStatus? status, [FromQuery] string? deviceId)
        => Ok(_store.List(status, deviceId));

    /// <summary>Consulta el estado de un ticket.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<PrintJob>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<PrintJob> Get(Guid id)
        => _store.Get(id) is { } job ? Ok(job) : NotFound();

    /// <summary>Cancela un ticket que todavía no se ha impreso.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult Cancel(Guid id)
        => _store.Cancel(id) ? NoContent() : Conflict(new { error = "Solo se pueden cancelar tickets pendientes" });

    // ---------- Endpoints que usa la app del POS ----------

    /// <summary>(POS) Toma el siguiente ticket pendiente. 204 si no hay nada que imprimir.</summary>
    [HttpPost("claim")]
    [ServiceFilter<DeviceKeyFilter>]
    [ProducesResponseType<PrintJob>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult<PrintJob> Claim([FromQuery] string deviceId)
        => _store.ClaimNext(deviceId) is { } job ? Ok(job) : NoContent();

    /// <summary>(POS) Confirma que el ticket se imprimió.</summary>
    [HttpPost("{id:guid}/done")]
    [ServiceFilter<DeviceKeyFilter>]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult Done(Guid id)
        => _store.MarkDone(id) ? NoContent() : Conflict(new { error = "El ticket no está en impresión" });

    /// <summary>(POS) Informa un error (sin papel, tapa abierta...). Se reintenta hasta MaxAttempts.</summary>
    [HttpPost("{id:guid}/failed")]
    [ServiceFilter<DeviceKeyFilter>]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult Failed(Guid id, [FromBody] PrintFailureRequest body)
        => _store.MarkFailed(id, body.Error) ? NoContent() : Conflict(new { error = "El ticket no está en impresión" });
}
