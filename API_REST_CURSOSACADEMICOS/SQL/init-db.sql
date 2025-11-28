-- ========================================
-- SCRIPT DE INICIALIZACIÓN DE BASE DE DATOS
-- Para contenedor Docker SQL Server
-- ========================================

-- Esperar a que SQL Server esté completamente iniciado
WAITFOR DELAY '00:00:10';
GO

-- Crear la base de datos si no existe
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'GestionAcademica')
BEGIN
    CREATE DATABASE GestionAcademica;
    PRINT 'Base de datos GestionAcademica creada exitosamente';
END
GO

USE GestionAcademica;
GO

-- ========================================
-- NOTA IMPORTANTE:
-- ========================================
-- Este script crea la base de datos vacía.
-- Las tablas serán creadas automáticamente por Entity Framework
-- cuando la aplicación ASP.NET Core se ejecute por primera vez
-- mediante las migraciones automáticas.
--
-- Si deseas pre-cargar datos, puedes copiar aquí los scripts:
-- - ActualizarPasswordsCorrectamente.sql
-- - InsertarCursosCompletos.sql
-- - InsertarDocentesYCursosCompletos.sql
-- ========================================

PRINT 'Base de datos lista para uso. EF Core creará las tablas automáticamente.';
GO
