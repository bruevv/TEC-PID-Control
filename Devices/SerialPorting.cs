﻿using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace Devices
{
  public enum CCmd
  {
    Init = 0x00,
    Reset = 0x01,
    Abort = 0x02,
    Custom = 0x03,
    NOP = 0xff
  };
}

namespace SerialPorting
{
  using CSUtils;
  using Devices;
  using WPFUtils;

  abstract class UARTConnectionBase : ConnectionBase
  {

    protected SerialPort SP = null;

    public override string PortName => SP?.PortName ?? "";
    public int BoudRate => SP?.BaudRate ?? -1;
    public double BytesPerS => BoudRate / (1.0 + 8.0 + (int)SP.StopBits + (SP.Parity == Parity.None ? 0.0 : 1.0));

    protected virtual int BaudRate => 57600;
    protected virtual string Address => null;
    protected virtual string NewLine => "\r";
    protected virtual string ACK => "ACK";
    protected virtual bool TrimDevCtrlCahrs => true;

    /// <summary> Default is BasicTimeout=100 </summary> 
    public int ReadTimeOut {
      get => SP.ReadTimeout;
      set => SP.ReadTimeout = value;
    }

    /// <summary> 9bit with address </summary>
    protected bool Is9bit => Address != null;
    protected bool EnableNewLine => NewLine != null;
    protected bool NeedAck => ACK != null;


    public static string[] GetPortNames() => SerialPort.GetPortNames();

    protected UARTConnectionBase(WaitHandle abortWaitHandle, int idlewait = -1, string TQName = "noname") : base(abortWaitHandle, idlewait, TQName)
    {
      TQ.ThreadDispose += () => SP?.Dispose();
      TQ.ThreadIdle += OnSPIdle;
      TQ.ThreadIdleTimeOut += OnSPIdleTimeout;
      TQ.Start();/// TODO: check why TQ.Start() is before the next string: "if(...) SP=..."
      if (TQ.Owner != this) SP = ((UARTConnectionBase)TQ.Owner).SP;
    }

    public override void Connect(string port)
    {
      if (IsConnected && SP.PortName == port)
        return;

      if (IsConnected)
        Disconnect();

      try {
        SP.PortName = port;
        SP.Open();
      } catch (Exception e) {
        State = SState.Disconnected;
        OnDisconnected();
        throw new Exception($"Cannot connect to Serial Port {port}\n{e.Message}", e);
      }

      State = SState.Connected;
    }
    public override void Disconnect()
    {
      if (SP.IsOpen)
        SP.Close();
      State = SState.Disconnected;
      OnDisconnected();
      //    TQ.ClearQueue();
    }

    public override void Flush()
    {
      Thread.Sleep(BasicTimeout);
      SP.DiscardInBuffer();
      SP.DiscardOutBuffer();
    }
  }
  /// <summary>
  /// ConnectionInterface incapsulates all transactions to the device
  /// </summary>
  /// <typeparam name="T">Connection communication style (ASCII:string or Binary: byte[])</typeparam>
  abstract class UARTConnection<T> : UARTConnectionBase
    where T : class
  {
    enum TransferType { ASCII, Binary };
    static readonly TransferType TT = typeof(T) == typeof(string) ? TransferType.ASCII : TransferType.Binary;

    protected UARTConnection(WaitHandle abortWaitHandle, int idlewait = -1, string TQName = "noname") : base(abortWaitHandle, idlewait, TQName) { }

    protected override void OnThreadInit()
    {
      SP = new SerialPort {
        BaudRate = BaudRate,
        ReadTimeout = BasicTimeout,
        WriteTimeout = BasicTimeout
      };
      if (NewLine is string s) SP.NewLine = s;
    }

    public override string Iitialize()
    {
      string ret = null;

      try {
        Flush();
        switch (TT) {
          case TransferType.ASCII:
            ret = Request(GenCommand(CCmd.Init), null, true);
            break;
          case TransferType.Binary:
            byte[] ba = new byte[1];
            Request(CCmd.Init, ba, 1, BasicTimeout);
            ret = Encoding.ASCII.GetString(ba);
            break;
        }
      } catch (Exception e) {
        //        SetError();
        throw new IOException($"Initialization Failed at {BaudRate}Baud\n{e.Message}", e);
      }
      if (!ret.Contains(InitString))
        throw new IOException($"Wrong Init string at {BaudRate}Baud\n{ret}");
      State |= SState.Initialized;
      return ret;
    }
    public override void Reset()
    {
      if (!IsConnected)
        throw new Exception("Serial Port Disconnected");

      int rt = ReadTimeOut;
      try {
        SP.ReadTimeout = BasicTimeout;
        Request(GenCommand(CCmd.Reset), null);
      } catch (TimeoutException) {
      } finally {
        ReadTimeOut = rt;
      }

      Thread.Sleep(BasicTimeout);
    }
    public override void Abort()
    {
      if (!IsConnected) throw new Exception("Serial Port Disconnected");

      int rt = ReadTimeOut;
      try {
        SP.ReadTimeout = BasicTimeout;
        Request(GenCommand(CCmd.Abort), null);

        Flush();
      } finally {
        ReadTimeOut = rt;
      }
    }

    protected virtual string ArgSeparator => " ";
    string Request(string cmd, T args, bool NeedReturn = false)
    {
      if (!IsConnected) throw new Exception("Serial Port Disconnected");

      if (State.HasFlag(SState.Error)) Flush();

      if (Is9bit) {
        SP.Parity = Parity.Mark;
        SP.Write(Address);
        SP.Parity = Parity.Space;
      }

      switch (args) {
        case string str_args:
          if (string.IsNullOrEmpty(cmd)) SP.Write(str_args);
          else SP.Write(cmd + ArgSeparator + str_args);
          break;
        case byte[] ba_args:
          if (!string.IsNullOrEmpty(cmd)) SP.Write(cmd + ArgSeparator);
          SP.Write(ba_args, 0, ba_args.Length);
          break;
        case null:
          SP.Write(cmd);
          break;
        default:
          throw new ArgumentException();
      }

      if (EnableNewLine) SP.WriteLine("");

      if (NeedAck || NeedReturn) {
        string rl = SP.ReadLine();
        return TrimDevCtrlCahrs ? rl.Trim('\u0013', '\u0011') : rl;
      } else {
        return null;
      }
    }

    static string AsString(T t)
    {
      switch (t) {
        case string str:
          return str;
        case byte[] ba:
          return "[" + string.Join(",", ba) + "]";
        default:
          throw new ArgumentException();
      }
    }
    public void Command(Enum c)
    {
      string ack;

      try {
        ack = Request(GenCommand(c), null);
      } catch (Exception e) {
        //        SetError();
        throw new IOException($"Command {c.GetName()} Failed\n{e.Message}", e);
      }
      if (NeedAck && ack != ACK) {
        //        SetError();
        throw new IOException($"Command {c.GetName()} Error String '{ack}'");
      }

      State &= ~SState.Busy;
    }
    public void Command(Enum c, T args)
    {
      string ack;
      try {
        ack = Request(GenCommand(c), args);
      } catch (Exception e) {
        //      SetError();
        throw new IOException($"Command {c.GetName()} Args '{AsString(args)}' Failed\n{e.Message}", e);
      }
      if (NeedAck && ack != ACK) {
        //       SetError();
        throw new IOException($"Command {c.GetName()} Args '{AsString(args)}' String '{ack}'");
      }
      State &= ~SState.Busy;
    }

    protected virtual string GenCommand(Enum c) => Coms[c];

    /// <summary> In Milliseconds </summary>
    const int SerialReadInterval = 10;

    void Read(byte[] buf, int BytesToReceive, int WaitTimeout)
    {
      int received = 0;

      DateTime dtStart = DateTime.Now;
      TimeSpan timeout = TimeSpan.FromMilliseconds(WaitTimeout);
      do {
        int count = SP.BytesToRead;
        if (count > 0) {
          if (received + count > BytesToReceive)
            throw new IOException($"Too many bytes received per request.\n" +
                                  $"Requested {BytesToReceive}, Received {received + count}");

          received += SP.Read(buf, received, count);

          if (received == BytesToReceive) {
            Logger.Log("Waited " + (DateTime.Now - dtStart) +
                       "\nOf " + timeout, Logger.Mode.Debug, nameof(UARTConnection<T>));
            return;
          }
        }
        if (WaitTimeout > 2 * BasicTimeout) {
          if (AbortWaitHandle.WaitOne(SerialReadInterval))
            throw new AbortException("Read Command Aborted");
        } else {
          Thread.Sleep(SerialReadInterval);
        }
      } while (DateTime.Now - dtStart < timeout);
      throw new TimeoutException($"Failed to receive the reqested amount of binary data:\n" +
        $"Requested: {BytesToReceive}\nReceived: {received}\nTime Waited: {(DateTime.Now - dtStart).TotalSeconds}s");
    }

    public string Request(Enum r) => Request(r, null);
    public string Request(Enum r, T args)
    {
      string ret;
      State |= SState.Busy;
      string com = "";
      try {
        com = $"{GenCommand(r)}{(GenCommand(r) != "" && AddQuestionMark ? "?" : "")}";
        ret = Request(com, args, true);
      } catch (Exception e) {
        State &= ~SState.Busy;
        //      SetError();
        throw new IOException(
          $"Request {com}{ArgSeparator}{args} Failed\n{e.Message}", e);
      }
      State &= ~SState.Busy;
      return ret;
    }

    public void Request(Enum r, byte[] buf, int BytesToReceive = -1, int WaitTimeout = -1)
    {
      if (BytesToReceive == -1) BytesToReceive = buf.Length;
      if (WaitTimeout == -1) WaitTimeout = BasicTimeout;
      State |= SState.Busy;
      try {
        Request($"{GenCommand(r)}", null);
        Read(buf, BytesToReceive, WaitTimeout);
      } catch (AbortException) {
        State &= ~SState.Busy;
        throw;
      } catch (Exception e) {
        State &= ~SState.Busy;
        //      SetError();
        throw new IOException($"Request {GenCommand(r)} Failed\n{e.Message}", e);
      }
      State &= ~SState.Busy;
    }
  }

}