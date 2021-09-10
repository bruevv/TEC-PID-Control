using Ranges;
using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace CSUtils.Samples
{
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Runtime.CompilerServices;
  using System.Windows;
  using static Math;

  /// <summary>
  /// Base class for Sample that can be saved in Origin compatible ASCII file
  /// </summary>
  public abstract class Sample
  {
    public object Tag { get; set; } = null;

    protected string fileName = "";
    public string FileName => fileName;
    public string DefaultFileName => $"{Key}.{FileExtension}";
    protected bool IsLoaded;
    public abstract string FileExtension { get; }

    public string Name {
      get => string.IsNullOrEmpty(SP.SampleName) ? "Photons" : SP.SampleName;
      set => SP.SampleName = value;
    }
    public string Comments {
      get => GUtils.Unescape(SP.Comments);
      set => SP.Comments = GUtils.Escape(value);
    }

    public string Key => SP.MeasureDateTime.ToString(@"yyyy.MM.dd HH.mm.ss");
    public string NameDateComments {
      get {
        StringBuilder sb = new StringBuilder(100);
        sb.AppendLine((!string.IsNullOrEmpty(SP.SampleName)) ? "Name not set" : SP.SampleName);
        sb.Append("Measured:" + SP.MeasureDateTime);
        if(!string.IsNullOrEmpty(SP.Comments)) {
          sb.AppendLine();
          sb.AppendLine("Comments:");
          sb.Append(Comments);
        }
        return sb.ToString();
      }
    }

    public DateTime MeasureDateTime => SP.MeasureDateTime;
    public bool Rejected { get => SP.Rejected; set => SP.Rejected = value; }

    protected SampleParams SP;
    public Range Range => SP.Range;

    abstract public int Length { get; }

    protected Sample(SampleParams sp) { SP = sp; IsLoaded = true; }
    protected Sample(string filename)
    {
      IsLoaded = false;
      fileName = filename;

      using(StreamReader fs = new StreamReader(fileName)) {
        SP = new SampleParams();
        SampleParams.ReadFromStream(fs, SP);
        // TODO chang sampleparams to proper type
      }
    }
    protected Sample()
    {
      IsLoaded = true;
      SP = new SampleParams();
    }

    protected DataColumn[] data = null;
    public int SampleRank => data?.Length ?? 0;
    public int SampleLength => SampleRank == 0 ? 0 : data[0].Length;

    protected string FileHeader {
      get {
        StringBuilder sb = new StringBuilder();
        for(int i = 0; i < SampleRank; i++) {
          sb.Append(data[i].Name);
          if(i != SampleRank - 1)
            sb.Append('\t');
        }
        sb.AppendLine();
        for(int i = 0; i < SampleRank; i++) {
          sb.Append(data[i].Units);
        }
        return sb.ToString();
      }
    }

    protected virtual void SaveToStream(Stream s)
    {
      StreamWriter sw = new StreamWriter(s);
      SP.WriteToStream(sw);
      sw.WriteLine(FileHeader);
      for(int i = 0; i < Length; i++) {
        for(int j = 0; j < SampleRank; j++) {
          sw.Write(data[j].GetDataAsString(i));
          if(j != SampleRank - 1)
            sw.Write('\t');
        }
        if(i != Length - 1)
          sw.WriteLine();
      }
      sw.Flush();
    }
    protected virtual void ReadFromStream(Stream s)
    {
      //TODO implement somehow
    }

    public void SaveToFile(string path = "", string fn = "")
    {
      if(Length == 0) throw new Exception("Sample is empty");

      if(fn == "") {
        if(fileName == "")
          fileName = DefaultFileName;
      } else {
        fileName = fn;
      }

      SP.Points = Length;

      using(FileStream f = File.Create(Path.Combine(path, fileName), 4096, FileOptions.WriteThrough)) {
        SaveToStream(f);
        //       f.Flush();
      }
    }
    public string FileAsString {
      get {
        using(MemoryStream ms = new MemoryStream()) {
          SaveToStream(ms);
          ms.Position = 0;
          StreamReader sr = new StreamReader(ms);
          return sr.ReadToEnd();
        }
      }
    }
    public void LoadFromFile()
    {
      if(IsLoaded) return;
      using(FileStream fs = new FileStream(fileName, FileMode.Open)) {
        ReadFromStream(fs);
      }
      IsLoaded = true;
    }

    protected static int IFromG(Group g) => int.Parse(g.Value);

    public delegate void EnumSampleDel(Sample s);

    public override string ToString()
    {
      if(SP.SampleName == "")
        return SP.MeasureDateTime.ToLongTimeString();
      else
        return SP.SampleName;
    }
    public string ToFileName() => GUtils.CleanFileName(ToString());
    /// <summary>
    /// Properties saved to file (can be restored to current measure
    /// settings). Some of them may not exist in measure settings.
    /// </summary>
    protected class SampleParams
    {
      public string SampleName = "";
      public string Comments = "";
      public DateTime MeasureDateTime = default;
      public bool Rejected = false;
      public Range Range = null;
      public int Points;
      Nullable<int> a;
      internal SampleParams() { }
      protected SampleParams(SampleParams sp)
      {
        FieldInfo[] pia = this.GetType().GetFields();
        foreach(FieldInfo pi in pia)
          pi.SetValue(this, pi.GetValue(sp));
      }
      protected SampleParams(params SettingsBase[] sets)
      {
        if((sets?.Length ?? 0) > 0) {
          FieldInfo[] pia = this.GetType().GetFields();
          foreach(FieldInfo pi in pia) {
            foreach(SettingsBase set in sets) {
              try {
                pi.SetValue(this, set[pi.Name]);
              } catch(SettingsPropertyNotFoundException) {
                // Do not set (no such property)
              }
            }
          }
        }
        MeasureDateTime = DateTime.Now;
      }

      public void RestoreSettings(params SettingsBase[] sets)
      {
        FieldInfo[] pia = typeof(SampleParams).GetFields();
        foreach(FieldInfo pi in pia) {
          foreach(SettingsBase set in sets) {
            try {
              set[pi.Name] = pi.GetValue(this);
            } catch(System.Configuration.SettingsPropertyNotFoundException) {
              // Do not set (no such property)
            }
          }
        }
      }
      internal void WriteToStream(StreamWriter sw)
      {
        FieldInfo[] pia = this.GetType().GetFields();
        foreach(FieldInfo pi in pia) {
          object val = pi.GetValue(this);
          if(val != null)
            sw.WriteLine($"{pi.Name}={val}");
        }
      }
      internal static void ReadFromStream(StreamReader sr, SampleParams sp)
      {
        FieldInfo[] pia = sp.GetType().GetFields();
        string line;
        while((line = sr.ReadLine()).Contains("=")) {
          int colonindex = line.IndexOf('=');
          string name = line.Substring(0, colonindex);
          string val = line.Substring(colonindex + 1);
          foreach(FieldInfo fi in pia) {
            if(fi.Name.Equals(name)) {
              fi.SetValue(sp, Convert.ChangeType(val, fi.FieldType));
              break;
            }
          }
        }
      }
    }
  }

  public class SampleSpectrum : Sample
  {
    const string extension = "sps";
    public override string FileExtension => extension;

    //ValueArray nanometers = null;
    //ValueArray seconds = null;
    //ValueArray photons = null;

    public ValueArray Nanometers {
      get => (ValueArray)data[0];
      private set => data[0] = value;
    }
    public ValueArray Seconds {
      get => (ValueArray)data[1];
      private set => data[1] = value;
    }
    public ValueArray Photons {
      get => (ValueArray)data[2];
      private set => data[2] = value;
    }

    public override int Length => Nanometers?.Length ?? 0;

    SampleSpectrum(ValueArray Nanometers, ValueArray Seconds,
      ValueArray Photons, SampleSpectParams sp)
        : base(sp)
    {
      if(Nanometers.Length != Seconds.Length || Nanometers.Length != Photons.Length)
        throw new ArgumentException("Arrays should correspond to each other");

      data = new DataColumn[3];

      this.Nanometers = (ValueArray)Nanometers.Clone();
      this.Seconds = (ValueArray)Seconds.Clone();
      this.Photons = (ValueArray)Photons.Clone();

      SP.Points = Length;
    }
    public SampleSpectrum(ValueArray Nanometers, ValueArray Seconds,
      ValueArray Photons, params SettingsBase[] sets)
        : this(Nanometers, Seconds, Photons, new SampleSpectParams(sets)) { }
    public SampleSpectrum(string filename) : base(filename) { }
    public SampleSpectrum(Range Range, params SettingsBase[] sets)
      : base(new SampleSpectParams(sets))
    {
      SP.Range = Range;
      int len = Range.Length;

      data = new DataColumn[3];

      Nanometers = new ValueArray("Wavelength", "nm", len);
      Seconds = new ValueArray("Time", "s", len);
      Photons = new ValueArray("Photons", "1/s", len);
    }

    protected override void ReadFromStream(Stream s)
    {
      StreamReader sr = new StreamReader(s);
      SP = SampleSpectParams.Read(sr);
      int points = ((SampleSpectParams)SP).Points;
      Nanometers = new ValueArray("Wavelength", "nm", points + 1);
      Seconds = new ValueArray("Time", "s", points + 1);
      Photons = new ValueArray("Photons", "", points + 1);
      do {
        string[] strs = sr.ReadLine().Split('\t');
        double dn, ds, dp;
        try {
          dn = double.Parse(strs[0]);
          ds = double.Parse(strs[1]);
          dp = double.Parse(strs[2]);
        } catch(FormatException) { continue; }
        Nanometers.Add(dn);
        Seconds.Add(ds);
        Photons.Add(dp);
      } while(!sr.EndOfStream);
    }

    public static void EnumerateDir(string dir, EnumSampleDel esd)
    {
      string[] files = Directory.GetFiles(dir, $"*.{extension}");
      for(int i = 0; i < files.Length; i++) {
        SampleSpectrum s = new SampleSpectrum(files[i]);
        esd(s);
      }
    }

    /// <summary>
    /// Properties saved to file (can be restored to current measure
    /// settings). Some of them may not exist in measure settings.
    /// </summary>
    class SampleSpectParams : SampleParams
    {
      public double Period = 0;
      public double LONTime = 0;
      public double Delay = 0;
      public double SyncWidth = 0;
      public int SBMode = 0;

      SampleSpectParams() : base() { }
      public SampleSpectParams(SampleParams sp) : base(sp) { }
      public SampleSpectParams(params SettingsBase[] sets)
          : base(sets) { }

      internal static SampleSpectParams Read(StreamReader sr)
      {
        SampleSpectParams sp = new SampleSpectParams();
        ReadFromStream(sr, sp);
        return sp;
      }
    }
  }

  public class SampleScaler : Sample
  {
    const string extension = "sps";
    public override string FileExtension => extension;

    //ValueArray nanometers = null;
    //ValueArray seconds = null;
    //ValueArray photons = null;

    public ValueArray Nanometers {
      get => (ValueArray)data[0];
      private set => data[0] = value;
    }
    public ValueArray MeasureTime {
      get => (ValueArray)data[1];
      private set => data[1] = value;
    }
    public ValueArray KineticAxis {
      get => (ValueArray)data[2];
      private set => data[2] = value;
    }
    public ValueMatrix Photons {
      get => (ValueMatrix)data[3];
      private set => data[3] = value;
    }

    public override int Length => Nanometers?.Length ?? 0;

    SampleScaler(ValueArray MeasureTime, ValueMatrix Photons, SampleScalerParams sp,
      ValueArray Nanometers = null, ValueArray KineticAxis = null) : base(sp)
    {
      Nanometers = Nanometers ?? Photons.YAxis;
      KineticAxis = KineticAxis ?? Photons.XAxis;
      if(Nanometers.Length != MeasureTime.Length || Nanometers.Length != Photons.Length ||
        Photons.NumColumns != KineticAxis.Length)
        throw new ArgumentException("Arrays should correspond to each other");

      data = new DataColumn[4];

      this.Nanometers = (ValueArray)Nanometers.Clone();
      this.MeasureTime = (ValueArray)MeasureTime.Clone();
      this.KineticAxis = (ValueArray)KineticAxis.Clone();
      this.Photons = (ValueMatrix)Photons.Clone();
      this.Photons.XAxis = this.KineticAxis;
      this.Photons.YAxis = this.Nanometers;

      SP.Points = Length;
    }
    public SampleScaler(ValueArray MeasureTime, ValueMatrix Photons,
      ValueArray Nanometers = null, ValueArray KineticAxis = null,
      params SettingsBase[] sets)
        : this(MeasureTime, Photons, new SampleScalerParams(sets), Nanometers, KineticAxis) { }
    public SampleScaler(string filename) : base(filename) { }
    public SampleScaler(RangeGatedScaler Range, params SettingsBase[] sets)
      : base(new SampleScalerParams(sets))
    {
      SP.Range = Range;
      int len = Range.Length;

      data = new DataColumn[4];

      Nanometers = new ValueArray("Wavelength", "nm", len);
      MeasureTime = new ValueArray("Time", "s", len);
      //      KineticAxis = new ValueArray("Delay", "s", Range///);
      Photons = new ValueMatrix("Photons", null, len);
      this.Photons.XAxis = this.KineticAxis;
      this.Photons.YAxis = this.Nanometers;
    }
    protected override void ReadFromStream(Stream s) => throw new NotImplementedException();

    public static void EnumerateDir(string dir, EnumSampleDel esd)
    {
      string[] files = Directory.GetFiles(dir, $"*.{extension}");
      for(int i = 0; i < files.Length; i++) {
        SampleScaler s = new SampleScaler(files[i]);
        esd(s);
      }
    }

    /// <summary>
    /// Properties saved to file (can be restored to current measure
    /// settings). Some of them may not exist in measure settings.
    /// </summary>
    class SampleScalerParams : SampleParams
    {
      /// TODO populate!
      public double Period = 0;
      public double LONTime = 0;
      public double Delay = 0;
      public double SyncWidth = 0;
      public int SBMode = 0;

      SampleScalerParams() : base() { }
      public SampleScalerParams(SampleParams sp) : base(sp) { }
      public SampleScalerParams(params SettingsBase[] sets) : base(sets) { }

      internal static SampleScalerParams Read(StreamReader sr)
      {
        SampleScalerParams sp = new SampleScalerParams();
        ReadFromStream(sr, sp);
        return sp;
      }
    }
  }

  /// <summary> Base class for Sample Data </summary>
  public abstract class DataColumn : INotifyPropertyChanged, ICloneable
  {
    public readonly string Name;
    public readonly string Units;

    HashSet<string> SuspendedNotifications = new HashSet<string>();
    bool suspendNotify = false;
    public bool SuspendNotify {
      get => suspendNotify;
      set {
        suspendNotify = value;
        if(!value) {
          foreach(string n in SuspendedNotifications) RaisePropertyChanged(n);
          SuspendedNotifications.Clear();
        }
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(PropertyChangedEventArgs ea) => PropertyChanged?.Invoke(this, ea);

    protected void RaisePropertyChanged([CallerMemberName] string property = null)
    {
      if(SuspendNotify) {
        SuspendedNotifications.Add(property);
      } else {
        OnPropertyChanged(new PropertyChangedEventArgs(property));
      }
    }

    public int length;
    public int Length {
      get => length;
      protected set {
        if(length != value) {
          length = value;
          RaisePropertyChanged();
        }
      }
    }

    public abstract string GetDataAsString(int i);
    object ICloneable.Clone() => Clone();
    public abstract DataColumn Clone();


    protected DataColumn(string name, string units)
    {
      Name = name;
      Units = units;
    }
  }

  /// <summary> Variable size double array. Like List(double) </summary>
  public class ValueArray : DataColumn
  {
    double[] values;

    public double FirstValue { get => values[0]; set => values[0] = value; }
    public double LastValue { get => values[Length - 1]; set => values[Length - 1] = value; }

    public int Capacity => values.Length;
    const int MaxCapacity = 0x1000000;

    /// <summary>
    /// Copy producing Array with minimum needed Capacity
    /// </summary>
    override public DataColumn Clone() => Clone(0);
    /// <summary>
    /// Copy producing Array with specific Capacity
    /// </summary>
    /// <param name="cap">0 - minimum needed Capacity</param>
    public DataColumn Clone(int cap)
    {
      ValueArray nva = new ValueArray(Name, Units, cap > Length ? cap : Length);
      nva.Length = Length;
      Array.Copy(values, nva.values, Length);
      return nva;
    }
    public void Clone(ValueArray toFill)
    {
      toFill.Length = this.Length;
      Array.Copy(this.values, toFill.values, this.Length);
    }
    public static ValueArray operator +(ValueArray a, ValueArray b)
    {
      ValueArray nva = (ValueArray)a.Clone(a.Length + b.Length);
      nva.Add(double.NaN);
      for(int i = 0; i < b.Length; i++)
        nva.Add(b[i]);
      return nva;
    }
    public void Add(double v)
    {
      if(Length >= MaxCapacity)
        throw new IndexOutOfRangeException();
      if(Length >= Capacity) {
        double[] nv = new double[Min(Capacity * 2, MaxCapacity)];
        values.CopyTo(nv, 0);
        values = nv;
      }
      values[Length] = v;
      Length++;
    }
    public void Clear() => Length = 0;

    public override string GetDataAsString(int i) => this[i].ToString();
    public double this[int i] {
      get {
        if(i < 0 || i >= Length)
          throw new IndexOutOfRangeException();
        return values[i];
      }
      set {
        if(i < 0 || i >= Length)
          throw new IndexOutOfRangeException();
        if(values[i] != value) {
          values[i] = value;
          RaisePropertyChanged("Item[]");
        }
      }
    }
    /// <summary> Linear interpolation based on floating point quasi-index </summary>
    /// <param name="i">(double) quasi-index</param>
    public double this[double i] {
      get {
        if(Length == 0) throw new ApplicationException();
        if(Length == 1) return values[0];

        if(i < 0.0) {
          return values[0] + (values[1] - values[0]) * i;
        } else if(i > Length - 1.0) {
          return values[Length - 1] + (values[Length - 1] - values[Length - 2]) * (i - (Length - 1));
        } else {
          return values[(int)i] + (i - (int)i) * (values[(int)i + 1] - values[(int)i]);
        }
      }
    }
    public ValueArray(string name = null, string units = null, int capacity = 1024) : base(name, units)
    {
      if(MaxCapacity < capacity)
        throw new ArgumentOutOfRangeException();
      Length = 0;
      values = new double[capacity];
    }

    public double GetIndexAprox(double pos)
    {
      if(Length < 2)
        throw new ApplicationException();
      double err, lasterr;
      err = lasterr = pos - values[0];
      if(lasterr == 0.0) return 0.0;
      int i;
      for(i = 1; i < Length; i++) {
        if(double.IsNaN(values[i])) {
          i++;
          err = lasterr = pos - values[i];
          if(err == 0.0) return (double)i;
          continue;
        }
        err = pos - values[i];
        if(err == lasterr) continue;
        if(err == 0.0) return (double)i;
        if(Sign(err) == Sign(lasterr)) {
          //					if(Math.Abs(err) > Math.Abs(lasterr))
          //						return ((double)i) - Math.Abs(err) / (Math.Abs(err - lasterr));
          lasterr = err;
        } else {
          return ((double)(i - 1)) + Abs(lasterr) / (Abs(err - lasterr));
        }
      }
      lasterr = pos - values[i - 2];
      return ((double)(i - 1)) + Abs(err) / (Abs(err - lasterr));
    }

    public static implicit operator double[] (ValueArray VA)
    {
      double[] da = new double[VA.Length];
      Array.Copy(VA.values, da, VA.Length);
      return da;
    }
    public static implicit operator ValueArray(double[] da)
    {
      ValueArray va = new ValueArray(capacity: da.Length);
      da.CopyTo(va.values, 0);
      va.length = da.Length;
      return va;
    }
  }

  public class ValueMatrix : DataColumn
  {
    double[][] rows;

    public static string DataDelimeter = "\t";

    public int? NumColumns => FirstRow?.Length;
    public bool AutoCalc { get; set; } = true;

    public ValueArray XAxis { get; set; } = null;
    public ValueArray YAxis { get; set; } = null;

    public double GetDataAtXY(double X, double Y)
    {
      int x = (int)((XAxis?.GetIndexAprox(X) ?? X) + 0.5);
      int y = (int)((YAxis?.GetIndexAprox(Y) ?? Y) + 0.5);

      if(x < 0 || x >= NumColumns || y < 0 || y >= Length) return 0;

      return rows[y][x];
    }

    public double IntegrateData(Rect r) => throw new NotImplementedException();

    int firstCol = int.MinValue;
    int lastCol = int.MaxValue;
    int firstRow = int.MinValue;
    int lastRow = int.MaxValue;

    public int? FirstColI {
      get => firstCol == int.MinValue ? (int?)null : firstCol;
      set {
        if(value < 0) throw new ArgumentException();
        if(FirstColI != value) {
          firstCol = value ?? int.MinValue;
          RecalculateX();
        }
      }
    }
    public int? LastColI {
      get => lastCol == int.MaxValue ? (int?)null : lastCol;
      set {
        if(value < 0) throw new ArgumentException();
        if(LastColI != value) {
          lastCol = value ?? int.MaxValue;
          RecalculateX();
        }
      }
    }
    public int? FirstRowI {
      get => firstRow == int.MinValue ? (int?)null : firstRow;
      set {
        if(value < 0) throw new ArgumentException();
        if(FirstRowI != value) {
          firstRow = value ?? int.MinValue;
          RecalculateX();
        }
      }
    }
    public int? LastRowI {
      get => lastRow == int.MaxValue ? (int?)null : lastRow;
      set {
        if(value < 0) throw new ArgumentException();
        if(LastRowI != value) {
          lastRow = value ?? int.MaxValue;
          RecalculateX();
        }
      }
    }

    public ValueArray AccRow { get; protected set; }
    public ValueArray AccColumn { get; protected set; }
    public ValueArray FullRow { get; protected set; }
    public ValueArray FullColumn { get; protected set; }

    public void RecalculateX()
    {
      //TODO
      throw new NotImplementedException();
    }

    public ValueArray FirstRow { get => rows?[0]; set => rows[0] = value; }
    public ValueArray LastRow {
      get => rows[Length - 1];
      set => rows[Length - 1] = value;
    }

    int RowCapacity => rows?.Length ?? 0;
    const int MaxRowCapacity = 0x10000;

    /*  /// <summary>
      /// Copy producing Array with minimum needed Capacity
      /// </summary>
      object ICloneable.Clone() => Clone(0);
      /// <summary>
      /// Copy producing Array with specific Capacity
      /// </summary>
      /// <param name="cap">0 - minimum needed Capacity</param>
      public ValueMatrix Clone(int cap = 0)
      {
        ValueMatrix nva = new ValueMatrix(Name, Units, cap > NumRows ? cap : NumRows);
        nva.NumRows = NumRows;
        Array.Copy(rows, nva.rows, NumRows);
        return nva;
      }
      public void Clone(ValueMatrix toFill)
      {
        toFill.NumRows = this.NumRows;
        Array.Copy(this.rows, toFill.rows, this.NumRows);
      }*/

    //public static ValueMatrix operator +(ValueMatrix a, ValueMatrix b)
    //{
    //  ValueMatrix nva = a.Clone();
    //  nva.Add(double.NaN);
    //  for(int i = 0; i < b.NumRows; i++)
    //    nva.Add(b[i]);
    //  return nva;
    //}
    public static ValueMatrix operator +(ValueMatrix a, double[] v) => a.AddRow(v);
    public ValueMatrix AddRow(double[] v)
    {
      if(Length >= MaxRowCapacity)
        throw new IndexOutOfRangeException();
      if(Length >= RowCapacity) {
        double[][] nv = new double[Min(RowCapacity * 2, MaxRowCapacity)][];
        rows.CopyTo(nv, 0);
        rows = nv;
      }
      if(NumColumns != null && NumColumns != v.Length)
        throw new IndexOutOfRangeException("Array Sizes do not Match");

      rows[Length] = v;
      // FirstRowAdded
      if(Length == 0) {
        AccRow = new ValueArray("Accamulated Row", Units, v.Length);
        AccColumn = new ValueArray("Accamulated Column", Units, DefaultCapacity);
        FullRow = new ValueArray("Full Row", Units, v.Length);
        FullColumn = new ValueArray("Full Column", Units, DefaultCapacity);
        for(int i = 0; i < v.Length; i++) {
          AccRow.Add(0);
          FullRow.Add(0);
        }
      }
      if(AutoCalc) {
        FullRow.SuspendNotify = true;
        AccRow.SuspendNotify = true;
        double fullc = 0, accc = 0;
        for(int i = 0; i < v.Length; i++) {
          FullRow[i] += v[i];
          fullc += v[i];
          if(firstCol <= i && i <= lastCol)
            accc += v[i];
          if(firstRow <= Length && Length <= lastRow)
            AccRow[i] += v[i];
        }
        FullColumn.Add(fullc);
        AccColumn.Add(accc);
        FullRow.SuspendNotify = false;
        AccRow.SuspendNotify = false;
      }
      Length++;
      return this;
    }

    public void Clear() => Length = 0;
    StringBuilder GetDataAsString_sb = null;

    public override string GetDataAsString(int row)
    {
      if(NumColumns == 0) return "";

      if(GetDataAsString_sb == null) GetDataAsString_sb = new StringBuilder();
      else GetDataAsString_sb.Clear();

      GetDataAsString_sb.Append(rows[row][0].ToString());

      for(int i = 1; i < NumColumns; i++) {
        GetDataAsString_sb.Append(DataDelimeter);
        GetDataAsString_sb.Append(rows[row][i].ToString());
      }

      return GetDataAsString_sb.ToString();
    }

    /// <summary> Copy producing Array with minimum needed Capacity </summary>
    override public DataColumn Clone() => Clone(null);
    public ValueMatrix Clone(int? capacity)
    {
      int cap = Max(capacity ?? 0, Length);
      ValueMatrix nva = new ValueMatrix(Name, Units, cap) {
        XAxis = XAxis, YAxis = YAxis
      };
      if(NumColumns is int numcollumns) {
        nva.Length = Length;
        for(int i = 0; i < Length; i++) {
          nva.rows[i] = new double[numcollumns];
          Array.Copy(rows[i], nva.rows[i], numcollumns);
        }
      }
      return nva;
    }

    public static int DefaultCapacity = 200;

    public double[] this[int i] {
      get {
        if(i < 0 || i >= Length) throw new IndexOutOfRangeException();
        return rows[i];
      }
      set {
        if(i < 0 || i >= Length) throw new IndexOutOfRangeException();
        rows[i] = value;
      }
    }

    public ValueMatrix(string name = null, string units = null, int? capacity = null)
      : base(name, units)
    {
      int cap = capacity ?? DefaultCapacity;
      if(MaxRowCapacity < cap) throw new ArgumentOutOfRangeException();
      Length = 0;
      rows = new double[cap][];
    }
    public static implicit operator double[][] (ValueMatrix VA)
    {
      double[][] da = new double[VA.Length][];
      Array.Copy(VA.rows, da, VA.Length);
      return da;
    }
  }
}
