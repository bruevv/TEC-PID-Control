using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CustomWindows
{
  public class SimpleToolWindow : Window
  {

    #region DependancyProperties
    public bool MinimizeButtonVisible {
      get { return (bool)GetValue(MinimizeButtonVisibleProperty); }
      set { SetValue(MinimizeButtonVisibleProperty, value); }
    }

    // Using a DependencyProperty as the backing store for CloseButtonVisible.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty MinimizeButtonVisibleProperty =
        DependencyProperty.Register("MinimizeButtonVisible", typeof(bool), typeof(SimpleToolWindow), new PropertyMetadata(true));

    public bool CloseButtonVisible {
      get { return (bool)GetValue(CloseButtonVisibleProperty); }
      set { SetValue(CloseButtonVisibleProperty, value); }
    }

    // Using a DependencyProperty as the backing store for CloseButtonVisible.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty CloseButtonVisibleProperty =
        DependencyProperty.Register("CloseButtonVisible", typeof(bool), typeof(SimpleToolWindow), new PropertyMetadata(true));
    #endregion DependancyProperties

    static SimpleToolWindow()
    {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(SimpleToolWindow),
          new FrameworkPropertyMetadata(typeof(SimpleToolWindow)));
    }

    public override void OnApplyTemplate()
    {
      Button closeButton = GetTemplateChild("closeButton") as Button;
      if (closeButton != null) closeButton.Click += CloseButton_Click;

      Button minimizeButton = GetTemplateChild("minimizeButton") as Button;
      if (minimizeButton != null) minimizeButton.Click += MinimizeButton_Click; ;

      UIElement DragRectange = GetTemplateChild("DragRectange") as UIElement;
      if (DragRectange != null) DragRectange.PreviewMouseDown += Drag_PreviewMouseDown;

      UIElement TitleTextBlock = GetTemplateChild("TitleTextBlock") as UIElement;
      if (TitleTextBlock != null) TitleTextBlock.PreviewMouseDown += Drag_PreviewMouseDown;
      base.OnApplyTemplate();
    }

    private void Drag_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (Mouse.LeftButton == MouseButtonState.Pressed) {

        DragMove();
      }
    }
    protected void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    protected void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnStateChanged(EventArgs e)
    {
      if (WindowState == WindowState.Maximized) {
        Top = 0; Left = 0;
        Width = Math.Min(MaxWidth, SystemParameters.WorkArea.Width);
        Height = Math.Min(MaxHeight, SystemParameters.WorkArea.Height);
        WindowState = WindowState.Normal;
      } else {
        base.OnStateChanged(e);
      }
    }
  }

}
