dotnet publish -r linux-arm
scp -r ./bin/Debug/netcoreapp3.0/linux-arm/publish/* pi@10.0.20.30:~/inferno/api
# ssh pi@10.0.20.30 sudo reboot