# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CommissionQuote.sln .
COPY src/CommissionQuote.Service/CommissionQuote.Service.csproj src/CommissionQuote.Service/
RUN dotnet restore src/CommissionQuote.Service/CommissionQuote.Service.csproj

COPY src/CommissionQuote.Service/ src/CommissionQuote.Service/
RUN dotnet publish src/CommissionQuote.Service/CommissionQuote.Service.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "CommissionQuote.Service.dll"]
