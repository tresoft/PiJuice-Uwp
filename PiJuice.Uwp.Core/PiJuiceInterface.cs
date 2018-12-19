using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using PiJuice.Uwp.Core.Status;
using System.Threading;

/// <summary>
/// Contains function for interface handling. Ported to C# in December 2018 from Stephan Trautvetter (st@trefon.de).
/// Code is based on the origin project: https://github.com/PiSupply/PiJuice
/// </summary>
namespace PiJuice.Uwp.Core.Interface
{
    public class PiJuiceInterfaceResult
    {
        public bool Success { get; set; }
        public Exception LastException { get; set; }
        public byte[] Request { get; set; }
        public byte[] Response { get; set; }
        public TimeSpan Delay { get; set; }
        public int RetryCounter { get; set; }
    }

    public class PiJuiceInterface: IDisposable
    {
        private byte _Bus;
        private byte _Address;
        private I2cBusSpeed _Speed;
        private I2cDevice _Device = null;
        private object _DeviceLock = new object();

        public PiJuiceInterface(byte bus = 1, byte address = 0x14, I2cBusSpeed speed = I2cBusSpeed.FastMode)
        {
            _Bus = bus;
            _Address = address;
            _Speed = speed;
        }

        public void Dispose()
        {
            if (_Device!=null)
            {
                _Device.Dispose();
                _Device = null;
            }
        }

        private async Task<bool> InitAsync()
        {
            if (_Device == null)
            {
                var controlerName = $"I2C{_Bus}";
                var i2cSettings = new I2cConnectionSettings(_Address) { BusSpeed = _Speed };
                var deviceSelector = I2cDevice.GetDeviceSelector(controlerName);
                var i2cDeviceControllers = await DeviceInformation.FindAllAsync(deviceSelector);
                if (i2cDeviceControllers != null && i2cDeviceControllers.Any())
                    _Device = await I2cDevice.FromIdAsync(i2cDeviceControllers[0].Id, i2cSettings);
            }

            return _Device != null;
        }

        public async Task<PiJuiceInterfaceResult> ReadData(PiJuiceStatusCommands cmd, byte lenght)
        {
            PiJuiceInterfaceResult result = new PiJuiceInterfaceResult();
            int RetryCounter = 3;
            var StartTime = DateTime.Now;

            try
            {
                // init  device
                var dok = await InitAsync();
                if (!dok)
                    throw new Exception("Device initialisation error");

                // do transfer
                for (int i = 0; i < RetryCounter; i++)
                {
                    // init
                    result.RetryCounter = i;
                    result.Request = new byte[] { (byte)cmd };
                    result.Response = new byte[lenght + 1];

                    // send request and read response
                    lock (_DeviceLock)
                    {
                        //// write and read
                        //var res = _Device.WriteReadPartial(result.Request, result.Response);
                        //if (res.Status == I2cTransferStatus.FullTransfer)
                        //{
                        //    // finish
                        //    result.Success = true;
                        //    return result;
                        //}

                        // write
                        _Device.Write(result.Request);

                        Thread.Sleep(1);

                        // read
                        _Device.Read(result.Response);
                        // calc checksum
                        var cs = GetCheckSum(result.Response);
                        if (cs == result.Response[lenght])
                        {
                            // finish
                            result.Success = true;
                            return result;
                        }

                    }

                    await Task.Delay(10);
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

        private byte GetCheckSum(byte[] data)
        {
            var fcs = 0xff;

            for (int i = 0; i < data.Length - 1; i++)
                fcs = fcs ^ data[i];

            return (byte)(fcs & 0xff);
        }

        //private async Task DoTransfer()
        //{
        //    var req = new byte[_Lenght];
        //    var res = new byte[10];

        //    req[0] = _Address;
        //    req[1] = (byte) _Cmd;

        //    if (_Data != null && _Data.Length > 0)
        //        Array.Copy(_Data, 0, req, 0, _Data.Length);

        //    // write request to device
        //    lock (_DeviceLock)
        //        _Device.Write(req);

        //    // read response from device

        //    lock(_DeviceLock)
        //        _Device.Read()
        //}

    }
}
