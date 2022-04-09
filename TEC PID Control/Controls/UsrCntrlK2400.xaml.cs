using CSUtils;
using Devices;
using Devices.Keithley;
using System;
using System.Collections;
using System.ComponentModel;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using TEC_PID_Control.Properties;
using WPFUtils.Adorners;

namespace TEC_PID_Control.Controls
{
  /// <summary>
  /// Interaction logic for UsrCntrlK2400.xaml
  /// </summary>
  public partial class UsrCntrlK2400 : UserControl
  {
    public Keithley2400 KD { get; private init; }

    #region Binding
    public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(nameof(Settings), typeof(K2400S), typeof(UsrCntrlK2400), new PropertyMetadata(K2400S.Default, K2400S_PropChanged));

    static readonly DependencyPropertyKey VoltageKey = DependencyProperty.RegisterReadOnly(nameof(Voltage), typeof(double), typeof(UsrCntrlK2400), new PropertyMetadata(double.NaN));
    static readonly DependencyPropertyKey CurrentKey = DependencyProperty.RegisterReadOnly(nameof(Current), typeof(double), typeof(UsrCntrlK2400), new PropertyMetadata(double.NaN));
    static readonly DependencyPropertyKey IsConnectedKey = DependencyProperty.RegisterReadOnly(nameof(IsConnected), typeof(bool), typeof(UsrCntrlK2400), new PropertyMetadata(false));
    static readonly DependencyPropertyKey IsOnKey = DependencyProperty.RegisterReadOnly(nameof(IsOn), typeof(bool), typeof(UsrCntrlK2400), new PropertyMetadata(false));
    static readonly DependencyPropertyKey StateKey = DependencyProperty.RegisterReadOnly(nameof(State), typeof(SState), typeof(UsrCntrlK2400), new PropertyMetadata(SState.Disconnected));

    public static readonly DependencyProperty VoltageProperty = VoltageKey.DependencyProperty;
    public static readonly DependencyProperty CurrentProperty = CurrentKey.DependencyProperty;
    public static readonly DependencyProperty IsConnectedProperty = IsConnectedKey.DependencyProperty;
    public static readonly DependencyProperty IsOnProperty = IsOnKey.DependencyProperty;
    public static readonly DependencyProperty StateProperty = StateKey.DependencyProperty;

    static readonly DependencyProperty Voltage_Prop = DependencyProperty.Register(nameof(Voltage_Prop), typeof(double), typeof(UsrCntrlK2400), new PropertyMetadata(double.NaN, Voltage_PropChanged));
    static readonly DependencyProperty Current_Prop = DependencyProperty.Register(nameof(Current_Prop), typeof(double), typeof(UsrCntrlK2400), new PropertyMetadata(double.NaN, Current_PropChanged));
    static readonly DependencyProperty IsOn_Prop = DependencyProperty.Register(nameof(IsOn_Prop), typeof(bool), typeof(UsrCntrlK2400), new PropertyMetadata(false, IsOn_PropChanged));
    static readonly DependencyProperty State_Prop = DependencyProperty.Register(nameof(State_Prop), typeof(SState), typeof(UsrCntrlK2400), new PropertyMetadata(SState.Disconnected, State_PropChanged));

    static void K2400S_PropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      //UsrCntrlK2400 o = (UsrCntrlK2400)d;
      //o.expander.DataContext = e.NewValue;
    }
    static void Voltage_PropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      UsrCntrlK2400 This = (UsrCntrlK2400)d;
      This.Voltage = (double)e.NewValue;
      This.OnMeasurementCompleted();
    }
    static void Current_PropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      UsrCntrlK2400 This = (UsrCntrlK2400)d;
      This.Current = (double)e.NewValue;
      if (This.KD.MeasureMode == Mode.CURR) This.OnMeasurementCompleted();
    }
    static void IsOn_PropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      UsrCntrlK2400 This = (UsrCntrlK2400)d;
      This.IsOn = (bool)e.NewValue;
    }
    static void State_PropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      UsrCntrlK2400 This = (UsrCntrlK2400)d;
      This.State = (SState)e.NewValue;
    }

    #endregion Binding
    #region DProperties
    [Category("Common")]
    public K2400S Settings { get => (K2400S)GetValue(SettingsProperty); set => SetValue(SettingsProperty, value); }

    [Category("Device")]
    public double Voltage {
      get { return (double)GetValue(VoltageProperty); }
      protected set { SetValue(VoltageKey, value); }
    }
    [Category("Device")]
    public double Current {
      get { return (double)GetValue(CurrentProperty); }
      protected set { SetValue(CurrentKey, value); }
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
    public SState State {
      get { return (SState)GetValue(StateProperty); }
      protected set { SetValue(StateKey, value); }
    }
    #endregion DProperties

    void AutoPollChanged(object o, RoutedEventArgs e) => KD.IdlePollEnable = (o as ToggleButton)?.IsChecked ?? false;

    public event EventHandler MeasurementCompleted;

    void OnMeasurementCompleted() => MeasurementCompleted?.Invoke(this, EventArgs.Empty);
    public UsrCntrlK2400()
    {
      KD = new Keithley2400();

      InitializeComponent();

      KD.ConnectedToDevice += KD_ConnectedToDevice;
      KD.DisconnectedFromDevice += KD_DisconnectedFromDevice;

      Logger.Default.AttachLog(KD.DeviceName, AddToLog, Logger.Mode.NoAutoPoll);

      title.Text = KD.DeviceName;

      SetBinding(IsOn_Prop, new Binding("Output") { Source = KD, Mode = BindingMode.OneWay });
      SetBinding(State_Prop, new Binding("State") { Source = KD, Mode = BindingMode.OneWay });
      SetBinding(Voltage_Prop, new Binding("Voltage") { Source = KD, Mode = BindingMode.OneWay });
      SetBinding(Current_Prop, new Binding("Current") { Source = KD, Mode = BindingMode.OneWay });
    }



    void AddToLog(object s, Logger.LogFeedBEA e) => tbLog.Text += ">" + e.Message + "\n";

    void KD_DisconnectedFromDevice(object sender, EventArgs e)
    {
      IsConnected = false;
      //      circle.Fill = Brushes.LightGray;
      //      circle.ToolTip = KD.State.ToString();
    }

    void KD_ConnectedToDevice(object sender, EventArgs e)
    {
      IsConnected = true;
      //      circle.Fill = Brushes.LimeGreen;
      //      circle.ToolTip = KD.State.ToString();
    }

    void bDisconnect_Click(object sender, RoutedEventArgs e) => KD.Disconnect();
    void bConnect_Click(object sender, RoutedEventArgs e) => ConnectCommand();
    void usrCntrlK2400_Loaded(object sender, RoutedEventArgs e)
    {
      UpdateComboBox();

      if (Settings.AutoConnect && !string.IsNullOrEmpty(Settings.Port)) {
        try {
          Logger.Default?.log($"Trying to automatically connect to Keithley 2400 SM at port:" +
            $"{Settings.Port}", Logger.Mode.NoAutoPoll, nameof(UsrCntrlK2400));
          ConnectCommand();
        } catch (Exception ex) {
          Logger.Default?.log("Error trying to automatically connect to Keithley 2400",
            ex, Logger.Mode.Error, nameof(UsrCntrlK2400));
        }
      }
    }
    void UpdateComboBox()
    {
      string oldport = Settings.Port;
      cbPort.Items.Clear();

      foreach (string port in SerialPort.GetPortNames())
        cbPort.Items.Add(port);

      if (!string.IsNullOrEmpty(Settings.Port) && cbPort.Items.Contains(Settings.Port))
        cbPort.SelectedItem = oldport;
      else Settings.Port = "";
    }
    void cbPort_SelectionChanged(object s, EventArgs e)
    {
      string str = cbPort.SelectedItem as string;
      if (!string.IsNullOrEmpty(str)) Settings.Port = str;
    }

    void cbPort_DropDownOpened(object sender, EventArgs e) => UpdateComboBox();

    void bOutput_Click(object sender, RoutedEventArgs e)
    {
      if (KD.Output)
        KD.ScheduleTurnOFF();
      else
        KD.ScheduleTurnON();
    }

    void bSetUpI_Click(object s, RoutedEventArgs e) => SetUpICommand();

    async void bMesRes_Click(object sender, RoutedEventArgs e)
    {
      _ = await KD.MeasureRAsync(utbSetCurrent.Value, Math.Abs(utbSetVoltage.Value));
    }

    void tbLog_TextChanged(object sender, TextChangedEventArgs e)
    {
      TextBox tb = (TextBox)sender;
      if (tb.Text.Length > 5000)
        tb.Text = "<Log trimmed>" + tb.Text.Substring(tb.Text.IndexOf('\n', 1000));
      tb.ScrollToEnd();
    }

    public void SetUpICommand() => KD.SourceI(utbSetCurrent.Value, Math.Abs(utbSetVoltage.Value));
    public void ConnectCommand() => KD.Connect(Settings.Port);

    void bReset_Click(object sender, RoutedEventArgs e) => KD.ScheduleReset();

  }
  public static class LocalValueEnumeratorExtention
  {
    public static IEnumerator GetEnumerator(this LocalValueEnumerator lve) => lve;
  }
}
