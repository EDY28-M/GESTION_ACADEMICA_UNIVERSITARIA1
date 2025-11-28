-- ============================================
-- SCRIPT DE DIAGNÓSTICO Y CORRECCIÓN
-- Ejecutar este script para diagnosticar el problema actual
-- ============================================

PRINT '========================================';
PRINT 'DIAGNÓSTICO: Estado actual del sistema';
PRINT '========================================';
PRINT '';

-- 1. Ver todos los períodos y cuál está activo
PRINT '1. PERÍODOS REGISTRADOS:';
SELECT 
    id,
    nombre,
    anio,
    ciclo,
    CASE WHEN activo = 1 THEN 'ACTIVO ✅' ELSE 'Cerrado' END AS estado,
    fecha_inicio,
    fecha_fin,
    fecha_creacion
FROM Periodo
ORDER BY anio DESC, ciclo DESC;

PRINT '';
PRINT '2. ESTUDIANTES Y SU CICLO ACTUAL:';
SELECT 
    e.id,
    e.codigo,
    e.nombres + ' ' + e.apellidos AS estudiante,
    e.ciclo_actual AS ciclo,
    e.creditos_aprobados,
    e.promedio_acumulado,
    e.promedio_semestral,
    e.id_periodo_ultimo
FROM Estudiante e
ORDER BY e.codigo;

PRINT '';
PRINT '3. MATRÍCULAS DEL PERÍODO CERRADO (2025-I):';
SELECT 
    e.codigo,
    e.nombres + ' ' + e.apellidos AS estudiante,
    e.ciclo_actual,
    p.nombre AS periodo,
    c.curso,
    c.ciclo AS ciclo_curso,
    m.estado,
    m.promedio_final
FROM Matricula m
JOIN Estudiante e ON e.id = m.idEstudiante
JOIN Curso c ON c.id = m.idCurso
JOIN Periodo p ON p.id = m.idPeriodo
WHERE p.nombre = '2025-I'
ORDER BY e.codigo, c.ciclo, c.curso;

PRINT '';
PRINT '4. ANÁLISIS: ¿Deberían haber avanzado de ciclo?';
WITH ResumenPorEstudiante AS (
    SELECT 
        e.id,
        e.codigo,
        e.nombres + ' ' + e.apellidos AS estudiante,
        e.ciclo_actual,
        p.nombre AS periodo,
        COUNT(*) AS total_matriculas,
        SUM(CASE WHEN m.estado = 'Aprobado' THEN 1 ELSE 0 END) AS aprobados,
        SUM(CASE WHEN m.estado = 'Desaprobado' THEN 1 ELSE 0 END) AS desaprobados,
        SUM(CASE WHEN m.estado = 'Retirado' THEN 1 ELSE 0 END) AS retirados
    FROM Estudiante e
    JOIN Matricula m ON m.idEstudiante = e.id
    JOIN Periodo p ON p.id = m.idPeriodo
    WHERE p.nombre = '2025-I'
    GROUP BY e.id, e.codigo, e.nombres, e.apellidos, e.ciclo_actual, p.nombre
)
SELECT 
    codigo,
    estudiante,
    periodo,
    ciclo_actual AS ciclo_actual,
    CASE 
        WHEN aprobados = (total_matriculas - retirados) AND (total_matriculas - retirados) > 0 
        THEN ciclo_actual + 1
        ELSE ciclo_actual
    END AS ciclo_deberia_ser,
    total_matriculas,
    aprobados,
    desaprobados,
    retirados,
    CASE 
        WHEN aprobados = (total_matriculas - retirados) AND (total_matriculas - retirados) > 0 
        THEN '❌ DEBERÍA ESTAR EN CICLO ' + CAST(ciclo_actual + 1 AS VARCHAR)
        ELSE '✅ Ciclo correcto'
    END AS diagnostico
FROM ResumenPorEstudiante
ORDER BY codigo;

PRINT '';
PRINT '5. CURSOS DISPONIBLES POR CICLO:';
SELECT 
    ciclo,
    COUNT(*) AS cantidad_cursos,
    STRING_AGG(curso, ', ') AS cursos
FROM Curso
WHERE ciclo IS NOT NULL
GROUP BY ciclo
ORDER BY ciclo;

PRINT '';
PRINT '========================================';
PRINT 'SOLUCIÓN MANUAL (Si es necesario):';
PRINT '========================================';
PRINT 'Si los estudiantes NO avanzaron de ciclo automáticamente,';
PRINT 'ejecuta el siguiente UPDATE (descomentar):';
PRINT '';

/*
-- ⚠️ DESCOMENTA ESTO SOLO SI LOS ESTUDIANTES NO AVANZARON AUTOMÁTICAMENTE

-- Avanzar manualmente el ciclo de estudiantes que aprobaron TODO en 2025-I
UPDATE e
SET e.ciclo_actual = e.ciclo_actual + 1
FROM Estudiante e
WHERE e.id IN (
    SELECT m.idEstudiante
    FROM Matricula m
    JOIN Periodo p ON p.id = m.idPeriodo
    WHERE p.nombre = '2025-I'
      AND m.estado != 'Retirado'
    GROUP BY m.idEstudiante
    HAVING COUNT(*) = SUM(CASE WHEN m.estado = 'Aprobado' THEN 1 ELSE 0 END)
       AND COUNT(*) > 0
)
AND e.ciclo_actual < 10;

-- Ver el resultado
SELECT 
    e.codigo,
    e.nombres + ' ' + e.apellidos AS estudiante,
    e.ciclo_actual AS nuevo_ciclo
FROM Estudiante e
ORDER BY e.codigo;
*/

PRINT '';
PRINT '========================================';
PRINT 'PRÓXIMOS PASOS:';
PRINT '========================================';
PRINT '1. Si el ciclo NO avanzó: Ejecuta el UPDATE manual arriba';
PRINT '2. Verifica que el backend esté corriendo: http://localhost:5251';
PRINT '3. Inicia sesión como estudiante';
PRINT '4. Ve a "Cursos Disponibles" - deberías ver cursos del nuevo ciclo';
PRINT '5. Si sigues viendo cursos del ciclo anterior, el backend necesita reiniciarse';
