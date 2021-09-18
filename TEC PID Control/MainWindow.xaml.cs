using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CustomWindows;

namespace TEC_PID_Control
{
  using System.ComponentModel;
  using Calibration;
  using CSUtils;
  using Devices.Keithley;
  using Devices.GWI;
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : CustomWindow
  {
    public Keithley2400 KD;
    public GWPowerSupply GWPS;

    TempSensor TC;
    public MainWindow()
    {
      var logger = new Logger(null, Logger.Mode.Full);

      InitializeComponent();

      logger.AttachLog(
        nameof(Keithley2400),
        (string msg, Logger.Mode lm) =>
        ConsoleOut.Dispatcher.Invoke(() => ConsoleOut.Text += $">{msg}\n"),
        Logger.Mode.Error);

      KD = usrCntrlK2400.KD;
      GWPS = usrCntrlGWPS.GWPS;
      //utbVoltage.DataContext = KD;
      //utbCurrent.DataContext = KD;
      //KD.Connect("COM4");
      //TC = new TempSensor(KD);
      //TC.LoadCalibration();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
      MainSettings.Default.Save();
      base.OnClosing(e);
    }

    private void TextBox_KeyDown(object sender, KeyEventArgs e)
    {
      if(e.Key == Key.Return) {
        TextBox tb = (TextBox)sender;
        KD.CustomCommand(tb.Text);
        tb.Clear();
      }
    }

    void GetCurrent_Click(object sender, RoutedEventArgs e)
    {
      KD.MeasureI();
    }
    private void Button2_Click(object sender, RoutedEventArgs e)
    {
      KD.MeasureV();

    }
    private void ConsoleOut_TextChanged(object sender, TextChangedEventArgs e)
    {
      ((TextBox)sender).ScrollToEnd();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
      usrCntrlGWPS.AutoPoll = false;
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
