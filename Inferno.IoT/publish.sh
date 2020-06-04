dotnet publish -r linux-arm
scp -r ./bin/Debug/netcoreapp3.1/linux-arm/publish/* pi@10.0.20.30:~/inferno/iot
