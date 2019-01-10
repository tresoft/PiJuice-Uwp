using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using PiJuice.Uwp.Core.Status;
using System.Threading;
using PiJuice.Uwp.Core.Power;

/// <summary>
/// Contains function for interface handling. Ported to C# in December 2018 from Stephan Trautvetter.
/// Code is based on the origin project: https://github.com/PiSupply/PiJuice
/// </summary>
namespace PiJuice.Uwp.Core.Interface
{
    public class ResultBase
    {
        public bool Success { get; set; }

        public ResultBase()
        {
            Success = false;
        }
    }

    public class PiJuiceInterfaceResult
    {
        public bool Success { get; set; }
        public Exception LastException { get; set; }
        public byte[] Request { get; set; }
        public byte[] Response { get; set; }
        public TimeSpan Delay { get; set; }
        public int RetryCounter { get; set; }
    }

    public class PiJuiceInterface : IDisposable
    {
        private byte _Bus;
        private byte _Address;
        private I2cDevice _Device = null;
        private object _DeviceLock = new object();

        public bool Shutdown { get; set; }
        public int ReadRetryCounter { get; set; }
        public int ReadRetryDelay { get; set; }
        public I2cBusSpeed Speed { get; set; }

        public PiJuiceInterface(byte bus = 1, byte address = 0x14)
        {
            Speed = I2cBusSpeed.StandardMode;
            Shutdown = false;
            ReadRetryCounter = 3;
            ReadRetryDelay = 5;

            _Bus = bus;
            _Address = address;
        }

        public void Dispose()
        {
            if (_Device != null)
            {
                _Device.Dispose();
                _Device = null;
            }
        }

        public async Task<bool> InitAsync()
        {
            if (_Device == null && !Shutdown)
            {
                var controlerName = $"I2C{_Bus}";
                var i2cSettings = new I2cConnectionSettings(_Address) { BusSpeed = Speed };
                var deviceSelector = I2cDevice.GetDeviceSelector(controlerName);
                var i2cDeviceControllers = await DeviceInformation.FindAllAsync(deviceSelector);
                if (i2cDeviceControllers != null && i2cDeviceControllers.Any())
                    _Device = await I2cDevice.FromIdAsync(i2cDeviceControllers[0].Id, i2cSettings);
            }

            return _Device != null;
        }

        #region reading data

        public async Task<PiJuiceInterfaceResult> ReadData(PiJuiceStatusCommands cmd, byte lenght)
        {
            return await ReadData((byte)cmd, lenght);
        }

        public async Task<PiJuiceInterfaceResult> ReadData(PiJuicePowerCommands cmd, byte lenght)
        {
            return await ReadData((byte)cmd, lenght);
        }

        public async Task<PiJuiceInterfaceResult> ReadData(byte cmd, byte lenght)
        {
            PiJuiceInterfaceResult result = new PiJuiceInterfaceResult();
            var StartTime = DateTime.Now;

            if (Shutdown)
                return result;

            try
            {
                // init  device
                var dok = await InitAsync();
                if (!dok)
                    throw new Exception("Device initialisation error");

                // do transfer
                for (int i = 0; i < ReadRetryCounter; i++)
                {
                    // init
                    result.RetryCounter = i;
                    result.Request = new byte[] { (byte)cmd };
                    result.Response = new byte[lenght + 1];
                    // send request and read response
                    _Device.WriteRead(result.Request, result.Response);
                    // calc checksum
                    var cs = GetCheckSum(result.Response);
                    if (cs == result.Response[lenght])
                    {
                        // finish
                        result.Success = true;
                        return result;
                    }

                    await Task.Delay(ReadRetryDelay);
                }

                result.Success = false;
                return result;
            }
            catch (Exception ex)
            {
                result.LastException = ex;
                result.Success = false;
                return result;
            }
            finally
            {
                result.Delay = DateTime.Now - StartTime;
            }
        }

        #endregion

        #region writing data

        public async Task<PiJuiceInterfaceResult> WriteData(byte cmd, byte[] data)
        {
            PiJuiceInterfaceResult result = new PiJuiceInterfaceResult();
            var StartTime = DateTime.Now;

            if (Shutdown)
                return result;

            try
            {
                // init  device
                var dok = await InitAsync();
                if (!dok)
                    throw new Exception("Device initialisation error");

                // create request
                var buffer = new byte[data.Length + 2];
                Array.Copy(data, 0, buffer, 1, data.Length);
                buffer[0] = cmd;
                buffer[buffer.Length - 1] = GetCheckSum(data, all: true);
                result.Request = buffer.ToArray();
                // write
                _Device.Write(result.Request);
                // finish
                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.LastException = ex;
                result.Success = false;
                return result;
            }
            finally
            {
                result.Delay = DateTime.Now - StartTime;
            }
        }

        #endregion

        #region validate data

        private byte GetCheckSum(byte[] data, bool all = false)
        {
            byte fcs = 0xff;

            for (int i = 0; i < data.Length - (all ? 0 : 1); i++)
                fcs = (byte)(fcs ^ data[i]);

            return fcs;
        }

        #endregion
    }
}
