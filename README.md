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
