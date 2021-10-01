using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TEC_PID_Control
{
  public static class DllInterface
  {
    [DllImport("TEC-PID-dll.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern double GetTemperature();
    [DllImport("TEC-PID-dll.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetTemperature(double temp);
    [DllImport("TEC-PID-dll.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern double GetSetPoint();
    [DllImport("TEC-PID-dll.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetSetPoint(double sp);
  }
}
