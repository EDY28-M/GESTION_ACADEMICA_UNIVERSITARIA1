using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using FluentAssertions;
using API_REST_CURSOSACADEMICOS.Controllers;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using System.Security.Claims;

namespace API_REST_CURSOSACADEMICOS.Tests.Controllers.Horarios
{
    public class HorariosControllerBatchTests
    {
        private readonly Mock<IHorarioService> _mockHorarioService;
        private readonly Mock<IUserLookupService> _mockUserLookupService;
        private readonly HorariosController _controller;

        public HorariosControllerBatchTests()
        {
            _mockHorarioService = new Mock<IHorarioService>();
            _mockUserLookupService = new Mock<IUserLookupService>();
            _controller = new HorariosController(_mockHorarioService.Object, _mockUserLookupService.Object);
        }

        private void SetupAdminUser()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Administrador")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        #region GetDocentesConCursos Tests

        [Fact]
        public async Task GetDocentesConCursos_ReturnsOkWithDocentes()
        {
            // Arrange
            SetupAdminUser();

            var docentes = new List<DocenteConCursosDto>
            {
                new DocenteConCursosDto
                {
                    IdDocente = 1,
                    NombreDocente = "Juan Pérez",
                    Profesion = "Ingeniero",
                    Correo = "juan@test.com",
                    Cursos = new List<CursoConHorariosDto>
                    {
                        new CursoConHorariosDto
                        {
                            IdCurso = 1,
                            NombreCurso = "Matemáticas",
                            Codigo = "MAT101",
                            Horarios = new List<HorarioDto>()
                        }
                    }
                },
                new DocenteConCursosDto
                {
                    IdDocente = 2,
                    NombreDocente = "María López",
                    Profesion = "Licenciada",
                    Correo = "maria@test.com",
                    Cursos = new List<CursoConHorariosDto>()
                }
            };

            _mockHorarioService.Setup(s => s.ObtenerDocentesConCursosActivosAsync())
                .ReturnsAsync(docentes);

            // Act
            var result = await _controller.GetDocentesConCursos();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedDocentes = okResult.Value.Should().BeAssignableTo<IEnumerable<DocenteConCursosDto>>().Subject;
            returnedDocentes.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetDocentesConCursos_WhenEmpty_ReturnsOkWithEmptyList()
        {
            // Arrange
            SetupAdminUser();

            _mockHorarioService.Setup(s => s.ObtenerDocentesConCursosActivosAsync())
                .ReturnsAsync(new List<DocenteConCursosDto>());

            // Act
            var result = await _controller.GetDocentesConCursos();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedDocentes = okResult.Value.Should().BeAssignableTo<IEnumerable<DocenteConCursosDto>>().Subject;
            returnedDocentes.Should().BeEmpty();
        }

        [Fact]
        public async Task GetDocentesConCursos_WhenServiceThrows_ReturnsInternalServerError()
        {
            // Arrange
            SetupAdminUser();

            _mockHorarioService.Setup(s => s.ObtenerDocentesConCursosActivosAsync())
                .ThrowsAsync(new Exception("Error de base de datos"));

            // Act
            var result = await _controller.GetDocentesConCursos();

            // Assert
            var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetDocentesConCursos_ReturnsDocenteWithMultipleCursos()
        {
            // Arrange
            SetupAdminUser();

            var docentes = new List<DocenteConCursosDto>
            {
                new DocenteConCursosDto
                {
                    IdDocente = 1,
                    NombreDocente = "Carlos García",
                    Profesion = "Ingeniero",
                    Correo = "carlos@test.com",
                    Cursos = new List<CursoConHorariosDto>
                    {
                        new CursoConHorariosDto
                        {
                            IdCurso = 1,
                            NombreCurso = "Álgebra",
                            Codigo = "ALG101",
                            Horarios = new List<HorarioDto>
                            {
                                new HorarioDto { DiaSemana = 1, HoraInicio = "08:00", HoraFin = "10:00" }
                            }
                        },
                        new CursoConHorariosDto
                        {
                            IdCurso = 2,
                            NombreCurso = "Cálculo",
                            Codigo = "CAL101",
                            Horarios = new List<HorarioDto>
                            {
                                new HorarioDto { DiaSemana = 3, HoraInicio = "10:00", HoraFin = "12:00" }
                            }
                        }
                    }
                }
            };

            _mockHorarioService.Setup(s => s.ObtenerDocentesConCursosActivosAsync())
                .ReturnsAsync(docentes);

            // Act
            var result = await _controller.GetDocentesConCursos();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedDocentes = okResult.Value.Should().BeAssignableTo<IEnumerable<DocenteConCursosDto>>().Subject;
            var docente = returnedDocentes.First();
            docente.Cursos.Should().HaveCount(2);
        }

        #endregion

        #region CrearHorariosBatch Tests

        [Fact]
        public async Task CrearHorariosBatch_WithValidData_ReturnsOk()
        {
            // Arrange
            SetupAdminUser();

            var batchDto = new CrearHorariosBatchDto
            {
                IdDocente = 1,
                Horarios = new List<CrearHorarioDto>
                {
                    new CrearHorarioDto
                    {
                        IdCurso = 1,
                        DiaSemana = 1,
                        HoraInicio = "08:00",
                        HoraFin = "10:00",
                        Aula = "A101",
                        Tipo = "Teoría"
                    },
                    new CrearHorarioDto
                    {
                        IdCurso = 1,
                        DiaSemana = 3,
                        HoraInicio = "10:00",
                        HoraFin = "12:00",
                        Aula = "A102",
                        Tipo = "Práctica"
                    }
                }
            };

            var resultado = new ResultadoBatchHorariosDto
            {
                TotalCreados = 2,
                TotalFallidos = 0,
                HorariosCreados = new List<HorarioDto>
                {
                    new HorarioDto { Id = 1, Aula = "A101" },
                    new HorarioDto { Id = 2, Aula = "A102" }
                },
                Errores = new List<ErrorHorarioDto>()
            };

            _mockHorarioService.Setup(s => s.CrearHorariosBatchAsync(It.IsAny<CrearHorariosBatchDto>()))
                .ReturnsAsync(resultado);

            // Act
            var result = await _controller.CrearHorariosBatch(batchDto);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedResult = okResult.Value.Should().BeOfType<ResultadoBatchHorariosDto>().Subject;
            returnedResult.TotalCreados.Should().Be(2);
            returnedResult.TotalFallidos.Should().Be(0);
        }

        [Fact]
        public async Task CrearHorariosBatch_WithPartialSuccess_ReturnsOkWithErrors()
        {
            // Arrange
            SetupAdminUser();

            var batchDto = new CrearHorariosBatchDto
            {
                IdDocente = 1,
                Horarios = new List<CrearHorarioDto>
                {
                    new CrearHorarioDto
                    {
                        IdCurso = 1,
                        DiaSemana = 1,
                        HoraInicio = "08:00",
                        HoraFin = "10:00",
                        Aula = "A101",
                        Tipo = "Teoría"
                    },
                    new CrearHorarioDto
                    {
                        IdCurso = 1,
                        DiaSemana = 1,
                        HoraInicio = "09:00",
                        HoraFin = "11:00",
                        Aula = "A101",
                        Tipo = "Práctica"
                    }
                }
            };

            var resultado = new ResultadoBatchHorariosDto
            {
                TotalCreados = 1,
                TotalFallidos = 1,
                HorariosCreados = new List<HorarioDto>
                {
                    new HorarioDto { Id = 1, Aula = "A101" }
                },
                Errores = new List<ErrorHorarioDto>
                {
                    new ErrorHorarioDto { IdCurso = 1, NombreCurso = "Curso", DiaSemana = 1, Error = "Conflicto de horario para el segundo horario" }
                }
            };

            _mockHorarioService.Setup(s => s.CrearHorariosBatchAsync(It.IsAny<CrearHorariosBatchDto>()))
                .ReturnsAsync(resultado);

            // Act
            var result = await _controller.CrearHorariosBatch(batchDto);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedResult = okResult.Value.Should().BeOfType<ResultadoBatchHorariosDto>().Subject;
            returnedResult.TotalCreados.Should().Be(1);
            returnedResult.TotalFallidos.Should().Be(1);
            returnedResult.Errores.Should().NotBeEmpty();
        }

        [Fact]
        public async Task CrearHorariosBatch_WhenAllFail_ReturnsBadRequest()
        {
            // Arrange
            SetupAdminUser();

            var batchDto = new CrearHorariosBatchDto
            {
                IdDocente = 1,
                Horarios = new List<CrearHorarioDto>
                {
                    new CrearHorarioDto
                    {
                        IdCurso = 999,
                        DiaSemana = 1,
                        HoraInicio = "08:00",
                        HoraFin = "10:00",
                        Aula = "A101",
                        Tipo = "Teoría"
                    }
                }
            };

            var resultado = new ResultadoBatchHorariosDto
            {
                TotalCreados = 0,
                TotalFallidos = 1,
                HorariosCreados = new List<HorarioDto>(),
                Errores = new List<ErrorHorarioDto>
                {
                    new ErrorHorarioDto { IdCurso = 999, NombreCurso = "Curso", DiaSemana = 1, Error = "Curso no encontrado" }
                }
            };

            _mockHorarioService.Setup(s => s.CrearHorariosBatchAsync(It.IsAny<CrearHorariosBatchDto>()))
                .ReturnsAsync(resultado);

            // Act
            var result = await _controller.CrearHorariosBatch(batchDto);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CrearHorariosBatch_WhenServiceThrows_ReturnsInternalServerError()
        {
            // Arrange
            SetupAdminUser();

            var batchDto = new CrearHorariosBatchDto
            {
                IdDocente = 1,
                Horarios = new List<CrearHorarioDto>()
            };

            _mockHorarioService.Setup(s => s.CrearHorariosBatchAsync(It.IsAny<CrearHorariosBatchDto>()))
                .ThrowsAsync(new Exception("Error inesperado"));

            // Act
            var result = await _controller.CrearHorariosBatch(batchDto);

            // Assert
            var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task CrearHorariosBatch_WithEmptyHorariosList_CallsService()
        {
            // Arrange
            SetupAdminUser();

            var batchDto = new CrearHorariosBatchDto
            {
                IdDocente = 1,
                Horarios = new List<CrearHorarioDto>()
            };

            var resultado = new ResultadoBatchHorariosDto
            {
                TotalCreados = 0,
                TotalFallidos = 0,
                HorariosCreados = new List<HorarioDto>(),
                Errores = new List<ErrorHorarioDto>()
            };

            _mockHorarioService.Setup(s => s.CrearHorariosBatchAsync(It.IsAny<CrearHorariosBatchDto>()))
                .ReturnsAsync(resultado);

            // Act
            var result = await _controller.CrearHorariosBatch(batchDto);

            // Assert
            _mockHorarioService.Verify(s => s.CrearHorariosBatchAsync(batchDto), Times.Once);
        }

        [Fact]
        public async Task CrearHorariosBatch_WithMultipleDocentes_ReturnsCorrectResult()
        {
            // Arrange
            SetupAdminUser();

            var batchDto = new CrearHorariosBatchDto
            {
                IdDocente = 5,
                Horarios = new List<CrearHorarioDto>
                {
                    new CrearHorarioDto
                    {
                        IdCurso = 10,
                        DiaSemana = 2,
                        HoraInicio = "14:00",
                        HoraFin = "16:00",
                        Aula = "B201",
                        Tipo = "Laboratorio"
                    }
                }
            };

            var resultado = new ResultadoBatchHorariosDto
            {
                TotalCreados = 1,
                TotalFallidos = 0,
                HorariosCreados = new List<HorarioDto>
                {
                    new HorarioDto 
                    { 
                        Id = 100, 
                        IdCurso = 10,
                        DiaSemana = 2,
                        DiaSemanaTexto = "Martes",
                        HoraInicio = "14:00",
                        HoraFin = "16:00",
                        Aula = "B201",
                        Tipo = "Laboratorio"
                    }
                },
                Errores = new List<ErrorHorarioDto>()
            };

            _mockHorarioService.Setup(s => s.CrearHorariosBatchAsync(It.IsAny<CrearHorariosBatchDto>()))
                .ReturnsAsync(resultado);

            // Act
            var result = await _controller.CrearHorariosBatch(batchDto);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedResult = okResult.Value.Should().BeOfType<ResultadoBatchHorariosDto>().Subject;
            returnedResult.TotalCreados.Should().Be(1);
            returnedResult.HorariosCreados.First().Aula.Should().Be("B201");
        }

        #endregion
    }
}
