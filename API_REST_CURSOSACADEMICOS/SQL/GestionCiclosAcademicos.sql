USE [GestionAcademica]
GO

-- =============================================
-- GESTIÓN AUTOMÁTICA DE CICLOS ACADÉMICOS
-- =============================================
-- Este script implementa:
-- 1. Índice único para un solo periodo activo
-- 2. SP para cerrar periodo (calcular promedios y créditos)
-- 3. SP para abrir nuevo periodo (avanzar ciclos)
-- 4. Función para obtener cursos disponibles por ciclo
-- 5. Vistas para el panel de administración
-- =============================================

-- =============================================
-- PASO 0: Agregar campos necesarios a Estudiante
-- =============================================
-- Verificar y agregar promedio_semestral
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Estudiante]') AND name = 'promedio_semestral')
BEGIN
    ALTER TABLE [dbo].[Estudiante] ADD promedio_semestral DECIMAL(10,4) NULL;
    PRINT 'Campo promedio_semestral agregado a tabla Estudiante';
END
GO

-- Verificar y agregar promedio_acumulado
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Estudiante]') AND name = 'promedio_acumulado')
BEGIN
    ALTER TABLE [dbo].[Estudiante] ADD promedio_acumulado DECIMAL(10,4) NULL;
    PRINT 'Campo promedio_acumulado agregado a tabla Estudiante';
END
GO

-- Verificar y agregar id_periodo_ultimo
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Estudiante]') AND name = 'id_periodo_ultimo')
BEGIN
    ALTER TABLE [dbo].[Estudiante] ADD id_periodo_ultimo INT NULL;
    ALTER TABLE [dbo].[Estudiante] ADD CONSTRAINT [FK_Estudiante_PeriodoUltimo] 
        FOREIGN KEY([id_periodo_ultimo]) REFERENCES [dbo].[Periodo] ([id]);
    PRINT 'Campo id_periodo_ultimo agregado a tabla Estudiante';
END
GO

-- =============================================
-- PASO 1: Garantizar un solo periodo activo
-- =============================================
-- Índice único para asegurar que solo un periodo esté activo
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_Periodo_Activo')
BEGIN
    DROP INDEX UX_Periodo_Activo ON dbo.Periodo;
    PRINT 'Índice UX_Periodo_Activo eliminado';
END
GO

CREATE UNIQUE INDEX UX_Periodo_Activo ON dbo.Periodo(activo)
WHERE activo = 1;
GO
PRINT 'Índice único UX_Periodo_Activo creado - Solo un periodo puede estar activo';
GO

-- =============================================
-- PASO 2: SP para CERRAR PERIODO
-- =============================================
-- Calcula promedios finales, créditos aprobados,
-- promedios semestrales y acumulados
-- =============================================

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_CerrarPeriodo')
BEGIN
    DROP PROCEDURE dbo.sp_CerrarPeriodo;
END
GO

CREATE PROCEDURE dbo.sp_CerrarPeriodo 
    @IdPeriodo INT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        PRINT '=== INICIANDO CIERRE DE PERIODO ' + CAST(@IdPeriodo AS VARCHAR) + ' ===';
        
        -- 1) Calcular promedios finales por matrícula del periodo
        PRINT 'Paso 1: Calculando promedios finales por matrícula...';
        
        ;WITH wNotas AS (
            SELECT  
                m.id            AS idMatricula,
                c.id            AS idCurso,
                SUM(n.nota * ISNULL(n.peso, te.peso)) AS sum_np,
                SUM(ISNULL(n.peso, te.peso))           AS sum_peso
            FROM dbo.Matricula m
            JOIN dbo.Curso     c  ON c.id = m.idCurso
            LEFT JOIN dbo.Nota n  ON n.idMatricula = m.id
            LEFT JOIN dbo.TipoEvaluacion te 
                   ON te.id_curso = c.id AND te.nombre = n.tipo_evaluacion AND te.activo = 1
            WHERE m.idPeriodo = @IdPeriodo
            GROUP BY m.id, c.id
        )
        UPDATE m
            SET m.promedio_final = CASE 
                WHEN w.sum_peso IS NULL OR w.sum_peso = 0 
                    THEN NULL 
                    ELSE ROUND(w.sum_np / w.sum_peso, 0) 
                END
        FROM dbo.Matricula m
        JOIN wNotas w ON w.idMatricula = m.id;
        
        PRINT '✓ Promedios finales calculados';

        -- 2) Calcular créditos aprobados y promedios del periodo por estudiante
        PRINT 'Paso 2: Calculando créditos aprobados y promedios semestrales...';
        
        ;WITH wCursos AS (
            SELECT  
                m.idEstudiante,
                SUM(CASE WHEN m.promedio_final >= 11 THEN c.creditos ELSE 0 END) AS creditos_aprobados_periodo,
                SUM(c.creditos)                                                   AS creditos_cursados_periodo,
                SUM(m.promedio_final * c.creditos)                                AS suma_promxcred
            FROM dbo.Matricula m
            JOIN dbo.Curso c ON c.id = m.idCurso
            WHERE m.idPeriodo = @IdPeriodo
              AND m.promedio_final IS NOT NULL  -- Solo contar los que tienen notas
            GROUP BY m.idEstudiante
        )
        -- Actualizar créditos, promedio_semestral y acumulado
        MERGE dbo.Estudiante AS T
        USING (
            SELECT 
                e.id AS idEst, 
                w.creditos_aprobados_periodo,
                CASE 
                    WHEN w.creditos_cursados_periodo > 0 
                        THEN CAST(w.suma_promxcred AS DECIMAL(10,4)) / w.creditos_cursados_periodo 
                    ELSE NULL 
                END AS prom_semestral
            FROM dbo.Estudiante e
            JOIN wCursos w ON w.idEstudiante = e.id
        ) AS S
        ON T.id = S.idEst
        WHEN MATCHED THEN UPDATE SET
            T.creditos_aprobados = ISNULL(T.creditos_aprobados, 0) + ISNULL(S.creditos_aprobados_periodo, 0),
            T.promedio_semestral = S.prom_semestral,
            T.promedio_acumulado = CASE 
                WHEN ISNULL(T.creditos_aprobados, 0) + ISNULL(S.creditos_aprobados_periodo, 0) = 0 
                     OR S.prom_semestral IS NULL
                    THEN T.promedio_acumulado
                ELSE 
                    ROUND( 
                        ( ISNULL(T.promedio_acumulado, 0) * ISNULL(T.creditos_aprobados, 0)
                          + ISNULL(S.prom_semestral, 0)  * ISNULL(S.creditos_aprobados_periodo, 0)
                        )
                        / NULLIF( ISNULL(T.creditos_aprobados, 0) + ISNULL(S.creditos_aprobados_periodo, 0), 0 ), 2)
            END,
            T.id_periodo_ultimo = @IdPeriodo;
        
        PRINT '✓ Créditos y promedios actualizados';

        -- 3) Marcar periodo como cerrado
        PRINT 'Paso 3: Marcando periodo como cerrado...';
        
        UPDATE dbo.Periodo 
        SET activo = 0 
        WHERE id = @IdPeriodo;
        
        PRINT '✓ Periodo marcado como cerrado';
        
        COMMIT TRANSACTION;
        PRINT '=== CIERRE DE PERIODO COMPLETADO EXITOSAMENTE ===';
        
        -- Mostrar resumen
        SELECT 
            COUNT(DISTINCT m.idEstudiante) AS total_estudiantes,
            COUNT(m.id) AS total_matriculas,
            SUM(CASE WHEN m.promedio_final >= 11 THEN 1 ELSE 0 END) AS cursos_aprobados,
            SUM(CASE WHEN m.promedio_final < 11 AND m.promedio_final IS NOT NULL THEN 1 ELSE 0 END) AS cursos_desaprobados
        FROM dbo.Matricula m
        WHERE m.idPeriodo = @IdPeriodo;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        PRINT 'ERROR al cerrar periodo: ' + @ErrorMessage;
        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

PRINT 'Procedimiento sp_CerrarPeriodo creado exitosamente';
GO

-- =============================================
-- PASO 3: SP para ABRIR NUEVO PERIODO
-- =============================================
-- Activa el nuevo periodo y avanza ciclo solo
-- a estudiantes con al menos una nota en el periodo anterior
-- =============================================

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_AbrirPeriodo')
BEGIN
    DROP PROCEDURE dbo.sp_AbrirPeriodo;
END
GO

CREATE PROCEDURE dbo.sp_AbrirPeriodo 
    @IdPeriodoNuevo INT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        PRINT '=== INICIANDO APERTURA DE PERIODO ' + CAST(@IdPeriodoNuevo AS VARCHAR) + ' ===';
        
        -- 1) Garantizar único periodo activo
        PRINT 'Paso 1: Desactivando periodos anteriores...';
        
        UPDATE dbo.Periodo 
        SET activo = 0 
        WHERE activo = 1;
        
        UPDATE dbo.Periodo 
        SET activo = 1 
        WHERE id = @IdPeriodoNuevo;
        
        PRINT '✓ Periodo ' + CAST(@IdPeriodoNuevo AS VARCHAR) + ' activado';

        -- 2) Avanzar ciclo solo si tuvo evaluación en su último periodo
        PRINT 'Paso 2: Avanzando ciclo de estudiantes con evaluaciones...';
        
        ;WITH wUltimo AS (
            SELECT e.id AS idEst, e.id_periodo_ultimo
            FROM dbo.Estudiante e
            WHERE e.id_periodo_ultimo IS NOT NULL
        ),
        wTuvoNotas AS (
            SELECT m.idEstudiante
            FROM dbo.Matricula m
            JOIN wUltimo u ON u.idEst = m.idEstudiante AND u.id_periodo_ultimo = m.idPeriodo
            WHERE m.promedio_final IS NOT NULL
            GROUP BY m.idEstudiante
        )
        UPDATE e 
        SET e.ciclo_actual = ISNULL(e.ciclo_actual, 0) + 1
        FROM dbo.Estudiante e
        WHERE EXISTS (SELECT 1 FROM wTuvoNotas t WHERE t.idEstudiante = e.id);
        
        DECLARE @estudiantesAvanzados INT;
        SELECT @estudiantesAvanzados = @@ROWCOUNT;
        PRINT '✓ ' + CAST(@estudiantesAvanzados AS VARCHAR) + ' estudiantes avanzaron de ciclo';

        -- 3) Actualizar último periodo a todos los estudiantes activos
        PRINT 'Paso 3: Actualizando referencia de último periodo...';
        
        UPDATE dbo.Estudiante 
        SET id_periodo_ultimo = @IdPeriodoNuevo
        WHERE estado = 'Activo';
        
        PRINT '✓ Referencias actualizadas';
        
        COMMIT TRANSACTION;
        PRINT '=== APERTURA DE PERIODO COMPLETADA EXITOSAMENTE ===';
        
        -- Mostrar resumen de ciclos
        SELECT 
            ciclo_actual,
            COUNT(*) AS cantidad_estudiantes
        FROM dbo.Estudiante
        WHERE estado = 'Activo'
        GROUP BY ciclo_actual
        ORDER BY ciclo_actual;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        PRINT 'ERROR al abrir periodo: ' + @ErrorMessage;
        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

PRINT 'Procedimiento sp_AbrirPeriodo creado exitosamente';
GO

-- =============================================
-- PASO 4: FUNCIÓN para obtener cursos disponibles
-- =============================================
-- Retorna cursos del ciclo actual del estudiante
-- que NO ha aprobado previamente
-- =============================================

IF EXISTS (SELECT * FROM sys.objects WHERE type IN ('IF', 'FN', 'TF') AND name = 'fn_CursosDisponibles')
BEGIN
    DROP FUNCTION dbo.fn_CursosDisponibles;
END
GO

CREATE FUNCTION dbo.fn_CursosDisponibles(@IdEstudiante INT)
RETURNS TABLE
AS
RETURN
(
    WITH PeriodoActivo AS (
        SELECT TOP 1 ciclo FROM dbo.Periodo WHERE activo = 1
    )
    SELECT 
        c.id, 
        c.curso, 
        c.creditos, 
        c.ciclo, 
        c.horasSemanal,
        c.idDocente,
        c.semestre,
        d.nombres AS docente_nombres,
        d.apellidos AS docente_apellidos
    FROM dbo.Estudiante e
    CROSS JOIN PeriodoActivo p
    JOIN dbo.Curso c ON (
        (
            -- Cursos del ciclo actual que coinciden con el semestre activo
            c.ciclo = e.ciclo_actual
            AND (c.semestre = p.ciclo OR c.semestre IS NULL)
        )
        OR
        (
            -- Cursos reprobados de ciclos anteriores que coinciden con el semestre activo
            c.ciclo < e.ciclo_actual
            AND (c.semestre = p.ciclo OR c.semestre IS NULL)
            AND EXISTS (
                SELECT 1 
                FROM dbo.Matricula m 
                WHERE m.idEstudiante = e.id 
                  AND m.idCurso = c.id
                  AND m.promedio_final IS NOT NULL
                  AND m.promedio_final < 11  -- Reprobado
            )
        )
    )
    LEFT JOIN dbo.Docente d ON d.id = c.idDocente
    WHERE e.id = @IdEstudiante
      -- Excluir cursos ya aprobados
      AND NOT EXISTS (
            SELECT 1
            FROM dbo.Matricula m
            WHERE m.idEstudiante = e.id 
              AND m.idCurso = c.id
              AND m.promedio_final >= 11
      )
      -- VALIDAR PREREQUISITOS: Solo mostrar si todos los prerequisitos están aprobados
      AND NOT EXISTS (
            SELECT 1
            FROM dbo.CursoPrerequisito cp
            WHERE cp.idCurso = c.id
              AND NOT EXISTS (
                    SELECT 1
                    FROM dbo.Matricula m
                    WHERE m.idEstudiante = e.id
                      AND m.idCurso = cp.idCursoPrerequisito
                      AND m.promedio_final IS NOT NULL
                      AND m.promedio_final >= 11  -- Prerequisito aprobado
              )
      )
);
GO

PRINT 'Función fn_CursosDisponibles creada exitosamente';
GO

-- =============================================
-- PASO 5a: VISTA de Estado Actual del Estudiante
-- =============================================
-- Muestra información consolidada para el panel Admin
-- =============================================

IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_EstadoActualEstudiante')
BEGIN
    DROP VIEW dbo.vw_EstadoActualEstudiante;
END
GO

CREATE VIEW dbo.vw_EstadoActualEstudiante AS
SELECT 
    e.id,
    e.codigo,
    e.nombres, 
    e.apellidos,
    e.ciclo_actual,
    e.creditos_aprobados,
    e.promedio_semestral,
    e.promedio_acumulado,
    e.estado,
    p.id         AS idPeriodoActivo,
    p.nombre     AS periodo_activo,
    p.anio       AS periodo_anio,
    p.ciclo      AS periodo_ciclo,
    COUNT(m.id)  AS cursos_activos,
    SUM(CASE WHEN m.promedio_final >= 11 THEN 1 ELSE 0 END) AS cursos_aprobados_periodo,
    SUM(CASE WHEN m.promedio_final < 11 AND m.promedio_final IS NOT NULL THEN 1 ELSE 0 END) AS cursos_desaprobados_periodo
FROM dbo.Estudiante e
LEFT JOIN dbo.Periodo p ON p.activo = 1
LEFT JOIN dbo.Matricula m ON m.idEstudiante = e.id AND m.idPeriodo = p.id
GROUP BY 
    e.id, e.codigo, e.nombres, e.apellidos, e.ciclo_actual, 
    e.creditos_aprobados, e.promedio_semestral, e.promedio_acumulado, 
    e.estado, p.id, p.nombre, p.anio, p.ciclo;
GO

PRINT 'Vista vw_EstadoActualEstudiante creada exitosamente';
GO

-- =============================================
-- PASO 5b: VISTA de Cursos Disponibles
-- =============================================
-- Lista todos los cursos disponibles por estudiante
-- =============================================

IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_CursosDisponibles')
BEGIN
    DROP VIEW dbo.vw_CursosDisponibles;
END
GO

CREATE VIEW dbo.vw_CursosDisponibles AS
SELECT 
    e.id AS idEstudiante,
    e.codigo AS codigo_estudiante,
    e.nombres + ' ' + e.apellidos AS estudiante,
    e.ciclo_actual,
    c.*
FROM dbo.Estudiante e
CROSS APPLY dbo.fn_CursosDisponibles(e.id) c
WHERE e.estado = 'Activo';
GO

PRINT 'Vista vw_CursosDisponibles creada exitosamente';
GO

-- =============================================
-- EJEMPLOS DE USO
-- =============================================

PRINT '';
PRINT '=== EJEMPLOS DE USO ===';
PRINT '';
PRINT '-- 1. Ver cursos disponibles para un estudiante:';
PRINT '   SELECT * FROM dbo.fn_CursosDisponibles(@IdEstudiante);';
PRINT '';
PRINT '-- 2. Ver estado actual de todos los estudiantes:';
PRINT '   SELECT * FROM dbo.vw_EstadoActualEstudiante ORDER BY ciclo_actual DESC, codigo;';
PRINT '';
PRINT '-- 3. Cerrar periodo actual:';
PRINT '   DECLARE @IdPeriodoActual INT;';
PRINT '   SELECT @IdPeriodoActual = id FROM dbo.Periodo WHERE activo = 1;';
PRINT '   EXEC dbo.sp_CerrarPeriodo @IdPeriodo = @IdPeriodoActual;';
PRINT '';
PRINT '-- 4. Crear y abrir nuevo periodo:';
PRINT '   INSERT INTO dbo.Periodo (nombre, anio, ciclo, fecha_inicio, fecha_fin, activo)';
PRINT '   VALUES (''2025-II'', 2025, ''II'', ''2025-08-01'', ''2025-12-15'', 0);';
PRINT '   DECLARE @IdNuevoPeriodo INT = SCOPE_IDENTITY();';
PRINT '   EXEC dbo.sp_AbrirPeriodo @IdPeriodoNuevo = @IdNuevoPeriodo;';
PRINT '';
PRINT '==============================================';
PRINT 'INSTALACIÓN COMPLETADA EXITOSAMENTE';
PRINT '==============================================';
GO
