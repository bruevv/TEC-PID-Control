using CSUtils;
using Devices;
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
        DependencyProperty.Register(nameof(SelectedPort), typeof(string), typeof(UsrCntrlGWPS), new FrameworkPropertyMetadata("", SelectedPortChanged) { BindsTwoWayByDefault = true });
    static void SelectedPortChanged(DependencyObject o, DependencyPropertyChangedEventArgs ea) {
      var t = (UsrCntrlGWPS)o;
      if (!t.cbPort.Items.Contains(t.SelectedPort)) t.cbPort.Items.Add(t.SelectedPort);
      t.cbPort.SelectedItem = t.SelectedPort;
    }
    public static readonly DependencyProperty ChannelProperty =
        DependencyProperty.Register("Channel", typeof(int), typeof(UsrCntrlGWPS), new FrameworkPropertyMetadata(1, ChannelChanged) { BindsTwoWayByDefault = true });

    static readonly DependencyPropertyKey VoltageKey = DependencyProperty.RegisterReadOnly(nameof(Voltage), typeof(double), typeof(UsrCntrlGWPS), new PropertyMetadata(double.NaN));
    static readonly DependencyPropertyKey CurrentKey = DependencyProperty.RegisterReadOnly(nameof(Current), typeof(double), typeof(UsrCntrlGWPS), new PropertyMetadata(double.NaN));

    public static readonly DependencyProperty VoltageProperty = VoltageKey.DependencyProperty;
    public static readonly DependencyProperty CurrentProperty = CurrentKey.DependencyProperty;

    static readonly DependencyProperty Voltage_Prop = DependencyProperty.Register(nameof(Voltage_Prop), typeof(double), typeof(UsrCntrlGWPS), new PropertyMetadata(double.NaN, Voltage_PropChanged));
    static readonly DependencyProperty Current_Prop = DependencyProperty.Register(nameof(Current_Prop), typeof(double), typeof(UsrCntrlGWPS), new PropertyMetadata(double.NaN, Current_PropChanged));



    public static readonly DependencyProperty OutputVoltageProperty =
        DependencyProperty.Register(nameof(OutputVoltage), typeof(double), typeof(UsrCntrlGWPS), new FrameworkPropertyMetadata(1.0) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty OutputCurrentProperty =
    DependencyProperty.Register(nameof(OutputCurrent), typeof(double), typeof(UsrCntrlGWPS), new FrameworkPropertyMetadata(1.0e-4) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty AutoPollProperty =
        DependencyProperty.Register(nameof(AutoPoll), typeof(bool), typeof(UsrCntrlGWPS), new FrameworkPropertyMetadata(false, AutoPollChanged) { BindsTwoWayByDefault = true });

    static readonly DependencyPropertyKey IsConnectedKey =
       DependencyProperty.RegisterReadOnly(nameof(IsConnected), typeof(bool), typeof(UsrCntrlGWPS), new PropertyMetadata(false));
    public static readonly DependencyProperty IsConnectedProperty = IsConnectedKey.DependencyProperty;

    static readonly DependencyProperty IsOn_Prop =
      DependencyProperty.Register(nameof(IsOn_Prop), typeof(bool), typeof(UsrCntrlGWPS), new PropertyMetadata(false, IsOn_PropChanged));
    static readonly DependencyProperty State_Prop =
      DependencyProperty.Register(nameof(State_Prop), typeof(SState), typeof(UsrCntrlGWPS), new PropertyMetadata(SState.Disconnected, State_PropChanged));

    static readonly DependencyPropertyKey IsOnKey =
       DependencyProperty.RegisterReadOnly(nameof(IsOn), typeof(bool), typeof(UsrCntrlGWPS), new PropertyMetadata(false));
    static readonly DependencyPropertyKey StateKey =
       DependencyProperty.RegisterReadOnly(nameof(State), typeof(SState), typeof(UsrCntrlGWPS), new PropertyMetadata(SState.Disconnected));
    public static readonly DependencyProperty IsOnProperty = IsOnKey.DependencyProperty;
    public static readonly DependencyProperty StateProperty = StateKey.DependencyProperty;

    static void Voltage_PropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((UsrCntrlGWPS)d).Voltage = (double)e.NewValue;
    static void Current_PropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((UsrCntrlGWPS)d).Current = (double)e.NewValue;
    static void ChannelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((UsrCntrlGWPS)d).GWPS.Channel = (int)e.NewValue;
    static void IsOn_PropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((UsrCntrlGWPS)d).IsOn = (bool)e.NewValue;
    static void State_PropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((UsrCntrlGWPS)d).State = (SState)e.NewValue;
    static void AutoPollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((UsrCntrlGWPS)d).GWPS.IdlePollEnable = (bool)e.NewValue;
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
    public int Channel {
      get { return (int)GetValue(ChannelProperty); }
      set { SetValue(ChannelProperty, value); }
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


    public UsrCntrlGWPS()
    {
      InitializeComponent();

      GWPS = new GWPowerSupply();
      GWPS.ConnectedToDevice += ConnectedToDevice;
      GWPS.DisconnectedFromDevice += KD_DisconnectedFromDevice;

      Logger.Default.AttachLog(nameof(GWPowerSupply), AddToLog, Logger.Mode.NoAutoPoll);

      SetBinding(IsOn_Prop, new Binding("Output") { Source = GWPS, Mode = BindingMode.OneWay });
      SetBinding(State_Prop, new Binding("State") { Source = GWPS, Mode = BindingMode.OneWay });
      SetBinding(Voltage_Prop, new Binding("Voltage") { Source = GWPS, Mode = BindingMode.OneWay });
      SetBinding(Current_Prop, new Binding("Current") { Source = GWPS, Mode = BindingMode.OneWay });
    }

    void AddToLog(object s, Logger.LogFeedBEA e) => tbLog.Text += $">{e.Message}\n";

    void KD_DisconnectedFromDevice(object sender, EventArgs e)
    {
      IsConnected = false;
      circle.Fill = Brushes.LightGray;
 //     circle.ToolTip = GWPS.State.ToString();
    }

    private void ConnectedToDevice(object sender, EventArgs e)
    {
      IsConnected = true;

      circle.Fill = Brushes.LimeGreen;
//      circle.ToolTip = GWPS.State.ToString();
    }

    void bDisconnect_Click(object sender, RoutedEventArgs e) => GWPS.Disconnect();
    void bConnect_Click(object sender, RoutedEventArgs e) => ConnectCommand();

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
      if (!string.IsNullOrEmpty(str) && SelectedPort != str) SelectedPort = str;
    }

    void cbPort_DropDownOpened(object sender, EventArgs e) => UpdateComboBox();

    void bOutput_Click(object sender, RoutedEventArgs e)
    {
      if (GWPS.Output)
        GWPS.ScheduleTurnOff();
      else
        GWPS.ScheduleTurnOn();
    }

    void bSetUp_Click(object s, RoutedEventArgs e) => SetUpCommand();

    public void ConnectCommand() => GWPS.Connect(SelectedPort);
    public void SetUpCommand()
    {
      GWPS.ScheduleSetV(OutputVoltage);
      GWPS.ScheduleSetI(OutputCurrent);
    }

    void TbLog_TextChanged(object sender, TextChangedEventArgs e)
    {
      TextBox tb = (TextBox)sender;
      if (tb.Text.Length > 5000)
        tb.Text = "<Log trimmed>" + tb.Text.Substring(tb.Text.IndexOf('\n', 1000));
      tb.ScrollToEnd();
    }

    void cbChannel_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      title.Text = $"GWI Power Supply (Channel {Channel})";
    }
  }
}
