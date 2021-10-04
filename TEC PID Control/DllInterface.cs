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
  }

  class TECPIDdll32 : TECPIDdll
  {
    public double GetTemperature() => DllInterface32.GetTemperature();
    public void SetTemperature(double temp) => DllInterface32.SetTemperature(temp);
    public double GetSetPoint() => DllInterface32.GetSetPoint();
    public double GetSetPointOncePerChange() => DllInterface32.GetSPOPC();
    public void SetSetPoint(double sp) => DllInterface32.SetSetPoint(sp);
  }
  class TECPIDdll64 : TECPIDdll
  {
    public double GetTemperature() => DllInterface64.GetTemperature();
    public void SetTemperature(double temp) => DllInterface64.SetTemperature(temp);
    public double GetSetPoint() => DllInterface64.GetSetPoint();
    public double GetSetPointOncePerChange() => DllInterface64.GetSPOPC();
    public void SetSetPoint(double sp) => DllInterface64.SetSetPoint(sp);
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
  }
}
