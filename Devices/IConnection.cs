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
    Ready = Connected | Initialized,
  }
  abstract class ConnectionBase : IDisposable
  {
    /// <summary> Default is 100 </summary> 
    public virtual int BasicTimeout => 100;
    public virtual bool AddQuestionMark => true;

    public ThreadQueue TQ { get; }

    public abstract string PortName { get; }
    protected abstract string InitString { get; }

    public SState State { get; set; } = SState.Disconnected;

    public bool IsConnected => (State & SState.Connected) == SState.Connected;

    public event EventHandler Connected;
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
    protected virtual void OnConnected()
    {
      foreach (object o in TQ.UserObjects) {
        if (o is ConnectionBase si) {
          si.State &= ~SState.Disconnected;
          si.State |= SState.Connected;
          si.Connected?.Invoke(this, EventArgs.Empty);
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