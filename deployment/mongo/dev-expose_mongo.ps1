# --- Beállítások ---
$Namespace   = "docratis-store"
$ServiceName = "mongodb-external"
$LocalPort   = 28000              # ezen a host porton lesz elérhető
$ProxyName   = "mongo-port-proxy" # docker container neve

# --- Node IP (Docker Desktop alatt az INTERNAL-IP jó) ---
$NodeIP = (kubectl get nodes -o json |
  ConvertFrom-Json).items[0].status.addresses |
  Where-Object { $_.type -eq "InternalIP" } |
  Select-Object -ExpandProperty address

if (-not $NodeIP) { throw "Node IP nem található." }

# --- NodePort lekérése a Service-ből ---
$NodePort = kubectl -n $Namespace get svc $ServiceName -o jsonpath="{.spec.ports[0].nodePort}"
if (-not $NodePort) { throw "NodePort nem található a szolgáltatáson: $Namespace/$ServiceName" }

# --- (Opció) Root jelszó kiolvasása és URL-encode ---
Add-Type -AssemblyName System.Web
$MongoPass = kubectl -n $Namespace get secret mongodb-auth -o jsonpath="{.data.mongodb-root-password}" |
  ForEach-Object { [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($_)) }
$MongoPassEnc = [System.Web.HttpUtility]::UrlEncode($MongoPass)

# --- Ha létezik régi proxy konténer, töröljük ---
docker rm -f $ProxyName 2>$null | Out-Null

# --- Proxy konténer indítása (host:27018 -> NodeIP:NodePort) ---
docker run -d --restart=always --name $ProxyName `
  -p ${LocalPort}:${LocalPort} alpine/socat `
  "tcp-listen:${LocalPort},reuseaddr,fork" "tcp:${NodeIP}:${NodePort}" | Out-Null

# --- Eredmény kiírása ---
Write-Host "Proxy is running: docker container = $ProxyName | $env:COMPUTERNAME:$LocalPort -> $NodeIP`:$NodePort"
$Conn = "mongodb://root:$MongoPassEnc@127.0.0.1:$LocalPort/admin?replicaSet=rs0"
Write-Host "Mongo Connbection String `"$Conn`"" -ForegroundColor Cyan
