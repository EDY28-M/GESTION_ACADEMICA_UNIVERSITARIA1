-- ============================================
-- Script para probar el avance automático de ciclo
-- Ejecutar ANTES y DESPUÉS de activar un nuevo período
-- ============================================

PRINT '========================================';
PRINT 'PASO 1: Ver ciclo actual de estudiantes';
PRINT '========================================';

SELECT 
    e.id,
    e.codigo,
    e.nombres + ' ' + e.apellidos AS estudiante,
    e.ciclo_actual AS ciclo,
    e.creditos_aprobados,
    e.promedio_acumulado,
    e.promedio_semestral
FROM Estudiante e
ORDER BY e.codigo;

PRINT '';
PRINT '========================================';
PRINT 'PASO 2: Ver período activo actual';
PRINT '========================================';

SELECT 
    id,
    nombre,
    anio,
    ciclo,
    activo,
    fecha_inicio,
    fecha_fin
FROM Periodo
WHERE activo = 1;

PRINT '';
PRINT '========================================';
PRINT 'PASO 3: Estudiantes y sus matrículas en período activo';
PRINT '========================================';

SELECT 
    e.codigo,
    e.nombres + ' ' + e.apellidos AS estudiante,
    e.ciclo_actual AS ciclo_estudiante,
    c.curso AS nombre_curso,
    c.ciclo AS ciclo_curso,
    m.estado,
    m.promedio_final
FROM Matricula m
JOIN Estudiante e ON e.id = m.idEstudiante
JOIN Curso c ON c.id = m.idCurso
JOIN Periodo p ON p.id = m.idPeriodo
WHERE p.activo = 1
ORDER BY e.codigo, c.curso;

PRINT '';
PRINT '========================================';
PRINT 'PASO 4: ¿Quiénes DEBERÍAN avanzar de ciclo?';
PRINT '========================================';

WITH EstadoCursos AS (
    SELECT 
        e.id,
        e.codigo,
        e.nombres + ' ' + e.apellidos AS estudiante,
        e.ciclo_actual,
        p.nombre AS periodo,
        COUNT(*) AS total_cursos,
        SUM(CASE WHEN m.estado = 'Aprobado' THEN 1 ELSE 0 END) AS cursos_aprobados,
        SUM(CASE WHEN m.estado = 'Desaprobado' THEN 1 ELSE 0 END) AS cursos_desaprobados,
        SUM(CASE WHEN m.estado = 'Retirado' THEN 1 ELSE 0 END) AS cursos_retirados
    FROM Estudiante e
    JOIN Matricula m ON m.idEstudiante = e.id
    JOIN Periodo p ON p.id = m.idPeriodo
    WHERE p.activo = 1 
    GROUP BY e.id, e.codigo, e.nombres, e.apellidos, e.ciclo_actual, p.nombre
)
SELECT 
    codigo,
    estudiante,
    periodo,
    ciclo_actual AS ciclo_actual,
    CASE 
        WHEN cursos_aprobados = (total_cursos - cursos_retirados) AND (total_cursos - cursos_retirados) > 0 
        THEN ciclo_actual + 1
        ELSE ciclo_actual
    END AS ciclo_despues_activar,
    total_cursos,
    cursos_aprobados,
    cursos_desaprobados,
    cursos_retirados,
    CASE 
        WHEN cursos_aprobados = (total_cursos - cursos_retirados) AND (total_cursos - cursos_retirados) > 0 
        THEN '✅ AVANZARÁ AL ACTIVAR NUEVO PERIODO'
        ELSE '❌ SE MANTIENE EN EL MISMO CICLO'
    END AS resultado
FROM EstadoCursos
ORDER BY codigo;

PRINT '';
PRINT '========================================';
PRINT 'INSTRUCCIONES:';
PRINT '========================================';
PRINT '1. Ejecuta este script para ver el estado actual';
PRINT '2. Ve al panel admin y activa un nuevo período o crea uno activo';
PRINT '3. Vuelve a ejecutar este script para ver que los estudiantes avanzaron de ciclo';
PRINT '';
PRINT 'REGLA: Solo avanzan los estudiantes que APROBARON TODOS sus cursos';
PRINT '       (excluyendo los cursos retirados)';
PRINT '========================================';

-- Después de activar el nuevo período, ejecuta esta consulta para verificar:
/*
PRINT '';
PRINT '========================================';
PRINT 'VERIFICACIÓN POST-ACTIVACIÓN';
PRINT '========================================';

SELECT 
    e.codigo,
    e.nombres + ' ' + e.apellidos AS estudiante,
    e.ciclo_actual AS nuevo_ciclo,
    e.promedio_acumulado,
    e.promedio_semestral,
    e.creditos_aprobados
FROM Estudiante e
ORDER BY e.codigo;
*/
