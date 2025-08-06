# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# NuGet feed + helyi packages bemásolása
COPY nuget.docker.config ./nuget.config
COPY packages/ ./packages/

# Teljes forrás bemásolása
COPY . .

# Töröljük az összes nem kívánt nuget.config-ot, kivéve amit most másoltunk be
RUN find . -type f -iname "nuget.config" ! -path "./nuget.config" -delete

# Build ARG – paraméterezett csproj
ARG CS_PROJ_PATH
RUN dotnet restore "$CS_PROJ_PATH" --configfile nuget.config
RUN dotnet publish "$CS_PROJ_PATH" -c Release -o /app/publish

# --- Final stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Build ARG – paraméterezett entrypoint
ARG ENTRY_DLL
ENV ENTRY_DLL=$ENTRY_DLL
CMD ["sh", "-c", "dotnet $ENTRY_DLL"]