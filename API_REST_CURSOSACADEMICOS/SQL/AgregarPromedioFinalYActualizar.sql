-- ============================================
-- SCRIPT PARA AGREGAR PROMEDIO FINAL Y CONFIGURAR LÓGICA DE ACUMULACIÓN
-- ============================================

USE GestionAcademica;
GO

-- PASO 1: Agregar columna promedio_final en Matricula si no existe
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'Matricula') 
               AND name = 'promedio_final')
BEGIN
    ALTER TABLE Matricula
    ADD promedio_final DECIMAL(5,2) NULL;
    
    PRINT '✅ Columna promedio_final agregada a Matricula';
END
ELSE
BEGIN
    PRINT '⚠️ La columna promedio_final ya existe en Matricula';
END
GO

-- PASO 2: Calcular y guardar promedios retroactivos para matrículas cerradas
-- IMPORTANTE: Solo para cursos NO retirados
UPDATE m
SET m.promedio_final = (
    SELECT ROUND(SUM(n.nota * n.peso) / 100.0, 2)
    FROM Nota n
    WHERE n.idMatricula = m.id
    GROUP BY n.idMatricula
)
FROM Matricula m
WHERE m.estado IN ('Aprobado', 'Desaprobado')
  AND m.estado != 'Retirado'  -- EXCLUIR RETIRADOS
  AND m.promedio_final IS NULL
  AND EXISTS (SELECT 1 FROM Nota WHERE idMatricula = m.id);

PRINT '✅ Promedios finales calculados para ' + CAST(@@ROWCOUNT AS VARCHAR) + ' matrículas';
GO

-- PASO 3: Crear índices para optimizar consultas
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Matricula_EstudianteCurso')
BEGIN
    CREATE INDEX IX_Matricula_EstudianteCurso 
    ON Matricula(idEstudiante, idCurso, promedio_final);
    PRINT '✅ Índice IX_Matricula_EstudianteCurso creado';
END
GO

-- PASO 4: Recalcular créditos y promedio acumulado de cada estudiante
DECLARE @estudianteId INT;
DECLARE @creditosAcumulados INT;
DECLARE @promedioAcumulado DECIMAL(5,2);

DECLARE estudiante_cursor CURSOR FOR
SELECT DISTINCT idEstudiante FROM Matricula WHERE estado IN ('Aprobado', 'Desaprobado');

OPEN estudiante_cursor;
FETCH NEXT FROM estudiante_cursor INTO @estudianteId;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Calcular créditos acumulados (sin duplicados de cursos repetidos)
    -- IMPORTANTE: EXCLUIR cursos retirados
    SELECT @creditosAcumulados = SUM(c.creditos)
    FROM (
        -- Seleccionar solo la matrícula con MEJOR NOTA por curso
        SELECT m.idCurso, MAX(m.promedio_final) as mejorNota
        FROM Matricula m
        WHERE m.idEstudiante = @estudianteId
          AND m.estado = 'Aprobado'
          AND m.estado != 'Retirado'  -- EXCLUIR RETIRADOS
          AND m.promedio_final IS NOT NULL
          AND m.promedio_final >= 10.5
        GROUP BY m.idCurso
    ) mejoresNotas
    INNER JOIN Curso c ON c.id = mejoresNotas.idCurso;

    -- Calcular promedio ponderado acumulado (usando mejores notas, SIN retirados)
    SELECT @promedioAcumulado = 
        CASE 
            WHEN SUM(c.creditos) > 0 THEN
                ROUND(
                    SUM(mejoresNotas.mejorNota * c.creditos) / SUM(c.creditos), 
                    2
                )
            ELSE 0
        END
    FROM (
        SELECT m.idCurso, MAX(m.promedio_final) as mejorNota
        FROM Matricula m
        WHERE m.idEstudiante = @estudianteId
          AND m.estado = 'Aprobado'
          AND m.estado != 'Retirado'  -- EXCLUIR RETIRADOS
          AND m.promedio_final IS NOT NULL
        GROUP BY m.idCurso
    ) mejoresNotas
    INNER JOIN Curso c ON c.id = mejoresNotas.idCurso;

    -- Actualizar estudiante
    UPDATE Estudiante
    SET 
        creditos_aprobados = ISNULL(@creditosAcumulados, 0),
        promedio_ponderado = ISNULL(@promedioAcumulado, 0)
    WHERE id = @estudianteId;

    FETCH NEXT FROM estudiante_cursor INTO @estudianteId;
END

CLOSE estudiante_cursor;
DEALLOCATE estudiante_cursor;

PRINT '✅ Créditos y promedios acumulados recalculados';
GO

-- ============================================
-- VERIFICACIÓN DE RESULTADOS
-- ============================================

PRINT '';
PRINT '==================== VERIFICACIÓN ====================';
PRINT '';

-- Ver estudiantes con sus estadísticas
SELECT 
    e.id,
    e.codigo,
    e.nombres + ' ' + e.apellidos as estudiante,
    e.creditos_aprobados as creditos,
    e.promedio_ponderado as promedio,
    COUNT(DISTINCT CASE WHEN m.estado = 'Aprobado' THEN m.idCurso END) as cursosAprobados,
    COUNT(CASE WHEN m.idCurso IN (
        SELECT m2.idCurso 
        FROM Matricula m2 
        WHERE m2.idEstudiante = e.id 
        GROUP BY m2.idCurso 
        HAVING COUNT(*) > 1
    ) THEN 1 END) as cursosRepetidos
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
PRINT '✅ Script completado exitosamente';
