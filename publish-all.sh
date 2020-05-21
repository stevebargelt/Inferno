pushd ./Inferno.Cli
sh publish.sh
cd ../Inferno.RelayListener
sh publish.sh
cd ../Inferno.TemperatureLogger
sh publish.sh
cd ../Inferno.Api
sh publish.sh
popd