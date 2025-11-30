-- =============================================
-- Script: Corregir tipo de dato de columna 'peso' en tabla Nota
-- Problema: decimal(3,2) no puede almacenar valores como 10, 15, 20
-- Solución: Cambiar a decimal(5,2) para permitir valores hasta 999.99
-- =============================================

USE [GestionAcademica]
GO

-- Verificar si la columna existe y su tipo actual
IF EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'[dbo].[Nota]') 
           AND name = 'peso')
BEGIN
    PRINT 'Modificando columna peso de decimal(3,2) a decimal(5,2)...'
    
    -- Cambiar el tipo de dato de la columna
    ALTER TABLE [dbo].[Nota]
    ALTER COLUMN [peso] DECIMAL(5,2) NULL
    
    PRINT 'Columna peso modificada exitosamente a decimal(5,2)'
END
ELSE
BEGIN
    PRINT 'La columna peso no existe en la tabla Nota'
END
GO

-- Verificar el cambio
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.precision AS Precision,
    c.scale AS Scale
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID(N'[dbo].[Nota]')
AND c.name = 'peso'
GO

PRINT 'Script completado. La columna peso ahora puede almacenar valores de 0 a 999.99'
GO
