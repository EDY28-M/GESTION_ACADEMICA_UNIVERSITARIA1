-- Actualizar contraseñas de estudiantes con el hash correcto
-- Contraseña: Admin123! (misma que el admin para probar)

UPDATE Usuario 
SET password_hash = '$2a$11$DG/3dmYxRPf55Pfi7jb7iOZ9w65iKAK8COSm1CIHhSYiEJqDijWwO'
WHERE email IN (
    'juan.perez@estudiante.edu.pe',
    'maria.lopez@estudiante.edu.pe',
    'carlos.gomez@estudiante.edu.pe',
    'ana.torres@estudiante.edu.pe'
);

-- Verificar actualización
SELECT 
    email,
    rol,
    LEN(password_hash) as hash_length,
    LEFT(password_hash, 10) as hash_inicio
FROM Usuario 
WHERE email LIKE '%@estudiante.edu.pe';

PRINT '';
PRINT '===========================================';
PRINT 'Contraseñas actualizadas correctamente';
PRINT 'Email: cualquier @estudiante.edu.pe';
PRINT 'Contraseña: Admin123!';
PRINT '===========================================';
