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
 //           {"Ms", 1e6},
            {"min", 60},
            {"m", 60},
            {"ks", 1e3 },
            {"h", 60*60},
            {"d", 60*60*24},
            {"y", 60*60*24*365.2422},
        };

    public static readonly UnitTime Default = new UnitTime();

    public static double FromFrequency(double from) => 1 / from;

    public UnitTime() : base() { }
    public UnitTime(string def) : base(def) { }

    internal protected override string invariant => "s";
    internal protected override Dictionary<string, double> dic => s_dic;
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

    internal protected override string invariant => "Hz";
    internal protected override Dictionary<string, double> dic => s_dic;
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

    internal protected override string invariant => "nm";
    internal protected override Dictionary<string, double> dic => s_dic;
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

    internal protected override string invariant => "cm⁻¹";
    internal protected override Dictionary<string, double> dic => s_dic;
  }


  [Designators("Voltage", "V", "Volts")]
  public class UnitVoltage : Unit
  {
    static Dictionary<string, double> s_dic = new Dictionary<string, double>{
            {"MV", 1e6},
            {"kV", 1e3},
            {"V", 1},
            {"mV", 1e-3},
            {"µV", 1e-6},
            {"uV", 1e-6},
            {"nV", 1e-9},
    };

    public static readonly UnitVoltage Default = new UnitVoltage();

    public UnitVoltage() : base() { }
    public UnitVoltage(string def) : base(def) { }

    internal protected override string invariant => "V";
    internal protected override Dictionary<string, double> dic => s_dic;
  }

  [Designators("Current", "I", "Ampers")]
  public class UnitCurrent : Unit
  {
    static Dictionary<string, double> s_dic = new Dictionary<string, double>{
            {"kA", 1e3},
            {"A", 1},
            {"mA", 1e-3},
            {"µA", 1e-6},
            {"uA", 1e-6},
            {"nA", 1e-9},
            {"pA", 1e-12},
  };

    public static readonly UnitCurrent Default = new UnitCurrent();

    public UnitCurrent() : base() { }
    public UnitCurrent(string def) : base(def) { }

    internal protected override string invariant => "A";
    internal protected override Dictionary<string, double> dic => s_dic;
  }

  [Designators("Percent", "%")]
  public class UnitPercent : Unit
  {
    static Dictionary<string, double> s_dic = new Dictionary<string, double>{
           {" ", 1},
           {"%", 1e-2},
           {"‰", 1e-3},
           {"permil", 1e-3},
           {"permill", 1e-3},
           {"ppm", 1e-6},
        };

    public static readonly UnitPercent Default = new UnitPercent();

    public UnitPercent() : base() { }
    public UnitPercent(string def) : base(def) { }

    internal protected override string invariant => " ";
    internal protected override Dictionary<string, double> dic => s_dic;
  }

  [Designators("Resistance", "Ohm", "Ω")]
  public class UnitResistance : Unit
  {
    static Dictionary<string, double> s_dic = new Dictionary<string, double>{
           {"TΩ", 1e12},
           {"TOhm", 1e12},
           {"GΩ", 1e9},
           {"GOhm", 1e9},
           {"MΩ", 1e6},
           {"MOhm", 1e6},
           {"kΩ", 1e3},
           {"kOhm", 1e3},
           {"Ω", 1},
           {"Ohm", 1},
           {"mΩ", 1e-3},
           {"mOhm", 1e-3},
           {"µΩ", 1e-6},
           {"µOhm", 1e-6},
           {"uΩ", 1e-6},
           {"uOhm", 1e-6},
        };

    public static readonly UnitResistance Default = new UnitResistance();

    public UnitResistance() : base() { }
    public UnitResistance(string def) : base(def) { }

    internal protected override string invariant => "Ω";
    internal protected override Dictionary<string, double> dic => s_dic;
  }

  [Designators("Temperature(Celsius)", "TemperatureC")]
  [ConvertsFrom(typeof(UnitTemperatureK), nameof(FromK))]
  public class UnitTemperatureC : Unit
  {
    static Dictionary<string, double> s_dic = new Dictionary<string, double>{
           {"°C", 1},
           {"C", 1},
        };

    public static readonly UnitTemperatureC Default = new UnitTemperatureC();

    public UnitTemperatureC() : base() { }
    public UnitTemperatureC(string def) : base(def) { }
    public static double FromK(double from) => from + 273.15;

    internal protected override string invariant => "°C";
    internal protected override Dictionary<string, double> dic => s_dic;
  }
  [Designators("Temperature(Kelvins)", "TemperatureK")]
  [ConvertsFrom(typeof(UnitTemperatureC), nameof(FromC))]
  public class UnitTemperatureK : Unit
  {
    static Dictionary<string, double> s_dic = new Dictionary<string, double>{
           {"kK", 1e3},
           {"K", 1},
           {"mK", 1e-3},
      };

    public static readonly UnitTemperatureK Default = new UnitTemperatureK();

    public UnitTemperatureK() : base() { }
    public UnitTemperatureK(string def) : base(def) { }
    public static double FromC(double from) => from - 273.15;

    internal protected override string invariant => "K";
    internal protected override Dictionary<string, double> dic => s_dic;
  }
  [Designators("Relative Temperature", "RelTemperature")]
  public class UnitRelTemperature : Unit
  {
    static Dictionary<string, double> s_dic = new Dictionary<string, double>{
           {"°C", 1},
           {"C", 1},
           {"K", 1},
           {"F", 5.0/9.0 },
           {"mK", 1e-3},
           {"mC", 1e-3},
           {"mF", 5.0e-3/9.0 },
    };

    public static readonly UnitRelTemperature Default = new UnitRelTemperature();

    public UnitRelTemperature() : base() { }
    public UnitRelTemperature(string def) : base(def) { }

    internal protected override string invariant => "°C";
    internal protected override Dictionary<string, double> dic => s_dic;
  }

  [Designators("Power", "PWR", "Watts")]
  public class UnitPower : Unit
  {
    static Dictionary<string, double> s_dic = new Dictionary<string, double>{
           {"MW", 1e6},
           {"kW", 1e3},
           {"W", 1},
           {"Watt", 1},
           {"mW", 1e-3},
           {"µW", 1e-6},
           {"uW", 1e-6},
           {"nW", 1e-9},
      };

    public static readonly UnitPower Default = new UnitPower();

    public UnitPower() : base() { }
    public UnitPower(string def) : base(def) { }

    internal protected override string invariant => "W";
    internal protected override Dictionary<string, double> dic => s_dic;
  }

  [Designators("TemperatureCPerS")]
  public class UnitBandTempCperS : Unit
  {
    static Dictionary<string, double> s_dic = new Dictionary<string, double>{
           {"°C/s", 1.0},
           {"K/s", 1.0},
           {"C/s", 1.0},
           {"°C/m",1.0/60},
           {"K/m",1.0/60},
           {"C/m",1.0/60},
           {"°C/h",1.0/3600},
           {"K/h",1.0/3600},
           {"C/h",1.0/3600},
    };

    public static readonly UnitBandTempCperS Default = new UnitBandTempCperS();

    public UnitBandTempCperS() : base() { }
    public UnitBandTempCperS(string def) : base(def) { }

    internal protected override string invariant => "°C/s";
    internal protected override Dictionary<string, double> dic => s_dic;
  }
  [Designators("TemperatureConS")]
  public class UnitBandTempConS : Unit
  {
    static Dictionary<string, double> s_dic = new Dictionary<string, double>{
           {"°C·s", 1.0},
           {"°Cs", 1.0},
           {"°C*s", 1.0},
           {"Cs", 1.0},
           {"C*s", 1.0},
           {"°C·m",60.0},
           {"°Cm",60.0},
           {"°C*m", 60.0},
           {"Cm",60.0},
           {"C*m", 60.0},
           {"°C·h",3600.0},
           {"°Ch",3600.0},
           {"°C*h",3600.0},
           {"Ch",3600.0},
           {"C*h",3600.0},
    };

    public static readonly UnitBandTempConS Default = new UnitBandTempConS();

    public UnitBandTempConS() : base() { }
    public UnitBandTempConS(string def) : base(def) { }

    internal protected override string invariant => "°C·s";
    internal protected override Dictionary<string, double> dic => s_dic;
  }
}

