dotnet publish -r linux-arm
scp pi@10.0.20.30:/home/pi/inferno/api/appsettings.json ./temp.json
scp -r ./bin/Debug/netcoreapp3.0/linux-arm/publish/* pi@10.0.20.30:~/inferno/api
scp ./temp.json pi@10.0.20.30:/home/pi/inferno/api/appsettings.json

# ssh pi@10.0.20.30 sudo reboot
