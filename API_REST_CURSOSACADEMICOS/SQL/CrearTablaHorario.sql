-- Script idempotente para crear tabla Horario
-- Este script verifica si la tabla existe antes de crearla

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Horario')
BEGIN
    CREATE TABLE Horario (
        id INT IDENTITY(1,1) PRIMARY KEY,
        idCurso INT NOT NULL,
        dia_semana INT NOT NULL, -- 1=Lunes, 2=Martes, etc.
        hora_inicio TIME NOT NULL,
        hora_fin TIME NOT NULL,
        aula NVARCHAR(50) NULL,
        tipo NVARCHAR(20) NOT NULL DEFAULT 'Teoría', -- Teoría, Práctica
        
        CONSTRAINT FK_Horario_Curso FOREIGN KEY (idCurso) REFERENCES Curso(id) ON DELETE CASCADE
    );

    -- Índices para mejorar el rendimiento
    CREATE INDEX IX_Horario_Curso ON Horario(idCurso);
    CREATE INDEX IX_Horario_DiaSemana ON Horario(dia_semana);
    
    PRINT 'Tabla Horario creada correctamente.';
END
ELSE
BEGIN
    PRINT 'La tabla Horario ya existe.';
END
