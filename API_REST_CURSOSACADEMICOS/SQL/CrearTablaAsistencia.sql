-- Script idempotente para crear tabla Asistencia
-- Este script verifica si la tabla existe antes de crearla

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Asistencia')
BEGIN
    CREATE TABLE Asistencia (
        id INT IDENTITY(1,1) PRIMARY KEY,
        idEstudiante INT NOT NULL,
        idCurso INT NOT NULL,
        fecha DATE NOT NULL,
        presente BIT NOT NULL DEFAULT 0,
        observaciones NVARCHAR(500) NULL,
        fecha_registro DATETIME NOT NULL DEFAULT GETDATE()
    );

    -- Índices para mejorar el rendimiento
    CREATE INDEX IX_Asistencia_Estudiante ON Asistencia(idEstudiante);
    CREATE INDEX IX_Asistencia_Curso ON Asistencia(idCurso);
    CREATE INDEX IX_Asistencia_Fecha ON Asistencia(fecha);
    
    -- Índice compuesto para evitar duplicados
    CREATE UNIQUE INDEX IX_Asistencia_Estudiante_Curso_Fecha 
        ON Asistencia(idEstudiante, idCurso, fecha);

    PRINT 'Tabla Asistencia creada exitosamente con índices.';
END
ELSE
BEGIN
    PRINT 'La tabla Asistencia ya existe.';
END
GO

-- Agregar Foreign Keys después de crear la tabla (si no existen)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Asistencia_Estudiante')
BEGIN
    ALTER TABLE Asistencia
    ADD CONSTRAINT FK_Asistencia_Estudiante 
    FOREIGN KEY (idEstudiante) REFERENCES Estudiante(id) ON DELETE CASCADE;
    PRINT 'Foreign Key FK_Asistencia_Estudiante creada.';
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Asistencia_Curso')
BEGIN
    ALTER TABLE Asistencia
    ADD CONSTRAINT FK_Asistencia_Curso 
    FOREIGN KEY (idCurso) REFERENCES Curso(id) ON DELETE CASCADE;
    PRINT 'Foreign Key FK_Asistencia_Curso creada.';
END
GO
