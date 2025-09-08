docker build -f ./deployment/Dockerfile --build-arg CS_PROJ_PATH=./src/TemplateManagement/Service/TemplateManagement.Service.csproj --build-arg ENTRY_DLL=TemplateManagement.Service.dll  -t docratis/template-management .
docker build -f ./deployment/Dockerfile --build-arg CS_PROJ_PATH=./src/IAM/Service/IAM.Service.csproj --build-arg ENTRY_DLL=IAM.Service.dll  -t docratis/identity-and-access-management .

kubectl create namespace docratis
kubectl create namespace docratis-infra
kubectl create namespace docratis-store
kubectl config set-context --current --namespace=doctratis

helm upgrade --install template-management ./deployment/docratis-services --values ./deployment/docratis-services/values.yaml --values ./src/TemplateManagement/Service/deployment/values.yaml --namespace docratis
helm upgrade --install identity-and-access-management ./deployment/docratis-services --values ./deployment/docratis-services/values.yaml --values ./src/IAM/Service/deployment/values.yaml --namespace docratis

# -- KONG Ingress --
helm repo add kong https://charts.konghq.com
helm repo update
helm upgrade --install kong-gateway kong/kong -n docratis-infra -f ./deployment/kong/values.yaml
kubectl apply -n docratis-infra -f ./deployment/kong/ingress.yaml

# -- Metrics Ingress --
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update
release:
	helm upgrade --install monitoring prometheus-community/kube-prometheus-stack -n docratis-infra -f ./deployment/monitoring/values.yaml
docker desktop:
	helm upgrade --install monitoring prometheus-community/kube-prometheus-stack -n docratis-infra -f ./deployment/monitoring/values.yaml --set kubeControllerManager.enabled=false --set kubeScheduler.enabled=false --set kubeProxy.enabled=false --set kubeEtcd.enabled=false
	kubectl apply -f deployment/kong/kong-servicemonitor.yaml
	kubectl apply -f deployment/kong/kong-metrics-service.yaml
	kubectl apply -f deployment/kong/kong-prometheus-plugin.yaml
	kubectl apply -f deployment/kong/ui-paths.yaml

promteus:
	kubectl -n docratis-infra port-forward svc/monitoring-kube-prometheus-prometheus 9090
	http://localhost:9090/targets
grafana:
	admin user és password lehkérdezése:
	kubectl -n docratis-infra get secret monitoring-grafana -o jsonpath="{.data.admin-user}" | % { [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($_)) }
	kubectl -n docratis-infra get secret monitoring-grafana -o jsonpath="{.data.admin-password}" | % { [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($_)) }
	kubectl -n docratis-infra port-forward svc/monitoring-grafana 3000:80
	http://localhost:3000/login

# ------------------MONGO------------------------------
helm repo add bitnami https://charts.bitnami.com/bitnami
helm repo update
kubectl apply -f ./deployment/mongo/secrets.yaml -n docratis-store
Dev:
	helm upgrade --install mongodb oci://registry-1.docker.io/bitnamicharts/mongodb -n docratis-store -f .\deployment\mongo\values.yaml -f .\deployment\mongo\dev-values.yaml
	kubectl apply -f .\deployment\mongo\dev-external-svc.yaml -n docratis-store
	powershell -File .\deployment\mongo\dev-expose_scylla.ps1
Prod:
	helm upgrade --install mongodb oci://registry-1.docker.io/bitnamicharts/mongodb -n docratis-store -f ./deployment/mongo/values.yaml
	kubectl apply -f .\deployment\mongo\prod-external-svc.yaml -n docratis-store

check:
kubectl -n docratis-store get pods -l app.kubernetes.io/instance=mongodb -o wide
kubectl -n docratis-store get svc mongodb,mongodb-headless,mongodb-external
kubectl -n docratis-store get svc mongodb-external
mongosh mongodb://root:DocratisMongoPassword@127.0.0.1:28000/admin?directConnection=true
# ASP.NET connection string
mongodb://root:DocratisMongoPassword@\mongodb-0.mongodb-headless.docratis-store.svc.cluster.local:27017/docratis_documents?replicaSet=rs0&authSource=admin

# ----------------- cert-manager ------------------------------------
# helm install cert-manager oci://quay.io/jetstack/charts/cert-manager --version v1.18.2 -n docratis-infra --set crds.enabled=true
helm install cert-manager oci://quay.io/jetstack/charts/cert-manager --version v1.18.2 -n docratis-infra -f .\deployment\cert-manager\values.yaml --set crds.enabled=true
# check
kubectl wait -n docratis-infra --for=condition=available deploy/cert-manager --timeout=180s
kubectl wait -n docratis-infra --for=condition=available deploy/cert-manager-webhook --timeout=180s
kubectl wait -n docratis-infra --for=condition=available deploy/cert-manager-cainjector --timeout=180s
kubectl -n docratis-infra get pods


# ----------------- CASSANDRA / SCYLLA -----------------------------------
# --- repo + operator ---
helm repo add scylla https://scylla-operator-charts.storage.googleapis.com/stable
helm repo update

helm upgrade --install scylla-operator scylla/scylla-operator -n scylla-operator --create-namespace
helm list -n scylla-operator | findstr /I scylla-operator

# --- auth bekapcsolás (CM) + secret + CQL configmap ---
kubectl apply -f .\deployment\scylla\auth-config.yaml
kubectl apply -f .\deployment\scylla\secrets.yaml
kubectl -n docratis-store create configmap scylla-bootstrap-cql --from-file=bootstrap.cql=.\deployment\scylla\bootstrap.cql --dry-run=client -o yaml | kubectl apply -f -

helm upgrade --install scylla scylla/scylla -n docratis-store -f ./deployment/scylla/values.yaml
# várj, míg a node-ok felállnak:
kubectl -n docratis-store wait --for=condition=Ready pod -l app.kubernetes.io/name=scylla --timeout=600s

Dev:
	kubectl apply -f .\deployment\scylla\dev-external-svc.yaml -n docratis-store
	powershell -File .\deployment\scylla\dev-expose_scylla.ps1
Prod:
	kubectl apply -f ./deployment/scylla/prod-external-svc.yaml -n docratis-store

# --- bootstrap job (user/pass + keyspace) ---
kubectl -n docratis-store delete job scylla-bootstrap --ignore-not-found
kubectl -n docratis-store get pods -l job-name=scylla-bootstrap -o name | ForEach-Object { kubectl -n docratis-store delete $_ }

kubectl apply -f .\deployment\scylla\bootstrap-job.yaml
kubectl -n docratis-store wait --for=condition=complete job/scylla-bootstrap --timeout=120s
kubectl -n docratis-store logs job/scylla-bootstrap

check:
# Operator települt?
helm list -n scylla-operator | findstr /I scylla-operator
# Scylla cluster release megvan?
helm list -n docratis-store | findstr /I scylla
# Podok állapota + node info
kubectl -n docratis-store get pods -l app.kubernetes.io/instance=scylla -o wide
# Kliens és az external service-ek
kubectl -n docratis-store get svc scylla-client -o jsonpath="{.spec.selector}"
kubectl -n docratis-store get svc scylla-external
# Endpointok (van-e névszerint 3 tag):
kubectl -n docratis-store get endpointslice -l kubernetes.io/service-name=scylla-client
# cqlsh (nem TLS, 9042) – gyors próba
docker run -it --rm --entrypoint cqlsh scylladb/scylla:5.4.3 127.0.0.1 29042
# cql (9042) NodePort:
kubectl -n docratis-store get svc scylla-external -o jsonpath="{.spec.ports[?(@.name=='cql')].nodePort}"
echo.
# cql-tls (9142) NodePort:
kubectl -n docratis-store get svc scylla-external -o jsonpath="{.spec.ports[?(@.name=='cql-tls')].nodePort}"
echo.

# CQLS connection test
$sec = kubectl -n docratis-store get secret scylla-app-user -o json | ConvertFrom-Json
$U = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($sec.data.username))
$P = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($sec.data.password))
docker run -it --rm --entrypoint cqlsh scylladb/scylla host.docker.internal 29042 -u $U -p $P

# cassandra GUI dev-en, és teszt
docker rm -f cassandra-web 2>$null
# ipushc/cassandra-web a XXXX-es porton futtatva
docker run -d --name cassandra-web -p 9876:3000 -e HOST_PORT=":3000" -e APP_PATH="/" -e CASSANDRA_HOST=host.docker.internal -e CASSANDRA_PORT=29042 -e CASSANDRA_USERNAME=$U -e CASSANDRA_PASSWORD=$P ipushc/cassandra-web:latest
docker run -d --name cassandra-web -p 9876:8083 -e CASSANDRA_HOST=host.docker.internal -e CASSANDRA_PORT=29042 -e CASSANDRA_USERNAME=docratis -e CASSANDRA_PASSWORD=DocratisScyllaPassword ipushc/cassandra-web:v1.1.0
docker logs cassandra-web --tail 200
Invoke-WebRequest http://localhost:9876/ | Select-Object StatusCode, StatusDescription
browser: http://localhost:9876/
# ASP.NET connection string
host=scylla-client.docratis-store.svc.cluster.local;port=9042;username=docratis;password=DocratisScyllaPassword;keyspace=docratis


-- docker image ls
-- kubectl get pods
-- kubectl get ingress -n docratis
-- kubectl describe ingress -n docratis
-- helm list -n docratis-store
-- helm list -n docratis-infra
-- kubectl rollout restart deployment identity-and-access-management -n docratis
-- kubectl rollout restart deployment template-management -n docratis


kubectl get events -n docratis --field-selector "involvedObject.kind=Ingress,involvedObject.name=apps-gateway" ` --sort-by=.lastTimestamp | Select-Object -Last 10

kubectl run -it --rm curl --image=curlimages/curl -n docratis -- sh -lc "set -x; curl -i http://template-management:8080/projects/projectif/v1/listaccessibleprojects; echo; curl -i http://template-management:8080/templatemanagement/projects/projectif/v1/listaccessibleprojects; echo; curl -i http://template-management:8080/swagger/index.html || true"


http://localhost/templatemanagement/projects/projectif/v1/listaccessibleprojects
http://template-management:8080/templatemanagement/projects/projectif/v1/listaccessibleprojects


kubectl get deploy -n docratis template-management `-o jsonpath='{.spec.template.spec.containers[0].image}{"`n"}'
kubectl get deploy -n docratis identity-management `-o jsonpath='{.spec.template.spec.containers[0].image}{"`n"}'




kubectl -n docratis-store run -it --rm cqltest --image=scylladb/scylla --restart=Never -- cqlsh scylla-client 9042 -u cassandra -p cassandra -e "SHOW VERSION"


Get-Content -Path $path -Encoding Byte -TotalCount 4 | ForEach-Object { "{0:X2}" -f $_ } -join " "