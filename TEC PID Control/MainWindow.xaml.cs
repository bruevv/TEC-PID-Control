using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace TEC_PID_Control
{
  using CSUtils;
  using CustomWindows;
  using Devices.GWI;
  using Devices.Keithley;
  using TEC_PID_Control.Controls;
  using TEC_PID_Control.Properties;
  using WPFUtils.Adorners;

  public partial class MainWindow : SimpleToolWindow
  {
    public Keithley2400 KD;
    public GWPowerSupply GWPS;

    TempSensor TC;

    //    _Settings Sets = _Settings.Instance;

    public MainWindow()
    {
      Logger logger = null;
      try {
        logger = Logger.Default;

        ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject),
          new FrameworkPropertyMetadata(20000));
        ToolTipService.PlacementProperty.OverrideMetadata(typeof(DependencyObject),
          new FrameworkPropertyMetadata(PlacementMode.Top));
        InitializeComponent();

        Dispatcher.ShutdownStarted += (o, e) => usrCntrlPID.Dispose();

        logger?.AttachLog(null, AddToLog, Logger.Mode.NoAutoPoll);

        UpdateSettings();

        ToolTipWithAdorner.ShowHelpMarkers = () => Settings.Instance.ShowHelpMarkers;
        ToolTipWithAdorner.IconBrush = (System.Windows.Media.Brush)FindResource("InfoIcon");

        BuldHelpMarkerAdorners();
        EnableTooltipsMI_Checked(null, null);

        KD = usrCntrlK2400.KD;
        GWPS = usrCntrlGWPS.GWPS;

        TC = new TempSensor(usrCntrlK2400);
        TC.LoadCalibration();

        TECPIDdll.DLL.SetSetPoint(10.0); // check if dll file present

        usrCntrlPID.Init(new TempSensorInterface(TC), new GWIPSControlInterface(usrCntrlGWPS));
        usrCntrlK2400.MeasurementCompleted += K2400_MC;
        usrCntrlPID.UpdateVIs += DllUpdateVIs;

      } catch (Exception e) {
        string link = Environment.Is64BitProcess ?
          @"https://aka.ms/vs/17/release/vc_redist.x64.exe" :
          @"https://aka.ms/vs/17/release/vc_redist.x86.exe";

        if (e.Message.Contains("0x8007007E")) {
          logger?.log("Error Loading Application\n" +
            "Try reinstalling application or\n" +
            "installing the VS++ redistributtable:\n" +
            link, e, Logger.Mode.Error, "APP");
          var res = MessageBox.Show($"{e.Message}\n\nSee Log:\n\n{logger?.FileName ?? "<N/A>"}\n\nfor details\n\n" +
            "Try installing the VS++ redistributtable:\n" +
            link + "\nPress Ok to download from this link", "Error", MessageBoxButton.OKCancel);
          if (res == MessageBoxResult.OK) {
            ProcessStartInfo psi = new ProcessStartInfo(link) { UseShellExecute = true };
            Process p = new Process() { StartInfo = psi };
            p.Start();
          }
        } else {
          logger?.log("Error Loading Application", e, Logger.Mode.Error, "APP");
          MessageBox.Show($"{e.Message}\n\nSee Log:\n\n{logger?.FileName ?? "<N/A>"}\n\nfor details", "Error");
        }
        Application.Current.Shutdown();
      }
    }

    void BuldHelpMarkerAdorners()
    {
      ToolTipWithAdorner.AddToAllChilderenWithTooltip<TextBlock>(this);
      ToolTipWithAdorner.AddToAllChilderenWithTooltip<ToggleButton>(this);
    }
    void EnableTooltips(bool enable)
    {
      ToolTipWithAdorner.EnableTooltip<TextBlock>(this, enable);
      ToolTipWithAdorner.EnableTooltip<ToggleButton>(this, enable);
    }
    void DllUpdateVIs(object sender, UsrCntrlPID.UpdateVIsEA e)
    {
      UsrCntrlGWPS[] ucga = { usrCntrlGWPS, usrCntrlGWPS2 };
      foreach (var ucg in ucga) {
        if (ucg.GWPS.IsControlled) continue;

        if (ucg.Settings.Channel == GWPSChannel.A) {
          if (e.V1 is double v1) {
            ucg.Settings.OutputVoltage = v1;
            ucg.GWPS.ScheduleSetV(v1);
          }
          if (e.I1 is double i1) {
            ucg.Settings.OutputVoltage = i1;
            ucg.GWPS.ScheduleSetI(i1);
          }
        } else if (ucg.Settings.Channel == GWPSChannel.B) {
          if (e.V2 is double v2) {
            ucg.Settings.OutputVoltage = v2;
            ucg.GWPS.ScheduleSetV(v2);
          }
          if (e.I2 is double i2) {
            ucg.Settings.OutputVoltage = i2;
            ucg.GWPS.ScheduleSetI(i2);
          }
        }
      }
    }

    void K2400_MC(object s, EventArgs e)
    {
      double t = TC.ConvertRtoT(KD.Resistance);
      utbTemp.Value = t;
      if (!usrCntrlPID.IsControlEnabled)
        usrCntrlPID.SetCurrentTemperature(t);
    }

    void AddToLog(object s, Logger.LogFeedBEA e) => ConsoleOut.Text += $"{e.Source.PadRight(10)}> {e.Message}\n";

    private void TextBox_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Return) {
        TextBox tb = (TextBox)sender;
        KD.CustomCommand(tb.Text);
        tb.Clear();
      }
    }

    private void ConsoleOut_TextChanged(object sender, TextChangedEventArgs e)
    {
      TextBox tb = (TextBox)sender;
      if (tb.Text.Length > 30000)
        tb.Text = "<Log trimmed>" + tb.Text.Substring(tb.Text.IndexOf('\n', 1000));
      tb.ScrollToEnd();
    }

    async void bMeatureT_Click(object s, RoutedEventArgs e)
    {
      double T = await TC.ReadTemperatureAsync();
      utbTemp.Value = T;
    }

    void MiViewLog_Click(object s, RoutedEventArgs e)
    {
      ProcessStartInfo psi;
      try {
        RegistryKey rk = Registry.LocalMachine.OpenSubKey(
          @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\notepad++.exe");
        rk ??= Registry.LocalMachine.OpenSubKey(
          @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\notepad++.exe");
        if (rk?.GetValue("") is string path) {
          psi = new(path, $"-n999999 \"{Logger.Default.FileName}\"");
          Process p = Process.Start(psi);
        } else throw new InvalidOperationException();
      } catch {
        try {
          psi = new("notepad.exe", Logger.Default.FileName);
          Process.Start(psi);
        } catch (Exception ex) {
          MessageBox.Show("Cannot Open Log File\n" + ex.Message, "Warning");
        }
      }
    }

    void SaveSettings(object sender, RoutedEventArgs e)
    {
      SaveFileDialog saveDialog = new() {
        AddExtension = true,
        Filter = "Settings (.settings) | *.settings",
        CheckPathExists = true,
        OverwritePrompt = true,
        DefaultExt = ".settings",
        ValidateNames = true,
        FileName = DateTime.Today.ToString("yy.MM.dd"),
      };
      bool? res = saveDialog.ShowDialog(this);

      if (res == true) Settings.Instance.Save(saveDialog.FileName);
    }

    private void LoadSettings(object sender, ExecutedRoutedEventArgs e)
    {
      OpenFileDialog openDialog = new() {
        Filter = "Settings (.settings) | *.settings",
        CheckPathExists = true,
        DefaultExt = ".settings",
        ValidateNames = true
      };
      bool? res = openDialog.ShowDialog(this);

      if (res == true) Settings.Instance.Load(openDialog.FileName);
    }

    void Exit(object sender, ExecutedRoutedEventArgs e) => Close();

    void RefreshInterface(object s, RoutedEventArgs e) => ToolTipWithAdorner.RefreshAllSCA();

    void EditSettings(object s, ExecutedRoutedEventArgs e)
    {
      EditProperties ep = new(Settings.Instance, Settings.Default);
      ep.Owner = this;
      ep.ShowDialog();
      Settings.Instance.Save();
      UpdateSettings();
      RefreshInterface(s, e);
    }
    void UpdateSettings()
    {
      ToolTipWithAdorner.InactiveOppacity = Settings.Instance.Interface.HelpMarkersOpacity;
    }

    void EnableTooltipsMI_Checked(object sender, RoutedEventArgs e)
    {
      if (Settings.Instance.ShowToolTips) {
        EnableTooltips(true);
      } else {
        EnableTooltips(false);
        if (Settings.Instance.ShowHelpMarkers) {
          Settings.Instance.ShowHelpMarkers = false;
          RefreshInterface(null, null);
        }
      }
    }

    void EBHMI_Click(object s, RoutedEventArgs e) => MessageBox.Show("Sorry, no better help available.", "Warning");

    //async void Set_Voltage_Click(object sender, RoutedEventArgs e)
    //{
    //  double r = await KD.MeasureR_AW(utbILim.Value, utbSetVolt.Value);
    //  utbRes.Value = r;
    //}

    //async void Set_Temperature_Click(object sender, RoutedEventArgs e)
    //{
    //  double t= await TC.ReadTemperature(utbILim.Value, utbSetVolt.Value);
    //  utbTemp.Value = t;
    //}
  }
}
