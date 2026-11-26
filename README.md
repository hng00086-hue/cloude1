# SapConnection

App de consola en C# (.NET 8) para conectarse por SQL directo a la base de datos
de tu SAP (SQL Server) y ejecutar consultas.

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Acceso de red al servidor SQL Server de SAP (por defecto `192.168.10.9:1433`)

## Configuración

1. Copia el archivo de ejemplo y complétalo con tus datos reales:

   ```bash
   cd SapConnection
   cp config.example.json config.json
   ```

2. Edita `config.json` con tu servidor, base de datos, usuario y contraseña:

   ```json
   {
     "Server": "192.168.10.9",
     "Port": 1433,
     "Database": "SBODEMOUS",
     "User": "sa",
     "Password": "TU_PASSWORD_AQUI",
     "TrustServerCertificate": true,
     "ConnectTimeoutSeconds": 10
   }
   ```

   `config.json` está en `.gitignore` y nunca se sube al repositorio.

## Uso

Ejecutar una consulta de prueba (versión de SQL Server, base actual y hora del servidor):

```bash
dotnet run
```

Ejecutar una consulta propia:

```bash
dotnet run -- "SELECT TOP 10 * FROM OCRD"
```

## Notas de seguridad

- No compartas ni subas `config.json` a control de versiones.
- El usuario configurado debería tener únicamente los permisos mínimos necesarios
  (lectura) sobre las tablas que necesitas consultar.

---

# Impresión en el POS Telpo TPS320

<!-- Creado por mí — Equipo: Telpo TPS320 -->

La impresora térmica del Telpo TPS320 (Android 8.1) no se instala en Windows: solo se
controla desde una app Android con el SDK de Telpo. Por eso son dos piezas:

```
API .NET 8 (PrintQueueApi)  ◄── el POS pregunta cada 3 s ──  App Android (TelpoPrintBridge)
  POST /api/print-jobs                                         imprime con el SDK de Telpo
                             ── le entrega el ticket ──►       y confirma done / failed
```

## 1. PrintQueueApi (.NET 8 + Swagger)

```bash
cd PrintQueueApi
dotnet run
```

Abre `http://localhost:5080/swagger`. La API escucha en `0.0.0.0:5080` para que el POS
la alcance por la red: abre el puerto 5080 en el firewall de Windows.

| Endpoint | Uso |
|---|---|
| `POST /api/print-jobs` | Tu sistema encola un ticket (líneas de texto, separadores y avance de papel) |
| `POST /api/print-jobs/test` | Encola un ticket de prueba |
| `GET /api/print-jobs` · `GET /api/print-jobs/{id}` | Ver la cola o el estado de un ticket |
| `DELETE /api/print-jobs/{id}` | Cancelar un ticket pendiente |
| `POST /api/print-jobs/claim?deviceId=` | (POS) Tomar el siguiente ticket; 204 si no hay |
| `POST /api/print-jobs/{id}/done` · `/failed` | (POS) Confirmar o informar un error |

Configuración en `appsettings.json` → `PrintQueue`:

- `DeviceApiKey`: clave que el POS manda en `X-Device-Key`. Vacía = sin validar (solo para pruebas).
  Mejor pásala por variable de entorno: `PrintQueue__DeviceApiKey=...`
- `ClaimTimeoutSeconds`: si el POS toma un ticket y no confirma, vuelve a la cola.
- `MaxAttempts`: intentos antes de marcarlo `Failed`.

> La cola está en memoria: se borra al reiniciar la API. Para producción conviene guardarla en SQL Server.

## 2. TelpoPrintBridge (app Android para el POS)

1. Consigue el SDK de Telpo para el TPS320 (con Telpo o tu distribuidor) y copia el `.jar`/`.aar`
   en `TelpoPrintBridge/app/libs/`.
2. Abre la carpeta `TelpoPrintBridge` en Android Studio y compila el APK.
3. Instálalo en el POS (por USB con `adb install`, o copiando el APK).
4. En la app escribe la URL de la API (IP de la PC, ej. `http://192.168.10.20:5080`), el ID del POS
   (`TPS320-01`) y la clave; toca **Iniciar**.
5. Usa **Imprimir prueba local** para comprobar la impresora sin la API, y luego
   `POST /api/print-jobs/test` desde Swagger.

La app usa `com.telpo.tps550.api.printer.UsbThermalPrinter`. Si tu versión del SDK tiene otros
nombres de métodos, ajústalos en `TelpoTicketPrinter.java` usando la demo que trae el SDK.
