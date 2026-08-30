/* ============================================================================
   Informe de auditoría de stock (Inventory Audit Report) - SAP Business One 10.1
   ----------------------------------------------------------------------------
   Reproduce los criterios de selección de la ventana estándar de SAP B1:
     - Fecha de contabilización (Desde/Hasta)
     - Artículos (Código Desde/Hasta, Grupo de artículos)
     - Almacenes (por defecto TODOS; opcionalmente se puede limitar a una lista)
     - Visualizar: "Por artículos" o "Resumir por cuentas"
     - Visualizar SÍ para artículos/cuentas sin transacciones
     - Ocultar artículos con cantidad acumulada igual a cero

   La tabla origen es OINM (Registro de transacciones de stock), que es la
   misma tabla que alimenta el informe nativo.

   NOTA: "Ocultar transacciones de serie/lote si el método de valoración del
   artículo actual no es válido" es un caso de borde muy específico del motor
   nativo de SAP y no se reproduce aquí.
   ============================================================================ */

DECLARE @FechaDesde        date = '2019-01-01';
DECLARE @FechaHasta         date = '2026-08-30';

DECLARE @CodigoArticuloDesde nvarchar(20) = NULL;   -- NULL = sin límite inferior
DECLARE @CodigoArticuloHasta nvarchar(20) = NULL;   -- NULL = sin límite superior
DECLARE @GrupoArticulo        int          = NULL;   -- NULL = 'Todos' (OITM.ItmsGrpCod)

DECLARE @VisualizarSinTransacciones bit = 1; -- 'Visualizar SI para artículos/cuentas sin transacciones'
DECLARE @OcultarSaldoCero           bit = 0; -- 'Ocultar artículos con cantidad acumulada equivalente a cero'
DECLARE @ResumirPorCuentas          bit = 0; -- 0 = Por artículos, 1 = Resumir por cuentas

-- Almacenes a incluir: dejar vacía = TODOS los almacenes (OWHS completo).
-- Para limitar a almacenes concretos, insertar sus códigos aquí, p. ej.:
--   INSERT INTO @Almacenes (WhsCode) VALUES (N'PRODUC-1'), (N'PRODUC-2');
DECLARE @Almacenes TABLE (WhsCode nvarchar(20) PRIMARY KEY);

/* ----------------------------------------------------------------------------
   1) "Por artículos" - vista COLAPSADA (una fila por artículo), igual a la
      grilla nativa de SAP B1: al abrir el informe, cada artículo aparece
      colapsado (flecha ▶) mostrando solo la cantidad y el valor acumulados
      a la fecha; las columnas de una transacción puntual (Fecha del
      sistema, Documento, Almacén, Cantidad, Costos, Valor trans., Nombre
      de usuario) van en blanco hasta que el usuario expande el artículo.
      "Cantidad acumulada"/"Valor acumulado" es el saldo TOTAL del artículo
      a @FechaHasta (histórico completo, no solo el rango Desde/Hasta),
      igual que en SAP; @FechaDesde solo decide qué artículos se listan
      cuando @VisualizarSinTransacciones = 0 (deben tener movimiento en
      el rango para aparecer).
   ---------------------------------------------------------------------------- */
IF @ResumirPorCuentas = 0
BEGIN
    ;WITH Saldos AS
    (
        SELECT
            T1.ItemCode,
            T1.ItemName,
            SUM(CASE WHEN T0.DocDate <= @FechaHasta THEN T0.InQty - T0.OutQty ELSE 0 END)   AS CantidadAcumulada,
            SUM(CASE WHEN T0.DocDate <= @FechaHasta THEN T0.TransValue        ELSE 0 END)   AS ValorAcumulado,
            MAX(CASE WHEN T0.DocDate BETWEEN @FechaDesde AND @FechaHasta THEN 1 ELSE 0 END) AS TuvoMovimientoEnPeriodo
        FROM OITM T1
        LEFT JOIN OINM T0
               ON T0.ItemCode = T1.ItemCode
              AND (NOT EXISTS (SELECT 1 FROM @Almacenes) OR T0.Warehouse IN (SELECT WhsCode FROM @Almacenes))
        WHERE (@CodigoArticuloDesde IS NULL OR T1.ItemCode >= @CodigoArticuloDesde)
          AND (@CodigoArticuloHasta IS NULL OR T1.ItemCode <= @CodigoArticuloHasta)
          AND (@GrupoArticulo IS NULL OR T1.ItmsGrpCod = @GrupoArticulo)
          AND T1.InvntItem = 'Y'
        GROUP BY T1.ItemCode, T1.ItemName
    )
    SELECT
        ROW_NUMBER() OVER (ORDER BY ItemCode)          AS [#],
        ItemCode                                       AS [Número de artículo],
        ItemName                                        AS Descripción,
        CAST(NULL AS datetime)                          AS [Fecha del sistema],
        CAST(NULL AS date)                              AS [Fecha de contabilización],
        CAST(NULL AS nvarchar(20))                      AS Documento,
        CAST(NULL AS nvarchar(20))                      AS Almacén,
        CAST(NULL AS decimal(19,6))                     AS Cantidad,
        CAST(NULL AS decimal(19,6))                     AS Costos,
        CAST(NULL AS decimal(19,6))                     AS [Valor trans.],
        CantidadAcumulada                               AS [Cantidad acumulada],
        ValorAcumulado                                  AS [Valor acumulado],
        CAST(NULL AS nvarchar(50))                      AS [Nombre de usuario]
    FROM Saldos
    WHERE (@VisualizarSinTransacciones = 1 OR TuvoMovimientoEnPeriodo = 1)
      AND (@OcultarSaldoCero = 0 OR CantidadAcumulada <> 0)
    ORDER BY ItemCode;
END

/* ----------------------------------------------------------------------------
   1b) Drill-down: detalle de transacciones de UN artículo (equivalente a
       expandir la flecha ▶ de una fila en la grilla anterior). Completar
       @ItemCodeDrillDown y ejecutar por separado.
   ---------------------------------------------------------------------------- */
/*
DECLARE @ItemCodeDrillDown nvarchar(20) = N'1.25-LUNIFRESA-2.0';

;WITH Movimientos AS
(
    SELECT
        T0.ItemCode,
        T1.ItemName                                                 AS Descripcion,
        T0.CreateDate                                               AS FechaDelSistema,
        T0.DocDate                                                  AS FechaContabilizacion,
        CASE T0.TransType
            WHEN 13 THEN N'Factura de deudores'
            WHEN 14 THEN N'Abono de deudores'
            WHEN 18 THEN N'Factura de acreedores'
            WHEN 19 THEN N'Abono de acreedores'
            WHEN 20 THEN N'Entrega'
            WHEN 21 THEN N'Devolución'
            WHEN 22 THEN N'Entrada de mercancías (GRPO)'
            WHEN 23 THEN N'Devolución de compra'
            WHEN 59 THEN N'Entrada de mercancías'
            WHEN 60 THEN N'Salida de mercancías'
            WHEN 67 THEN N'Traspaso de stock'
            WHEN 1250000001 THEN N'Saldo inicial de stock'
            WHEN 1470000113 THEN N'Recuento de inventario'
            ELSE CAST(T0.TransType AS nvarchar(20))
        END                                                          AS Documento,
        T0.Warehouse                                                 AS CodigoAlmacen,
        T2.WhsName                                                   AS NombreAlmacen,
        (T0.InQty - T0.OutQty)                                       AS Cantidad,
        T0.CalcPrice                                                 AS Costos,
        T0.TransValue                                                AS ValorTrans,
        T0.CreatedBy                                                 AS IdUsuario,
        SUM(T0.InQty - T0.OutQty) OVER (
            PARTITION BY T0.ItemCode
            ORDER BY T0.DocDate, T0.CreateDate, T0.TransNum
            ROWS UNBOUNDED PRECEDING
        )                                                             AS CantidadAcumulada,
        SUM(T0.TransValue) OVER (
            PARTITION BY T0.ItemCode
            ORDER BY T0.DocDate, T0.CreateDate, T0.TransNum
            ROWS UNBOUNDED PRECEDING
        )                                                             AS ValorAcumulado
    FROM OINM T0
    INNER JOIN OITM T1 ON T1.ItemCode = T0.ItemCode
    INNER JOIN OWHS T2 ON T2.WhsCode  = T0.Warehouse
    WHERE T0.ItemCode = @ItemCodeDrillDown
      AND T0.DocDate BETWEEN @FechaDesde AND @FechaHasta
      AND (NOT EXISTS (SELECT 1 FROM @Almacenes) OR T0.Warehouse IN (SELECT WhsCode FROM @Almacenes))
)
SELECT * FROM Movimientos
ORDER BY FechaContabilizacion, FechaDelSistema;
*/

/* ----------------------------------------------------------------------------
   2) "Resumir por cuentas": totales agrupados por la cuenta contable de
      inventario (G/L Account Determination > Inventario > General >
      Cuenta de inventario) asignada al grupo de artículos.
      Columna confirmada en OITB: BalInvntAc ("Balance Sheet Inventory
      Account"). Si tu empresa usa determinación de cuentas "por
      artículo" o "por almacén" en vez de "por grupo de artículos",
      cambia el JOIN de T3 (OITB) por OITM u OWHS respectivamente; esas
      tablas tienen una columna equivalente con el mismo nombre.
   ---------------------------------------------------------------------------- */
IF @ResumirPorCuentas = 1
BEGIN
    SELECT
        T3.BalInvntAc                                               AS CuentaContable,
        A0.AcctName                                                 AS NombreCuenta,
        SUM(T0.InQty)                                               AS TotalEntradas,
        SUM(T0.OutQty)                                              AS TotalSalidas,
        SUM(T0.InQty - T0.OutQty)                                   AS CantidadNeta,
        SUM(T0.TransValue)                                          AS ValorNeto
    FROM OINM T0
    INNER JOIN OITM T1 ON T1.ItemCode   = T0.ItemCode
    INNER JOIN OITB T3 ON T3.ItmsGrpCod = T1.ItmsGrpCod
    LEFT JOIN OACT A0  ON A0.AcctCode   = T3.BalInvntAc
    WHERE T0.DocDate BETWEEN @FechaDesde AND @FechaHasta
      AND (NOT EXISTS (SELECT 1 FROM @Almacenes) OR T0.Warehouse IN (SELECT WhsCode FROM @Almacenes))
      AND (@CodigoArticuloDesde IS NULL OR T0.ItemCode >= @CodigoArticuloDesde)
      AND (@CodigoArticuloHasta IS NULL OR T0.ItemCode <= @CodigoArticuloHasta)
      AND (@GrupoArticulo IS NULL OR T1.ItmsGrpCod = @GrupoArticulo)
    GROUP BY T3.BalInvntAc, A0.AcctName
    HAVING (@OcultarSaldoCero = 0 OR SUM(T0.InQty - T0.OutQty) <> 0)
    ORDER BY T3.BalInvntAc;
END
