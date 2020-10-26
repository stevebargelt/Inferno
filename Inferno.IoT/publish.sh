dotnet publish -r linux-arm
scp -r ./bin/Debug/netcoreapp3.1/linux-arm/publish/* pi@10.0.20.30:~/inferno/iot
scp ./infernoiot.service pi@10.0.20.30:/home/pi/infernoiot.service
ssh pi@10.0.20.30 sudo mv /home/pi/infernoiot.service /etc/systemd/system/infernoiot.service