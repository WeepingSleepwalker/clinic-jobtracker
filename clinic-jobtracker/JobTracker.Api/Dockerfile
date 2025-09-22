# --- build stage ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy solution
COPY JobTracker.Domain/ JobTracker.Domain/
COPY JobTracker.Application/ JobTracker.Application/
COPY JobTracker.Infrastructure/ JobTracker.Infrastructure/
COPY JobTracker.Api/ JobTracker.Api/

RUN dotnet restore JobTracker.Api/JobTracker.Api.csproj
RUN dotnet publish JobTracker.Api/JobTracker.Api.csproj -c Release -o /app/publish --no-restore

# --- runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# place DB under /app/data (bind-mounted volume)
RUN mkdir -p /app/data
ENV ASPNETCORE_URLS=http://0.0.0.0:5002
# Use env var–based connection string so compose can set it
ENV ConnectionStrings__Default="Data Source=/app/data/app.db"

COPY --from=build /app/publish ./
EXPOSE 5002
ENTRYPOINT ["dotnet", "JobTracker.Api.dll"]
