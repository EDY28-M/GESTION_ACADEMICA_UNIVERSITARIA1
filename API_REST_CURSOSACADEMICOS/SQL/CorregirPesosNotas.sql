-- Script para corregir los pesos de TODAS las notas para que sumen exactamente 100%
-- Problema: Los estudiantes tienen 7-8 evaluaciones pero los pesos suman más de 100%

-- Configuración correcta de pesos (debe sumar 100%):
-- Parcial 1: 10%
-- Parcial 2: 10%
-- Práctica: 20%
-- Medio Curso: 20%
-- Examen Final: 20% (NO 25%)
-- Actitud: 5%
-- Trabajos: 10% (NO 15%)
-- Trabajo encargado: 5% (NO 10%)
-- TOTAL: 100%

-- 1. Actualizar Examen Final de 25% a 20%
UPDATE Nota 
SET peso = 20.00 
WHERE tipo_evaluacion = 'Examen Final' 
  AND peso = 25.00;

-- 2. Actualizar Trabajos de 15% a 10%
UPDATE Nota 
SET peso = 10.00 
WHERE tipo_evaluacion = 'Trabajos' 
  AND peso = 15.00;

-- 3. Actualizar Trabajo encargado de 10% a 5%
UPDATE Nota 
SET peso = 5.00 
WHERE tipo_evaluacion = 'Trabajo encargado' 
  AND peso = 10.00;

-- 4. Verificar que ahora todos suman 100%
SELECT 
    m.id as IdMatricula,
    e.Codigo,
    e.Nombres + ' ' + e.Apellidos as NombreEstudiante,
    SUM(n.peso) as PesoTotal,
    COUNT(*) as CantidadNotas,
    SUM(n.nota * n.peso / 100.0) as PromedioCalculado
FROM Nota n
INNER JOIN Matricula m ON n.idMatricula = m.id
INNER JOIN Estudiante e ON m.idEstudiante = e.id
WHERE m.id IN (12, 26, 32)
GROUP BY m.id, e.Codigo, e.Nombres, e.Apellidos
ORDER BY m.id;

-- 5. Ver detalle de cada estudiante
SELECT 
    idMatricula,
    tipo_evaluacion,
    nota,
    peso,
    (nota * peso / 100.0) as Contribucion
FROM Nota
WHERE idMatricula IN (12, 26, 32)
ORDER BY idMatricula, tipo_evaluacion;

-- 6. Verificar cálculo manual para Junior Edinson (matrícula 12)
-- Con todas las notas en 10 y pesos correctos, debería dar 10
SELECT 
    'Cálculo para Junior Edinson' as Descripcion,
    idMatricula,
    SUM(nota * peso / 100.0) as PromedioFinal,
    ROUND(SUM(nota * peso / 100.0), 0) as PromedioRedondeado
FROM Nota
WHERE idMatricula = 12
GROUP BY idMatricula;
