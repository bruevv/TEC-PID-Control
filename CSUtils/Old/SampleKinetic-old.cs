using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace Samples
{
  using CSUtils;
  using Data;

  public class SampleKinetic : Sample, ISKinetic
  {
    public const string Extension = "spk";
    protected override string FileExtension => Extension;

    public ValueArray Delay {
      get => (ValueArray)data[0];
      protected set => data[0] = value;
    }
    public ValueArray Kinetic {
      get => (ValueArray)data[1];
      protected set => data[1] = value;
    }

    ValueArray ISKinetic.X => Delay;
    ValueArray ISKinetic.Y => Kinetic;

    public override int Length => Delay?.Length ?? 0;

    public new RangeKinetic Range {
      get => (RangeKinetic)base.Range;
      set {
        var sp = (SKineticParams)SP;
        sp.Range = value;

        sp.Wavelength = Range.Wavelength;
        sp.Period = Range.Period;
        sp.LaserWidth = Range.LONTime;
        sp.Points = Range.Length;
        sp.NBins = Range.Length;
        sp.RelBinDelay = sp.Range.From;

        // Invalidate other parameters
        sp.BinLen = double.NaN;
        sp.Accumulations = 0;
        sp.HVLevel = double.NaN;
        sp.DiscLevel = double.NaN;
        sp.RelGateDelay = double.NaN;
        sp.GateWidth = double.NaN;
        sp.Gate_Enabled = false;
        sp.SyncIN_ON = false;
      }
    }

    public SampleKinetic(string filename) : base(filename, new SKineticParams()) { }
    public SampleKinetic(RangeKinetic Range, params SettingsBase[] sets)
      : base(new SKineticParams(sets)) => SetRange(Range);
    public void SetRange(RangeKinetic range)
    {
      var sp = (SKineticParams)SP;
      sp.Range = range;
      sp.Wavelength = range.Wavelength;
      sp.Period = range.Period;
      sp.LaserWidth = range.LONTime;
      sp.Points = range.Length;

      if(Delay == null) Delay = new ValueArray(nameof(Delay), "s", range.Length);
      if(Kinetic == null) Kinetic = new ValueArray(nameof(Kinetic), "", range.Length);
      else ClearData();

      for(int i = 0; i < range.Length; i++)
        Delay.Add(range.From + i * range.Step);
    }
    protected override void ReadFromStream(StreamReader sr)
    {
      var sp = SKineticParams.Read(sr);
      SP = sp;
      int points = sp.Points;
      data = new DataColumn[2];
      Delay = new ValueArray(nameof(Delay), "nm", points + 1);
      Kinetic = new ValueArray(nameof(Kinetic), "", points + 1);
      do {
        string[] strs = sr.ReadLine().Split('\t');
        double dn, dp;
        try {
          dn = double.Parse(strs[0]);
          dp = double.Parse(strs[1]);
        } catch(FormatException) { continue; }
        Delay.Add(dn);
        Kinetic.Add(dp);
      } while(!sr.EndOfStream);
    }
    public new static async Task<SampleKinetic> PreLoadSample(string fname)
    {
      SampleKinetic s;
      try {
        s = await Task.Factory.StartNew(() => new SampleKinetic(fname));
      } catch(Exception e) {
        Logger.Default?.Log(fname, e, Logger.Mode.Error, nameof(SampleKinetic));
        return null;
      }
      return s;
    }
    public static async Task<bool> EnumerateDir(string dir, EnumSampleDel esd)
    {
      bool succesful = false;
      string[] files = Directory.GetFiles(dir, $"*.{Extension}");
      for(int i = 0; i < files.Length; i++) {
        SampleKinetic s;
        try {
          s = await Task.Factory.StartNew(() => new SampleKinetic(files[i]));
        } catch(Exception e) {
          Logger.Default?.Log(files[i], e, Logger.Mode.Error, nameof(SampleKinetic));
          continue;
        }
        esd(s);
        succesful = true;
      }
      return succesful;
    }

    /// <summary>
    /// Properties saved to file (can be restored to current measure
    /// settings). Some of them may not exist in measure settings.
    /// </summary>
    class SKineticParams : SampleParams
    {
      public int NBins = -1;
      public double BinLen = double.NaN;
      public uint Accumulations = 0;
      public double Period = double.NaN;
      public double HVLevel = double.NaN;
      public double DiscLevel = double.NaN;
      public double RelBinDelay = double.NaN;
      public double RelGateDelay = double.NaN;
      public double GateWidth = double.NaN;
      public double LaserWidth = double.NaN;
      public bool Gate_Enabled = false;
      public bool SyncIN_ON = false;
      public double Wavelength = double.NaN;

      internal SKineticParams() : base() { }
      public SKineticParams(SampleParams sp) : base(sp) { }
      public SKineticParams(params SettingsBase[] sets)
          : base(sets) { }

      internal static SKineticParams Read(StreamReader sr)
      {
        SKineticParams sp = new SKineticParams();
        ReadFromStream(sr, sp);
        return sp;
      }
    }
  }
}
