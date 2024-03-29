﻿using System;
using System.Threading;

namespace Devices
{
  using System.ComponentModel;
  using System.Runtime.CompilerServices;
  using System.Threading.Tasks;
  using CSUtils;
  using ThreadQueuing;
  using static CSUtils.Invoke;

  abstract public class Device : IDisposable, INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string property = "") =>
     PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

    readonly private protected ConnectionBase iCI;
    public virtual string DeviceName => nameof(Device);
    abstract private protected ConnectionBase InitSPI(string name);
    protected Device(string name)
    {
      iCI = InitSPI(name);
      iCI.StateChangedEvent += ICIStateChanged;
    }
    void ICIStateChanged(SState prev)
    {
      OnPropertyChanged(nameof(State));
      if ((prev & SState.ControlOn) != (State & SState.ControlOn))
        OnPropertyChanged(nameof(IsControlled));
    }
    public SState State {
      get => iCI.State;
      protected set {
        if (iCI.State != value) {
          if (iCI.State.HasFlag(SState.ControlOn) && !value.HasFlag(SState.ControlOn))
            Logger.Log($"Leaving Controled State", Logger.Mode.NoAutoPoll, DeviceName);
          else if (!iCI.State.HasFlag(SState.ControlOn) && value.HasFlag(SState.ControlOn))
            Logger.Log($"Entering Controled State", Logger.Mode.NoAutoPoll, DeviceName);

          iCI.State = value;
        }
      }
    }
    const SState ModifiableState = SState.Error;
    public string StatusStr { get; protected set; } = null;

    public bool IsConnected => (State & SState.Ready) == SState.Ready;
    public bool HasError => (State & SState.Error) == SState.Error;
    protected bool IsAutopolling => (State & SState.AutoPollingOn) == SState.AutoPollingOn;
    public bool IsControlled {
      get => (State & SState.ControlOn) == SState.ControlOn;
      set {
        if (IsControlled != value) {
          if (value) State |= SState.ControlOn;
          else State &= ~SState.ControlOn;
        }
      }
    }

    public event EventHandler<DevStatusChangedEA> StatusChanged;
    public event EventHandler Idle;

    protected void ChangeStatus(SState state, Exception e = null) => ChangeStatus(null, state, e);
    protected void ChangeStatus(string str, Exception e = null) => ChangeStatus(str, State, e);
    protected void ChangeStatus(string str, SState state, Exception e = null) =>
      ChangeStatus(ref str, state, e);
    protected virtual void ChangeStatus(ref string str, SState state, Exception e = null)
    {
      if (state.HasFlag(SState.Error)) iCI.SetError();

      State = (State | (state & ModifiableState)) & (state | ~ModifiableState);// better move to property
      Logger.Mode lm = HasError ?
        Logger.Mode.Error : (IsAutopolling || IsControlled) ?
         Logger.Mode.Full : Logger.Mode.NoAutoPoll;
      Logger.Log(ref str, e, lm, DeviceName);
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
          throw new TimeoutException($"{DeviceName} not responding");
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
      if(iCI.IsConnected)
        ChangeStatus($"Disconnected");
      OnDisconnectedFromDevice(this, EventArgs.Empty);
    }
    bool connecting = false;
    void СonnectedEvent(object o, EventArgs e)
    {
      if (connecting) return;

      if (!iCI.IsInitialized) iCI.TQ.EnqueueUnique(Connect_AS, iCI.PortName);
      else OnConnectedToDevice(this, EventArgs.Empty);
    }

    protected TDevice(string name) : base(name)
    {
      iCI.Disconnected += DisconnectedEvent;
      iCI.Initialized += СonnectedEvent;

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
      iCI.OnInitialized();

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

  public class InvalidDeviceStateException : Exception
  {
    public SState DeviceState { get; init; }

    public InvalidDeviceStateException() : base() { }
    public InvalidDeviceStateException(string err) : base(err) { }
    public InvalidDeviceStateException(SState ds) : base($"Device State:{ds}") { DeviceState = ds; }
    public InvalidDeviceStateException(string err, SState ds) : base(err) { DeviceState = ds; }
  }
  public class DeviceDisconnectedException : InvalidDeviceStateException
  {
    public DeviceDisconnectedException() : base(SState.Disconnected) { }
    public DeviceDisconnectedException(string error) : base(error, SState.Disconnected) { }
    public DeviceDisconnectedException(string error, SState ds) : base(error, ds) { }
  }
}