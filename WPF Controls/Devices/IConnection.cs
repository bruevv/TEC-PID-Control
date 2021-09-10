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
    Error = 8
  }
  public abstract class ConnectionBase : IDisposable
  {
    /// <summary> Default is 100 </summary> 
    public virtual int BasicTimeout => 100;
    public virtual bool AddQuestionMark => true;

    public ThreadQueue TQ { get; }

    public abstract string PortName { get; }
    protected abstract string InitString { get; }

    public SState State { get; protected set; } = SState.Disconnected;

    public bool IsConnected => (State & SState.Connected) == SState.Connected;

    public event EventHandler Connected;
    public event EventHandler Disconnected;
    public event EventHandler Idle;

    protected readonly WaitHandle AbortWaitHandle;

    protected ConnectionBase(WaitHandle abortWaitHandle, int idlewait, string TQName)
    {
      AbortWaitHandle = abortWaitHandle;
      TQ = ThreadQueue.Create(TQName, this, OnThreadInit);
      TQ.QueueIdleWait = idlewait;
    }

    protected abstract void OnThreadInit();

    protected virtual void OnSPIdle() => Idle?.Invoke(this, EventArgs.Empty);

    protected virtual void OnConnected()
    {
      foreach(object o in TQ.UserObjects) {
        if(o is ConnectionBase si) {
          si.State &= ~SState.Disconnected;
          si.State |= SState.Connected;
          si.Connected?.Invoke(this, EventArgs.Empty);
        }
      }
    }
    protected virtual void OnDisconnected()
    {
      foreach(object o in TQ.UserObjects) {
        if(o is ConnectionBase si) {
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
    public abstract void Abort();
    public abstract void Flush();

    protected abstract IDictionary<Enum, string> Coms { get; }
    #region implementing Dispose

    bool disposed = false;

    protected void Dispose(bool disposing)
    {
      if(disposed) return;

      TQ?.Dispose();
      disposed = true;
    }

    public void Dispose()
    {
      Dispose(true);
    }
    #endregion
  }

  /// <summary>
  /// ConnectionInterface rncapsulates all transactions to the device
  /// </summary>
  /// <typeparam name="T">Connection communication style (ASCII:string or Binary: byte[])</typeparam>
  public interface IConnection<T> : IDisposable where T : class
  {
    string PortName { get; }
    SState State { get; }
    ThreadQueue TQ { get; }

    bool IsConnected { get; }

    event EventHandler Connected;
    event EventHandler Disconnected;
    event EventHandler Idle;

    void Connect(string port);
    void Disconnect();

    string Iitialize();
    void Reset();
    void Abort();
    void Flush();

    public void Command(Enum c);
    void Command(Enum c, T args);
    string Request(Enum r);
    void Request(Enum r, byte[] buf, int BytesToReceive, int WaitTimeout);
  }
}