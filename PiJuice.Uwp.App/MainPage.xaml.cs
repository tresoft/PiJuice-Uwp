using PiJuice.Uwp.Core.Interface;
using PiJuice.Uwp.Core.Status;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainPage()
        {
            this.InitializeComponent();
            this.DataContext = this;

            _PiJuiceInterface = new PiJuiceInterface();
            _PiJuiceStatus = new PiJuiceStatus(_PiJuiceInterface);

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

                if (bsr.PowerInput5vIo != PowerInStates.NOT_PRESENT)
                    PowerInputGpioText = $"{PowerInputGpioVoltageText} | {PowerInputGpioCurrentText} | {PowerInputGpioStatusText}";
                else
                    PowerInputGpioText = bsr.PowerInput5vIo.ToString();

                if (bsr.PowerInput != PowerInStates.NOT_PRESENT)
                    PowerInputUsbText = $"{PowerInputUsbVoltage} | {PowerInputUsbCurrent} | {PowerInputUsbStatus}";
                else
                    PowerInputUsbText = bsr.PowerInput.ToString();

                // read fault status
                var fr = await _PiJuiceStatus.GetFaultStatus();
                if (fr.Success)
                {
                    FaultText = fr.ToString();
                }
                else
                    FaultText = "?";

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
    }
}
