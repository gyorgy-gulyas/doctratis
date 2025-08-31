helm uninstall template-management --namespace docratis --wait 
docker build -f ./deployment/Dockerfile --build-arg CS_PROJ_PATH=./src/TemplateManagement/Service/TemplateManagement.Service.csproj --build-arg ENTRY_DLL=TemplateManagement.Service.dll  -t docratis/template-management .
helm upgrade --install template-management ./deployment/docratis-services --values ./deployment/docratis-services/values.yaml --values ./src/TemplateManagement/Service/deployment/values.yaml --namespace docratis
kubectl rollout restart deployment template-management -n docratis