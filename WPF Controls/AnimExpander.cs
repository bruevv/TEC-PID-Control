using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace WPFControls
{
  public class AnimExpander : Expander
  {
    static readonly bool IsDesignMode = DesignerProperties.GetIsInDesignMode(new DependencyObject());
    public new static readonly DependencyProperty IsExpandedProperty =
                           DependencyProperty.Register(
      "IsExpanded", typeof(bool), typeof(AnimExpander),
      new FrameworkPropertyMetadata(IsExpanded_changed) { BindsTwoWayByDefault = true });

    [EditorBrowsable(EditorBrowsableState.Always)]
    [Category("Common")]
    public new bool IsExpanded {
      get => (bool)GetValue(IsExpandedProperty);
      set {
        SetValue(IsExpandedProperty, value);
        base.IsExpanded = IsEnabled ? value : false;
      }
    }

    static void IsExpanded_changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      AnimExpander ae = (AnimExpander)d;
      ((Expander)ae).IsExpanded = ae.IsEnabled ? (bool)e.NewValue : false;
    }

    [Browsable(false)]
    public new double Height => base.Height;

    DoubleAnimation daExpand = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.25));
    DoubleAnimation daColapse = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.25));

    Storyboard sbExpand = new Storyboard() { FillBehavior = FillBehavior.Stop };
    Storyboard sbColapse = new Storyboard() { FillBehavior = FillBehavior.Stop };

    void InitAnimation()
    {
      base.Height = Double.NaN;
      sbExpand.Children.Add(daExpand);
      sbColapse.Children.Add(daColapse);
      Storyboard.SetTarget(daExpand, this);
      Storyboard.SetTargetProperty(daExpand, new PropertyPath(HeightProperty));
      Storyboard.SetTarget(daColapse, this);
      Storyboard.SetTargetProperty(daColapse, new PropertyPath(HeightProperty));
      sbExpand.Completed += SbExpandColapse_Completed;
      sbColapse.Completed += SbExpandColapse_Completed;
    }

    double CollabsedHeight = 23;
    double ExpandedHeight = 23;
    protected override void OnCollapsed()
    {
      if(IsDesignMode) IsExpanded = true;
      if(IsEnabled) {
        daColapse.From = ExpandedHeight = ActualHeight;
        if(Header is FrameworkElement h) {
          h.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
          daColapse.To = h.DesiredSize.Height;
        } else {
          daColapse.To = CollabsedHeight;
        }
        sbColapse.Begin();
      }
      base.OnCollapsed();
    }
    protected override void OnExpanded()
    {
      if(ActualHeight != 0)
        daExpand.From = CollabsedHeight = ActualHeight;

      if(Content is FrameworkElement c) {
        c.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        daExpand.To = c.DesiredSize.Height + ActualHeight;
        if(!IsDesignMode && IsLoaded)
          c.Visibility = Visibility.Hidden;
      } else {
        daExpand.To = ExpandedHeight;
      }
      sbExpand.Begin();

      base.OnExpanded();
    }
    private void SbExpandColapse_Completed(object sender, EventArgs e)
    {
      base.Height = double.NaN;
      if(Content is FrameworkElement c) {    
        c.Visibility = Visibility.Visible;
      }
    }

    public AnimExpander() : base()
    {
      base.Height = Double.NaN;
      Loaded += (o, e) => InitAnimation();
      IsEnabledChanged += (o, e) => IsExpanded = IsExpanded;
    }
  }
}