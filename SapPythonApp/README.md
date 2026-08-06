# SapPythonApp

Aplicación en Python que se conecta a **SAP Business One** a través del
**Service Layer** (API REST oficial de SAP B1 9.x+, vía HTTPS).

## Requisitos

- Python 3.9+
- Acceso de red desde el servidor al Service Layer de SAP B1
  (por defecto en el puerto `50000`, ej. `https://192.168.10.9:50000/b1s/v1`)

## Configuración local

1. Copia el archivo de ejemplo y complétalo con tus datos reales:

   ```bash
   cd SapPythonApp
   cp config.example.json config.json
   ```

2. Edita `config.json`:

   ```json
   {
     "ServiceLayerUrl": "https://192.168.10.9:50000/b1s/v1",
     "CompanyDB": "SBODEMOUS",
     "UserName": "manager",
     "Password": "TU_PASSWORD_AQUI",
     "VerifySSL": false,
     "TimeoutSeconds": 15
   }
   ```

   `config.json` está en `.gitignore` y nunca se sube al repositorio.

## Uso local

```bash
pip install -r requirements.txt
python main.py                                              # consulta de ejemplo
python main.py "Items?\$top=5&\$select=ItemCode,ItemName"   # consulta propia
```

## Despliegue manual en el servidor Ubuntu

Estos pasos se ejecutan tú mismo por tu propio SSH — este repo solo deja el
código y la configuración listos, no se conecta al servidor automáticamente.

1. Copia el proyecto al servidor (ej. `/opt/sap-python-app`):

   ```bash
   scp -P 56744 -r SapPythonApp/ itln@216.183.239.90:/opt/sap-python-app
   ```

2. En el servidor, crea el entorno virtual e instala dependencias:

   ```bash
   cd /opt/sap-python-app
   python3 -m venv venv
   ./venv/bin/pip install -r requirements.txt
   cp config.example.json config.json   # y edítalo con los datos reales
   ```

3. (Opcional) Instala el servicio systemd para que corra como daemon:

   ```bash
   sudo cp deploy/sap-python-app.service.example /etc/systemd/system/sap-python-app.service
   sudo systemctl daemon-reload
   sudo systemctl enable --now sap-python-app
   sudo systemctl status sap-python-app
   ```

## Notas de seguridad

- No compartas ni subas `config.json` a control de versiones.
- El usuario de SAP B1 configurado debería tener únicamente los permisos
  mínimos necesarios sobre los objetos que necesitas consultar.
- Si `VerifySSL` es `false` (certificado autofirmado, común en instalaciones
  locales de Service Layer), considera importar el certificado real cuando
  sea posible en vez de deshabilitar la verificación en producción.
