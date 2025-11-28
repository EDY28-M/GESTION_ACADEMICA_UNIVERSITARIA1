-- Script para corregir notas duplicadas y pesos incorrectos
-- Problema: Algunos estudiantes tienen notas duplicadas que hacen que los pesos sumen más de 100%

-- 1. Ver el problema actual para matrícula 12 (Junior Edinson)
SELECT 
    idMatricula,
    tipo_evaluacion,
    nota,
    peso,
    fecha_evaluacion
FROM Nota
WHERE idMatricula = 12
ORDER BY id;

-- Ver suma de pesos
SELECT 
    idMatricula,
    SUM(peso) as PesoTotal,
    COUNT(*) as CantidadNotas
FROM Nota
WHERE idMatricula = 12
GROUP BY idMatricula;

-- 2. SOLUCIÓN: Eliminar las notas duplicadas antiguas (las que tienen "Prácticas" en lugar de "Práctica")
-- Mantener solo las notas más recientes

-- Para matrícula 12: Eliminar "Prácticas" (id 12) y mantener "Práctica" (id 33)
DELETE FROM Nota WHERE id = 12; -- Prácticas (antigua)

-- Actualizar el peso del Examen Final de 25% a 20% para que sume 100%
UPDATE Nota SET peso = 20.00 WHERE id = 14 AND idMatricula = 12;

-- 3. Verificar que ahora suma 100%
SELECT 
    idMatricula,
    tipo_evaluacion,
    nota,
    peso,
    fecha_evaluacion,
    (nota * peso / 100.0) as Contribucion
FROM Nota
WHERE idMatricula = 12
ORDER BY tipo_evaluacion;

SELECT 
    idMatricula,
    SUM(peso) as PesoTotal,
    SUM(nota * peso / 100.0) as PromedioCalculado,
    COUNT(*) as CantidadNotas
FROM Nota
WHERE idMatricula = 12
GROUP BY idMatricula;

-- 4. Hacer lo mismo para matrícula 26 (que también tiene duplicados)
-- Ver el problema
SELECT 
    idMatricula,
    tipo_evaluacion,
    nota,
    peso,
    fecha_evaluacion
FROM Nota
WHERE idMatricula = 26
ORDER BY id;

-- Ver suma de pesos
SELECT 
    idMatricula,
    SUM(peso) as PesoTotal,
    COUNT(*) as CantidadNotas
FROM Nota
WHERE idMatricula = 26
GROUP BY idMatricula;

-- Para matrícula 26: Eliminar "Prácticas" (id 19) y mantener "Práctica" (id 34)
DELETE FROM Nota WHERE id = 19; -- Prácticas (antigua)

-- Actualizar el peso del Examen Final de 25% a 20% para que sume 100%
UPDATE Nota SET peso = 20.00 WHERE id = 21 AND idMatricula = 26;

-- Verificar que ahora suma 100%
SELECT 
    idMatricula,
    tipo_evaluacion,
    nota,
    peso,
    fecha_evaluacion,
    (nota * peso / 100.0) as Contribucion
FROM Nota
WHERE idMatricula = 26
ORDER BY tipo_evaluacion;

SELECT 
    idMatricula,
    SUM(peso) as PesoTotal,
    SUM(nota * peso / 100.0) as PromedioCalculado,
    COUNT(*) as CantidadNotas
FROM Nota
WHERE idMatricula = 26
GROUP BY idMatricula;

-- 5. Verificar todas las matrículas que tengan pesos que no suman 100%
SELECT 
    m.id as IdMatricula,
    e.Codigo,
    e.Nombres + ' ' + e.Apellidos as NombreEstudiante,
    c.curso as NombreCurso,
    SUM(n.peso) as PesoTotal,
    COUNT(*) as CantidadNotas,
    SUM(n.nota * n.peso / 100.0) as PromedioActual
FROM Nota n
INNER JOIN Matricula m ON n.idMatricula = m.id
INNER JOIN Estudiante e ON m.idEstudiante = e.id
INNER JOIN Curso c ON m.idCurso = c.id
GROUP BY m.id, e.Codigo, e.Nombres, e.Apellidos, c.curso
HAVING ABS(SUM(n.peso) - 100.0) > 0.01  -- Pesos que no suman exactamente 100%
ORDER BY PesoTotal DESC;
