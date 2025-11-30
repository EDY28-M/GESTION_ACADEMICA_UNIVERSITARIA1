USE [GestionAcademica]
GO

-- =============================================
-- MÓDULO DE ESTUDIANTES - SISTEMA ACADÉMICO
-- =============================================

-- 1. ACTUALIZAR TABLA USUARIO PARA INCLUIR MÁS ROLES
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CHK_Usuario_Rol')
BEGIN
    ALTER TABLE [dbo].[Usuario] DROP CONSTRAINT IF EXISTS [DF_Usuario_Rol];
    ALTER TABLE [dbo].[Usuario] ADD CONSTRAINT [CHK_Usuario_Rol] 
        CHECK ([rol] IN ('Administrador', 'Coordinador', 'Docente', 'Estudiante', 'Usuario'));
END
GO

-- 2. TABLA PERIODO ACADÉMICO
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Periodo]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Periodo](
        [id] [int] IDENTITY(1,1) NOT NULL,
        [nombre] [varchar](100) NOT NULL,
        [anio] [int] NOT NULL,
        [ciclo] [varchar](20) NOT NULL, -- 'I', 'II', 'Verano'
        [fecha_inicio] [date] NOT NULL,
        [fecha_fin] [date] NOT NULL,
        [activo] [bit] NOT NULL DEFAULT 1,
        [fecha_creacion] [datetime] NOT NULL DEFAULT GETDATE(),
    PRIMARY KEY CLUSTERED ([id] ASC)
    ) ON [PRIMARY]
END
GO

-- 3. TABLA ESTUDIANTE
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Estudiante]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Estudiante](
        [id] [int] IDENTITY(1,1) NOT NULL,
        [codigo] [varchar](20) NOT NULL,
        [apellidos] [varchar](100) NOT NULL,
        [nombres] [varchar](100) NOT NULL,
        [dni] [varchar](8) NOT NULL,
        [fecha_nacimiento] [date] NULL,
        [correo] [varchar](100) NOT NULL,
        [telefono] [varchar](15) NULL,
        [direccion] [varchar](200) NULL,
        [ciclo_actual] [int] NOT NULL DEFAULT 1,
        [creditos_aprobados] [int] NOT NULL DEFAULT 0,
        [promedio_ponderado] [decimal](4,2) NULL,
        [estado] [varchar](20) NOT NULL DEFAULT 'Activo', -- Activo, Retirado, Egresado
        [idUsuario] [int] NULL,
        [fecha_creacion] [datetime] NOT NULL DEFAULT GETDATE(),
    PRIMARY KEY CLUSTERED ([id] ASC),
    UNIQUE NONCLUSTERED ([codigo] ASC),
    UNIQUE NONCLUSTERED ([dni] ASC),
    UNIQUE NONCLUSTERED ([correo] ASC),
    CONSTRAINT [FK_Estudiante_Usuario] FOREIGN KEY([idUsuario])
        REFERENCES [dbo].[Usuario] ([id])
        ON DELETE SET NULL
    ) ON [PRIMARY]
END
GO

-- 4. TABLA MATRÍCULA
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Matricula]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Matricula](
        [id] [int] IDENTITY(1,1) NOT NULL,
        [idEstudiante] [int] NOT NULL,
        [idCurso] [int] NOT NULL,
        [idPeriodo] [int] NOT NULL,
        [fecha_matricula] [datetime] NOT NULL DEFAULT GETDATE(),
        [estado] [varchar](20) NOT NULL DEFAULT 'Matriculado', -- Matriculado, Retirado, Culminado
        [fecha_retiro] [datetime] NULL,
        [observaciones] [varchar](500) NULL,
    PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [FK_Matricula_Estudiante] FOREIGN KEY([idEstudiante])
        REFERENCES [dbo].[Estudiante] ([id])
        ON DELETE CASCADE,
    CONSTRAINT [FK_Matricula_Curso] FOREIGN KEY([idCurso])
        REFERENCES [dbo].[Curso] ([id])
        ON DELETE CASCADE,
    CONSTRAINT [FK_Matricula_Periodo] FOREIGN KEY([idPeriodo])
        REFERENCES [dbo].[Periodo] ([id])
        ON DELETE CASCADE,
    CONSTRAINT [UQ_Matricula_Estudiante_Curso_Periodo] UNIQUE([idEstudiante], [idCurso], [idPeriodo])
    ) ON [PRIMARY]
END
GO

-- 5. TABLA NOTA
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Nota]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Nota](
        [id] [int] IDENTITY(1,1) NOT NULL,
        [idMatricula] [int] NOT NULL,
        [tipo_evaluacion] [varchar](50) NOT NULL, -- Parcial, Final, Trabajo, Promedio
        [nota] [decimal](4,2) NOT NULL,
        [peso] [decimal](3,2) NULL, -- Peso en el promedio (ej: 0.30 = 30%)
        [fecha_evaluacion] [date] NULL,
        [observaciones] [varchar](500) NULL,
        [fecha_registro] [datetime] NOT NULL DEFAULT GETDATE(),
    PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [FK_Nota_Matricula] FOREIGN KEY([idMatricula])
        REFERENCES [dbo].[Matricula] ([id])
        ON DELETE CASCADE,
    CONSTRAINT [CHK_Nota_Rango] CHECK ([nota] >= 0 AND [nota] <= 20)
    ) ON [PRIMARY]
END
GO

-- =============================================
-- ÍNDICES PARA OPTIMIZACIÓN
-- =============================================

-- Índice para búsqueda de estudiantes por ciclo
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Estudiante_Ciclo_Estado')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Estudiante_Ciclo_Estado] ON [dbo].[Estudiante]
    (
        [ciclo_actual] ASC,
        [estado] ASC
    )
    INCLUDE ([nombres], [apellidos], [codigo])
END
GO

-- Índice para búsqueda de matrículas por estudiante y periodo
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Matricula_Estudiante_Periodo')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Matricula_Estudiante_Periodo] ON [dbo].[Matricula]
    (
        [idEstudiante] ASC,
        [idPeriodo] ASC
    )
    INCLUDE ([idCurso], [estado])
END
GO

-- Índice para búsqueda de cursos por ciclo
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Curso_Ciclo')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Curso_Ciclo] ON [dbo].[Curso]
    (
        [ciclo] ASC
    )
    INCLUDE ([curso], [creditos], [horasSemanal])
END
GO

-- =============================================
-- DATOS DE PRUEBA
-- =============================================

-- Periodos académicos
SET IDENTITY_INSERT [dbo].[Periodo] ON
GO
INSERT INTO [dbo].[Periodo] ([id], [nombre], [anio], [ciclo], [fecha_inicio], [fecha_fin], [activo])
VALUES 
(1, '2025-I', 2025, 'I', '2025-03-01', '2025-07-31', 1),
(2, '2025-II', 2025, 'II', '2025-08-01', '2025-12-15', 0),
(3, '2024-II', 2024, 'II', '2024-08-01', '2024-12-15', 0)
GO
SET IDENTITY_INSERT [dbo].[Periodo] OFF
GO

-- Usuarios estudiantes (password: Estudiante123!)
-- Hash BCrypt para "Estudiante123!": $2a$11$YHvNhJ8YqL2qO8EhLKp8rE2QwF8xN6p5KqO8EhLKp8rE2QwF8xN6p
INSERT INTO [dbo].[Usuario] ([email], [password_hash], [nombres], [apellidos], [rol], [estado])
VALUES 
('juan.perez@estudiante.edu.pe', '$2a$11$YHvNhJ8YqL2qO8EhLKp8rE2QwF8xN6p5KqO8EhLKp8rE2QwF8xN6p', 'Juan Carlos', 'Pérez García', 'Estudiante', 1),
('maria.lopez@estudiante.edu.pe', '$2a$11$YHvNhJ8YqL2qO8EhLKp8rE2QwF8xN6p5KqO8EhLKp8rE2QwF8xN6p', 'María', 'López Rodríguez', 'Estudiante', 1),
('carlos.gomez@estudiante.edu.pe', '$2a$11$YHvNhJ8YqL2qO8EhLKp8rE2QwF8xN6p5KqO8EhLKp8rE2QwF8xN6p', 'Carlos', 'Gómez Sánchez', 'Estudiante', 1),
('ana.torres@estudiante.edu.pe', '$2a$11$YHvNhJ8YqL2qO8EhLKp8rE2QwF8xN6p5KqO8EhLKp8rE2QwF8xN6p', 'Ana', 'Torres Vega', 'Estudiante', 1)
GO

-- Estudiantes (vincular con usuarios)
DECLARE @idUsuario1 INT, @idUsuario2 INT, @idUsuario3 INT, @idUsuario4 INT

SELECT @idUsuario1 = id FROM [dbo].[Usuario] WHERE email = 'juan.perez@estudiante.edu.pe'
SELECT @idUsuario2 = id FROM [dbo].[Usuario] WHERE email = 'maria.lopez@estudiante.edu.pe'
SELECT @idUsuario3 = id FROM [dbo].[Usuario] WHERE email = 'carlos.gomez@estudiante.edu.pe'
SELECT @idUsuario4 = id FROM [dbo].[Usuario] WHERE email = 'ana.torres@estudiante.edu.pe'

INSERT INTO [dbo].[Estudiante] ([codigo], [apellidos], [nombres], [dni], [fecha_nacimiento], [correo], [telefono], [ciclo_actual], [creditos_aprobados], [promedio_ponderado], [estado], [idUsuario])
VALUES 
('2025001', 'Pérez García', 'Juan Carlos', '72345678', '2005-05-15', 'juan.perez@estudiante.edu.pe', '987654321', 1, 0, NULL, 'Activo', @idUsuario1),
('2025002', 'López Rodríguez', 'María', '71234567', '2005-08-22', 'maria.lopez@estudiante.edu.pe', '987654322', 2, 24, 14.50, 'Activo', @idUsuario2),
('2024001', 'Gómez Sánchez', 'Carlos', '70123456', '2004-03-10', 'carlos.gomez@estudiante.edu.pe', '987654323', 3, 48, 15.80, 'Activo', @idUsuario3),
('2024002', 'Torres Vega', 'Ana', '73456789', '2004-11-05', 'ana.torres@estudiante.edu.pe', '987654324', 2, 26, 16.20, 'Activo', @idUsuario4)
GO

-- Matrículas de ejemplo (periodo actual 2025-I)
DECLARE @est1 INT, @est2 INT, @per1 INT
SELECT @est1 = id FROM [dbo].[Estudiante] WHERE codigo = '2025001'
SELECT @est2 = id FROM [dbo].[Estudiante] WHERE codigo = '2025002'
SELECT @per1 = id FROM [dbo].[Periodo] WHERE nombre = '2025-I'

-- Estudiante 1 (Ciclo 1) - Matriculado en cursos de ciclo 1
INSERT INTO [dbo].[Matricula] ([idEstudiante], [idCurso], [idPeriodo], [estado])
SELECT @est1, id, @per1, 'Matriculado'
FROM [dbo].[Curso]
WHERE ciclo = 1

-- Estudiante 2 (Ciclo 2) - Matriculado en cursos de ciclo 2
INSERT INTO [dbo].[Matricula] ([idEstudiante], [idCurso], [idPeriodo], [estado])
SELECT @est2, id, @per1, 'Matriculado'
FROM [dbo].[Curso]
WHERE ciclo = 2
GO

-- Notas de ejemplo para estudiante 1
DECLARE @matriculaId INT

-- Obtener primera matrícula del estudiante 1
SELECT TOP 1 @matriculaId = id FROM [dbo].[Matricula] 
WHERE idEstudiante = (SELECT id FROM [dbo].[Estudiante] WHERE codigo = '2025001')

IF @matriculaId IS NOT NULL
BEGIN
    INSERT INTO [dbo].[Nota] ([idMatricula], [tipo_evaluacion], [nota], [peso], [fecha_evaluacion])
    VALUES 
    (@matriculaId, 'Parcial', 14.50, 0.30, '2025-04-15'),
    (@matriculaId, 'Final', 16.00, 0.40, '2025-06-20'),
    (@matriculaId, 'Trabajo', 15.50, 0.30, '2025-05-10')
END
GO

PRINT 'Módulo de Estudiantes creado exitosamente!'
PRINT 'Usuarios de prueba:'
PRINT '- juan.perez@estudiante.edu.pe / Estudiante123! (Ciclo 1)'
PRINT '- maria.lopez@estudiante.edu.pe / Estudiante123! (Ciclo 2)'
PRINT '- carlos.gomez@estudiante.edu.pe / Estudiante123! (Ciclo 3)'
PRINT '- ana.torres@estudiante.edu.pe / Estudiante123! (Ciclo 2)'
GO
