-- Crear tabla TipoEvaluacion
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TipoEvaluacion]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[TipoEvaluacion](
        [id] INT IDENTITY(1,1) NOT NULL,
        [id_curso] INT NOT NULL,
        [nombre] NVARCHAR(100) NOT NULL,
        [peso] DECIMAL(5, 2) NOT NULL,
        [orden] INT NOT NULL,
        [activo] BIT NOT NULL DEFAULT 1,
        [fecha_creacion] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_TipoEvaluacion] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [FK_TipoEvaluacion_Curso] FOREIGN KEY([id_curso]) 
            REFERENCES [dbo].[Curso] ([id]) ON DELETE CASCADE
    )
    
    CREATE NONCLUSTERED INDEX [IX_TipoEvaluacion_IdCurso] 
        ON [dbo].[TipoEvaluacion]([id_curso] ASC)
    
    CREATE NONCLUSTERED INDEX [IX_TipoEvaluacion_IdCurso_Orden] 
        ON [dbo].[TipoEvaluacion]([id_curso] ASC, [orden] ASC)
END
GO

PRINT 'Tabla TipoEvaluacion creada correctamente'
GO
