using System;
using System.Runtime.InteropServices;

namespace TEC_PID_Control
{
  public interface TECPIDdll
  {
    static TECPIDdll DLL = Environment.Is64BitProcess ? new TECPIDdll64() : new TECPIDdll32();
    double GetTemperature();
    void SetTemperature(double temp);
    double GetSetPoint();
    double GetSetPointOncePerChange();
    void SetSetPoint(double sp);
    public void GetVIOnce(int channel, out double voltage, out double current);
    public void GetVI(int channel, out double voltage, out double current);
  }

  class TECPIDdll32 : TECPIDdll
  {
    public double GetTemperature() => DllInterface32.GetTemperature();
    public void SetTemperature(double temp) => DllInterface32.SetTemperature(temp);
    public double GetSetPoint() => DllInterface32.GetSetPoint();
    public double GetSetPointOncePerChange() => DllInterface32.GetSPOPC();
    public void SetSetPoint(double sp) => DllInterface32.SetSetPoint(sp);
    public void GetVIOnce(int channel, out double voltage, out double current)
    {
      if (channel > 2 || channel < 1) throw new ArgumentException("Cannel can only be 1 or 2");
      voltage = double.NaN;
      current = double.NaN;
      DllInterface32.GetVIOnce(channel, ref voltage, ref current);
    }
    public void GetVI(int channel, out double voltage, out double current)
    {
      if (channel > 2 || channel < 1) throw new ArgumentException("Cannel can only be 1 or 2");
      voltage = double.NaN;
      current = double.NaN;
      DllInterface32.GetVI(channel, ref voltage, ref current);
    }
  }
  class TECPIDdll64 : TECPIDdll
  {
    public double GetTemperature() => DllInterface64.GetTemperature();
    public void SetTemperature(double temp) => DllInterface64.SetTemperature(temp);
    public double GetSetPoint() => DllInterface64.GetSetPoint();
    public double GetSetPointOncePerChange() => DllInterface64.GetSPOPC();
    public void SetSetPoint(double sp) => DllInterface64.SetSetPoint(sp);
    public void GetVIOnce(int channel, out double voltage, out double current)
    {
      if (channel > 2 || channel < 1) throw new ArgumentException("Cannel can only be 1 or 2");
      voltage = double.NaN;
      current = double.NaN;
      DllInterface64.GetVIOnce(channel, ref voltage, ref current);
    }
    public void GetVI(int channel, out double voltage, out double current)
    {
      if (channel > 2 || channel < 1) throw new ArgumentException("Cannel can only be 1 or 2");
      voltage = double.NaN;
      current = double.NaN;
      DllInterface64.GetVI(channel, ref voltage, ref current);
    }
  }
  static class DllInterface32
  {
    [DllImport("TEC-PID-dll-32.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern double GetTemperature();
    [DllImport("TEC-PID-dll-32.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetTemperature(double temp);
    [DllImport("TEC-PID-dll-32.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern double GetSetPoint();
    [DllImport("TEC-PID-dll-32.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern double GetSPOPC();
    [DllImport("TEC-PID-dll-32.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetSetPoint(double sp);
    [DllImport("TEC-PID-dll-32.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void GetVIOnce(int channel, ref double voltage, ref double current);
    [DllImport("TEC-PID-dll-32.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void GetVI(int channel, ref double voltage, ref double current);
  }
  static class DllInterface64
  {
    [DllImport("TEC-PID-dll-64.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern double GetTemperature();
    [DllImport("TEC-PID-dll-64.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetTemperature(double temp);
    [DllImport("TEC-PID-dll-64.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern double GetSetPoint();
    [DllImport("TEC-PID-dll-64.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern double GetSPOPC();
    [DllImport("TEC-PID-dll-64.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetSetPoint(double sp);
    [DllImport("TEC-PID-dll-64.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void GetVIOnce(int channel, ref double voltage, ref double current);
    [DllImport("TEC-PID-dll-64.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void GetVI(int channel, ref double voltage, ref double current);
  }
}
