-- ============================================
-- SCRIPT DE VERIFICACIÓN: Cursos retirados NO deben contar
-- ============================================

USE GestionAcademica;
GO

PRINT '==================== VERIFICACIÓN DE CURSOS RETIRADOS ====================';
PRINT '';

-- 1. Ver todas las matrículas por estudiante agrupadas por estado
SELECT 
    e.codigo,
    e.nombres + ' ' + e.apellidos as estudiante,
    c.curso as curso,
    p.nombre as periodo,
    m.estado,
    m.promedio_final,
    CASE 
        WHEN m.estado = 'Retirado' THEN '❌ NO DEBE CONTAR'
        WHEN m.estado = 'Aprobado' THEN '✅ CUENTA EN ACUMULADO'
        WHEN m.estado = 'Desaprobado' THEN '⚠️ NO CUENTA (DESAPROBADO)'
        ELSE '⏳ EN CURSO'
    END as observacion
FROM Estudiante e
INNER JOIN Matricula m ON m.idEstudiante = e.id
INNER JOIN Curso c ON m.idCurso = c.id
INNER JOIN Periodo p ON m.idPeriodo = p.id
ORDER BY e.codigo, p.anio DESC, p.ciclo DESC, c.curso;

PRINT '';
PRINT '==================== RESUMEN POR ESTUDIANTE ====================';
PRINT '';

-- 2. Resumen de créditos y promedio por estudiante
SELECT 
    e.codigo,
    e.nombres + ' ' + e.apellidos as estudiante,
    e.creditos_aprobados as creditos_acumulados,
    e.promedio_ponderado as promedio_acumulado,
    COUNT(CASE WHEN m.estado = 'Matriculado' THEN 1 END) as cursos_matriculados,
    COUNT(CASE WHEN m.estado = 'Retirado' THEN 1 END) as cursos_retirados,
    COUNT(CASE WHEN m.estado = 'Aprobado' THEN 1 END) as cursos_aprobados,
    COUNT(CASE WHEN m.estado = 'Desaprobado' THEN 1 END) as cursos_desaprobados
FROM Estudiante e
LEFT JOIN Matricula m ON m.idEstudiante = e.id
GROUP BY 
    e.id,
    e.codigo,
    e.nombres,
    e.apellidos,
    e.creditos_aprobados,
    e.promedio_ponderado
ORDER BY e.codigo;

PRINT '';
PRINT '==================== CURSOS REPETIDOS ====================';
PRINT '';

-- 3. Identificar cursos repetidos y mostrar cuál se cuenta
SELECT 
    e.codigo,
    e.nombres + ' ' + e.apellidos as estudiante,
    c.curso,
    m.estado,
    m.promedio_final,
    p.nombre as periodo,
    ROW_NUMBER() OVER (PARTITION BY e.id, c.id ORDER BY m.promedio_final DESC) as ranking,
    CASE 
        WHEN ROW_NUMBER() OVER (PARTITION BY e.id, c.id ORDER BY m.promedio_final DESC) = 1 
             AND m.estado = 'Aprobado' 
        THEN '✅ ESTA NOTA SE USA (MEJOR PROMEDIO)'
        WHEN m.estado = 'Retirado' THEN '❌ RETIRADO - NO CUENTA'
        ELSE '⚠️ NO SE USA (HAY MEJOR NOTA)'
    END as estado_calculo
FROM Estudiante e
INNER JOIN Matricula m ON m.idEstudiante = e.id
INNER JOIN Curso c ON m.idCurso = c.id
INNER JOIN Periodo p ON m.idPeriodo = p.id
WHERE c.id IN (
    -- Cursos que el estudiante tomó más de una vez
    SELECT m2.idCurso
    FROM Matricula m2
    WHERE m2.idEstudiante = e.id
    GROUP BY m2.idCurso
    HAVING COUNT(*) > 1
)
ORDER BY e.codigo, c.curso, m.promedio_final DESC;

PRINT '';
PRINT '==================== CÁLCULO MANUAL VS SISTEMA ====================';
PRINT '';

-- 4. Verificar que el promedio acumulado sea correcto
WITH MejoresNotas AS (
    SELECT 
        m.idEstudiante,
        m.idCurso,
        MAX(m.promedio_final) as mejor_promedio,
        MAX(c.creditos) as creditos
    FROM Matricula m
    INNER JOIN Curso c ON m.idCurso = c.id
    WHERE m.estado = 'Aprobado'
      AND m.estado != 'Retirado'  -- EXCLUIR RETIRADOS
      AND m.promedio_final IS NOT NULL
      AND m.promedio_final >= 10.5
    GROUP BY m.idEstudiante, m.idCurso
)
SELECT 
    e.codigo,
    e.nombres + ' ' + e.apellidos as estudiante,
    e.creditos_aprobados as creditos_sistema,
    SUM(mn.creditos) as creditos_calculado,
    e.promedio_ponderado as promedio_sistema,
    ROUND(SUM(mn.mejor_promedio * mn.creditos) / NULLIF(SUM(mn.creditos), 0), 2) as promedio_calculado,
    CASE 
        WHEN e.creditos_aprobados = SUM(mn.creditos) 
             AND ABS(e.promedio_ponderado - ROUND(SUM(mn.mejor_promedio * mn.creditos) / NULLIF(SUM(mn.creditos), 0), 2)) < 0.01
        THEN '✅ CORRECTO'
        ELSE '❌ DIFERENCIA - VERIFICAR'
    END as validacion
FROM Estudiante e
LEFT JOIN MejoresNotas mn ON mn.idEstudiante = e.id
GROUP BY 
    e.codigo,
    e.nombres,
    e.apellidos,
    e.creditos_aprobados,
    e.promedio_ponderado
HAVING e.creditos_aprobados > 0 OR SUM(mn.creditos) > 0
ORDER BY e.codigo;

PRINT '';
PRINT '✅ Verificación completada';
