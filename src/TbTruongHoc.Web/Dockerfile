# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0.406 AS build
WORKDIR /src

# Copy the SDK pin first so restore/build inside the container also resolves
# to the .NET 8 SDK (mirrors the repo-root global.json behavior).
COPY global.json ./
COPY src/TbTruongHoc.Web/TbTruongHoc.Web.csproj src/TbTruongHoc.Web/
RUN dotnet restore src/TbTruongHoc.Web/TbTruongHoc.Web.csproj

COPY src/TbTruongHoc.Web/ src/TbTruongHoc.Web/
RUN dotnet publish src/TbTruongHoc.Web/TbTruongHoc.Web.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Media is volume-mounted at runtime (see docker-compose.yml) so uploaded
# assets persist across container restarts/rebuilds. Owned by the non-root
# "app" user (below) so Piranha's local file storage can write into it.
RUN mkdir -p /app/wwwroot/uploads && chown -R app:app /app/wwwroot/uploads

# Run as the built-in non-root "app" user instead of root.
USER app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "TbTruongHoc.Web.dll"]
