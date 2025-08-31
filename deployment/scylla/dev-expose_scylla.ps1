# --- Settings ---
$Namespace   = "docratis-store"
$ServiceName = "scylla-external"   # NodePort service (see dev-external-svc.yaml)
$LocalPort   = 29042               # local host port for non-TLS CQL
$ProxyName   = "scylla-port-proxy" # docker container name

# --- Node IP (Docker Desktop: INTERNAL-IP works) ---
$NodeIP = (kubectl get nodes -o json |
  ConvertFrom-Json).items[0].status.addresses |
  Where-Object { $_.type -eq "InternalIP" } |
  Select-Object -ExpandProperty address
if (-not $NodeIP) { throw "Node IP not found." }

# --- NodePort from the Service ---
$NodePort = kubectl -n $Namespace get svc $ServiceName -o jsonpath="{.spec.ports[?(@.name=='cql')].nodePort}"
if (-not $NodePort) { throw "NodePort (cql) not found on service: $Namespace/$ServiceName" }

# --- Clean old proxy container if exists ---
docker rm -f $ProxyName 2>$null | Out-Null

# --- Start proxy container (host:LocalPort -> NodeIP:NodePort) ---
docker run -d --restart=always --name $ProxyName `
  -p ${LocalPort}:${LocalPort} alpine/socat `
  "tcp-listen:${LocalPort},reuseaddr,fork" "tcp:${NodeIP}:${NodePort}" | Out-Null

Write-Host "Proxy is running: $env:COMPUTERNAME:$LocalPort -> $NodeIP`:$NodePort (Service: $ServiceName)" -ForegroundColor Green
Write-Host "Try: docker run -it --rm --entrypoint cqlsh scylladb/scylla 127.0.0.1 $LocalPort"
