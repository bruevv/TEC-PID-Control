using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TEC_PID_Control
{
  using CSUtils;
  using CustomWindows;
  using Devices.GWI;
  using Devices.Keithley;
  using System.Diagnostics;
  using TEC_PID_Control.Controls;
  using TEC_PID_Control.PID;

  public partial class MainWindow : CustomWindow
  {
    public Keithley2400 KD;
    public GWPowerSupply GWPS;

    TempSensor TC;
    public MainWindow()
    {
      var logger = Logger.Default;

      try {
        InitializeComponent();

        Dispatcher.ShutdownStarted += (o, e) => usrCntrlPID.Dispose();

        logger?.AttachLog(null, AddToLog, Logger.Mode.NoAutoPoll);

        KD = usrCntrlK2400.KD;
        GWPS = usrCntrlGWPS.GWPS;

        TC = new TempSensor(usrCntrlK2400);
        TC.LoadCalibration();
       
        TECPIDdll.DLL.SetSetPoint(10.0); // check if dll file present

        usrCntrlPID.Init(new TempSensorInterface(TC), new GWIPSControlInterface(usrCntrlGWPS));
        usrCntrlK2400.MeasurementCompleted += K2400_MC;

        logger?.log($"Trying to automatically connect to following devices:\n" +
                    $"Keithley 2400 Port:<{usrCntrlK2400.SelectedPort}>\n" +
                    $"GWI Power Supply Port:<{usrCntrlGWPS.SelectedPort}>",
                    Logger.Mode.AppState, nameof(MainWindow));
        try { 
          usrCntrlK2400.ConnectCommand();
          usrCntrlGWPS.ConnectCommand();
        }catch(Exception e) {
          logger?.log("Error trying to automatically connect", 
            e, Logger.Mode.Error, nameof(MainWindow));
        }
      } catch (Exception e) {
        logger?.log("Error Loading Application", e, Logger.Mode.Error, nameof(MainWindow));
        MessageBox.Show($"{e.Message}\n\nSee Log:\n\n{logger.FileName}\n\nfor details", "Error");
        Application.Current.Shutdown();
      }
    }

    void K2400_MC(object s, EventArgs e) => utbTemp.Value = TC.ConvertRtoT(KD.Resistance);

    void AddToLog(object s, Logger.LogFeedBEA e) => ConsoleOut.Text += $">{e.Message}\n";

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

    private void Button_Click(object sender, RoutedEventArgs e)
    {
      IMeasureParameter ki = new TempSensorInterface(TC);
      ki.Init();
      double d = ki.Measure();
      d = ki.Measure();
    }


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
