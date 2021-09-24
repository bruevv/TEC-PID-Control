using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Threading;

namespace CSUtils
{
  using static Math;

  public class AbortException : Exception
  {
    public AbortException() : base() { }
    public AbortException(string message) : base(message) { }
    public AbortException(string m, Exception ie) : base(m, ie) { }
  }

  public static class GUtils
  {
    public static T Bound<T>(T val, T min, T max) where T : IComparable<T>
    {
      if (val.CompareTo(min) < 0)
        return min;
      if (val.CompareTo(max) > 0)
        return max;
      return val;
    }
    public static T Bound<T>(ref T val, T min, T max) where T : IComparable<T>
    {
      if (val.CompareTo(min) < 0)
        return val = min;
      if (val.CompareTo(max) > 0)
        return val = max;
      return val;
    }

    public static bool DChk(double d) => double.IsNaN(d) || double.IsInfinity(d);

    public static string CleanFileName(string str)
    {
      str = str.Replace(':', '.');
      char[] ipc = System.IO.Path.GetInvalidPathChars();
      foreach (char c in ipc) {
        str = str.Replace(c, '_');
      }
      return str;
    }

    public static void CleanFileName(ref string str)
    {
      str = str.Replace(':', '.');
      char[] ipc = System.IO.Path.GetInvalidPathChars();
      foreach (char c in ipc) {
        str = str.Replace(c, '_');
      }
    }

    public static string Escape(string str) { return str.Replace(Environment.NewLine, "\\n"); }
    public static string Unescape(string str) { return str.Replace("\\n", Environment.NewLine); }

    public static string GenerateUniqueName(Predicate<string> isunique, string original = "sample")
    {
      if (original == null) original = "sample";

      string name = original;
      string leftname = null;
      int i = 0;
      while (!isunique(name)) {
        if (leftname == null) {
          leftname = Regex.Match(name, @"(.+)(\s*\d*)$", RegexOptions.RightToLeft).Groups[1]?.Value ?? "series";
          if (leftname == "") leftname = "X";
        }
        name = leftname + (i == 0 ? "" : " " + i);
        i++;
      }
      return name;
    }

    /// <summary>
    /// method of trapezoids
    /// </summary>
    public static double Integral_Tr(double dx, double[] Y)
    {
      double I = 0;
      int N = Y.Length;
      I += Y[0];
      I += Y[N - 1];
      I /= 2;
      for (int i = 1; i < N - 1; i++)
        I += Y[i];
      return I * dx;
    }
    /// <summary>
    /// method of trapezoids for sin/cos Integral
    /// </summary>
    public static Complex Integral_Tr(double dx, double[] Y, double[] SIN, double[] COS)
    {
      double SinI = 0;
      double CosI = 0;
      int N = Y.Length;
      int N2 = SIN.Length;
      SinI += Y[0] * SIN[0];
      SinI += Y[N - 1] * SIN[(N - 1) % N2];
      CosI += Y[0] * COS[0];
      CosI += Y[N - 1] * COS[(N - 1) % N2];
      SinI /= 2; CosI /= 2;
      for (int i = 1; i < N - 1; i++) {
        SinI += Y[i] * SIN[i % N2];
        CosI += Y[i] * COS[i % N2];
      }
      return new Complex(CosI / N * dx, -SinI / N * dx);
    }

    public static double Average(params double[] DATA)
    {
      double res = 0.0;
      for (int i = 0; i < DATA.Length; i++)
        res += DATA[i];
      return res / DATA.Length;
    }
  }

  public static class Atomic
  {
    public static double Read(ref double s) => Interlocked.CompareExchange(ref s, 0.0, 0.0);
    public static void Write(ref double d, double val) => Interlocked.Exchange(ref d, val);

    public static int Read(ref int s) => Interlocked.CompareExchange(ref s, 0, 0);
    public static void Write(ref int d, int val) => Interlocked.Exchange(ref d, val);
  }

  public class MovingAverage
  {
    double[] values;
    double[] squares;
    double sum;
    double sumsquares;
    uint length;

    uint cur_index = uint.MaxValue;
    uint fill_len = 0;

    public uint Length {
      get => length;
      set {
        if (value == 0) throw new ArgumentException("Cannot be zero");
        if (length != value) {
          length = value;
          values = new double[length];
          squares = new double[length];
          Initialize();
        }
      }
    }

    public double Average => sum / fill_len;
    public double SD => Math.Sqrt((sumsquares - sum * sum / fill_len) / fill_len);
    public double ERROR =>
       Math.Sqrt((sumsquares - sum * sum / fill_len) / (fill_len * (fill_len - 1)));

    /// <summary>
    /// Binomial statistics
    /// 0.1%:3.2905, 1%:2.5758, 10%:1.6449
    /// </summary>
    public double ConfidenceError => SD / Math.Sqrt(Average);

    public double Next(double val)
    {
      if (++cur_index >= length) cur_index = 0;
      double square = val * val;

      sum += val;
      sumsquares += square;
      sum -= values[cur_index];
      sumsquares -= squares[cur_index];
      values[cur_index] = val;
      squares[cur_index] = square;

      if (++fill_len > length) fill_len = length;

      return Average;
    }

    public void Initialize()
    {
      for (int i = 0; i < values.Length; i++) {
        values[i] = 0;
        squares[i] = 0;
      }
      cur_index = uint.MaxValue;
      fill_len = 0;
      sum = 0;
      sumsquares = 0;
    }

    public MovingAverage(uint length = 1)
    {
      this.length = length;
      values = new double[length];
      squares = new double[length];
      Initialize();
    }

  }

  public static class ByteCastExtension
  {
    public static byte LoByte(this ushort us) => (byte)us;
    public static byte HiByte(this ushort us) => (byte)(us >> 8);

    public static void LoByte(ref this ushort us, byte l) => us = (ushort)((us & 0xff00) | l);
    public static void HiByte(ref this ushort us, byte h) => us = (ushort)((us & 0x00ff) | (h << 8));
    /// <summary> LSB - 3 </summary>
    public static byte Byte(this int us, byte num)
    {
      switch (num) {
        case 0:
          return (byte)(us >> 24);
        case 1:
          return (byte)(us >> 16);
        case 2:
          return (byte)(us >> 8);
        case 3:
          return (byte)us;
        default:
          throw new ArgumentException("Should be 0-3", nameof(num));
      }
    }

    /// <summary> LSB - 3 </summary>
    public static byte Byte(this uint us, byte num)
    {
      switch (num) {
        case 0:
          return (byte)(us >> 24);
        case 1:
          return (byte)(us >> 16);
        case 2:
          return (byte)(us >> 8);
        case 3:
          return (byte)us;
        default:
          throw new ArgumentException("Should be 0-3", nameof(num));
      }
    }

    public static ushort LoWORD(this int dword) => (ushort)dword;
    public static ushort LoWORD(this uint dword) => (ushort)dword;
    public static ushort HiWORD(this int dword) => (ushort)(dword >> 16);
    public static ushort HiWORD(this uint dword) => (ushort)(dword >> 16);
  }
  public static class DoubleExtension
  {
    public static double? NaNToNull(this double d) => double.IsNaN(d) ? (double?)null : d;
    public static double NullToNaN(this double? d) => d.HasValue ? (double)d : double.NaN;

    public static bool EqualsRelPrecision(this double d1, double d2, double r_eps)
    {
      double eps = Max(Max(d1, d2) * r_eps, double.Epsilon);
      return Abs(d2 - d1) <= eps;
    }
  }

  public static class Invoke
  {
    public static void InContextInvoke<TEventArgs>(object caller, EventHandler<TEventArgs> EH, TEventArgs EA)
    {
      if (EH == null) return;

      foreach (EventHandler<TEventArgs> del in EH.GetInvocationList()) {
        if (del.Target is DispatcherObject DO && Dispatcher.FromThread(Thread.CurrentThread) != DO.Dispatcher) {
          DO.Dispatcher.BeginInvoke(del, caller, EA);
        } else {
          del(caller, EA);
        }
      }
    }
    public static void InContextInvoke(object caller, EventHandler EH, EventArgs EA)
    {
      if (EH == null) return;

      foreach (EventHandler del in EH.GetInvocationList()) {
        if (del.Target is DispatcherObject DO && Dispatcher.FromThread(Thread.CurrentThread) != DO.Dispatcher) {
          DO.Dispatcher.BeginInvoke(EH, caller, EA);
        } else {
          del(caller, EA);
        }
      }
    }

    public static void InContextInvoke(Action del)
    {
      if (del == null) return;

      if (del.Target is DispatcherObject DO && Dispatcher.FromThread(Thread.CurrentThread) != DO.Dispatcher) {
        DO.Dispatcher.BeginInvoke(del);
      } else {
        del();
      }
    }
    public static void InContextInvoke<T1>(Action<T1> del, T1 t1)
    {
      if (del == null) return;

      if (del.Target is DispatcherObject DO && Dispatcher.FromThread(Thread.CurrentThread) != DO.Dispatcher) {
        DO.Dispatcher.BeginInvoke(del, t1);
      } else {
        del(t1);
      }
    }
    public static void InContextInvoke<T1, T2>(Action<T1, T2> del, T1 t1, T2 t2)
    {
      if (del == null) return;

      if (del.Target is DispatcherObject DO && Dispatcher.FromThread(Thread.CurrentThread) != DO.Dispatcher) {
        DO.Dispatcher.BeginInvoke(del, t1, t2);
      } else {
        del(t1, t2);
      }
    }
    public static void InContextInvoke<T1, T2, T3>(Action<T1, T2, T3> del, T1 t1, T2 t2, T3 t3)
    {
      if (del == null) return;

      if (del.Target is DispatcherObject DO && Dispatcher.FromThread(Thread.CurrentThread) != DO.Dispatcher) {
        DO.Dispatcher.BeginInvoke(del, t1, t2, t3);
      } else {
        del(t1, t2, t3);
      }
    }
    public static void InContextInvokeD(Delegate del, params object[] args)
    {
      if (del == null) return;

      if (del.Target is DispatcherObject DO && Dispatcher.FromThread(Thread.CurrentThread) != DO.Dispatcher) {
        DO.Dispatcher.BeginInvoke(del, args);
      } else {
        del.DynamicInvoke(args);
      }
    }
  }

  #region old crap
  public struct Complex
  {
    public double real;
    public double imaginary;

    public Complex(double real, double imaginary)  //constructor
    {
      this.real = real;
      this.imaginary = imaginary;
    }

    public double Abs { get { return Math.Sqrt(real * real + imaginary * imaginary); } }

    public static Complex operator +(Complex c1, Complex c2) { return new Complex(c1.real + c2.real, c1.imaginary + c2.imaginary); }
    public static Complex operator +(double d, Complex c) { return new Complex(d + c.real, c.imaginary); }
    public static Complex operator +(Complex c, double d) { return new Complex(d + c.real, c.imaginary); }
    public static Complex operator -(Complex c1, Complex c2) { return new Complex(c1.real - c2.real, c1.imaginary - c2.imaginary); }
    public static Complex operator -(double d, Complex c) { return new Complex(d - c.real, -c.imaginary); }
    public static Complex operator -(Complex c, double d) { return new Complex(c.real - d, c.imaginary); }
    public static Complex operator -(Complex c) { return new Complex(-c.real, -c.imaginary); }
    public static Complex operator *(Complex c1, Complex c2) { return new Complex(c1.real * c2.real - c1.imaginary * c2.imaginary, c1.real * c2.imaginary + c1.imaginary * c2.real); }
    public static Complex operator *(double d, Complex c) { return new Complex(d * c.real, d * c.imaginary); }
    public static Complex operator *(Complex c, double d) { return new Complex(d * c.real, d * c.imaginary); }
    public static Complex operator /(Complex c1, Complex c2)
    {
      double div = c2.real * c2.real + c2.imaginary * c2.imaginary;
      return new Complex((c1.real * c2.real + c1.imaginary * c2.imaginary) / div, (c1.imaginary * c2.real - c1.real * c2.imaginary) / div);
    }
    public static Complex operator /(double d, Complex c)
    {
      double div = c.real * c.real + c.imaginary * c.imaginary;
      return new Complex(d * c.real / div, -d * c.imaginary / div);
    }
    public static Complex operator /(Complex c, double d) { return new Complex(c.real / d, c.imaginary / d); }

    public static implicit operator Complex(double d) { return new Complex(d, 0); }

    public override string ToString()
    {
      if (real == 0 && imaginary == 0)
        return "0";
      if (imaginary == 0) return real.ToString();
      if (imaginary > 0) {
        if (real == 0) return $"i{imaginary}";
        else return $"{real} + i{imaginary}";
      } else {
        if (real == 0) return $"-i{-imaginary}";
        else return $"{real} - i{-imaginary}";
      }
    }
  }
  #endregion

  // public delegate void CallbackInt(int i);
  //public delegate void CallbackVoid();
}
