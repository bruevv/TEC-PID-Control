using CSUtils;
using Devices;
using Devices.GWI;
using System;
using System.ComponentModel;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using TEC_PID_Control.Properties;

namespace TEC_PID_Control.Controls
{
  /// <summary>
  /// Interaction logic for UsrCntrlGWPS.xaml
  /// </summary>
  public partial class UsrCntrlGWPS : UserControl
  {
    public GWPowerSupply GWPS;

    #region DepProps
    public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(nameof(Settings), typeof(GWPSS), typeof(UsrCntrlGWPS), new PropertyMetadata(GWPSS.Default));

    static readonly DependencyPropertyKey VoltageKey = DependencyProperty.RegisterReadOnly(nameof(Voltage), typeof(double), typeof(UsrCntrlGWPS), new PropertyMetadata(double.NaN));
    static readonly DependencyPropertyKey CurrentKey = DependencyProperty.RegisterReadOnly(nameof(Current), typeof(double), typeof(UsrCntrlGWPS), new PropertyMetadata(double.NaN));
    static readonly DependencyPropertyKey IsConnectedKey = DependencyProperty.RegisterReadOnly(nameof(IsConnected), typeof(bool), typeof(UsrCntrlGWPS), new PropertyMetadata(false));
    static readonly DependencyPropertyKey IsControlledKey = DependencyProperty.RegisterReadOnly(nameof(IsControlled), typeof(bool), typeof(UsrCntrlGWPS), new PropertyMetadata(false));
    static readonly DependencyPropertyKey IsOnKey = DependencyProperty.RegisterReadOnly(nameof(IsOn), typeof(bool), typeof(UsrCntrlGWPS), new PropertyMetadata(false));
    static readonly DependencyPropertyKey StateKey = DependencyProperty.RegisterReadOnly(nameof(State), typeof(SState), typeof(UsrCntrlGWPS), new PropertyMetadata(SState.Disconnected));

    public static readonly DependencyProperty VoltageProperty = VoltageKey.DependencyProperty;
    public static readonly DependencyProperty CurrentProperty = CurrentKey.DependencyProperty;
    public static readonly DependencyProperty IsConnectedProperty = IsConnectedKey.DependencyProperty;
    public static readonly DependencyProperty IsControlledProperty = IsControlledKey.DependencyProperty;
    public static readonly DependencyProperty IsOnProperty = IsOnKey.DependencyProperty;
    public static readonly DependencyProperty StateProperty = StateKey.DependencyProperty;

    static readonly DependencyProperty Voltage_Prop = DependencyProperty.Register(nameof(Voltage_Prop), typeof(double), typeof(UsrCntrlGWPS), new PropertyMetadata(double.NaN, Voltage_PropChanged));
    static readonly DependencyProperty Current_Prop = DependencyProperty.Register(nameof(Current_Prop), typeof(double), typeof(UsrCntrlGWPS), new PropertyMetadata(double.NaN, Current_PropChanged));
    static readonly DependencyProperty IsOn_Prop = DependencyProperty.Register(nameof(IsOn_Prop), typeof(bool), typeof(UsrCntrlGWPS), new PropertyMetadata(false, IsOn_PropChanged));
    static readonly DependencyProperty State_Prop = DependencyProperty.Register(nameof(State_Prop), typeof(SState), typeof(UsrCntrlGWPS), new PropertyMetadata(SState.Disconnected, State_PropChanged));
    #endregion DepProps
    #region DepPropsP
    [Category("Common")]
    public GWPSS Settings { get => (GWPSS)GetValue(SettingsProperty); set => SetValue(SettingsProperty, value); }

    [Category("Common")]
    public double Voltage {
      get { return (double)GetValue(VoltageProperty); }
      protected set { SetValue(VoltageKey, value); }
    }
    [Category("Common")]
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
    public bool IsControlled {
      get { return (bool)GetValue(IsControlledProperty); }
      protected set { SetValue(IsControlledKey, value); }
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
    #endregion DepPropsP
    #region DepPropsCB
    static void Voltage_PropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((UsrCntrlGWPS)d).Voltage = (double)e.NewValue;
    static void Current_PropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((UsrCntrlGWPS)d).Current = (double)e.NewValue;


    static void IsOn_PropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((UsrCntrlGWPS)d).IsOn = (bool)e.NewValue;
    static void State_PropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      UsrCntrlGWPS d1 = ((UsrCntrlGWPS)d);
      if ((d1.State & SState.Controlled) != (((SState)e.NewValue) & SState.Controlled))
        d1.IsControlled = ((SState)e.NewValue).HasFlag(SState.Controlled);
      d1.State = (SState)e.NewValue;
    }
    #endregion DepPropsCB

    string attachedLogName = null;
    public UsrCntrlGWPS()
    {
      GWPS = new GWPowerSupply();

      if (attachedLogName == null) {
        Logger.Default.AttachLog(GWPS.DeviceName, AddToLog, Logger.Mode.NoAutoPoll);
        attachedLogName = GWPS.DeviceName;
      }

      InitializeComponent();

      GWPS.ConnectedToDevice += ConnectedToDevice;
      GWPS.DisconnectedFromDevice += KD_DisconnectedFromDevice;



      title.Text = GWPS.DeviceName;

      SetBinding(IsOn_Prop, new Binding("Output") { Source = GWPS, Mode = BindingMode.OneWay });
      SetBinding(State_Prop, new Binding("State") { Source = GWPS, Mode = BindingMode.OneWay });
      SetBinding(Voltage_Prop, new Binding("Voltage") { Source = GWPS, Mode = BindingMode.OneWay });
      SetBinding(Current_Prop, new Binding("Current") { Source = GWPS, Mode = BindingMode.OneWay });
    }
    public new string Name => title?.Text ?? "";

    void AddToLog(object s, Logger.LogFeedBEA e) => tbLog.Text += $">{e.Message}\n";

    void KD_DisconnectedFromDevice(object sender, EventArgs e) => IsConnected = false;
    void ConnectedToDevice(object sender, EventArgs e) => IsConnected = true;

    void bDisconnect_Click(object sender, RoutedEventArgs e) => GWPS.Disconnect();
    void bConnect_Click(object sender, RoutedEventArgs e) => ConnectCommand();

    void UsrCntrlGWPS_Loaded(object sender, RoutedEventArgs e)
    {
      UpdateComboBox();

      if (Settings.AutoConnect && !string.IsNullOrEmpty(Settings.Port)) {
        try {
          Logger.Default?.log($"Trying to automatically connect to GWI PS at port:" +
            $"{Settings.Port}", Logger.Mode.NoAutoPoll, nameof(UsrCntrlGWPS));
          ConnectCommand();
        } catch (Exception ex) {
          Logger.Default?.log("Error trying to automatically connect to GWI PS",
            ex, Logger.Mode.Error, nameof(UsrCntrlGWPS));
        }
      }

    }

    void UpdateComboBox()
    {
      string oldport = Settings.Port;
      cbPort.Items.Clear();

      foreach (string port in SerialPort.GetPortNames())
        cbPort.Items.Add(port);

      if (!string.IsNullOrEmpty(oldport) && cbPort.Items.Contains(oldport))
        Settings.Port = oldport;
      else Settings.Port = "";
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

    public void ConnectCommand() => GWPS.Connect(Settings.Port);
    public void SetUpCommand()
    {
      GWPS.ScheduleSetV(Settings.OutputVoltage);
      GWPS.ScheduleSetI(Settings.OutputCurrent);
    }

    void TbLog_TextChanged(object sender, TextChangedEventArgs e)
    {
      TextBox tb = (TextBox)sender;
      if (tb.Text.Length > 5000)
        tb.Text = "<Log trimmed>" + tb.Text.Substring(tb.Text.IndexOf('\n', 1000));
      tb.ScrollToEnd();
    }

    void Channel_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      GWPS.Channel = (int)Settings.Channel;
      title.Text = GWPS.DeviceName;

      if (attachedLogName != null) Logger.Default.DetachLog(attachedLogName, AddToLog);

      Logger.Default.AttachLog(GWPS.DeviceName, AddToLog, Logger.Mode.NoAutoPoll);
      attachedLogName = GWPS.DeviceName;
    }

    void AutoPoll_CheckedUnchecked(object o, RoutedEventArgs e) => GWPS.IdlePollEnable = Settings.AutoPoll;
  }
}
