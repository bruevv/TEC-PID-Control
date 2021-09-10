using SerialPorting;
using UCCommands;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using ThreadQueuing;
using System.Diagnostics;

namespace Devices.StepMotoring
{
  using static CSUtils.GUtils;
  using static Math;
  using static Invoke;
  using System.Threading;
  using CSUtils;

  public class SMStatusChangedEventArgs : EventArgs
  {
    internal SMStatusChangedEventArgs(string str, SMState state, int? pos)
    {
      StatusString = str;
      State = state;
      Position = pos;
    }
    public string StatusString { get; }
    public SMState State { get; }
    public int? Position { get; }
  }
  internal enum SMCmd
  {
    Goto = 0x10,
    Stop = 0x11,
    SetParams = 0x12,
    GetStatus = 0x13,
    SetPosition = 0x14,
    Power = 0x15
  };
  class USBDKSP : UARTConnection<string>
  {

    protected override string Address => "M";
    protected override string InitString => "*Initialized*";
    protected override Dictionary<Enum, string> Coms => coms;

    static readonly Dictionary<Enum, string> coms = new Dictionary<Enum, string>() {
            {CCmd.Init,         "*INI"},
            {CCmd.Reset,        "*RES"},

            {SMCmd.Goto,        "SM:GOTO"},
            {SMCmd.Stop,        "SM:STOP"},
            {SMCmd.SetParams,   "SM:SPRM"},
            {SMCmd.GetStatus,   "SM:GSTA"},
            {SMCmd.SetPosition, "SM:SPOS"},
            {SMCmd.Power,       "SM:POWR"},
        };

    public USBDKSP(WaitHandle abortWaitHandle) : base(abortWaitHandle, idlewait: 30) { }
  }

  public enum SMDir : byte { Left = 0, Right = 0x01 }
  public enum SMLSONs : byte {[Display(Name = "Short cirquit")] Short = 0, Open = 0x02 }
  public enum SMLSLSDirs : byte { Direct = 0, Inverted = 0x04 }
  public enum SMStopPower : byte { Full = 0, Saver = 0x08, Off = 0x10 }
  public enum SMStepDivs : byte { Full = 0x40, uStep8 = 0x20, uStep64 = 0 }

  [Flags]
  public enum SMState
  {
    Disconnected = 0x01,
    Connected = 0x02,
    Busy = 0x04,
    Moving = 0x86,
    Connecting = 0x05,
    Error = 0x08,
    LeftLimitSwitch = 0x10,
    RightLimitSwitch = 0x20,
    PositionSet = 0x40,
    Power = 0x80,
    Disconnecting = 0x101
  }

  class StepMotor : IDisposable
  {
//    Properties.SM settings = Properties.SM.Default;
    USBDKSP sPI;

    public SMState State { get; private set; } = SMState.Disconnected;
    public string statusStr { get; private set; } = null;

    public bool IsMoving => (State & SMState.Moving) == SMState.Moving;
    public bool IsPowered => (State & SMState.Power) == SMState.Power;
    public bool IsPositionSet => (State & SMState.PositionSet) == SMState.PositionSet;
    public bool IsConnected => (State & SMState.Connected) == SMState.Connected;

    bool started = false;

    public static string[] COM_Ports => UARTConnectionBase.GetPortNames();

    void ChangeStatus(SMState state, int? pos = null) => ChangeStatus(null, state, pos);
    void ChangeStatus(string str, SMState state, int? pos = null)
    {
      State = state;
      statusStr = str; // was str ?? statusStr;
      position = pos ?? position;

      InContextInvoke(this, StatusChanged,
          new SMStatusChangedEventArgs(str, state, pos));
#if DEBUG
      if(str!=null)
        Debug.WriteLine(str, "IO");
#endif
    }

    public event EventHandler<SMStatusChangedEventArgs> StatusChanged;
    public event EventHandler SMStopped;

    static List<StepMotor> instances = new();
    public static IList<StepMotor> Instances = instances.AsReadOnly();

    public static StepMotor Instance { get; } = instances.Count == 0 ? new StepMotor() : instances[0];

    public StepMotor(int lastCurPos = 0)
    {
      LastCurPos = lastCurPos;

      sPI = new USBDKSP(null);

      sPI.Disconnected += DisconnectedEvent;
      sPI.Connected += OnConnectedToCOM;

      sPI.Idle += OnSPIdle;

      TimeoutTimer = new Timer(TimerTimeOut, EventTimeout, Timeout.Infinite, Timeout.Infinite);

      Instances.Add(this);
    }
    ~StepMotor() => Dispose(); 

    bool SPIdle;

    void OnSPIdle(object o, EventArgs e)
    {
      if(IsConnected) {
        if(SPIdle) {
          sPI.TQ.Enqueue(GetState_AS);
          SPIdle = false;
        } else {
          SPIdle = true;
        }
      }
    }

    public event EventHandler ConnectedToCOM;

    void OnConnectedToCOM(object o, EventArgs e)
      => InContextInvoke(this, ConnectedToCOM, e);

    void Connect_AS(string port)
    {
      ChangeStatus($"Trying to Connect to SMC\nPort:{port}", SMState.Connecting);
      string initS = "";
      try {
        sPI.Connect(port);
        initS = sPI.Iitialize();
      } catch(Exception e) {
        sPI.Disconnect();
        ChangeStatus($"Error connecting to SMC\n{e.Message}", SMState.Disconnected);
        return;
      }

      ChangeStatus($"Connected succsefully. Init String:\n{initS}", SMState.Connected);

      SetParams();
      if(SMStopMode != SMStopPower.Off)
        Power(true);
    }
    void Disconnect_AS()
    {
      State = SMState.Disconnecting;
      try {
        sPI.Reset();
      } catch { }

      try {
        sPI.Disconnect();
      } catch(Exception e) {
        ChangeStatus($"Error disconnecting\n{e.Message}", SMState.Disconnected);
        return;
      }
      ChangeStatus("Disconnected\nController reset", SMState.Disconnected);
    }

    void DisconnectedEvent(object o, EventArgs e)
    {
      if((State & SMState.Connected) == SMState.Connected)
        ChangeStatus("Disconnected", SMState.Disconnected);
      else
        ChangeStatus(SMState.Disconnected);
    }

    int position = 0;
    public int StepPosition => position;
    public double Position => Callibration.Transform(position);

    void Goto_AS(int dest)
    {
      string arg = dest.ToString("X8");

      try {
        started = true;

        sPI.Command(SMCmd.Goto, arg);
      } catch(Exception e) {
        ChangeStatus($"Error in Goto\n{e.Message}",
               State | SMState.Error);
        return;
      }
      ChangeStatus($"Goto {arg}", State & ~SMState.Error);
    }
    void Stop_AS()
    {
      try {
        sPI.Command(SMCmd.Stop);
      } catch(Exception e) {
        ChangeStatus($"Error in Stop\n{e.Message}",
                  State | SMState.Error);
        return;
      }
      ChangeStatus("Stopped", State & ~SMState.Error);
    }
    void SetParams_AS()
    {
      string prms = $"{highSpeed:X4}:{lowSpeed:X4}:{setBack:X4}:{(byte)Params:X2}";

      try {
        sPI.Command(SMCmd.SetParams, prms);
      } catch(Exception e) {
        ChangeStatus($"Error in SetParams\n{e.Message}",
            State | SMState.Error);
        return;
      }
      ChangeStatus($"Parameters Set:\n{prms}", State & ~SMState.Error);
    }

    void GetState_AS()
    {
      string str;
      SMState s;
      int pos;

      try {
        str = sPI.Request(SMCmd.GetStatus);
        string[] prms = str.Split(':');
        pos = int.Parse(prms[0], NumberStyles.HexNumber);
        s = (SMState)byte.Parse(prms[1], NumberStyles.HexNumber);
      } catch(Exception e) {
        ChangeStatus($"Error in GetState\n{e.Message}",
                    State | SMState.Error);
        return;
      }


      s &= GetStateMask;
      if((s & (SMState.LeftLimitSwitch | SMState.RightLimitSwitch)) == (SMState.LeftLimitSwitch | SMState.RightLimitSwitch))
        State |= SMState.Error;
      else State &= ~SMState.Error;
      ChangeStatus((State & ~GetStateMask) | s, pos);

      if(!IsPositionSet && !IsMoving) {
        SetPosition(LastCurPos);
      }
      if(started && !IsMoving) {
        EventStop.Set();
        InContextInvoke(this, SMStopped, EventArgs.Empty);
        started = false;
      }
    }
    void SetPos_AS(int pos)
    {
      if(IsMoving)
        ChangeStatus("Cannot SetPosition while moving", State);

      string arg = pos.ToString("X8");

      try {
        sPI.Command(SMCmd.SetPosition, arg);
      } catch(Exception e) {
        ChangeStatus($"Error in SetPosition\n{e.Message}",
                  State | SMState.Error);
        return;
      }
      ChangeStatus($"Position Set:\n{arg}", State & ~SMState.Error);
    }
    void Power_AS(bool Power)
    {
      string arg = Power ? "ON" : "OFF";

      try {
        sPI.Command(SMCmd.Power, arg);
      } catch(Exception e) {
        ChangeStatus($"Error in Power\n{e.Message}",
                    State | SMState.Error);
        return;
      }
      ChangeStatus($"Turned Power {arg}", State & ~SMState.Error);
    }

    public void Connect(string port)
    {
      if(IsConnected) return;
      sPI.TQ.Enqueue(Connect_AS, port);
    }
    public void Disconnect()
    {
      if(!IsConnected) return;
      sPI.TQ.Enqueue(Disconnect_AS);
    }

    static ushort ConvertSpeed(double speed)
    {
      int s = (int)(65536.5 - SysCLK / speed);
      Bound(ref s, 0, 65535);
      return (ushort)s;
    }
    static ushort ConvertSB(int setback)
    {
      Bound(ref setback, 0, 65535);
      return (ushort)setback;
    }
    static double ConvertSpeed(uint s) => SysCLK / (65536 - s);
    static int ConvertSB(uint sb) => (int)sb;

    const double SysCLK = 48.0E6;

    // Async Methods
    public void SetParams(double? hspeed = null, double? lspeed = null, int? setback = null, ParamStuct? ps = null)
        => SetParams(ref hspeed, ref lspeed, ref setback, ref ps);
    public void SetParams(ref double? hspeed, ref double? lspeed, ref int? setback, ref ParamStuct? ps)
    {
      if(hspeed != null)
        highSpeed = ConvertSpeed((double)hspeed);
      hspeed = ConvertSpeed(highSpeed);
      if(lspeed != null)
        lowSpeed = ConvertSpeed((double)lspeed);
      lspeed = ConvertSpeed(lowSpeed);
      if(setback != null)
        setBack = ConvertSB((int)setback);
      setback = ConvertSB(setBack);
      if(ps != null)
        Params = (ParamStuct)ps;
      ps = Params;

      if(!IsConnected) return;
      sPI.TQ.Enqueue(SetParams_AS);
    }
    public double GoTo(double destination)
    {
      if(!IsConnected) return double.NaN;
      double dest = Callibration.TransformBack(destination);
      Bound(ref dest, int.MinValue, int.MaxValue);
      int idest = (int)Round(dest);

      sPI.TQ.Enqueue(Goto_AS, idest);

      return Callibration.Transform(idest);
    }
    public void GoToStep(int step_destination)
    {
      if(!IsConnected) return;

      sPI.TQ.Enqueue(Goto_AS, step_destination);
    }
    public void Stop()
    {
      if(!IsConnected) return;

      sPI.TQ.Enqueue(Stop_AS);
    }
    public void Power(bool Power)
    {
      if(!IsConnected) return;

      sPI.TQ.Enqueue(Power_AS, Power);
    }
    public void SetPosition(int steppos)
    {
      if(!IsConnected) return;

      sPI.TQ.Enqueue(SetPos_AS, steppos);
    }

    WaitHandle[] ResetEvents = new WaitHandle[] {
      new ManualResetEvent(false),
      new AutoResetEvent(false),
      new AutoResetEvent(false)
    };

    ManualResetEvent EventStop => (ManualResetEvent)ResetEvents[0];
    AutoResetEvent EventAbort => (AutoResetEvent)ResetEvents[1];
    AutoResetEvent EventTimeout => (AutoResetEvent)ResetEvents[2];

    public void Abort() => EventAbort.Set();
    public void ClearAbort() => EventAbort.Reset();

    enum EventSource : int
    {
      Stop = 0, Abort = 1, EventTimeout = 2,
      Timeout = WaitHandle.WaitTimeout
    }

    Timer TimeoutTimer;
    static readonly TimeSpan LongTimeout = TimeSpan.FromMinutes(1.0);
    static readonly TimeSpan ShortTimeout = TimeSpan.FromSeconds(1.0);
    static readonly TimeSpan ThreadSleepTimeout = TimeSpan.FromMilliseconds(10.0);
    private readonly int LastCurPos;

    void TimerTimeOut(object TimeOut) => ((AutoResetEvent)TimeOut).Set();
    void StartTimeout(TimeSpan timeout)
    {
      EventTimeout.Reset();
      TimeoutTimer.Change(timeout, TimeSpan.Zero);
    }
    // Sync Methods
    public void GoToAndWait(double pos)
    {
      StartTimeout(LongTimeout);
      while(Position != pos) {
        EventStop.Reset();
        pos = GoTo(pos);
        switch((EventSource)WaitHandle.WaitAny(ResetEvents, LongTimeout)) {
          case EventSource.Abort:
            throw new AbortException("Aborted");
          case EventSource.Timeout:
          case EventSource.EventTimeout:
            throw new TimeoutException($"Cannot reach position: {pos}");
          default:
            break;
        }
      }
    }
    public void InitExperiment()
    {
      ClearAbort();
      PowerONAndWait();
    }

    void PowerONAndWait()
    {
      if(SMStopMode != SMStopPower.Off && !IsPowered) {
        Power(true);
        StartTimeout(ShortTimeout);
        Thread.Sleep(10);

        EventStop.Reset();

        while(!IsPowered) {
          switch((EventSource)WaitHandle.WaitAny(ResetEvents, ThreadSleepTimeout)) {
            case EventSource.Abort:
              throw new AbortException("Aborted");
            case EventSource.EventTimeout:
              throw new TimeoutException("Cannot turn StepMotor ON");
            default:
              break;
          }
        }
      }
    }

    [Flags]
    public enum ParamStuct : byte
    {
      SBDIR = 0x01,   // 0 - left, 1 - right?
      LSON = 0x02,    // 0 - short, 1 - open 
      LSDIR = 0x04,   // 0 - direct, 1 - inverse 
      STOPLP = 0x08,  // 0 - full power, 1 - low power 
      STOPOFF = 0x10, // 0 - P_STOPLP, 1 - power off 
      STEPDIV8 = 0x20,
      STEPDIV1 = 0x40
    }

    const ParamStuct ParamStuctSTEPDIV =
        ParamStuct.STEPDIV8 | ParamStuct.STEPDIV1;
    const ParamStuct ParamStuctSTOPPW =
        ParamStuct.STOPLP | ParamStuct.STOPOFF;

    const SMState GetStateMask = SMState.Busy | SMState.LeftLimitSwitch |
        SMState.RightLimitSwitch | SMState.PositionSet | SMState.Power;

    ParamStuct Params = ParamStuct.STOPLP;
    ushort highSpeed = 0xF8AC;
    ushort lowSpeed = 0xE2B0;
    ushort setBack = 0x2AC8;

    public double HighSpeed {
      get => ConvertSpeed(highSpeed);
      set => SetParams(hspeed: value);
    }
    public double LowSpeed {
      get => ConvertSpeed(lowSpeed);
      set => SetParams(lspeed: value);
    }
    public int SetBack {
      get => ConvertSB(setBack);
      set => SetParams(setback: value);
    }

    public ParamStuct Parameters {
      get => Params;
      set => SetParams(ps: value);
    }

    public SMDir SMDirection {
      get => (SMDir)(Params & ParamStuct.SBDIR);
      set => SetParams(ps: Params & ~ParamStuct.SBDIR | (ParamStuct)value);
    }
    public SMLSONs SMLSON {
      get => (SMLSONs)(Params & ParamStuct.LSON);
      set => SetParams(ps: Params & ~ParamStuct.LSON | (ParamStuct)value);
    }
    public SMLSLSDirs SMLSLSDir {
      get => (SMLSLSDirs)(Params & ParamStuct.LSDIR);
      set => SetParams(ps: Params & ~ParamStuct.LSDIR | (ParamStuct)value);
    }
    public SMStopPower SMStopMode {
      get => (SMStopPower)(Params & ParamStuctSTOPPW);
      set => SetParams(ps: Params & ~ParamStuctSTOPPW | (ParamStuct)value);
    }
    public SMStepDivs SMStepDiv {
      get => (SMStepDivs)(Params & ParamStuctSTEPDIV);
      set => SetParams(ps: Params & ~ParamStuctSTEPDIV | (ParamStuct)value);
    }

    public SMDir ResetDir { get; set; }
    public double ResetPos { get; set; }
    public LinearCallibration Callibration { get; set; } = new LinearCallibration();


    #region implementing Dispose

    bool disposed = false;

    protected void Dispose(bool disposing)
    {
      if(disposed) return;

      if(disposing) {
        sPI.Dispose();
      }

      disposed = true;
    }

    public void Dispose()
    {
      Dispose(true);
    }
    #endregion

  }
  /// <summary>
  /// Y = A + B*X
  /// </summary>
  public class LinearCallibration
  {
    public double A { get; set; } = 0;
    public double B { get; set; } = 1;

    double x1 = 0, x2 = 1, y1 = 0, y2 = 1;

    public double X1 {
      get => x1;
      set { x1 = value; ABfromXY(x1, x2, y1, y2); }
    }
    public double X2 {
      get => x2;
      set { x2 = value; ABfromXY(x1, x2, y1, y2); }
    }
    public double Y1 {
      get => y1;
      set { y1 = value; ABfromXY(x1, x2, y1, y2); }
    }
    public double Y2 {
      get => y2;
      set { y2 = value; ABfromXY(x1, x2, y1, y2); }
    }

    void ABfromXY(double X1, double X2, double Y1, double Y2)
    {
      B = (Y2 - Y1) / (X2 - X1);
      A = Y1 - X1 * B;
    }

    public void InitfromXY(double X1, double X2, double Y1, double Y2)
    {
      if(Y1 == Y2 || X1 == X2)
        throw new ArgumentException("Wrong points");
      ABfromXY(X1, X2, Y1, Y2);
    }

    public double Transform(double X) => A + B * X;
    public double TransformBack(double Y) => (Y - A) / B;
  }
}
