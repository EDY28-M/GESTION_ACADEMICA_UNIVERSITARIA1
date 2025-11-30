-- =============================================
-- SCRIPT DE VERIFICACIÓN Y CORRECCIÓN
-- Verifica que todas las columnas necesarias existan
-- =============================================
USE [GestionAcademica]
GO

PRINT '=== VERIFICANDO ESQUEMA DE BASE DE DATOS ===';
PRINT '';

-- Verificar columnas en Estudiante
PRINT 'Verificando tabla Estudiante...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Estudiante]') AND name = 'estado')
BEGIN
    ALTER TABLE [dbo].[Estudiante] ADD [estado] VARCHAR(20) NOT NULL DEFAULT 'Activo';
    PRINT '✓ Columna estado agregada';
END
ELSE
BEGIN
    PRINT '✓ Columna estado existe';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Estudiante]') AND name = 'promedio_semestral')
BEGIN
    ALTER TABLE [dbo].[Estudiante] ADD [promedio_semestral] DECIMAL(10,4) NULL;
    PRINT '✓ Columna promedio_semestral agregada';
END
ELSE
BEGIN
    PRINT '✓ Columna promedio_semestral existe';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Estudiante]') AND name = 'promedio_acumulado')
BEGIN
    ALTER TABLE [dbo].[Estudiante] ADD [promedio_acumulado] DECIMAL(10,4) NULL;
    PRINT '✓ Columna promedio_acumulado agregada';
END
ELSE
BEGIN
    PRINT '✓ Columna promedio_acumulado existe';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Estudiante]') AND name = 'id_periodo_ultimo')
BEGIN
    ALTER TABLE [dbo].[Estudiante] ADD [id_periodo_ultimo] INT NULL;
    ALTER TABLE [dbo].[Estudiante] ADD CONSTRAINT [FK_Estudiante_PeriodoUltimo] 
        FOREIGN KEY([id_periodo_ultimo]) REFERENCES [dbo].[Periodo] ([id]);
    PRINT '✓ Columna id_periodo_ultimo agregada';
END
ELSE
BEGIN
    PRINT '✓ Columna id_periodo_ultimo existe';
END

PRINT '';
PRINT '=== VERIFICACIÓN COMPLETADA ===';
PRINT '';

-- Mostrar estructura actual de la tabla Estudiante
PRINT 'Columnas de la tabla Estudiante:';
SELECT 
    c.name AS Columna,
    t.name AS Tipo,
    c.max_length AS Longitud,
    c.is_nullable AS Nullable
FROM sys.columns c
JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID(N'[dbo].[Estudiante]')
ORDER BY c.column_id;
