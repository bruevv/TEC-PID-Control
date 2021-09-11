using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace WPFControls
{
  public class OptionButton : Button
  {
    static OptionButton()
    {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(OptionButton), new FrameworkPropertyMetadata(typeof(OptionButton)));
    }
    public OptionButton()
    {
      Click += OptionButton_Click;
      AddHandler(ToggleButton.CheckedEvent, new RoutedEventHandler(OnChecked));
      AddHandler(ToggleButton.UncheckedEvent, new RoutedEventHandler(OnUnChecked));
    }

    private void OptionButton_Click(object sender, RoutedEventArgs e)
    {
      if(e.OriginalSource?.GetType() == typeof(ToggleButton)) {
        e.Handled = true;
        OnOptionClick(e);
      }
    }
 
    #region Dependency Properties
    public static readonly DependencyProperty IsOptionSetProperty =
        DependencyProperty.Register("IsOptionSet", typeof(bool?),
          typeof(OptionButton), new PropertyMetadata(null));
    public static readonly DependencyProperty OptionTextProperty =
        DependencyProperty.Register("OptionText", typeof(string),
          typeof(OptionButton), new PropertyMetadata("A"));
    #endregion Dependency Properties

    [Category("Common")]
    public bool? IsOptionSet {
      get { return (bool?)GetValue(IsOptionSetProperty); }
      set { SetValue(IsOptionSetProperty, value); }
    }
    [Category("Common")]
    public string OptionText {
      get { return (string)GetValue(OptionTextProperty); }
      set { SetValue(OptionTextProperty, value); }
    }

    public event EventHandler Checked;
    public event EventHandler UnChecked;
    public event EventHandler OptionClick;

    protected virtual void OnChecked(object s, EventArgs ea) => Checked?.Invoke(this, ea);
    protected virtual void OnUnChecked(object s, EventArgs ea) => UnChecked?.Invoke(this, ea);
    protected virtual void OnOptionClick(EventArgs ea) => OptionClick?.Invoke(this, ea);
  }
}
