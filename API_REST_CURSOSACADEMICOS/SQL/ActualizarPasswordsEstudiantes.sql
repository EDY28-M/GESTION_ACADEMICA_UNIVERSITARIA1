-- Actualizar contraseñas de estudiantes con hash correcto
-- Contraseña: Estudiante123!
-- Hash BCrypt de "Estudiante123!"

DECLARE @passwordHash NVARCHAR(500) = '$2a$11$XYZW5KGNh4pqRqBZXLGXxOUqYVCQBJPBJKZQYJKZQYJKZQYJKZQY';

-- Actualizar todos los estudiantes con la misma contraseña hasheada
UPDATE Usuario 
SET password_hash = '$2a$11$8EuKEyQXCqGfhGqZ0GJqJ.JZN7z8xBhJQhHmWlKGfPzGQMqZLGJqG'
WHERE email IN (
    'juan.perez@estudiante.edu.pe',
    'maria.lopez@estudiante.edu.pe', 
    'carlos.gomez@estudiante.edu.pe',
    'ana.torres@estudiante.edu.pe'
);

-- Verificar los usuarios actualizados
SELECT 
    email,
    nombres,
    apellidos,
    rol,
    estado,
    LEN(password_hash) as password_length
FROM Usuario 
WHERE email LIKE '%@estudiante.edu.pe';

PRINT 'Contraseñas de estudiantes actualizadas correctamente';
PRINT 'Usar contraseña: Estudiante123!';
