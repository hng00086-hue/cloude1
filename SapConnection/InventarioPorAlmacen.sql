-- Informe de stocks por almacén (equivalente al informe nativo de SAP Business One).
-- Para todos los artículos (con y sin manejo de inventario) y todos los almacenes,
-- sin restricciones de rango. Los artículos con InvntItem = 'N' no tienen registros
-- en OITW (SAP no lleva existencias por almacén para ellos), por eso se usa LEFT JOIN
-- y aparecen con las columnas de almacén en blanco.
-- Pegar y ejecutar directamente en SQL Server Management Studio (no en un editor
-- de "Query Generator" de SAP B1, que no admite DECLARE/EXEC).

DECLARE @cols NVARCHAR(MAX);
DECLARE @totalExpr NVARCHAR(MAX);
DECLARE @sql NVARCHAR(MAX);

SELECT @cols = STRING_AGG(QUOTENAME(WhsCode), ',') WITHIN GROUP (ORDER BY WhsCode)
FROM OWHS;

SELECT @totalExpr = STRING_AGG('ISNULL(' + QUOTENAME(WhsCode) + ',0)', '+') WITHIN GROUP (ORDER BY WhsCode)
FROM OWHS;

SET @sql = N'
SELECT
    ItemCode   AS [Código de artículo],
    ItemName   AS [Descripción],
    InvntryUom AS [UM],
    (' + @totalExpr + N') AS [Total de almacén],
    ' + @cols + N'
FROM (
    SELECT T1.ItemCode, T1.ItemName, T1.InvntryUom, T0.WhsCode, T0.OnHand
    FROM OITM T1
    LEFT JOIN OITW T0 ON T0.ItemCode = T1.ItemCode
) AS src
PIVOT (
    SUM(OnHand) FOR WhsCode IN (' + @cols + N')
) AS piv
ORDER BY ItemCode;';

EXEC sp_executesql @sql;
