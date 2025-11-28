-- ============================================
-- SCRIPT PARA RECALCULAR DATOS RETROACTIVOS
-- Ejecutar después de agregar la columna promedio_final
-- ============================================

USE GestionAcademica;
GO

-- PASO 1: Calcular y guardar promedio_final para matrículas que ya fueron cerradas
UPDATE m
SET m.promedio_final = (
    SELECT ROUND(
        SUM(n.nota * n.peso) / 100.0,
        2
    )
    FROM Notas n
    WHERE n.id_matricula = m.id_matricula
)
FROM Matriculas m
WHERE m.estado IN ('Aprobado', 'Desaprobado')
  AND m.promedio_final IS NULL
  AND EXISTS (
      SELECT 1 FROM Notas n 
      WHERE n.id_matricula = m.id_matricula
  );

PRINT 'Paso 1 completado: Promedios finales calculados';
PRINT CAST(@@ROWCOUNT AS VARCHAR) + ' matrículas actualizadas';
GO

-- PASO 2: Recalcular créditos acumulados para cada estudiante
UPDATE e
SET e.creditos_aprobados = (
    SELECT ISNULL(SUM(c.creditos), 0)
    FROM Matriculas m
    INNER JOIN Cursos c ON m.id_curso = c.id_curso
    WHERE m.id_estudiante = e.id_estudiante
      AND m.estado = 'Aprobado'
      AND m.promedio_final IS NOT NULL
)
FROM Estudiantes e;

PRINT 'Paso 2 completado: Créditos acumulados actualizados';
GO

-- PASO 3: Recalcular promedio ponderado para cada estudiante
UPDATE e
SET e.promedio_ponderado = (
    SELECT CASE 
        WHEN SUM(c.creditos) > 0 THEN
            ROUND(
                SUM(m.promedio_final * c.creditos) / SUM(c.creditos),
                2
            )
        ELSE 0
    END
    FROM Matriculas m
    INNER JOIN Cursos c ON m.id_curso = c.id_curso
    WHERE m.id_estudiante = e.id_estudiante
      AND m.estado = 'Aprobado'
      AND m.promedio_final IS NOT NULL
)
FROM Estudiantes e;

PRINT 'Paso 3 completado: Promedios ponderados actualizados';
GO

-- ============================================
-- CONSULTAS DE VERIFICACIÓN
-- ============================================

PRINT '';
PRINT '==================== RESULTADOS ====================';
PRINT '';

-- Ver matrículas con promedio calculado
SELECT 
    COUNT(*) as 'Total Matrículas con Promedio',
    AVG(promedio_final) as 'Promedio General'
FROM Matriculas
WHERE promedio_final IS NOT NULL
  AND estado IN ('Aprobado', 'Desaprobado');

-- Ver estudiantes con créditos y promedio
SELECT 
    e.id_estudiante,
    u.nombre,
    u.apellido,
    e.creditos_aprobados as 'Créditos',
    e.promedio_ponderado as 'Promedio',
    COUNT(m.id_matricula) as 'Cursos Aprobados'
FROM Estudiantes e
INNER JOIN Usuarios u ON e.id_usuario = u.id_usuario
LEFT JOIN Matriculas m ON m.id_estudiante = e.id_estudiante 
    AND m.estado = 'Aprobado'
GROUP BY 
    e.id_estudiante,
    u.nombre,
    u.apellido,
    e.creditos_aprobados,
    e.promedio_ponderado
ORDER BY e.id_estudiante;

-- Ver detalle de matrículas por estudiante
SELECT 
    e.id_estudiante,
    u.nombre + ' ' + u.apellido as 'Estudiante',
    c.nombre as 'Curso',
    c.creditos as 'Créditos',
    m.promedio_final as 'Promedio Final',
    m.estado as 'Estado',
    p.nombre as 'Período'
FROM Estudiantes e
INNER JOIN Usuarios u ON e.id_usuario = u.id_usuario
INNER JOIN Matriculas m ON m.id_estudiante = e.id_estudiante
INNER JOIN Cursos c ON m.id_curso = c.id_curso
INNER JOIN Periodos p ON m.id_periodo = p.id_periodo
WHERE m.estado IN ('Aprobado', 'Desaprobado')
ORDER BY e.id_estudiante, p.id_periodo, c.nombre;

PRINT '';
PRINT '==================== SCRIPT COMPLETADO ====================';
