-- =============================================
-- Script para LIMPIAR BASE DE DATOS
-- Elimina todos los cursos, estudiantes y periodos
-- ¡PRECAUCIÓN! Este script elimina TODOS los datos
-- =============================================

USE [GestionAcademica]
GO

PRINT '╔═══════════════════════════════════════════════════════════════╗';
PRINT '║              LIMPIEZA COMPLETA DE BASE DE DATOS               ║';
PRINT '║                    ⚠️  PRECAUCIÓN ⚠️                          ║';
PRINT '╚═══════════════════════════════════════════════════════════════╝';
PRINT '';

-- Deshabilitar restricciones de claves foráneas temporalmente
PRINT '▶ Deshabilitando restricciones de claves foráneas...';
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';
PRINT '✓ Restricciones deshabilitadas';
PRINT '';

-- =============================================
-- 1. ELIMINAR ASISTENCIAS
-- =============================================
PRINT '▶ Eliminando asistencias...';
DELETE FROM Asistencia;
DECLARE @AsistenciasCount INT = @@ROWCOUNT;
PRINT '✓ Asistencias eliminadas: ' + CAST(@AsistenciasCount AS VARCHAR(10));
PRINT '';

-- =============================================
-- 2. ELIMINAR NOTAS
-- =============================================
PRINT '▶ Eliminando notas...';
DELETE FROM Nota;
DECLARE @NotasCount INT = @@ROWCOUNT;
PRINT '✓ Notas eliminadas: ' + CAST(@NotasCount AS VARCHAR(10));
PRINT '';

-- =============================================
-- 3. ELIMINAR TIPOS DE EVALUACIÓN
-- =============================================
PRINT '▶ Eliminando tipos de evaluación...';
DELETE FROM TipoEvaluacion;
DECLARE @TiposEvaluacionCount INT = @@ROWCOUNT;
PRINT '✓ Tipos de evaluación eliminados: ' + CAST(@TiposEvaluacionCount AS VARCHAR(10));
PRINT '';

-- =============================================
-- 4. ELIMINAR MATRÍCULAS
-- =============================================
PRINT '▶ Eliminando matrículas...';
DELETE FROM Matricula;
DECLARE @MatriculasCount INT = @@ROWCOUNT;
PRINT '✓ Matrículas eliminadas: ' + CAST(@MatriculasCount AS VARCHAR(10));
PRINT '';

-- =============================================
-- 5. ELIMINAR ESTUDIANTE-PERIODO
-- =============================================
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'EstudiantePeriodo')
BEGIN
    PRINT '▶ Eliminando relaciones estudiante-periodo...';
    DELETE FROM EstudiantePeriodo;
    DECLARE @EstudiantePeriodoCount INT = @@ROWCOUNT;
    PRINT '✓ Relaciones estudiante-periodo eliminadas: ' + CAST(@EstudiantePeriodoCount AS VARCHAR(10));
    PRINT '';
END

-- =============================================
-- 6. ELIMINAR ESTUDIANTES
-- =============================================
PRINT '▶ Eliminando estudiantes...';
DELETE FROM Estudiante;
DECLARE @EstudiantesCount INT = @@ROWCOUNT;
PRINT '✓ Estudiantes eliminados: ' + CAST(@EstudiantesCount AS VARCHAR(10));
PRINT '';

-- =============================================
-- 7. ELIMINAR PERIODOS
-- =============================================
PRINT '▶ Eliminando periodos académicos...';
DELETE FROM Periodo;
DECLARE @PeriodosCount INT = @@ROWCOUNT;
PRINT '✓ Periodos eliminados: ' + CAST(@PeriodosCount AS VARCHAR(10));
PRINT '';

-- =============================================
-- 8. ELIMINAR PREREQUISITOS DE CURSOS
-- =============================================
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CursoPrerequisito')
BEGIN
    PRINT '▶ Eliminando prerequisitos de cursos...';
    DELETE FROM CursoPrerequisito;
    DECLARE @PrerequisitosCount INT = @@ROWCOUNT;
    PRINT '✓ Prerequisitos eliminados: ' + CAST(@PrerequisitosCount AS VARCHAR(10));
    PRINT '';
END

-- =============================================
-- 9. ELIMINAR CURSOS
-- =============================================
PRINT '▶ Eliminando cursos...';
DELETE FROM Curso;
DECLARE @CursosCount INT = @@ROWCOUNT;
PRINT '✓ Cursos eliminados: ' + CAST(@CursosCount AS VARCHAR(10));
PRINT '';

-- =============================================
-- 10. ELIMINAR NOTIFICACIONES (OPCIONAL)
-- =============================================
PRINT '▶ Eliminando notificaciones...';
DELETE FROM Notificacion;
DECLARE @NotificacionesCount INT = @@ROWCOUNT;
PRINT '✓ Notificaciones eliminadas: ' + CAST(@NotificacionesCount AS VARCHAR(10));
PRINT '';

-- Habilitar restricciones de claves foráneas nuevamente
PRINT '▶ Habilitando restricciones de claves foráneas...';
EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';
PRINT '✓ Restricciones habilitadas';
PRINT '';

-- Reiniciar los contadores de identidad
PRINT '▶ Reiniciando contadores de identidad...';

IF EXISTS (SELECT * FROM Asistencia)
    DBCC CHECKIDENT ('Asistencia', RESEED, 0);
ELSE
    DBCC CHECKIDENT ('Asistencia', RESEED, 0);

IF EXISTS (SELECT * FROM Nota)
    DBCC CHECKIDENT ('Nota', RESEED, 0);
ELSE
    DBCC CHECKIDENT ('Nota', RESEED, 0);

IF EXISTS (SELECT * FROM TipoEvaluacion)
    DBCC CHECKIDENT ('TipoEvaluacion', RESEED, 0);
ELSE
    DBCC CHECKIDENT ('TipoEvaluacion', RESEED, 0);

IF EXISTS (SELECT * FROM Matricula)
    DBCC CHECKIDENT ('Matricula', RESEED, 0);
ELSE
    DBCC CHECKIDENT ('Matricula', RESEED, 0);

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'EstudiantePeriodo')
BEGIN
    IF EXISTS (SELECT * FROM EstudiantePeriodo)
        DBCC CHECKIDENT ('EstudiantePeriodo', RESEED, 0);
    ELSE
        DBCC CHECKIDENT ('EstudiantePeriodo', RESEED, 0);
END

IF EXISTS (SELECT * FROM Estudiante)
    DBCC CHECKIDENT ('Estudiante', RESEED, 0);
ELSE
    DBCC CHECKIDENT ('Estudiante', RESEED, 0);

IF EXISTS (SELECT * FROM Periodo)
    DBCC CHECKIDENT ('Periodo', RESEED, 0);
ELSE
    DBCC CHECKIDENT ('Periodo', RESEED, 0);

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CursoPrerequisito')
BEGIN
    IF EXISTS (SELECT * FROM CursoPrerequisito)
        DBCC CHECKIDENT ('CursoPrerequisito', RESEED, 0);
    ELSE
        DBCC CHECKIDENT ('CursoPrerequisito', RESEED, 0);
END

IF EXISTS (SELECT * FROM Curso)
    DBCC CHECKIDENT ('Curso', RESEED, 0);
ELSE
    DBCC CHECKIDENT ('Curso', RESEED, 0);

IF EXISTS (SELECT * FROM Notificacion)
    DBCC CHECKIDENT ('Notificacion', RESEED, 0);
ELSE
    DBCC CHECKIDENT ('Notificacion', RESEED, 0);

PRINT '✓ Contadores reiniciados';
PRINT '';

-- =============================================
-- RESUMEN FINAL
-- =============================================
PRINT '╔═══════════════════════════════════════════════════════════════╗';
PRINT '║                    RESUMEN DE LIMPIEZA                        ║';
PRINT '╚═══════════════════════════════════════════════════════════════╝';
PRINT 'Asistencias eliminadas: ' + CAST(@AsistenciasCount AS VARCHAR(10));
PRINT 'Notas eliminadas: ' + CAST(@NotasCount AS VARCHAR(10));
PRINT 'Tipos de evaluación eliminados: ' + CAST(@TiposEvaluacionCount AS VARCHAR(10));
PRINT 'Matrículas eliminadas: ' + CAST(@MatriculasCount AS VARCHAR(10));
PRINT 'Estudiantes eliminados: ' + CAST(@EstudiantesCount AS VARCHAR(10));
PRINT 'Periodos eliminados: ' + CAST(@PeriodosCount AS VARCHAR(10));
PRINT 'Cursos eliminados: ' + CAST(@CursosCount AS VARCHAR(10));
PRINT 'Notificaciones eliminadas: ' + CAST(@NotificacionesCount AS VARCHAR(10));
PRINT '';
PRINT '╔═══════════════════════════════════════════════════════════════╗';
PRINT '║           ✓ BASE DE DATOS LIMPIADA EXITOSAMENTE              ║';
PRINT '╚═══════════════════════════════════════════════════════════════╝';
PRINT '';
PRINT 'NOTA: Los docentes y usuarios NO fueron eliminados.';
PRINT 'Si deseas eliminarlos también, ejecuta:';
PRINT '  DELETE FROM Docente;';
PRINT '  DELETE FROM Usuario WHERE rol != ''Administrador'';';
PRINT '';
