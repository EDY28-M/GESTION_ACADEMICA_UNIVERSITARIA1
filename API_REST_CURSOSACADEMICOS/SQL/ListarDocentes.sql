-- =============================================
-- Script para LISTAR TODOS LOS DOCENTES
-- Muestra información completa de los docentes
-- =============================================

USE [GestionAcademica]
GO

PRINT '╔═══════════════════════════════════════════════════════════════╗';
PRINT '║                   LISTADO DE DOCENTES                         ║';
PRINT '╚═══════════════════════════════════════════════════════════════╝';
PRINT '';

-- =============================================
-- 1. LISTADO COMPLETO DE DOCENTES
-- =============================================
SELECT 
    d.id AS ID,
    d.nombres AS Nombres,
    d.apellidos AS Apellidos,
    d.nombres + ' ' + d.apellidos AS [Nombre Completo],
    d.profesion AS Profesión,
    d.correo AS Correo,
    d.fecha_nacimiento AS [Fecha Nacimiento],
    d.fecha_creacion AS [Fecha Registro],
    COUNT(c.id) AS [Cantidad Cursos]
FROM Docente d
LEFT JOIN Curso c ON d.id = c.idDocente
GROUP BY 
    d.id, 
    d.nombres, 
    d.apellidos, 
    d.profesion, 
    d.correo, 
    d.fecha_nacimiento,
    d.fecha_creacion
ORDER BY d.apellidos, d.nombres;

PRINT '';
PRINT '═══════════════════════════════════════════════════════════════';
PRINT '';

-- =============================================
-- 2. RESUMEN DE DOCENTES
-- =============================================
PRINT '▶ RESUMEN ESTADÍSTICO:';
PRINT '';

DECLARE @TotalDocentes INT;
DECLARE @DocentesConCursos INT;
DECLARE @DocentesSinCursos INT;
DECLARE @TotalCursosAsignados INT;

SELECT @TotalDocentes = COUNT(*) FROM Docente;
SELECT @DocentesConCursos = COUNT(DISTINCT idDocente) FROM Curso WHERE idDocente IS NOT NULL;
SET @DocentesSinCursos = @TotalDocentes - @DocentesConCursos;
SELECT @TotalCursosAsignados = COUNT(*) FROM Curso WHERE idDocente IS NOT NULL;

PRINT 'Total de docentes: ' + CAST(@TotalDocentes AS VARCHAR(10));
PRINT 'Docentes con cursos asignados: ' + CAST(@DocentesConCursos AS VARCHAR(10));
PRINT 'Docentes sin cursos asignados: ' + CAST(@DocentesSinCursos AS VARCHAR(10));
PRINT 'Total de cursos asignados: ' + CAST(@TotalCursosAsignados AS VARCHAR(10));

IF @TotalDocentes > 0
BEGIN
    DECLARE @PromedioCursosPorDocente DECIMAL(10,2);
    SET @PromedioCursosPorDocente = CAST(@TotalCursosAsignados AS DECIMAL(10,2)) / @TotalDocentes;
    PRINT 'Promedio de cursos por docente: ' + CAST(@PromedioCursosPorDocente AS VARCHAR(10));
END

PRINT '';
PRINT '═══════════════════════════════════════════════════════════════';
PRINT '';

-- =============================================
-- 3. DOCENTES CON SUS CURSOS ASIGNADOS
-- =============================================
PRINT '▶ DOCENTES Y SUS CURSOS ASIGNADOS:';
PRINT '';

SELECT 
    d.id AS [ID Docente],
    d.nombres + ' ' + d.apellidos AS Docente,
    d.profesion AS Profesión,
    c.id AS [ID Curso],
    c.codigo AS Código,
    c.curso AS Curso,
    c.creditos AS Créditos,
    c.ciclo AS Ciclo
FROM Docente d
LEFT JOIN Curso c ON d.id = c.idDocente
ORDER BY d.apellidos, d.nombres, c.ciclo, c.codigo;

PRINT '';
PRINT '═══════════════════════════════════════════════════════════════';
PRINT '';

-- =============================================
-- 4. DOCENTES SIN CURSOS ASIGNADOS
-- =============================================
PRINT '▶ DOCENTES SIN CURSOS ASIGNADOS:';
PRINT '';

IF EXISTS (
    SELECT 1 
    FROM Docente d 
    LEFT JOIN Curso c ON d.id = c.idDocente 
    WHERE c.id IS NULL
)
BEGIN
    SELECT 
        d.id AS ID,
        d.nombres + ' ' + d.apellidos AS [Nombre Completo],
        d.profesion AS Profesión,
        d.correo AS Correo
    FROM Docente d
    LEFT JOIN Curso c ON d.id = c.idDocente
    WHERE c.id IS NULL
    ORDER BY d.apellidos, d.nombres;
END
ELSE
BEGIN
    PRINT '(No hay docentes sin cursos asignados)';
END

PRINT '';
PRINT '═══════════════════════════════════════════════════════════════';
PRINT '';

-- =============================================
-- 5. TOP 5 DOCENTES CON MÁS CURSOS
-- =============================================
PRINT '▶ TOP 5 DOCENTES CON MÁS CURSOS:';
PRINT '';

SELECT TOP 5
    d.id AS ID,
    d.nombres + ' ' + d.apellidos AS Docente,
    d.profesion AS Profesión,
    COUNT(c.id) AS [Cantidad Cursos]
FROM Docente d
LEFT JOIN Curso c ON d.id = c.idDocente
GROUP BY d.id, d.nombres, d.apellidos, d.profesion
HAVING COUNT(c.id) > 0
ORDER BY COUNT(c.id) DESC, d.apellidos;

PRINT '';
PRINT '╔═══════════════════════════════════════════════════════════════╗';
PRINT '║                   FIN DEL REPORTE                             ║';
PRINT '╚═══════════════════════════════════════════════════════════════╝';
