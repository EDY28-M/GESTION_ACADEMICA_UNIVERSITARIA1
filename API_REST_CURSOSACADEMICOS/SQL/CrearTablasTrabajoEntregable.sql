-- ============================================
-- SCRIPT PARA CREAR TABLAS DE TRABAJO ENTREGABLE
-- ============================================
-- Este script crea las tablas necesarias para soportar múltiples entregables por trabajo
-- Cada estudiante puede subir múltiples entregables (ej: 3 entregables) y cada uno se califica por separado

-- 1. Agregar columna totalEntregables a TrabajoEncargado
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TrabajoEncargado]') AND name = 'totalEntregables')
BEGIN
    ALTER TABLE [dbo].[TrabajoEncargado]
    ADD [totalEntregables] INT NULL;
    PRINT 'Columna totalEntregables agregada a TrabajoEncargado.';
END
GO

-- 2. Crear tabla TrabajoEntregable
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TrabajoEntregable]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[TrabajoEntregable](
        [id] [int] IDENTITY(1,1) NOT NULL,
        [idEntrega] [int] NOT NULL,
        [numeroEntregable] [int] NOT NULL,
        [comentario] [nvarchar](MAX) NULL,
        [fechaEntrega] [datetime] NOT NULL DEFAULT GETDATE(),
        [calificacion] [decimal](5,2) NULL,
        [observaciones] [nvarchar](MAX) NULL,
        [fechaCalificacion] [datetime] NULL,
        [entregadoTarde] [bit] NOT NULL DEFAULT 0,
        CONSTRAINT [PK_TrabajoEntregable] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [FK_TrabajoEntregable_TrabajoEntrega] FOREIGN KEY([idEntrega])
            REFERENCES [dbo].[TrabajoEntrega] ([id])
            ON DELETE CASCADE,
        CONSTRAINT [UQ_TrabajoEntregable_Entrega_Numero] UNIQUE([idEntrega], [numeroEntregable]),
        CONSTRAINT [CHK_TrabajoEntregable_Calificacion] CHECK ([calificacion] IS NULL OR ([calificacion] >= 0 AND [calificacion] <= 20))
    ) ON [PRIMARY];
    
    CREATE NONCLUSTERED INDEX [IX_TrabajoEntregable_IdEntrega] 
    ON [dbo].[TrabajoEntregable]([idEntrega] ASC);
    
    PRINT 'Tabla TrabajoEntregable creada correctamente.';
END
ELSE
BEGIN
    PRINT 'La tabla TrabajoEntregable ya existe.';
END
GO

-- 3. Crear tabla TrabajoEntregableArchivo
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TrabajoEntregableArchivo]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[TrabajoEntregableArchivo](
        [id] [int] IDENTITY(1,1) NOT NULL,
        [idEntregable] [int] NOT NULL,
        [nombreArchivo] [nvarchar](500) NOT NULL,
        [rutaArchivo] [nvarchar](1000) NOT NULL,
        [tipoArchivo] [nvarchar](50) NULL,
        [tamaño] [bigint] NULL,
        [fechaSubida] [datetime] NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_TrabajoEntregableArchivo] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [FK_TrabajoEntregableArchivo_TrabajoEntregable] FOREIGN KEY([idEntregable])
            REFERENCES [dbo].[TrabajoEntregable] ([id])
            ON DELETE CASCADE
    ) ON [PRIMARY];
    
    CREATE NONCLUSTERED INDEX [IX_TrabajoEntregableArchivo_IdEntregable] 
    ON [dbo].[TrabajoEntregableArchivo]([idEntregable] ASC);
    
    PRINT 'Tabla TrabajoEntregableArchivo creada correctamente.';
END
ELSE
BEGIN
    PRINT 'La tabla TrabajoEntregableArchivo ya existe.';
END
GO

-- 4. Crear tabla TrabajoEntregableLink
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TrabajoEntregableLink]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[TrabajoEntregableLink](
        [id] [int] IDENTITY(1,1) NOT NULL,
        [idEntregable] [int] NOT NULL,
        [url] [nvarchar](500) NOT NULL,
        [descripcion] [nvarchar](200) NULL,
        [fechaCreacion] [datetime] NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_TrabajoEntregableLink] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [FK_TrabajoEntregableLink_TrabajoEntregable] FOREIGN KEY([idEntregable])
            REFERENCES [dbo].[TrabajoEntregable] ([id])
            ON DELETE CASCADE
    ) ON [PRIMARY];
    
    CREATE NONCLUSTERED INDEX [IX_TrabajoEntregableLink_IdEntregable] 
    ON [dbo].[TrabajoEntregableLink]([idEntregable] ASC);
    
    PRINT 'Tabla TrabajoEntregableLink creada correctamente.';
END
ELSE
BEGIN
    PRINT 'La tabla TrabajoEntregableLink ya existe.';
END
GO

PRINT 'Script completado exitosamente.';

