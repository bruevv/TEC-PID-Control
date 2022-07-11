using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace WPFUtils.Adorners
{
  public class ToolTipWithAdorner : Adorner
  {
    // Be sure to call the base class constructor.
    public ToolTipWithAdorner(FrameworkElement adornedElement) : base(adornedElement)
    {
      adornedElement.MouseEnter += Invalidate;
      adornedElement.MouseLeave += Invalidate;
      adornedElement.IsEnabledChanged += InvalidateDP;

      adornedElement.Tag = "Adorned";
    }

    void InvalidateDP(object o, DependencyPropertyChangedEventArgs e) => InvalidateVisual();
    void Invalidate(object o, EventArgs e) => InvalidateVisual();

    public static Brush IconBrush { get; set; } = null;
    static Brush grayIconBrush = null;

    static public Func<bool> ShowHelpMarkers { get; set; } = null;
    static public double InactiveOppacity {
      get => inactiveOppacity;
      set {
        inactiveOppacity = value;
        grayIconBrush = null;
      }
    }
    protected override void OnRender(DrawingContext drawingContext)
    {
      if (!AdornedElement.IsEnabled) return;
      if (!ShowHelpMarkers?.Invoke() ?? false) return;

      Rect adornedElementRect = new(AdornedElement.RenderSize);

      double border = (AdornedElement as Control)?.BorderThickness.Top ?? 0;
      if (border > 0) border++;
      double scale = 8;
      double ofsetX = adornedElementRect.Width - scale - border;
      double ofsetY = border;

      IconBrush ??= (Brush)FindResource("InfoIcon");
      grayIconBrush ??= new DrawingBrush(((DrawingBrush)IconBrush).Drawing) { Opacity = InactiveOppacity };
      bool gray = AdornedElement.IsMouseOver;
      Rect rect = new(ofsetX, ofsetY, scale, scale);

      if (IconBrush != null) {
        drawingContext.DrawRectangle(gray ? IconBrush : grayIconBrush, null, rect);
        Point p = new Point(ofsetX + scale / 2 - 1, ofsetY + 1);
        ToolTipService.SetPlacementRectangle(AdornedElement, new Rect(p, p));
      }
    }
    static List<ToolTipWithAdorner> allSCAdorners = new();
    private static double inactiveOppacity = 0.2;

    public static void RefreshAllSCA()
    {
      foreach (var sca in allSCAdorners) sca.InvalidateVisual();
    }
    public static void AddToAllChilderenWithTooltip<T>(FrameworkElement container) where T : FrameworkElement
    {
      foreach (var uie in GetLogicalChildCollection<T>(container))
        if (uie.ToolTip != null) RegisterAdorner(uie);
    }

    public static void EnableTooltip<T>(FrameworkElement container, bool enable = true) where T : FrameworkElement
    {
      foreach (var uie in ToolTipWithAdorner.GetLogicalChildCollection<TextBlock>(container)) {
        if (uie.ToolTip != null) ToolTipService.SetIsEnabled(uie, enable);
      }
    }
    public static ToolTipWithAdorner RegisterAdorner(FrameworkElement uie)
    {
      AdornerLayer myAdornerLayer = AdornerLayer.GetAdornerLayer(uie);
      if (myAdornerLayer == null) return null;
      ToolTipWithAdorner sca = new ToolTipWithAdorner(uie);
      myAdornerLayer.Add(sca);
      allSCAdorners.Add(sca);
      return sca;
    }
    public static List<T> GetLogicalChildCollection<T>(DependencyObject parent) where T : DependencyObject
    {
      List<T> logicalCollection = new List<T>();
      GetLogicalChildCollection(parent, logicalCollection);
      return logicalCollection;
    }

    private static void GetLogicalChildCollection<T>(DependencyObject parent, List<T> logicalCollection) where T : DependencyObject
    {
      IEnumerable children = LogicalTreeHelper.GetChildren(parent);
      foreach (object child in children) {
        if (child is DependencyObject) {
          DependencyObject depChild = child as DependencyObject;
          if (child is T) {
            logicalCollection.Add(child as T);
          }
          GetLogicalChildCollection(depChild, logicalCollection);
        }
      }
    }
  }

}
