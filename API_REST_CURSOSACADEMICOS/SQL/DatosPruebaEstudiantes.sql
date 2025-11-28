-- Script para agregar datos de prueba completos al módulo estudiante
USE GestionAcademica;
GO

-- 1. Verificar período activo
DECLARE @idPeriodoActivo INT;
SELECT @idPeriodoActivo = id FROM Periodo WHERE activo = 1;

IF @idPeriodoActivo IS NULL
BEGIN
    PRINT 'No hay período activo. Creando uno...';
    INSERT INTO Periodo (nombre, anio, ciclo, fecha_inicio, fecha_fin, activo) 
    VALUES ('2024-II', 2024, 'II', '2024-08-01', '2024-12-31', 1);
    SET @idPeriodoActivo = SCOPE_IDENTITY();
END

PRINT 'Período activo ID: ' + CAST(@idPeriodoActivo AS VARCHAR);

-- 2. Obtener IDs de estudiantes existentes
DECLARE @idEstudiante1 INT, @idEstudiante2 INT;
SELECT TOP 1 @idEstudiante1 = id FROM Estudiante ORDER BY id;
SELECT @idEstudiante2 = id FROM Estudiante WHERE id > @idEstudiante1 ORDER BY id;

PRINT 'Estudiantes encontrados: ' + CAST(@idEstudiante1 AS VARCHAR) + ', ' + CAST(ISNULL(@idEstudiante2, 0) AS VARCHAR);

-- 3. Obtener cursos del ciclo del estudiante
DECLARE @cicloEstudiante INT;
SELECT @cicloEstudiante = ciclo_actual FROM Estudiante WHERE id = @idEstudiante1;

-- 4. Matricular estudiante en 5 cursos de su ciclo
DECLARE @idCurso INT;
DECLARE cur_cursos CURSOR FOR 
    SELECT TOP 5 id FROM Curso WHERE ciclo = @cicloEstudiante ORDER BY id;

OPEN cur_cursos;
FETCH NEXT FROM cur_cursos INTO @idCurso;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Verificar si ya está matriculado
    IF NOT EXISTS (SELECT 1 FROM Matricula WHERE idEstudiante = @idEstudiante1 AND idCurso = @idCurso AND idPeriodo = @idPeriodoActivo)
    BEGIN
        INSERT INTO Matricula (idEstudiante, idCurso, idPeriodo, estado, fecha_matricula)
        VALUES (@idEstudiante1, @idCurso, @idPeriodoActivo, 'Matriculado', GETDATE());
        
        DECLARE @idMatricula INT = SCOPE_IDENTITY();
        
        -- Agregar 3 notas por curso (Parcial, Final, Práctica)
        INSERT INTO Nota (idMatricula, tipo_evaluacion, nota, peso, fecha_evaluacion)
        VALUES 
            (@idMatricula, 'Examen Parcial', 15.0, 0.30, GETDATE()),
            (@idMatricula, 'Examen Final', 16.5, 0.40, GETDATE()),
            (@idMatricula, 'Prácticas', 14.0, 0.30, GETDATE());
            
        PRINT 'Matriculado en curso ID: ' + CAST(@idCurso AS VARCHAR);
    END
    
    FETCH NEXT FROM cur_cursos INTO @idCurso;
END

CLOSE cur_cursos;
DEALLOCATE cur_cursos;

-- 5. Si hay segundo estudiante, matricularlo también
IF @idEstudiante2 IS NOT NULL
BEGIN
    SELECT @cicloEstudiante = ciclo_actual FROM Estudiante WHERE id = @idEstudiante2;
    
    DECLARE cur_cursos2 CURSOR FOR 
        SELECT TOP 4 id FROM Curso WHERE ciclo = @cicloEstudiante ORDER BY id;
    
    OPEN cur_cursos2;
    FETCH NEXT FROM cur_cursos2 INTO @idCurso;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Matricula WHERE idEstudiante = @idEstudiante2 AND idCurso = @idCurso AND idPeriodo = @idPeriodoActivo)
        BEGIN
            INSERT INTO Matricula (idEstudiante, idCurso, idPeriodo, estado, fecha_matricula)
            VALUES (@idEstudiante2, @idCurso, @idPeriodoActivo, 'Matriculado', GETDATE());
            
            SET @idMatricula = SCOPE_IDENTITY();
            
            INSERT INTO Nota (idMatricula, tipo_evaluacion, nota, peso, fecha_evaluacion)
            VALUES 
                (@idMatricula, 'Examen Parcial', 13.5, 0.30, GETDATE()),
                (@idMatricula, 'Examen Final', 14.0, 0.40, GETDATE()),
                (@idMatricula, 'Prácticas', 15.5, 0.30, GETDATE());
        END
        
        FETCH NEXT FROM cur_cursos2 INTO @idCurso;
    END
    
    CLOSE cur_cursos2;
    DEALLOCATE cur_cursos2;
END

-- 6. Mostrar resumen
SELECT 
    e.codigo,
    e.nombres + ' ' + e.apellidos AS estudiante,
    COUNT(m.id) AS cursos_matriculados,
    AVG(n.nota) AS promedio_general
FROM Estudiante e
LEFT JOIN Matricula m ON e.id = m.idEstudiante
LEFT JOIN Nota n ON m.id = n.idMatricula
GROUP BY e.id, e.codigo, e.nombres, e.apellidos;

PRINT '¡Datos de prueba insertados exitosamente!';
GO
