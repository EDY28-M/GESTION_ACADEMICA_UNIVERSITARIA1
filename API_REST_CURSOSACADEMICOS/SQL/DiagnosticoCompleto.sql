-- ============================================
-- DIAGNÓSTICO: Ver estado actual del sistema
-- ============================================

PRINT '========================================';
PRINT '1. PERÍODOS (activo y recientes)';
PRINT '========================================';

SELECT 
    id,
    nombre,
    anio,
    ciclo,
    activo,
    fecha_inicio,
    fecha_fin,
    fecha_creacion
FROM Periodo
ORDER BY fecha_creacion DESC;

PRINT '';
PRINT '========================================';
PRINT '2. ESTUDIANTES - Ciclo Actual';
PRINT '========================================';

SELECT 
    e.id,
    e.codigo,
    e.nombres + ' ' + e.apellidos AS estudiante,
    e.ciclo_actual,
    e.creditos_aprobados,
    e.promedio_acumulado,
    e.promedio_semestral,
    e.id_periodo_ultimo
FROM Estudiante e
ORDER BY e.codigo;

PRINT '';
PRINT '========================================';
PRINT '3. MATRÍCULAS POR PERÍODO';
PRINT '========================================';

SELECT 
    p.nombre AS periodo,
    p.activo,
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
ORDER BY p.nombre, e.codigo, c.curso;

PRINT '';
PRINT '========================================';
PRINT '4. RESUMEN POR ESTUDIANTE Y PERÍODO';
PRINT '========================================';

SELECT 
    p.nombre AS periodo,
    p.activo,
    e.codigo,
    e.nombres + ' ' + e.apellidos AS estudiante,
    e.ciclo_actual,
    COUNT(*) AS total_cursos,
    SUM(CASE WHEN m.estado = 'Aprobado' THEN 1 ELSE 0 END) AS aprobados,
    SUM(CASE WHEN m.estado = 'Desaprobado' THEN 1 ELSE 0 END) AS desaprobados,
    SUM(CASE WHEN m.estado = 'Retirado' THEN 1 ELSE 0 END) AS retirados,
    SUM(CASE WHEN m.estado = 'Matriculado' THEN 1 ELSE 0 END) AS matriculados
FROM Matricula m
JOIN Estudiante e ON e.id = m.idEstudiante
JOIN Periodo p ON p.id = m.idPeriodo
GROUP BY p.nombre, p.activo, e.codigo, e.nombres, e.apellidos, e.ciclo_actual
ORDER BY p.nombre, e.codigo;

PRINT '';
PRINT '========================================';
PRINT '5. CURSOS DISPONIBLES POR CICLO';
PRINT '========================================';

SELECT 
    c.ciclo,
    COUNT(*) AS total_cursos,
    STRING_AGG(c.curso, ', ') AS cursos
FROM Curso c
GROUP BY c.ciclo
ORDER BY c.ciclo;

PRINT '';
PRINT '========================================';
PRINT '6. DIAGNÓSTICO: ¿Por qué no ve cursos?';
PRINT '========================================';

SELECT 
    e.codigo,
    e.nombres + ' ' + e.apellidos AS estudiante,
    e.ciclo_actual AS ciclo_estudiante,
    (SELECT COUNT(*) FROM Curso WHERE ciclo = e.ciclo_actual) AS cursos_disponibles_en_su_ciclo,
    (SELECT COUNT(*) 
     FROM Matricula m2 
     JOIN Periodo p2 ON p2.id = m2.idPeriodo 
     WHERE m2.idEstudiante = e.id 
       AND p2.activo = 1) AS cursos_ya_matriculados_periodo_activo
FROM Estudiante e
ORDER BY e.codigo;

PRINT '';
PRINT '========================================';
PRINT '7. SOLUCIÓN: ¿Deben avanzar de ciclo?';
PRINT '========================================';

-- Ver período anterior (el que estaba antes del activo)
DECLARE @PeriodoAnteriorId INT = (
    SELECT TOP 1 id 
    FROM Periodo 
    WHERE activo = 0 
    ORDER BY fecha_fin DESC
);

SELECT 
    e.codigo,
    e.nombres + ' ' + e.apellidos AS estudiante,
    e.ciclo_actual AS ciclo_actual,
    (SELECT nombre FROM Periodo WHERE id = @PeriodoAnteriorId) AS periodo_anterior,
    COUNT(*) AS cursos_periodo_anterior,
    SUM(CASE WHEN m.estado = 'Aprobado' THEN 1 ELSE 0 END) AS cursos_aprobados,
    SUM(CASE WHEN m.estado = 'Retirado' THEN 1 ELSE 0 END) AS cursos_retirados,
    CASE 
        WHEN SUM(CASE WHEN m.estado = 'Aprobado' THEN 1 ELSE 0 END) = COUNT(*) - SUM(CASE WHEN m.estado = 'Retirado' THEN 1 ELSE 0 END)
             AND (COUNT(*) - SUM(CASE WHEN m.estado = 'Retirado' THEN 1 ELSE 0 END)) > 0
        THEN e.ciclo_actual + 1
        ELSE e.ciclo_actual
    END AS ciclo_correcto,
    CASE 
        WHEN SUM(CASE WHEN m.estado = 'Aprobado' THEN 1 ELSE 0 END) = COUNT(*) - SUM(CASE WHEN m.estado = 'Retirado' THEN 1 ELSE 0 END)
             AND (COUNT(*) - SUM(CASE WHEN m.estado = 'Retirado' THEN 1 ELSE 0 END)) > 0
        THEN '✅ DEBE ESTAR EN CICLO ' + CAST(e.ciclo_actual + 1 AS VARCHAR)
        ELSE '❌ DEBE SEGUIR EN CICLO ' + CAST(e.ciclo_actual AS VARCHAR)
    END AS resultado
FROM Estudiante e
LEFT JOIN Matricula m ON m.idEstudiante = e.id AND m.idPeriodo = @PeriodoAnteriorId
GROUP BY e.codigo, e.nombres, e.apellidos, e.ciclo_actual, e.id
ORDER BY e.codigo;
