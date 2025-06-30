# --- Dockerfile (Colocar este archivo dentro de la carpeta 'UI') ---

# Usa una imagen base para construir el proyecto.
# Reemplaza '8.0' con la versión de .NET que uses.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src
# Copia el archivo .csproj primero para restaurar dependencias.
# Asegúrate de que el nombre del proyecto coincida exactamente con tu .csproj
COPY ["UI.csproj", "UI/"]
RUN dotnet restore "UI/UI.csproj"

# Copia todo el código fuente.
# La ruta de origen '.' se refiere al directorio del contexto de construcción (tu carpeta UI).
COPY . /src/UI
WORKDIR /src/UI

# Publica la aplicación en modo Release.
# La opción -o publica en el directorio 'app/out'.
RUN dotnet publish "UI.csproj" -c Release -o /app/out

# Usa una imagen base más pequeña para la ejecución final.
# Reemplaza '8.0' con la versión de .NET que uses.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

WORKDIR /app
# Copia los archivos publicados desde la etapa 'build'.
COPY --from=build /app/out .

# Expone el puerto por defecto de Kestrel (para HTTP y HTTPS).
EXPOSE 80
EXPOSE 443

# Define el punto de entrada para ejecutar la aplicación.
# El nombre del archivo DLL debe coincidir con el nombre de tu proyecto.
ENTRYPOINT ["dotnet", "UI.dll"]