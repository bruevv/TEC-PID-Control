using CSUtils;
using Devices;
using Devices.Keithley;
using System;
using System.ComponentModel;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace TEC_PID_Control.Controls
{
  /// <summary>
  /// Interaction logic for UsrCntrlK2400.xaml
  /// </summary>
  public partial class UsrCntrlK2400 : UserControl
  {
    public Keithley2400 KD;

    #region Binding
    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(UsrCntrlK2400), new FrameworkPropertyMetadata(true) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty IsLogExpandedProperty =
        DependencyProperty.Register("IsLogExpanded", typeof(bool), typeof(UsrCntrlK2400), new FrameworkPropertyMetadata(false) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty SelectedPortProperty =
        DependencyProperty.Register("SelectedPort", typeof(string), typeof(UsrCntrlK2400), new FrameworkPropertyMetadata("") { BindsTwoWayByDefault = true });

    static readonly DependencyPropertyKey VoltageKey = DependencyProperty.RegisterReadOnly(nameof(Voltage), typeof(double), typeof(UsrCntrlK2400), new PropertyMetadata(double.NaN));
    static readonly DependencyPropertyKey CurrentKey = DependencyProperty.RegisterReadOnly(nameof(Current), typeof(double), typeof(UsrCntrlK2400), new PropertyMetadata(double.NaN));

    public static readonly DependencyProperty VoltageProperty = VoltageKey.DependencyProperty;
    public static readonly DependencyProperty CurrentProperty = CurrentKey.DependencyProperty;

    static readonly DependencyProperty Voltage_Prop = DependencyProperty.Register(nameof(Voltage_Prop), typeof(double), typeof(UsrCntrlK2400), new PropertyMetadata(double.NaN, Voltage_PropChanged));
    static readonly DependencyProperty Current_Prop = DependencyProperty.Register(nameof(Current_Prop), typeof(double), typeof(UsrCntrlK2400), new PropertyMetadata(double.NaN, Current_PropChanged));

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

    public static readonly DependencyProperty OutputVoltageProperty =
        DependencyProperty.Register("OutputVoltage", typeof(double), typeof(UsrCntrlK2400), new FrameworkPropertyMetadata(1.0) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty OutputCurrentProperty =
    DependencyProperty.Register("OutputCurrent", typeof(double), typeof(UsrCntrlK2400), new FrameworkPropertyMetadata(1.0e-4) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty AutoPollProperty =
    DependencyProperty.Register("AutoPoll", typeof(bool), typeof(UsrCntrlK2400), new FrameworkPropertyMetadata(false, AutoPollChanged) { BindsTwoWayByDefault = true });
    static readonly DependencyPropertyKey IsConnectedKey =
       DependencyProperty.RegisterReadOnly(nameof(IsConnected), typeof(bool), typeof(UsrCntrlK2400), new PropertyMetadata(false));
    public static readonly DependencyProperty IsConnectedProperty = IsConnectedKey.DependencyProperty;

    static readonly DependencyProperty IsOn_Prop =
      DependencyProperty.Register(nameof(IsOn_Prop), typeof(bool), typeof(UsrCntrlK2400), new PropertyMetadata(false, IsOn_PropChanged));
    static readonly DependencyProperty State_Prop =
   DependencyProperty.Register(nameof(State_Prop), typeof(SState), typeof(UsrCntrlK2400), new PropertyMetadata(SState.Disconnected, State_PropChanged));

    static void IsOn_PropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((UsrCntrlK2400)d).IsOn = (bool)e.NewValue;
    static void State_PropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((UsrCntrlK2400)d).State = (SState)e.NewValue;

    static readonly DependencyPropertyKey IsOnKey =
       DependencyProperty.RegisterReadOnly(nameof(IsOn), typeof(bool), typeof(UsrCntrlK2400), new PropertyMetadata(false));
    static readonly DependencyPropertyKey StateKey =
     DependencyProperty.RegisterReadOnly(nameof(State), typeof(SState), typeof(UsrCntrlK2400), new PropertyMetadata(SState.Disconnected));
    public static readonly DependencyProperty IsOnProperty = IsOnKey.DependencyProperty;
    public static readonly DependencyProperty StateProperty = StateKey.DependencyProperty;
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
    public SState State {
      get { return (SState)GetValue(StateProperty); }
      protected set { SetValue(StateKey, value); }
    }
    [Category("Device")]
    public bool AutoPoll {
      get { return (bool)GetValue(AutoPollProperty); }
      set { SetValue(AutoPollProperty, value); }
    }
    static void AutoPollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((UsrCntrlK2400)d).KD.IdlePollEnable = (bool)e.NewValue;

    public event EventHandler MeasurementCompleted;

    void OnMeasurementCompleted() => MeasurementCompleted?.Invoke(this, EventArgs.Empty);

    public UsrCntrlK2400()
    {
      InitializeComponent();

      KD = new Keithley2400();
      KD.ConnectedToDevice += KD_ConnectedToDevice;
      KD.DisconnectedFromDevice += KD_DisconnectedFromDevice;

      utbResistance.DataContext = KD;

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
    void usrCntrlK2400_Loaded(object sender, RoutedEventArgs e) => UpdateComboBox();
    void UpdateComboBox()
    {
      string oldport = SelectedPort;
      cbPort.Items.Clear();

      foreach (string port in SerialPort.GetPortNames())
        cbPort.Items.Add(port);

      if (!string.IsNullOrEmpty(SelectedPort) && cbPort.Items.Contains(SelectedPort))
        cbPort.SelectedItem = oldport;
      else SelectedPort = "";
    }
    void cbPort_SelectionChanged(object s, EventArgs e)
    {
      string str = cbPort.SelectedItem as string;
      if (!string.IsNullOrEmpty(str)) SelectedPort = str;
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
    public void ConnectCommand() => KD.Connect(SelectedPort);

    void bReset_Click(object sender, RoutedEventArgs e) => KD.ScheduleReset();
  }
}
