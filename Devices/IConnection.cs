using System;
using System.Collections.Generic;
using System.Threading;
using SerialPorting;
using ThreadQueuing;

namespace Devices
{
  [Flags]
  public enum SState
  {
    Disconnected = 1,
    Connected = 2,
    Busy = 4,
    Error = 8,
    AutoPolling = 0x10,
    Initialized = 0x20,
    ControlOn = 0x40,
    Ready = Connected | Initialized,
    Controlled = Ready | ControlOn,
  }
  abstract class ConnectionBase : IDisposable
  {
    /// <summary> Default is 100 </summary> 
    public virtual int BasicTimeout => 100;
    public virtual bool AddQuestionMark => true;

    public ThreadQueue TQ { get; }

    public abstract string PortName { get; }
    protected abstract string InitString { get; }

    SState state = SState.Disconnected;
    public SState State {
      get => state;
      set {
        if (state != value) {
          state = value;
          if (!state.HasFlag(SState.Error)) NumberOfErrors = 0;
          StateChangedDelegate?.Invoke();
        }
      }
    }

    public static uint MaxNumberOfErrors = 3;
    public int NumberOfErrors { get; private set; } = 0;
    public virtual bool SetError()
    {
      State |= SState.Error;
      if (++NumberOfErrors >= MaxNumberOfErrors) {
        TQ.EnqueueUnique(Disconnect);
        return true;
      }
      return false;
    }

    public Action StateChangedDelegate;

    public bool IsConnected => (State & SState.Connected) == SState.Connected;
    public bool IsInitialized => (State & SState.Initialized) == SState.Initialized;

    public event EventHandler Initialized;
    public event EventHandler Disconnected;
    public event EventHandler Idle;
    public event EventHandler IdleTimeout;

    protected readonly WaitHandle AbortWaitHandle;

    protected ConnectionBase(WaitHandle abortWaitHandle, int idlewait, string TQName = "noname")
    {
      AbortWaitHandle = abortWaitHandle;
      TQ = ThreadQueue.Create(TQName, this, OnThreadInit);
      TQ.QueueIdleWait = idlewait;
    }

    protected abstract void OnThreadInit();

    protected virtual void OnSPIdle()
    {
      if (IsConnected)
        Idle?.Invoke(this, EventArgs.Empty);
    }
    protected virtual void OnSPIdleTimeout()
    {
      if (IsConnected) {
        State |= SState.AutoPolling;
        IdleTimeout?.Invoke(this, EventArgs.Empty);
        State &= ~SState.AutoPolling;
      }
    }
    public virtual void OnInitialized()
    {
      foreach (object o in TQ.UserObjects) {
        if (o is ConnectionBase si) {
          si.State &= ~SState.Disconnected;
          si.State |= SState.Ready;
          si.Initialized?.Invoke(this, EventArgs.Empty);
        }
      }
    }
    protected virtual void OnDisconnected()
    {
      foreach (object o in TQ.UserObjects) {
        if (o is ConnectionBase si) {
          si.State &= ~SState.Connected;
          si.State |= SState.Disconnected;
          si.Disconnected?.Invoke(this, EventArgs.Empty);
        }
      }
    }

    public abstract void Connect(string port);
    public abstract void Disconnect();

    public abstract string Iitialize();
    public abstract void Reset();
    internal virtual void PreDisconnectCommand() => Reset();
    public abstract void Abort();
    public abstract void Flush();

    protected abstract IDictionary<Enum, string> Coms { get; }
    #region implementing Dispose

    bool disposed = false;

    protected void Dispose(bool disposing)
    {
      if (disposed) return;

      TQ?.Dispose();
      disposed = true;
    }

    public void Dispose()
    {
      Dispose(true);
    }
    #endregion
  }
}