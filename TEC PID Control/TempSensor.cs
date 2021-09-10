using System;
using System.Threading.Tasks;
using Calibration;
using Devices.Keithley;

namespace TEC_PID_Control
{
  public class TempSensor
  {
    Keithley2400 keithley;
    ICalibration cal;

    const string SetupCommands = "";
    bool IsSetup = false;

    public TempSensor() => keithley = new Keithley2400();
    public TempSensor(Keithley2400 dev) => keithley = dev;

    public void Connect(string port) => keithley.Connect(port);
    public void LoadCalibration(string filename = "Calibration.csv") => cal = new TableCalibration(filename);

    public async Task<double> ReadTemperature(double curr = 1e-6, double VLim = 2.1)
    {
      if(!IsSetup) {
        keithley.Reset();
        if(string.IsNullOrEmpty(SetupCommands))
          keithley.CustomCommand(SetupCommands);
        IsSetup = true;      
      }
      double res = await keithley.MeasureR_AW(curr, VLim);
      return cal.TransformBack(res);
    }

  }
}
