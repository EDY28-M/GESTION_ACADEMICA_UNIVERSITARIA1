-- Script para agregar la columna fecha_creacion a la tabla Docente
-- Si la columna ya existe, este script no causará error

USE GestionAcademica;
GO

-- Verificar si la columna existe antes de agregarla
IF NOT EXISTS (
    SELECT * 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Docente' 
    AND COLUMN_NAME = 'fecha_creacion'
)
BEGIN
    ALTER TABLE Docente
    ADD fecha_creacion DATETIME NOT NULL DEFAULT GETDATE();
    
    PRINT 'Columna fecha_creacion agregada exitosamente a la tabla Docente';
END
ELSE
BEGIN
    PRINT 'La columna fecha_creacion ya existe en la tabla Docente';
END
GO
