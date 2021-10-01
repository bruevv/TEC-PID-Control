using CSUtils;
using Devices;
using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
//using System.Windows.Threading;
using TEC_PID_Control.PID;


namespace TEC_PID_Control.PID
{
  public interface IMeasureParameter
  {
    void Init();
    double Measure();

    bool ExpGoing { get; set; }
  }
  public interface IControlParameter
  {
    void Init();
    void Control(double parameter);
    bool ExpGoing { get; set; }
  }
}

namespace TEC_PID_Control.Controls
{
  public partial class UsrCntrlPID : UserControl, IDisposable
  {
    #region Binding
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
    static void SetPointCB(DependencyObject o, DependencyPropertyChangedEventArgs ea)
    {
      var t = (UsrCntrlPID)o;
      lock (t.RCCLock) t.setPoint = t.SetPoint;
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
      if (t.isControlEnabled != b) {
        lock (t.RCCLock) {
          if (b) {
            t.needInit = true;
            logger?.log($"PID Enabled", Logger.Mode.NoAutoPoll, nameof(UsrCntrlPID));
          } else {
            logger?.log($"PID Disabled",  Logger.Mode.NoAutoPoll, nameof(UsrCntrlPID));
          }
          t.isControlEnabled = t.IsControlEnabled;
        }
      }
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

    #endregion Binding
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
    //[Category("PID")]
    //public double MeasureParameter {
    //  get { return (double)GetValue(MeasureParameterProperty); }
    //  set { SetValue(MeasureParameterProperty, value); }
    //}
    //[Category("PID")]
    //public double ControlParameter {
    //  get { return (double)GetValue(ControlParameterProperty); }
    //  set { SetValue(ControlParameterProperty, value); }
    //}
    //[Category("PID")]
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
    double measureParameter = 0.0;
    public double MeasureParameter {
      get => Atomic.Read(ref measureParameter);
      set => Atomic.Write(ref measureParameter, value);
    }
    double imSetPoint = 0.0;
    public double ImSetPoint {
      get => Atomic.Read(ref imSetPoint);
      set => Atomic.Write(ref imSetPoint, value);
    }
    void ControlCycle()
    {
      var cancellation = CTS.Token;
      errorint = 0.0;
      lasterror = 0.0;
      DateTime MeasurementStamp = DateTime.Now;
      double ierror = 0;
      double dtm;
      int averagecount = 0;

      DispatcherOperation DO = null;

      try {

        while (!cancellation.IsCancellationRequested) {
          if (!isControlEnabled) {
            cancellation.WaitHandle.WaitOne(10);
            continue;
          }

          if (needInit) {
            needInit = false;
            try {
              iM.Init();
              iC.Init();
              logger?.log($"Devices Configured", Logger.Mode.NoAutoPoll, nameof(UsrCntrlPID));
            } catch (InvalidDeviceStateException e) {
              isControlEnabled = false;
              Dispatcher.BeginInvoke(() => { IsControlEnabled = false; });
              string err = $"Unable to initialize the PID controller";
              logger?.log(ref err, e, Logger.Mode.Error, nameof(UsrCntrlPID));
              MessageBox.Show(err, "Error");
              continue;
            }
          }

          double mp = iM.Measure();
          DateTime Now = DateTime.Now;
          dtm = (Now - MeasurementStamp).TotalSeconds;

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
          }

          double error = mp - ImSetPoint;
          ierror += error * dtm;
          MeasurementStamp = Now;

          averagecount++; // for debug

          double dti = (Now - ControlIterationStamp).TotalSeconds;
          double tc;
          lock (RCCLock) tc = timeConstant;
          if (dti >= tc) {
            if (double.IsNaN(ierror)) ierror = 0;
            ControlIteration(ierror / dti, tc);
            if ((DO?.Status ?? DispatcherOperationStatus.Completed) == DispatcherOperationStatus.Completed)
              DO = Dispatcher.BeginInvoke((Action)OnCycleInterfaceUpdate);
            ControlIterationStamp = Now;
            ierror = 0;
            averagecount = 0;
          }
        }
        Logger.Default.log("PID Thread Cancelled", Logger.Mode.Full, nameof(UsrCntrlPID));
      } catch (ThreadInterruptedException ae) {
        Logger.Default.log("PID Thread Aborted", ae, Logger.Mode.Error, nameof(UsrCntrlPID));
      } catch (Exception e) {
        Logger.Default.log("PID Thread Exception", e, Logger.Mode.Error, nameof(UsrCntrlPID));
      }
    }
    void OnCycleInterfaceUpdate()
    {
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
      double cp = pbandw + ibandw + dbandw;
      rSetP.Rect = new Rect(0, 0, cp > 0 ? cp : 0, 1);
    }
    float pbandw = 0.0f, ibandw = 0.0f, dbandw = 0.0f;

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
        GUtils.Bound(ref errorint, -maxIntegralError * ctrlI, +maxIntegralError * ctrlI);

        output = globalGain * /*Math.Sqrt*/(error / ctrlP + errorint / ctrlI + errorder / ctrlD);
        pbandw = (float)(error / ctrlP);
        ibandw = (float)(errorint / ctrlI);
        dbandw = (float)(errorder / ctrlD);
        GUtils.Bound(ref output, minCtrlPar, maxCtrlPar);
      }
      ControlParameter = output;
      iC.Control(output);
    }
    public void ResetRamp() => resetRamp = true;
    public UsrCntrlPID()
    {
      if (logger == null) logger = Logger.Default;
      logger?.AttachLog(nameof(UsrCntrlPID), AddToLog, Logger.Mode.NoAutoPoll);

      InitializeComponent();

      thread = new(ControlCycle);

      ControlIterationStamp = DateTime.Now;
    }
    void AddToLog(object s, Logger.LogFeedBEA e) => tbLog.Text += ">" + e.Message + "\n";

    public void Init(IMeasureParameter im, IControlParameter ic)
    {
      iM = im; iC = ic;

      if (thread.IsAlive) StopThread();
      IsControlEnabled = false;

      logger?.log($"PID Initialized.\nMeasure Parameter\n{im}\nControl Parameter\n{ic}",
         Logger.Mode.AppState, nameof(UsrCntrlPID));

      thread.Start();
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
      iM.ExpGoing = tb.IsChecked ?? false;
      iC.ExpGoing = tb.IsChecked ?? false;
    }
    void bRRamp_Click(object sender, RoutedEventArgs e) => ResetRamp();
  }

  public class TempSensorInterface : IMeasureParameter
  {
    TempSensor TC;
    public TempSensorInterface(TempSensor tc) => TC = tc;

    public void Init() => TC.ScheduleInit();

    public double Measure() => TC.ReadTemperature();
    public bool ExpGoing {
      get => TC.KC.KD.IsExperimentOn;
      set => TC.KC.KD.IsExperimentOn = value;
    }
  }

  public class GWIPSControlInterface : IControlParameter
  {
    UsrCntrlGWPS ucGWPS;

    public GWIPSControlInterface(UsrCntrlGWPS ucgwps) => ucGWPS = ucgwps;

    public void Control(double parameter)
    {
      ucGWPS.GWPS.ScheduleSetI(parameter);
      ucGWPS.GWPS.ScheduleUpdateI();
      ucGWPS.GWPS.ScheduleUpdateV();
    }

    public void Init()
    {
      if (!ucGWPS.GWPS.IsConnected)
        throw new DeviceDisconnectedException(
          "GWIPS Device should be connected.\n" +
          "Correct port should be selected.\n");

      ucGWPS.Dispatcher.Invoke(ucGWPS.SetUpCommand);
      if (!ucGWPS.GWPS.Output) ucGWPS.GWPS.ScheduleTurnOn();

    }

    public bool ExpGoing {
      get => ucGWPS.GWPS.IsExperimentOn;
      set => ucGWPS.GWPS.IsExperimentOn = value;
    }
  }
}
