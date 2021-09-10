using System;
using System.Configuration;
using System.IO;

namespace Samples
{
  using System.Threading.Tasks;
  using CSUtils;
  using Data;
  using static Math;
  public class SampleScaler : Sample, ISSpectrum, ISKinetic, ISSpecRange, ISKineticRange
  {
    public const string Extension = "spm"; // to have static value
    protected override string FileExtension => Extension;

    public ValueArray Wavelength {
      get => (ValueArray)data[0];
      protected set => data[0] = value;
    }
    public ValueArray Time {
      get => (ValueArray)data[1];
      private set => data[1] = value;
    }
    public ValueMatrix Photons {
      get => (ValueMatrix)data[2];
      private set => data[2] = value;
    }
    public ValueArray Delay { get; private set; }
    ValueArray ISSpectrum.X => Wavelength;
    ValueArray ISSpectrum.Y => Photons.AccColumn;
    ValueArray ISKinetic.X => Delay;
    ValueArray ISKinetic.Y => Photons.AccRow;

    //event Action<IList<double>, IList<double>> SpectrumChanged;
    //event Action<IList<double>, IList<double>> ISSpectrum.Changed {
    //  add => SpectrumChanged += value;
    //  remove => SpectrumChanged -= value;
    //}
    //public void RefreshSpectrum()
    //{
    //  if(SpectrumChanged != null && Length > 0) {
    //    ISSpectrum ifc = this;
    //    SpectrumChanged(ifc.X.AsIList, ifc.Y.AsIList);
    //  }
    //}
    //event Action<IList<double>, IList<double>> KineticChanged;
    //event Action<IList<double>, IList<double>> ISKinetic.Changed {
    //  add => KineticChanged += value;
    //  remove => KineticChanged -= value;
    //}
    //public void RefreshKinetic()
    //{
    //  if(KineticChanged != null && Length > 0) {
    //    ISKinetic ifc = this;
    //    KineticChanged(ifc.X.AsIList, ifc.Y.AsIList);
    //  }
    //}

    public void SetSpecralRegion(double X1, double X2)
    {
      double min = Min(X1, X2);
      double max = Max(X1, X2);

      int imin = (int)Round(Wavelength.GetIndexAprox(min));
      int imax = (int)Round(Wavelength.GetIndexAprox(max));

      Photons.FirstRowI = imin <= 0 ? (int?)null : imin;
      Photons.LastRowI = imax >= Wavelength.Length - 1 ? (int?)null : imax;
    }
    double ISSpecRange.X1 => Photons.FirstRowI == null ? double.NaN : Wavelength[(int)Photons.FirstRowI];
    double ISSpecRange.X2 => Photons.LastRowI == null ? double.NaN : Wavelength[(int)Photons.LastRowI];
    void ISSpecRange.SetRange(double x1, double x2)
    {
      double ia = Wavelength.GetIndexAprox(x1);
      Photons.FirstRowI = double.IsNaN(ia) ? (int?)null : (int)(ia + 0.5);
      ia = Wavelength.GetIndexAprox(x2);
      Photons.LastRowI = double.IsNaN(ia) ? (int?)null : (int)(ia + 0.5);
      Photons.RecalculateAccRow();
      ChangeProperty("NeedUpdate");
    }
    double ISKineticRange.X1 => Photons.FirstColI == null ? double.NaN : Delay[(int)Photons.FirstColI];
    double ISKineticRange.X2 => Photons.LastColI == null ? double.NaN : Delay[(int)Photons.LastColI];
    void ISKineticRange.SetRange(double x1, double x2)
    {
      double ia = Delay.GetIndexAprox(x1);
      Photons.FirstColI = double.IsNaN(ia) ? (int?)null : (int)(ia + 0.5);
      ia = Delay.GetIndexAprox(x2);
      Photons.LastColI = double.IsNaN(ia) ? (int?)null : (int)(ia + 0.5);
      if(Photons.LastColI >= Photons.NumColumns) {

      }
      Photons.RecalculateAccCol();
      ChangeProperty("NeedUpdate");
    }

    public override int Length => Wavelength?.Length ?? 0;

    public SampleScaler(string filename) : base(filename, new SScalerParams())
    {
      data = new DataColumn[3];
    }
    public SampleScaler(RangeGatedScaler Range, DateTime? dt = null, params SettingsBase[] sets)
      : base(new SScalerParams(dt, sets))
    {
      SP.Range = Range;
      int len = Range.Length;

      data = new DataColumn[3];

      Wavelength = new ValueArray("Wavelength", "nm", len);
      Time = new ValueArray("Time", "s", len);
      Photons = new ValueMatrix("Photons", null, len);
      Delay = new ValueArray("Delay", "s");
      Photons.XAxis = Delay;
      Photons.YAxis = Wavelength;

      SScalerParams sp = (SScalerParams)SP;

      sp.Period = Range.Period;
      sp.LaserWidth = Range.LONTime;
      sp.Gate_Enabled = Range.GateEnabled;
    }

    protected override void ReadFromStream(StreamReader sr)
    {
      this.SP = SScalerParams.Read(sr);// to skip sr pointer through header lines

      int points = SP.Points;
      int kpoints = SP.NBins;

      Delay = new ValueArray("Delay", "s", kpoints + 1);

      string[] delays = sr.ReadLine().Split('\t');
      for(int i = 2; i < delays.Length; i++) {
        double del = double.NaN;
        try {
          del = double.Parse(delays[i]);
        } catch(FormatException) { }
        if(delays[0] == "Delay")
          Delay.Add(del);
        else
          Delay.Add((i - 32) * 1e-6);
      }
      kpoints = Delay.Length;

      Wavelength = new ValueArray("Wavelength", "nm", points);
      Time = new ValueArray("Time", "s", points);
      Photons = new ValueMatrix("Photons", null, points);

      do {
        string[] strs = sr.ReadLine().Split('\t');
        double dn, ds;
        double[] dp = new double[kpoints];
        try {
          dn = double.Parse(strs[0]);
          ds = double.Parse(strs[1]);
          for(int i = 0; (i < strs.Length - 2) && (i < kpoints); i++)
            dp[i] = double.Parse(strs[i + 2]);
        } catch(FormatException) { continue; }
        Wavelength.Add(dn);
        Time.Add(ds);
        Photons.AddRow(dp);
      } while(!sr.EndOfStream);

      Photons.FullColumn.Normalize = Photons.AccColumn.Normalize =
        SP.BinLen / (SP.Accumulations * (SP.Range?.Averages ?? 1) * Photons.NumRows);

      Photons.FullRow.Normalize = Photons.AccRow.Normalize =
        SP.BinLen / (SP.Accumulations * (SP.Range?.Averages ?? 1) * Photons.NumColumns);
    }
    protected override void WriteFileHeader(StreamWriter sw)
    {
      base.WriteFileHeader(sw);
      sw.Write($"{Delay.Name}\t{Delay.Units}\t");
      for(int i = 0; i < Delay.Length; i++) {
        sw.Write(Delay[i]);
        if(i != Delay.Length - 1) sw.Write('\t');
      }
      sw.WriteLine();
    }

    public new static async Task<SampleScaler> PreLoadSample(string fname)
    {
      SampleScaler s;
      try {
        s = await Task.Factory.StartNew(() => new SampleScaler(fname));
      } catch(Exception e) {
        Logger.Log(fname, e, Logger.Mode.Error, nameof(SampleScaler));
        return null;
      }
      return s;
    }
    public static async Task<bool> EnumerateDir(string dir, EnumSampleDel esd)
    {
      bool succesful = false;

      string[] files = Directory.GetFiles(dir, $"*.{Extension}");
      for(int i = 0; i < files.Length; i++) {
        SampleScaler s;
        try {
          s = await Task.Factory.StartNew(() => new SampleScaler(files[i]));
        } catch(Exception e) {
          Logger.Log(files[i], e, Logger.Mode.Error, nameof(SampleSpectrum));
          continue;
        }
        esd(s);
        succesful = true;
      }
      return succesful;
    }

    new SScalerParams SP {
      get => (SScalerParams)base.SP;
      set => base.SP = value;
    }
    public SampleKinetic AccKinetic()
    {
      ISSpecRange issr = this;
      ISKinetic isk = this;
      ISSpectrum iss = this;

      double min = double.IsNaN(issr.X1) ? iss.X[0] : issr.X1;
      double max = double.IsNaN(issr.X2) ? iss.X[iss.X.Length - 1] : issr.X2;

      RangeKinetic rk = new RangeKinetic(SP.RelBinDelay,
                                         SP.BinLen,
                                         SP.NBins,
                                         SP.Range?.Averages ?? 1,
                                         SP.Period,
                                         SP.LaserWidth,
                                         (min + max) / 2,
                                         true);
      SampleKinetic sk = new SampleKinetic(rk, MeasureDateTime);
      SampleParams.CopyParamsByName(this, sk);

      for(int i = 0; i < sk.Length; i++)
        sk.Kinetic.Add(isk.Y[i]);

      if(!string.IsNullOrEmpty(SP.SampleName))
        sk.Name = $"{Name} ({min:0.#}-{max:0.#}nm)";
      sk.Key = $"{MeasureDateTimeString} ({min:0.#}-{max:0.#}nm)";

      return sk;
    }
    public SampleSpectrum AccSpectrum()
    {
      ISKineticRange iskr = this;
      ISKinetic isk = this;
      ISSpectrum iss = this;

      double min = double.IsNaN(iskr.X1) ? isk.X[0] : iskr.X1;
      double max = double.IsNaN(iskr.X2) ? isk.X[isk.X.Length - 1] : iskr.X2;

      RangeSpectrum rk = new RangeSpectrum(Range.From,
                                           Range.To,
                                           iss.Y.Length - 1,
                                           Range?.Averages ?? 1,
                                           true);
      SampleSpectrum ss = new SampleSpectrum(rk, SP.MeasureDateTime);
      SampleParams.CopyParamsByName(this, ss);

      for(int i = 0; i < iss.Y.Length; i++) {
        ss.Wavelength.Add(iss.X[i]);
        ss.Time.Add(Time[i]);
        ss.Spectrum.Add(iss.Y[i]);
      }

      min *= 1e6;
      max *= 1e6;

      if(!string.IsNullOrEmpty(SP.SampleName))
        ss.Name = $"{Name} ({min:0.##} {max:0.##}us)";
      ss.Key = $"{MeasureDateTimeString} ({min:0.##} {max:0.##}us)";

      return ss;
    }
    public void AddKinetic(double[] kinetic)
    {
      Photons.AddRow(kinetic);

      if(Length == 1) {
        Photons.FullRow.Normalize = Photons.AccRow.Normalize =
          SP.BinLen / (SP.Accumulations * SP.Range.Averages * Photons.NumColumns);
      }

      Photons.FullColumn.Normalize = Photons.AccColumn.Normalize =
        SP.BinLen / (SP.Accumulations * SP.Range.Averages * Photons.NumRows);
    }

    class SScalerParams : SampleParams
    {
      public int NBins = 50;// TODO Change to -1 (error)
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

      internal SScalerParams() : base() { }
      public SScalerParams(SampleParams sp) : base(sp) { }
      public SScalerParams(DateTime? dt, params SettingsBase[] sets) : base(dt, sets) { }

      internal static SScalerParams Read(StreamReader sr)
      {
        SScalerParams sp = new SScalerParams();
        ReadFromStream(sr, sp);
        return sp;
      }
    }
  }

}
