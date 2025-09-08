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

RUN echo "=== Project dir contents ===" && ls -R ./src/IAM/Identities/Interfaces || true
RUN echo "=== Proto dir contents ===" && ls -R ./src/IAM/Identities/Interfaces/Protos || true
RUN chmod -Rf +x ./packages/grpc.tools/2.57.0/tools

#RUN dotnet build ./src/IAM/Identities/Interfaces/IAM.Identities.Interfaces.csproj -c Debug -v:detailed

# MSBuild közvetlen hívása, kényszerített rebuild + részletes logok
# - /t:Rebuild: ne legyen up-to-date skip
# - /clp:...: részletes konzol, summary nélkül
# - /noconsolelogger + /fileLogger: minden menjen fájlba is
# - /bl: bináris log, Structured Log Viewer-rel nézhető
RUN dotnet msbuild ./src/IAM/Identities/Interfaces/IAM.Identities.Interfaces.csproj /t:Rebuild /p:ContinuousIntegrationBuild=true "/clp:Verbosity=diagnostic;NoSummary" /fileLogger "/flp:logfile=/artifacts/msbuild.log;verbosity=diagnostic" "/bl:/artifacts/msbuild.binlog"

RUN echo "=== Debug ===" && ls -lah -R ./src/IAM/Identities/Interfaces/obj/Debug || true
RUN echo "=== Obj ===" && ls -lah -R ./src/IAM/Identities/Interfaces/obj/Debug/net8.0 || true
RUN echo "=== Generated ===" && ls -lah -R ./src/IAM/Identities/Interfaces/obj/Debug/net8.0/Protos || true


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