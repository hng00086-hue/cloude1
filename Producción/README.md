# Producción

App de escritorio Windows Forms (C#, .NET 8) para conectarse por SQL directo a la
base de datos de tu SAP (SQL Server) y consultar información de producción
(órdenes de producción, tabla `OWOR` por defecto).

Requiere Windows y Visual Studio 2022 para compilar y ejecutar (WinForms
depende del SDK de escritorio de Windows).

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download) con el workload de
  desarrollo de escritorio (.NET Desktop Development)
- Visual Studio 2022 (17.x) — abrir `Producción.sln` en la raíz del repo
- Acceso de red al servidor SQL Server de SAP

## Configuración

1. Copia el archivo de ejemplo y complétalo con tus datos reales:

   ```bash
   cd Producción
   cp config.example.json config.json
   ```

2. Edita `config.json` con tu servidor, base de datos, usuario y contraseña
   (mismo formato que usa `SapConnection`).

   `config.json` está en `.gitignore` y nunca se sube al repositorio.

## Uso

Abre `Producción.sln` en Visual Studio 2022 y ejecuta el proyecto. La ventana
principal permite editar la consulta SQL y pulsar "Ejecutar" para ver los
resultados en una tabla.

## Notas de seguridad

- No compartas ni subas `config.json` a control de versiones.
- El usuario configurado debería tener únicamente los permisos mínimos
  necesarios (lectura) sobre las tablas que necesitas consultar.
