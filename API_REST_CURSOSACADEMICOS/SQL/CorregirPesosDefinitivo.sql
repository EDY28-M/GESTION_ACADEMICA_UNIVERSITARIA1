-- Script para corregir los pesos eliminando "Trabajos" y dejando solo "Trabajo encargado"
-- Configuración correcta (debe sumar 100%):
-- Parcial 1: 10%
-- Parcial 2: 10%
-- Práctica: 20%
-- Medio Curso: 20%
-- Examen Final: 20%
-- Actitud: 5%
-- Trabajo encargado: 15%
-- TOTAL: 100%

-- 1. ELIMINAR todas las notas de tipo "Trabajos" (es duplicado)
DELETE FROM Nota 
WHERE tipo_evaluacion = 'Trabajos';

-- 2. Actualizar Examen Final de 25% a 20%
UPDATE Nota 
SET peso = 20.00 
WHERE tipo_evaluacion = 'Examen Final' 
  AND peso = 25.00;

-- 3. Actualizar Trabajo encargado de 10% a 15%
UPDATE Nota 
SET peso = 15.00 
WHERE tipo_evaluacion = 'Trabajo encargado' 
  AND peso = 10.00;

-- 4. Verificar que ahora todos suman 100%
SELECT 
    m.id as IdMatricula,
    e.Codigo,
    e.Nombres + ' ' + e.Apellidos as NombreEstudiante,
    SUM(n.peso) as PesoTotal,
    COUNT(*) as CantidadNotas,
    SUM(n.nota * n.peso / 100.0) as PromedioCalculado,
    ROUND(SUM(n.nota * n.peso / 100.0), 0) as PromedioRedondeado
FROM Nota n
INNER JOIN Matricula m ON n.idMatricula = m.id
INNER JOIN Estudiante e ON m.idEstudiante = e.id
WHERE m.id IN (12, 26, 32)
GROUP BY m.id, e.Codigo, e.Nombres, e.Apellidos
ORDER BY m.id;

-- 5. Ver detalle de cada estudiante con el cálculo
SELECT 
    idMatricula,
    tipo_evaluacion,
    nota,
    peso,
    (nota * peso / 100.0) as Contribucion
FROM Nota
WHERE idMatricula IN (12, 26, 32)
ORDER BY idMatricula, tipo_evaluacion;

-- 6. Verificar que NO quede ninguna matrícula con pesos incorrectos
SELECT 
    m.id as IdMatricula,
    e.Codigo,
    e.Nombres + ' ' + e.Apellidos as NombreEstudiante,
    SUM(n.peso) as PesoTotal,
    COUNT(*) as CantidadNotas
FROM Nota n
INNER JOIN Matricula m ON n.idMatricula = m.id
INNER JOIN Estudiante e ON m.idEstudiante = e.id
GROUP BY m.id, e.Codigo, e.Nombres, e.Apellidos
HAVING ABS(SUM(n.peso) - 100.0) > 0.01
ORDER BY PesoTotal DESC;
