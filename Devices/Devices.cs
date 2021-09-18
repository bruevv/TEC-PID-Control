using System;
using System.Threading;

namespace Devices
{
  using System.Threading.Tasks;
  using CSUtils;
  using ThreadQueuing;
  using static ThreadQueuing.Invoke;

  abstract public class Device : IDisposable
  {
    readonly private protected ConnectionBase iCI;
    abstract private protected ConnectionBase InitSPI(string name);
    protected Device(string name) => iCI = InitSPI(name);

    public SState State {
      get => iCI.State;
      protected set {
        if (iCI.State != value) {
          iCI.State = value;
        }
      }
    }
    const SState ModifiableState = SState.Error;
    public string StatusStr { get; protected set; } = null;

    public bool IsConnected => (State & SState.Ready) == SState.Ready;
    public bool HasError => (State & SState.Error) == SState.Error;
    protected bool IsAutopollingNow => (State & SState.AutoPolling) == SState.AutoPolling;

    public event EventHandler<DevStatusChangedEA> StatusChanged;
    public event EventHandler Idle;

    protected void ChangeStatus(SState state, Exception e = null) => ChangeStatus(null, state, e);
    protected void ChangeStatus(string str, Exception e = null) => ChangeStatus(str, State, e);
    protected void ChangeStatus(string str, SState state, Exception e = null) =>
      ChangeStatus(ref str, state, e);
    protected virtual void ChangeStatus(ref string str, SState state, Exception e = null)
    {
      State = (State | (state & ModifiableState)) & (state | ~ModifiableState);// better move to property
      Logger.Mode lm = state.HasFlag(SState.Error) ?
        Logger.Mode.Error : state.HasFlag(SState.AutoPolling) ?
         Logger.Mode.Full : Logger.Mode.NoAutoPoll;
      Logger.Log(ref str, e, lm, GetType().Name);
      StatusStr = str ?? StatusStr;

      InContextInvoke(this, StatusChanged, new DevStatusChangedEA(str, state));
    }
    protected virtual void SetErrorStatus(string status = null, Exception e = null) => ChangeStatus(ref status, state: State | SState.Error, e);

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
        if (isIdle) EventIdle.Set();
        else EventIdle.Reset();
      }
    }
    protected virtual void OnIdle()
    {
      IsIdle = true;
      if (IsConnected && Idle != null) InContextInvoke(this, Idle, EventArgs.Empty);
    }
    protected virtual void OnNotIdle() => IsIdle = false;

    public virtual void WaitForIdle(int timeout = 1000)
    {
      if (IsIdle) return;

      switch ((EventSource)WaitHandle.WaitAny(ResetEvents, timeout)) {
        case EventSource.Abort:
          throw new AbortException("Wait for Idle Aborted");
        case EventSource.Timeout:
          throw new TimeoutException($"{GetType().Name} not responding");
      }
    }

    public abstract void Dispose();

    public class DevStatusChangedEA : EventArgs
    {
      internal DevStatusChangedEA(string str, SState state)
      {
        StatusString = str;
        DState = state;
      }

      public string StatusString { get; }
      public SState DState { get; }

      public bool IsConnected => (DState & SState.Connected) == SState.Connected;
      public bool HasError => (DState & SState.Error) == SState.Error;
    }
  }
  public abstract class TDevice<T> : Device, IDisposable
    where T : class
  {
    public int IdleTimeout {
      get => iCI.TQ.QueueIdleWait;
      set => iCI.TQ.QueueIdleWait = value;
    }

    public string PortName => iCI.PortName;
    public virtual string Name => iCI.TQ.Name;

    public event EventHandler ConnectedToDevice;
    public event EventHandler DisconnectedFromDevice;

    virtual protected void OnConnectedToDevice(object o, EventArgs e)
      => InContextInvoke(this, ConnectedToDevice, e);
    virtual protected void OnDisconnectedFromDevice(object o, EventArgs e)
   => InContextInvoke(this, DisconnectedFromDevice, e);
    void DisconnectedEvent(object o, EventArgs e)
    {
      iCI.State &= ~SState.Initialized;
      OnDisconnectedFromDevice(this, EventArgs.Empty);
    }
    bool connecting = false;
    void СonnectedEvent(object o, EventArgs e)
    {
      if (!connecting && !iCI.IsInitialized) iCI.TQ.EnqueueUnique(Connect_AS, iCI.PortName);
    }

    protected TDevice(string name) : base(name)
    {
      iCI.Disconnected += DisconnectedEvent;
      iCI.Connected += СonnectedEvent;

      iCI.TQ.ThreadIdle += OnIdle;
      iCI.TQ.ExitIdle += OnNotIdle;
    }
    protected void Connect_AS(string port) => Connect_AS(port, null);
    protected void Connect_AS(string port, EventWaitHandle ewh)
    {
      ChangeStatus($"Trying to Connect\nPort:{port}");
      string initS = "";
      bool disconnect = false;
      connecting = true;
      try {
        if (!iCI.IsConnected || !iCI.PortName.Equals(port)) {
          disconnect = true;
          iCI.Connect(port);
        }
  //      if(!iCI.IsInitialized)
          initS = iCI.Iitialize();
      } catch (Exception e) {
        if (disconnect)
          iCI.Disconnect();
        ChangeStatus($"Error connecting to device", SState.Error, e);
        goto finish;
      }
      ChangeStatus($"Connected succsefully. Init String:\n{initS}");

      OnConnectedToDevice(this, EventArgs.Empty);

    finish:
      connecting = false;
      ewh?.Set();
    }
    protected void Disconnect_AS() => Disconnect_AS(null);
    protected void Disconnect_AS(EventWaitHandle ewh)
    {
      iCI.State &= ~SState.Initialized;

      try { iCI.PreDisconnectCommand(); } catch { }

      try { iCI.Disconnect(); } catch (Exception e) {
        ChangeStatus($"Error disconnecting\n{e.Message}");
        goto finish;
      }
      ChangeStatus("Disconnected");

      OnDisconnectedFromDevice(this, EventArgs.Empty);

    finish:
      ewh?.Set();
    }

    public void Connect(string port)
    {
      if (IsConnected) {
        OnConnectedToDevice(this, EventArgs.Empty);
        return;
      }
      iCI.TQ.EnqueueUnique(Connect_AS, port);
    }
    public async Task Connect_AW(string port)
    {
      if (IsConnected) {
        OnConnectedToDevice(this, EventArgs.Empty);
        return;
      }
      var ewh = EventWaitHandlePool.GetHandle();
      iCI.TQ.Enqueue(Connect_AS, port, ewh);
      _ = await Task.Run(() => ewh.WaitOne());
      EventWaitHandlePool.ReturnHandle(ewh);
    }
    public void Disconnect()
    {
      if (!IsConnected) return;
      iCI.TQ.EnqueueUnique(Disconnect_AS);
    }
    public async Task Disconnect_AW()
    {
      if (!IsConnected) return;
      var ewh = EventWaitHandlePool.GetHandle();
      iCI.TQ.EnqueueUnique(Disconnect_AS, ewh);
      _ = await Task.Run(() => ewh.WaitOne());
      EventWaitHandlePool.ReturnHandle(ewh);
    }

    #region implementing Dispose

    bool disposed = false;

    void Dispose(bool disposing)
    {
      if (disposed) return;

      if (disposing) {
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
  public abstract class ASCIIDevice : TDevice<string>
  {
    protected ASCIIDevice(string name) : base(name) { }
  }
}