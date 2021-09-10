using System;
using System.Threading;

namespace Devices
{
  using CSUtils;
  using SerialPorting;
  using static ThreadQueuing.Invoke;

  abstract public class Device : IDisposable
  {
    [Flags]
    public enum DevState
    {
      Disconnected = 0x01, Connected = 0x02,
      //     On = 0x04,//!
      Connecting = 0x05, Error = 0x08,
      Disconnecting = 0x101
    }

    public DevState DState { get; protected set; } = DevState.Disconnected;
    public string StatusStr { get; protected set; } = null;

    public bool IsConnected => (DState & DevState.Connected) == DevState.Connected;
    public bool HasError => (DState & DevState.Error) == DevState.Error;

    public event EventHandler<DevStatusChangedEA> StatusChanged;
    public event EventHandler Idle;

    protected void ChangeStatus(DevState state, Exception e = null) => ChangeStatus(null, state, e);
    protected void ChangeStatus(string str, DevState state, Exception e = null) =>
      ChangeStatus(ref str, state, e);
    protected virtual void ChangeStatus(ref string str, DevState state, Exception e = null)
    {
      DState = state;
      Logger.Mode lm = state.HasFlag(DevState.Error) ? Logger.Mode.Error : Logger.Mode.Full;
      Logger.Log(ref str, e, lm, GetType().Name);
      StatusStr = str ?? StatusStr;

      InContextInvoke(this, StatusChanged, new DevStatusChangedEA(str, state));
    }
    protected virtual void SetErrorStatus(string status = null, Exception e = null) => ChangeStatus(ref status, state: DState | DevState.Error, e);

    protected readonly WaitHandle[] ResetEvents = new WaitHandle[2] {
      new ManualResetEvent(false),
      new ManualResetEvent(false)
    };

    protected ManualResetEvent EventIdle => (ManualResetEvent)ResetEvents[0];
    protected ManualResetEvent EventAbort => (ManualResetEvent)ResetEvents[1];

    public virtual void Abort() => EventAbort.Set();
    public virtual void ClearAbort() => EventAbort.Reset();

    public enum EventSource : int
    {
      Idle = 0, Abort = 1,
      Timeout = WaitHandle.WaitTimeout
    }


    bool isIdle = false;
    public bool IsIdle {
      get => isIdle;
      private set {
        isIdle = value;
        if(isIdle) EventIdle.Set();
        else EventIdle.Reset();
      }
    }
    protected virtual void OnIdle()
    {
      IsIdle = true;
      if(IsConnected && Idle != null) InContextInvoke(this, Idle, EventArgs.Empty);
    }
    protected virtual void OnNotIdle() => IsIdle = false;

    public virtual void WaitForIdle(int timeout = 1000)
    {
      if(IsIdle) return;

      switch((EventSource)WaitHandle.WaitAny(ResetEvents, timeout)) {
        case EventSource.Abort:
          throw new AbortException("Wait for Idle Aborted");
        case EventSource.Timeout:
          throw new TimeoutException($"{GetType().Name} not responding");
      }
    }

    public abstract void Dispose();

    public class DevStatusChangedEA : EventArgs
    {
      internal DevStatusChangedEA(string str, DevState state)
      {
        StatusString = str;
        DState = state;
      }

      public string StatusString { get; }
      public DevState DState { get; }

      public bool IsConnected => (DState & DevState.Connected) == DevState.Connected;
      public bool HasError => (DState & DevState.Error) == DevState.Error;
    }
  }

  public abstract class TDevice<T, C> : Device, IDisposable
    where T : class
    where C : IConnection<T>
  {

    readonly protected C iCI;

    public string PortName => iCI.PortName;

    public event EventHandler ConnectedToDevice;

    virtual protected void OnConnectedToDevice(object o, EventArgs e)
      => InContextInvoke(this, ConnectedToDevice, e);
    void DisconnectedEvent(object o, EventArgs e)
    {
      if((DState & DevState.Connected) == DevState.Connected)
        ChangeStatus("Disconnected", DevState.Disconnected);
      else
        ChangeStatus(DevState.Disconnected);
    }

    abstract protected C InitSPI();
    protected TDevice()
    {
      iCI = InitSPI();

      iCI.Disconnected += DisconnectedEvent;
      /// it was like that, now only after initialize      sPI.Connected += OnConnectedToCOM;

      iCI.TQ.ThreadIdle += OnIdle;
      iCI.TQ.ExitIdle += OnNotIdle;
    }

    protected void Connect_AS(string port)
    {
      ChangeStatus($"Trying to Connect\nPort:{port}", DevState.Connecting);
      string initS = "";
      bool disconnect = false;
      try {
        if(!iCI.IsConnected || !iCI.PortName.Equals(port)) {
          disconnect = true;
          iCI.Connect(port);
        }
        initS = iCI.Iitialize();
      } catch(Exception e) {
        if(disconnect)
          iCI.Disconnect();
        ChangeStatus($"Error connecting to device", DevState.Disconnected, e);
        return;
      }
      ChangeStatus($"Connected succsefully. Init String:\n{initS}", DevState.Connected);

      OnConnectedToDevice(this, EventArgs.Empty);
    }
    protected void Disconnect_AS()
    {
      DState = DevState.Disconnecting;
      try { iCI.Reset(); } catch { }

      try { iCI.Disconnect(); } catch(Exception e) {
        ChangeStatus($"Error disconnecting\n{e.Message}", DevState.Disconnected);
        return;
      }
      ChangeStatus("Disconnected\nController reset", DevState.Disconnected);
    }

    public void Connect(string port)
    {
      if(IsConnected) return;
      iCI.TQ.EnqueueUnique(Connect_AS, port);
    }
    public void Disconnect()
    {
      if(!IsConnected) return;
      iCI.TQ.EnqueueUnique(Disconnect_AS);
    }

    #region implementing Dispose

    bool disposed = false;

    void Dispose(bool disposing)
    {
      if(disposed) return;

      if(disposing) {
        iCI.Dispose();
      }

      disposed = true;
    }

    public override void Dispose()
    {
      Dispose(true);
    }
    #endregion
  }

  //  abstract class UARTDevice<T> : TDevice<T, UARTConnection<T>> where T : class { }
  public abstract class ASCIIDevice : TDevice<string, IConnection<string>>
  {

  }
}