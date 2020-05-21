using System;
using System.Device.I2c;
using System.Device.Gpio;
using Inferno.Api.Interfaces;
using Inferno.Common.Models;
using Iot.Device.CharacterLcd;
using Iot.Device.Pcx857x;

namespace Inferno.Api.Devices
{
    public class Display : IDisplay, IDisposable
    {
        I2cDevice _i2c;
        Pcf8574 _pcf;
        Lcd2004 _lcd;

        public Display()
        {
            Init();
        }

        public void Init()
        {
            _i2c = I2cDevice.Create(new I2cConnectionSettings(1, 0x27));
            _pcf = new Pcf8574(_i2c);
            _lcd = new Lcd2004(registerSelectPin: 0, enablePin: 2, dataPins: new int[] { 4, 5, 6, 7 },
            backlightPin: 3, readWritePin: 1, controller: _pcf);
        }

        public void DisplayInfo(Temps temps, string mode, string hardwareStatus)
        {
            string grillLabel = "Grill: ";
            string probe1Label = "P1: ";
            string probe2Label = "P2: ";
            string probe3Label = "P3: ";
            string probe4Label = "P4: ";
            string grillValue = (temps.GrillTemp == -1) ? "Unplg" : $"{temps.GrillTemp}*F";
            string probe1Value = (temps.Probe1Temp == -1) ? "Unplg" : $"{temps.Probe1Temp}*F";
            string probe2Value = (temps.Probe2Temp == -1) ? "Unplg" : $"{temps.Probe2Temp}*F";
            string probe3Value = (temps.Probe3Temp == -1) ? "Unplg" : $"{temps.Probe3Temp}*F";
            string probe4Value = (temps.Probe4Temp == -1) ? "Unplg" : $"{temps.Probe4Temp}*F";

            _lcd.SetCursorPosition(0, 0);
            _lcd.Write(JustifyWithSpaces((probe1Label + probe1Value), (probe2Label + probe2Value)));
            _lcd.SetCursorPosition(0, 1);
            _lcd.Write(JustifyWithSpaces((probe3Label + probe3Value), (probe4Label + probe4Value)));
            _lcd.SetCursorPosition(0, 2);
            _lcd.Write((grillLabel + grillValue).PadRight(20));
            _lcd.SetCursorPosition(0, 3);
            _lcd.Write(JustifyWithSpaces(mode, hardwareStatus));
        }

        public void DisplayText(string line1 = "", string line2 = "", string line3 = "", string line4 = "")
        {
            _lcd.SetCursorPosition(0, 0);
            _lcd.Write(line1.PadRight(20));
            _lcd.SetCursorPosition(0, 1);
            _lcd.Write(line2.PadRight(20));
            _lcd.SetCursorPosition(0, 2);
            _lcd.Write(line3.PadRight(20));
            _lcd.SetCursorPosition(0, 3);
            _lcd.Write(line4.PadRight(20));
        }

        private string JustifyWithSpaces(string string1, string string2, int maxChars = 20)
        {
            if (string1.Length + string2.Length > maxChars)
            {
                if (string1.Length > 10)
                    string1 = string1.Substring(0, 10);

                if (string2.Length > 10)
                    string2 = string2.Substring(0, 10);
            }

            string spaces = new string(' ', (maxChars - (string1.Length + string2.Length)));
            return $"{string1}{spaces}{string2}";
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    DisplayText("Shutting down...", "", "", "Goodbye!".PadLeft(20));
                    _i2c.Dispose();
                    _lcd.Dispose();
                    _pcf.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Display()
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