using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SerialPorting;
using ThreadQueuing;

namespace Devices.Keithley
{
  using SG = System.Globalization;

  public enum CK2400
  {
    Fetch = 0x11,
    MeasureI = 0x20, // Read Current
    MeasureV = 0x21, // Read Voltage
    Output = 0x22, // Turn On/Off
    SetSourceV = 0x24, //
    SetSourceI = 0x25, //
    SourceV = 0x26, //
    SourceI = 0x27, //
    ILim = 0x28, //
    VLim = 0x29, //
    SetMeasureV,
    SetMeasureI,
    SetMeasureC,
    Read,
    RangeVO,
    RangeIO,
    RangeVM,
    RangeIM,
    AutoRangeV,
    AutoRangeI,
    Local,
  };

  class KeithleyDevCon : UARTConnection<string>
  {
    protected override string InitString => "KEITHLEY INSTRUMENTS INC.,";
    protected override IDictionary<Enum, string> Coms => coms;

    protected static readonly Dictionary<Enum, string> coms = new Dictionary<Enum, string>() {
            {CCmd.Init,         "*IDN?"},
            {CCmd.Reset,        "*RST"},
            {CCmd.Custom,       ""},
        };

    public KeithleyDevCon(WaitHandle abortWaitHandle, int idlewait = 30, string name = "noname") : base(abortWaitHandle, idlewait: 30, name) { }
    protected override int BaudRate => 9600;
    public override int BasicTimeout => 1000;
    protected override string ACK => null;
    public override bool AddQuestionMark => true;

    //    void 
  }
  class Keithley2400Con : KeithleyDevCon
  {
    protected override IDictionary<Enum, string> Coms => coms;
    // this dictionary is appended in static constructor
    static new readonly Dictionary<Enum, string> coms = new Dictionary<Enum, string>() {
            {CK2400.Fetch,      "FETC"},
            {CK2400.SetMeasureV,"FORM:ELEM VOLT" },
            {CK2400.SetMeasureI,"FORM:ELEM CURR" },
            {CK2400.SetMeasureC,"FORM:ELEM VOLT,CURR,TIME,STAT\n" +
                                "FUNC 'CURR','VOLT'" },
            {CK2400.Read,       "READ" },
            {CK2400.MeasureI,   "MEAS:CURR"},
            {CK2400.MeasureV,   "MEAS:VOLT"},
            {CK2400.SetSourceV, "SOUR:FUNC VOLT" },
            {CK2400.SetSourceI, "SOUR:FUNC CURR" },
            {CK2400.SourceV,    "SOUR:VOLT" },
            {CK2400.SourceI,    "SOUR:CURR" },
            {CK2400.ILim,       "CURR:PROT" },
            {CK2400.VLim,       "VOLT:PROT" },
            {CK2400.RangeIO,     "SOUR:CURR:RANG" },
            {CK2400.RangeVO,     "SOUR:VOLT:RANG" },
            {CK2400.RangeIM,     "SENS:CURR:RANG" },
            {CK2400.RangeVM,     "SENS:VOLT:RANG" },
            {CK2400.AutoRangeI, "CURR:RANG:AUTO" },
            {CK2400.AutoRangeV, "VOLT:RANG:AUTO" },
            {CK2400.Output,     "OUTP" },
            {CK2400.Local,      "SYST:KEY 23" },
       };

    public enum Mode
    {
      CURR,
      VOLT,
      BOTH,
      unknown,
    }
    // public override bool AddQuestionMark => true;
    static Keithley2400Con()
    {
      foreach (var keyValuePair in KeithleyDevCon.coms)
        coms.Add(keyValuePair.Key, keyValuePair.Value);
    }
    public Keithley2400Con(WaitHandle abortWaitHandle, string name) : base(abortWaitHandle, name: name) { }
    internal override void PreDisconnectCommand()
    {
      Command(CK2400.Output, "0");
      Command(CK2400.Local);
    }
  }
  public class KeithleyDevice : ASCIIDevice
  {
    new private protected KeithleyDevCon iCI => (KeithleyDevCon)base.iCI;
    private protected override ConnectionBase InitSPI(string name) => new KeithleyDevCon(EventAbort, name: name);

    public KeithleyDevice(string name = "noname") : base(name) { }

    void CustomCommand_AS(string command)
    {
      string res = "";
      try
      {
        string cmd = command.Trim();
        if (cmd.EndsWith('?'))
          res = iCI.Request(CCmd.Custom, cmd);
        else
          iCI.Command(CCmd.Custom, command);
      }
      catch (Exception e)
      {
        ChangeStatus($"Error in CustomCommand\n{e.Message}",
                  State | SState.Error);
        return;
      }
      if (res == "") ChangeStatus($"Command <{command}>", State & ~SState.Error);
      else ChangeStatus($"Command <{command}>\n{res}", State & ~SState.Error);
    }

    public void CustomCommand(string command)
    {
      if (!IsConnected) return;
      iCI.TQ.EnqueueUnique(CustomCommand_AS, command);
    }
  }
  public class Keithley2400 : KeithleyDevice, INotifyPropertyChanged
  {
    new private protected Keithley2400Con iCI => (Keithley2400Con)base.iCI;

    public event PropertyChangedEventHandler PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string property = "") =>
     PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

    double voltage = double.NaN;
    double current = double.NaN;
    double time = 0;
    int status = 0;
    public double Voltage
    {
      get => voltage;
      protected set
      {
        if (voltage != value)
        {
          voltage = value;
          OnPropertyChanged();
        }
      }
    }
    public double Current
    {
      get => current;
      protected set
      {
        if (current != value)
        {
          current = value;
          OnPropertyChanged();
        }
      }
    }
    public double Time
    {
      get => time;
      protected set
      {
        if (time != value)
        {
          time = value;
          OnPropertyChanged();
        }
      }
    }
    public int Status
    {
      get => status;
      protected set
      {
        if (status != value)
        {
          status = value;
          OnPropertyChanged();
        }
      }
    }
    bool output;
    public bool Output
    {
      get => output;
      protected set
      {
        if (output != value)
        {
          output = value;
          OnPropertyChanged();
        }
      }
    }
    public bool AutoRange { get; set; } = true;

    bool idlePollEnable;
    public bool IdlePollEnable {
      get => idlePollEnable;
      set {
        if (idlePollEnable != value) {
          idlePollEnable = value;
          OnPropertyChanged();
        }
      }
    }

    Keithley2400Con.Mode SourceMode = Keithley2400Con.Mode.unknown;
    Keithley2400Con.Mode MeasureMode = Keithley2400Con.Mode.unknown;

    private protected override Keithley2400Con InitSPI(string name) => new Keithley2400Con(EventAbort, name);
    public Keithley2400(string name = "noname") : base(name)
    {
      iCI.IdleTimeout += OnIdleTimeout;
    }
    void OnIdleTimeout(object sender, EventArgs ea)
    {
      if (!IdlePollEnable || !Output) return;

      MeasureConc_AS();
    }

    void Reset_AS()
    {
      try
      {
        iCI.Command(CCmd.Reset);
        SourceMode = Keithley2400Con.Mode.VOLT;
        MeasureMode = Keithley2400Con.Mode.unknown;
      }
      catch (Exception e)
      {
        ChangeStatus($"Error in Reset\n{e.Message}",
                  State | SState.Error);
        goto finish;
      }
      ChangeStatus($"K2400 Reset to Default state", State & ~SState.Error);
      Output = false;

    finish:
      return;
    }
    void MeasureI_AS() => MeasureI_AS(null);
    void MeasureI_AS(EventWaitHandle ewh)
    {
      string res;
      try
      {
        SetupMeasureI();

        res = iCI.Request(CK2400.MeasureI);
      }
      catch (Exception e)
      {
        ChangeStatus($"Error in MeasureI\n{e.Message}",
                  State | SState.Error);
        goto finish;
      }
      if (double.TryParse(res, SG.NumberStyles.Float, SG.CultureInfo.InvariantCulture, out double val))
      {
        Current = val;
        ChangeStatus($"Measure Current:{Current}A", State & ~SState.Error);
      }
      else
      {
        ChangeStatus($"Measure Current Parse Failed", State & ~SState.Error);
      }
      Output = true;

    finish:
      ewh?.Set();
    }

    void MeasureV_AS() => MeasureV_AS(null);
    void MeasureV_AS(EventWaitHandle ewh)
    {
      string res;
      try
      {
        SetupMeasureV();
        res = iCI.Request(CK2400.MeasureV);
      }
      catch (Exception e)
      {
        ChangeStatus($"Error in MeasureV\n{e.Message}",
                  State | SState.Error);
        goto finish;
      }
      if (double.TryParse(res, SG.NumberStyles.Float, SG.CultureInfo.InvariantCulture, out double val))
      {
        Voltage = val;
        ChangeStatus($"Measure Voltage:{Voltage}V", State & ~SState.Error);
      }
      else
      {
        ChangeStatus($"Measure Voltage Parse Failed", State & ~SState.Error);
      }
      Output = true;

    finish:
      ewh?.Set();
    }

    void TurnOutput_AS(bool b) => TurnOutput_AS(b, null);
    void TurnOutput_AS(bool b, EventWaitHandle ewh)
    {
      try
      {
        iCI.Command(CK2400.Output, b ? "1" : "0");
      }
      catch (Exception e)
      {
        ChangeStatus($"Error in TurnOutput\n{e.Message}",
                  State | SState.Error);
        goto finish;
      }

      ChangeStatus($"Output Set:{(b ? "ON" : "OFF")}", State & ~SState.Error);
      Output = b;

    finish:
      ewh?.Set();
    }

    void MeasureConc_AS() => MeasureConc_AS(null);
    void MeasureConc_AS(EventWaitHandle ewh)
    {
      string res;
      try
      {
        SetupMeasureConcurent();

        iCI.Command(CK2400.Output, "1");
        res = iCI.Request(CK2400.Read);
      }
      catch (Exception e)
      {
        ChangeStatus($"Error in MeasureI\n{e.Message}",
                  State | SState.Error);
        goto finish;
      }
      string[] spl = res.Split(',');
      try
      {
        Voltage = double.Parse(spl[0], SG.NumberStyles.Float, SG.CultureInfo.InvariantCulture);
        Current = double.Parse(spl[1], SG.NumberStyles.Float, SG.CultureInfo.InvariantCulture);
        Time = double.Parse(spl[2], SG.NumberStyles.Float, SG.CultureInfo.InvariantCulture);
        Status = (int)(double.Parse(spl[3], SG.NumberStyles.Float, SG.CultureInfo.InvariantCulture) + 0.5);
        ChangeStatus($"Measure:{Voltage}V,{Current}A", State & ~SState.Error);
      }
      catch (Exception e)
      {
        ChangeStatus($"Measure Parse Failed\n{e.Message}", State & ~SState.Error);
      }
      Output = true;

    finish:
      ewh?.Set();
    }

    void SetupMeasureI()
    {
      if (MeasureMode != Keithley2400Con.Mode.CURR)
      {
        iCI.Command(CK2400.SetMeasureI);
        MeasureMode = Keithley2400Con.Mode.CURR;
      }
    }
    void SetupMeasureV()
    {
      if (MeasureMode != Keithley2400Con.Mode.VOLT)
      {
        iCI.Command(CK2400.SetMeasureV);
        MeasureMode = Keithley2400Con.Mode.VOLT;
      }
    }
    void SetupMeasureConcurent()
    {
      if (MeasureMode != Keithley2400Con.Mode.BOTH)
      {
        iCI.Command(CK2400.SetMeasureC);
        MeasureMode = Keithley2400Con.Mode.BOTH;
      }
    }

    void SourceV_AS(double V, double ILim)
    {
      try
      {
        SetupSourceV(ILim);
        iCI.Command(CK2400.RangeVO, V.ToString("G6"));
        iCI.Command(CK2400.SourceV, V.ToString("G6"));
      }
      catch (Exception e)
      {
        ChangeStatus($"Error in SourceV\n{e.Message}",
                  State | SState.Error);
        return;
      }

      ChangeStatus($"Set Voltage: {V:G6}", State & ~SState.Error);
    }
    void SourceI_AS(double I, double VLim)
    {
      try
      {
        SetupSourceI(VLim);
        iCI.Command(CK2400.RangeIO, I.ToString("G6"));
        //   iCI.Command(CK2400.AutoRangeI, AutoRange ? "1" : "0");
        iCI.Command(CK2400.SourceI, I.ToString("G6"));
      }
      catch (Exception e)
      {
        ChangeStatus($"Error in SourceI\n{e.Message}",
                  State | SState.Error);
        return;
      }

      ChangeStatus($"Set Current: {I:G6}", State & ~SState.Error);

    }

    void SetupSourceI(double VLim)
    {
      if (SourceMode != Keithley2400Con.Mode.CURR)
      {
        iCI.Command(CK2400.SetSourceI);
        SourceMode = Keithley2400Con.Mode.CURR;
      }
      if (!double.IsNaN(VLim) && VLim != 0)
      {
        iCI.Command(CK2400.VLim, VLim.ToString("G6"));
        iCI.Command(CK2400.RangeVM, VLim.ToString("G6"));
        iCI.Command(CK2400.AutoRangeV, AutoRange ? "1" : "0");
      }
    }
    void SetupSourceV(double ILim)
    {
      if (SourceMode != Keithley2400Con.Mode.VOLT)
      {
        iCI.Command(CK2400.SetSourceV);
        SourceMode = Keithley2400Con.Mode.VOLT;
      }
      if (!double.IsNaN(ILim) && ILim != 0)
      {
        iCI.Command(CK2400.ILim, ILim.ToString("G6"));
        iCI.Command(CK2400.RangeIM, ILim.ToString("G6"));
        iCI.Command(CK2400.AutoRangeI, AutoRange ? "1" : "0");
      }
    }

    public void MeasureI()
    {
      if (!IsConnected) return;
      iCI.TQ.EnqueueUnique(MeasureI_AS);
    }
    public void MeasureV()
    {
      if (!IsConnected) return;
      iCI.TQ.EnqueueUnique(MeasureV_AS);
    }
    public void Measure()
    {
      if (!IsConnected) return;
      iCI.TQ.Enqueue(MeasureConc_AS);
    }
    public async Task<double> MeasureI_AW()
    {
      if (!IsConnected) return double.NaN;
      var ewh = EventWaitHandlePool.GetHandle();
      iCI.TQ.Enqueue(MeasureI_AS, ewh);
      _ = await Task.Run(() => ewh.WaitOne());
      EventWaitHandlePool.ReturnHandle(ewh);
      return Current;
    }
    public async Task<double> MeasureV_AW()
    {
      if (!IsConnected) return double.NaN;
      var ewh = EventWaitHandlePool.GetHandle();
      iCI.TQ.Enqueue(MeasureV_AS, ewh);
      _ = await Task.Run(() => ewh.WaitOne());
      EventWaitHandlePool.ReturnHandle(ewh);
      return Voltage;
    }
    public async Task<double> MeasureR_AW(double I, double VLim = 2.0/*V*/)
    {
      if (!IsConnected) return double.NaN;
      SourceI(I, VLim);
      var ewh = EventWaitHandlePool.GetHandle();
      iCI.TQ.Enqueue(MeasureConc_AS, ewh);
      _ = await Task.Run(() => ewh.WaitOne());
      EventWaitHandlePool.ReturnHandle(ewh);
      return Voltage / Current;
    }
    public async Task<(double Voltage, double Current, double Time, int Status)> Measure_AW()
    {
      if (!IsConnected) return (double.NaN, double.NaN, 0, 0);
      var ewh = EventWaitHandlePool.GetHandle();
      iCI.TQ.Enqueue(MeasureConc_AS, ewh);
      _ = await Task.Run(() => ewh.WaitOne());
      EventWaitHandlePool.ReturnHandle(ewh);
      return (Voltage, Current, Time, Status);
    }

    public void Reset()
    {
      if (!IsConnected) return;
      iCI.TQ.Enqueue(Reset_AS);
    }
    public void SourceV(double V, double ILim)
    {
      if (!IsConnected) return;
      iCI.TQ.Enqueue(SourceV_AS, V, ILim);
    }
    public void SourceI(double I, double VLim)
    {
      if (!IsConnected) return;
      iCI.TQ.Enqueue(SourceI_AS, I, VLim);
    }

    public void TurnOFF()
    {
      if (!IsConnected) return;
      iCI.TQ.Enqueue(TurnOutput_AS, false);
    }
    public void TurnON()
    {
      if (!IsConnected) return;
      iCI.TQ.Enqueue(TurnOutput_AS, true);
    }
    public async Task TurnOFF_AW()
    {
      if (!IsConnected) return;
      var ewh = EventWaitHandlePool.GetHandle();
      iCI.TQ.Enqueue(TurnOutput_AS, false, ewh);
      _ = await Task.Run(() => ewh.WaitOne());
      EventWaitHandlePool.ReturnHandle(ewh);
    }
    public async Task TurnOn_AW()
    {
      if (!IsConnected) return;
      var ewh = EventWaitHandlePool.GetHandle();
      iCI.TQ.Enqueue(TurnOutput_AS, true, ewh);
      _ = await Task.Run(() => ewh.WaitOne());
      EventWaitHandlePool.ReturnHandle(ewh);
    }
  }
}