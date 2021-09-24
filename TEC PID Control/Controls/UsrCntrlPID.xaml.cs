using CSUtils;
using Devices.Keithley;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TEC_PID_Control.PID;


namespace TEC_PID_Control.PID
{
  public interface IMeasureParameter
  {
    void Init();
    double Measure();
  }
  public interface IControlParameter
  {
    void Init();
    void Control(double parameter);
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
        DependencyProperty.Register(nameof(SetPoint), typeof(double), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(1.0, CtrlChanged) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty MeasureParameterProperty =
        DependencyProperty.Register(nameof(MeasureParameter), typeof(double), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(1.0) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty ControlParameterProperty =
        DependencyProperty.Register(nameof(ControlParameter), typeof(double), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(1.0) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty CtrlPProperty =
        DependencyProperty.Register(nameof(CtrlP), typeof(double), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(1.0, CtrlChanged) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty CtrlIProperty =
        DependencyProperty.Register(nameof(CtrlI), typeof(double), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(1.0, CtrlChanged) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty CtrlDProperty =
        DependencyProperty.Register(nameof(CtrlD), typeof(double), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(1.0, CtrlChanged) { BindsTwoWayByDefault = true });

    public static readonly DependencyProperty MaxCtrlParProperty =
    DependencyProperty.Register(nameof(MaxCtrlPar), typeof(double), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(1.0, CtrlChanged) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty MinCtrlParProperty =
        DependencyProperty.Register(nameof(MinCtrlPar), typeof(double), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(1.0, CtrlChanged) { BindsTwoWayByDefault = true });


    public static readonly DependencyProperty GlobalGainProperty =
        DependencyProperty.Register(nameof(GlobalGain), typeof(double), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(1.0, CtrlChanged) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty TimeConstantProperty =
        DependencyProperty.Register(nameof(TimeConstant), typeof(double), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(1.0, CtrlChanged) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty MaxIntegralErrorProperty =
        DependencyProperty.Register(nameof(MaxIntegralError), typeof(double), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(1.0, CtrlChanged) { BindsTwoWayByDefault = true });
    public static readonly DependencyProperty IsControlEnabledProperty =
        DependencyProperty.Register(nameof(IsControlEnabled), typeof(bool), typeof(UsrCntrlPID), new FrameworkPropertyMetadata(false, CtrlChanged) { BindsTwoWayByDefault = true });
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
    public double MeasureParameter {
      get { return (double)GetValue(MeasureParameterProperty); }
      set { SetValue(MeasureParameterProperty, value); }
    }
    [Category("PID")]
    public double ControlParameter {
      get { return (double)GetValue(ControlParameterProperty); }
      set { SetValue(ControlParameterProperty, value); }
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

    public IMeasureParameter iM { get; set; }
    public IControlParameter iC { get; set; }

    Thread thread;

    CancellationTokenSource CTS = new();

    DateTime ControlIterationStamp;

    object ResetControlCycleLock = new();

    double ctrlP, ctrlI, ctrlD, globalGain, maxIntegralError, timeConstant;
    double setPoint, minCtrlPar, maxCtrlPar;
    bool isControlEnabled;
    static void CtrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      UsrCntrlPID t = (UsrCntrlPID)d;
      lock (t.ResetControlCycleLock) {
        t.isControlEnabled = t.IsControlEnabled;
        t.ctrlP = t.CtrlP;
        t.ctrlI = t.CtrlI;
        t.ctrlD = t.CtrlD;
        t.globalGain = t.GlobalGain;
        t.maxIntegralError = t.MaxIntegralError;
        t.timeConstant = t.TimeConstant;
        t.setPoint = t.SetPoint;
        t.minCtrlPar = t.MinCtrlPar;
        t.maxCtrlPar = t.MaxCtrlPar;
      }
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

      try {
        iM.Init();
        iC.Init();

        while (!cancellation.IsCancellationRequested) {
          if (!isControlEnabled) {
            cancellation.WaitHandle.WaitOne(10);
            continue;
          }

          double sp;
          lock (ResetControlCycleLock) sp = setPoint;
          double error = iM.Measure() - sp;
          DateTime Now = DateTime.Now;
          dtm = (Now - MeasurementStamp).TotalSeconds;
          ierror += error * dtm;
          MeasurementStamp = Now;

          averagecount++; // for debug

          double dti = (Now - ControlIterationStamp).TotalSeconds;
          double tc;
          lock (ResetControlCycleLock) tc = timeConstant;
          if (dti >= tc) {
            if (double.IsNaN(ierror)) ierror = 0;
            ControlIteration(ierror / dti, tc);

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

    double errorint = 0.0;
    double lasterror = 0.0;

    void ControlIteration(double error, double dt)
    {
      errorint += error * dt;
      double errorder = (error - lasterror) / dt;
      lasterror = error;
      double output;
      lock (ResetControlCycleLock) {
        GUtils.Bound(ref errorint, -maxIntegralError, +maxIntegralError);

        output = globalGain * (error / ctrlP + errorint / ctrlI + errorder / ctrlD);

        GUtils.Bound(ref output, minCtrlPar, maxCtrlPar);
      }
      iC.Control(output);
    }

    public UsrCntrlPID()
    {
      InitializeComponent();

      thread = new(ControlCycle);

      ControlIterationStamp = DateTime.Now;
    }
    public void Init(IMeasureParameter im, IControlParameter ic)
    {
      iM = im; iC = ic;

      if (thread.IsAlive) StopThread();
      IsControlEnabled = false;

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

    void ToggleButton_Checked(object sender, RoutedEventArgs e)
    {

    }
  }

  public class TempSensorInterface : IMeasureParameter
  {
    TempSensor TC;
    public TempSensorInterface(TempSensor tc) => TC = tc;

    public void Init() => TC.ScheduleInit();

    public double Measure() => TC.ReadTemperature();
  }

  public class GWIPSControlInterface : IControlParameter
  {
    UsrCntrlGWPS ucGWPS;

    public GWIPSControlInterface(UsrCntrlGWPS ucgwps) => ucGWPS = ucgwps;

    public void Control(double parameter) => ucGWPS.GWPS.ScheduleSetI(parameter);

    public void Init()
    {
      ucGWPS.Dispatcher.Invoke(ucGWPS.SetUpCommand);
      if (!ucGWPS.GWPS.Output) ucGWPS.GWPS.ScheduleTurnOn();
    }
  }
}
