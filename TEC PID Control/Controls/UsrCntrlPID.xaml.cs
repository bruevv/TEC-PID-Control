﻿using CSUtils;
using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace TEC_PID_Control.PID
{

  public interface IMeasureParameter
  {
    void Reset();
    void Init();
    double Measure();

    bool ExpGoing { get; set; }
  }
  public interface IControlParameter
  {
    void Reset();
    void Init();
    void Control(double parameter);
    bool ExpGoing { get; set; }
  }
}

namespace TEC_PID_Control.Controls
{
  using Devices;
  using TEC_PID_Control.PID;
  using TEC_PID_Control.Properties;

  public partial class UsrCntrlPID : UserControl, IDisposable
  {
    [Flags]
    public enum EState
    {
      Disabled = 1,
      Enabled = 2,
      ReachedSetpointBit = 4,
      ReachedSetpoint = ReachedSetpointBit | Enabled,
      Error = 8,
    }

    #region DPBinding
    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(true) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty IsLogExpandedProperty =
        DependencyProperty.Register("IsLogExpanded", typeof(bool), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(false) { BindsTwoWayByDefault = true });

    public static readonly DependencyProperty SetPointProperty =
        DependencyProperty.Register(nameof(SetPoint), typeof(double), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(0.0, SetPointCB) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty CRateProperty =
      DependencyProperty.Register(nameof(CRate), typeof(double), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(0.0, CRateCB) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty CtrlPProperty =
        DependencyProperty.Register(nameof(CtrlP), typeof(double), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(0.0, CtrlPCB) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty CtrlIProperty =
        DependencyProperty.Register(nameof(CtrlI), typeof(double), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(0.0, CtrlICB) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty CtrlDProperty =
        DependencyProperty.Register(nameof(CtrlD), typeof(double), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(0.0, CtrlDCB) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty MaxCtrlParProperty =
    DependencyProperty.Register(nameof(MaxCtrlPar), typeof(double), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(0.0, MaxCtrlParCB) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty MinCtrlParProperty =
        DependencyProperty.Register(nameof(MinCtrlPar), typeof(double), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(0.0, MinCtrlParCB) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty GlobalGainProperty =
        DependencyProperty.Register(nameof(GlobalGain), typeof(double), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(0.0, GlobalGainCB) { BindsTwoWayByDefault = true });

    public static readonly DependencyProperty TimeConstantProperty =
        DependencyProperty.Register(nameof(TimeConstant), typeof(double), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(0.0, TimeConstantCB) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty MaxIntegralErrorProperty =
        DependencyProperty.Register(nameof(MaxIntegralError), typeof(double), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(0.0, MaxIntegralErrorCB) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty IsControlEnabledProperty =
        DependencyProperty.Register(nameof(IsControlEnabled), typeof(bool), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(false, IsControlEnabledCB) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty IsRampEnabledProperty =
     DependencyProperty.Register(nameof(IsRampEnabled), typeof(bool), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(false, IsRampEnabledCB) { BindsTwoWayByDefault = true });

    static readonly DependencyPropertyKey IsSetPointReachedKey =
     DependencyProperty.RegisterReadOnly(nameof(IsSPReached), typeof(bool), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(true, IsSetPointReachedCB));
    public static readonly DependencyProperty IsSetPointReachedProperty = IsSetPointReachedKey.DependencyProperty;

    static readonly DependencyPropertyKey StateKey =
      DependencyProperty.RegisterReadOnly(nameof(State), typeof(EState), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(EState.Disabled, StateCB));
    public static readonly DependencyProperty StateProperty = StateKey.DependencyProperty;


    #endregion DPBinding
    #region DPCallBacks
    static void SetPointCB(DependencyObject o, DependencyPropertyChangedEventArgs ea)
    {
      var t = (UsrCntrlPID)o;
      bool changed = false;
      double sp;
      lock (t.RCCLock) {
        sp = t.SetPoint;
        if (t.setPoint != sp) {
          t.setPoint = sp;
          changed = true;
        }
      }
      if (changed) Log($"SetPoint changed to {sp:N3} °C", Logger.Mode.NoAutoPoll);
    }
    static void CRateCB(DependencyObject o, DependencyPropertyChangedEventArgs ea)
    {
      var t = (UsrCntrlPID)o;
      lock (t.RCCLock) t.cRate = t.CRate;
    }
    static void CtrlPCB(DependencyObject o, DependencyPropertyChangedEventArgs ea)
    {
      var t = (UsrCntrlPID)o;
      lock (t.RCCLock) t.ctrlP = t.CtrlP;
    }
    static void CtrlICB(DependencyObject o, DependencyPropertyChangedEventArgs ea)
    {
      var t = (UsrCntrlPID)o;
      lock (t.RCCLock) t.ctrlI = t.CtrlI;
    }
    static void CtrlDCB(DependencyObject o, DependencyPropertyChangedEventArgs ea)
    {
      var t = (UsrCntrlPID)o;
      lock (t.RCCLock) t.ctrlD = t.CtrlD;
    }
    static void MaxCtrlParCB(DependencyObject o, DependencyPropertyChangedEventArgs ea)
    {
      var t = (UsrCntrlPID)o;
      lock (t.RCCLock) t.maxCtrlPar = t.MaxCtrlPar;
    }
    static void MinCtrlParCB(DependencyObject o, DependencyPropertyChangedEventArgs ea)
    {
      var t = (UsrCntrlPID)o;
      lock (t.RCCLock) t.minCtrlPar = t.MinCtrlPar;
    }
    static void GlobalGainCB(DependencyObject o, DependencyPropertyChangedEventArgs ea)
    {
      var t = (UsrCntrlPID)o;
      lock (t.RCCLock) t.globalGain = t.GlobalGain;
    }
    static void TimeConstantCB(DependencyObject o, DependencyPropertyChangedEventArgs ea)
    {
      var t = (UsrCntrlPID)o;
      lock (t.RCCLock) t.timeConstant = t.TimeConstant;
    }
    static void MaxIntegralErrorCB(DependencyObject o, DependencyPropertyChangedEventArgs ea)
    {
      var t = (UsrCntrlPID)o;
      lock (t.RCCLock) t.maxIntegralError = t.MaxIntegralError;
    }
    static void IsControlEnabledCB(DependencyObject o, DependencyPropertyChangedEventArgs ea)
    {
      var t = (UsrCntrlPID)o;
      bool b = t.IsControlEnabled;
      if (b) {
        t.State = EState.Enabled;
      } else {
        t.State = (t.State | EState.Disabled) & ~(EState.Enabled | EState.ReachedSetpoint);
      }
      lock (t.RCCLock) {
        if (b) {
          t.needInit = true;
          Log($"PID Enabled", Logger.Mode.NoAutoPoll);
          t.SetPointLostTime = DateTime.Now;
        } else {
          Log($"PID Disabled", Logger.Mode.NoAutoPoll);
          t.IsSPReached = false;
        }
        t.isControlEnabled = t.IsControlEnabled;
      }

      t.iM.ExpGoing = b;
      t.iC.ExpGoing = b;

    }
    static void IsRampEnabledCB(DependencyObject o, DependencyPropertyChangedEventArgs ea)
    {
      var t = (UsrCntrlPID)o;
      bool b = t.IsRampEnabled;
      lock (t.RCCLock) {
        if (b) t.resetRamp = true;
        t.isRampEnabled = b;
      }
    }
    static void IsSetPointReachedCB(DependencyObject o, DependencyPropertyChangedEventArgs ea)
    {
      var t = (UsrCntrlPID)o;
      if (t.IsControlEnabled) {
        t.State = t.IsSPReached ? EState.ReachedSetpoint : EState.Enabled;
      }
    }
    static void StateCB(DependencyObject o, DependencyPropertyChangedEventArgs ea)
    {
      var t = (UsrCntrlPID)o;
      switch (t.State) {
        case EState.Disabled:
          t.circle.Fill = Brushes.LightGray;
          break;
        case EState.Enabled:
          t.circle.Fill = Brushes.Yellow;
          break;
        case EState.ReachedSetpoint:
          t.circle.Fill = Brushes.LimeGreen;
          break;
        case EState.Error:
        default:
          t.circle.Fill = Brushes.Red;
          break;
      }
    }
    #endregion DPCallBacks
    #region DProperties
    [Category("Appearance")]
    public bool IsExpanded {
      get { return (bool)GetValue(IsExpandedProperty); }
      set { SetValue(IsExpandedProperty, value); }
    }
    [Category("Appearance")]
    public bool IsLogExpanded {
      get { return (bool)GetValue(IsLogExpandedProperty); }
      set { SetValue(IsLogExpandedProperty, value); }
    }

    [Category("PID")]
    public double SetPoint {
      get { return (double)GetValue(SetPointProperty); }
      set { SetValue(SetPointProperty, value); }
    }
    [Category("PID")]
    public double CRate {
      get { return (double)GetValue(CRateProperty); }
      set { SetValue(CRateProperty, value); }
    }
    [Category("PID")]
    public double CtrlP {
      get { return (double)GetValue(CtrlPProperty); }
      set { SetValue(CtrlPProperty, value); }
    }
    [Category("PID")]
    public double CtrlI {
      get { return (double)GetValue(CtrlIProperty); }
      set { SetValue(CtrlIProperty, value); }
    }
    [Category("PID")]
    public double CtrlD {
      get { return (double)GetValue(CtrlDProperty); }
      set { SetValue(CtrlDProperty, value); }
    }
    [Category("PID")]
    public double MaxCtrlPar {
      get { return (double)GetValue(MaxCtrlParProperty); }
      set { SetValue(MaxCtrlParProperty, value); }
    }
    [Category("PID")]
    public double MinCtrlPar {
      get { return (double)GetValue(MinCtrlParProperty); }
      set { SetValue(MinCtrlParProperty, value); }
    }
    [Category("PID")]
    public double GlobalGain {
      get { return (double)GetValue(GlobalGainProperty); }
      set { SetValue(GlobalGainProperty, value); }
    }
    [Category("PID")]
    public double TimeConstant {
      get { return (double)GetValue(TimeConstantProperty); }
      set { SetValue(TimeConstantProperty, value); }
    }
    [Category("PID")]
    public double MaxIntegralError {
      get { return (double)GetValue(MaxIntegralErrorProperty); }
      set { SetValue(MaxIntegralErrorProperty, value); }
    }

    public bool IsControlEnabled {
      get { return (bool)GetValue(IsControlEnabledProperty); }
      set { SetValue(IsControlEnabledProperty, value); }
    }
    [Category("PID")]
    public bool IsRampEnabled {
      get { return (bool)GetValue(IsRampEnabledProperty); }
      set { SetValue(IsRampEnabledProperty, value); }
    }

    [Category("PID")]
    public bool IsSPReached {
      get { return (bool)GetValue(IsSetPointReachedProperty); }
      protected set { SetValue(IsSetPointReachedKey, value); }
    }
    [Category("PID")]
    public EState State {
      get => (EState)GetValue(StateProperty);
      protected set => SetValue(StateKey, value);
    }
    #endregion DProperties
    #region IDisposable
    bool disposedValue;
    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue) {
        if (disposing) {
        }

        StopThread();
        CTS.Dispose();
        disposedValue = true;
      }
    }

    ~UsrCntrlPID()
    {
      Dispose(disposing: false);
    }
    public void Dispose()
    {
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }
    #endregion IDisposable

    public IMeasureParameter iM { get; set; }
    public IControlParameter iC { get; set; }

    Thread thread;
    static Logger logger = null;

    CancellationTokenSource CTS = new();

    DateTime ControlIterationStamp;

    object RCCLock = new();

    double ctrlP, ctrlI, ctrlD, globalGain, maxIntegralError, timeConstant;
    double setPoint, minCtrlPar, maxCtrlPar;
    double cRate;
    bool isControlEnabled, isRampEnabled;
    bool resetRamp = true;
    bool needInit = false;

    double controlParameter = 0.0;
    public double ControlParameter {
      get => Atomic.Read(ref controlParameter);
      set => Atomic.Write(ref controlParameter, value);
    }
    double measureParameter = double.NaN;
    public double MeasureParameter {
      get => Atomic.Read(ref measureParameter);
      set => Atomic.Write(ref measureParameter, value);
    }
    double imSetPoint = double.NaN;
    public double ImSetPoint {
      get => Atomic.Read(ref imSetPoint);
      set => Atomic.Write(ref imSetPoint, value);
    }
    Exception DispatcherException = null;
    void ControlCycle()
    {
      var cancellation = CTS.Token;
      errorint = 0.0;
      lasterror = 0.0;
      DateTime MeasurementStamp = DateTime.Now;
      double ierror = 0;
      double dtm;
      int averagecount = 0;
      int numerrors = 0;

      DispatcherOperation DO = null;

      try {
        while (!cancellation.IsCancellationRequested) {
          try {
            if (DispatcherException != null) {
              Exception e = DispatcherException;
              DispatcherException = null;
              throw e;
            }
            if (!isControlEnabled) {
              if ((DO?.Status ?? DispatcherOperationStatus.Completed) == DispatcherOperationStatus.Completed)
                DO = Dispatcher.BeginInvoke((Action)UpdateWithDll);

              cancellation.WaitHandle.WaitOne(10);
              continue;
            }

            if (needInit) {
              needInit = false;
              try {
                iM.Init();
                iC.Init();
                resetRamp = true;
                Log($"Devices Configured", Logger.Mode.NoAutoPoll);
              } catch (InvalidDeviceStateException e) {
                isControlEnabled = false;
                Dispatcher.BeginInvoke(() => { IsControlEnabled = false; });
                string err = $"Unable to initialize the PID controller";
                Log(ref err, e); Dispatcher.BeginInvoke(() => { MessageBox.Show(err, "Error"); });
                continue;
              }
            }

            double mp = iM.Measure();
            DateTime Now = DateTime.Now;
            dtm = (Now - MeasurementStamp).TotalSeconds;

            if (double.IsNaN(mp)) {
              if (++numerrors == 3) throw new InvalidDeviceStateException("K2400 not working properly or thermoresistor disconnected");
              Log("Problem with K2400 configuration\nTrying to Reset", Logger.Mode.Error);
              needInit = true;
              iM.Reset();
              Log("Waiting to Reset (2s)", Logger.Mode.Error);
              Thread.Sleep(2000);
              MeasurementStamp = Now;
              continue;
            } else { numerrors = 0; }

            double tc;
            lock (RCCLock) {
              if (isRampEnabled) {
                if (resetRamp) {
                  ImSetPoint = mp;
                  lasterror = null;
                  errorint = 0.0;
                  dtm = 0.0;
                  resetRamp = false;
                }

                if (ImSetPoint > setPoint) {
                  ImSetPoint -= cRate * dtm;
                  if (ImSetPoint < setPoint) ImSetPoint = setPoint;
                } else if (ImSetPoint < setPoint) {
                  ImSetPoint += cRate * dtm;
                  if (ImSetPoint > setPoint) ImSetPoint = setPoint;
                }
              } else {
                if (ImSetPoint != setPoint)
                  lasterror = null;
                ImSetPoint = setPoint;
              }
              tc = timeConstant;
            }

            double error = mp - ImSetPoint;
            ierror += error * dtm;
            MeasurementStamp = Now;

            averagecount++; // for debug

            double dti = (Now - ControlIterationStamp).TotalSeconds;
            if (dti >= tc) {
              if (double.IsNaN(ierror)) ierror = 0;
              ControlIteration(ierror / dti, tc);
              if ((DO?.Status ?? DispatcherOperationStatus.Completed) == DispatcherOperationStatus.Completed)
                DO = Dispatcher.BeginInvoke((Action)OnCycleDispatcherUpdate);
              ControlIterationStamp = Now;
              ierror = 0;
              averagecount = 0;
            }
          } catch (Exception e) when (e is not ThreadInterruptedException) {
            Logger.Default.log("PID Control Exception", e, Logger.Mode.Error, DeviceName);
            iM.Reset();
            iC.Reset();
            Dispatcher.Invoke(() => { IsControlEnabled = false; });
          }
        }
        Logger.Default.log("PID Thread Cancelled", Logger.Mode.Full, DeviceName);
      } catch (ThreadInterruptedException ae) {
        Logger.Default.log("PID Thread Aborted", ae, Logger.Mode.Error, DeviceName);
      }
    }
    float pbandw = 0.0f, ibandw = 0.0f, dbandw = 0.0f;
    void OnCycleDispatcherUpdate()
    {
      try {
        utbMP.Value = MeasureParameter;
        utbISP.Value = ImSetPoint;
        if (pbandw >= 0) {
          rPBandP.Rect = new Rect(0, 0, pbandw, 1);
          rPBandN.Rect = new Rect(1, 0, 0, 1);
        } else {
          rPBandP.Rect = new Rect(0, 0, 0, 1);
          rPBandN.Rect = new Rect(1 + pbandw, 0, -pbandw, 1);
        }
        if (ibandw >= 0) {
          rIBandP.Rect = new Rect(0, 0, ibandw, 1);
          rIBandN.Rect = new Rect(1, 0, 0, 1);
        } else {
          rIBandP.Rect = new Rect(0, 0, 0, 1);
          rIBandN.Rect = new Rect(1 + ibandw, 0, -ibandw, 1);
        }
        if (dbandw >= 0) {
          rDBandP.Rect = new Rect(0, 0, dbandw, 1);
          rDBandN.Rect = new Rect(1, 0, 0, 1);
        } else {
          rDBandP.Rect = new Rect(0, 0, 0, 1);
          rDBandN.Rect = new Rect(1 + dbandw, 0, -dbandw, 1);
        }
        double cp = (pbandw + ibandw + dbandw) * GlobalGain / MaxCtrlPar;
        rSetP.Rect = new Rect(0, 0, cp > 0 ? cp : 0, 1);

        UpdateWithDll();
        UpdateSetPointReached();
      }catch(Exception e) {
        DispatcherException = e;
        return;
      }
    }
    const float pbandC = 0.005f;
    const float dbandC = 0.001f;

    DateTime SetPointLostTime;
    public TimeSpan SetPointReachTime { get; set; } = TimeSpan.FromMinutes(5);
    void UpdateSetPointReached()
    {
      bool oldispr = IsSPReached;
      bool newispr;
      if (oldispr) {
        if (Math.Abs(pbandw) < 2 * pbandC && Math.Abs(dbandw) < 2 * dbandC) newispr = true;
        else newispr = false;
      } else {
        if (Math.Abs(pbandw) < pbandC && Math.Abs(dbandw) < dbandC) newispr = true;
        else newispr = false;
      }
      if (newispr && !oldispr) {
        IsSPReached = true;
      } else if (!newispr && oldispr) {
        IsSPReached = false;
        SetPointLostTime = DateTime.Now;
      } else if (!newispr && !oldispr) {
        if (DateTime.Now - SetPointLostTime > SetPointReachTime) {
          State |= EState.Error;
          throw new TimeoutException(
            $"PID could not reach Setpoint ({SetPoint} °C)\n" +
            $"after Period of ({SetPointReachTime})");
        }
      }
    }
    public event EventHandler<UpdateVIsEA> UpdateVIs;
    public void UpdateWithDll()
    {

      double sp = TECPIDdll.DLL.GetSetPointOncePerChange();
      if (!double.IsNaN(sp)) SetPoint = sp;
      TECPIDdll.DLL.SetTemperature(MeasureParameter);

      var set = MainSettings.Default;

      TECPIDdll.DLL.GetVIOnce(1, out double v1, out double i1);
      TECPIDdll.DLL.GetVIOnce(2, out double v2, out double i2);

      UpdateVIs?.Invoke(this, new() {
        V1 = v1.NaNToNull(),
        V2 = v2.NaNToNull(),
        I1 = i1.NaNToNull(),
        I2 = i2.NaNToNull()
      });
    }
    double errorint = 0.0;
    double? lasterror = null;

    void ControlIteration(double error, double dt)
    {
      errorint += error * dt;
      double errorder = (error - lasterror ?? error) / dt;
      lasterror = error;
      double output;

      MeasureParameter = error + ImSetPoint;

      lock (RCCLock) {
        GUtils.Limit(ref errorint, -maxIntegralError * ctrlI, +maxIntegralError * ctrlI);

        output = globalGain * /*Math.Sqrt*/(error / ctrlP + errorint / ctrlI + errorder / ctrlD);
        pbandw = (float)(error / ctrlP);
        ibandw = (float)(errorint / ctrlI);
        dbandw = (float)(errorder / ctrlD);
        GUtils.Limit(ref output, minCtrlPar, maxCtrlPar);
      }
      ControlParameter = output;
      iC.Control(output);
    }
    public void ResetRamp()
    {
      resetRamp = true;
      Log($"PID is Reset", Logger.Mode.NoAutoPoll);
    }
    public static string DeviceName => "PID-C";
    public UsrCntrlPID()
    {
      if (logger == null) logger = Logger.Default;
      logger?.AttachLog(DeviceName, AddToLog, Logger.Mode.NoAutoPoll);

      InitializeComponent();

      title.Text = DeviceName;

      thread = new(ControlCycle);

      ControlIterationStamp = DateTime.Now;
    }
    void AddToLog(object s, Logger.LogFeedBEA e) => tbLog.Text += ">" + e.Message + "\n";

    static void Log(string txt, Logger.Mode mode) => logger?.log(txt, mode, DeviceName);
    static void Log(ref string txt, Exception e) => logger?.log(ref txt, e, Logger.Mode.Error, DeviceName);
    public void Init(IMeasureParameter im, IControlParameter ic)
    {
      iM = im; iC = ic;

      if (thread.IsAlive) StopThread();
      IsControlEnabled = false;

      Log($"PID Initialized.\nMeasure Device\n{im}\nControl Device\n{ic}", Logger.Mode.AppState);

      thread.Start();

      OnCycleDispatcherUpdate();
    }
    void StopThread()
    {
      CTS.Cancel();
      if (!thread.Join(10)) {
        thread.Interrupt();
        thread.Join();
      }
    }
    void tbLog_TextChanged(object sender, TextChangedEventArgs e)
    {
      TextBox tb = (TextBox)sender;
      if (tb.Text.Length > 5000)
        tb.Text = "<Log trimmed>" + tb.Text.Substring(tb.Text.IndexOf('\n', 1000));
      tb.ScrollToEnd();
    }

    void ToggleButton_C_U(object sender, RoutedEventArgs e)
    {
      ToggleButton tb = (ToggleButton)sender;
    }
    void bRRamp_Click(object sender, RoutedEventArgs e) => ResetRamp();

    public void SetCurrentTemperature(double t)
    {
      if (IsControlEnabled) return;

      utbMP.Value = t;
      utbISP.Value = t;

      lock (RCCLock) {
        ImSetPoint = double.NaN;
        resetRamp = true;
      }
    }
    public class UpdateVIsEA : EventArgs
    {
      public double? V1 { get; init; } = null;
      public double? I1 { get; init; } = null;
      public double? V2 { get; init; } = null;
      public double? I2 { get; init; } = null;
    }
  }

  public class TempSensorInterface : IMeasureParameter
  {
    TempSensor TC;
    public TempSensorInterface(TempSensor tc) => TC = tc;

    public void Init() => TC.ScheduleInit();
    public void Reset() => TC.KC.KD.ScheduleReset();

    public double Measure()
    {
      if (!TC.KC.KD.IsConnected) throw new DeviceDisconnectedException(
        $"Error: {nameof(TempSensorInterface)} is disconnected");
      return TC.ReadTemperature();
    }
    public bool ExpGoing {
      get => TC.KC.KD.IsControlled;
      set => TC.KC.KD.IsControlled = value;
    }
  }

  public class GWIPSControlInterface : IControlParameter
  {
    UsrCntrlGWPS ucGWPS;

    public GWIPSControlInterface(UsrCntrlGWPS ucgwps) => ucGWPS = ucgwps;

    public void Control(double parameter)
    {
      if (!ucGWPS.GWPS.IsConnected) throw new DeviceDisconnectedException(
        $"Error: {nameof(GWIPSControlInterface)} is disconnected");
      ucGWPS.GWPS.ScheduleSetI(parameter);
      ucGWPS.GWPS.ScheduleUpdateI();
      ucGWPS.GWPS.ScheduleUpdateV();
    }

    public void Init()
    {
      if (!ucGWPS.GWPS.IsConnected)
        throw new DeviceDisconnectedException(
          $"{nameof(ucGWPS.GWPS)}GWIPS Device should be connected.\n" +
          $"Correct port should be selected.");

      ucGWPS.Dispatcher.Invoke(ucGWPS.SetUpCommand);
      if (!ucGWPS.GWPS.Output) ucGWPS.GWPS.ScheduleTurnOn();

    }
    public void Reset()
    {
      ucGWPS.GWPS.ScheduleSetI(0.0);
      ucGWPS.GWPS.ScheduleTurnOff();
    }

    public bool ExpGoing {
      get => ucGWPS.GWPS.IsControlled;
      set => ucGWPS.GWPS.IsControlled = value;
    }
  }
}