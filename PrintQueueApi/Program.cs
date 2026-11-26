// Creado por mí — Equipo: Telpo TPS320
using System.Reflection;
using System.Text.Json.Serialization;
using PrintQueueApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<PrintQueueOptions>(builder.Configuration.GetSection(PrintQueueOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<PrintJobStore>();
builder.Services.AddScoped<DeviceKeyFilter>();

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Cola de impresión Telpo TPS320", Version = "v1" });
    var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xml)) c.IncludeXmlComments(xml);
});

var app = builder.Build();

// Swagger queda activo siempre para poder probar la impresión desde el navegador.
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();

public partial class Program { }
