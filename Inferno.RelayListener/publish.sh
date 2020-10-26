dotnet publish -r linux-arm
scp pi@10.0.20.30:/home/pi/inferno/listener/appsettings.json ./temp.json
scp -r ./bin/Debug/netcoreapp3.0/linux-arm/publish/* pi@10.0.20.30:~/inferno/listener
scp ./temp.json pi@10.0.20.30:/home/pi/inferno/listener/appsettings.json
#scp ./infernorelaylistener.service pi@10.0.20.30:/etc/systemd/system/infernorelaylistener.service
# the above command fails... so copying to home...
scp ./infernorelaylistener.service pi@10.0.20.30:/home/pi/infernorelaylistener.service
ssh pi@10.0.20.30 sudo mv /home/pi/infernorelaylistener.service /etc/systemd/system/infernorelaylistener.service
# rm temp.json