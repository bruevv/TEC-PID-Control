#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using CSUtils;
using Cursor = System.Windows.Forms.DataVisualization.Charting.Cursor;
using UserControl = System.Windows.Controls.UserControl;

namespace WPFControls
{
  using inp = System.Windows.Input;
  using static Math;

  /// <summary>
  /// Interaction logic for ChartControl.xaml
  /// </summary>
  public partial class ChartControl : UserControl
  {
    const double NaN = double.NaN;

    public ChartControl()
    {
      InitializeComponent();

      Chart.Scale(new SizeF(2, 2));

      System.Windows.Forms.Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

      if(!string.IsNullOrEmpty(Template))
        chart.LoadTemplate(Template);

      Chart.Series.Add(FeedbackPoint);
      Chart.Series.Add(selectedCurve);
      Chart.Series.Add(selectedRange);

      verticalLine1.AxisX = AxisX;
      verticalLine1.AxisY = AxisY;
      verticalLine1.ClipToChartArea = chartArea.Name;
      verticalLine2.AxisX = AxisX;
      verticalLine2.AxisY = AxisY;
      verticalLine2.ClipToChartArea = chartArea.Name;
      Chart.Annotations.Add(verticalLine1);
      Chart.Annotations.Add(verticalLine2);

      Series = new ObservableCollection<NSeries>();
      Series.CollectionChanged += Series_CollectionChanged;

      chart.ChartAreas[0].IsSameFontSizeForAllAxes = true;
      chart.SuppressExceptions = true;

      ChartArea.Tag = this;

      AxisX.MinorTickMark.Enabled = true;
      AxisY.MinorTickMark.Enabled = true;
      AxisX.IsStartedFromZero = false;
      AxisY.IsStartedFromZero = false;
      AxisX.LabelStyle.Format = "G5";
      AxisY.LabelStyle.Format = "G5";
      AxisX.MinorGrid.Enabled = true;
      AxisX.MinorGrid.LineColor = Color.LightGray;
      AxisY.MinorGrid.Enabled = true;
      AxisY.MinorGrid.LineColor = Color.LightGray;
      AxisX.LineWidth = 2;
      AxisY.LineWidth = 2;
      AxisX.ScaleView.SmallScrollMinSize = 0;
      AxisY.ScaleView.SmallScrollMinSize = 0;
      AxisX.MajorTickMark.LineWidth = 2;
      AxisY.MajorTickMark.LineWidth = 2;
      AxisX.MinorTickMark.Size = AxisX.MajorTickMark.Size / 2;
      AxisY.MinorTickMark.Size = AxisY.MajorTickMark.Size / 2;
      AxisX.ScaleView.MinSize = 1e-7;
      AxisY.ScaleView.MinSize = 1e-5;
      AxisX.IsMarginVisible = false;
      AxisX.ScaleView.Zoomable = false;
      AxisY.ScaleView.Zoomable = false;
      AxisX.Minimum = 0;
      AxisX.Maximum = 1;
      AxisY.Minimum = 0;
      AxisY.Maximum = 1;
      //Empty series needed to display axes
      //   Series es = new Series("empty") { ChartType = SeriesChartType.Line };
      //   es.Points.Add(new DataPoint() { IsEmpty = true });
      //  Chart.Series.Add(es);

      CursorX.Interval = 0;
      CursorY.Interval = 0;
      CursorX.IsUserEnabled = true;//////////////////////

      chartArea.Visible = true;

      Chart.Customize += Chart_Customize;
    }

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

    NSeries selectedSeries = null;
    public NSeries SelectedSeries {
      get => selectedSeries?.Enabled ?? false ? selectedSeries : null;
      set {
        if(selectedSeries != value) {
          //      CursorX.SetSelectionPosition(NaN, NaN);
          if(selectedSeries != null) {
            selectedSeries.Selected = false;
            selectedCurve.Enabled = false;
            selectedRange.Enabled = false;
          }
          if(Series.Contains(value))
            selectedSeries = value;
          else if(value?.LinkedSeries != null && Series.Contains(value.LinkedSeries))
            selectedSeries = value.LinkedSeries;
          else selectedSeries = null;

          if(selectedSeries != null) selectedSeries.Selected = true;
        }
      }
    }
    public Title Title {
      get => Chart.Titles.Count == 0 ? null : Chart.Titles[0];
      set {
        if(value == null) {
          if(Chart.Titles.Count != 0)
            chart.Titles.Clear();
        } else {
          if(Chart.Titles.Count == 0)
            chart.Titles.Add(value);
          else
            Chart.Titles[0] = value;
        }
      }
    }

    public AntiAliasingStyles AntiAliasing {
      get => Chart.AntiAliasing;
      set => Chart.AntiAliasing = value;
    }

    //   public event EventHandler<CursorEventArgs> CursorXPositionChanged;
    public event EventHandler<CursorEventArgs> CursorXPositionChanging;
    public event EventHandler<CursorEventArgs> SelectionRangeChanged;

    #region Scaling
    void RescaleX()
    {
      if(AxisX.ScaleView.IsZoomed) {
        ChartArea.Visible = false;
        AxisX.ScaleView.ZoomReset(0);
      }

      AxisX.Minimum = NaN;
      AxisX.Maximum = NaN;
      AxisX.Interval = 0;
      if(!AxisY.ScaleView.IsZoomed && LogScaleY) {
        AxisY.Minimum = Pow(10, Floor(Log10(AxisY.Minimum) + 0.01));
        AxisY.Maximum = Pow(10, Ceiling(Log10(AxisY.Maximum) - 0.01));
      } else {
        AxisY.Minimum = AxisY.Minimum;
        AxisY.Maximum = AxisY.Maximum;
      }
    }
    void RescaleY()
    {
      if(AxisY.ScaleView.IsZoomed) {
        ChartArea.Visible = false;
        AxisY.ScaleView.ZoomReset(0);
      }
      AxisY.Minimum = NaN;
      AxisY.Maximum = NaN;
      AxisY.Interval = 0;
      AxisX.Minimum = AxisX.Minimum;
      AxisX.Maximum = AxisX.Maximum;
    }
    public void RescaleXY()
    {
      if(AxisX.ScaleView.IsZoomed) {
        ChartArea.Visible = false;
        AxisX.ScaleView.ZoomReset(0);
      }
      if(AxisY.ScaleView.IsZoomed) {
        ChartArea.Visible = false;
        AxisY.ScaleView.ZoomReset(0);
      }

      AxisX.Minimum = NaN;
      AxisX.Maximum = NaN;
      AxisX.Interval = 0;
      AxisY.Minimum = NaN;
      AxisY.Maximum = NaN;
      AxisY.Interval = 0;
    }
    void RescaleNone()
    {
      if(!AxisY.ScaleView.IsZoomed && LogScaleY) {
        AxisY.Minimum = Pow(10, Floor(Log10(AxisY.Minimum) + 0.01));
        AxisY.Maximum = Pow(10, Ceiling(Log10(AxisY.Maximum) - 0.01));
      } else {
        AxisY.Minimum = AxisY.Minimum;
        AxisY.Maximum = AxisY.Maximum;
      }
      AxisX.Minimum = AxisX.Minimum;
      AxisX.Maximum = AxisX.Maximum;
    }

    public void QuerryRescale(bool? asx = null, bool? asy = null)
    {
      if(SuspendRescale) return;

      bool ax, ay;
      ax = asx ?? AutoScaleX;
      ay = asy ?? AutoScaleY;

      if(ax && ay) RescaleXY();
      else if(ax) RescaleX();
      else if(ay) RescaleY();
      else RescaleNone();

      ChartArea.RecalculateAxesScale();
      if(LogScaleY && AxisY.Minimum <= 1.0) AxisY.Minimum = minlogy;
      TuneAxisOffset(AxisX);
      TuneAxisOffset(AxisY);
      ChartArea.Visible = true;
    }
    double xAxisScale = 1.0;
    public double XAxisScale {
      get => xAxisScale;
      set {
        if(xAxisScale != value) {
          xAxisScale = value;
          Chart.Invalidate();
        }
      }
    }
    void Chart_Customize(object sender, EventArgs e)
    {

      //TuneAxisOffset(AxisX);
      //TuneAxisOffset(AxisY);
      //foreach(CustomLabel cl in AxisX.CustomLabels) {
      //  double pos = (cl.FromPosition + cl.ToPosition) / 2;
      //  cl.Text = (pos / XAxisScale).ToString(AxisX.LabelStyle.Format);
      //}
    }

    #endregion Scaling

    public bool IsUniqueName(string name) => chart.Series.IsUniqueName(name);

    bool SuspendRescale = false;

    public void UpdateAllSeries()
    {
      SuspendRescale = true;
      foreach(NSeries ns in Series) ns.TryUpdate();
      SuspendRescale = false;
      QuerryRescale();
    }
    public ObservableCollection<NSeries> Series { get; }
    int? StartingSeriesCount = null;
    void Series_CollectionChanged(object s, NotifyCollectionChangedEventArgs e)
    {
      int c0 = StartingSeriesCount ?? Chart.Series.Count;
      StartingSeriesCount = c0;
      int i;
      switch(e.Action) {
        case NotifyCollectionChangedAction.Add:
          i = e.NewStartingIndex + c0;
          Chart.Series.SuspendUpdates();
          foreach(NSeries ser in e.NewItems) {
            if(ser.Color == Color.Empty) ser.AutoColor();
            Chart.Series.Insert(i++, ser);
          }
          QuerryRescale();
          Chart.Series.ResumeUpdates();
          break;
        case NotifyCollectionChangedAction.Remove:
          Chart.Series.SuspendUpdates();
          foreach(NSeries ser in e.OldItems)
            Chart.Series.Remove(ser);
          QuerryRescale();
          Chart.Series.ResumeUpdates();
          break;
        case NotifyCollectionChangedAction.Replace:
          Chart.Series.SuspendUpdates();
          foreach(NSeries ser in e.OldItems)
            Chart.Series.Remove(ser);
          i = e.NewStartingIndex + c0;
          foreach(NSeries ser in e.NewItems) {
            if(ser.Color == Color.Empty) ser.AutoColor();
            Chart.Series.Insert(i++, ser);
          }
          QuerryRescale();
          Chart.Series.ResumeUpdates();
          break;
        case NotifyCollectionChangedAction.Move:
          Chart.Series.SuspendUpdates();
          i = e.NewStartingIndex + c0;
          foreach(NSeries ser in e.OldItems) {
            Chart.Series.Remove(ser);
            Chart.Series.Insert(i++, ser);
          }
          Chart.Series.ResumeUpdates();
          break;
        case NotifyCollectionChangedAction.Reset:
          NSeries[] sl = new NSeries[Chart.Series.Count];
          Chart.Series.SuspendUpdates();
          Chart.Series.CopyTo(sl, 0);
          foreach(NSeries sr in sl)
            if(!sr.IsIntrinsic) Chart.Series.Remove(sr);
          QuerryRescale();
          Chart.Series.ResumeUpdates();
          break;
      }
    }

    #region Intrinsic Series
    const string FeedbackPoint_Name = "_Feedback_";
    readonly NSeries FeedbackPoint = new NSeries(FeedbackPoint_Name) {
      ChartType = SeriesChartType.Point,
      MarkerStyle = MarkerStyle.Circle,
      BorderColor = Color.Black,
      Color = Color.White,
      BorderWidth = 1,
      MarkerSize = 8,
      Enabled = false,
      IsIntrinsic = true,
      Points = { { 0 } }
    };
    readonly VerticalLineAnnotation verticalLine1 = new VerticalLineAnnotation() {
      //    IsInfinitive = true,
      LineWidth = 3,
      LineColor = Color.FromArgb(80, 0, 0, 0)
    };
    readonly VerticalLineAnnotation verticalLine2 = new VerticalLineAnnotation() {
      //    IsInfinitive = true,
      LineWidth = 3,
      LineColor = Color.FromArgb(80, 0, 0, 0)
    };
    const string SelectedCurve_Name = "_SelectedCurve_";
    readonly NSeries selectedCurve = new NSeries(SelectedCurve_Name) {
      ChartType = SeriesChartType.FastLine,
      Color = Color.Black,
      Enabled = false,
      IsIntrinsic = true,
      ReplaceZeroWithNaN = true,
    };
    const string SelectedRange_Name = "_SelectedRange_";
    readonly NSeries selectedRange = new NSeries(SelectedRange_Name) {
      ChartType = SeriesChartType.Area,
      Color = Color.Transparent,
      BorderWidth = 0,
      Enabled = false,
      IsIntrinsic = true,
      ReplaceZeroWithNaN = true,
    };

    public void UpdateSelectedCurve(IList<double> X, IList<double> Y)
    {
      selectedCurve.Enabled = false;
      selectedCurve.UpdateSample(X, Y);
      if(!selectedCurve.IsEmpty) selectedCurve.Enabled = true;
    }
    public void ShowRange(double x1, double x2, Color c)
    {
      verticalLine1.X = x1;
      verticalLine2.X = x2;
      var points = selectedCurve.Points;
      if(!double.IsNaN(x1)) {
        try {
          int ind = (int)(selectedCurve.GetIndexAprox(x1) + 0.5);
          verticalLine1.Y = points[ind].YValues[0];
          verticalLine1.Height = 100;
        } catch {
          verticalLine1.Y = 1e-5;
          verticalLine1.Height = -100;
        }
      }
      if(!double.IsNaN(x2)) {
        try {
          int ind = (int)(selectedCurve.GetIndexAprox(x2) + 0.5);
          verticalLine2.Y = points[ind].YValues[0];
          verticalLine2.Height = 100;
        } catch {
          verticalLine2.Y = 1e-5;
          verticalLine2.Height = -100;
        }
      }
      int count = points.Count;
      if(!c.IsEmpty && count >= 2) {
        selectedRange.Points.SuspendUpdates();
        selectedRange.Clear();
        //     if(double.IsNaN(x1) && double.IsNaN(x2)) selectedCurve.Enabled = false;

        x1 = double.IsNaN(x1) ? double.NegativeInfinity : x1;
        x2 = double.IsNaN(x2) ? double.PositiveInfinity : x2;
        for(int i = 0; i < count; i++) {
          if(points[i].XValue >= x1 && points[i].XValue <= x2)
            selectedRange.AddPoint(points[i].XValue, points[i].YValues[0]);
        }

        if(!selectedRange.IsEmpty) {
          selectedRange.Color = Color.FromArgb(32, c);
          selectedRange.Points.ResumeUpdates();
          selectedRange.Enabled = true;
        } else {
          selectedRange.Enabled = false;
          selectedRange.Points.ResumeUpdates();
        }
      } else {
        selectedRange.Enabled = false;
      }

      verticalLine1.Visible = true;
      verticalLine2.Visible = true;
    }
    public void HideRange()
    {
      verticalLine1.Visible = false;
      verticalLine2.Visible = false;
      selectedRange.Enabled = false;
    }

    public void SetFeedback(double x, double y, Color? color = null)
    {
      Color c = color ?? Color.White;
      if(x > 1e10 || x < -1e10 || y > 1e10 || y < -1e10) {
        x = double.NaN;
        y = double.NaN;
      }
      if(double.IsNaN(x) || double.IsNaN(y)) {
        FeedbackPoint.Enabled = false;
      } else {
        FeedbackPoint.Points[0].IsEmpty = false;
        FeedbackPoint.Color = c;
        FeedbackPoint.Enabled = true;
        FeedbackPoint.Points[0].SetValueXY(x, y);
      }
    }
    public void ClearFeedback()
    {
      bool rescale = FeedbackPoint.Enabled;
      SetFeedback(double.NaN, double.NaN);

      if(rescale) QuerryRescale();
    }
    #endregion Intrinsic Series

    public event MouseEventHandler RightMouseDown;
    public event EventHandler<NSeries> CurveSelected;
    public delegate string GetTootipAtPoint_delegate(Series series, double x);
    public event GetTootipAtPoint_delegate GetTootipAtPoint;

    static readonly ChartElementType[] CETA = { ChartElementType.DataPoint };
    void Chart_MouseClick(object sender, MouseEventArgs e)
    {
      if(e.Button == MouseButtons.Right) {
        RightMouseDown?.Invoke(this, e);
      } else if(e.Button == MouseButtons.Left) {
        if(SelectedTool == ChartTool.None) {
          HitTestResult[] htrs = Chart.HitTest(e.X, e.Y, true, CETA);
          foreach(HitTestResult htr in htrs) {
            if(htr.ChartElementType == ChartElementType.DataPoint &&
              htr.Series.ChartType == SeriesChartType.FastLine) {
              if(htr.Series is NSeries ns) {
                CurveSelected?.Invoke(this, ns);
                break;
              }
            } /*else if(htr.ChartElementType == ChartElementType.PlottingArea ||
                      htr.ChartElementType == ChartElementType.Gridlines) {
              CurveSelected?.Invoke(this, null);
            }*/
          }
        }
      }
    }
    void Chart_GetToolTipText(object sender, ToolTipEventArgs e)
    {
      switch(e.HitTestResult.ChartElementType) {
        case ChartElementType.DataPoint:
          DataPoint dp = e.HitTestResult.Object as DataPoint;
          double x = AxisX.PixelPositionToValue(e.X);
          string tt = GetTootipAtPoint?.Invoke(e.HitTestResult.Series, x);
          NSeries ns = (NSeries)e.HitTestResult.Series;
          e.Text = tt ?? $"{ns.SampleName}\nX {dp.XValue:G5}\nY {dp.YValues[0]:G5}";
          break;
      }
    }
    void Chart_MouseMove(object sender, MouseEventArgs e)
    {
      /*      switch(Chart.HitTest(e.X, e.Y).ChartElementType) {
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
            }*/
    }

    #region Tools
    public void ClearXCursor()
    {
      CursorX.Position = NaN;
      CursorX.SetSelectionPosition(NaN, NaN);
      ClearFeedback();
    }
    public void ClearYCursor()
    {
      CursorY.Position = NaN;
      CursorY.SetSelectionPosition(NaN, NaN);
    }
    void EnableCursorX()
    {
      CursorX.IsUserEnabled = false;
      CursorX.IsUserSelectionEnabled = true;
    }
    void EnableCursorY()
    {
      CursorX.IsUserEnabled = false;
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
      CursorX.IsUserSelectionEnabled = true;
      CursorX.IsUserEnabled = false;
      AxisX.ScaleView.Zoomable = true;
    }
    void EnableZoomY()
    {
      CursorY.IsUserSelectionEnabled = true;
      CursorX.IsUserEnabled = false;
      AxisY.ScaleView.Zoomable = true;
    }
    void EnableZoomXY()
    {
      CursorX.IsUserSelectionEnabled = true;
      CursorX.IsUserEnabled = false;
      CursorY.IsUserSelectionEnabled = true;
      CursorY.IsUserEnabled = false;
      AxisX.ScaleView.Zoomable = true;
      AxisY.ScaleView.Zoomable = true;
    }
    void DisableTool()
    {
      CursorX.IsUserEnabled = true;//////////////////////
      CursorX.IsUserSelectionEnabled = false;
      CursorY.IsUserEnabled = false;
      CursorY.IsUserSelectionEnabled = false;
      AxisX.ScaleView.Zoomable = false;
      AxisY.ScaleView.Zoomable = false;
    }

    ChartTool OldTool = ChartTool.None;

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
    #endregion Tools

    protected override void OnGotKeyboardFocus(inp.KeyboardFocusChangedEventArgs e)
    {
      BorderBrush = System.Windows.Media.Brushes.Gray;
      base.OnGotKeyboardFocus(e);
    }

    protected override void OnLostKeyboardFocus(inp.KeyboardFocusChangedEventArgs e)
    {
      BorderBrush = System.Windows.Media.Brushes.White;
      base.OnLostKeyboardFocus(e);
    }

    protected override void OnKeyDown(inp.KeyEventArgs e)
    {
      if(!double.IsNaN(CursorX.Position) && !e.Handled) {
        double dx = AxisX.MinorTickMark.Interval;
        double ix = SelectedSeries?.GetIndexAprox(CursorX.Position) ?? double.NaN;
        double newpos = double.NaN;
        switch(e.Key) {
          case inp.Key.Left:
            newpos = CursorX.Position - dx;
            ix = Floor(ix - 0.001);
            e.Handled = true;
            break;
          case inp.Key.Right:
            newpos = CursorX.Position + dx;
            ix = Ceiling(ix + 0.001);
            e.Handled = true;
            break;
        }
        if(e.Handled) {
          try {
            newpos = double.IsNaN(ix) ? newpos : SelectedSeries.Points[(int)ix].XValue;
          } catch { }
          CursorX.SetCursorPosition(newpos);
          var cea = new CursorEventArgs(ChartArea, AxisX, newpos);
          Chart_CPChanging(this, cea);
        }
      }
      base.OnKeyDown(e);
    }
    const double minlogy = 0.8;
    #region Zooming
    void TuneAxisOffset(Axis a, double? minimum = null, double? maximum = null)
    {
      try {
        if(a.IsLogarithmic) {
          double min;
          double max;
          //  if(a.ScaleView.IsZoomed) {
          min = minimum ?? a.ScaleView.ViewMinimum;

          //          max = maximum ?? a.ScaleView.ViewMaximum;
          //  } else {
          //    min = minimum ?? Log10(a.ScaleView.ViewMinimum);
          //   max = maximum ?? Log10(a.ScaleView.ViewMaximum);
          //  }
          a.Interval = 1;
          a.MinorTickMark.Interval = 1;
          a.MinorGrid.Interval = 1;
          a.MinorTickMark.IntervalOffset = 0;
          a.MinorGrid.IntervalOffset = 0;
          if(a.ScaleView.IsZoomed) {
            a.IntervalOffset = Ceiling(min) - (min);
            a.MinorTickMark.Enabled = false;
            a.MinorGrid.Enabled = false;
          } else {
            a.IntervalOffset = Ceiling(min) - (min);
            if(min < 1)
              a.IntervalOffset = Ceiling(Log10(min)) - (Log10(min)); // the wierdest undocumented convension from .NET programmers
            a.MinorTickMark.IntervalOffset = a.IntervalOffset;
            a.MinorGrid.IntervalOffset = a.IntervalOffset;
            a.MinorTickMark.Enabled = true;
            a.MinorGrid.Enabled = true;
          }
        } else {
          double min = minimum ?? a.ScaleView.ViewMinimum;
          double max = maximum ?? a.ScaleView.ViewMaximum;
          if(double.IsNaN(min) || double.IsNaN(max)) return;
          (double MajInt, double MinInt) = CalculateIntervals(a, min, max);
          a.Interval = MajInt;
          a.MinorTickMark.Interval = MinInt;
          a.MinorTickMark.Enabled = true;
          a.MinorGrid.Interval = MinInt;
          a.MinorGrid.Enabled = true;
          if(min > 0) {
            a.IntervalOffset = MajInt - min % MajInt;
            a.MinorTickMark.IntervalOffset = -(min % MajInt);
            a.MinorGrid.IntervalOffset = -(min % MajInt);
          } else {
            a.IntervalOffset = (-min) % MajInt;
            a.MinorTickMark.IntervalOffset = -MajInt + (-min) % MajInt;
            a.MinorGrid.IntervalOffset = -MajInt + (-min) % MajInt;
          }
        }
      } catch { }
    }
    static readonly int[] roundInterval = { 1, 2, 5, 10 };
    static readonly int[] numMinTicks = { 5, 4, 5, 5 };
    (double MajInt, double MinInt) CalculateIntervals(Axis a, double minValue, double maxValue)
    {
      double axislen = (int)(a.ValueToPixelPosition(maxValue) - a.ValueToPixelPosition(minValue));
      int MaxMajTicks = (int)(axislen / (7 * a.LabelAutoFitMaxFontSize));
      GUtils.Limit(ref MaxMajTicks, 5, 30);
      double min = Min(minValue, maxValue);
      double max = Max(minValue, maxValue);
      double delta = max - min;
      double MajInt, MinInt;

      if(delta == 0) return (0, 0);

      double flInt = delta / MaxMajTicks;
      int N = (int)Floor(Log10(flInt));
      double tenToN = Pow(10, N);
      double A = flInt / tenToN;

      int i;
      for(i = 0; i < roundInterval.Length; i++) {
        if(A <= roundInterval[i]) break;
      }
      MajInt = roundInterval[i] * tenToN;
      MinInt = MajInt / numMinTicks[i];

      return (MajInt, MinInt);
    }
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
      QuerryRescale();
      base.OnRenderSizeChanged(sizeInfo);
    }

    void Chart_AxisViewChanged(object sender, ViewEventArgs e)
    {
      Axis a = e.Axis;
      AxisScaleView asv = a.ScaleView;

      double minValue = e.NewPosition;
      double maxValue = e.NewPosition + e.NewSize;

      TuneAxisOffset(a, minValue, maxValue);
    }
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

      o.AxisY.IsLogarithmic = val;
      o.QuerryRescale(null, true);
    }
    static void LogScaleX_PC(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
      ChartControl o = (ChartControl)obj;
      bool val = (bool)e.NewValue;

      o.AxisX.IsLogarithmic = val;
      o.QuerryRescale(true, null);
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

    //  void Chart_CPChanged(object _, CursorEventArgs e) => CursorXPositionChanged?.Invoke(this, e);
    void Chart_CPChanging(object o, CursorEventArgs e)
    {
      if(SelectedTool == ChartTool.None)
        CursorXPositionChanging?.Invoke(this, e);
    }

    void Chart_SRChanged(object o, CursorEventArgs e)
    {
      if(SelectedTool == ChartTool.CursorX)
        SelectionRangeChanged?.Invoke(this, e);
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
