using CSUtils;
using System;
using System.Configuration;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;
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

    Settings Sets;


    public static ApplicationSettingsBase[] SettingsList;
    //  public static new App Current => (App)Application.Current;

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

      try {
        Sets = Settings.Instance;
      } catch {
      }


      // ClickOnce-Related
      //if (ApplicationDeployment.IsNetworkDeployed) {
      //  Version = ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
      //} else {
      Version = Assembly.GetExecutingAssembly().GetName().Version.ToString() 
        + (IsDebugMode ? " - debug" : "");

      //}

      CultureInfo ci = (CultureInfo)CultureInfo.InvariantCulture.Clone();
      ci.NumberFormat.NumberGroupSeparator = "";
      CultureInfo.CurrentCulture = ci;
      try {
        logger = new Logger(Settings.Instance.LogFile, Sets.LogMode);
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

      Settings.Instance.FirstRun = false;
      Settings.Instance.Save();

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

    void SetupApplicationCommands()
    {
      ApplicationCommands.Open.InputGestures.Clear();
      ApplicationCommands.Open.InputGestures.Add(new KeyGesture(Key.L, ModifierKeys.Control));
      ApplicationCommands.Open.Text = "Load Settings...";
      ApplicationCommands.Save.Text = "Save Settings...";
    }
  }
}
