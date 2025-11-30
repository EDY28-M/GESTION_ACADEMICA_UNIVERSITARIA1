-- Script para verificar las notas y pesos de los estudiantes

-- Ver todas las notas con sus pesos para el estudiante Junior Edinson
SELECT 
    e.Codigo,
    e.Nombres + ' ' + e.Apellidos AS NombreCompleto,
    c.NombreCurso,
    n.tipo_evaluacion AS TipoEvaluacion,
    n.nota AS NotaValor,
    n.peso AS Peso,
    n.fecha_evaluacion AS Fecha,
    (n.nota * n.peso / 100.0) AS Contribucion
FROM Nota n
INNER JOIN Matricula m ON n.idMatricula = m.id
INNER JOIN Estudiante e ON m.idEstudiante = e.id
INNER JOIN Curso c ON m.idCurso = c.id
WHERE e.Codigo = 'EST202510300009'  -- Junior Edinson
ORDER BY c.NombreCurso, n.tipo_evaluacion;

-- Calcular promedio del estudiante
SELECT 
    e.Codigo,
    e.Nombres + ' ' + e.Apellidos AS NombreCompleto,
    c.NombreCurso,
    SUM(n.nota * n.peso / 100.0) AS PromedioCalculado,
    SUM(n.peso) AS PesoTotal,
    COUNT(*) AS CantidadNotas
FROM Nota n
INNER JOIN Matricula m ON n.idMatricula = m.id
INNER JOIN Estudiante e ON m.idEstudiante = e.id
INNER JOIN Curso c ON m.idCurso = c.id
WHERE e.Codigo = 'EST202510300009'  -- Junior Edinson
GROUP BY e.Codigo, e.Nombres, e.Apellidos, c.NombreCurso;

-- Ver todas las notas con sus pesos para el estudiante Castaño Leon (que calcula bien)
SELECT 
    e.Codigo,
    e.Nombres + ' ' + e.Apellidos AS NombreCompleto,
    c.NombreCurso,
    n.tipo_evaluacion AS TipoEvaluacion,
    n.nota AS NotaValor,
    n.peso AS Peso,
    n.fecha_evaluacion AS Fecha,
    (n.nota * n.peso / 100.0) AS Contribucion
FROM Nota n
INNER JOIN Matricula m ON n.idMatricula = m.id
INNER JOIN Estudiante e ON m.idEstudiante = e.id
INNER JOIN Curso c ON m.idCurso = c.id
WHERE e.Codigo = 'EST202511020012'  -- Castaño Leon
ORDER BY c.NombreCurso, n.tipo_evaluacion;

-- Calcular promedio del estudiante Castaño Leon
SELECT 
    e.Codigo,
    e.Nombres + ' ' + e.Apellidos AS NombreCompleto,
    c.NombreCurso,
    SUM(n.nota * n.peso / 100.0) AS PromedioCalculado,
    SUM(n.peso) AS PesoTotal,
    COUNT(*) AS CantidadNotas
FROM Nota n
INNER JOIN Matricula m ON n.idMatricula = m.id
INNER JOIN Estudiante e ON m.idEstudiante = e.id
INNER JOIN Curso c ON m.idCurso = c.id
WHERE e.Codigo = 'EST202511020012'  -- Castaño Leon
GROUP BY e.Codigo, e.Nombres, e.Apellidos, c.NombreCurso;
