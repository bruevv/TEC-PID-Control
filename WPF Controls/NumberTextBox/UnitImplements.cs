using System.Collections.Generic;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace WPFControls
{
  [Designators("Time", "s")]
  [ConvertsFrom(typeof(UnitFrequency), nameof(FromFrequency))]
  public class UnitTime : Unit
  {
    static Dictionary<string, double> s_dic = new Dictionary<string, double>{
 //           {"as", 1e-18},
 //           {"fs", 1e-15},
 //           {"ps", 1e-12},
            {"ns", 1e-9},
            {"µs", 1e-6},
            {"us", 1e-6},
            {"ms", 1e-3},
            {"s", 1},
 //           {"ks", 1e3},
 //           {"Ms", 1e6},
            {"m", 60},
            {"h", 60*60},
            {"d", 60*60*24},
            {"y", 60*60*24*365.2422},
        };

    public static readonly UnitTime Default = new UnitTime();

    public static double FromFrequency(double from) => 1 / from;

    public UnitTime() : base() { }
    public UnitTime(string def) : base(def) { }

    protected override string Invariant => "s";
    protected override Dictionary<string, double> dic => s_dic;
  }

  [Designators("Frequency", "Hz")]
  [ConvertsFrom(typeof(UnitTime), nameof(FromTime))]
  public class UnitFrequency : Unit
  {
    static Dictionary<string, double> s_dic = new Dictionary<string, double>{
            {"THz", 1e12},
            {"GHz", 1e9},
            {"MHz", 1e6},
            {"kHz", 1e3},
            {"Hz", 1},
            {"mHz", 1e-3},
        };

    public static readonly UnitFrequency Default = new UnitFrequency();

    public static double FromTime(double from) => 1 / from;

    public UnitFrequency() : base() { }
    public UnitFrequency(string def) : base(def) { }

    protected override string Invariant => "Hz";
    protected override Dictionary<string, double> dic => s_dic;
  }
  [Designators("Wavelength", "nm")]
  [ConvertsFrom(typeof(UnitWavenumbers), nameof(FromWavenumbers))]
  public class UnitWavelength : Unit
  {
    static Dictionary<string, double> s_dic = new Dictionary<string, double>{
            {"Å", 1e-1},
            { "A", 1e-1},
            {"nm", 1},
            {"µm", 1e3},
            {"um", 1e3}
        };

    public static readonly UnitWavelength Default = new UnitWavelength();

    public static double FromWavenumbers(double from) => 1e7 / from;

    public UnitWavelength() : base() { }
    public UnitWavelength(string def) : base(def) { }

    protected override string Invariant => "nm";
    protected override Dictionary<string, double> dic => s_dic;
  }
  [Designators("Wavenumbers", "k", "Energy", "eV")]
  [ConvertsFrom(typeof(UnitWavelength), nameof(FromWavelength))]
  public class UnitWavenumbers : Unit
  {
    static Dictionary<string, double> s_dic = new Dictionary<string, double>{
            {"THz", 1/29.97925e-3},
            {"cm⁻¹", 1},
            {"cm-1", 1},
            {" 1/cm", 1},
            {"cm", 1},
            {"wn", 1},
            {"eV", 1/123.98421e-6},
        };

    public static readonly UnitWavenumbers Default = new UnitWavenumbers();

    public static double FromWavelength(double from) => 1e7 / from;

    public UnitWavenumbers() : base() { }
    public UnitWavenumbers(string def) : base(def) { }

    protected override string Invariant => "cm⁻¹";
    protected override Dictionary<string, double> dic => s_dic;
  }


  [Designators("Voltage", "V", "Volts")]
  [ConvertsFrom(typeof(UnitVoltage), nameof(FromWavelength))]
  public class UnitVoltage : Unit
  {
    static Dictionary<string, double> s_dic = new Dictionary<string, double>{
            {"kV", 1e3},
            {"V", 1},
            {"mV", 1e-3},
            {"µV", 1e-6},
            {"uV", 1e-6},
        };

    public static readonly UnitWavenumbers Default = new UnitWavenumbers();

    public static double FromWavelength(double from) => 1e7 / from;

    public UnitVoltage() : base() { }
    public UnitVoltage(string def) : base(def) { }

    protected override string Invariant => "V";
    protected override Dictionary<string, double> dic => s_dic;
  }

  [Designators("Percent", "%")]
  public class UnitPercent : Unit
  {
    static Dictionary<string, double> s_dic = new Dictionary<string, double>{
           {" ", 1},
           {"%", 1e-2},
           {"‰", 1e-3},
           {"ppm", 1e-6},
        };

    public static readonly UnitPercent Default = new UnitPercent();

    public UnitPercent() : base() { }
    public UnitPercent(string def) : base(def) { }

    protected override string Invariant => " ";
    protected override Dictionary<string, double> dic => s_dic;
  }
}