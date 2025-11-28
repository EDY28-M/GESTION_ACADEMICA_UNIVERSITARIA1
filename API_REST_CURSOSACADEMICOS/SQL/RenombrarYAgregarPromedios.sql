-- =============================================
-- Script: Renombrar promedio_ponderado a promedio_acumulado
--         y agregar promedio_semestral por período
-- =============================================

USE GestionAcademica;
GO

-- 1. Renombrar columna en tabla Estudiante
IF EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID('Estudiante') 
           AND name = 'promedio_ponderado')
BEGIN
    EXEC sp_rename 'Estudiante.promedio_ponderado', 'promedio_acumulado', 'COLUMN';
    PRINT '✅ Columna renombrada: Estudiante.promedio_ponderado → Estudiante.promedio_acumulado';
END
ELSE
BEGIN
    PRINT '⚠️  La columna Estudiante.promedio_ponderado no existe o ya fue renombrada';
END
GO

-- 2. Agregar columna promedio_semestral a tabla Estudiante para cada período
-- Esta columna almacenará el promedio del último período calculado
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID('Estudiante') 
               AND name = 'promedio_semestral')
BEGIN
    ALTER TABLE Estudiante
    ADD promedio_semestral DECIMAL(5,2) NULL;
    PRINT '✅ Columna agregada: Estudiante.promedio_semestral';
END
ELSE
BEGIN
    PRINT '⚠️  La columna Estudiante.promedio_semestral ya existe';
END
GO

-- 3. Agregar columna id_periodo_ultimo para trackear el período del promedio_semestral
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID('Estudiante') 
               AND name = 'id_periodo_ultimo')
BEGIN
    ALTER TABLE Estudiante
    ADD id_periodo_ultimo INT NULL;
    PRINT '✅ Columna agregada: Estudiante.id_periodo_ultimo';
END
ELSE
BEGIN
    PRINT '⚠️  La columna Estudiante.id_periodo_ultimo ya existe';
END
GO

-- 4. Recalcular promedio_semestral para cada estudiante en el período activo
PRINT '📊 Recalculando promedios semestrales...';

-- Obtener el período activo
DECLARE @periodoActivo INT;
SELECT TOP 1 @periodoActivo = id 
FROM Periodo 
WHERE activo = 1 
ORDER BY fecha_inicio DESC;

IF @periodoActivo IS NOT NULL
BEGIN
    UPDATE e
    SET 
        e.promedio_semestral = subq.promedioSemestre,
        e.id_periodo_ultimo = @periodoActivo
    FROM Estudiante e
    INNER JOIN (
        SELECT 
            m.idEstudiante,
            AVG(m.promedio_final) AS promedioSemestre
        FROM Matricula m
        WHERE m.idPeriodo = @periodoActivo
          AND m.estado IN ('Aprobado', 'Desaprobado', 'Matriculado')
          AND m.estado <> 'Retirado'
          AND m.promedio_final IS NOT NULL
        GROUP BY m.idEstudiante
    ) AS subq ON e.id = subq.idEstudiante;

    PRINT '✅ Promedios semestrales recalculados para el período activo: ' + CAST(@periodoActivo AS VARCHAR);
END
ELSE
BEGIN
    PRINT '⚠️  No hay período activo, no se recalcularon promedios semestrales';
END
GO

-- 5. Recalcular promedio_acumulado (antes promedio_ponderado)
-- Usando mejor nota por curso de TODOS los períodos
PRINT '📊 Recalculando promedios acumulados...';

UPDATE e
SET e.promedio_acumulado = subq.promedioAcumulado
FROM Estudiante e
INNER JOIN (
    SELECT 
        m.idEstudiante,
        -- Promedio ponderado por créditos usando mejor nota por curso
        SUM(mejorNota.promedio_final * c.creditos) / NULLIF(SUM(c.creditos), 0) AS promedioAcumulado
    FROM (
        -- Mejor nota por curso (cualquier período)
        SELECT 
            m2.idEstudiante,
            m2.idCurso,
            MAX(m2.promedio_final) AS promedio_final
        FROM Matricula m2
        WHERE m2.estado IN ('Aprobado', 'Desaprobado')
          AND m2.estado <> 'Retirado'
          AND m2.promedio_final IS NOT NULL
          AND m2.promedio_final >= 10.5  -- Solo cursos aprobados
        GROUP BY m2.idEstudiante, m2.idCurso
    ) AS mejorNota
    INNER JOIN Matricula m ON m.idEstudiante = mejorNota.idEstudiante 
                            AND m.idCurso = mejorNota.idCurso
                            AND m.promedio_final = mejorNota.promedio_final
    INNER JOIN Curso c ON m.idCurso = c.id
    GROUP BY m.idEstudiante
) AS subq ON e.id = subq.idEstudiante;

PRINT '✅ Promedios acumulados recalculados';
GO

-- 6. Verificación
PRINT '';
PRINT '📋 VERIFICACIÓN DE CAMBIOS:';
PRINT '-----------------------------------';

SELECT 
    e.codigo AS Codigo,
    e.nombres + ' ' + e.apellidos AS Estudiante,
    e.promedio_acumulado AS PromedioAcumulado,
    e.promedio_semestral AS PromedioSemestral,
    p.nombre AS PeriodoUltimo
FROM Estudiante e
LEFT JOIN Periodo p ON e.id_periodo_ultimo = p.id
WHERE e.promedio_acumulado IS NOT NULL OR e.promedio_semestral IS NOT NULL
ORDER BY e.codigo;

PRINT '';
PRINT '✅ Script completado exitosamente';
GO
