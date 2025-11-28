-- Script simplificado para corregir pesos sin usar TipoEvaluacion

-- 1. Ver todas las tablas que existen
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

-- 2. Ver las notas actuales con sus pesos
SELECT 
    e.nombres + ' ' + e.apellidos AS Estudiante,
    c.curso AS Curso,
    n.tipo_evaluacion AS TipoEvaluacion,
    n.nota AS NotaValor,
    n.peso AS PesoActual
FROM Nota n
INNER JOIN Matricula m ON n.idMatricula = m.id
INNER JOIN Estudiante e ON m.idEstudiante = e.id
INNER JOIN Curso c ON m.idCurso = c.id
ORDER BY c.curso, e.apellidos, n.tipo_evaluacion;

-- 3. Ver promedio actual de cada estudiante
SELECT 
    e.codigo AS Codigo,
    e.nombres + ' ' + e.apellidos AS Estudiante,
    c.curso AS Curso,
    SUM(n.nota * (n.peso / 100.0)) AS PromedioCalculado,
    SUM(n.peso) AS SumaPesos
FROM Nota n
INNER JOIN Matricula m ON n.idMatricula = m.id
INNER JOIN Estudiante e ON m.idEstudiante = e.id
INNER JOIN Curso c ON m.idCurso = c.id
GROUP BY e.codigo, e.nombres, e.apellidos, c.curso
ORDER BY c.curso, e.apellidos;

-- 4. ACTUALIZAR PESOS MANUALMENTE según tu configuración
-- Reemplaza estos valores con los pesos correctos que configuraste

-- Ejemplo: Si tu configuración es:
-- PARCIAL 1 = 10%
-- PARCIAL 2 = 10%
-- PRÁCTICA = 20%
-- MEDIO CURSO = 20%
-- EXAMEN FINAL = 25%
-- ACTITUD = 5%
-- TRABAJO ENCARGADO = 10%

-- Descomentar y ajustar según tu configuración:
/*
UPDATE Nota SET peso = 10 WHERE tipo_evaluacion = 'PARCIAL 1';
UPDATE Nota SET peso = 10 WHERE tipo_evaluacion = 'PARCIAL 2';
UPDATE Nota SET peso = 20 WHERE tipo_evaluacion = 'PRÁCTICA';
UPDATE Nota SET peso = 20 WHERE tipo_evaluacion = 'MEDIO CURSO';
UPDATE Nota SET peso = 25 WHERE tipo_evaluacion = 'EXAMEN FINAL';
UPDATE Nota SET peso = 5 WHERE tipo_evaluacion = 'ACTITUD';
UPDATE Nota SET peso = 10 WHERE tipo_evaluacion = 'TRABAJO ENCARGADO';
*/

-- 5. Verificar que todos los pesos suman 100% por estudiante
SELECT 
    e.nombres + ' ' + e.apellidos AS Estudiante,
    c.curso AS Curso,
    SUM(n.peso) AS SumaPesos,
    CASE 
        WHEN SUM(n.peso) = 100 THEN 'OK'
        ELSE 'ERROR: No suma 100%'
    END AS Estado
FROM Nota n
INNER JOIN Matricula m ON n.idMatricula = m.id
INNER JOIN Estudiante e ON m.idEstudiante = e.id
INNER JOIN Curso c ON m.idCurso = c.id
GROUP BY e.nombres, e.apellidos, c.curso
ORDER BY c.curso, e.apellidos;
