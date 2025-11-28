-- ============================================
-- SOLUCIÓN INMEDIATA: Avanzar ciclos manualmente
-- ============================================

PRINT '========================================';
PRINT 'DIAGNÓSTICO RÁPIDO';
PRINT '========================================';
PRINT '';

-- Ver períodos
PRINT 'Períodos existentes:';
SELECT id, nombre, anio, ciclo, activo, fecha_inicio, fecha_fin
FROM Periodo
ORDER BY anio DESC, ciclo DESC;

PRINT '';
PRINT 'Estudiantes y su ciclo actual:';
SELECT 
    id,
    codigo,
    nombres + ' ' + apellidos AS estudiante,
    ciclo_actual,
    creditos_aprobados
FROM Estudiante
ORDER BY codigo;

PRINT '';
PRINT '========================================';
PRINT 'AVANZANDO CICLOS DE ESTUDIANTES';
PRINT '========================================';

-- Declarar variables
DECLARE @PeriodoAnteriorId INT;
DECLARE @PeriodoActivoId INT;

-- Obtener el período anterior (2025-I)
SELECT @PeriodoAnteriorId = id 
FROM Periodo 
WHERE nombre = '2025-I';

-- Obtener el período activo (2025-II)
SELECT @PeriodoActivoId = id 
FROM Periodo 
WHERE activo = 1;

PRINT 'Período anterior: 2025-I (ID: ' + CAST(@PeriodoAnteriorId AS VARCHAR) + ')';
PRINT 'Período activo: ' + (SELECT nombre FROM Periodo WHERE id = @PeriodoActivoId);
PRINT '';

-- Avanzar ciclo de estudiantes que aprobaron TODO en 2025-I
UPDATE e
SET e.ciclo_actual = e.ciclo_actual + 1
FROM Estudiante e
WHERE e.id IN (
    -- Estudiantes que aprobaron TODOS sus cursos en 2025-I
    SELECT m.idEstudiante
    FROM Matricula m
    WHERE m.idPeriodo = @PeriodoAnteriorId
      AND m.estado != 'Retirado'
    GROUP BY m.idEstudiante
    HAVING COUNT(*) = SUM(CASE WHEN m.estado = 'Aprobado' THEN 1 ELSE 0 END)
       AND COUNT(*) > 0
)
AND e.ciclo_actual < 10; -- No pasar del ciclo 10

PRINT 'Estudiantes actualizados: ' + CAST(@@ROWCOUNT AS VARCHAR);
PRINT '';

PRINT '========================================';
PRINT 'RESULTADO FINAL';
PRINT '========================================';

-- Ver resultado
SELECT 
    e.id,
    e.codigo,
    e.nombres + ' ' + e.apellidos AS estudiante,
    e.ciclo_actual AS nuevo_ciclo,
    COUNT(m.id) AS cursos_en_2025_I,
    SUM(CASE WHEN m.estado = 'Aprobado' THEN 1 ELSE 0 END) AS aprobados,
    SUM(CASE WHEN m.estado = 'Desaprobado' THEN 1 ELSE 0 END) AS desaprobados
FROM Estudiante e
LEFT JOIN Matricula m ON m.idEstudiante = e.id AND m.idPeriodo = @PeriodoAnteriorId AND m.estado != 'Retirado'
GROUP BY e.id, e.codigo, e.nombres, e.apellidos, e.ciclo_actual
ORDER BY e.codigo;

PRINT '';
PRINT '========================================';
PRINT 'CURSOS DISPONIBLES AHORA PARA CADA CICLO';
PRINT '========================================';

-- Ver qué cursos hay por ciclo
SELECT 
    ciclo,
    COUNT(*) AS total_cursos,
    STRING_AGG(curso, ', ') AS cursos
FROM Curso
WHERE ciclo IS NOT NULL
GROUP BY ciclo
ORDER BY ciclo;

PRINT '';
PRINT '✅ PROCESO COMPLETADO';
PRINT 'Los estudiantes deberían ver ahora los cursos de su nuevo ciclo';
PRINT 'Refresca el navegador (F5) para ver los cambios';
