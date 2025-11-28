-- Agregar columna isAutorizado a la tabla Matricula
-- Esta columna permite identificar matrículas autorizadas por el administrador
-- que no siguen las restricciones normales de ciclo

USE GestionAcademica;
GO

-- Verificar si la columna ya existe
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'Matricula') 
    AND name = 'isAutorizado'
)
BEGIN
    ALTER TABLE Matricula
    ADD isAutorizado BIT NOT NULL DEFAULT 0;
    
    PRINT 'Columna isAutorizado agregada exitosamente a la tabla Matricula';
END
ELSE
BEGIN
    PRINT 'La columna isAutorizado ya existe en la tabla Matricula';
END
GO

-- Actualizar todas las matrículas existentes como NO autorizadas
UPDATE Matricula 
SET isAutorizado = 0 
WHERE isAutorizado IS NULL;
GO

PRINT 'Matrículas existentes actualizadas como no autorizadas';
GO
