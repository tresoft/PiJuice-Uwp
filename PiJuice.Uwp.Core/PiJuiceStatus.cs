using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PiJuice.Uwp.Core.Interface;

/// <summary>
/// Contains function for status handling. Ported to C# in December 2018 from Stephan Trautvetter.
/// Code is based on the origin project: https://github.com/PiSupply/PiJuice
/// </summary>
namespace PiJuice.Uwp.Core.Status
{

    #region Enums

    public enum PiJuiceStatusCommands
    {
        STATUS_CMD = 0x40,
        CHARGE_LEVEL_CMD = 0x41,
        FAULT_EVENT_CMD = 0x44,
        BUTTON_EVENT_CMD = 0x45,
        BATTERY_TEMPERATURE_CMD = 0x47,
        BATTERY_VOLTAGE_CMD = 0x49,
        BATTERY_CURRENT_CMD = 0x4b,
        IO_VOLTAGE_CMD = 0x4d,
        IO_CURRENT_CMD = 0x4f,
        LED_STATE_CMD = 0x66,
        LED_BLINK_CMD = 0x68,
        IO_PIN_ACCESS_CMD = 0x75,
    }

    public enum BatteryStates
    {
        NORMAL = 0,
        CHARGING_FROM_IN,
        CHARGING_FROM_5V_IO,
        NOT_PRESENT,
    }

    public enum PowerInStates
    {
        NOT_PRESENT = 0,
        BAD,
        WEAK,
        PRESENT,
    }

    public enum BatteryChargingTempFaults
    {
        NORMAL = 0,
        SUSPEND,
        COOL,
        WARM
    }

    #endregion

    #region Results

    public class LedStateResult : ResultBase
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public LedStateResult() { }

        public LedStateResult(byte[] data)
        {
            R = data[0];
            G = data[1];
            B = data[2];
        }

        public byte[] ToArray()
        {
            return new byte[] { R, G, B };
        }

        public override string ToString()
        {
            return $"R={R} G={G} B={B}";
        }
    }

    public class StatusResult
    {
        public bool Success { get; set; }
        public bool IsFault { get; set; }
        public bool IsButton { get; set; }
        public BatteryStates Battery { get; set; }
        public PowerInStates PowerInput { get; set; }
        public PowerInStates PowerInput5vIo { get; set; }

        public StatusResult() { }

        public StatusResult(byte d, bool success)
        {
            Success = success;
            if (!Success)
                return;

            IsFault = (d & 0x01) == 0x01;
            IsButton = (d & 0x02) == 0x02;
            Battery = (BatteryStates)((d >> 2) & 0x03);
            PowerInput = (PowerInStates)((d >> 4) & 0x03);
            PowerInput5vIo = (PowerInStates)((d >> 6) & 0x03);
        }
    }

    public class FaultResult
    {
        public bool Success { get; set; }
        public bool ButtonPowerOff { get; private set; }
        public bool ForcedPowerOff { get; private set; }
        public bool ForcedSysPowerOff { get; private set; }
        public bool WatchdogReset { get; private set; }
        public bool BatteryProfilInvalid { get; private set; }
        public BatteryChargingTempFaults BatteryChargingTempFault { get; private set; }

        public FaultResult() { }

        public FaultResult(byte d, bool success)
        {
            Success = success;
            if (!Success)
                return;

            ButtonPowerOff = (d & 0x01) == 0x01;
            ForcedPowerOff = (d & 0x02) == 0x02;
            ForcedSysPowerOff = (d & 0x04) == 0x04;
            WatchdogReset = (d & 0x08) == 0x08;
            BatteryProfilInvalid = (d & 0x20) == 0x20;
            BatteryChargingTempFault = (BatteryChargingTempFaults)((d >> 6) & 0x03);
        }

        public override string ToString()
        {
            return $"ButtonPowerOff={ButtonPowerOff} ForcedPowerOff={ForcedPowerOff} ForcedSysPowerOff={ForcedSysPowerOff} WatchdogReset={WatchdogReset} BatteryProfilInvalid={BatteryProfilInvalid} BatteryChargingTempFault={BatteryChargingTempFault}";
        }
    }

    public class ChargeLevelResult
    {
        public bool Success { get; set; }
        public byte ChargeLevel { get; set; }

        public ChargeLevelResult() { }

        public ChargeLevelResult(byte d, bool success)
        {
            Success = success;
            if (!Success)
                return;

            ChargeLevel = d;
        }
    }
    
    public class StatusUint16Result
    {
        public bool Success { get; set; }
        public UInt16 Value { get; set; }

        public StatusUint16Result() { }

        public StatusUint16Result(byte[] d , bool success, int offset = 0)
        {
            Success = success;
            if (!Success)
                return;

            Value = (UInt16)((d[offset + 1] << 8) | d[offset + 0]);
        }
    }

    public class StatusDoubleResult
    {
        public bool Success { get; set; }
        public double Value { get; set; }

        public StatusDoubleResult() { }

        public StatusDoubleResult(byte[] d, bool success, int offset = 0)
        {
            Success = success;
            if (!Success)
                return;

            //var l = (double)((d[offset + 1] << 8) | d[offset + 0]);
            var l = (double)BitConverter.ToInt16(d, offset);
            Value = l / 1000.0;
        }
    }

    #endregion

    public class PiJuiceStatus
    {
        private PiJuiceInterface _Interface;

        public PiJuiceStatus(PiJuiceInterface iface)
        {
            _Interface = iface;
        }

        public async Task<StatusResult> GetStatus()
        {
            StatusResult sres = null;

            try
            {
                var ires = await _Interface.ReadData(PiJuiceStatusCommands.STATUS_CMD, 1);

                if (!ires.Success)
                    return new StatusResult() { Success = false };

                sres = new StatusResult(ires.Response[0], true);

                return sres;
            }
            catch(Exception ex)
            {
                return new StatusResult() { Success = false };
            }
        }

        public async Task<FaultResult> GetFaultStatus()
        {
            FaultResult sres = null;

            try
            {
                var ires = await _Interface.ReadData(PiJuiceStatusCommands.FAULT_EVENT_CMD, 1);

                if (!ires.Success)
                    return new FaultResult() { Success = false };

                sres = new FaultResult(ires.Response[0], true);

                return sres;
            }
            catch (Exception ex)
            {
                return new FaultResult() { Success = false };
            }
        }

        public async Task<ChargeLevelResult> GetChargeLevel()
        {
            ChargeLevelResult sres = null;

            try
            {
                var ires = await _Interface.ReadData(PiJuiceStatusCommands.CHARGE_LEVEL_CMD, 1);

                if (!ires.Success)
                    return new ChargeLevelResult() { Success = false };

                sres = new ChargeLevelResult(ires.Response[0], true);

                return sres;
            }
            catch (Exception ex)
            {
                return new ChargeLevelResult() { Success = false };
            }
        }

        public async Task<StatusUint16Result> GetBatteryTemperatur()
        {
            StatusUint16Result sres = null;

            try
            {
                var ires = await _Interface.ReadData(PiJuiceStatusCommands.BATTERY_TEMPERATURE_CMD, 2);

                if (!ires.Success)
                    return new StatusUint16Result() { Success = false };

                sres = new StatusUint16Result(ires.Response, true);

                return sres;
            }
            catch (Exception ex)
            {
                return new StatusUint16Result() { Success = false };
            }
        }

        public async Task<StatusDoubleResult> GetBatteryVoltage()
        {
            StatusDoubleResult sres = null;

            try
            {
                var ires = await _Interface.ReadData(PiJuiceStatusCommands.BATTERY_VOLTAGE_CMD, 2);

                if (!ires.Success)
                    return new StatusDoubleResult() { Success = false };

                sres = new StatusDoubleResult(ires.Response, true);

                return sres;
            }
            catch (Exception ex)
            {
                return new StatusDoubleResult() { Success = false };
            }
        }

        public async Task<StatusDoubleResult> GetBatteryCurrent()
        {
            StatusDoubleResult sres = null;

            try
            {
                var ires = await _Interface.ReadData(PiJuiceStatusCommands.BATTERY_CURRENT_CMD, 2);

                if (!ires.Success)
                    return new StatusDoubleResult() { Success = false };

                sres = new StatusDoubleResult(ires.Response, true);

                return sres;
            }
            catch (Exception ex)
            {
                return new StatusDoubleResult() { Success = false };
            }
        }

        public async Task<StatusDoubleResult> GetIoVoltage()
        {
            StatusDoubleResult sres = null;

            try
            {
                var ires = await _Interface.ReadData(PiJuiceStatusCommands.IO_VOLTAGE_CMD, 2);

                if (!ires.Success)
                    return new StatusDoubleResult() { Success = false };

                sres = new StatusDoubleResult(ires.Response, true);

                return sres;
            }
            catch (Exception ex)
            {
                return new StatusDoubleResult() { Success = false };
            }

        }
        public async Task<StatusDoubleResult> GetIoCurrent()
        {
            StatusDoubleResult sres = null;

            try
            {
                var ires = await _Interface.ReadData(PiJuiceStatusCommands.IO_CURRENT_CMD, 2);

                if (!ires.Success)
                    return new StatusDoubleResult() { Success = false };

                sres = new StatusDoubleResult(ires.Response, true);

                return sres;
            }
            catch (Exception ex)
            {
                return new StatusDoubleResult() { Success = false };
            }
        }

        #region LEDs

        public async Task<LedStateResult> GetLedState(byte index)
        {
            try
            {
                var ires = await _Interface.ReadData(PiJuiceStatusCommands.LED_STATE_CMD + index, 3);

                if (!ires.Success)
                    return new LedStateResult();

                return new LedStateResult(ires.Response) { Success = true };
            }
            catch (Exception ex)
            {
                return new LedStateResult(); ;
            }
        }

        public async Task<bool> SetLedState(byte index, byte r, byte g, byte b)
        {
            try
            {
                var data = new byte[] { r, g, b };
                var ires = await _Interface.WriteData((byte)(PiJuiceStatusCommands.LED_STATE_CMD + index), data);

                if (!ires.Success)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        #endregion

    }
}
