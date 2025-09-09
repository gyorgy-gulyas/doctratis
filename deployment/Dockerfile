# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Teljes forrás bemásolása
COPY . .

# Lecseréljük a repo-ban levő nuget.config-okat a sajátunkra
RUN rm -f nuget.config && \
    cp nuget.docker.config nuget.config && \
    find . -type f -iname "nuget.config" ! -path "./nuget.config" -delete || true


# Build ARG – paraméterezett csproj
ARG CS_PROJ_PATH
RUN dotnet restore "$CS_PROJ_PATH" --configfile nuget.config
RUN dotnet publish "$CS_PROJ_PATH" -c Debug -o /app/publish

# --- Final stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

LABEL app.kubernetes.io/part-of="docratis"

# Build ARG – paraméterezett entrypoint
ARG ENTRY_DLL
ENV ENTRY_DLL=$ENTRY_DLL
#CMD ["sh", "-c", "dotnet $ENTRY_DLL"]
ENTRYPOINT dotnet $ENTRY_DLL