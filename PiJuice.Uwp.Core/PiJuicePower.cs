using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PiJuice.Uwp.Core.Interface;

/// <summary>
/// Contains function for power handling. Ported to C# in December 2018 from Stephan Trautvetter.
/// Code is based on the origin project: https://github.com/PiSupply/PiJuice
/// </summary>
namespace PiJuice.Uwp.Core.Power
{

    #region Enums

    public enum PiJuicePowerCommands
    {
        WATCHDOG_ACTIVATION_CMD = 0x61,
        POWER_OFF_CMD = 0x62,
        WAKEUP_ON_CHARGE_CMD = 0x63,
        SYSTEM_POWER_SWITCH_CTRL_CMD = 0x64
    }

    #endregion

    #region Results

    public class PowerOffResult : ResultBase
    {
        public byte Delay { get; set; }
    }

    public class SystemPowerSwitchResult : ResultBase
    {
        public byte Value { get; set; }
    }

    public class WakeUpOnChargeResult : SystemPowerSwitchResult
    {

    }

    #endregion

    public class PiJuicePower
    {
        private PiJuiceInterface _Interface;

        public PiJuicePower(PiJuiceInterface iface)
        {
            _Interface = iface;
        }

        #region PowerOff

        public async Task<PowerOffResult> GetPowerOff()
        {
            PowerOffResult sres = null;

            try
            {
                var ires = await _Interface.ReadData(PiJuicePowerCommands.POWER_OFF_CMD, 1);

                if (!ires.Success)
                    return new PowerOffResult();

                sres = new PowerOffResult() { Delay = ires.Response[0], Success = true };

                return sres;
            }
            catch(Exception ex) 
            {
                return new PowerOffResult();
            }
        }

        public async Task<bool> SetPowerOff(byte delay)
        {
            try
            {
                var data = new byte[] { delay, 0 };
                var ires = await _Interface.WriteData((byte)PiJuicePowerCommands.POWER_OFF_CMD, data);

                return ires.Success;
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                return false;
            }
        }

        #endregion

        #region SystemPowerSwitch

        public async Task<SystemPowerSwitchResult> GetSystemPowerSwitch()
        {
            try
            {
                var ires = await _Interface.ReadData(PiJuicePowerCommands.SYSTEM_POWER_SWITCH_CTRL_CMD, 1);

                if (!ires.Success)
                    return new SystemPowerSwitchResult();

                return new SystemPowerSwitchResult()
                {
                    Value = ires.Response[0],
                    Success = true,
                };
            }
            catch (Exception ex)
            {
                return new SystemPowerSwitchResult() { Success = false };
            }
        }

        public async Task<bool> SetSystemPowerSwitch(UInt16 value)
        {
            try
            {
                var data = new byte[] { (byte)(value & 0xff), (byte)((value >> 8) & 0xff) };
                var ires = await _Interface.WriteData((byte)PiJuicePowerCommands.SYSTEM_POWER_SWITCH_CTRL_CMD, data);

                return ires.Success;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        #endregion

        #region WakeUpOnCharge

        public async Task<WakeUpOnChargeResult> GetWakeUpOnCharge()
        {
            try
            {
                var ires = await _Interface.ReadData(PiJuicePowerCommands.WAKEUP_ON_CHARGE_CMD, 1);

                if (!ires.Success)
                    return new WakeUpOnChargeResult();

                return new WakeUpOnChargeResult()
                {
                    Value = ires.Response[0],
                    Success = true,
                };
            }
            catch (Exception ex)
            {
                return new WakeUpOnChargeResult() { Success = false };
            }
        }

        public async Task<bool> SetWakeUpOnCharge(byte value)
        {
            try
            {
                var data = new byte[] { value, 0 };
                var ires = await _Interface.WriteData((byte)PiJuicePowerCommands.WAKEUP_ON_CHARGE_CMD, data);

                return ires.Success;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        #endregion
    }
}
