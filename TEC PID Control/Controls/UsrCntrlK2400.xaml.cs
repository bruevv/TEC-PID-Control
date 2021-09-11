using CSUtils;
using Devices.Keithley;
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

    public static readonly DependencyProperty OutputVoltageProperty =
        DependencyProperty.Register("OutputVoltage", typeof(double), typeof(UsrCntrlK2400), new FrameworkPropertyMetadata(1.0) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty OutputCurrentProperty =
    DependencyProperty.Register("OutputCurrent", typeof(double), typeof(UsrCntrlK2400), new FrameworkPropertyMetadata(1.0e-4) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty AutoPollProperty =
    DependencyProperty.Register("AutoPoll", typeof(bool), typeof(UsrCntrlK2400), new FrameworkPropertyMetadata(false) { BindsTwoWayByDefault = true });

    static readonly DependencyPropertyKey IsConnectedKey =
       DependencyProperty.RegisterReadOnly(nameof(IsConnected), typeof(bool), typeof(UsrCntrlK2400), new PropertyMetadata(false));
    public static readonly DependencyProperty IsConnectedProperty = IsConnectedKey.DependencyProperty;

    static readonly DependencyProperty IsOn_Prop =
      DependencyProperty.Register(nameof(IsOn_Prop), typeof(bool), typeof(UsrCntrlK2400), new PropertyMetadata(false, IsOn_PropChanged));
    static void IsOn_PropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((UsrCntrlK2400)d).IsOn = (bool)e.NewValue;
    static readonly DependencyPropertyKey IsOnKey =
       DependencyProperty.RegisterReadOnly(nameof(IsOn), typeof(bool), typeof(UsrCntrlK2400), new PropertyMetadata(false));
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

    public UsrCntrlK2400()
    {
      InitializeComponent();

      KD = new Keithley2400();
      KD.ConnectedToDevice += KD_ConnectedToDevice;
      KD.DisconnectedFromDevice += KD_DisconnectedFromDevice;

      utbVoltage.DataContext = KD;
      utbCurrent.DataContext = KD;

      Logger.Default.AttachLog(nameof(Keithley2400), (string msg, Logger.Mode lm) =>
                                tbLog.Dispatcher.Invoke(() => tbLog.Text += $">{msg}\n"));

      SetBinding(IsOn_Prop, new Binding("Output") { Source = KD, Mode = BindingMode.OneWay });
      SetBinding(AutoPollProperty, new Binding("IdlePollEnable") { Source = KD, Mode = BindingMode.TwoWay });
    }

    private void KD_DisconnectedFromDevice(object sender, EventArgs e)
    {
      IsConnected = false;
      circle.Fill = Brushes.LightGray;
      circle.ToolTip = KD.State.ToString();
    }

    private void KD_ConnectedToDevice(object sender, EventArgs e)
    {
      IsConnected = true;
      circle.Fill = Brushes.LimeGreen;
      circle.ToolTip = KD.State.ToString();
    }

    void bDisconnect_Click(object sender, RoutedEventArgs e) => KD.Disconnect();
    void bConnect_Click(object sender, RoutedEventArgs e) => KD.Connect(SelectedPort);

    void usrCntrlK2400_Loaded(object sender, RoutedEventArgs e) => UpdateComboBox();
    void UpdateComboBox()
    {
      string oldport = SelectedPort;
      cbPort.Items.Clear();

      foreach(string port in SerialPort.GetPortNames())
        cbPort.Items.Add(port);

      if(!string.IsNullOrEmpty(SelectedPort) && cbPort.Items.Contains(SelectedPort))
        cbPort.SelectedItem = oldport;
    }
    void cbPort_SelectionChanged(object s, EventArgs e)
    {
      string str = cbPort.SelectedItem as string;
      if(!string.IsNullOrEmpty(str)) SelectedPort = str;
    }

    void cbPort_DropDownOpened(object sender, EventArgs e) => UpdateComboBox();

    void bOutput_Click(object sender, RoutedEventArgs e)
    {
      if(KD.Output)
        KD.TurnOFF();
      else
        KD.TurnON();
    }

    void bSetUp_Click(object s, RoutedEventArgs e) => KD.SourceI(utbSetCurrent.Value, Math.Abs(utbSetVoltage.Value));

    async void bMesRes_Click(object sender, RoutedEventArgs e)
    {
      double r = await KD.MeasureR_AW(utbSetCurrent.Value, Math.Abs(utbSetVoltage.Value));
      utbResistance.Value = r;
    }

    void tbLog_TextChanged(object sender, TextChangedEventArgs e)
    {
      TextBox tb = (TextBox)sender;
      if (tb.Text.Length > 5000)
        tb.Text = "<Log trimmed>" + tb.Text.Substring(tb.Text.IndexOf('\n', 1000));
      tb.ScrollToEnd();
    }
  }
}
