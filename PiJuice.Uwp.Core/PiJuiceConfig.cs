using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PiJuice.Uwp.Core.Interface;

/// <summary>
/// Contains function for power handling. Ported to C# in December 2018 from Stephan Trautvetter.
/// Code is based on the origin project: https://github.com/PiSupply/PiJuice
/// </summary>
namespace PiJuice.Uwp.Core.Config
{

    #region Enums

    public enum PiJuiceConfigCommands
    {
        CHARGING_CONFIG_CMD = 0x51,
        BATTERY_PROFILE_ID_CMD = 0x52,
        BATTERY_PROFILE_CMD = 0x53,
        BATTERY_TEMP_SENSE_CONFIG_CMD = 0x5D,
        POWER_INPUTS_CONFIG_CMD = 0x5E,
        RUN_PIN_CONFIG_CMD = 0x5F,
        POWER_REGULATOR_CONFIG_CMD = 0x60,
        LED_CONFIGURATION_CMD = 0x6A,
        BUTTON_CONFIGURATION_CMD = 0x6E,
        IO_CONFIGURATION_CMD = 0x72,
        I2C_ADDRESS_CMD = 0x7C,
        ID_EEPROM_WRITE_PROTECT_CTRL_CMD = 0x7E,
        ID_EEPROM_ADDRESS_CMD = 0x7F,
        RESET_TO_DEFAULT_CMD = 0xF0,
        FIRMWARE_VERSION_CMD = 0xFD,
    }

    #endregion

    #region Results

    public class FirmwareResult : ResultBase
    {
        public Version Version { get; set; }
        public byte Format { get; set; }
    }

    public class ChargingConfigResult : ResultBase
    {
        public bool Enabled{ get; set; }
        public bool NonVolatile { get; set; }

        public ChargingConfigResult() : base()
        {
            Success = false;
        }

        public ChargingConfigResult(byte[] data) : base()
        {
            Enabled = (data[0] & 0x01) == 0x01;
            NonVolatile = (data[0] & 0x80) == 0x80;
        }

        public byte[] ToArray()
        {
            byte v = 0;
            if (Enabled)
                v |= 0x01;
            if (NonVolatile)
                v |= 0x80;
            return new byte[] { v, 0 };
        }
    }
    
    #endregion

    public class PiJuiceConfig
    {
        private PiJuiceInterface _Interface;

        public PiJuiceConfig(PiJuiceInterface iface)
        {
            _Interface = iface;
        }

        public async Task<FirmwareResult> GetFirmwareVersion()
        {
            try
            {
                var ires = await _Interface.ReadData((byte)PiJuiceConfigCommands.FIRMWARE_VERSION_CMD, 2);

                if (!ires.Success)
                    return new FirmwareResult();

                var major_version = ires.Response[0] >> 4;
                var minor_version = (ires.Response[0] << 4 & 0xf0) >> 4;

                return new FirmwareResult()
                {
                    Version = new Version(major_version, minor_version),
                    Format = ires.Response[1],
                    Success = true,
                };
            }
            catch (Exception ex)
            {
                return new FirmwareResult();
            }
        }

        public async Task<ChargingConfigResult> GetChargingConfig()
        {
            try
            {
                var ires = await _Interface.ReadData((byte)PiJuiceConfigCommands.CHARGING_CONFIG_CMD, 1);

                if (!ires.Success)
                    return new ChargingConfigResult();

                return new ChargingConfigResult(ires.Response) { Success = true };
            }
            catch (Exception ex)
            {
                return new ChargingConfigResult();
            }
        }

        public async Task<bool> SetChargingConfig(ChargingConfigResult config)
        {
            try
            {
                var ires = await _Interface.WriteData((byte)PiJuiceConfigCommands.CHARGING_CONFIG_CMD, config.ToArray());
                return ires.Success;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

    }
}
