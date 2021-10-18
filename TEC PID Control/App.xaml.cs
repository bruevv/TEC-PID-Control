using CSUtils;
using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using TEC_PID_Control.Properties;

namespace TEC_PID_Control
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {
    const string AppGuid = "63580DDD-8C2F-46ED-8A87-C5111FD7511E";
    public string Version { get; private set; } = null;

#if DEBUG
    public bool IsDebugMode => true;
#else
    public bool IsDebugMode => false;
#endif
    Mutex mutex_AppGuid = new Mutex(false, AppGuid);
    Logger logger;
    protected override void OnStartup(StartupEventArgs e)
    {
      DispatcherUnhandledException += App_DispatcherUnhandledException;
      if (!mutex_AppGuid.WaitOne(0, false)) throw new Exception("Instance already running");

      // ClickOnce-Related
      //if (ApplicationDeployment.IsNetworkDeployed) {
      //  Version = ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
      //} else {
      Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
      if (Settings.Default.UpgradeRequired) {
        try {
          Settings.Default.Upgrade();
          MainSettings.Default.Upgrade();
          PIDSettings.Default.Upgrade();
          Interface.Default.Upgrade();
        } catch {
          Settings.Default.Reset();
          MainSettings.Default.Reset();
          PIDSettings.Default.Reset();
          Interface.Default.Reset();
        }
        Settings.Default.UpgradeRequired = false;
      }
      //}

      MainSettings.Default.LogMode = Logger.Mode.NoAutoPoll;

      CultureInfo ci = (CultureInfo)CultureInfo.InvariantCulture.Clone();
      ci.NumberFormat.NumberGroupSeparator = "";
      CultureInfo.CurrentCulture = ci;
      try {
        logger = new Logger(MainSettings.Default.LogFile, MainSettings.Default.LogMode);
      } catch (Exception ex) {
        MessageBox.Show(ex.Message, "Cannot Start Log");
        logger = null;
      }

      logger?.log($"Application Started. Version '{Version}'\n"
                  + $"Logging mode '{logger.LoggerMode}'",
                  Logger.Mode.LogState);

      base.OnStartup(e);
    }
    protected override void OnExit(ExitEventArgs e)
    {
      logger?.log($"Application Closing", Logger.Mode.LogState);
      logger?.Dispose();

      Settings.Default.Save();
      MainSettings.Default.Save();
      PIDSettings.Default.Save();
      Interface.Default.Save();

      mutex_AppGuid.Dispose();

      base.OnExit(e);
    }
    bool ShutdDownInitiated = false;

    void App_DispatcherUnhandledException(object s, DispatcherUnhandledExceptionEventArgs e)
    {
      if (ShutdDownInitiated) {
        return;
      }
      ShutdDownInitiated = true;
      logger?.log($"Error - Application terminated", e.Exception, Logger.Mode.LogState);
      e.Dispatcher.Invoke(() => {
        MessageBox.Show($"{e.Exception.Message}\n\n" +
        $"{e.Exception.InnerException?.Message}\n\n" +
        $"See Log File for Details\n{Logger.Default.FileName}",
        "Error - Application terminated");
      });
    }
  }
}
