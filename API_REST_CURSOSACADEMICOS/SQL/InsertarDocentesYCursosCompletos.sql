-- Script completo para insertar docentes y asignar cursos
-- Universidad Nacional Agraria de la Selva
-- Semestre 2025-2

USE GestionAcademica;
GO

-- ==================================================
-- PASO 1: Limpiar datos existentes
-- ==================================================
DELETE FROM Curso;
DELETE FROM Docente;
DBCC CHECKIDENT ('Curso', RESEED, 0);
DBCC CHECKIDENT ('Docente', RESEED, 0);
GO

-- ==================================================
-- PASO 2: Insertar Docentes
-- ==================================================
INSERT INTO Docente (nombres, apellidos, profesion, fecha_nacimiento, correo) VALUES
('Edwin Jesus', 'Vega Ventocilla', 'Ingeniero de Sistemas', '1980-01-15', 'evega@unas.edu.pe'),
('Rannoverng', 'Yanac Montesino', 'Ingeniero de Sistemas', '1978-03-20', 'ryanac@unas.edu.pe'),
('Alvaro Ulises', 'Paredes Acuña', 'Estadístico', '1975-06-10', 'aparedes@unas.edu.pe'),
('Hubel', 'Solis Bonifacio', 'Ingeniero de Sistemas', '1982-09-05', 'hsolis@unas.edu.pe'),
('Jose Antonio', 'Cardenas Vega', 'Ingeniero de Sistemas', '1979-11-25', 'jcardenas@unas.edu.pe'),
('Daniel Ivan', 'Leon Rivera', 'Ingeniero de Sistemas', '1983-04-18', 'dleon@unas.edu.pe'),
('Johnny Ronald', 'Tarmeño Berrocal', 'Matemático', '1976-07-30', 'jtarmeno@unas.edu.pe'),
('Darwin Emerson', 'Guerrero Vejarano', 'Matemático', '1977-12-08', 'dguerrero@unas.edu.pe'),
('Santos Victor', 'Ponce Guizabalo', 'Matemático', '1974-02-14', 'sponce@unas.edu.pe'),
('Gardyn', 'Olivera Ruiz', 'Ingeniero de Redes', '1981-05-22', 'golivera@unas.edu.pe'),
('William Rogelio', 'Marchand Niño', 'Ingeniero de Sistemas', '1980-08-16', 'wmarchand@unas.edu.pe'),
('Julio Cesar', 'Gonzales Paico', 'Ingeniero de Sistemas', '1979-10-12', 'jgonzales@unas.edu.pe'),
('Jeraldine Sheribel', 'Alca Yaranga', 'Ingeniera de Sistemas', '1985-03-28', 'jalca@unas.edu.pe'),
('William George', 'Paucar Palomino', 'Ingeniero de Sistemas', '1978-09-19', 'wpaucar@unas.edu.pe'),
('Nilton', 'Chucos Baquerizo', 'Ingeniero de Sistemas', '1980-06-07', 'nchucos@unas.edu.pe'),
('Christian', 'Garcia Villegas', 'Arquitecto de Software', '1982-11-03', 'cgarcia@unas.edu.pe'),
('Marco Arturo', 'Canales Aguirre', 'Ingeniero de Sistemas', '1976-04-25', 'mcanales@unas.edu.pe'),
('Carlos Abraham', 'Rios Rivera', 'Ingeniero de Sistemas', '1981-07-14', 'crios@unas.edu.pe'),
('Ronald Eduardo', 'Ibarra Zapata', 'Ingeniero de Software', '1983-12-21', 'ribarra@unas.edu.pe'),
('Alberto Lucio', 'Acevedo Aliaga', 'Ingeniero de Sistemas', '1979-02-09', 'aacevedo@unas.edu.pe'),
('Juan', 'Ramos Estela', 'Ingeniero Cloud', '1984-05-17', 'jramos@unas.edu.pe'),
('Brian Cesar', 'Pando Soto', 'Ingeniero de Sistemas', '1986-08-30', 'bpando@unas.edu.pe'),
('Noel', 'Juipa Campo', 'Ingeniero de Bases de Datos', '1980-01-11', 'njuipa@unas.edu.pe'),
('Jose Orlando', 'Castillo Cornelio', 'Diseñador UX/UI', '1985-06-23', 'jcastillo@unas.edu.pe'),
('Jorge Luis', 'Pozo Malpartida', 'Ingeniero de Procesos', '1977-09-15', 'jpozo@unas.edu.pe'),
('Cesar Armando', 'Santisteban Alvarado', 'Físico', '1975-03-08', 'csantisteban@unas.edu.pe'),
('Eudolio Gregorio', 'Vasquez Pinedo', 'Profesor de Inglés', '1978-11-27', 'evasquez@unas.edu.pe'),
('Jorge Luis', 'Gonzales Lafosse', 'Matemático', '1976-05-19', 'jgonzalesl@unas.edu.pe'),
('Jina Lize', 'Dolores Lezameta', 'Ingeniera de Sistemas', '1984-07-04', 'jdolores@unas.edu.pe'),
('Pedro Crisologo', 'Trujillo Natividad', 'Ingeniero de Sistemas', '1979-10-31', 'ptrujillo@unas.edu.pe');
GO

-- ==================================================
-- PASO 3: Insertar Cursos y Asignar Docentes
-- ==================================================

-- CICLO 1
INSERT INTO Curso (curso, creditos, horasSemanal, ciclo, idDocente) VALUES
('Fundamentos de Computación', 5, 7, 1, (SELECT id FROM Docente WHERE apellidos = 'Cardenas Vega'));

-- CICLO 2
INSERT INTO Curso (curso, creditos, horasSemanal, ciclo, idDocente) VALUES
('Matemática Básica II', 4, 5, 2, (SELECT id FROM Docente WHERE apellidos = 'Gonzales Lafosse')),
('Pensamiento Sistémico', 3, 4, 2, (SELECT id FROM Docente WHERE apellidos = 'Dolores Lezameta')),
('Programación Básica', 5, 7, 2, (SELECT id FROM Docente WHERE apellidos = 'Trujillo Natividad')),
('Soporte de TI', 4, 5, 2, (SELECT id FROM Docente WHERE apellidos = 'Cardenas Vega')),
('Inglés Técnico para Informática', 4, 5, 2, (SELECT id FROM Docente WHERE apellidos = 'Vasquez Pinedo'));

-- CICLO 3
INSERT INTO Curso (curso, creditos, horasSemanal, ciclo, idDocente) VALUES
('Estructura de Datos y Algoritmos', 4, 5, 3, (SELECT id FROM Docente WHERE apellidos = 'Solis Bonifacio')),
('Matemática Discreta', 4, 5, 3, (SELECT id FROM Docente WHERE apellidos = 'Tarmeño Berrocal')),
('Matemática Superior', 4, 5, 3, (SELECT id FROM Docente WHERE apellidos = 'Guerrero Vejarano')),
('Redes y Conectividad I', 4, 5, 3, (SELECT id FROM Docente WHERE apellidos = 'Olivera Ruiz'));

-- CICLO 4
INSERT INTO Curso (curso, creditos, horasSemanal, ciclo, idDocente) VALUES
('Construcción de Software I', 5, 7, 4, (SELECT id FROM Docente WHERE apellidos = 'Solis Bonifacio')),
('Diseño de Base de Datos', 4, 5, 4, (SELECT id FROM Docente WHERE apellidos = 'Juipa Campo')),
('Física', 4, 5, 4, (SELECT id FROM Docente WHERE apellidos = 'Santisteban Alvarado')),
('Redes y Conectividad II', 4, 5, 4, (SELECT id FROM Docente WHERE apellidos = 'Gonzales Paico')),
('Sistemas Operativos II', 4, 5, 4, (SELECT id FROM Docente WHERE apellidos = 'Vasquez Pinedo'));

-- CICLO 5
INSERT INTO Curso (curso, creditos, horasSemanal, ciclo, idDocente) VALUES
('Estadística y Probabilidades', 4, 5, 5, (SELECT id FROM Docente WHERE apellidos = 'Paredes Acuña')),
('Servidores y Centro de Datos', 4, 5, 5, (SELECT id FROM Docente WHERE apellidos = 'Gonzales Paico'));

-- CICLO 6
INSERT INTO Curso (curso, creditos, horasSemanal, ciclo, idDocente) VALUES
('Analítica de Datos', 3, 4, 6, (SELECT id FROM Docente WHERE apellidos = 'Paucar Palomino')),
('Arquitectura de Software', 4, 5, 6, (SELECT id FROM Docente WHERE apellidos = 'Garcia Villegas')),
('Computación en la Nube', 3, 4, 6, (SELECT id FROM Docente WHERE apellidos = 'Ramos Estela')),
('Gestión de Procesos de Negocio', 4, 5, 6, (SELECT id FROM Docente WHERE apellidos = 'Canales Aguirre')),
('Diseño Detallado de Software', 4, 5, 6, (SELECT id FROM Docente WHERE apellidos = 'Yanac Montesino')),
('Gestión de Proyectos de TI', 4, 5, 6, (SELECT id FROM Docente WHERE apellidos = 'Vega Ventocilla'));

-- CICLO 7
INSERT INTO Curso (curso, creditos, horasSemanal, ciclo, idDocente) VALUES
('Fundamentos de Investigación', 4, 5, 7, (SELECT id FROM Docente WHERE apellidos = 'Yanac Montesino')),
('Seguridad Informática', 4, 5, 7, (SELECT id FROM Docente WHERE apellidos = 'Marchand Niño'));

-- CICLO 8
INSERT INTO Curso (curso, creditos, horasSemanal, ciclo, idDocente) VALUES
('Calidad de Producto de Software', 4, 5, 8, (SELECT id FROM Docente WHERE apellidos = 'Ibarra Zapata')),
('Diseño de Investigación I', 4, 5, 8, (SELECT id FROM Docente WHERE apellidos = 'Garcia Villegas')),
('Diseño de Sistemas de Información', 3, 4, 8, (SELECT id FROM Docente WHERE apellidos = 'Paucar Palomino')),
('Prácticas Pre Profesional', 4, 5, 8, (SELECT id FROM Docente WHERE apellidos = 'Vega Ventocilla')),
('Seguridad de la Información', 4, 5, 8, (SELECT id FROM Docente WHERE apellidos = 'Marchand Niño'));

-- CICLO 9
INSERT INTO Curso (curso, creditos, horasSemanal, ciclo, idDocente) VALUES
('Arquitectura de Infraestructura de TI', 4, 5, 9, (SELECT id FROM Docente WHERE apellidos = 'Vega Ventocilla')),
('Diseño de Investigación II', 4, 5, 9, (SELECT id FROM Docente WHERE apellidos = 'Yanac Montesino')),
('Integración de Sistemas de Software', 4, 5, 9, (SELECT id FROM Docente WHERE apellidos = 'Leon Rivera'));

-- CICLO 10
INSERT INTO Curso (curso, creditos, horasSemanal, ciclo, idDocente) VALUES
('Arquitectura Empresarial', 4, 5, 10, (SELECT id FROM Docente WHERE apellidos = 'Canales Aguirre')),
('Calidad de Procesos de Software', 3, 4, 10, (SELECT id FROM Docente WHERE apellidos = 'Rios Rivera')),
('Desarrollo de Investigación', 5, 7, 10, (SELECT id FROM Docente WHERE apellidos = 'Ibarra Zapata')),
('Mantenimiento de Software', 4, 5, 10, (SELECT id FROM Docente WHERE apellidos = 'Castillo Cornelio')),
('Planeamiento y Gobierno de TI', 3, 4, 10, (SELECT id FROM Docente WHERE apellidos = 'Chucos Baquerizo'));

-- CURSOS ELECTIVOS (Ciclo 12)
INSERT INTO Curso (curso, creditos, horasSemanal, ciclo, idDocente) VALUES
('Desarrollo de Aplicaciones para la Nube', 3, 4, 12, (SELECT id FROM Docente WHERE apellidos = 'Pando Soto')),
('Diseño de Interfaz y Experiencia de Usuario', 3, 4, 12, (SELECT id FROM Docente WHERE apellidos = 'Leon Rivera')),
('Programabilidad de Redes', 3, 4, 12, (SELECT id FROM Docente WHERE apellidos = 'Gonzales Paico')),
('Redes Avanzadas', 3, 4, 12, (SELECT id FROM Docente WHERE apellidos = 'Gonzales Paico'));

GO

-- ==================================================
-- VERIFICACIONES
-- ==================================================

PRINT '================================================';
PRINT 'RESUMEN DE INSERCIÓN';
PRINT '================================================';

DECLARE @TotalDocentes INT, @TotalCursos INT, @CursosConDocente INT;

SELECT @TotalDocentes = COUNT(*) FROM Docente;
SELECT @TotalCursos = COUNT(*) FROM Curso;
SELECT @CursosConDocente = COUNT(*) FROM Curso WHERE idDocente IS NOT NULL;

PRINT 'Total de Docentes insertados: ' + CAST(@TotalDocentes AS VARCHAR);
PRINT 'Total de Cursos insertados: ' + CAST(@TotalCursos AS VARCHAR);
PRINT 'Cursos con Docente asignado: ' + CAST(@CursosConDocente AS VARCHAR);
PRINT 'Cursos sin Docente: ' + CAST((@TotalCursos - @CursosConDocente) AS VARCHAR);
PRINT '================================================';

-- Mostrar cursos por ciclo con docentes
SELECT 
    c.ciclo,
    COUNT(*) as total_cursos,
    SUM(c.creditos) as total_creditos,
    COUNT(c.idDocente) as cursos_con_docente
FROM Curso c
GROUP BY c.ciclo
ORDER BY c.ciclo;

-- Mostrar todos los cursos con sus docentes
SELECT 
    c.id,
    c.curso,
    c.creditos,
    c.horasSemanal,
    c.ciclo,
    CONCAT(d.nombres, ' ', d.apellidos) as docente,
    d.profesion
FROM Curso c
LEFT JOIN Docente d ON c.idDocente = d.id
ORDER BY c.ciclo, c.id;

-- Mostrar docentes con cantidad de cursos asignados
SELECT 
    d.id,
    CONCAT(d.nombres, ' ', d.apellidos) as docente,
    d.profesion,
    COUNT(c.id) as cursos_asignados
FROM Docente d
LEFT JOIN Curso c ON d.id = c.idDocente
GROUP BY d.id, d.nombres, d.apellidos, d.profesion
ORDER BY cursos_asignados DESC, d.apellidos;

GO
