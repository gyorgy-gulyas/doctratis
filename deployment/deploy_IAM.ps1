helm uninstall identity-and-access-management --namespace docratis --wait
docker build -f ./deployment/Dockerfile --build-arg CS_PROJ_PATH=./src/IAM/Service/IAM.Service.csproj --build-arg ENTRY_DLL=IAM.Service.dll  -t docratis/identity-and-access-management .
helm upgrade --install identity-and-access-management ./deployment/docratis-services --values ./deployment/docratis-services/values.yaml --values ./src/IAM/Service/deployment/values.yaml --namespace docratis
kubectl rollout restart deployment identity-and-access-management -n docratis