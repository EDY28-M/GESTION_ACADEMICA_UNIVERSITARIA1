-- ============================================
-- CORRECCIÓN MANUAL: Avanzar ciclos de estudiantes
-- ============================================
-- IMPORTANTE: Ejecuta primero DiagnosticoCompleto.sql para ver quiénes deben avanzar
-- ============================================

PRINT '========================================';
PRINT 'CORRECCIÓN AUTOMÁTICA DE CICLOS';
PRINT '========================================';

-- Obtener el período anterior (el último cerrado antes del activo)
DECLARE @PeriodoAnteriorId INT = (
    SELECT TOP 1 id 
    FROM Periodo 
    WHERE activo = 0 
    ORDER BY fecha_fin DESC, fecha_creacion DESC
);

DECLARE @PeriodoActivoId INT = (
    SELECT TOP 1 id 
    FROM Periodo 
    WHERE activo = 1
);

PRINT 'Período anterior ID: ' + CAST(ISNULL(@PeriodoAnteriorId, 0) AS VARCHAR);
PRINT 'Período activo ID: ' + CAST(ISNULL(@PeriodoActivoId, 0) AS VARCHAR);
PRINT '';

-- Crear tabla temporal con estudiantes que deben avanzar
IF OBJECT_ID('tempdb..#EstudiantesAvanzar') IS NOT NULL
    DROP TABLE #EstudiantesAvanzar;

SELECT 
    e.id,
    e.codigo,
    e.nombres + ' ' + e.apellidos AS estudiante,
    e.ciclo_actual AS ciclo_actual,
    e.ciclo_actual + 1 AS ciclo_nuevo,
    COUNT(*) AS cursos_periodo_anterior,
    SUM(CASE WHEN m.estado = 'Aprobado' THEN 1 ELSE 0 END) AS cursos_aprobados,
    SUM(CASE WHEN m.estado = 'Retirado' THEN 1 ELSE 0 END) AS cursos_retirados
INTO #EstudiantesAvanzar
FROM Estudiante e
LEFT JOIN Matricula m ON m.idEstudiante = e.id AND m.idPeriodo = @PeriodoAnteriorId
GROUP BY e.id, e.codigo, e.nombres, e.apellidos, e.ciclo_actual
HAVING 
    -- Solo si aprobó TODOS los cursos (excluyendo retirados)
    SUM(CASE WHEN m.estado = 'Aprobado' THEN 1 ELSE 0 END) = COUNT(*) - SUM(CASE WHEN m.estado = 'Retirado' THEN 1 ELSE 0 END)
    AND (COUNT(*) - SUM(CASE WHEN m.estado = 'Retirado' THEN 1 ELSE 0 END)) > 0
    AND e.ciclo_actual < 10; -- Máximo 10 ciclos

-- Mostrar quiénes van a avanzar
PRINT '========================================';
PRINT 'ESTUDIANTES QUE AVANZARÁN DE CICLO:';
PRINT '========================================';

SELECT 
    codigo,
    estudiante,
    ciclo_actual AS [Ciclo Actual],
    ciclo_nuevo AS [Ciclo Nuevo],
    cursos_periodo_anterior AS [Cursos Período Anterior],
    cursos_aprobados AS [Aprobados],
    cursos_retirados AS [Retirados]
FROM #EstudiantesAvanzar
ORDER BY codigo;

-- Confirmar la acción
DECLARE @CantidadEstudiantes INT = (SELECT COUNT(*) FROM #EstudiantesAvanzar);
PRINT '';
PRINT 'Total de estudiantes a avanzar: ' + CAST(@CantidadEstudiantes AS VARCHAR);
PRINT '';

-- ACTUALIZAR LOS CICLOS
IF @CantidadEstudiantes > 0
BEGIN
    PRINT '========================================';
    PRINT 'ACTUALIZANDO CICLOS...';
    PRINT '========================================';
    
    UPDATE e
    SET e.ciclo_actual = ea.ciclo_nuevo
    FROM Estudiante e
    INNER JOIN #EstudiantesAvanzar ea ON ea.id = e.id;
    
    PRINT 'Ciclos actualizados exitosamente.';
    PRINT '';
    
    -- Mostrar resultado
    PRINT '========================================';
    PRINT 'RESULTADO FINAL:';
    PRINT '========================================';
    
    SELECT 
        e.codigo,
        e.nombres + ' ' + e.apellidos AS estudiante,
        e.ciclo_actual AS [Ciclo Nuevo],
        (SELECT COUNT(*) FROM Curso WHERE ciclo = e.ciclo_actual) AS [Cursos Disponibles en Nuevo Ciclo]
    FROM Estudiante e
    INNER JOIN #EstudiantesAvanzar ea ON ea.id = e.id
    ORDER BY e.codigo;
END
ELSE
BEGIN
    PRINT 'No hay estudiantes que deban avanzar de ciclo.';
    PRINT 'Posibles razones:';
    PRINT '- No aprobaron todos los cursos del período anterior';
    PRINT '- No tienen matrículas en el período anterior';
    PRINT '- Ya están en el ciclo correcto';
END

-- Limpiar
DROP TABLE #EstudiantesAvanzar;

PRINT '';
PRINT '========================================';
PRINT 'PROCESO COMPLETADO';
PRINT '========================================';
