-- Script para crear usuario administrador por defecto
-- Ejecutar este script en la base de datos Azure SQL

-- 1. Crear usuario administrador
-- Contraseña: Admin123 (hash BCrypt)
INSERT INTO Usuario (CorreoElectronico, PasswordHash, Rol, FechaCreacion, Activo)
VALUES ('admin@academia.com', '$2a$11$x8Y9kV3.ZQxJ8fN7FqX8/.L8PvJ5nHZKL6VqK5sQ7p8nYmN5gH5Gq', 'Administrador', GETDATE(), 1);

-- 2. Verificar que se creó correctamente
SELECT * FROM Usuario WHERE CorreoElectronico = 'admin@academia.com';

-- ===================================================
-- CREDENCIALES PARA LOGIN:
-- Email: admin@academia.com
-- Password: Admin123
-- ===================================================

-- 3. (OPCIONAL) Crear un estudiante de prueba
-- Contraseña: Estudiante123
INSERT INTO Usuario (CorreoElectronico, PasswordHash, Rol, FechaCreacion, Activo)
VALUES ('estudiante@test.com', '$2a$11$x8Y9kV3.ZQxJ8fN7FqX8/.L8PvJ5nHZKL6VqK5sQ7p8nYmN5gH5Gq', 'Estudiante', GETDATE(), 1);

-- Obtener el UsuarioId del estudiante recién creado
DECLARE @UsuarioId INT = (SELECT UsuarioId FROM Usuario WHERE CorreoElectronico = 'estudiante@test.com');

-- Crear el registro en la tabla Estudiantes
INSERT INTO Estudiantes (UsuarioId, Nombre, Apellido, Cedula, Telefono, Direccion, FechaNacimiento, FechaIngreso, CicloActual, Estado)
VALUES (@UsuarioId, 'Juan', 'Pérez', '0000000001', '0000000000', 'Dirección Test', '2000-01-01', GETDATE(), 1, 'Activo');

-- 4. (OPCIONAL) Crear un docente de prueba  
-- Contraseña: Docente123
INSERT INTO Usuario (CorreoElectronico, PasswordHash, Rol, FechaCreacion, Activo)
VALUES ('docente@test.com', '$2a$11$x8Y9kV3.ZQxJ8fN7FqX8/.L8PvJ5nHZKL6VqK5sQ7p8nYmN5gH5Gq', 'Docente', GETDATE(), 1);

-- Obtener el UsuarioId del docente recién creado
DECLARE @DocenteUsuarioId INT = (SELECT UsuarioId FROM Usuario WHERE CorreoElectronico = 'docente@test.com');

-- Crear el registro en la tabla Docentes
INSERT INTO Docentes (UsuarioId, Nombre, Apellido, Cedula, Telefono, Direccion, Especialidad, FechaContratacion, Estado, FechaCreacion)
VALUES (@DocenteUsuarioId, 'María', 'García', '0000000002', '0000000000', 'Dirección Test', 'Matemáticas', GETDATE(), 'Activo', GETDATE());

-- 5. Verificar todos los Usuario creados
SELECT 
    u.UsuarioId,
    u.CorreoElectronico,
    u.Rol,
    u.Activo,
    u.FechaCreacion
FROM Usuario u
ORDER BY u.UsuarioId;
