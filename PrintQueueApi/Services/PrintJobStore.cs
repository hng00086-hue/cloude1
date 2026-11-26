// Creado por mí — Equipo: Telpo TPS320
using Microsoft.Extensions.Options;
using PrintQueueApi.Models;

namespace PrintQueueApi.Services;

/// <summary>
/// Cola de impresión en memoria. Se pierde al reiniciar la API;
/// para producción conviene guardarla en SQL Server.
/// </summary>
public class PrintJobStore
{
    private readonly object _lock = new();
    private readonly List<PrintJob> _jobs = new();
    private readonly PrintQueueOptions _options;
    private readonly TimeProvider _time;

    public PrintJobStore(IOptions<PrintQueueOptions> options, TimeProvider time)
    {
        _options = options.Value;
        _time = time;
    }

    public PrintJob Enqueue(CreatePrintJobRequest request)
    {
        var job = new PrintJob
        {
            DeviceId = string.IsNullOrWhiteSpace(request.DeviceId) ? _options.DefaultDeviceId : request.DeviceId.Trim(),
            Copies = Math.Clamp(request.Copies, 1, 10),
            Lines = request.Lines,
            CreatedAt = _time.GetUtcNow()
        };
        lock (_lock) _jobs.Add(job);
        return job;
    }

    public PrintJob? Get(Guid id)
    {
        lock (_lock) return _jobs.FirstOrDefault(j => j.Id == id);
    }

    public IReadOnlyList<PrintJob> List(PrintJobStatus? status, string? deviceId)
    {
        lock (_lock)
        {
            ReleaseExpiredClaims();
            return _jobs
                .Where(j => status is null || j.Status == status)
                .Where(j => deviceId is null || string.Equals(j.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(j => j.CreatedAt)
                .ToList();
        }
    }

    /// <summary>
    /// Entrega al POS el ticket pendiente más antiguo y lo marca como Printing,
    /// para que no se imprima dos veces.
    /// </summary>
    public PrintJob? ClaimNext(string deviceId)
    {
        lock (_lock)
        {
            ReleaseExpiredClaims();
            var job = _jobs
                .Where(j => j.Status == PrintJobStatus.Pending
                            && string.Equals(j.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(j => j.CreatedAt)
                .FirstOrDefault();
            if (job is null) return null;

            job.Status = PrintJobStatus.Printing;
            job.Attempts++;
            job.ClaimedAt = _time.GetUtcNow();
            return job;
        }
    }

    public bool MarkDone(Guid id)
    {
        lock (_lock)
        {
            var job = _jobs.FirstOrDefault(j => j.Id == id);
            if (job is null || job.Status != PrintJobStatus.Printing) return false;
            job.Status = PrintJobStatus.Done;
            job.LastError = null;
            job.CompletedAt = _time.GetUtcNow();
            return true;
        }
    }

    /// <summary>Registra el error; si quedan intentos, el ticket vuelve a Pending.</summary>
    public bool MarkFailed(Guid id, string? error)
    {
        lock (_lock)
        {
            var job = _jobs.FirstOrDefault(j => j.Id == id);
            if (job is null || job.Status != PrintJobStatus.Printing) return false;
            job.LastError = string.IsNullOrWhiteSpace(error) ? "Error de impresión sin detalle" : error;
            RequeueOrFail(job);
            return true;
        }
    }

    public bool Cancel(Guid id)
    {
        lock (_lock)
        {
            var job = _jobs.FirstOrDefault(j => j.Id == id);
            if (job is null || job.Status != PrintJobStatus.Pending) return false;
            job.Status = PrintJobStatus.Cancelled;
            job.CompletedAt = _time.GetUtcNow();
            return true;
        }
    }

    private void ReleaseExpiredClaims()
    {
        var limit = _time.GetUtcNow().AddSeconds(-_options.ClaimTimeoutSeconds);
        foreach (var job in _jobs.Where(j => j.Status == PrintJobStatus.Printing && j.ClaimedAt < limit))
        {
            job.LastError = "El POS no confirmó la impresión a tiempo";
            RequeueOrFail(job);
        }
    }

    private void RequeueOrFail(PrintJob job)
    {
        if (job.Attempts >= _options.MaxAttempts)
        {
            job.Status = PrintJobStatus.Failed;
            job.CompletedAt = _time.GetUtcNow();
        }
        else
        {
            job.Status = PrintJobStatus.Pending;
            job.ClaimedAt = null;
        }
    }
}
