docker build -f ./deployment/Dockerfile --build-arg CS_PROJ_PATH=./src/TemplateManagement/Projects/Service/TemplateManagement.Projects.Service.csproj --build-arg ENTRY_DLL=TemplateManagement.Projects.Service.dll  -t docratis/template-management .
docker build -f ./deployment/Dockerfile --build-arg CS_PROJ_PATH=./src/Core/Identities/Service/Core.Identities.Service.csproj --build-arg ENTRY_DLL=Core.Identities.Service.dll  -t docratis/identity-management .

kubectl create namespace docratis
kubectl create namespace docratis-infra
kubectl config set-context --current --namespace=doctratis

helm upgrade --install template-management ./deployment/docratis-services --values ./deployment/docratis-services/values.yaml --values ./src/TemplateManagement/Projects/Service/deployment/values.yaml --namespace docratis
helm upgrade --install identity-management ./deployment/docratis-services --values ./deployment/docratis-services/values.yaml --values ./src/Core/Identities/Service/deployment/values.yaml --namespace docratis

-- docker image ls
-- kubectl get pods


helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo update
helm upgrade --install ingress-nginx ingress-nginx/ingress-nginx --namespace docratis-infra

-- kubectl get ingress -n docratis
-- kubectl describe ingress -n docratis