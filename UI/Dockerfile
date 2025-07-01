# --- Dockerfile (Colocar este archivo dentro de la carpeta 'UI') ---
# Este Dockerfile sirve para desarrollo con dotnet watch y para ejecución.
# NOTA: Para producción REAL, se recomienda un Dockerfile distinto y más ligero.

# 1. ETAPA DE CONSTRUCCIÓN (usamos el SDK para restaurar y copiar el código)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copia el archivo .csproj primero para restaurar dependencias.
COPY ["UI.csproj", "UI/"]
RUN dotnet restore "UI/UI.csproj"

# Copia todo el código fuente al directorio de trabajo del build.
# Este código se usará si no hay un volumen montado (ejecución "normal").
# Si hay un volumen montado para 'dotnet watch', este COPY será sobrescrito en /app.
COPY . /src/UI

WORKDIR /src/UI

# NO PUBLICAMOS AQUÍ: dotnet watch run ejecutará directamente desde la fuente
# La publicación sería para crear una imagen más ligera para producción.
# RUN dotnet publish "UI.csproj" -c Release -o /app/out

# 2. ETAPA FINAL (basada en el SDK para usar 'dotnet watch')
# Esta será la imagen final que correrá tu aplicación.
# Debe ser una imagen SDK para que 'dotnet watch' esté disponible.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS final 

WORKDIR /app

# Configura el ambiente a Desarrollo por defecto para dotnet watch.
# Esto activa DeveloperExceptionPage, logging detallado, etc.
ENV ASPNETCORE_ENVIRONMENT=Development 
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Expone los puertos que Kestrel escuchará (HTTP y HTTPS)
EXPOSE 80
EXPOSE 443

# Define el punto de entrada para ejecutar la aplicación con 'dotnet watch'.
# Cuando ejecutes el contenedor y montes tu código local, dotnet watch detectará los cambios.
ENTRYPOINT ["dotnet", "watch", "run", "--project", "UI.csproj", "--urls", "http://+:80"]

# Si quieres ejecutar la aplicación sin dotnet watch (ej. como "producción" sin montar volumen de código):
# Cambiarías el ENTRYPOINT a: ["dotnet", "run", "--urls", "http://+:80"]
# O, si no montas el volumen de código, necesitarías la etapa de publicación y copiar los resultados:
# COPY --from=build /src/UI /app # Copia el código fuente desde la etapa build
# ENTRYPOINT ["dotnet", "run", "--urls", "http://+:80"]