/* ============================================================================
   Informe de auditoría de stock (Inventory Audit Report) - SAP Business One 10.1
   ----------------------------------------------------------------------------
   Reproduce los criterios de selección de la ventana estándar de SAP B1:
     - Fecha de contabilización (Desde/Hasta)
     - Artículos (Código Desde/Hasta, Grupo de artículos)
     - Almacenes (lista de almacenes seleccionados)
     - Visualizar: "Por artículos" o "Resumir por cuentas"
     - Visualizar SÍ para artículos/cuentas sin transacciones
     - Ocultar artículos con cantidad acumulada igual a cero

   La tabla origen es OINM (Registro de transacciones de stock), que es la
   misma tabla que alimenta el informe nativo. El saldo acumulado se calcula
   con una función de ventana (SUM ... OVER) sobre el histórico completo del
   artículo (no solo del rango de fechas), tal como hace el informe estándar.

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

-- Almacenes marcados en la captura de pantalla
DECLARE @Almacenes TABLE (WhsCode nvarchar(20) PRIMARY KEY);
INSERT INTO @Almacenes (WhsCode) VALUES
    (N'PRODUC-1'), (N'PRODUC-2'), (N'PRODUC-3'),
    (N'RETENIDO'), (N'SOPLADO'), (N'SOP-RECA');

/* ----------------------------------------------------------------------------
   1) Detalle "Por artículos": movimientos + saldo acumulado por artículo
   ---------------------------------------------------------------------------- */
IF @ResumirPorCuentas = 0
BEGIN
    ;WITH Movimientos AS
    (
        SELECT
            T0.ItemCode,
            T1.ItemName                                                AS Descripcion,
            T0.Warehouse                                                AS CodigoAlmacen,
            T2.WhsName                                                  AS NombreAlmacen,
            T0.TransType,
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
            END                                                          AS TipoTransaccion,
            T0.DocDate                                                  AS FechaContabilizacion,
            T0.CreateDate                                               AS FechaCreacion,
            T0.TransNum,
            T0.DocLine,
            T0.InQty                                                    AS CantidadEntrada,
            T0.OutQty                                                   AS CantidadSalida,
            T0.CalcPrice                                                AS PrecioUnitario,
            T0.TransValue                                               AS ValorTransaccion,
            SUM(T0.InQty - T0.OutQty) OVER (
                PARTITION BY T0.ItemCode
                ORDER BY T0.DocDate, T0.CreateDate, T0.TransNum, T0.DocLine
                ROWS UNBOUNDED PRECEDING
            )                                                            AS CantidadAcumulada
        FROM OINM T0
        INNER JOIN OITM T1 ON T1.ItemCode = T0.ItemCode
        INNER JOIN OWHS T2 ON T2.WhsCode  = T0.Warehouse
        WHERE T0.DocDate BETWEEN @FechaDesde AND @FechaHasta
          AND T0.Warehouse IN (SELECT WhsCode FROM @Almacenes)
          AND (@CodigoArticuloDesde IS NULL OR T0.ItemCode >= @CodigoArticuloDesde)
          AND (@CodigoArticuloHasta IS NULL OR T0.ItemCode <= @CodigoArticuloHasta)
          AND (@GrupoArticulo IS NULL OR T1.ItmsGrpCod = @GrupoArticulo)
    )
    SELECT *
    FROM Movimientos
    WHERE (@OcultarSaldoCero = 0 OR CantidadAcumulada <> 0)
    ORDER BY ItemCode, FechaContabilizacion, FechaCreacion, TransNum, DocLine;

    -- Artículos del rango/grupo/almacén seleccionado sin ninguna transacción en el período
    IF @VisualizarSinTransacciones = 1
    BEGIN
        SELECT
            T1.ItemCode,
            T1.ItemName AS Descripcion,
            T2.WhsCode  AS CodigoAlmacen,
            T2.WhsName  AS NombreAlmacen,
            NULL        AS TipoTransaccion,
            NULL        AS FechaContabilizacion,
            NULL        AS FechaCreacion,
            0           AS CantidadEntrada,
            0           AS CantidadSalida,
            0           AS CantidadAcumulada
        FROM OITM T1
        CROSS JOIN @Almacenes AAA
        INNER JOIN OWHS T2 ON T2.WhsCode = AAA.WhsCode
        WHERE (@CodigoArticuloDesde IS NULL OR T1.ItemCode >= @CodigoArticuloDesde)
          AND (@CodigoArticuloHasta IS NULL OR T1.ItemCode <= @CodigoArticuloHasta)
          AND (@GrupoArticulo IS NULL OR T1.ItmsGrpCod = @GrupoArticulo)
          AND T1.InvntItem = 'Y'
          AND NOT EXISTS (
                SELECT 1 FROM OINM T0
                WHERE T0.ItemCode  = T1.ItemCode
                  AND T0.Warehouse = T2.WhsCode
                  AND T0.DocDate BETWEEN @FechaDesde AND @FechaHasta
          )
        ORDER BY T1.ItemCode, T2.WhsCode;
    END
END

/* ----------------------------------------------------------------------------
   2) "Resumir por cuentas": totales agrupados por la cuenta contable de
      inventario del grupo de artículos (aproximación al método de
      determinación de cuentas a nivel de grupo).
   ---------------------------------------------------------------------------- */
IF @ResumirPorCuentas = 1
BEGIN
    SELECT
        T3.StockAct                                                 AS CuentaContable,
        A0.AcctName                                                 AS NombreCuenta,
        SUM(T0.InQty)                                               AS TotalEntradas,
        SUM(T0.OutQty)                                              AS TotalSalidas,
        SUM(T0.InQty - T0.OutQty)                                   AS CantidadNeta,
        SUM(T0.TransValue)                                          AS ValorNeto
    FROM OINM T0
    INNER JOIN OITM T1 ON T1.ItemCode   = T0.ItemCode
    INNER JOIN OITB T3 ON T3.ItmsGrpCod = T1.ItmsGrpCod
    LEFT JOIN OACT A0  ON A0.AcctCode   = T3.StockAct
    WHERE T0.DocDate BETWEEN @FechaDesde AND @FechaHasta
      AND T0.Warehouse IN (SELECT WhsCode FROM @Almacenes)
      AND (@CodigoArticuloDesde IS NULL OR T0.ItemCode >= @CodigoArticuloDesde)
      AND (@CodigoArticuloHasta IS NULL OR T0.ItemCode <= @CodigoArticuloHasta)
      AND (@GrupoArticulo IS NULL OR T1.ItmsGrpCod = @GrupoArticulo)
    GROUP BY T3.StockAct, A0.AcctName
    HAVING (@OcultarSaldoCero = 0 OR SUM(T0.InQty - T0.OutQty) <> 0)
    ORDER BY T3.StockAct;
END
