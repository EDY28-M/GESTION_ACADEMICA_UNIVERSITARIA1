-- Script para agregar columna idTipoEvaluacion a TrabajoEncargado
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[TrabajoEncargado]') 
    AND name = 'idTipoEvaluacion'
)
BEGIN
    ALTER TABLE [dbo].[TrabajoEncargado]
    ADD [idTipoEvaluacion] INT NULL;

    -- Agregar foreign key constraint
    ALTER TABLE [dbo].[TrabajoEncargado]
    ADD CONSTRAINT [FK_TrabajoEncargado_TipoEvaluacion] 
    FOREIGN KEY ([idTipoEvaluacion]) 
    REFERENCES [dbo].[TipoEvaluacion] ([id]) 
    ON DELETE SET NULL;

    -- Crear índice para mejorar rendimiento
    CREATE NONCLUSTERED INDEX [IX_TrabajoEncargado_IdTipoEvaluacion] 
    ON [dbo].[TrabajoEncargado]([idTipoEvaluacion] ASC);

    PRINT 'Columna idTipoEvaluacion agregada correctamente a TrabajoEncargado';
END
ELSE
BEGIN
    PRINT 'La columna idTipoEvaluacion ya existe en TrabajoEncargado';
END
GO

