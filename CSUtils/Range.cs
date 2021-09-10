﻿using CSUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Markup;

namespace Samples
{
  using System.ComponentModel;
  using System.Linq;
  using Samples;
  using static GUtils;

  public class Range : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected const string Range_pattern =
      @"^(?<wf>-?\d+(?:\.\d+)?(?:[Ee][+-]?\d+)?):" +
      @"(?<ws>-?\d+(?:\.\d+)?(?:[Ee][+-]?\d+)?):" +
      @"(?<wt>-?\d+(?:\.\d+)?(?:[Ee][+-]?\d+)?)nm ";
    protected virtual void OnPropertyChanged(object s, PropertyChangedEventArgs e)
    {
      PropertyChanged?.Invoke(s, e);
    }
    protected void RaisePropertyChanged(string PropertyName = null)
    {
      OnPropertyChanged(this, new PropertyChangedEventArgs(PropertyName));
    }

    public virtual string Name => GetType().Name;

    public double From { get; protected set; }
    public double To { get; protected set; }
    public double Step { get; protected set; }
    public uint Averages { get; protected set; }
    public bool GateEnabled { get; protected set; }

    protected int completed;
    public int Completed {
      get => completed;
      set {
        int cpl = Bound(value, 0, 100);
        if(completed != cpl) {
          completed = cpl;
          RaisePropertyChanged(nameof(Completed));
        }
      }
    }
    bool active = false;
    public bool Active {
      get => active;
      set {
        if(active != value) {
          active = value;
          RaisePropertyChanged(nameof(Active));
        }
        if(value && Container != null) {
          try {
            foreach(Range r in Container)
              if(r != this) r.Active = false;
          } catch { }
        }
      }
    }
    public IEnumerable Container = null;

    public bool Measured => completed == 100;

    public double this[int i] => From + i * Step;
    public double this[double i] => From + i * Step;
    public virtual int Length => (int)(Math.Abs(To - From) / Step + 1.5);
    public double Index(double val) => (val - From) / Step;

    public Range(double from, double step, double to, bool gateenabled, bool measured = false)
    {
      if(DChk(from) || DChk(to) || DChk(step)) throw new ArgumentException("double is incorrect");
      From = from; To = to; Step = step;
      GateEnabled = gateenabled;
      Completed = measured ? 100 : 0;
    }
    public Range(double from, int nsteps, double to, bool gateenabled, bool measured = false)
    {
      if(DChk(from) || DChk(to)) throw new ArgumentException("double is incorrect");
      if(nsteps < 1) throw new ArgumentException("Should be at least 1 point");
      From = from; To = to;
      Step = (To - From) / nsteps;
      GateEnabled = gateenabled;
      Completed = measured ? 100 : 0;
    }
    public Range(double from, double step, int nsteps, bool gateenabled, bool measured = false)
    {
      if(DChk(from) || DChk(step)) throw new ArgumentException("double is incorrect");
      if(nsteps != -1 && nsteps < 1) throw new ArgumentException("Should be at least 1 point");
      From = from;
      To = nsteps == -1 ? from : from + step * nsteps;
      Step = step;
      GateEnabled = gateenabled;
      Completed = measured ? 100 : 0;
    }

    protected Range() => completed = 0;

    public override string ToString()
      => this == null ? "" : $"{From}:{Step}:{To}";
    public virtual string Designator
      => this == null ? "" : $"{From:N1}:{Step:N1}:{To:N1}";
    public virtual object Description => Description_str;
    string Description_str
        => this == null ? "" : $"From:{From:N1} To:{To:N1} Step:{Step:N1}";

    public static string RangesToString(IEnumerable ranges)
    {
      StringBuilder sb = new StringBuilder();
      foreach(Range r in ranges) {
        sb.Append(r.ToString());
        sb.Append("\n");
      }
      return sb.ToString();
    }
    static ConstructorInfo[] RangeCtors = null;
    public static Range[] RangesFromString(string str)
    {
      var rl = new List<Range>();
      string[] lines = str.Split('\n');
      foreach(string line in lines) {
        Range r = (Range)line;
        if(r != null) rl.Add(r);
      }
      return rl.ToArray();
    }

    public static explicit operator Range(string str)
    {
      if(!string.IsNullOrEmpty(str)) {
        if(RangeCtors == null) {
          Assembly rAss = Assembly.GetAssembly(typeof(Range));
          RangeCtors = (from Type t in rAss.GetTypes()
                        where t.BaseType == typeof(Range)
                        select t.GetConstructor(new Type[] { typeof(string) })).ToArray();
        }
        foreach(ConstructorInfo c in RangeCtors) {
          try {
            Range r = (Range)c.Invoke(new object[] { str });
            return r;
          } catch { }
        }
      }
      return null;
    }

    public static bool EqualAxis(Range r1, Range r2) =>
      r1.From == r2.From && r1.Step == r2.Step && r1.To == r2.To;
  }

  public class RangeSpectrum : Range
  {
    public override string Name => "Simple Spectrum";

    public RangeSpectrum(double from, double to, double step, uint averages, bool gateenabled, bool measured = false) :
      base(from, step, to, gateenabled, measured) => Averages = averages;
    public RangeSpectrum(double from, double to, int length, uint averages, bool gateenabled, bool measured = false) :
       base(from, length, to, gateenabled, measured) => Averages = averages;

    object ToolTip = null;
    static string xamlformat = null;

    public override object Description {
      get {
        if(ToolTip == null) {
          if(xamlformat == null) {
            using(StreamReader reader = new StreamReader(
              Assembly.GetExecutingAssembly().GetManifestResourceStream(
              $"CSUtils.Resources.{nameof(RangeSpectrum)}.xaml"))) {
              xamlformat = reader.ReadToEnd();
            }
          }
          string res = string.Format(xamlformat,
                                     Name,
                                     From,
                                     To,
                                     Step,
                                     Averages,
                                     GateEnabled ? "Enabled" : "Disabled");
          ToolTip = XamlReader.Parse(res);
        }
        return ToolTip;
      }
    }

    const string pattern = @"^(-?(?:\d*\.)?\d+):"
                         + @"(-?(?:\d*\.)?\d+):"
                         + @"(-?(?:\d*\.)?\d+)nm "
                         + @"A(?<A>\d+) "
                         + @"G(?<G>[0,1])\s?$";

    public override string Designator => $"{base.Designator}nm A{Averages} G{(GateEnabled ? 1 : 0)}";
    public override string ToString() => $"{base.ToString()}nm A{Averages} G{(GateEnabled ? 1 : 0)}";

    static Regex regex = null;
    /// <summary>
    /// "{Low}:{Step}:{High}nm
    /// </summary>
    /// <param name="str"></param>
    public RangeSpectrum(string str)
    {
      if(regex == null) regex = new Regex(pattern);
      Match m = regex.Match(str);
      if(!m.Success) throw new ArgumentException($"Incorrect {nameof(RangeSpectrum)} string format");

      From = double.Parse(m.Groups[1].Value);
      Step = double.Parse(m.Groups[2].Value);
      To = double.Parse(m.Groups[3].Value);
      Averages = uint.Parse(m.Groups["A"].Value);
      GateEnabled = m.Groups["G"].Value == "1";
      if(From == To) {
        Step = 0.0;
      } else {
        int points = (int)(Math.Abs((To - From) / Step) + 0.5);
        if(points < 1) points = 1;
        Step = Math.Abs(To - From) / points;
      }
      completed = 0;
    }
    protected RangeSpectrum() : base() { }

  }
  public class RangeKinetic : Range
  {
    public override string Name => "Simple Kinetic";
    public override int Length => NBins;

    public readonly int NBins;
    public readonly double Period;
    public readonly double LONTime;
    public readonly double Wavelength;


    public RangeKinetic(double from, double step, int nbins, uint averages, double period, double lontime, double wavelength, bool gateenabled, bool measured = false) :
      base(from, step, nbins, gateenabled, measured)
    {
      NBins = nbins;
      Period = period;
      LONTime = lontime;
      Wavelength = wavelength;
      Averages = averages;
    }

    object ToolTip = null;
    static string xamlformat = null;

    public override object Description {
      get {
        if(ToolTip == null) {
          if(xamlformat == null) {
            using(StreamReader reader = new StreamReader(
              Assembly.GetExecutingAssembly().GetManifestResourceStream(
              $"CSUtils.Resources.{nameof(RangeKinetic)}.xaml"))) {
              xamlformat = reader.ReadToEnd();
            }
          }
          string res = string.Format(xamlformat,
                                     Name,
                                     From * 1e6,
                                     NBins == -1 ? "Auto" : (To * 1e6).ToString("N1"),
                                     NBins == -1 ? "Auto" : NBins.ToString(),
                                     Period * 1e6,
                                     LONTime * 1e6,
                                     Averages,
                                     GateEnabled ? "Enabled" : "Disabled");
          ToolTip = XamlReader.Parse(res);
        }
        return ToolTip;
      }
    }

    const string pattern = @"^(?<df>-?\d+(?:\.\d+)?(?:[Ee][+-]?\d+)?):"
                           + @"(?<ds>-?\d+(?:\.\d+)?(?:[Ee][+-]?\d+)?)\("
                           + @"(?<dn>-?\d+)\)us "
                           + @"p(?<p>-?\d+(?:\.\d+)?(?:[Ee][+-]?\d+)?)us "
                           + @"L(?<L>-?\d+(?:\.\d+)?(?:[Ee][+-]?\d+)?)us "
                           + @"W(?<W>-?\d+(?:\.\d+)?(?:[Ee][+-]?\d+)?)nm "
                           + @"A(?<A>\d+) "
                           + @"G(?<G>[0,1])\s?$";

    public override string Designator =>
      $"{From * 1e6:N1}:{Step * 1e6:N1}({NBins})us " +
      $"p{Period * 1e6:N1}us L{LONTime * 1e6:N1}us " +
      $"W{Wavelength:N1}nm A{Averages} G{(GateEnabled ? 1 : 0)}";
    public override string ToString() =>
      $"{From * 1e6}:{Step * 1e6}({NBins})us " +
      $"p{Period * 1e6}us L{LONTime * 1e6}us " +
      $"W{Wavelength}nm A{Averages} G{(GateEnabled ? 1 : 0)}";

    static Regex regex = null;
    /// <summary>
    /// "{Low}:{Step}:{High}nm
    /// </summary>
    /// <param name="str"></param>
    public RangeKinetic(string str)
    {
      if(regex == null) regex = new Regex(pattern);
      Match m = regex.Match(str);
      if(!m.Success) throw new ArgumentException($"Incorrect {nameof(RangeKinetic)} string format");

      From = double.Parse(m.Groups["df"].Value) / 1e6;
      Step = double.Parse(m.Groups["ds"].Value) / 1e6;
      NBins = int.Parse(m.Groups["dn"].Value);
      Period = double.Parse(m.Groups["p"].Value) * 1e-6;
      LONTime = double.Parse(m.Groups["L"].Value) * 1e-6;
      Wavelength = double.Parse(m.Groups["W"].Value);
      Averages = uint.Parse(m.Groups["A"].Value);
      GateEnabled = m.Groups["G"].Value == "1";

      To = NBins == -1 ? From : From + NBins * Step;

      completed = 0;
    }
    protected RangeKinetic() : base() { }

    public static bool Equal(RangeKinetic r1, RangeKinetic r2) => EqualAxis(r1, r2) && r1.Period == r2.Period && r1.LONTime == r2.LONTime && r1.Wavelength == r2.Wavelength;
  }
  public class RangeGatedScaler : Range
  {
    public override string Name => "Phosphorescence Spectrum";

    public readonly double Period;
    public readonly double LONTime;

    public RangeGatedScaler(double from, double to, double step, double period, double lontime, uint averages, bool gateenabled, bool measured = false) :
      base(from, step, to, gateenabled, measured)
    {
      Period = period;
      LONTime = lontime;
      Averages = averages;
    }

    public override string Designator => $"{base.Designator}nm p{Period * 1e6:N1}us L{LONTime * 1e6:N1}us A{Averages} G{(GateEnabled ? 1 : 0)}";
    public override string ToString() => $"{base.ToString()}nm p{Period * 1e6}us L{LONTime * 1e6}us A{Averages} G{(GateEnabled ? 1 : 0)}";

    object ToolTip = null;
    static string xamlformat = null;

    public override object Description {
      get {
        if(ToolTip == null) {
          if(xamlformat == null) {
            using(StreamReader reader = new StreamReader(
              Assembly.GetExecutingAssembly().GetManifestResourceStream(
              $"CSUtils.Resources.{nameof(RangeGatedScaler)}.xaml"))) {
              xamlformat = reader.ReadToEnd();
            }
          }
          string res = string.Format(xamlformat,
                                     Name,
                                     From,
                                     To,
                                     Step,
                                     Period * 1e6,
                                     LONTime * 1e6,
                                     Averages,
                                     GateEnabled ? "Enabled" : "Disabled");
          ToolTip = XamlReader.Parse(res);
        }
        return ToolTip;
      }
    }

    const string pattern = Range_pattern
                         + @"p(?<p>-?\d+(?:\.\d+)?(?:[Ee][+-]?\d+)?)us "
                         + @"L(?<L>-?\d+(?:\.\d+)?(?:[Ee][+-]?\d+)?)us "
                         + @"A(?<A>\d+) "
                         + @"G(?<G>[0,1])\s?$";

    static Regex regex = null;

    public RangeGatedScaler(string str) : base()
    {
      if(regex == null) regex = new Regex(pattern);
      Match m = regex.Match(str);
      if(!m.Success) throw new ArgumentException($"Incorrect {nameof(RangeGatedScaler)} string format");

      From = double.Parse(m.Groups["wf"].Value);
      Step = double.Parse(m.Groups["ws"].Value);
      To = double.Parse(m.Groups["wt"].Value);
      Period = double.Parse(m.Groups["p"].Value) * 1e-6;
      LONTime = double.Parse(m.Groups["L"].Value) * 1e-6;
      Averages = uint.Parse(m.Groups["A"].Value);
      GateEnabled = m.Groups["G"].Value == "1";

      if(From == To) {
        Step = 0.0;
      } else {
        int points = (int)(Math.Abs((To - From) / Step) + 0.5);
        if(points < 1) points = 1;
        Step = Math.Abs(To - From) / points;
      }
    }
  }

}