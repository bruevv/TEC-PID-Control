﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CustomWindows;

namespace TEC_PID_Control
{
  using Calibration;
  using CSUtils;
  using Devices.Keithley;
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : CustomWindow
  {
    public Keithley2400 KD;
    TempSensor TC;
    public MainWindow()
    {
      var logger = new Logger(null, Logger.Mode.Full);

      InitializeComponent();

      logger.AttachLog(
        nameof(Keithley2400),
        (string msg, Logger.Mode lm) =>
        ConsoleOut.Dispatcher.Invoke(() => ConsoleOut.Text += $">{msg}\n"));

      KD = new Keithley2400();
      utbVoltage.DataContext = KD;
      utbCurrent.DataContext = KD;
      KD.Connect("COM4");
      TC = new TempSensor(KD);
      TC.LoadCalibration();
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

    async void Set_Voltage_Click(object sender, RoutedEventArgs e)
    {
      double r = await KD.MeasureR_AW(utbILim.Value, utbSetVolt.Value);
      utbRes.Value = r;
    }

    async void Set_Temperature_Click(object sender, RoutedEventArgs e)
    {
      double t= await TC.ReadTemperature(utbILim.Value, utbSetVolt.Value);
      utbTemp.Value = t;
    }
  }
}