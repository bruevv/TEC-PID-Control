using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Collections.Generic;

namespace CSUtils
{
  public class Logger : IDisposable
  {
    protected readonly Encoding Encoding = Encoding.UTF8;

    public delegate void LeggerFeedback(string message, Mode mode);

    Dictionary<string, LeggerFeedback> AttachedLogs = new Dictionary<string, LeggerFeedback>();
    Dictionary<LeggerFeedback, Mode> AttachedLogModes = new Dictionary<LeggerFeedback, Mode>();

    public void AttachLog(string name, LeggerFeedback del, Mode? mode = null)
    {
      if (AttachedLogModes.ContainsKey(del)) {
        AttachedLogModes[del] = mode ?? LoggerMode;
        return;
      }

      AttachedLogModes.Add(del, mode ?? LoggerMode);
      if (AttachedLogs.ContainsKey(name)) {
        AttachedLogs[name] += del;
      } else {
        AttachedLogs.Add(name, del);
      }
    }
    public void DeattachLog(string name, LeggerFeedback del)
    {
      AttachedLogModes.Remove(del);
      AttachedLogs[name] -= del;
      if (AttachedLogs[name] == null) AttachedLogs.Remove(name);
    }

    public enum Mode
    {
      None = 0, LogState = 9, Error = 10, AppState = 14,
      NoAutoPoll = 17, Full = 20, Debug = 30
    }

    System.Timers.Timer FlushTimer;
    const int FLUSH_INTERVAL = 1000;  // ms

    public Mode LoggerMode { get; }
    static string DefaultFilename = "debug.log";

    static Logger def = null;
    public static Logger Default => def ?? (def = new Logger(DefaultFilename, Mode.None));

    public string FileName { get; protected set; }

    StreamWriter logFile;

    public Logger(string filename, Mode mode)
    {
      if (def == null) def = this;
      LoggerMode = mode;
      if (mode == Mode.None) return;

      FileName = Path.GetFullPath(filename ?? DefaultFilename);
      bool newfile = false;
      if (!File.Exists(FileName)) newfile = true;
      var file = new FileStream(FileName, FileMode.Append, FileAccess.Write, FileShare.Read);
      logFile = new StreamWriter(file, Encoding);
      logFile.NewLine = "\n";
      if (newfile) {
        logFile?.WriteLine("Log started");
        logFile?.WriteLine(Assembly.GetEntryAssembly().FullName);
        logFile?.WriteLine($"Current DateTime '{DateTime.Now:yyyy/MM/dd HH:mm:ss}'");
      }

      FlushTimer = new System.Timers.Timer(FLUSH_INTERVAL) { AutoReset = true };
      FlushTimer.Elapsed += PeriodicFlush;
      FlushTimer.Start();
    }

    object Lock = new object();

    bool Unflushed = false;

    public static void LogWarning(string message, Exception ex, string source = null)
    {
      MessageBox.Show($"{message}\n{ex.Message ?? ""}\nSee log for details", "Warning");
      Log(message, ex, Mode.Error, source);
    }
    public static void Log(object o, Mode mode, string source = null) => Default?.log(o.ToString(), mode, source);
    public static void Log(string logMessage, Mode mode, string source = null) => Default?.log(logMessage, mode, source);
    public static void Log(string logMessage, Exception e, Mode mode, string source = null)
      => Default?.log(logMessage, e, mode, source);
    public static void Log(ref string logMessage, Exception e, Mode mode, string source = null)
      => Default?.log(ref logMessage, e, mode, source);
    public void log(object o, Mode mode, string source = null) => log(o.ToString(), mode, source);

    public void log(string logMessage, Mode mode, string source = null)
    {
      if (mode == Mode.None) throw new ArgumentException("Message Mode cannot be 'None'");

      UpdateAttachedLogs(logMessage, mode, source);

      if (this == null || mode > LoggerMode) return;

      lock (Lock) {
        string line;
        if (source == null)
          line = $"[{DateTime.Now:yyyy/MM/dd HH:mm:ss}]{mode.ToString()[0]}\t\t{logMessage.Replace("\n", "\n->\t")}";
        else
          line = $"[{DateTime.Now:yyyy/MM/dd HH:mm:ss}]{mode.ToString()[0]}\t{source}:\t{logMessage.Replace("\n", "\n->\t")}";

        try {
          logFile?.WriteLine(line);
          Unflushed = true;
        } catch (Exception) { }
      }
    }
    void UpdateAttachedLogs(string logMessage, Mode mode, string source)
    {
      try {
        if (AttachedLogs.ContainsKey(source)) {
          foreach (LeggerFeedback del in AttachedLogs[source].GetInvocationList()) {
            if (AttachedLogModes[del] >= mode)
              del.Invoke(logMessage, mode);
          }
        }
      } catch (Exception) { }
    }

    public void log(string logMessage, Exception e, Mode mode, string source = null) => Log(ref logMessage, e, mode, source);
    public void log(ref string logMessage, Exception e, Mode mode, string source = null)
    {
      string debug="";
      if (e != null) {
#if DEBUG
        debug = "\n" + e.FullStackTrace();
#endif
        if (logMessage != null) {
          log(logMessage + "\n" + e.Message + debug, mode, source);
          logMessage += "\n\n" + e.Message + debug;
        } else {
          log(e.Message + debug, mode, source);
          logMessage = e.Message + debug;
        }
      } else {
        if (logMessage != null) {
          log(logMessage, mode, source);
        }
      }
    }
    void PeriodicFlush(object sender, EventArgs e)
    {
      lock (Lock) {
        if (Unflushed) {
          logFile?.Flush();
          Unflushed = false;
        }
      }
    }

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue) {
        if (disposing) { }

        if (logFile != null) {
          FlushTimer.Stop();
          FlushTimer.Dispose();
          logFile.Flush();
          logFile.Dispose();
          logFile = null;
        }

        disposedValue = true;
      }
    }

    ~Logger() => Dispose(false);

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
    #endregion
  }

  static class ExceptionExtention
  {
    public static string FullStackTrace(this Exception e)
    {
      StringBuilder sb = new StringBuilder();
      while (e != null) {
        sb.Append(e.StackTrace);
        e = e.InnerException;
        if (e != null) sb.AppendLine();
      }
      return sb.ToString();
    }
  }
}
