docker build -f ./deployment/Dockerfile --build-arg CS_PROJ_PATH=./src/TemplateManagement/Service/TemplateManagement.Service.csproj --build-arg ENTRY_DLL=TemplateManagement.Service.dll  -t docratis/template-management .
docker build -f ./deployment/Dockerfile --build-arg CS_PROJ_PATH=./src/IAM/Service/IAM.Service.csproj --build-arg ENTRY_DLL=IAM.Service.dll  -t docratis/identity-and-access-management .

kubectl create namespace docratis
kubectl create namespace docratis-infra
kubectl config set-context --current --namespace=doctratis

helm upgrade --install template-management ./deployment/docratis-services --values ./deployment/docratis-services/values.yaml --values ./src/TemplateManagement/Service/deployment/values.yaml --namespace docratis
helm upgrade --install identity-and-access-management ./deployment/docratis-services --values ./deployment/docratis-services/values.yaml --values ./src/IAM/Service/deployment/values.yaml --namespace docratis

-- KONG Ingress --
helm repo add kong https://charts.konghq.com
helm repo update
helm upgrade --install kong-gateway kong/kong -n docratis-infra -f ./deployment/kong/values.yaml
kubectl apply -n docratis-infra -f ./deployment/kong/ingress.yaml

-- Metrics Ingress --
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


-- docker image ls
-- kubectl get pods
-- kubectl get ingress -n docratis
-- kubectl describe ingress -n docratis
-- helm list -n docratis-infra
-- kubectl rollout restart deployment identity-and-access-management -n docratis
-- kubectl rollout restart deployment template-management -n docratis


kubectl get events -n docratis --field-selector "involvedObject.kind=Ingress,involvedObject.name=apps-gateway" ` --sort-by=.lastTimestamp | Select-Object -Last 10

kubectl run -it --rm curl --image=curlimages/curl -n docratis -- sh -lc "set -x; curl -i http://template-management:8080/projects/projectif/v1/listaccessibleprojects; echo; curl -i http://template-management:8080/templatemanagement/projects/projectif/v1/listaccessibleprojects; echo; curl -i http://template-management:8080/swagger/index.html || true"


http://localhost/templatemanagement/projects/projectif/v1/listaccessibleprojects
http://template-management:8080/templatemanagement/projects/projectif/v1/listaccessibleprojects


kubectl get deploy -n docratis template-management `-o jsonpath='{.spec.template.spec.containers[0].image}{"`n"}'
kubectl get deploy -n docratis identity-management `-o jsonpath='{.spec.template.spec.containers[0].image}{"`n"}'