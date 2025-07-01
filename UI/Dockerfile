
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src


COPY ["UI.csproj", "UI/"]
RUN dotnet restore "UI/UI.csproj"

COPY . /src/UI

WORKDIR /src/UI


FROM mcr.microsoft.com/dotnet/sdk:9.0 AS final 

WORKDIR /app


ENV ASPNETCORE_ENVIRONMENT=Development 
ENV DOTNET_RUNNING_IN_CONTAINER=true


EXPOSE 80
EXPOSE 443


ENTRYPOINT ["dotnet", "watch", "run", "--project", "UI.csproj", "--urls", "http://+:80"]

