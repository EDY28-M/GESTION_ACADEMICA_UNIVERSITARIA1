-- Script para asignar contraseña a docentes de prueba
-- Genera hash BCrypt para la contraseña "Docente123!"

-- Este hash corresponde a la contraseña: Docente123!
-- Generado con BCrypt, salt factor 11

DECLARE @passwordHash NVARCHAR(255) = '$2a$11$XMvqE9RBK8rGxVYxJ0Z8JeH0K5BK5qK0HKLhQw6xKjZ9K5qK0HKLK'

-- Actualizar un docente específico (cambia el correo por uno que exista en tu base de datos)
UPDATE Docente 
SET password_hash = @passwordHash
WHERE correo = 'victor.matias@unas.edu.pe';

-- Verificar que se actualizó
SELECT 
    id,
    nombres,
    apellidos,
    correo,
    CASE 
        WHEN password_hash IS NOT NULL THEN 'Sí'
        ELSE 'No'
    END AS [Tiene Contraseña]
FROM Docente
WHERE correo = 'victor.matias@unas.edu.pe';

PRINT 'Contraseña asignada: Docente123!'
GO
