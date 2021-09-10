using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace Samples
{
  using CSUtils;
  using Data;

  public class SampleSpectrum : Sample, ISSpectrum
  {
    public const string Extension = "sps";
    protected override string FileExtension => Extension;

    public ValueArray Wavelength {
      get => (ValueArray)data[0];
      protected set => data[0] = value;
    }
    public ValueArray Time {
      get => (ValueArray)data[1];
      private set => data[1] = value;
    }
    public ValueArray Spectrum {
      get => (ValueArray)data[2];
      protected set => data[2] = value;
    }

    ValueArray ISSpectrum.X => Wavelength;
    ValueArray ISSpectrum.Y => Spectrum;

    public override int Length => Wavelength?.Length ?? 0;

    public SampleSpectrum(string filename) : base(filename, new SSpectrumParams()) { }
    public SampleSpectrum(Range Range, DateTime? dt, params SettingsBase[] sets)
      : base(new SSpectrumParams(dt, sets))
    {
      SP.Range = Range;
      SP.Gate_Enabled = Range.GateEnabled;
      int len = Range.Length;

      data = new DataColumn[3];

      Wavelength = new ValueArray("Wavelength", "nm", len);
      Time = new ValueArray("Time", "s", len);
      Spectrum = new ValueArray("Photons", "1/s", len);
    }

    new SSpectrumParams SP => (SSpectrumParams)base.SP;
    protected override void ReadFromStream(StreamReader sr)
    {
      base.SP = SSpectrumParams.Read(sr);
      int points = SP.Points;
      data = new DataColumn[3];
      Wavelength = new ValueArray("Wavelength", "nm", points + 1);
      Time = new ValueArray("Time", "s", points + 1);
      Spectrum = new ValueArray("Photons", "", points + 1);
      do {
        string[] strs = sr.ReadLine().Split('\t');
        double dn, ds, dp;
        try {
          dn = double.Parse(strs[0]);
          ds = double.Parse(strs[1]);
          dp = double.Parse(strs[2]);
        } catch(FormatException) { continue; }
        Wavelength.Add(dn);
        Time.Add(ds);
        Spectrum.Add(dp);
      } while(!sr.EndOfStream);

      Spectrum.Normalize = SP.BinLen / (SP.Accumulations * SP.Range.Averages * SP.NBins);
    }
    public new static async Task<SampleSpectrum> PreLoadSample(string fname)
    {
      SampleSpectrum s;
      try {
        s = await Task.Factory.StartNew(() => new SampleSpectrum(fname));
      } catch(Exception e) {
        Logger.Log(fname, e, Logger.Mode.Error, nameof(SampleSpectrum));
        return null;
      }
      return s;
    }
    public static async Task<bool> EnumerateDir(string dir, EnumSampleDel esd)
    {
      bool succesful = false;
      string[] files = Directory.GetFiles(dir, $"*.{Extension}");
      for(int i = 0; i < files.Length; i++) {
        SampleSpectrum s = await PreLoadSample(files[i]);
        if(s != null) {
          esd(s);
          succesful = true;
        }
      }
      return succesful;
    }

    /// <summary>
    /// Properties saved to file (can be restored to current measure
    /// settings). Some of them may not exist in measure settings.
    /// </summary>
    class SSpectrumParams : SampleParams
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

      internal SSpectrumParams() : base() { }
      public SSpectrumParams(SampleParams sp) : base(sp) { }
      public SSpectrumParams(DateTime? dt, params SettingsBase[] sets)
          : base(dt, sets) { }

      internal static SSpectrumParams Read(StreamReader sr)
      {
        SSpectrumParams sp = new SSpectrumParams();
        ReadFromStream(sr, sp);
        return sp;
      }
    }
  }

}
