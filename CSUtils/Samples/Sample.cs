using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text;

namespace Samples
{
  using System.ComponentModel;
  using System.Linq.Expressions;
  using System.Threading.Tasks;
  using CSUtils;
  using Data;

  /// <summary>
  /// Base class for Sample that can be saved in Origin compatible ASCII file
  /// </summary>
  public abstract class Sample : INotifyPropertyChanged, ISample
  {
    protected readonly Encoding Encoding = Encoding.UTF8;
    public object Tag { get; set; } = null;

    protected string fileName = "";
    public string FileName => fileName;
    string fullFileName = null;
    public string FullFileName => fullFileName ?? FileName;
    public bool HasFile => !string.IsNullOrEmpty(FullFileName);
    public string DefaultFileName => $"{Key}.{FileExtension}";
    bool isLoaded;
    bool isHeaderLoaded;
    public bool IsLoaded {
      get => isLoaded;
      protected set {
        if(isLoaded != value) {
          bool ihl = IsHeaderLoaded;
          isLoaded = value;
          ChangeProperty(nameof(IsLoaded));
          if(ihl != IsHeaderLoaded) ChangeProperty(nameof(IsHeaderLoaded));
        }
      }
    }
    public bool IsHeaderLoaded {
      get => IsLoaded ? true : isHeaderLoaded;
      protected set {
        if(isHeaderLoaded != value) {
          isHeaderLoaded = value;
          ChangeProperty(nameof(IsHeaderLoaded));
        }
      }
    }
    protected abstract string FileExtension { get; }

    public string Name {
      get => string.IsNullOrEmpty(SP.SampleName) ? Key : SP.SampleName;
      set {
        if(SP.SampleName != value) {
          SP.SampleName = value;
          if(HasFile) {
            SaveToFile();
            string ofn = FullFileName;
            try {
              string name = value;
              foreach(char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
              name = name.Replace('.', '_');
              string nfn = Path.Combine(Path.GetDirectoryName(FullFileName), name);
              nfn = Path.ChangeExtension(nfn, FileExtension);
              if(File.Exists(nfn)) throw new IOException("File Already Exists");
              File.Move(ofn, nfn);
              fullFileName = nfn;
            } catch(Exception e) {
              Logger.Log("Error renaming sample file", e, Logger.Mode.Error, nameof(Sample));
              fullFileName = ofn;
            }
          }
          ChangeProperty(nameof(Name));
        }
      }
    }
    public string Comments {
      get => GUtils.Unescape(SP.Comments);
      set {

        SP.Comments = GUtils.Escape(value);
        if(HasFile) SaveToFile();
        ChangeProperty(nameof(Comments));
      }
    }
    public string Key {
      get => string.IsNullOrEmpty(SP.Key) ? MeasureDateTimeString : SP.Key;
      set => SP.Key = value;
    }
    public string NameDateComments {
      get {
        StringBuilder sb = new StringBuilder(100);
        sb.AppendLine(!string.IsNullOrEmpty(SP.SampleName) ? "Name not set" : SP.SampleName);
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
    protected string MeasureDateTimeString => SP.MeasureDateTime.ToString(@"yyyy.MM.dd HH.mm.ss");

    protected SampleParams SP;
    public Range Range => SP.Range;

    abstract public int Length { get; }
    public bool IsEmpty => Length == 0;


    protected Sample(SampleParams sp) { SP = sp; IsLoaded = true; }
    protected Sample(string filename, SampleParams sp)
    {
      isLoaded = false;
      fileName = filename;
      SP = sp;

      using(StreamReader fs = new StreamReader(fileName, Encoding))
        SampleParams.ReadFromStream(fs, SP);
      isHeaderLoaded = true;
    }
    protected Sample()
    {
      IsLoaded = true;
      SP = new SampleParams();
    }

    protected DataColumn[] data = null;
    public void ClearData() { foreach(DataColumn dc in data) dc?.Clear(); }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, e);
    protected void ChangeProperty(string p = null) => OnPropertyChanged(new PropertyChangedEventArgs(p));

    public int SampleRank => data?.Length ?? 0;
    protected virtual void WriteFileHeader(StreamWriter sw)
    {
      for(int i = 0; i < SampleRank; i++) {
        sw.Write(data[i].Name);
        if(i != SampleRank - 1) sw.Write('\t');
      }
      sw.WriteLine();
      for(int i = 0; i < SampleRank; i++) {
        sw.Write(data[i].Units);
        if(i != SampleRank - 1) sw.Write('\t');
      }
      sw.WriteLine();
    }

    protected virtual void SaveToStream(StreamWriter sw)
    {
      SP.WriteToStream(sw);
      WriteFileHeader(sw);
      for(int i = 0; i < Length; i++) {
        for(int j = 0; j < SampleRank; j++) {
          sw.Write(data[j].GetDataAsString(i));
          if(j != SampleRank - 1) sw.Write('\t');
        }
        if(i != Length - 1) sw.WriteLine();
      }
      sw.Flush();
    }
    protected abstract void ReadFromStream(StreamReader s);

    public void SaveToFile(string path = "", string fn = "")
    {
      if(Length == 0) throw new Exception("Sample is empty");

      fn = fn ?? ""; path = path ?? "";

      if(fn == "") {
        if(string.IsNullOrEmpty(FullFileName)) {
          if(SP.SampleName != null) fileName = $"{SP.SampleName}.{FileExtension}";
          else fileName = "";
          if(File.Exists(Path.Combine(path, fileName))) fileName = "";

          if(string.IsNullOrEmpty(fileName)) fileName = DefaultFileName;

          fullFileName = Path.Combine(path, fileName);
        }
      } else {
        fileName = fn;
        fullFileName = Path.Combine(path, fileName);
      }
      SP.Points = Length;
      using(FileStream f = File.Create(FullFileName)) {
        fullFileName = f.Name;
        SaveToStream(new StreamWriter(f, Encoding));
      }
      fileName = Path.GetFileName(fullFileName);
    }
    public void LoadFromFile()
    {
      if(IsLoaded) return;
      using(FileStream fs = new FileStream(fileName, FileMode.Open))
        ReadFromStream(new StreamReader(fs, Encoding));
      fullFileName = Path.GetFullPath(fileName);
      IsLoaded = true;
      ChangeProperty("NeedUpdate");
    }

    public delegate void EnumSampleDel(Sample s);
    delegate Task<T> PreLoadSDel<T>(string fname) where T : Sample;

    public static async Task<bool> LoadSamplesFromFolder(string folder, EnumSampleDel EnumSample)
    {
      bool succesful = false;
      succesful |= await EnumerateDir(folder, EnumSample, SampleSpectrum.PreLoadSample, SampleSpectrum.Extension);
      succesful |= await EnumerateDir(folder, EnumSample, SampleScaler.PreLoadSample, SampleScaler.Extension);
      succesful |= await EnumerateDir(folder, EnumSample, SampleKinetic.PreLoadSample, SampleKinetic.Extension);

      return succesful;
    }
    public static async Task<Sample> PreLoadSample(string file)
    {
      string ext = Path.GetExtension(file)?.Substring(1);
      Sample s;
      switch(ext) {
        case SampleSpectrum.Extension:
          s = await SampleSpectrum.PreLoadSample(file);
          break;
        case SampleScaler.Extension:
          s = await SampleScaler.PreLoadSample(file);
          break;
        case SampleKinetic.Extension:
          s = await SampleKinetic.PreLoadSample(file);
          break;
        default:
          return null;
      }

      return s;
    }
    public static bool CheckExtension(string file)
    {
      string ext = Path.GetExtension(file)?.Substring(1);
      switch(ext) {
        case SampleSpectrum.Extension:
        case SampleScaler.Extension:
        case SampleKinetic.Extension:
          return true;
        default:
          return false;
      }
    }
    static async Task<bool> EnumerateDir<T>(string dir, EnumSampleDel esd, PreLoadSDel<T> plsd, string ext) where T : Sample
    {
      bool succesful = false;
      if(!Directory.Exists(dir)) {
        Logger.Log($"Error in {nameof(EnumerateDir)}<{typeof(T).Name}>. Dir doesn't exist ({dir})",
          Logger.Mode.Error, nameof(Sample));
        return false;
      }
      try {
        string[] files = Directory.GetFiles(dir, $"*.{ext}");
        for(int i = 0; i < files.Length; i++) {
          T s = await plsd(files[i]);
          if(s != null) {
            esd(s);
            succesful = true;
          }
        }
      }catch (Exception e) {
        Logger.LogWarning($"Error Enumerating dir ({dir})", e, nameof(Sample));
      }
      return succesful;
    }

    public override string ToString()
    {
      if(SP.SampleName == "")
        return SP.MeasureDateTime.ToLongTimeString();
      else
        return SP.SampleName;
    }

    /// <summary>
    /// Properties saved to file (can be restored to current measure
    /// settings). Some of them may not exist in measure settings.
    /// </summary>
    protected class SampleParams
    {
      public string SampleName = null;
      public string Comments = "";
      public DateTime MeasureDateTime = default;
      public string Key = "";
      public Range Range = null;
      public int Points;

      internal SampleParams() { }
      protected SampleParams(SampleParams sp)
      {
        foreach(FieldInfo pi in GetFields(GetType()))
          pi.SetValue(this, pi.GetValue(sp));
      }
      /// <summary> Don't copy range! </summary>
      public static void CopyParamsByName(Sample fromS, Sample toS)
      {
        SampleParams from = fromS.SP;
        SampleParams to = toS.SP;

        FieldInfo[] FromFields = GetFields(from.GetType());
        string[] FromFNames = new string[FromFields.Length];
        for(int i = 0; i < FromFields.Length; i++)
          FromFNames[i] = FromFields[i].Name;
        foreach(FieldInfo pi in GetFields(to.GetType())) {
          if(pi.Name == "Range") continue;
          int i = Array.IndexOf(FromFNames, pi.Name);
          if(i >= 0 && pi.FieldType.Equals(FromFields[i].FieldType))
            pi.SetValue(to, FromFields[i].GetValue(from));
        }

      }
      protected SampleParams(DateTime? dt, params SettingsBase[] sets)
      {
        if((sets?.Length ?? 0) > 0) {
          foreach(FieldInfo pi in GetFields(GetType())) {
            foreach(SettingsBase set in sets) {
              try {
                pi.SetValue(this, set[pi.Name]);
              } catch(SettingsPropertyNotFoundException) {
                // Do not set (no such property)
              }
            }
          }
        }
        MeasureDateTime = dt ?? DateTime.Now;
      }

      public void RestoreSettings(params SettingsBase[] sets)
      {
        foreach(FieldInfo pi in GetFields(GetType())) foreach(SettingsBase set in sets) try {
              set[pi.Name] = pi.GetValue(this);
            } catch(SettingsPropertyNotFoundException) {
              // Do not set (no such property)
            }
      }
      internal void WriteToStream(StreamWriter sw)
      {
        foreach(FieldInfo pi in GetFields(GetType())) {
          object val = pi.GetValue(this);
          sw.WriteLine($"{pi.Name}={val ?? ""}");
        }
      }
      static Dictionary<Type, FieldInfo[]> Fields = new Dictionary<Type, FieldInfo[]>();
      static FieldInfo[] GetFields(Type type)
      {
        FieldInfo[] pia;
        if(Fields.ContainsKey(type)) pia = Fields[type];
        else Fields.Add(type, pia = type.GetFields());
        return pia;
      }
      internal static void ReadFromStream(StreamReader sr, SampleParams sp)
      {
        string line;
        if(sr.BaseStream.Length == 0) throw new Exception("Sample File is EMPTY");
        while((line = sr.ReadLine()).Contains("=")) {
          int colonindex = line.IndexOf('=');
          string name = line.Substring(0, colonindex);
          string val = line.Substring(colonindex + 1);
          foreach(FieldInfo fi in GetFields(sp.GetType())) {
            if(fi.Name.Equals(name)) {
              if(fi.FieldType == typeof(Range))
                fi.SetValue(sp, (Range)val);
              else
                fi.SetValue(sp, Convert.ChangeType(val, fi.FieldType));
              break;
            }
          }
        }
        _ = sr.ReadLine(); // This line contains units
      }
    }
  }
  public interface ISample
  {
    string Name { get; set; }
    bool IsEmpty { get; }
  }
  public interface ISSpectrum : ISample
  {
    //    event Action<IList<double>, IList<double>> Changed;
    ValueArray X { get; }
    ValueArray Y { get; }
  }
  public interface ISKinetic : ISample
  {
    //    event Action<IList<double>, IList<double>> Changed;
    ValueArray X { get; }
    ValueArray Y { get; }
  }
  public interface ISSpecRange : ISKinetic, ISSpectrum
  {
    double X1 { get; }
    double X2 { get; }

    void SetRange(double x1 = double.NaN, double x2 = double.NaN);
  }
  public interface ISKineticRange : ISKinetic, ISSpectrum
  {
    double X1 { get; }
    double X2 { get; }
    void SetRange(double x1 = double.NaN, double x2 = double.NaN);
  }
  public static class SampleExtentions
  {
    public static double GetSpectrumAtWL(this ISSpectrum ss, double wl)
    {
      try {
        return ss.Y?[ss.X?.GetIndexAprox(wl) ?? 0] ?? double.NaN;
      } catch { return double.NaN; }
    }
    public static double GetNormSpectrumAtWL(this ISSpectrum ss, double wl)
    {
      try {
        return ss.Y?[ss.X?.GetIndexAprox(wl) ?? 0] / ss.Y.Normalize ?? double.NaN;
      } catch { return double.NaN; }
    }
    public static double GetKineticAtDelay(this ISKinetic sk, double del)
    {
      try {
        return sk.Y?[sk.X?.GetIndexAprox(del) ?? 0] ?? double.NaN;
      } catch { return double.NaN; }
    }
    public static double GetNormKineticAtDelay(this ISKinetic sk, double del)
    {
      try {
        return sk.Y?[sk.X?.GetIndexAprox(del) ?? 0] / sk.Y.Normalize ?? double.NaN;
      } catch { return double.NaN; }
    }
    public static bool IsFullRange(this ISKineticRange isr) => double.IsNaN(isr.X1) && double.IsNaN(isr.X2);
    public static bool IsFullRange(this ISSpecRange isr) => double.IsNaN(isr.X1) && double.IsNaN(isr.X2);
  }
}
