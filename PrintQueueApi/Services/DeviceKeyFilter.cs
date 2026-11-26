// Creado por mí — Equipo: Telpo TPS320
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace PrintQueueApi.Services;

/// <summary>
/// Protege los endpoints que usa el POS: exige el encabezado X-Device-Key
/// cuando PrintQueue:DeviceApiKey está configurada.
/// </summary>
public class DeviceKeyFilter : IActionFilter
{
    public const string HeaderName = "X-Device-Key";
    private readonly PrintQueueOptions _options;

    public DeviceKeyFilter(IOptions<PrintQueueOptions> options) => _options = options.Value;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (string.IsNullOrEmpty(_options.DeviceApiKey)) return;

        var sent = context.HttpContext.Request.Headers[HeaderName].ToString();
        if (!System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(sent),
                System.Text.Encoding.UTF8.GetBytes(_options.DeviceApiKey)))
        {
            context.Result = new UnauthorizedObjectResult(new { error = $"Falta o es incorrecto el encabezado {HeaderName}" });
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
