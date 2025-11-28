-- Script para verificar los pesos de las notas y sincronizarlos con TipoEvaluacion

-- 1. Ver los pesos configurados en TipoEvaluacion
SELECT 
    c.curso AS Curso,
    te.nombre AS TipoEvaluacion,
    te.peso AS PesoConfigurado,
    te.activo
FROM dbo.TipoEvaluacion te
INNER JOIN dbo.Curso c ON te.id_curso = c.id
ORDER BY c.curso, te.orden;

-- 2. Ver las notas con sus pesos actuales
SELECT 
    e.nombres + ' ' + e.apellidos AS Estudiante,
    c.curso AS Curso,
    n.tipo_evaluacion AS TipoEvaluacion,
    n.nota AS NotaValor,
    n.peso AS PesoEnNota
FROM dbo.Nota n
INNER JOIN dbo.Matricula m ON n.idMatricula = m.id
INNER JOIN dbo.Estudiante e ON m.idEstudiante = e.id
INNER JOIN dbo.Curso c ON m.idCurso = c.id
ORDER BY c.curso, e.apellidos, n.tipo_evaluacion;

-- 3. Detectar inconsistencias: notas con peso diferente al configurado
SELECT 
    e.nombres + ' ' + e.apellidos AS Estudiante,
    c.curso AS Curso,
    n.tipo_evaluacion AS TipoEvaluacion,
    n.peso AS PesoEnNota,
    te.peso AS PesoConfigurado,
    (te.peso - n.peso) AS Diferencia
FROM dbo.Nota n
INNER JOIN dbo.Matricula m ON n.idMatricula = m.id
INNER JOIN dbo.Estudiante e ON m.idEstudiante = e.id
INNER JOIN dbo.Curso c ON m.idCurso = c.id
LEFT JOIN dbo.TipoEvaluacion te ON te.id_curso = c.id 
    AND te.nombre = n.tipo_evaluacion
WHERE te.peso IS NOT NULL 
    AND n.peso != te.peso
ORDER BY c.curso, e.apellidos;

-- 4. SINCRONIZAR todos los pesos de las notas con la configuración actual
-- Este UPDATE corrige todas las notas para que tengan el peso correcto
UPDATE n
SET n.peso = te.peso
FROM dbo.Nota n
INNER JOIN dbo.Matricula m ON n.idMatricula = m.id
INNER JOIN dbo.Curso c ON m.idCurso = c.id
INNER JOIN dbo.TipoEvaluacion te ON te.id_curso = c.id 
    AND te.nombre = n.tipo_evaluacion
WHERE te.activo = 1
    AND n.peso != te.peso;

-- 5. Verificar que se corrigieron los pesos
SELECT 
    COUNT(*) AS NotasCorregidas
FROM dbo.Nota n
INNER JOIN dbo.Matricula m ON n.idMatricula = m.id
INNER JOIN dbo.Curso c ON m.idCurso = c.id
INNER JOIN dbo.TipoEvaluacion te ON te.id_curso = c.id 
    AND te.nombre = n.tipo_evaluacion
WHERE te.activo = 1;

-- 6. Ver el promedio de cada estudiante después de la corrección
SELECT 
    e.codigo AS Codigo,
    e.nombres + ' ' + e.apellidos AS Estudiante,
    c.curso AS Curso,
    SUM(n.nota * (n.peso / 100.0)) AS PromedioCalculado
FROM dbo.Nota n
INNER JOIN dbo.Matricula m ON n.idMatricula = m.id
INNER JOIN dbo.Estudiante e ON m.idEstudiante = e.id
INNER JOIN dbo.Curso c ON m.idCurso = c.id
GROUP BY e.codigo, e.nombres, e.apellidos, c.curso
ORDER BY c.curso, e.apellidos;
