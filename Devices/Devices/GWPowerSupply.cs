using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SerialPorting;
using ThreadQueuing;

namespace Devices.GWI
{
  using SG = System.Globalization;
  public enum GPD
  {
    ISet = 0x11,
    VSet,
    IOut,
    VOut,
    Output,
    Status
  };

  class GWPowerSupplyConnection : UARTConnection<string>
  {
    protected override string InitString => "GW INSTEK,GPD-X3303";
    protected override IDictionary<Enum, string> Coms => coms;
    protected static readonly Dictionary<Enum, string> coms = new Dictionary<Enum, string>() {
            {CCmd.Init,         "*IDN?"},
            {CCmd.Custom,       ""},
            {GPD.ISet,          "ISET<X>"},
            {GPD.VSet,          "VSET<X>"},
            {GPD.IOut,          "IOUT<X>"},
            {GPD.VOut,          "VOUT<X>"},
            {GPD.Output,        "OUT"},
            {GPD.Status,        "STATUS"},
        };
    public GWPowerSupplyConnection(WaitHandle abortWaitHandle) : base(abortWaitHandle, idlewait: 30) { }
    protected override int BaudRate => 57600;
    public string ChannelNumber = "1";
    protected override string GenCommand(Enum c) => Coms[c].Replace("<X>", ChannelNumber);
    protected override string ArgSeparator => ":";
    public override int BasicTimeout => 1000;
    protected override string ACK => null;
  }
  public class GWPowerSupply : ASCIIDevice, INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string property = "") =>
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

    new private protected GWPowerSupplyConnection iCI => (GWPowerSupplyConnection)base.iCI;
    private protected override ConnectionBase InitSPI() => new GWPowerSupplyConnection(EventAbort);

    int channel = 0;
    double voltage = double.NaN;
    double current = double.NaN;
    public int Channel {
      get => channel;
      set {
        if(channel != value) {
          if(value > 2 || value < 1)
            throw new ArgumentException("Only Channels 1 or 2 available");
          channel = value;
          iCI.ChannelNumber = channel.ToString();
          OnPropertyChanged();
          Voltage = double.NaN;
          Current = double.NaN;
        }
      }
    }
    public double Voltage {
      get => voltage;
      protected set {
        if(voltage != value) {
          voltage = value;
          OnPropertyChanged();
        }
      }
    }
    public double Current {
      get => current;
      protected set {
        if(current != value) {
          current = value;
          OnPropertyChanged();
        }
      }
    }
    void SetV_AS(double V)
    {
      try {
        iCI.Command(GPD.VSet, V.ToString("N3"));
      } catch(Exception e) {
        ChangeStatus($"Error in SetV\n{e.Message}",
                  DState | DevState.Error);
        return;
      }

      ChangeStatus($"Set Voltage: {V:N3}", DState & ~DevState.Error);
    }
    void SetI_AS(double I)
    {
      try {
        iCI.Command(GPD.ISet, I.ToString("N3"));
      } catch(Exception e) {
        ChangeStatus($"Error in SetI\n{e.Message}",
                  DState | DevState.Error);
        return;
      }

      ChangeStatus($"Set Current: {I:N3}", DState & ~DevState.Error);
    }
    void Out_AS(bool O)
    {
      try {
        iCI.Command(GPD.Output, O ? "1" : "0");
      } catch(Exception e) {
        ChangeStatus($"Error in Out\n{e.Message}",
                  DState | DevState.Error);
        return;
      }

      ChangeStatus($"Set Output: {(O ? "ON" : "OFF")}", DState & ~DevState.Error);
    }

    void GetI_AS() => GetI_AS(null);
    void GetI_AS(EventWaitHandle ewh)
    {
      string res;
      try {
        res = iCI.Request(GPD.IOut);
      } catch(Exception e) {
        ChangeStatus($"Error in GetI\n{e.Message}",
                  DState | DevState.Error);
        goto finish;
      }

      if(double.TryParse(res, SG.NumberStyles.Float, SG.CultureInfo.InvariantCulture, out double val)) {
        Current = val;
        ChangeStatus($"Measure Current:{Current}A", DState & ~DevState.Error);
      } else {
        ChangeStatus($"Measure Current Parse Failed", DState & ~DevState.Error);
      }

      finish:
      ewh?.Set();
    }
    void GetV_AS() => GetV_AS(null);
    void GetV_AS(EventWaitHandle ewh)
    {
      string res;
      try {
        res = iCI.Request(GPD.VOut);
      } catch(Exception e) {
        ChangeStatus($"Error in GetV\n{e.Message}", DState | DevState.Error);
        goto finish;
      }

      if(double.TryParse(res,
                         SG.NumberStyles.Float,
                         SG.CultureInfo.InvariantCulture,
                         out double val)) {
        Voltage = val;
        ChangeStatus($"Measure Voltage:{Voltage}V", DState & ~DevState.Error);
      } else {
        ChangeStatus($"Measure Voltage Parse Failed", DState & ~DevState.Error);
      }

      finish:
      ewh?.Set();
    }
    public void SetV(double V)
    {
      if(!IsConnected) return;
      iCI.TQ.Enqueue(SetV_AS, V);
    }
    public void SetI(double I)
    {
      if(!IsConnected) return;
      iCI.TQ.Enqueue(SetI_AS, I);
    }
    public void TurnOn()
    {
      if(!IsConnected) return;
      iCI.TQ.Enqueue(Out_AS, true);
    }
    public void TurnOff()
    {
      if(!IsConnected) return;
      iCI.TQ.Enqueue(Out_AS, false);
    }

    public void UpdateI()
    {
      if(!IsConnected) return;
      iCI.TQ.Enqueue(GetI_AS);
    }
    public void UpdateV()
    {
      if(!IsConnected) return;
      iCI.TQ.Enqueue(GetV_AS);
    }

    public async Task<double> GetI_AW()
    {
      if(!IsConnected) return double.NaN;
      EventWaitHandle ewh = EventWaitHandlePool.GetHandle();
      iCI.TQ.Enqueue(GetI_AS, ewh);
      _ = await Task.Run(() => ewh.WaitOne());
      EventWaitHandlePool.ReturnHandle(ewh);
      return Current;
    }
    public async Task<double> GetV_AW()
    {
      if(!IsConnected) return double.NaN;
      EventWaitHandle ewh = EventWaitHandlePool.GetHandle();
      iCI.TQ.Enqueue(GetV_AS, ewh);
      _ = await Task.Run(() => ewh.WaitOne());
      EventWaitHandlePool.ReturnHandle(ewh);
      return Voltage;
    }
  }
}
