using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Calibration;
using CSUtils;
using Devices;
using Devices.Keithley;
using TEC_PID_Control.Controls;

namespace TEC_PID_Control
{
  public class TempSensor : INotifyPropertyChanged
  {
    public UsrCntrlK2400 KC { get; private set; }
    ICalibration cal;

    public event PropertyChangedEventHandler PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string property = "") =>
     PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

    const string SetupCommands = "";

    double temperature;
    bool isInit = false;

    public double Temperature {
      get => Atomic.Read(ref temperature);
      protected set {
        if (temperature != value) {
          Atomic.Write(ref temperature, value);
          OnPropertyChanged();
        }
      }
    }
    public bool IsInit {
      get => isInit;
      private set {
        if (isInit != value) {
          isInit = value;
          OnPropertyChanged();
        }
      }
    }

    public TempSensor(UsrCntrlK2400 c)
    {
      KC = c;
      KC.KD.PropertyChanged += KD_PropertyChanged;
    }

    private void KD_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      Keithley2400 KD = (Keithley2400)sender;
      switch (e.PropertyName) {
        case nameof(KD.SourceMode) when KD.SourceMode != Mode.CURR:
          IsInit = false;
          break;
        case nameof(KD.Output) when KD.Output == false:
          // someone turned it off
          break;
      }
    }

    public void LoadCalibration(string filename = "Calibration.csv") => cal = new TableCalibration(filename);
    public void ScheduleInit()
    {
      if (!KC.KD.IsConnected)
        throw new DeviceDisconnectedException(
          "2400 SourceMeter should be connected.\n" +
          "Correct port should be selected.\n");

      if (!KC.KD.IsReset) {
        KC.KD.ScheduleReset();
        if (!string.IsNullOrEmpty(SetupCommands)) KC.KD.CustomCommand(SetupCommands);
      }
      KC.Dispatcher.Invoke(KC.SetUpICommand); // setup I source with parameters from control

      IsInit = true;
    }
    public async ValueTask<double> ReadTemperatureAsync()
    {
      if (!KC.IsConnected) KC.ConnectCommand();

      if (!KC.KD.IsReset) {
        KC.KD.ScheduleReset();
        if (string.IsNullOrEmpty(SetupCommands))
          KC.KD.CustomCommand(SetupCommands);
      }
      KC.SetUpICommand(); // setup I source with parameters from control
      double res = await KC.KD.MeasureRAsync();
      Temperature = cal.TransformBack(res);
      return Temperature;
    }
    public double ReadTemperature()
    {
      if (!IsInit) ScheduleInit();

      double res = KC.KD.MeasureR();
      Temperature = cal.TransformBack(res);
      return Temperature;
    }
    public double ConvertRtoT(double R) => Temperature = cal.TransformBack(R);
  }
}
