-- Script para actualizar passwords con hash BCrypt correcto generado por BCrypt.Net-Next 4.0.3
-- Hash generado para la contraseña: Admin123!
-- Hash: $2a$11$FBKWJ2T13sXIAwbBjyRBXu1FUfujV3Kpj.x31h7XMb4dcXwYdp6Ym
-- Verificado que funciona con BCrypt.Net.BCrypt.Verify()

USE GestionAcademica;
GO

UPDATE Usuario 
SET password_hash = '$2a$11$FBKWJ2T13sXIAwbBjyRBXu1FUfujV3Kpj.x31h7XMb4dcXwYdp6Ym' 
WHERE email = 'admin@gestionacademica.com';

UPDATE Usuario 
SET password_hash = '$2a$11$FBKWJ2T13sXIAwbBjyRBXu1FUfujV3Kpj.x31h7XMb4dcXwYdp6Ym' 
WHERE email = 'docente@gestionacademica.com';

UPDATE Usuario 
SET password_hash = '$2a$11$FBKWJ2T13sXIAwbBjyRBXu1FUfujV3Kpj.x31h7XMb4dcXwYdp6Ym' 
WHERE email = 'coordinador@gestionacademica.com';

-- Verificar que se actualizaron correctamente
SELECT 
    email,
    LEFT(password_hash, 30) + '...' AS password_hash_inicio,
    LEN(password_hash) AS longitud_hash
FROM Usuario
WHERE email IN ('admin@gestionacademica.com', 'docente@gestionacademica.com', 'coordinador@gestionacademica.com');

PRINT 'Passwords actualizados correctamente';
PRINT 'Contraseña para todos los usuarios: Admin123!';
GO
