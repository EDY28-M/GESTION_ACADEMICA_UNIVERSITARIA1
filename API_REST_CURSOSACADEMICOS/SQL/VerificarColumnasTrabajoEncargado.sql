-- Script para verificar que todas las columnas necesarias existen en TrabajoEncargado
-- Ejecutar este script para diagnosticar problemas

PRINT '=== Verificación de columnas en TrabajoEncargado ===';
PRINT '';

-- Verificar si la tabla existe
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'TrabajoEncargado' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    PRINT '✓ La tabla TrabajoEncargado existe';
    
    -- Verificar cada columna
    DECLARE @columnas TABLE (nombre VARCHAR(100), existe BIT);
    
    INSERT INTO @columnas VALUES ('idTipoEvaluacion', 0);
    INSERT INTO @columnas VALUES ('numeroTrabajo', 0);
    INSERT INTO @columnas VALUES ('totalTrabajos', 0);
    INSERT INTO @columnas VALUES ('pesoIndividual', 0);
    
    UPDATE @columnas 
    SET existe = 1
    WHERE nombre IN (
        SELECT name 
        FROM sys.columns 
        WHERE object_id = OBJECT_ID(N'[dbo].[TrabajoEncargado]')
    );
    
    -- Mostrar resultados
    DECLARE @nombre VARCHAR(100);
    DECLARE @existe BIT;
    
    DECLARE col_cursor CURSOR FOR SELECT nombre, existe FROM @columnas;
    OPEN col_cursor;
    FETCH NEXT FROM col_cursor INTO @nombre, @existe;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @existe = 1
            PRINT '✓ Columna ' + @nombre + ' existe';
        ELSE
            PRINT '✗ Columna ' + @nombre + ' NO existe';
        
        FETCH NEXT FROM col_cursor INTO @nombre, @existe;
    END;
    
    CLOSE col_cursor;
    DEALLOCATE col_cursor;
    
    -- Verificar índices
    PRINT '';
    PRINT '=== Verificación de índices ===';
    
    IF EXISTS (
        SELECT * FROM sys.indexes 
        WHERE name = 'IX_TrabajoEncargado_IdTipoEvaluacion_NumeroTrabajo' 
        AND object_id = OBJECT_ID(N'[dbo].[TrabajoEncargado]')
    )
    BEGIN
        PRINT '✓ Índice IX_TrabajoEncargado_IdTipoEvaluacion_NumeroTrabajo existe';
    END
    ELSE
    BEGIN
        PRINT '✗ Índice IX_TrabajoEncargado_IdTipoEvaluacion_NumeroTrabajo NO existe';
    END
END
ELSE
BEGIN
    PRINT '✗ La tabla TrabajoEncargado NO existe';
END

PRINT '';
PRINT '=== Verificación completada ===';
GO

