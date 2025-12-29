-- ============================================
-- SCRIPT DE CREACIÓN DE TABLAS PARA TRABAJOS ENCARGADOS
-- Sistema de Gestión Académica
-- ============================================

USE GestionAcademica;
GO

-- ============================================
-- TABLA: TrabajoEncargado
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TrabajoEncargado]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[TrabajoEncargado] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [idCurso] INT NOT NULL,
        [idDocente] INT NOT NULL,
        [titulo] NVARCHAR(200) NOT NULL,
        [descripcion] NVARCHAR(MAX) NULL,
        [fechaCreacion] DATETIME NOT NULL DEFAULT GETDATE(),
        [fechaLimite] DATETIME NOT NULL,
        [activo] BIT NOT NULL DEFAULT 1,
        [fechaActualizacion] DATETIME NULL,
        CONSTRAINT [PK_TrabajoEncargado] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [FK_TrabajoEncargado_Curso] FOREIGN KEY ([idCurso]) 
            REFERENCES [dbo].[Curso] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TrabajoEncargado_Docente] FOREIGN KEY ([idDocente]) 
            REFERENCES [dbo].[Docente] ([id]) ON DELETE NO ACTION
    );

    CREATE NONCLUSTERED INDEX [IX_TrabajoEncargado_IdCurso] ON [dbo].[TrabajoEncargado] ([idCurso]);
    CREATE NONCLUSTERED INDEX [IX_TrabajoEncargado_IdDocente] ON [dbo].[TrabajoEncargado] ([idDocente]);
    CREATE NONCLUSTERED INDEX [IX_TrabajoEncargado_FechaLimite] ON [dbo].[TrabajoEncargado] ([fechaLimite]);
    
    PRINT 'Tabla TrabajoEncargado creada exitosamente';
END
ELSE
BEGIN
    PRINT 'La tabla TrabajoEncargado ya existe';
END
GO

-- ============================================
-- TABLA: TrabajoArchivo
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TrabajoArchivo]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[TrabajoArchivo] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [idTrabajo] INT NOT NULL,
        [nombreArchivo] NVARCHAR(500) NOT NULL,
        [rutaArchivo] NVARCHAR(1000) NOT NULL,
        [tipoArchivo] NVARCHAR(50) NULL,
        [tamaño] BIGINT NULL,
        [fechaSubida] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_TrabajoArchivo] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [FK_TrabajoArchivo_TrabajoEncargado] FOREIGN KEY ([idTrabajo]) 
            REFERENCES [dbo].[TrabajoEncargado] ([id]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_TrabajoArchivo_IdTrabajo] ON [dbo].[TrabajoArchivo] ([idTrabajo]);
    
    PRINT 'Tabla TrabajoArchivo creada exitosamente';
END
ELSE
BEGIN
    PRINT 'La tabla TrabajoArchivo ya existe';
END
GO

-- ============================================
-- TABLA: TrabajoLink
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TrabajoLink]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[TrabajoLink] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [idTrabajo] INT NOT NULL,
        [url] NVARCHAR(500) NOT NULL,
        [descripcion] NVARCHAR(200) NULL,
        [fechaCreacion] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_TrabajoLink] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [FK_TrabajoLink_TrabajoEncargado] FOREIGN KEY ([idTrabajo]) 
            REFERENCES [dbo].[TrabajoEncargado] ([id]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_TrabajoLink_IdTrabajo] ON [dbo].[TrabajoLink] ([idTrabajo]);
    
    PRINT 'Tabla TrabajoLink creada exitosamente';
END
ELSE
BEGIN
    PRINT 'La tabla TrabajoLink ya existe';
END
GO

-- ============================================
-- TABLA: TrabajoEntrega
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TrabajoEntrega]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[TrabajoEntrega] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [idTrabajo] INT NOT NULL,
        [idEstudiante] INT NOT NULL,
        [comentario] NVARCHAR(MAX) NULL,
        [fechaEntrega] DATETIME NOT NULL DEFAULT GETDATE(),
        [calificacion] DECIMAL(5,2) NULL,
        [observaciones] NVARCHAR(MAX) NULL,
        [fechaCalificacion] DATETIME NULL,
        [entregadoTarde] BIT NOT NULL DEFAULT 0,
        CONSTRAINT [PK_TrabajoEntrega] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [FK_TrabajoEntrega_TrabajoEncargado] FOREIGN KEY ([idTrabajo]) 
            REFERENCES [dbo].[TrabajoEncargado] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TrabajoEntrega_Estudiante] FOREIGN KEY ([idEstudiante]) 
            REFERENCES [dbo].[Estudiante] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [UQ_TrabajoEntrega_Trabajo_Estudiante] UNIQUE ([idTrabajo], [idEstudiante])
    );

    CREATE NONCLUSTERED INDEX [IX_TrabajoEntrega_IdTrabajo] ON [dbo].[TrabajoEntrega] ([idTrabajo]);
    CREATE NONCLUSTERED INDEX [IX_TrabajoEntrega_IdEstudiante] ON [dbo].[TrabajoEntrega] ([idEstudiante]);
    
    PRINT 'Tabla TrabajoEntrega creada exitosamente';
END
ELSE
BEGIN
    PRINT 'La tabla TrabajoEntrega ya existe';
END
GO

-- ============================================
-- TABLA: TrabajoEntregaArchivo
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TrabajoEntregaArchivo]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[TrabajoEntregaArchivo] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [idEntrega] INT NOT NULL,
        [nombreArchivo] NVARCHAR(500) NOT NULL,
        [rutaArchivo] NVARCHAR(1000) NOT NULL,
        [tipoArchivo] NVARCHAR(50) NULL,
        [tamaño] BIGINT NULL,
        [fechaSubida] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_TrabajoEntregaArchivo] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [FK_TrabajoEntregaArchivo_TrabajoEntrega] FOREIGN KEY ([idEntrega]) 
            REFERENCES [dbo].[TrabajoEntrega] ([id]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_TrabajoEntregaArchivo_IdEntrega] ON [dbo].[TrabajoEntregaArchivo] ([idEntrega]);
    
    PRINT 'Tabla TrabajoEntregaArchivo creada exitosamente';
END
ELSE
BEGIN
    PRINT 'La tabla TrabajoEntregaArchivo ya existe';
END
GO

-- ============================================
-- TABLA: TrabajoEntregaLink
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TrabajoEntregaLink]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[TrabajoEntregaLink] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [idEntrega] INT NOT NULL,
        [url] NVARCHAR(500) NOT NULL,
        [descripcion] NVARCHAR(200) NULL,
        [fechaCreacion] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_TrabajoEntregaLink] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [FK_TrabajoEntregaLink_TrabajoEntrega] FOREIGN KEY ([idEntrega]) 
            REFERENCES [dbo].[TrabajoEntrega] ([id]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_TrabajoEntregaLink_IdEntrega] ON [dbo].[TrabajoEntregaLink] ([idEntrega]);
    
    PRINT 'Tabla TrabajoEntregaLink creada exitosamente';
END
ELSE
BEGIN
    PRINT 'La tabla TrabajoEntregaLink ya existe';
END
GO

-- ============================================
-- VERIFICACIÓN FINAL
-- ============================================
PRINT '';
PRINT '============================================';
PRINT 'VERIFICACIÓN DE TABLAS CREADAS';
PRINT '============================================';

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TrabajoEncargado]') AND type in (N'U'))
    PRINT '✓ TrabajoEncargado';
ELSE
    PRINT '✗ TrabajoEncargado - ERROR';

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TrabajoArchivo]') AND type in (N'U'))
    PRINT '✓ TrabajoArchivo';
ELSE
    PRINT '✗ TrabajoArchivo - ERROR';

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TrabajoLink]') AND type in (N'U'))
    PRINT '✓ TrabajoLink';
ELSE
    PRINT '✗ TrabajoLink - ERROR';

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TrabajoEntrega]') AND type in (N'U'))
    PRINT '✓ TrabajoEntrega';
ELSE
    PRINT '✗ TrabajoEntrega - ERROR';

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TrabajoEntregaArchivo]') AND type in (N'U'))
    PRINT '✓ TrabajoEntregaArchivo';
ELSE
    PRINT '✗ TrabajoEntregaArchivo - ERROR';

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TrabajoEntregaLink]') AND type in (N'U'))
    PRINT '✓ TrabajoEntregaLink';
ELSE
    PRINT '✗ TrabajoEntregaLink - ERROR';

PRINT '';
PRINT '============================================';
PRINT 'SCRIPT COMPLETADO';
PRINT '============================================';
GO

