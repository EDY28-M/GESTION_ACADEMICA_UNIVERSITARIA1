-- Script para aumentar el tamaño de la columna tipoArchivo en TrabajoEntregaArchivo
-- Esto corrige el error: "String or binary data would be truncated"

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TrabajoEntregaArchivo]') AND name = 'tipoArchivo')
BEGIN
    ALTER TABLE [dbo].[TrabajoEntregaArchivo]
    ALTER COLUMN [tipoArchivo] NVARCHAR(100) NULL;
    PRINT 'Columna tipoArchivo actualizada a NVARCHAR(100) en TrabajoEntregaArchivo.';
END
ELSE
BEGIN
    PRINT 'La columna tipoArchivo no existe en TrabajoEntregaArchivo.';
END
GO

