using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Devices.Keithley;

namespace TEC_PID_Control.Controls
{
  /// <summary>
  /// Interaction logic for UsrCntrlK2400.xaml
  /// </summary>
  public partial class UsrCntrlK2400 : UserControl
  {
    Keithley2400 KD;

    #region Binding
    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(UsrCntrlK2400), new FrameworkPropertyMetadata(true) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty IsConnectedProperty =
        DependencyProperty.Register(nameof(IsConnected), typeof(bool), typeof(UsrCntrlK2400), new PropertyMetadata(false));
    #endregion Binding

    public bool IsConnected {
      get { return (bool)GetValue(IsConnectedProperty); }
      set { SetValue(IsConnectedProperty, value); }
    }
    [Category("Appearance")]
    public bool IsExpanded {
      get { return (bool)GetValue(IsExpandedProperty); }
      set { SetValue(IsExpandedProperty, value); }
    }

    public string SelectedPort {
      get { return (string)GetValue(SelectedPortProperty); }
      set { SetValue(SelectedPortProperty, value); }
    }

    // Using a DependencyProperty as the backing store for SelectedPort.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty SelectedPortProperty =
        DependencyProperty.Register("SelectedPort", typeof(string), typeof(UsrCntrlK2400), new PropertyMetadata(null));

    public UsrCntrlK2400()
    {
      InitializeComponent();

      KD = new Keithley2400();
      KD.ConnectedToDevice += KD_ConnectedToDevice;
      KD.DisconnectedFromDevice += KD_DisconnectedFromDevice; 

      utbVoltage.DataContext = KD;
      utbCurrent.DataContext = KD;
    }

    private void KD_DisconnectedFromDevice(object sender, EventArgs e)
    {
      IsConnected = false;
      circle.Fill = Brushes.LightGray;
      circle.ToolTip = "Connected";
    }

    private void KD_ConnectedToDevice(object sender, EventArgs e)
    {
      IsConnected = true;
      circle.Fill = Brushes.LimeGreen;
      circle.ToolTip = "Disconnected";
    }

    void bDisconnect_Click(object sender, RoutedEventArgs e) => KD.Disconnect();
    void bConnect_Click(object sender, RoutedEventArgs e) => KD.Connect(SelectedPort);

    void usrCntrlK2400_Loaded(object sender, RoutedEventArgs e) => UpdateComboBox();
    void UpdateComboBox()
    {
      string selected = cbPort.SelectedItem as string;

      cbPort.Items.Clear();

      foreach(string port in SerialPort.GetPortNames())
        cbPort.Items.Add(port);

      if(selected != null && cbPort.Items.Contains(selected))
        cbPort.SelectedItem = selected;
    }
    void cbPort_SelectionChanged(object s, EventArgs e) => SelectedPort = cbPort.SelectedItem as string;

    void cbPort_DropDownOpened(object sender, EventArgs e) => UpdateComboBox();
  }
}
