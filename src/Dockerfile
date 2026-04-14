FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Order.WebAPI/Order.WebAPI.csproj", "Order.WebAPI/"]
COPY ["Order.Service/Order.Service.csproj", "Order.Service/"]
COPY ["Order.Model/Order.Model.csproj", "Order.Model/"]
COPY ["Order.Data/Order.Data.csproj", "Order.Data/"]
RUN dotnet restore "Order.WebAPI/Order.WebAPI.csproj"
COPY . .
WORKDIR "/src/Order.WebAPI"
RUN dotnet build "Order.WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Order.WebAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Order.WebAPI.dll"]