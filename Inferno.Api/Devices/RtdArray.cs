using System;
using System.Collections.Concurrent;
using System.Device.Spi;
using System.Threading.Tasks;
using Inferno.Api.Interfaces;
using Iot.Device.Mcp3008;
using System.Linq;

namespace Inferno.Api.Devices
{
    public class RtdArray : IRtdArray, IDisposable
    {
        Mcp3008 _adc;
        ConcurrentQueue<double> _grillResistances;
        ConcurrentQueue<double> _probe1Resistances;
        ConcurrentQueue<double> _probe2Resistances;
        ConcurrentQueue<double> _probe3Resistances;
        ConcurrentQueue<double> _probe4Resistances;

        Task _adcReadTask;

        public RtdArray(SpiDevice spi)
        {
            _adc = new Mcp3008(spi);
            _grillResistances = new ConcurrentQueue<double>();
            _probe1Resistances = new ConcurrentQueue<double>();
            _probe2Resistances = new ConcurrentQueue<double>();
            _probe3Resistances = new ConcurrentQueue<double>();
            _probe4Resistances = new ConcurrentQueue<double>();
            _adcReadTask = ReadAdc();
        }

        public double GrillTemp => Math.Round(RtdTempFahrenheitFromResistanceSteinhartHart("grill", _grillResistances.Average()), 0);
        public double Probe1Temp => Math.Round(RtdTempFahrenheitFromResistanceSteinhartHart("probe1", _probe1Resistances.Average()), 0);
        public double Probe2Temp => Math.Round(RtdTempFahrenheitFromResistanceSteinhartHart("probe2", _probe2Resistances.Average()), 0);
        public double Probe3Temp => Math.Round(RtdTempFahrenheitFromResistanceSteinhartHart("probe3", _probe3Resistances.Average()), 0);
        public double Probe4Temp => Math.Round(RtdTempFahrenheitFromResistanceSteinhartHart("probe4", _probe4Resistances.Average()), 0);

        private async Task ReadAdc()
        {
            while (true)
            {
                int grillValue = _adc.Read(0, Mcp3008.InputConfiguration.SingleEnded);
                int probe1Value = _adc.Read(1, Mcp3008.InputConfiguration.SingleEnded);
                int probe2Value = _adc.Read(2, Mcp3008.InputConfiguration.SingleEnded);
                int probe3Value = _adc.Read(3, Mcp3008.InputConfiguration.SingleEnded);
                int probe4Value = _adc.Read(4, Mcp3008.InputConfiguration.SingleEnded);
                _grillResistances.Enqueue(CalculateResistanceFromAdc("grill", grillValue));
                _probe1Resistances.Enqueue(CalculateResistanceFromAdc("probe1", probe1Value));
                _probe2Resistances.Enqueue(CalculateResistanceFromAdc("probe2", probe2Value));
                _probe3Resistances.Enqueue(CalculateResistanceFromAdc("probe3", probe3Value));
                _probe4Resistances.Enqueue(CalculateResistanceFromAdc("probe4", probe4Value));
                while (_grillResistances.Count > 100)
                {
                    double temp;
                    _grillResistances.TryDequeue(out temp);
                }
                while (_probe1Resistances.Count > 100)
                {
                    double temp;
                    _probe1Resistances.TryDequeue(out temp);
                }
                while (_probe2Resistances.Count > 100)
                {
                    double temp;
                    _probe2Resistances.TryDequeue(out temp);
                }
                while (_probe3Resistances.Count > 100)
                {
                    double temp;
                    _probe3Resistances.TryDequeue(out temp);
                }
                while (_probe4Resistances.Count > 100)
                {
                    double temp;
                    _probe4Resistances.TryDequeue(out temp);
                }
                await Task.Delay(TimeSpan.FromMilliseconds(10));
            }
        }

        static double CalculateResistanceFromAdc(string Title, double adcValue)
        {
            double rtdV = (adcValue / 1023) * 3.3;
            double R = ((3.3 * 1000) - (rtdV * 1000)) / rtdV;
            return R;
        }

        static double RtdTempFahrenheitFromResistanceSteinhartHart(string Title, double Resistance)
        {
            // from https://www.thinksrs.com/downloads/programs/therm%20calc/ntccalibrator/ntccalculator.html
            // Temps and resistances measured
            // R        c
            // 1008     0 
            // 1208     53.4
            // 1373     97.2
            double A = 70.27453460e-3;  // Coefficient A
            double B = -127.0393538e-4; // Coefficient B
            double C = 641.9441691e-7;  // Coefficient C

            // Excel       1 / (A + B * LN(R)                    + C * (POWER   (LN(R)               , 3)))
            double tempK = 1 / (A + B * Math.Log(Resistance) + C * (Math.Pow(Math.Log(Resistance), 3)));
            double tempC = tempK - 273.15;

            return tempC * 9 / 5 + 32;
        }

        static double RtdTempFahrenheitFromResistance(string Title, double Resistance)
        {

            double A = 3.90830e-3; // Coefficient A
            double B = -5.775e-7; // Coefficient B
            double ReferenceResistor = 1000;
            Console.Write(Title);
            Console.Write(" resistance=");
            Console.Write(Resistance);
            Console.Write(" tempC=");
            double TempCelsius = (-A + Math.Sqrt(A * A - 4 * B * (1 - Resistance / ReferenceResistor))) / (2 * B);
            Console.WriteLine(TempCelsius);
            return TempCelsius * 9 / 5 + 32;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _adc.Dispose();
                }

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~TempProbes()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}