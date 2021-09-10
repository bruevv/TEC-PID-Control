#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Cursor = System.Windows.Forms.DataVisualization.Charting.Cursor;
using UserControl = System.Windows.Controls.UserControl;

namespace WPFControls
{
  using static Math;

  /// <summary>
  /// Interaction logic for ChartControl.xaml
  /// </summary>
  public partial class ChartControl : UserControl
  {
    const double NaN = double.NaN;

    public new virtual string Template { get; set; } = "";

    public void SaveTemplate(string TemplateFile)
    {
      bool itm = chart.Serializer.IsTemplateMode;
      chart.Serializer.IsTemplateMode = true;

      chart.Serializer.Save(Template);

      chart.Serializer.IsTemplateMode = itm;
    }

    public Chart Chart => chart;
    public ChartArea ChartArea => chartArea;
    public Axis AxisX => ChartArea.AxisX;
    public Axis AxisY => ChartArea.AxisY;
    public Cursor CursorX => ChartArea.CursorX;
    public Cursor CursorY => ChartArea.CursorY;

    public Title Title {
      get => Chart.Titles[0];
      set => Chart.Titles[0] = value;
    }

    void RescaleX()
    {
      AxisX.Minimum = NaN;
      AxisX.Maximum = NaN;
      if(!LogScaleY) {
        AxisY.Minimum = AxisY.Minimum;
        AxisY.Maximum = AxisY.Maximum;
      }
      ChartArea.RecalculateAxesScale();
      AxisX.RoundAxisValues();
      if(AxisX.ScaleView.IsZoomed) AxisX.ScaleView.ZoomReset(0);
    }
    void RescaleY()
    {

      AxisY.Minimum = NaN;
      AxisY.Maximum = NaN;
      AxisX.Minimum = AxisX.Minimum;
      AxisX.Maximum = AxisX.Maximum;
      ChartArea.RecalculateAxesScale();
      if(AxisY.ScaleView.IsZoomed) AxisY.ScaleView.ZoomReset(0);
    }
    public void RescaleXY()
    {
      AxisX.Minimum = NaN;
      AxisX.Maximum = NaN;
      AxisY.Minimum = NaN;
      AxisY.Maximum = NaN;
      ChartArea.RecalculateAxesScale();
      AxisX.RoundAxisValues();
      if(AxisX.ScaleView.IsZoomed) AxisX.ScaleView.ZoomReset(0);
      if(AxisY.ScaleView.IsZoomed) AxisY.ScaleView.ZoomReset(0);
    }
    public void QuerryRescale(bool? asx = null, bool? asy = null)
    {
      bool ax, ay;
      ax = asx ?? AutoScaleX;
      ay = asy ?? AutoScaleY;

      if(ax && ay) RescaleXY();
      else if(ax) RescaleX();
      else if(ay) RescaleY();

      if(ax) {
        AxisX.Minimum = NaN;
        AxisX.Maximum = NaN;
      }
      if(ay) {
        AxisY.Minimum = NaN;
        AxisY.Maximum = NaN;
      }
    }

    public void ClearXCursor()
    {
      CursorX.Position = NaN;
      CursorX.SelectionStart = NaN;
      CursorX.SelectionEnd = NaN;
    }
    public void ClearYCursor()
    {
      CursorY.Position = NaN;
      CursorY.SelectionStart = NaN;
      CursorY.SelectionEnd = NaN;
    }

    public bool IsUniqueName(string name) => chart.Series.IsUniqueName(name);

    public ObservableCollection<NSeries> Series { get; }

    public AntiAliasingStyles AntiAliasing {
      get => Chart.AntiAliasing;
      set => Chart.AntiAliasing = value;
    }

    public ChartControl()
    {
      InitializeComponent();

      if(!string.IsNullOrEmpty(Template))
        chart.LoadTemplate(Template);

      Series = new ObservableCollection<NSeries>();
      Series.CollectionChanged += Series_CollectionChanged;

      Chart.ChartAreas[0].IsSameFontSizeForAllAxes = true;
      AxisX.MinorTickMark.Enabled = true;
      AxisY.MinorTickMark.Enabled = true;
      AxisX.LabelStyle.Format = "G3";
      AxisY.LabelStyle.Format = "G3";

      AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;
      AxisX.IsMarginVisible = false;
      AxisX.ScaleView.Zoomable = false;
      AxisY.ScaleView.Zoomable = false;

      //Empty series needed to display axes
      Series es = new Series("empty") { ChartType = SeriesChartType.Line };
      es.Points.Add(new DataPoint() { IsEmpty = true });
      Chart.Series.Add(es);

      chart.AxisViewChanged += Chart_AxisViewChanged;
      chart.Customize += Chart_Customize;
      Chart.MouseMove += Chart_MouseMove;
      Chart.GetToolTipText += Chart_GetToolTipText;
      Chart.MouseClick += Chart_MouseClick;
      Chart.MouseDown += Chart_MouseDown;
      CursorX.Interval = 0;
      CursorY.Interval = 0;

      Chart.Titles.Add("Title");
      Chart.PaletteCustomColors = PaletteRGB;
      Chart.Palette = ChartColorPalette.None;

      //      Chart.Palette = ChartColorPalette.Bright;
    }

    public event MouseEventHandler RightMouseDown;
    public event EventHandler<Series> CurveSelected;

    void Chart_MouseDown(object sender, MouseEventArgs e)
    {
      if(e.Button == MouseButtons.Left) {
        if(SelectedTool != OldTool) {
          switch(SelectedTool) {
            case ChartTool.None:
              break;
            case ChartTool.CursorX:
              ClearYCursor();
              break;
            case ChartTool.CursorY:
              ClearXCursor();
              break;
            case ChartTool.CursorXY:
              break;
            case ChartTool.ZoomX:
              ClearYCursor();
              AutoScaleX = false;
              break;
            case ChartTool.ZoomY:
              ClearXCursor();
              AutoScaleY = false;
              break;
            case ChartTool.ZoomXY:

              AutoScaleX = false;
              AutoScaleY = false;
              break;
            default:
              break;
          }
          OldTool = SelectedTool;
        }
      }
    }

    void Chart_MouseClick(object sender, MouseEventArgs e)
    {
      if(e.Button == MouseButtons.Right) {
        RightMouseDown?.Invoke(this, e);
      } else if(e.Button == MouseButtons.Left) {
        if(SelectedTool == ChartTool.None) {//TODO make selector tool
          HitTestResult htr = Chart.HitTest(e.X, e.Y);
          if(htr.ChartElementType == ChartElementType.DataPoint)
            CurveSelected?.Invoke(this, htr.Series);
          else if(htr.ChartElementType == ChartElementType.PlottingArea ||
                  htr.ChartElementType == ChartElementType.Gridlines)
            CurveSelected?.Invoke(this, null);
        }
      }
    }

    void Chart_GetToolTipText(object sender, ToolTipEventArgs e)
    {
      switch(e.HitTestResult.ChartElementType) {
        case ChartElementType.DataPoint:
          DataPoint dp = e.HitTestResult.Object as DataPoint;
          e.Text = $"{e.HitTestResult.Series.Name}\nX {dp.XValue:G5}\nY {dp.YValues[0]:G5}";
          break;
      }
    }
    void Chart_MouseMove(object sender, MouseEventArgs e)
    {
      switch(Chart.HitTest(e.X, e.Y).ChartElementType) {
        case ChartElementType.PlottingArea:
        case ChartElementType.Gridlines:
        case ChartElementType.StripLines:
        case ChartElementType.DataPoint:
        case ChartElementType.DataPointLabel:
        case ChartElementType.LegendArea:
        case ChartElementType.LegendTitle:
        case ChartElementType.LegendHeader:
        case ChartElementType.LegendItem:
          Chart.Cursor = Cursors.Cross;
          break;
        default:
          Chart.Cursor = null;
          break;
      }
    }
    void Chart_Customize(object sender, EventArgs e)
    {
      //    if (AutoScaleX) AxisX.RoundAxisValues();
      //        if (LogScaleY) AxisY.RoundAxisValues();
    }

    void Series_CollectionChanged(object s, NotifyCollectionChangedEventArgs e)
    {
      int i;
      switch(e.Action) {
        case NotifyCollectionChangedAction.Add:
          i = e.NewStartingIndex + 1;
          foreach(Series ser in e.NewItems) Chart.Series.Insert(i++, ser);
          Chart.ApplyPaletteColors();
          QuerryRescale();
          break;
        case NotifyCollectionChangedAction.Remove:
          foreach(Series ser in e.OldItems) Chart.Series.Remove(ser);
          QuerryRescale();
          break;
        case NotifyCollectionChangedAction.Replace:
          i = e.NewStartingIndex + 1;
          foreach(Series ser in e.OldItems) Chart.Series.Remove(ser);
          foreach(Series ser in e.NewItems) Chart.Series.Insert(i++, ser);
          Chart.ApplyPaletteColors();
          QuerryRescale();
          break;
        case NotifyCollectionChangedAction.Move:
          break;
        case NotifyCollectionChangedAction.Reset:
          Series[] sl = new Series[Chart.Series.Count];
          Chart.Series.CopyTo(sl, 0);
          foreach(Series sr in sl)
            if(sr.Name != "empty")
              Chart.Series.Remove(sr);
          break;
      }
    }

    void EnableCursorX()
    {
      AxisX.ScaleView.Zoomable = false;
      AxisY.ScaleView.Zoomable = false;
      CursorX.IsUserEnabled = true;
      CursorX.IsUserSelectionEnabled = true;
    }
    void EnableCursorY()
    {
      AxisX.ScaleView.Zoomable = false;
      AxisY.ScaleView.Zoomable = false;
      CursorY.IsUserEnabled = true;
      CursorY.IsUserSelectionEnabled = true;
    }
    void EnableCursorXY()
    {
      AxisX.ScaleView.Zoomable = false;
      AxisY.ScaleView.Zoomable = false;
      CursorX.IsUserEnabled = true;
      CursorX.IsUserSelectionEnabled = true;
      CursorY.IsUserEnabled = true;
      CursorY.IsUserSelectionEnabled = true;
    }
    void EnableZoomX()
    {
      AxisY.ScaleView.Zoomable = false;
      CursorX.IsUserSelectionEnabled = true;
      AxisX.ScaleView.Zoomable = true;
    }
    void EnableZoomY()
    {
      AxisX.ScaleView.Zoomable = false;
      CursorY.IsUserSelectionEnabled = true;
      AxisY.ScaleView.Zoomable = true;
    }
    void EnableZoomXY()
    {
      CursorX.IsUserSelectionEnabled = true;
      CursorY.IsUserSelectionEnabled = true;
      AxisX.ScaleView.Zoomable = true;
      AxisY.ScaleView.Zoomable = true;
    }
    void DisableTool()
    {
      CursorX.IsUserEnabled = false;
      CursorX.IsUserSelectionEnabled = false;
      CursorY.IsUserEnabled = false;
      CursorY.IsUserSelectionEnabled = false;
    }

    ChartTool OldTool = ChartTool.None;

    #region Zooming
    double? lastsizeX = null;
    double? lastsizeY = null;

    private void Chart_AxisViewChanged(object sender, ViewEventArgs e)
    {
      Axis a = e.Axis;
      AxisScaleView asv = a.ScaleView;
      ref double? lastsize = ref (a == AxisX) ? ref lastsizeX : ref lastsizeY;

      if(asv.IsZoomed) {
        double minValue = e.NewPosition;
        double maxValue = e.NewPosition + e.NewSize;
        double axisInterval = 0;
        double axisIntMinor = 0;

        if(a == AxisX) {
          ClearXCursor();
        } else {
          ClearYCursor();
        }
        if(e.Axis == AxisX) {
          GetNiceRoundNumbers(ref minValue, ref maxValue, ref axisInterval, ref axisIntMinor);
        } else if(LogScaleY) {

          int Int = (int)(maxValue - minValue);
          if(Int > 2) {
            AxisY.LabelStyle.IsEndLabelVisible = true;
            minValue = Floor(minValue);
            maxValue = Ceiling(maxValue);
          } else if(Int > 1) {
            AxisY.LabelStyle.IsEndLabelVisible = false;
            double floor = Floor(minValue);
            minValue = floor + Log10(Floor(Pow(10, minValue - floor)));

            floor = Floor(maxValue);
            maxValue = floor + Log10(Ceiling(Pow(10, maxValue - floor))); ;
          } else {
            AxisY.LabelStyle.IsEndLabelVisible = true;
            double floor = Floor(minValue);
            minValue = floor + Log10(Floor(Pow(10, minValue - floor)));

            floor = Floor(maxValue);
            maxValue = floor + Log10(Ceiling(Pow(10, maxValue - floor))); ;
          }
        }
        if(e.NewSize == lastsize) {
          asv.Position = minValue;
        } else {
          double newsize = maxValue - minValue; ;
          if(newsize >= a.Maximum - a.Minimum) {
            lastsize = null;
            asv.ZoomReset(0);

          } else if(newsize < (lastsize ?? (a.Maximum - a.Minimum)) / 20) {
            asv.ZoomReset(1);

          } else {
            asv.Position = minValue;
            lastsize = asv.Size = newsize;
            asv.SmallScrollSize = axisInterval;
            asv.SmallScrollMinSize = Min(asv.SmallScrollMinSize, axisIntMinor);
          }
        }
      } else {
        lastsize = null;
        AxisY.LabelStyle.IsEndLabelVisible = true;
      }
    }


    /// <summary>
    /// Base10Exponent returns the integer exponent (N) that would yield a
    /// number of the form A x Exp10(N), where 1.0 &lt;= |A| &gt; 10.0
    /// </summary>
    int Base10Exponent(double num)
    {
      if(num == 0)
        return -Int32.MaxValue;
      else
        return Convert.ToInt32(Floor(Log10(Abs(num))));
    }

    double[] roundMantissa = { 1.00d, 1.20d, 1.40d, 1.60d, 1.80d, 2.00d, 2.50d, 3.00d, 4.00d, 5.00d, 6.00d, 8.00d, 10.00d };
    double[] roundInterval = { 0.20d, 0.20d, 0.20d, 0.20d, 0.20d, 0.50d, 0.50d, 0.50d, 0.50d, 1.00d, 1.00d, 2.00d, 2.00d };
    double[] roundIntMinor = { 0.05d, 0.05d, 0.05d, 0.05d, 0.05d, 0.10d, 0.10d, 0.10d, 0.10d, 0.20d, 0.20d, 0.50d, 0.50d };
    /// <summary>
    /// Gets nice round numbers for the axes. For the horizontal axis, minValue is always 0.
    /// </summary>
    void GetNiceRoundNumbers(ref double minValue, ref double maxValue, ref double interval, ref double intMinor)
    {
      double min = Min(minValue, maxValue);
      double max = Max(minValue, maxValue);
      double delta = max - min; //The full range
                                //Special handling for zero full range
      if(delta == 0) {
        //When min == max == 0, choose arbitrary range of 0 - 1
        if(min == 0) {
          minValue = 0;
          maxValue = 1;
          interval = 0.2;
          intMinor = 0.5;
          return;
        }
        //min == max, but not zero, so set one to zero
        if(min < 0)
          max = 0; //min-max are -|min| to 0
        else
          min = 0; //min-max are 0 to +|max|
        delta = max - min;
      }

      int N = Base10Exponent(delta);
      double tenToN = Pow(10, N);
      double A = delta / tenToN;
      //At this point delta = A x Exp10(N), where
      // 1.0 <= A < 10.0 and N = integer exponent value
      //Now, based on A select a nice round interval and maximum value
      for(int i = 0; i < roundMantissa.Length; i++)
        if(A <= roundMantissa[i]) {
          interval = roundInterval[i] * tenToN;
          intMinor = roundIntMinor[i] * tenToN;
          break;
        }

      minValue = interval * Floor(min / interval);
      maxValue = interval * Ceiling(max / interval);
    }

    #region Adjust Axis Formatting (commented)
    /*
    /// <summary>
    /// Returns a consistent format string with minimum necessary precision for a range with intervals
    /// </summary>
    /// <param name="interval">Range interval</param>
    /// <param name="minVal">Minimum value of the range</param>
    /// <param name="maxVal">Maximum value of the range</param>
    /// <param name="xtraDigits">Extra digits to display beyond those for minimum necessary precision</param>
    /// <returns></returns>
    public string RangeFormatString(double interval, double minVal, double maxVal, int xtraDigits)
    {
      double maxAbsVal = Math.Max(Math.Abs(minVal), Math.Abs(maxVal));
      int minE = Base10Exponent(interval); //precision to which must show decimal
      int maxE = Base10Exponent(maxAbsVal);
      //(maxE - minE + 1) is the number of significant 
      //digits needed to distinguish two numbers spaced by "interval"
      if (maxE < -4 || 3 < maxE)
        //"Exx" format displays 1 digit to the left of the decimal place, and xx
        //digits to the right of the decimal place, so xx = maxE - minE.
        return "E" + (xtraDigits + maxE - minE).ToString();
      else
        //In fixed format, since all digits to the left of the decimal place are
        //displayed by default, for "Fxx" format, xx = -minE or zero, whichever is greater.
        return "F" + xtraDigits + Math.Max(0, -minE).ToString();
    }
    private void SetAxisFormats()
    {
      Axis axisX = chart.ChartAreas[0].AxisX;
      axisX.LabelStyle.Format = RangeFormatString(axisX.Interval, axisX.Minimum, axisX.Maximum, 0);
      Axis axisY = chart.ChartAreas[0].AxisY;
      axisY.LabelStyle.Format = RangeFormatString(axisY.Interval, axisY.Minimum, axisY.Maximum, 0);
      //  chart1.ChartAreas[0].AxisX.LabelStyle.Format = "";
      //   chart1.ChartAreas[0].AxisY.LabelStyle.Format = "";
    }*/
    #endregion Adjust Axis Formatting (commented)
    #endregion Zooming

    #region Dependency Properties
    static void AutoScaleY_PC(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
      ChartControl o = (ChartControl)obj;
      bool val = (bool)e.NewValue;

      if(val) {
        if(o.SelectedTool.HasFlag(ChartTool.ZoomY)) o.OldTool = ChartTool.None;
        o.QuerryRescale(asy: true);
      }
    }
    static void AutoScaleX_PC(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
      ChartControl o = (ChartControl)obj;
      bool val = (bool)e.NewValue;

      if(val) {
        if(o.SelectedTool.HasFlag(ChartTool.ZoomX)) o.OldTool = ChartTool.None;
        o.QuerryRescale(asx: true);
      }
    }
    static void LogScaleY_PC(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
      ChartControl o = (ChartControl)obj;
      bool val = (bool)e.NewValue;

      if(val) {
        o.AxisY.Minimum = NaN;
        o.AxisY.Maximum = NaN;
      }
      o.AxisY.IsLogarithmic = val;
    }
    static void LogScaleX_PC(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
      ChartControl o = (ChartControl)obj;
      bool val = (bool)e.NewValue;

      o.AxisX.IsLogarithmic = val;
    }
    static void SelectedTool_PC(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
      ChartControl o = (ChartControl)obj;
      ChartTool val = (ChartTool)e.NewValue;
      o.OldTool = (ChartTool)e.OldValue;

      o.DisableTool();

      switch(val) {
        case ChartTool.CursorX:
          o.EnableCursorX();
          break;
        case ChartTool.CursorY:
          o.EnableCursorY();
          break;
        case ChartTool.CursorXY:
          o.EnableCursorXY();
          break;
        case ChartTool.ZoomX:
          o.EnableZoomX();
          break;
        case ChartTool.ZoomY:
          o.EnableZoomY();
          break;
        case ChartTool.ZoomXY:
          o.EnableZoomXY();
          break;
      }
    }

    public static readonly DependencyProperty AutoScaleXProperty =
         DependencyProperty.Register("AutoScaleX",
         typeof(bool), typeof(ChartControl),
         new FrameworkPropertyMetadata(false,
           FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
           AutoScaleX_PC));
    public static readonly DependencyProperty AutoScaleYProperty =
         DependencyProperty.Register("AutoScaleY",
         typeof(bool), typeof(ChartControl),
         new FrameworkPropertyMetadata(false,
           FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
           AutoScaleY_PC));
    public static readonly DependencyProperty SelectedToolProperty =
        DependencyProperty.Register("SelectedTool",
        typeof(ChartTool), typeof(ChartControl),
        new FrameworkPropertyMetadata(default(ChartTool),
          FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
          SelectedTool_PC));
    public static readonly DependencyProperty LogScaleYProperty =
        DependencyProperty.Register("LogScaleY",
        typeof(bool), typeof(ChartControl),
        new FrameworkPropertyMetadata(default(bool),
          FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
          LogScaleY_PC));
    public static readonly DependencyProperty LogScaleXProperty =
        DependencyProperty.Register("LogScaleX",
        typeof(bool), typeof(ChartControl),
        new FrameworkPropertyMetadata(default(bool),
          FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
          LogScaleX_PC));

    public bool AutoScaleX {
      get { return (bool)GetValue(AutoScaleXProperty); }
      set { SetValue(AutoScaleXProperty, value); }
    }
    public bool AutoScaleY {
      get { return (bool)GetValue(AutoScaleYProperty); }
      set { SetValue(AutoScaleYProperty, value); }
    }
    public bool LogScaleY {
      get { return (bool)GetValue(LogScaleYProperty); }
      set { SetValue(LogScaleYProperty, value); }
    }
    public bool LogScaleX {
      get { return (bool)GetValue(LogScaleXProperty); }
      set { SetValue(LogScaleXProperty, value); }
    }
    public ChartTool SelectedTool {
      get { return (ChartTool)GetValue(SelectedToolProperty); }
      set { SetValue(SelectedToolProperty, value); }
    }


    #endregion

    #region Palettes
    static Color[] PaletteRGB = new Color[] {
      Color.FromArgb(255,128,0,128),
      Color.FromArgb(255,0,0,0),
      Color.FromArgb(255,0,255,0),
      Color.FromArgb(255,0,0,255),
      Color.FromArgb(255,255,0,0),
      Color.FromArgb(255,128,128,0),
      Color.FromArgb(255,128,128,128),
      Color.FromArgb(255,0,128,0),
      Color.FromArgb(255,0,0,128),
      Color.FromArgb(255,128,0,0),
      Color.FromArgb(255,0,128,128),
    };
    Color[] Palette = new Color[] {
      Color.Purple,
      Color.Black,
      Color.Blue,
      Color.Green,
      Color.Red,
      Color.Gold,
      Color.Gray,
      Color.DarkBlue,
      Color.DarkGreen,
      Color.DarkRed,
      Color.DarkOrange,
    };
    #endregion Palettes
  }

  public class NSeries : Series, INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(object s, PropertyChangedEventArgs e)
    {
      PropertyChanged?.Invoke(s, e);
    }

    /// <summary>
    /// Initializes a new instance of the System.Windows.Forms.DataVisualization.Charting.Series
    /// class.
    /// </summary>
    public NSeries() : base() { }
    /// <summary>
    /// Initializes a new instance of the System.Windows.Forms.DataVisualization.Charting.Series
    /// class with the specified series name.
    /// </summary>
    /// <param name="name">
    /// The name of the System.Windows.Forms.DataVisualization.Charting.Series object
    /// that will be created. This must be a unique name; otherwise, an exception will
    /// be thrown
    /// </param>
    public NSeries(string name) : base(name) { }
    /// <summary>
    /// Initializes a new instance of the System.Windows.Forms.DataVisualization.Charting.Series
    /// class with the specified name and maximum number of Y-values.
    /// </summary>
    /// <param name="name">
    /// The name of the System.Windows.Forms.DataVisualization.Charting.Series object
    /// that will be created.
    /// </param>
    /// <param name="yValues">
    /// The maximum number of Y-values allowed for the System.Windows.Forms.DataVisualization.Charting.DataPoint
    /// objects that belong to this series.
    /// </param>
    public NSeries(string name, int yValues) : base(name, yValues) { }

    public void Update(string propname = null)
    {
      PropertyChangedEventArgs e = new PropertyChangedEventArgs(propname);
      OnPropertyChanged(this, e);
    }
  }

  [Flags]
  public enum ChartTool
  {
    None = 0,
    CursorX = 1, CursorY = 2, CursorXY = 3,
    ZoomX = 4, ZoomY = 8, ZoomXY = 12,
  }
}
