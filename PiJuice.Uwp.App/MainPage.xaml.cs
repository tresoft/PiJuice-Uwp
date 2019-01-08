using PiJuice.Uwp.Core.Config;
using PiJuice.Uwp.Core.Interface;
using PiJuice.Uwp.Core.Power;
using PiJuice.Uwp.Core.Status;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.System;
using System.Diagnostics;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x407 dokumentiert.

namespace PiJuice.Uwp.App
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;


        private DispatcherTimer _Timer = null;
        private PiJuiceInterface _PiJuiceInterface = null;
        private PiJuiceStatus _PiJuiceStatus = null;
        private PiJuicePower _PiJuicePower = null;
        private PiJuiceConfig _PiJuiceConfig = null;

        private bool _FlagWakeUpOnCharge = false;

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainPage()
        {
            bool _FirstRoundPassed = false;

            this.InitializeComponent();
            this.DataContext = this;

            _PiJuiceInterface = new PiJuiceInterface();
            _PiJuiceStatus = new PiJuiceStatus(_PiJuiceInterface);
            _PiJuicePower = new PiJuicePower(_PiJuiceInterface);
            _PiJuiceConfig = new PiJuiceConfig(_PiJuiceInterface);

            _Timer = new DispatcherTimer();
            _Timer.Interval = TimeSpan.FromSeconds(1);
            _Timer.Tick += async (sender, e) =>
            {
                bool org = true;

                var BatteryChargeLevelText = "?";
                var BatteryVoltageText = "?";
                var BatteryCurrentText = "?";
                var BatteryTempText = "?";
                var BatteryStatusText = "?";

                var PowerInputGpioVoltageText = "?";
                var PowerInputGpioCurrentText = "?";
                var PowerInputGpioStatusText = "?";

                var PowerInputUsbVoltage = "?";
                var PowerInputUsbCurrent = "?";
                var PowerInputUsbStatus = "?";

                // read battery state
                var bsr = await _PiJuiceStatus.GetStatus();
                if (bsr.Success)
                {
                    ShutdownButton.IsEnabled = bsr.PowerInput5vIo == PowerInStates.NOT_PRESENT && bsr.PowerInput == PowerInStates.NOT_PRESENT;

                    BatteryStatusText = bsr.Battery.ToString(); 

                    PowerInputGpioStatusText = bsr.PowerInput5vIo.ToString();

                    PowerInputUsbStatus = bsr.PowerInput.ToString();

                    // read charge level
                    var bclr = await _PiJuiceStatus.GetChargeLevel();
                    if (bclr.Success)
                    {
                        BatteryChargeLevelText = $"{bclr.ChargeLevel}%";
                    }
                    // read battery voltage
                    var bvr = await _PiJuiceStatus.GetBatteryVoltage();
                    if (bvr.Success)
                    {
                        BatteryVoltageText = $"{bvr.Value.ToString("0.000")}V";
                    }
                    if (!org)
                    {
                        // read battery current
                        var bcr = await _PiJuiceStatus.GetBatteryCurrent();
                        if (bcr.Success)
                        {
                            BatteryCurrentText = $"{bcr.Value.ToString("0.000")}A";
                        }
                        // read battery temp
                        var btr = await _PiJuiceStatus.GetBatteryTemperatur();
                        if (btr.Success)
                        {
                            BatteryTempText = $"{btr.Value.ToString("0.000")}°C";
                        }
                    }
                    // read gpio voltage
                    var iovr = await _PiJuiceStatus.GetIoVoltage();
                    if (iovr.Success)
                    {
                        PowerInputGpioVoltageText = $"{iovr.Value.ToString("0.000")}V";
                    }
                    // read gpio current
                    var iocr = await _PiJuiceStatus.GetIoCurrent();
                    if (iocr.Success)
                    {
                        PowerInputGpioCurrentText = $"{iocr.Value.ToString("0.000")}A";
                    }
                }

                if (org)
                    BatteryText = $"{BatteryChargeLevelText} | {BatteryVoltageText} | {BatteryStatusText}";
                else
                    BatteryText = $"{BatteryChargeLevelText} | {BatteryVoltageText}  | {BatteryCurrentText} | {BatteryTempText} | {BatteryStatusText}";

                //if (bsr.PowerInput5vIo != PowerInStates.NOT_PRESENT)
                    PowerInputGpioText = $"{PowerInputGpioVoltageText} | {PowerInputGpioCurrentText} | {PowerInputGpioStatusText}";
                //else
                //    PowerInputGpioText = bsr.PowerInput5vIo.ToString();

                if (bsr.PowerInput != PowerInStates.NOT_PRESENT)
                    PowerInputUsbText = $"{PowerInputUsbVoltage} | {PowerInputUsbCurrent} | {PowerInputUsbStatus}";
                else
                    PowerInputUsbText = bsr.PowerInput.ToString();

                // read charging config
                var ccr = await _PiJuiceConfig.GetChargingConfig();
                if (ccr.Success)
                {
                    _ChargingEnabled = ccr.Enabled;
                    OnPropertyChanged("ChargingEnabled");
                }
                else
                {
                    ChargingText = "?";
                }

                //// read fault status
                //var fr = await _PiJuiceStatus.GetFaultStatus();
                //if (fr.Success)
                //{
                //    if (bsr.IsFault)
                //        FaultText = fr.ToString();
                //    else
                //        FaultText = "-";
                //}
                //else
                //    FaultText = "?";

                //// read LED-0 status
                //var led0 = await _PiJuiceStatus.GetLedState(0);
                //if (led0.Success)
                //    LedText0 = led0.ToString();
                //else
                //    LedText0 = "?";

                //// read LED-1 status
                //var led1 = await _PiJuiceStatus.GetLedState(1);
                //if (led1.Success)
                //    LedText1 = led1.ToString();
                //else
                //    LedText1 = "?";

                ////// set LED-1 status
                //await _PiJuiceStatus.SetLedState(1, (byte)(0), (byte)(0), (byte)(0));

                // config WakeUpOnCharge

                var wur = await _PiJuicePower.GetWakeUpOnCharge();
                if (wur.Success)
                    WakeUpOnChargeText = $"{wur.Value}%";
                else
                    WakeUpOnChargeText = "?";

                if (!_FlagWakeUpOnCharge && wur.Success)
                {
                    byte level = 0;

                    if (wur.Value != level)
                    {
                        // set to level
                        var swr = await _PiJuicePower.SetWakeUpOnCharge(level);
                        if (swr)
                            _FlagWakeUpOnCharge = true;
                    }
                }
                
                if (!_FirstRoundPassed)
                {
                    var fwr = await _PiJuiceConfig.GetFirmwareVersion();
                    if (fwr.Success)
                        FirmwareText = fwr.Version.ToString();
                    else
                        FirmwareText = "?";

                    _FirstRoundPassed = true;
                }

                //var wur = await _PiJuicePower.GetWakeUpOnCharge();
                //if (wur.Success)
                //    WakeUpOnChargeText = $"{wur.Value}%";
                //else
                //    WakeUpOnChargeText = "?";

            };

            this.Loaded += (sender, e) => { _Timer.Start(); };
            this.Unloaded += (sender, e) => { _Timer.Stop(); };
        }

        private string _BatteryText = "";

        public string BatteryText
        {
            get { return _BatteryText; }
            set
            {
                if (_BatteryText == value)
                    return;
                _BatteryText = value;
                OnPropertyChanged();
            }
        }

        private string _PowerInputGpio = "";

        public string PowerInputGpioText
        {
            get { return _PowerInputGpio; }
            set
            {
                if (_PowerInputGpio == value)
                    return;
                _PowerInputGpio = value;
                OnPropertyChanged();
            }
        }

        private string _PowerInputUsb = "";

        public string PowerInputUsbText
        {
            get { return _PowerInputUsb; }
            set
            {
                if (_PowerInputUsb == value)
                    return;
                _PowerInputUsb = value;
                OnPropertyChanged();
            }
        }

        private string _FaultText = "";

        public string FaultText
        {
            get { return _FaultText; }
            set
            {
                if (_FaultText == value)
                    return;
                _FaultText = value;
                OnPropertyChanged();
            }
        }

        private string _LedText0 = "";

        public string LedText0
        {
            get { return _LedText0; }
            set
            {
                if (_LedText0 == value)
                    return;
                _LedText0 = value;
                OnPropertyChanged();
            }
        }

        private string _LedText1 = "";

        public string LedText1
        {
            get { return _LedText1; }
            set
            {
                if (_LedText1 == value)
                    return;
                _LedText1 = value;
                OnPropertyChanged();
            }
        }

        private string _WakeUpOnChargeText = "";

        public string WakeUpOnChargeText
        {
            get { return _WakeUpOnChargeText; }
            set
            {
                if (_WakeUpOnChargeText == value)
                    return;
                _WakeUpOnChargeText = value;
                OnPropertyChanged();
            }
        }

        private string _FirmwareText = "";

        public string FirmwareText
        {
            get { return _FirmwareText; }
            set
            {
                if (_FirmwareText == value)
                    return;
                _FirmwareText = value;
                OnPropertyChanged();
            }
        }

        private string _ChargingText = "";

        public string ChargingText
        {
            get { return _ChargingText; }
            set
            {
                if (_ChargingText == value)
                    return;
                _ChargingText = value;
                OnPropertyChanged();
            }
        }

        private bool _ChargingEnabled = false;

        public bool ChargingEnabled
        {
            get { return _ChargingEnabled; }
            set
            {
                if (_ChargingEnabled == value)
                    return;

                //_ChargingEnabled = value;

                var t = _PiJuiceConfig.SetChargingConfig(new ChargingConfigResult() { Enabled = value, NonVolatile = true });
                t.Wait();
                if (t.Result)
                {
                    _ChargingEnabled = value;
                }

                OnPropertyChanged();
            }
        }
        
        private async void ShutdownButton_Click(object sender, RoutedEventArgs e)
        {

            _Timer.Stop();

            var res = await _PiJuicePower.SetWakeUpOnCharge(0);
            if (!res)
            {
                FaultText = "Error: SetWakeUpOnCharge(0)";
                return;
            }

            res = await _PiJuicePower.SetSystemPowerSwitch(0);
            if (!res)
            {
                FaultText = "Error: SetSystemPowerSwitch(0)";
                return;
            }

            res = await _PiJuicePower.SetPowerOff(30);
            if (!res)
            {
                FaultText = "Error: SetPowerOff(30)";
                return;
            }

            await Task.Run(() =>
            {
                try
                {
                    // shut down or restart
                    ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(0));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(false, ex.Message);
                    return;
                }

            });
        }

        private async void SystemPowerSwitchButton_Click(object sender, RoutedEventArgs e)
        {
            var res = await _PiJuicePower.GetSystemPowerSwitch();

        }

        private void ChargingConfigToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {

        }
    }
}
