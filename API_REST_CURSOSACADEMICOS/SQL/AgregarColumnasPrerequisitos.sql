-- Script para agregar columnas necesarias a la tabla Curso
-- Basado en el archivo CURSOS.csv

-- 1. Agregar columnas faltantes a la tabla Curso
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Curso]') AND name = 'codigo')
BEGIN
    ALTER TABLE Curso ADD codigo VARCHAR(20) NULL;
    PRINT 'Columna codigo agregada';
END
ELSE
BEGIN
    PRINT 'Columna codigo ya existe';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Curso]') AND name = 'horasTeoria')
BEGIN
    ALTER TABLE Curso ADD horasTeoria INT NULL;
    PRINT 'Columna horasTeoria agregada';
END
ELSE
BEGIN
    PRINT 'Columna horasTeoria ya existe';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Curso]') AND name = 'horasPractica')
BEGIN
    ALTER TABLE Curso ADD horasPractica INT NULL;
    PRINT 'Columna horasPractica agregada';
END
ELSE
BEGIN
    PRINT 'Columna horasPractica ya existe';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Curso]') AND name = 'horasTotales')
BEGIN
    ALTER TABLE Curso ADD horasTotales INT NULL;
    PRINT 'Columna horasTotales agregada';
END
ELSE
BEGIN
    PRINT 'Columna horasTotales ya existe';
END
GO

-- 2. Crear tabla de prerequisitos si no existe
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CursoPrerequisito')
BEGIN
    CREATE TABLE CursoPrerequisito (
        id INT IDENTITY(1,1) PRIMARY KEY,
        idCurso INT NOT NULL,
        idCursoPrerequisito INT NOT NULL,
        FOREIGN KEY (idCurso) REFERENCES Curso(id) ON DELETE CASCADE,
        FOREIGN KEY (idCursoPrerequisito) REFERENCES Curso(id),
        UNIQUE(idCurso, idCursoPrerequisito)
    );
    
    CREATE INDEX IX_CursoPrerequisito_Curso ON CursoPrerequisito(idCurso);
    CREATE INDEX IX_CursoPrerequisito_Prerequisito ON CursoPrerequisito(idCursoPrerequisito);
    
    PRINT 'Tabla CursoPrerequisito creada';
END
ELSE
BEGIN
    PRINT 'Tabla CursoPrerequisito ya existe';
END
GO

-- 3. Agregar índice único para el código del curso (solo si la columna codigo existe y el índice no existe)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Curso]') AND name = 'codigo')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Curso_Codigo' AND object_id = OBJECT_ID('Curso'))
    BEGIN
        CREATE UNIQUE INDEX IX_Curso_Codigo ON Curso(codigo) WHERE codigo IS NOT NULL;
        PRINT 'Índice IX_Curso_Codigo creado';
    END
    ELSE
    BEGIN
        PRINT 'Índice IX_Curso_Codigo ya existe';
    END
END
GO

PRINT 'Script completado exitosamente';
