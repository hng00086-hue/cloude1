# SapConnection

Solución en C# (.NET 8) para conectarse por SQL directo a la base de datos de tu
SAP (SQL Server) y ejecutar consultas, tanto desde una app de consola como desde
una API HTTP.

La solución `SapConnection.sln` contiene dos proyectos:

- **SapConnection**: app de consola para ejecutar consultas ad-hoc desde la
  terminal.
- **SapConnection.Api**: API HTTP (ASP.NET Core minimal API) que expone la misma
  conexión a SAP para consumirla desde otras aplicaciones.

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Acceso de red al servidor SQL Server de SAP (por defecto `192.168.10.9:1433`)

## Compilar toda la solución

```bash
dotnet build SapConnection.sln
```

## App de consola (SapConnection)

### Configuración

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

### Uso

Ejecutar una consulta de prueba (versión de SQL Server, base actual y hora del servidor):

```bash
dotnet run
```

Ejecutar una consulta propia:

```bash
dotnet run -- "SELECT TOP 10 * FROM OCRD"
```

## API HTTP (SapConnection.Api)

### Configuración

1. Copia el archivo de ejemplo y complétalo con tus datos reales:

   ```bash
   cd SapConnection.Api
   cp config.example.json config.json
   ```

   El formato de `config.json` es el mismo que el de la app de consola.
   También está en `.gitignore` y nunca se sube al repositorio.

### Uso

```bash
cd SapConnection.Api
dotnet run
```

Por defecto la API escucha en `http://localhost:5080`. Endpoints disponibles:

- `GET /api/sap/health`: comprobación básica de que la API está viva (no
  consulta la base de datos).
- `GET /api/sap/test`: ejecuta una consulta de prueba (versión de SQL Server,
  base actual y hora del servidor).
- `POST /api/sap/query`: ejecuta una consulta `SELECT` propia.

  ```bash
  curl -X POST http://localhost:5080/api/sap/query \
    -H "Content-Type: application/json" \
    -d '{"query": "SELECT TOP 10 * FROM OCRD"}'
  ```

  Por seguridad, la API solo permite ejecutar sentencias `SELECT`.

## Notas de seguridad

- No compartas ni subas ningún `config.json` a control de versiones.
- El usuario configurado debería tener únicamente los permisos mínimos necesarios
  (lectura) sobre las tablas que necesitas consultar.
- La API no incluye autenticación por defecto: si la vas a exponer más allá de
  tu propia máquina, colócala detrás de un proxy/gateway con autenticación y
  restringe el acceso de red.
