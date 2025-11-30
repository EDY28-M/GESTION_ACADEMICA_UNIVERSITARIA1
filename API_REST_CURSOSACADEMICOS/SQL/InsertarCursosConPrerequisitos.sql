-- Script de ejemplo para insertar cursos del PRIMER CICLO según CURSOS.csv
-- Este es un ejemplo. Debe adaptarse según los IDs de docentes existentes

-- PRIMER CICLO (I)
-- Nota: Los cursos del primer ciclo no tienen prerequisitos

-- 1. MATEMÁTICA BÁSICA I
INSERT INTO Curso (codigo, curso, creditos, horasTeoria, horasPractica, horasSemanal, horasTotales, ciclo, idDocente)
VALUES ('IS040101', 'MATEMÁTICA BÁSICA I', 4, 3, 2, 5, 80, 1, NULL);

-- 2. TALLER DE HABILIDADES BLANDAS
INSERT INTO Curso (codigo, curso, creditos, horasTeoria, horasPractica, horasSemanal, horasTotales, ciclo, idDocente)
VALUES ('IS040102', 'TALLER DE HABILIDADES BLANDAS', 3, 2, 3, 5, 80, 1, NULL);

-- 3. SOSTENIBILIDAD Y RESPONSABILIDAD SOCIAL
INSERT INTO Curso (codigo, curso, creditos, horasTeoria, horasPractica, horasSemanal, horasTotales, ciclo, idDocente)
VALUES ('IS040103', 'SOSTENIBILIDAD Y RESPONSABILIDAD SOCIAL', 3, 2, 2, 4, 64, 1, NULL);

-- 4. REDACCIÓN Y COMPRENSIÓN LECTORA
INSERT INTO Curso (codigo, curso, creditos, horasTeoria, horasPractica, horasSemanal, horasTotales, ciclo, idDocente)
VALUES ('IS040104', 'REDACCIÓN Y COMPRENSIÓN LECTORA', 3, 3, 2, 5, 80, 1, NULL);

-- 5. FUNDAMENTOS DE COMPUTACIÓN
INSERT INTO Curso (codigo, curso, creditos, horasTeoria, horasPractica, horasSemanal, horasTotales, ciclo, idDocente)
VALUES ('IS040105', 'FUNDAMENTOS DE COMPUTACIÓN', 5, 3, 4, 7, 112, 1, NULL);

-- 6. Actividad Libre – Físico Deportivo
INSERT INTO Curso (codigo, curso, creditos, horasTeoria, horasPractica, horasSemanal, horasTotales, ciclo, idDocente)
VALUES ('IS040106', 'Actividad Libre – Físico Deportivo', 1, 0, 2, 2, 32, 1, NULL);

PRINT 'Cursos del Primer Ciclo insertados correctamente';
GO

-- SEGUNDO CICLO (I)
-- Estos cursos tienen prerequisitos del primer ciclo

-- 1. MATEMÁTICA BÁSICA II (Prerequisito: IS040101)
INSERT INTO Curso (codigo, curso, creditos, horasTeoria, horasPractica, horasSemanal, horasTotales, ciclo, idDocente)
VALUES ('IS040201', 'MATEMÁTICA BÁSICA II', 4, 3, 2, 5, 80, 2, NULL);

-- Agregar prerequisito
INSERT INTO CursoPrerequisito (idCurso, idCursoPrerequisito)
SELECT c1.id, c2.id
FROM Curso c1, Curso c2
WHERE c1.codigo = 'IS040201' AND c2.codigo = 'IS040101';

-- 2. PENSAMIENTO SISTÉMICO (Prerequisito: IS040102)
INSERT INTO Curso (codigo, curso, creditos, horasTeoria, horasPractica, horasSemanal, horasTotales, ciclo, idDocente)
VALUES ('IS040202', 'PENSAMIENTO SISTÉMICO', 3, 2, 2, 4, 64, 2, NULL);

INSERT INTO CursoPrerequisito (idCurso, idCursoPrerequisito)
SELECT c1.id, c2.id
FROM Curso c1, Curso c2
WHERE c1.codigo = 'IS040202' AND c2.codigo = 'IS040102';

-- 3. PROGRAMACIÓN BÁSICA (Prerequisito: IS040105)
INSERT INTO Curso (codigo, curso, creditos, horasTeoria, horasPractica, horasSemanal, horasTotales, ciclo, idDocente)
VALUES ('IS040203', 'PROGRAMACIÓN BÁSICA', 5, 3, 4, 7, 112, 2, NULL);

INSERT INTO CursoPrerequisito (idCurso, idCursoPrerequisito)
SELECT c1.id, c2.id
FROM Curso c1, Curso c2
WHERE c1.codigo = 'IS040203' AND c2.codigo = 'IS040105';

-- 4. SOPORTE DE TI (Prerequisito: IS040105)
INSERT INTO Curso (codigo, curso, creditos, horasTeoria, horasPractica, horasSemanal, horasTotales, ciclo, idDocente)
VALUES ('IS040204', 'SOPORTE DE TI', 4, 3, 2, 5, 80, 2, NULL);

INSERT INTO CursoPrerequisito (idCurso, idCursoPrerequisito)
SELECT c1.id, c2.id
FROM Curso c1, Curso c2
WHERE c1.codigo = 'IS040204' AND c2.codigo = 'IS040105';

-- 5. INGLÉS TÉCNICO PARA INFORMÁTICA (Prerequisitos: IS040102, IS040104)
INSERT INTO Curso (codigo, curso, creditos, horasTeoria, horasPractica, horasSemanal, horasTotales, ciclo, idDocente)
VALUES ('IS040205', 'INGLÉS TÉCNICO PARA INFORMÁTICA', 4, 3, 2, 5, 80, 2, NULL);

-- Agregar prerequisitos múltiples
INSERT INTO CursoPrerequisito (idCurso, idCursoPrerequisito)
SELECT c1.id, c2.id
FROM Curso c1, Curso c2
WHERE c1.codigo = 'IS040205' AND c2.codigo = 'IS040102';

INSERT INTO CursoPrerequisito (idCurso, idCursoPrerequisito)
SELECT c1.id, c2.id
FROM Curso c1, Curso c2
WHERE c1.codigo = 'IS040205' AND c2.codigo = 'IS040104';

-- 6. Actividad Libre – Artístico Culturales
INSERT INTO Curso (codigo, curso, creditos, horasTeoria, horasPractica, horasSemanal, horasTotales, ciclo, idDocente)
VALUES ('IS040206', 'Actividad Libre – Artístico Culturales', 1, 0, 2, 2, 32, 2, NULL);

PRINT 'Cursos del Segundo Ciclo insertados correctamente';
GO

-- TERCER CICLO (I)
-- Ejemplo de cursos con prerequisitos del segundo ciclo

-- 1. MATEMÁTICA SUPERIOR (Prerequisito: IS040201)
INSERT INTO Curso (codigo, curso, creditos, horasTeoria, horasPractica, horasSemanal, horasTotales, ciclo, idDocente)
VALUES ('IS040301', 'MATEMÁTICA SUPERIOR', 4, 3, 2, 5, 80, 3, NULL);

INSERT INTO CursoPrerequisito (idCurso, idCursoPrerequisito)
SELECT c1.id, c2.id
FROM Curso c1, Curso c2
WHERE c1.codigo = 'IS040301' AND c2.codigo = 'IS040201';

-- 2. MATEMÁTICA DISCRETA (Prerequisito: IS040201)
INSERT INTO Curso (codigo, curso, creditos, horasTeoria, horasPractica, horasSemanal, horasTotales, ciclo, idDocente)
VALUES ('IS040302', 'MATEMÁTICA DISCRETA', 4, 3, 2, 5, 80, 3, NULL);

INSERT INTO CursoPrerequisito (idCurso, idCursoPrerequisito)
SELECT c1.id, c2.id
FROM Curso c1, Curso c2
WHERE c1.codigo = 'IS040302' AND c2.codigo = 'IS040201';

-- 3. ESTRUCTURA DE DATOS Y ALGORITMOS (Prerequisito: IS040203)
INSERT INTO Curso (codigo, curso, creditos, horasTeoria, horasPractica, horasSemanal, horasTotales, ciclo, idDocente)
VALUES ('IS040303', 'ESTRUCTURA DE DATOS Y ALGORITMOS', 4, 3, 2, 5, 80, 3, NULL);

INSERT INTO CursoPrerequisito (idCurso, idCursoPrerequisito)
SELECT c1.id, c2.id
FROM Curso c1, Curso c2
WHERE c1.codigo = 'IS040303' AND c2.codigo = 'IS040203';

-- 4. SISTEMAS OPERATIVOS I (Prerequisito: IS040204)
INSERT INTO Curso (codigo, curso, creditos, horasTeoria, horasPractica, horasSemanal, horasTotales, ciclo, idDocente)
VALUES ('IS040304', 'SISTEMAS OPERATIVOS I', 5, 3, 2, 5, 80, 3, NULL);

INSERT INTO CursoPrerequisito (idCurso, idCursoPrerequisito)
SELECT c1.id, c2.id
FROM Curso c1, Curso c2
WHERE c1.codigo = 'IS040304' AND c2.codigo = 'IS040204';

-- 5. REDES Y CONECTIVIDAD I (Prerequisito: IS040204)
INSERT INTO Curso (codigo, curso, creditos, horasTeoria, horasPractica, horasSemanal, horasTotales, ciclo, idDocente)
VALUES ('IS040305', 'REDES Y CONECTIVIDAD I', 4, 3, 2, 5, 80, 3, NULL);

INSERT INTO CursoPrerequisito (idCurso, idCursoPrerequisito)
SELECT c1.id, c2.id
FROM Curso c1, Curso c2
WHERE c1.codigo = 'IS040305' AND c2.codigo = 'IS040204';

-- 6. Actividad Libre – Cívico Comunitarias
INSERT INTO Curso (codigo, curso, creditos, horasTeoria, horasPractica, horasSemanal, horasTotales, ciclo, idDocente)
VALUES ('IS040306', 'Actividad Libre – Cívico Comunitarias', 1, 0, 2, 2, 32, 3, NULL);

PRINT 'Cursos del Tercer Ciclo insertados correctamente';
GO

-- Verificación: Ver cursos con sus prerequisitos
SELECT 
    c.codigo AS CursoCodigo,
    c.curso AS CursoNombre,
    c.ciclo,
    c.creditos,
    cp_prereq.codigo AS PrerequisitoCodigo,
    cp_prereq.curso AS PrerequisitoNombre
FROM Curso c
LEFT JOIN CursoPrerequisito cp ON c.id = cp.idCurso
LEFT JOIN Curso cp_prereq ON cp.idCursoPrerequisito = cp_prereq.id
ORDER BY c.ciclo, c.codigo;

PRINT 'Script completado exitosamente';
