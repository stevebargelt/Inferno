dotnet publish -r linux-arm
scp pi@10.0.20.30:/home/pi/inferno/listener/appsettings.json ./temp.json
scp -r ./bin/Debug/netcoreapp3.0/linux-arm/publish/* pi@10.0.20.30:~/inferno/listener
scp ./temp.json pi@10.0.20.30:/home/pi/inferno/listener/appsettings.json
# rm temp.json