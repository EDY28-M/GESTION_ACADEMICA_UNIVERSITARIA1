-- Script para agregar campos de división de trabajos a TrabajoEncargado
-- Permite dividir un tipo de evaluación en múltiples trabajos

IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[TrabajoEncargado]') 
    AND name = 'numeroTrabajo'
)
BEGIN
    ALTER TABLE [dbo].[TrabajoEncargado]
    ADD [numeroTrabajo] INT NULL;

    PRINT 'Columna numeroTrabajo agregada correctamente';
END
ELSE
BEGIN
    PRINT 'Columna numeroTrabajo ya existe';
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[TrabajoEncargado]') 
    AND name = 'totalTrabajos'
)
BEGIN
    ALTER TABLE [dbo].[TrabajoEncargado]
    ADD [totalTrabajos] INT NULL;

    PRINT 'Columna totalTrabajos agregada correctamente';
END
ELSE
BEGIN
    PRINT 'Columna totalTrabajos ya existe';
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[TrabajoEncargado]') 
    AND name = 'pesoIndividual'
)
BEGIN
    ALTER TABLE [dbo].[TrabajoEncargado]
    ADD [pesoIndividual] DECIMAL(5,2) NULL;

    PRINT 'Columna pesoIndividual agregada correctamente';
END
ELSE
BEGIN
    PRINT 'Columna pesoIndividual ya existe';
END
GO

-- Verificar que la tabla existe antes de crear el índice
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'TrabajoEncargado' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    -- Verificar que las columnas existen antes de crear el índice
    IF EXISTS (
        SELECT * FROM sys.columns 
        WHERE object_id = OBJECT_ID(N'[dbo].[TrabajoEncargado]') 
        AND name = 'idTipoEvaluacion'
    ) AND EXISTS (
        SELECT * FROM sys.columns 
        WHERE object_id = OBJECT_ID(N'[dbo].[TrabajoEncargado]') 
        AND name = 'numeroTrabajo'
    )
    BEGIN
        -- Crear índices para mejorar el rendimiento de consultas
        IF NOT EXISTS (
            SELECT * FROM sys.indexes 
            WHERE name = 'IX_TrabajoEncargado_IdTipoEvaluacion_NumeroTrabajo' 
            AND object_id = OBJECT_ID(N'[dbo].[TrabajoEncargado]')
        )
        BEGIN
            BEGIN TRY
                CREATE NONCLUSTERED INDEX [IX_TrabajoEncargado_IdTipoEvaluacion_NumeroTrabajo] 
                ON [dbo].[TrabajoEncargado]([idTipoEvaluacion] ASC, [numeroTrabajo] ASC)
                WHERE [idTipoEvaluacion] IS NOT NULL AND [numeroTrabajo] IS NOT NULL;

                PRINT 'Índice IX_TrabajoEncargado_IdTipoEvaluacion_NumeroTrabajo creado correctamente';
            END TRY
            BEGIN CATCH
                PRINT 'Error al crear el índice: ' + ERROR_MESSAGE();
            END CATCH
        END
        ELSE
        BEGIN
            PRINT 'Índice IX_TrabajoEncargado_IdTipoEvaluacion_NumeroTrabajo ya existe';
        END
    END
    ELSE
    BEGIN
        PRINT 'Advertencia: Las columnas idTipoEvaluacion o numeroTrabajo no existen. El índice no se puede crear.';
    END
END
ELSE
BEGIN
    PRINT 'Advertencia: La tabla TrabajoEncargado no existe.';
END
GO

PRINT 'Script de migración completado exitosamente';
GO

