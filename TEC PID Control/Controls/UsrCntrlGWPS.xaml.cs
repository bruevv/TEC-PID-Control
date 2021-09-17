﻿using CSUtils;
using Devices.GWI;
using System;
using System.ComponentModel;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace TEC_PID_Control.Controls
{
  /// <summary>
  /// Interaction logic for UsrCntrlGWPS.xaml
  /// </summary>
  public partial class UsrCntrlGWPS : UserControl
  {
    public GWPowerSupply GWPS;

    #region Binding
    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(UsrCntrlGWPS), new FrameworkPropertyMetadata(true) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty IsLogExpandedProperty =
        DependencyProperty.Register("IsLogExpanded", typeof(bool), typeof(UsrCntrlGWPS), new FrameworkPropertyMetadata(false) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty SelectedPortProperty =
        DependencyProperty.Register("SelectedPort", typeof(string), typeof(UsrCntrlGWPS), new FrameworkPropertyMetadata("") { BindsTwoWayByDefault = true });

    public static readonly DependencyProperty OutputVoltageProperty =
        DependencyProperty.Register("OutputVoltage", typeof(double), typeof(UsrCntrlGWPS), new FrameworkPropertyMetadata(1.0) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty OutputCurrentProperty =
    DependencyProperty.Register("OutputCurrent", typeof(double), typeof(UsrCntrlGWPS), new FrameworkPropertyMetadata(1.0e-4) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty AutoPollProperty =
        DependencyProperty.Register("AutoPoll", typeof(bool), typeof(UsrCntrlGWPS), new FrameworkPropertyMetadata(false) { BindsTwoWayByDefault = true });

    static readonly DependencyPropertyKey IsConnectedKey =
       DependencyProperty.RegisterReadOnly(nameof(IsConnected), typeof(bool), typeof(UsrCntrlGWPS), new PropertyMetadata(false));
    public static readonly DependencyProperty IsConnectedProperty = IsConnectedKey.DependencyProperty;

    static readonly DependencyProperty IsOn_Prop =
      DependencyProperty.Register(nameof(IsOn_Prop), typeof(bool), typeof(UsrCntrlGWPS), new PropertyMetadata(false, IsOn_PropChanged));
    static void IsOn_PropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((UsrCntrlGWPS)d).IsOn = (bool)e.NewValue;
    static readonly DependencyPropertyKey IsOnKey =
       DependencyProperty.RegisterReadOnly(nameof(IsOn), typeof(bool), typeof(UsrCntrlGWPS), new PropertyMetadata(false));
    public static readonly DependencyProperty IsOnProperty = IsOnKey.DependencyProperty;

    #endregion Binding


    [Category("Appearance")]
    public bool IsExpanded {
      get { return (bool)GetValue(IsExpandedProperty); }
      set { SetValue(IsExpandedProperty, value); }
    }
    [Category("Appearance")]
    public bool IsLogExpanded {
      get { return (bool)GetValue(IsLogExpandedProperty); }
      set { SetValue(IsLogExpandedProperty, value); }
    }

    [Category("Device")]
    public string SelectedPort {
      get { return (string)GetValue(SelectedPortProperty); }
      set { SetValue(SelectedPortProperty, value); }
    }

    [Category("Device")]
    public double OutputVoltage {
      get { return (double)GetValue(OutputVoltageProperty); }
      set { SetValue(OutputVoltageProperty, value); }
    }
    [Category("Device")]
    public double OutputCurrent {
      get { return (double)GetValue(OutputCurrentProperty); }
      set { SetValue(OutputCurrentProperty, value); }
    }
    [Category("Device")]
    public bool IsConnected {
      get { return (bool)GetValue(IsConnectedProperty); }
      protected set { SetValue(IsConnectedKey, value); }
    }
    [Category("Device")]
    public bool IsOn {
      get { return (bool)GetValue(IsOnProperty); }
      protected set { SetValue(IsOnKey, value); }
    }
    [Category("Device")]
    public bool AutoPoll {
      get { return (bool)GetValue(AutoPollProperty); }
      set { SetValue(AutoPollProperty, value); }
    }


    public UsrCntrlGWPS()
    {
      InitializeComponent();

      GWPS = new GWPowerSupply();
      GWPS.ConnectedToDevice += KD_ConnectedToDevice;
      GWPS.DisconnectedFromDevice += KD_DisconnectedFromDevice;

      utbVoltage.DataContext = GWPS;
      utbCurrent.DataContext = GWPS;

      Logger.Default.AttachLog(nameof(GWPowerSupply), (string msg, Logger.Mode lm) =>
                               tbLog.Dispatcher.Invoke(() => tbLog.Text += $">{msg}\n"),
                               Logger.Mode.Error);

      SetBinding(IsOn_Prop, new Binding("Output") { Source = GWPS, Mode = BindingMode.OneWay });
      SetBinding(AutoPollProperty, new Binding("IdlePollEnable") { Source = GWPS, Mode = BindingMode.TwoWay });
    }

    private void KD_DisconnectedFromDevice(object sender, EventArgs e)
    {
      IsConnected = false;
      circle.Fill = Brushes.LightGray;
      circle.ToolTip = GWPS.State.ToString();
    }

    private void KD_ConnectedToDevice(object sender, EventArgs e)
    {
      IsConnected = true;
      circle.Fill = Brushes.LimeGreen;
      circle.ToolTip = GWPS.State.ToString();
    }

    void bDisconnect_Click(object sender, RoutedEventArgs e) => GWPS.Disconnect();
    void bConnect_Click(object sender, RoutedEventArgs e) => GWPS.Connect(SelectedPort);

    void UsrCntrlGWPS_Loaded(object sender, RoutedEventArgs e) => UpdateComboBox();
    void UpdateComboBox()
    {
      string oldport = SelectedPort;
      cbPort.Items.Clear();

      foreach (string port in SerialPort.GetPortNames())
        cbPort.Items.Add(port);

      if (!string.IsNullOrEmpty(SelectedPort) && cbPort.Items.Contains(SelectedPort))
        cbPort.SelectedItem = oldport;
    }
    void cbPort_SelectionChanged(object s, EventArgs e)
    {
      string str = cbPort.SelectedItem as string;
      if (!string.IsNullOrEmpty(str)) SelectedPort = str;
    }

    void cbPort_DropDownOpened(object sender, EventArgs e) => UpdateComboBox();

    void bOutput_Click(object sender, RoutedEventArgs e)
    {
      if (GWPS.Output)
        GWPS.TurnOff();
      else
        GWPS.TurnOn();
    }

    void bSetUp_Click(object s, RoutedEventArgs e)
    {
      GWPS.SetV(OutputVoltage);
      GWPS.SetI(OutputCurrent);
    }

    void TbLog_TextChanged(object sender, TextChangedEventArgs e)
    {
      TextBox tb = (TextBox)sender;
      if (tb.Text.Length > 5000)
        tb.Text = "<Log trimmed>" + tb.Text.Substring(tb.Text.IndexOf('\n', 1000));
      tb.ScrollToEnd();
    }
  }
}