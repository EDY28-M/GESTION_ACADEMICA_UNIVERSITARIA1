-- Script para agregar la columna promedio_final a la tabla Matricula
-- Ejecutar en SQL Server Management Studio o Azure Data Studio

USE GestionAcademica;
GO

-- Verificar si la columna ya existe
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('Matricula') 
    AND name = 'promedio_final'
)
BEGIN
    ALTER TABLE Matricula
    ADD promedio_final DECIMAL(5,2) NULL;
    
    PRINT 'Columna promedio_final agregada exitosamente a la tabla Matricula';
END
ELSE
BEGIN
    PRINT 'La columna promedio_final ya existe en la tabla Matricula';
END
GO

-- Verificar la estructura de la tabla
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Matricula'
ORDER BY ORDINAL_POSITION;
GO
