using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TEC_PID_Control.Properties
{
  using CSSettings;
  using CSUtils;
  using WPFControls;

  [SharedProperty]
  public class Settings : GenericSB<Settings>
  {
    bool firstRun = true;
    bool showHelpMarkers = true, showToolTips = true;
    string logFile = "<Default>";
    Logger.Mode logMode = Logger.Mode.AppState;
    MainInterface mI = new();

    PIDSettings pID = new();
    GWPSS gwps1 = new() { Channel = GWPSChannel.A };
    GWPSS gwps2 = new() { Channel = GWPSChannel.B };
    K2400S k2400s = new();

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool FirstRun { get => firstRun; set => ChangeProperty(ref firstRun, value); }

    [Display(Name = "Show Help Markers", Description = "Show blue circles where Help-ToolTips are available")]
    public bool ShowHelpMarkers { get => showHelpMarkers; set => ChangeProperty(ref showHelpMarkers, value); }
    [Display(Name = "Show Help ToolTips", Description = "Show Help-ToolTips where they are available")]
    public bool ShowToolTips { get => showToolTips; set => ChangeProperty(ref showToolTips, value); }

    [Display(Name = "Log Mode", Description = "Level of Logger (log file only). Requires restart.")]
    public Logger.Mode LogMode { get => logMode; set => ChangeProperty(ref logMode, value); }
    [Display(Name = "Log File", Description = "Relative or absolute Path of log file " +
      "(with filename and extention). Requires restart.")]
    public string LogFile { get => logFile; set => ChangeProperty(ref logFile, value); }

    public PIDSettings PID { get => pID; set => ChangeProperty(ref pID, value); }

    [Display(Name = "Keithley 2400 Source/Meter", Description = "Settings of Keithley 2400 Control")]
    public K2400S K2400 { get => k2400s; set => ChangeProperty(ref k2400s, value); }

    [DisplayName("GWPS Virtual Channel 1")]
    public GWPSS GWPS1 { get => gwps1; set => ChangeProperty(ref gwps1, value); }

    [DisplayName("GWPS Virtual Channel 2")]
    public GWPSS GWPS2 { get => gwps2; set => ChangeProperty(ref gwps2, value); }

    [DisplayName("Main Interface")]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public MainInterface Interface { get => mI; set => ChangeProperty(ref mI, value); }

    public class MainInterface : GenericSB<MainInterface>
    {
      int width = 600, height = 600;
      double opacity = 0.9, hmOpacity = 0.2;

      [Range(330, int.MaxValue)]
      public int WindowWidth { get => width; set => ChangeProperty(ref width, value); }

      [Range(350, int.MaxValue)]
      public int WindowHeight { get => height; set => ChangeProperty(ref height, value); }

      [UnitBox(typeof(UnitPercent), "%", 0.3, 1.0, format: "N1")]
      [DisplayName("Main Window Opacity")]
      public double WindowOpacity { get => opacity; set => ChangeProperty(ref opacity, value); }
      [UnitBox(typeof(UnitPercent), "%", 0.0, 1.0, format: "N1")]
      [DisplayName("Help Markers Opacity")]
      public double HelpMarkersOpacity { get => hmOpacity; set => ChangeProperty(ref hmOpacity, value); }
    }
  }

  public enum GWPSChannel : int
  {
    [Description("Channel A - Right")] A = 1,
    [Description("Channel B - Left")] B = 2
  }
  public class GWPSS : GenericSB<GWPSS>
  {
    double gOutputVoltage = 1, gWPSOutputCurrent = 0.1;
    GWPSChannel gWPSChannel = GWPSChannel.A;
    bool autoConnect = true, gWPSAutoPoll = true;
    string gWPSPort = "COM3";
    GWPSInterface gwpsInterface = new();

    [Description("'Output Voltage' of GWI Power Supply in CV mode or maximum " +
      "'Output Voltage' in CC mode"), DisplayName("Output Voltage")]
    [UnitBox(typeof(UnitVoltage), "V", 0, 32)]
    public double OutputVoltage { get => gOutputVoltage; set => ChangeProperty(ref gOutputVoltage, value); }

    [Description("'Output Current' of GWI Power Supply in CC mode or maximum " +
      "'Output Current' in CV mode")]
    [DisplayName("Output Current"), UnitBox(typeof(UnitCurrent), "A", 0, 3.2)]
    public double OutputCurrent { get => gWPSOutputCurrent; set => ChangeProperty(ref gWPSOutputCurrent, value); }

    [Description("Channel mapping of this 'virtual' GWI Power Supply device")]
    [DisplayName("GWPS Channel")]
    public GWPSChannel Channel { get => gWPSChannel; set => ChangeProperty(ref gWPSChannel, value); }

    [Description("Enable poling of GWI Power Supply when it is idle")]
    [DisplayName("Auto Poll GWPS"), SharedProperty]
    public bool AutoPoll { get => gWPSAutoPoll; set => ChangeProperty(ref gWPSAutoPoll, value); }

    [Description("COM port name of GWPS. Port names normally maintained for particular USB port/hub connection.")]
    [DisplayName("GWPS COM-Port"), SharedProperty]
    public string Port { get => gWPSPort; set => ChangeProperty(ref gWPSPort, value); }

    [Description("Automatically connect to the device at startup")]
    [DisplayName("Auto Connect GWPS"), SharedProperty]
    public bool AutoConnect { get => autoConnect; set => ChangeProperty(ref autoConnect, value); }

    [Display(Name = "Interface Settings", Description = "These sttings only affect the interface " +
      "and do not affect measurements"), EditorBrowsable(EditorBrowsableState.Advanced)]
    public GWPSInterface Interface { get => gwpsInterface; set => ChangeProperty(ref gwpsInterface, value); }
  }
  public class GWPSInterface : GenericSB<GWPSInterface>
  {
    bool isExpanded = false, logExpanded = false;
    Unit uOC = new UnitCurrent("A");
    Unit uOV = new UnitVoltage("V");
    Unit uMC = new UnitCurrent("A");
    Unit uMV = new UnitVoltage("V");
    Unit uR = new UnitResistance("Ohm");
    Unit uW = new UnitPower("W");
    string fOC = "G4", fOV = "G4", fMC = "G4", fMV = "G4", fR = "G4", fW = "G4";

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool LogExpanded { get => logExpanded; set => ChangeProperty(ref logExpanded, value); }
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool IsExpanded { get => isExpanded; set => ChangeProperty(ref isExpanded, value); }

    public Unit OCUnit { get => uOC; set => ChangeProperty(ref uOC, value); }
    public Unit OVUnit { get => uOV; set => ChangeProperty(ref uOV, value); }
    public Unit MCUnit { get => uMC; set => ChangeProperty(ref uMC, value); }
    public Unit MVUnit { get => uMV; set => ChangeProperty(ref uMV, value); }
    public Unit RUnit { get => uR; set => ChangeProperty(ref uR, value); }
    public Unit WUnit { get => uW; set => ChangeProperty(ref uW, value); }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public string OCFormat { get => fOC; set => ChangeProperty(ref fOC, value); }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public string OVFormat { get => fOV; set => ChangeProperty(ref fOV, value); }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public string MCFormat { get => fMC; set => ChangeProperty(ref fMC, value); }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public string MVFormat { get => fMV; set => ChangeProperty(ref fMV, value); }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public string RFormat { get => fR; set => ChangeProperty(ref fR, value); }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public string WFormat { get => fW; set => ChangeProperty(ref fW, value); }
  }

  public class K2400S : GenericSB<K2400S>
  {
    double outputCurrent = 0.0001, outputVoltage = 1.0;
    string port = "COM1";
    bool autoPoll = true, autoConnect = true;
    K2400Interface xinterface = new();

    [Display(Name = "Output Voltage", Description = "Output Voltage is Voltage Source mode, or " +
      "Voltage compliance limit in Current Source mode")]
    [UnitBox(typeof(UnitVoltage), "V", -210, 210)]
    public double OutputVoltage { get => outputVoltage; set => ChangeProperty(ref outputVoltage, value); }

    [Display(Name = "Output Current", Description = "Output Current is Current Source mode, or " +
      "Current compliance limit in Voltage Source mode")]
    [UnitBox(typeof(UnitCurrent), "mA", -1.05, 1.05)]
    public double OutputCurrent { get => outputCurrent; set => ChangeProperty(ref outputCurrent, value); }

    [Display(Name = "Keithley 2400 COM-Port", Description = "COM port name of Keithley 2400. Port" +
      " Names normally maintained for particular USB port/hub connection.")]
    public string Port { get => port; set => ChangeProperty(ref port, value); }

    [Display(Name = "Automatic Polling", Description = "If Enabled, Keithley 2400 will " +
      "automatically perform aquisition when Idle (no experiment/control active)")]
    public bool AutoPoll { get => autoPoll; set => ChangeProperty(ref autoPoll, value); }

    [Display(Name = "Auto Connect", Description = "Automatically connect to the device at startup")]
    public bool AutoConnect { get => autoConnect; set => ChangeProperty(ref autoConnect, value); }

    [Display(Name = "Interface Settings", Description = "These sttings only affect the interface " +
      "and do not affect measurements"), EditorBrowsable(EditorBrowsableState.Advanced)]
    public K2400Interface Interface {
      get => xinterface; set => ChangeProperty(ref xinterface, value);
    }
  }
  public class K2400Interface : GenericSB<K2400Interface>
  {
    bool isExpanded = false, logExpanded = false;
    Unit uOC = new UnitCurrent("uA");
    Unit uOV = new UnitVoltage("V");
    Unit uMC = new UnitCurrent("uA");
    Unit uMV = new UnitVoltage("V");
    Unit uR = new UnitResistance("kOhm");
    string fOC = "N5", fOV = "N5", fMC = "N5", fMV = "N5", fR = "N5";

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool LogExpanded { get => logExpanded; set => ChangeProperty(ref logExpanded, value); }
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool IsExpanded { get => isExpanded; set => ChangeProperty(ref isExpanded, value); }

    public Unit OCUnit { get => uOC; set => ChangeProperty(ref uOC, value); }
    public Unit OVUnit { get => uOV; set => ChangeProperty(ref uOV, value); }
    public Unit MCUnit { get => uMC; set => ChangeProperty(ref uMC, value); }
    public Unit MVUnit { get => uMV; set => ChangeProperty(ref uMV, value); }
    public Unit RUnit { get => uR; set => ChangeProperty(ref uR, value); }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public string OCFormat { get => fOC; set => ChangeProperty(ref fOC, value); }
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public string OVFormat { get => fOV; set => ChangeProperty(ref fOV, value); }
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public string MCFormat { get => fMC; set => ChangeProperty(ref fMC, value); }
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public string MVFormat { get => fMV; set => ChangeProperty(ref fMV, value); }
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public string RFormat { get => fR; set => ChangeProperty(ref fR, value); }
  }

  public class PIDSettings : GenericSB<PIDSettings>
  {
    double ctrlP = 5.0, ctrlI = 40.0, ctrlD = 3.0;
    double maxCtrlPar = 2.8, minCtrlPar = 0, globalGain = 2.8;
    double timeConstant = 0.3, maxIntegralError = 1, cRate = 0.1;
    double pBandCin = 0.005, pBandCout = 0.01;
    uint sPReachedFilter = 10;
    double setPointReachTime = 5.0*60.0;
    bool rampEnable = true;
    PIDInterface pidInterface = new();

    [Display(Name = "P-Band", Description = "Proportional band of PID regulator in units of " +
      "temperature. If P-band is 1°C, P-component of control parameter will be 100% at 1°C error")]
    [UnitBox(typeof(UnitRelTemperature), "°C", 1e-3, double.MaxValue)]
    public double CtrlP { get => ctrlP; set => ChangeProperty(ref ctrlP, value); }

    [Display(Name = "I-Band", Description = "Integral band of PID regulator in units of " +
     "temperature multiplied on time. If I-band is 1°C*m, I-component of control parameter will " +
      "be 100% at 1°C error accumulated over 1 minute")]
    [UnitBox(typeof(UnitBandTempConS), "°C*s", 1e-3, double.MaxValue)]
    public double CtrlI { get => ctrlI; set => ChangeProperty(ref ctrlI, value); }

    [Display(Name = "D-Band", Description = "Differential (D) band of PID regulator in units of " +
      "temperature divided by time. If D-band is 1°C/s, D-component of control parameter will " +
      "be 100% when error has increased on 1°C over 1 second")]
    [UnitBox(typeof(UnitBandTempCperS), "°C/s", 1e-3, double.MaxValue)]
    public double CtrlD { get => ctrlD; set => ChangeProperty(ref ctrlD, value); }

    [Display(Name = "Maximum Current", Description = "Current limit of PID controller:" +
     "control current will not be higher than this value")]
    [UnitBox(typeof(UnitCurrent), "A")]
    public double MaxCtrlPar { get => maxCtrlPar; set => ChangeProperty(ref maxCtrlPar, value); }

    [Display(Name = "Minimum Current", Description = "The minimum PID control current. Normally" +
      "it should be set to 0 in unipolar controller")]
    [UnitBox(typeof(UnitCurrent), "A")]
    public double MinCtrlPar { get => minCtrlPar; set => ChangeProperty(ref minCtrlPar, value); }

    [Display(Name = "PID Gain", Description = "This current will be set for PID control parameter calculated at 100%")]
    [UnitBox(typeof(UnitCurrent), "A")]
    public double GlobalGain { get => globalGain; set => ChangeProperty(ref globalGain, value); }

    [Display(Name = "Time Constant", Description = "Time period for iterating PID control")]
    [UnitBox(typeof(UnitTime), "s")]
    public double TimeConstant { get => timeConstant; set => ChangeProperty(ref timeConstant, value); }

    [Display(Name = "Max I Error", Description = "Max Integral (I) Error is the limit for I" +
      "component of PID control that is integrated over time")]
    [UnitBox(typeof(UnitPercent ), " ", 0.0, 1.0)]
    public double MaxIntegralError { get => maxIntegralError; set => ChangeProperty(ref maxIntegralError, value); }

    [Display(Name = "Slope Rate", Description = "The rate of heating/cooling ramp that is used" +
      "every time new setpoint is set is Ramping is enabled")]
    [UnitBox(typeof(UnitBandTempCperS), "°C/m", 0.0, double.MaxValue)]
    public double CRate { get => cRate; set => ChangeProperty(ref cRate, value); }
  
    [Display(Name = "PBand Reached In", Description = "Minimum PBand Value when SetPoint is Considered Reached")]
    [UnitBox(typeof(UnitPercent), "%", 0.0, 1.0)]
    public double PBandCin { get => pBandCin; set => ChangeProperty(ref pBandCin, value); }
    [Display(Name = "PBand Reached Out", Description = "Maximum PBand Value when SetPoint Reached State Invalidated")]
    [UnitBox(typeof(UnitPercent), "%", 0.0, 1.0)]
    public double PBandCout { get => pBandCout; set => ChangeProperty(ref pBandCout, value); }
    [Display(Name = "SP Reached Count", Description = "Number of PID Control Cycles Setpoint Reached Event Should Fire to Consider SetPoint is Actually Reached")]
    public uint SPReachedFilter { get => sPReachedFilter; set => ChangeProperty(ref sPReachedFilter, value); }

    [Display(Name = "Maximum SP Reach Time", Description = "If PID cannot reach setpoint in time, PID and power will be disabled. It is not recommended to set it over 5 minutes.")]
    [UnitBox(typeof(UnitTime), "min", 10.0, 3600.0)]
    public double SetPointReachTime { get => setPointReachTime; set => ChangeProperty(ref setPointReachTime, value); }

    [Display(Name = "Enable Ramp", Description = "If Ramping is enabled, new setpoint will" +
      "not be applyed immidiately, but gradually set starting from the current temperature")]
    public bool RampEnable { get => rampEnable; set => ChangeProperty(ref rampEnable, value); }

    [Display(Name = "Interface Settings", Description = "These sttings only affect the interface " +
      "and do not affect behaviour"), EditorBrowsable(EditorBrowsableState.Advanced)]
    public PIDInterface Interface { get => pidInterface; set => ChangeProperty(ref pidInterface, value); }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public class PIDInterface : GenericSB<PIDInterface>
    {
      bool isExpanded = false, logExpanded = false;
      Unit uCR = new UnitBandTempCperS("C/m");
      Unit uT = new UnitTemperatureC("C");
      Unit uCP = new UnitRelTemperature("C");
      Unit uCI = new UnitBandTempConS("Cs");
      Unit uCD = new UnitBandTempCperS("C/s");
      Unit uMinC = new UnitCurrent("A");
      Unit uMaxC = new UnitCurrent("A");
      Unit uGG = new UnitCurrent("A");
      Unit uTC = new UnitTime("ms");
      Unit uME = new UnitPercent(" ");

      string fCR = "N3", fSPT = "N3", fMT = "N3", fISPT = "N3";
      string fCP = "N2", fCI = "N2", fCD = "N2";
      string fMinC = "N2", fMaxC = "N2", fGG = "N2", fTC = "N2", fME = "N2";

      [EditorBrowsable(EditorBrowsableState.Never)]
      public bool LogExpanded { get => logExpanded; set => ChangeProperty(ref logExpanded, value); }
      [EditorBrowsable(EditorBrowsableState.Never)]
      public bool IsExpanded { get => isExpanded; set => ChangeProperty(ref isExpanded, value); }

      public Unit CRUnit { get => uCR; set => ChangeProperty(ref uCR, value); }
      public Unit TUnit { get => uT; set => ChangeProperty(ref uT, value); }
      public Unit CPUnit { get => uCP; set => ChangeProperty(ref uCP, value); }
      public Unit CIUnit { get => uCI; set => ChangeProperty(ref uCI, value); }
      public Unit CDUnit { get => uCD; set => ChangeProperty(ref uCD, value); }
      public Unit MinCUnit { get => uMinC; set => ChangeProperty(ref uMinC, value); }
      public Unit MaxCUnit { get => uMaxC; set => ChangeProperty(ref uMaxC, value); }
      public Unit GGUnit { get => uGG; set => ChangeProperty(ref uGG, value); }
      public Unit TCUnit { get => uTC; set => ChangeProperty(ref uTC, value); }
      public Unit MEUnit { get => uME; set => ChangeProperty(ref uME, value); }

      [EditorBrowsable(EditorBrowsableState.Advanced)]
      public string CRFormat { get => fCR; set => ChangeProperty(ref fCR, value); }
      [EditorBrowsable(EditorBrowsableState.Advanced)]
      public string CPFormat { get => fCP; set => ChangeProperty(ref fCP, value); }
      [EditorBrowsable(EditorBrowsableState.Advanced)]
      public string CIFormat { get => fCI; set => ChangeProperty(ref fCI, value); }
      [EditorBrowsable(EditorBrowsableState.Advanced)]
      public string CDFormat { get => fCD; set => ChangeProperty(ref fCD, value); }
      [EditorBrowsable(EditorBrowsableState.Advanced)]
      [Display(Name = "Format of SetPoint")]
      public string SPTFormat { get => fSPT; set => ChangeProperty(ref fSPT, value); }
      [EditorBrowsable(EditorBrowsableState.Advanced)]
      [Display(Name = "Format of Measured T")]
      public string MTFormat { get => fMT; set => ChangeProperty(ref fMT, value); }
      [EditorBrowsable(EditorBrowsableState.Advanced)]
      [Display(Name = "Format of Immidiate SetPoint")]
      public string ISPTFormat { get => fISPT; set => ChangeProperty(ref fISPT, value); }
      [EditorBrowsable(EditorBrowsableState.Advanced)] 
      public string MinCFormat { get => fMinC; set => ChangeProperty(ref fMinC, value); }
      [EditorBrowsable(EditorBrowsableState.Advanced)] 
      public string MaxCFormat { get => fMaxC; set => ChangeProperty(ref fMaxC, value); }
      [EditorBrowsable(EditorBrowsableState.Advanced)] 
      public string GGFormat { get => fGG; set => ChangeProperty(ref fGG, value); }
      [EditorBrowsable(EditorBrowsableState.Advanced)] 
      public string TCFormat { get => fTC; set => ChangeProperty(ref fTC, value); }
      [EditorBrowsable(EditorBrowsableState.Advanced)] 
      public string MEFormat { get => fME; set => ChangeProperty(ref fME, value); }
    }
  }
}