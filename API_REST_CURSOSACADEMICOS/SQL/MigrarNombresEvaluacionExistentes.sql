-- Script para migrar nombres de evaluaciones existentes
-- Este script actualiza las notas que ya existen en la base de datos
-- para que coincidan con los nombres configurados en TipoEvaluacion

-- IMPORTANTE: Ejecutar este script UNA SOLA VEZ después de configurar los tipos de evaluación

-- Ver las notas actuales y sus tipos
SELECT DISTINCT tipo_evaluacion, COUNT(*) as cantidad
FROM Nota
GROUP BY tipo_evaluacion
ORDER BY tipo_evaluacion;

-- Ver los tipos de evaluación configurados
SELECT te.id, te.idCurso, te.nombre, te.peso, te.orden, c.nombre_curso
FROM TipoEvaluacion te
INNER JOIN Curso c ON te.idCurso = c.id
WHERE te.activo = 1
ORDER BY c.nombre_curso, te.orden;

-- EJEMPLO: Si configuraste "EXAMEN PARCIAL 1" pero las notas dicen "Parcial 1"
-- UPDATE Nota 
-- SET tipo_evaluacion = 'EXAMEN PARCIAL 1', peso = 10
-- WHERE tipo_evaluacion = 'Parcial 1'
-- AND idMatricula IN (
--     SELECT id FROM Matricula WHERE idCurso = TU_ID_CURSO
-- );

-- EJEMPLO: Actualizar "Práctica" a "Prácticas"
-- UPDATE Nota 
-- SET tipo_evaluacion = 'Prácticas', peso = 20
-- WHERE tipo_evaluacion = 'Práctica'
-- OR tipo_evaluacion = 'practica';

-- EJEMPLO COMPLETO: Migrar todas las evaluaciones de un curso específico
-- Reemplaza @cursoId con el ID de tu curso

/*
DECLARE @cursoId INT = 1; -- Cambiar por tu ID de curso

-- Parcial 1
UPDATE n
SET n.tipo_evaluacion = te.nombre, n.peso = te.peso
FROM Nota n
INNER JOIN Matricula m ON n.idMatricula = m.id
INNER JOIN TipoEvaluacion te ON te.idCurso = m.idCurso
WHERE m.idCurso = @cursoId
AND te.orden = 1
AND (n.tipo_evaluacion LIKE '%Parcial%1%' OR n.tipo_evaluacion LIKE '%EP1%');

-- Parcial 2
UPDATE n
SET n.tipo_evaluacion = te.nombre, n.peso = te.peso
FROM Nota n
INNER JOIN Matricula m ON n.idMatricula = m.id
INNER JOIN TipoEvaluacion te ON te.idCurso = m.idCurso
WHERE m.idCurso = @cursoId
AND te.orden = 2
AND (n.tipo_evaluacion LIKE '%Parcial%2%' OR n.tipo_evaluacion LIKE '%EP2%');

-- Prácticas
UPDATE n
SET n.tipo_evaluacion = te.nombre, n.peso = te.peso
FROM Nota n
INNER JOIN Matricula m ON n.idMatricula = m.id
INNER JOIN TipoEvaluacion te ON te.idCurso = m.idCurso
WHERE m.idCurso = @cursoId
AND te.orden = 3
AND (n.tipo_evaluacion LIKE '%Pr_ctica%' OR n.tipo_evaluacion LIKE '%PR%');

-- Medio Curso
UPDATE n
SET n.tipo_evaluacion = te.nombre, n.peso = te.peso
FROM Nota n
INNER JOIN Matricula m ON n.idMatricula = m.id
INNER JOIN TipoEvaluacion te ON te.idCurso = m.idCurso
WHERE m.idCurso = @cursoId
AND te.orden = 4
AND (n.tipo_evaluacion LIKE '%Medio%Curso%' OR n.tipo_evaluacion LIKE '%MC%');

-- Examen Final
UPDATE n
SET n.tipo_evaluacion = te.nombre, n.peso = te.peso
FROM Nota n
INNER JOIN Matricula m ON n.idMatricula = m.id
INNER JOIN TipoEvaluacion te ON te.idCurso = m.idCurso
WHERE m.idCurso = @cursoId
AND te.orden = 5
AND (n.tipo_evaluacion LIKE '%Examen%Final%' OR n.tipo_evaluacion LIKE '%EF%');

-- Actitud
UPDATE n
SET n.tipo_evaluacion = te.nombre, n.peso = te.peso
FROM Nota n
INNER JOIN Matricula m ON n.idMatricula = m.id
INNER JOIN TipoEvaluacion te ON te.idCurso = m.idCurso
WHERE m.idCurso = @cursoId
AND te.orden = 6
AND (n.tipo_evaluacion LIKE '%Actitud%' OR n.tipo_evaluacion LIKE '%EA%');

-- Trabajos / Trabajo encargado
UPDATE n
SET n.tipo_evaluacion = te.nombre, n.peso = te.peso
FROM Nota n
INNER JOIN Matricula m ON n.idMatricula = m.id
INNER JOIN TipoEvaluacion te ON te.idCurso = m.idCurso
WHERE m.idCurso = @cursoId
AND te.orden = 7
AND (n.tipo_evaluacion LIKE '%Trabajo%' OR n.tipo_evaluacion LIKE '%TE%' OR n.tipo_evaluacion LIKE '%T%');

-- Ver resultado
SELECT n.id, e.nombres + ' ' + e.apellidos AS estudiante, 
       n.tipo_evaluacion, n.nota, n.peso
FROM Nota n
INNER JOIN Matricula m ON n.idMatricula = m.id
INNER JOIN Estudiante e ON m.idEstudiante = e.id
WHERE m.idCurso = @cursoId
ORDER BY e.apellidos, e.nombres, n.tipo_evaluacion;
*/
