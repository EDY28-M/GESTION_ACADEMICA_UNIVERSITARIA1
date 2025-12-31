# ========================================
# DOCKERFILE PARA API REST CURSOS ACADÉMICOS
# ASP.NET Core 9.0
# ========================================

# Etapa 1: Build - Compilar la aplicación
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar el archivo de proyecto y restaurar dependencias
COPY ["API_REST_CURSOSACADEMICOS.csproj", "./"]
RUN dotnet restore "API_REST_CURSOSACADEMICOS.csproj"

# Copiar todo el código fuente (excluyendo tests)
COPY . .

# Compilar la aplicación en modo Release
RUN dotnet build "API_REST_CURSOSACADEMICOS.csproj" -c Release -o /app/build

# Etapa 2: Publish - Publicar la aplicación
FROM build AS publish
RUN dotnet publish "API_REST_CURSOSACADEMICOS.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 3: Runtime - Imagen final optimizada
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Exponer el puerto 8080 (puerto predeterminado de ASP.NET Core en contenedores)
EXPOSE 8080
EXPOSE 8081

# Copiar los archivos publicados desde la etapa publish
COPY --from=publish /app/publish .

# Configurar variables de entorno
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Punto de entrada de la aplicación
ENTRYPOINT ["dotnet", "API_REST_CURSOSACADEMICOS.dll"]
