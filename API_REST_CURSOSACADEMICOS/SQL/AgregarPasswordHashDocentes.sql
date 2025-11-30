-- Script para agregar la columna password_hash a la tabla Docente
-- Si la columna ya existe, este script no causará error

USE GestionAcademica;
GO

-- Verificar si la columna existe antes de agregarla
IF NOT EXISTS (
    SELECT * 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Docente' 
    AND COLUMN_NAME = 'password_hash'
)
BEGIN
    ALTER TABLE Docente
    ADD password_hash VARCHAR(255) NULL;
    
    PRINT 'Columna password_hash agregada exitosamente a la tabla Docente';
END
ELSE
BEGIN
    PRINT 'La columna password_hash ya existe en la tabla Docente';
END
GO
