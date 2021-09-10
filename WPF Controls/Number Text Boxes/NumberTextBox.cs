using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace WPFControls
{
  using System.Windows.Data;
  using static WPFControls;

  public class ValueChangingEventArgs<T> : EventArgs
      where T : struct, IConvertible, IComparable
      , IEquatable<T>, IComparable<T>
  {
    public T NewVal;
    public readonly T OldVal;

    public ValueChangingEventArgs(T newval, T oldval) : base()
    {
      NewVal = newval;
      OldVal = oldval;
    }
  }
  public class ValueUpdatedEventArgs : EventArgs
  {
    public bool IsChangedByInput { get; }
    public bool IsChanged { get; }

    public ValueUpdatedEventArgs(bool frominput = false, bool isChanged = true) : base()
    {
      IsChangedByInput = frominput;
      IsChanged = isChanged;
    }
  }

  /// <summary>
  /// Global function class
  /// </summary>
  internal static class WPFControls
  {
    internal static MathParserTK.MathParser mathParser =
        new MathParserTK.MathParser('.');
  }

  /// <summary> For property bindings </summary>
  public abstract class NumberTextBox_Base : TextBox
  {
    public abstract void UpdateBackground();

    [EditorBrowsable(EditorBrowsableState.Always), Category("Brush")]
    public Brush NormalBackground {
      get { return (Brush)GetValue(NormalBackgroundProperty); }
      set { SetValue(NormalBackgroundProperty, value); }
    }
    [EditorBrowsable(EditorBrowsableState.Always), Category("Brush")]
    public Brush ErrorBackground {
      get { return (Brush)GetValue(ErrorBackgroundProperty); }
      set { SetValue(ErrorBackgroundProperty, value); }
    }
    [EditorBrowsable(EditorBrowsableState.Always), Category("Brush")]
    public Brush CorrectBackground {
      get { return (Brush)GetValue(CorrectBackgroundProperty); }
      set { SetValue(CorrectBackgroundProperty, value); }
    }
    public static readonly DependencyProperty NormalBackgroundProperty =
        DependencyProperty.Register("NormalBackground",
        typeof(Brush), typeof(NumberTextBox_Base),
        new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.None,
          Background_PC));
    public static readonly DependencyProperty ErrorBackgroundProperty =
        DependencyProperty.Register("ErrorBackground",
        typeof(Brush), typeof(NumberTextBox_Base),
        new FrameworkPropertyMetadata(Brushes.LavenderBlush, FrameworkPropertyMetadataOptions.None,
          Background_PC));
    public static readonly DependencyProperty CorrectBackgroundProperty =
        DependencyProperty.Register("CorrectBackground",
        typeof(Brush), typeof(NumberTextBox_Base),
        new FrameworkPropertyMetadata(Brushes.MintCream, FrameworkPropertyMetadataOptions.None,
          Background_PC));

    static void Background_PC(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
      NumberTextBox_Base o = (NumberTextBox_Base)obj;

      o.UpdateBackground();
    }
  }

  [System.Windows.Markup.ContentProperty()]
  public abstract class NumberTextBox<T> : NumberTextBox_Base
      where T : struct, IConvertible, IComparable
      , IEquatable<T>, IComparable<T>
  {
    #region Binding
    protected abstract T value { get; set; }

    static protected void propertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      NumberTextBox<T> ntb = (NumberTextBox<T>)d;
      if(!ntb.updating) ntb.ChangeValue(ntb.ValueFromInvariant((T)e.NewValue));
    }
    #endregion

    public override void UpdateBackground() => State = state;

    protected virtual T ValueFromInvariant(T from) => from;
    protected virtual T ValueToInvariant(T to) => to;

    protected T val = new T();

    protected T minValue;
    protected T maxValue;

    abstract protected T NaN { get; }
    abstract protected bool IsNaNVal(T value);
    public bool IsNaN => IsNaNVal(val);
    virtual protected string NaNString => "-";
    public virtual string ValueFormat {
      get => null;
      set => throw new NotImplementedException();
    }
    protected T MinVal {
      get => minValue;
      set {
        minValue = value;
        if(maxValue.CompareTo(minValue) < 0)
          maxValue = minValue;
        LimitValue(ref val);
      }
    }
    protected T MaxVal {
      get => maxValue;
      set {
        maxValue = value;
        if(maxValue.CompareTo(minValue) < 0)
          minValue = maxValue;
        LimitValue(ref val);
      }
    }

    public enum NTBState { Normal, Error, Correct };
    NTBState state = NTBState.Normal;

    public bool IsValueValid { get => state != NTBState.Error; }

    [Browsable(false)]
    public new Brush Background {
      get { return base.Background; }
      set { base.Background = value; }
    }

    [Browsable(false)]
    public new string Text {
      get { return base.Text; }
      set { base.Text = value; }
    }

    public NTBState State {
      get { return state; }
      protected set {
        state = value;
        switch(state) {
          case NTBState.Normal:
            Background = NormalBackground ?? (Brush)NormalBackgroundProperty.DefaultMetadata.DefaultValue;
            break;
          case NTBState.Error:
            Background = ErrorBackground ?? (Brush)ErrorBackgroundProperty.DefaultMetadata.DefaultValue;
            break;
          case NTBState.Correct:
            Background = CorrectBackground ?? (Brush)CorrectBackgroundProperty.DefaultMetadata.DefaultValue;
            break;
        }
      }
    }

    protected bool updating = false;

    T LimitValue(T val)
    {
      T v = ValueToInvariant(val);

      if(v.CompareTo(minValue) < 0)
        return ValueFromInvariant(minValue);
      else if(v.CompareTo(maxValue) > 0)
        return ValueFromInvariant(maxValue);
      else return val;
    }
    bool LimitValue(ref T val)
    {
      if(IsNaNVal(val)) return false;

      T v = ValueToInvariant(val);

      if(v.CompareTo(minValue) < 0) {
        val = ValueFromInvariant(minValue);
        return true;
      } else if(v.CompareTo(maxValue) > 0) {
        val = ValueFromInvariant(maxValue);
        return true;
      } else {
        return false;
      }
    }

    public event EventHandler<ValueChangingEventArgs<T>> ValueChanging;
    /// <summary>
    /// Raised either my User Input either by Property change (including Binding).
    /// In the second case it's raised even if the value if not changed (to confirm user input).
    /// </summary>
    public event EventHandler<ValueUpdatedEventArgs> ValueUpdated;

    T oldVal = new T();
    protected void ChangeValue(T v)
    {
      updating = true;
      LimitValue(ref v);
      val = v;
      bool valchanged = !v.Equals(oldVal);

      if(valchanged) {
        var vcea = new ValueChangingEventArgs<T>(v, oldVal);
        OnValueChanging(vcea);
        LimitValue(ref vcea.NewVal);
        oldVal = v = vcea.NewVal;
      }
      if(!value.Equals(v)) value = v;
      SetMathText(v);
      updating = false;
      State = NTBState.Normal;
      toolTip.IsOpen = false;
      ToolTip = null;
      if(valchanged || !ValChangedByInput)
        OnValueUpdated(new ValueUpdatedEventArgs(ValChangedByInput, valchanged));
      ValChangedByInput = false;
    }
    static System.Globalization.CultureInfo CI;
    static NumberTextBox()
    {
      CI = new System.Globalization.CultureInfo("");
      CI.NumberFormat.NumberGroupSeparator = "";
    }
    protected void SetMathText(T v)
    {
      if(IsNaNVal(v))
        MathText = NaNString;
      else if(ValueFormat == null)
        MathText = v.ToString(CI);
      else
        MathText = string.Format(CI, $"{{0:{ValueFormat}}}", v);
    }

    protected virtual void OnValueChanging(ValueChangingEventArgs<T> vcea)
          => ValueChanging?.Invoke(this, vcea);
    protected virtual void OnValueUpdated(ValueUpdatedEventArgs ea)
          => ValueUpdated?.Invoke(this, ea);

    bool ValChangedByInput = false;
    protected override void OnKeyDown(KeyEventArgs e)
    {
      if(!e.Handled) {
        if(e.Key == Key.Enter) {
          if(State == NTBState.Error)
            e.Handled = true;
          ValChangedByInput = true;
          ChangeValue(val);
          CaretIndex = Text.Length;
        } else if(e.Key == Key.Escape) {
          ChangeValue(oldVal);
          CaretIndex = Text.Length;
          //               e.Handled = true;
        }
      }
      base.OnKeyDown(e);
    }

    virtual protected string MathText { get => Text; set => Text = value; }

    protected ToolTip toolTip = new ToolTip();

    protected override void OnInitialized(EventArgs e)
    {
      toolTip.PlacementTarget = this;
      toolTip.Placement =
          System.Windows.Controls.Primitives.PlacementMode.Bottom;
      if(State != NTBState.Normal)
        ChangeValue(val);
      base.OnInitialized(e);
    }
    protected virtual string CalculationResultsPreview {
      get { return $"Calculates to\n{LimitValue(val):G6}"; }
    }

    protected bool suppressToolTip = DesignerProperties.GetIsInDesignMode(new DependencyObject());

    protected override void OnTextChanged(TextChangedEventArgs ea)
    {
      string mathText = "";
      if(!updating) {
        try {
          mathText = MathText;
          if(string.IsNullOrWhiteSpace(mathText))
            val = new T();
          else if(mathText.Trim() == NaNString)
            val = NaN;
          else
            val = (T)Convert.ChangeType(mathText, typeof(T));
          toolTip.IsOpen = false;
          ToolTip = null;
          State = NTBState.Correct;
        } catch(Exception e1) {
          try {
            val = (T)Convert.ChangeType(mathParser.Parse(mathText), typeof(T));
            toolTip.Content = CalculationResultsPreview;
            toolTip.Background = CorrectBackground;
            ToolTip = toolTip;
            if(!suppressToolTip) toolTip.IsOpen = true;
            State = NTBState.Correct;
          } catch(Exception e2) {
            State = NTBState.Error;
            if(e1.Message == e2.Message)
              toolTip.Content = e1.Message;
            else
              toolTip.Content = $"{e1.Message}\n{e2.Message}";
            toolTip.Background = ErrorBackground;
            ToolTip = toolTip;
            if(!suppressToolTip) toolTip.IsOpen = true;

          }
        }
      }
      base.OnTextChanged(ea);
    }
    protected override void OnLostFocus(RoutedEventArgs e)
    {
      ValChangedByInput = true;
      ChangeValue(val);
      base.OnLostFocus(e);
    }
    public void UpdateUserInput()
    {
      ValChangedByInput = true;
      ChangeValue(val);
    }

    T GetDefaulVal(string prop)
    {
      MemberInfo mi = this?.GetType()?.GetMember(prop)[0];
      DefaultValueAttribute dva = null;
      if(mi != null)
        dva = (DefaultValueAttribute)Attribute.GetCustomAttribute(mi, typeof(DefaultValueAttribute));
      return (T)((dva?.Value) ?? new T());
    }

    protected void InitValues()
    {
      MinVal = GetDefaulVal("MinValue");
      MaxVal = GetDefaulVal("MaxValue");
    }
  }

  [DefaultEvent("ValueChanged")]
  public class DoubleTextBox : NumberTextBox<double>
  {
    #region Binding

    public static DependencyProperty ValueProperty
         = DependencyProperty.Register("Value",
         typeof(double),
         typeof(NumberTextBox<double>),
         new FrameworkPropertyMetadata(new double(), propertyChanged) { BindsTwoWayByDefault = true });
    [EditorBrowsable(EditorBrowsableState.Always)]
    [Category("Common")]
    [Bindable(true, BindingDirection.TwoWay)]
    public double Value {
      get { return (double)GetValue(ValueProperty); }
      set { SetValue(ValueProperty, value); }
    }
    protected override double value { get => Value; set => Value = value; }

    public static DependencyProperty ValueFormatProperty
         = DependencyProperty.Register("ValueFormat",
         typeof(string),
         typeof(DoubleTextBox),
         new FrameworkPropertyMetadata("N", ValueFormatChanged) { BindsTwoWayByDefault = true });
    [EditorBrowsable(EditorBrowsableState.Always)]
    [Category("Common")]
    [Bindable(true, BindingDirection.TwoWay)]
    public override string ValueFormat {
      get { return (string)GetValue(ValueFormatProperty); }
      set { SetValue(ValueFormatProperty, value); }
    }
    #endregion

    [EditorBrowsable(EditorBrowsableState.Always)]
    [Category("Common")]
    [DefaultValue(-1e100)]
    public double MinValue { get => base.MinVal; set => base.MinVal = value; }
    [DefaultValue(1e100)]
    [EditorBrowsable(EditorBrowsableState.Always)]
    [Category("Common")]
    public double MaxValue { get => base.MaxVal; set => base.MaxVal = value; }
    protected override double NaN => double.NaN;
    protected override bool IsNaNVal(double v) => double.IsNaN(v);

    static void ValueFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      DoubleTextBox ntb = (DoubleTextBox)d;
      ntb.ChangeValue(ntb.Value);
    }

    public DoubleTextBox() : base()
    {
      Text = "0.0";
      MinVal = double.MinValue;
      MaxVal = double.MaxValue;
    }
  }
  [DefaultEvent("ValueChanged")]
  public class IntTextBox : NumberTextBox<int>
  {
    #region Binding

    public static DependencyProperty ValueProperty
         = DependencyProperty.Register("Value",
         typeof(int),
         typeof(NumberTextBox<int>),
         new FrameworkPropertyMetadata(new int(), propertyChanged) { BindsTwoWayByDefault = true });

    [EditorBrowsable(EditorBrowsableState.Always)]
    [Category("Common")]
    [Bindable(true, BindingDirection.TwoWay)]
    public int Value {
      get { return (int)GetValue(ValueProperty); }
      set { SetValue(ValueProperty, value); }
    }

    protected override int value { get => Value; set => Value = value; }
    #endregion

    [DefaultValue(int.MinValue)]
    [EditorBrowsable(EditorBrowsableState.Always)]
    [Category("Common")]
    public int MinValue { get => base.MinVal; set => base.MinVal = value; }
    [EditorBrowsable(EditorBrowsableState.Always)]
    [Category("Common")]
    [DefaultValue(int.MaxValue)]
    public int MaxValue { get => base.MaxVal; set => base.MaxVal = value; }
    protected override int NaN => int.MinValue;
    protected override bool IsNaNVal(int v) => v == NaN;

    public IntTextBox() : base()
    {
      Text = "0";
      MinVal = int.MinValue;
      MaxVal = int.MaxValue;
    }
  }

  [DefaultEvent("ValueChanged")]
  public class UnitTextBox : DoubleTextBox
  {
    #region Binding

    public static DependencyProperty UnitProperty
         = DependencyProperty.Register("Unit",
         typeof(Unit),
         typeof(UnitTextBox),
         new FrameworkPropertyMetadata(new UnitTime(), UnitChanged) { BindsTwoWayByDefault = true });

    [EditorBrowsable(EditorBrowsableState.Always)]
    [Category("Common")]
    [Bindable(true, BindingDirection.TwoWay)]
    public Unit Unit {
      get => (Unit)GetValue(UnitProperty);
      set => SetValue(UnitProperty, value);
    }
    #endregion Binding
    static UnitTextBox()
    {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(UnitTextBox), new FrameworkPropertyMetadata(typeof(UnitTextBox)));
    }
    Type outputUnits = null;

    [EditorBrowsable(EditorBrowsableState.Always)]
    [Category("Common")]
    [DefaultValue(null)]
    public Type OutputUnits {
      get => outputUnits;
      set {
        Type unitt = typeof(Unit);
        if(value.BaseType != unitt)
          throw new ArgumentException($"{nameof(OutputUnits)} should be type derived from {unitt.FullName}");
        outputUnits = value;
      }
    }
    static protected void UnitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      UnitTextBox ntb = (UnitTextBox)d;
      if(e.NewValue == e.OldValue) return;
      bool b = ntb.suppressToolTip;
      ntb.suppressToolTip = true;
      ntb.ChangeValue(ntb.value);
      ntb.suppressToolTip = b;
      if(ntb.OutputUnits == null) ntb.OutputUnits = e.NewValue.GetType();
    }
    protected override double ValueFromInvariant(double from)
    {
      if(outputUnits == null || outputUnits.Equals(Unit.GetType()))
        return Unit.ConvertToInvariant(from);
      else
        return Unit.ConvertToOtherUnits(from, outputUnits);
    }
    protected override double ValueToInvariant(double to)
    {
      if(outputUnits == null || outputUnits.Equals(Unit.GetType()))
        return Unit.ConvertFromInvariant(to);
      else
        return Unit.ConvertFromOtherUnits(to, outputUnits);
      ;
    }

    protected override double value {
      get => ValueFromInvariant(Value);
      set => Value = ValueToInvariant(value);
    }
    double oldval = 0; // TODO why not to use OldValue from argument?!
    protected override void OnValueChanging(ValueChangingEventArgs<double> vcea)
    {
      var nea = new ValueChangingEventArgs<double>(ValueToInvariant(vcea.NewVal), oldval);
      if(nea.NewVal != nea.OldVal) {
        base.OnValueChanging(nea);
        vcea.NewVal = ValueFromInvariant(nea.NewVal);
        oldval = nea.NewVal;
      }
    }
    public UnitTextBox() : base()
    {
      InitValues();
      ContextMenu = null;
      ContextMenuOpening += UnitTextBox_ContextMenuOpening;
    }

    void UnitTextBox_ContextMenuOpening(object o, ContextMenuEventArgs rea)
    {
      if(ContextMenu == null && IsEnabled) {
        UpdateContextMenu();
        ContextMenu.IsOpen = true;
        rea.Handled = true;
      }
    }

    MenuItem OtherUnitsMI = null;
    void UpdateContextMenu()
    {
      if(ContextMenu == null) {
        ContextMenu = new ContextMenu();

        ContextMenu.Items.Add(new MenuItem() { Command = ApplicationCommands.Copy });
        ContextMenu.Items.Add(new MenuItem() { Command = ApplicationCommands.Cut });
        ContextMenu.Items.Add(new MenuItem() { Command = ApplicationCommands.Paste });

        ContextMenu.Items.Add(new Separator());

        OtherUnitsMI = new MenuItem() {
          Header = "Other Units",
          IsCheckable = false,
        };
        ContextMenu.Items.Add(OtherUnitsMI);
      } else {
        List<MenuItem> l = new List<MenuItem>();
        foreach(object cmi in ContextMenu.Items) {
          if(cmi is MenuItem mi) {
            if(mi.Tag != null) l.Add(mi);
          }
        }
        foreach(MenuItem mi in l) {
          ContextMenu.Items.Remove(mi);
        }
        l.Clear();
        foreach(object cmi in OtherUnitsMI.Items) {
          l.Add((MenuItem)cmi);
        }
        foreach(MenuItem mi in l) {
          OtherUnitsMI.Items.Remove(mi);
        }
      }

      if(Unit.HasConverters == null) Unit.InitUnitsToConvertFrom();
      if(Unit.UnipPairs[Unit.GetType()].Count == 0) {
        OtherUnitsMI.IsEnabled = false;
      } else {
        OtherUnitsMI.IsEnabled = true;

        foreach(Unit u in Unit.UnipPairs[Unit.GetType()]) {
          MenuItem mi = new MenuItem() {
            Header = u.Name,
            IsCheckable = false,
            Tag = u
          };
          mi.Click += OtherUnitSelected;
          OtherUnitsMI.Items.Add(mi);
        }
      }
      string name = Unit.Name;
      foreach(string key in Unit) {
        if(key == Unit[Unit[key]]) {
          MenuItem mi = new MenuItem() {
            Header = $"{name}({key})",
            Tag = key,
            IsCheckable = Unit.DefID != key,
            IsChecked = Unit.DefID == key,
          };
          mi.Checked += UnitSelected;
          ContextMenu.Items.Add(mi);
        }
      }
    }

    bool checking = false;

    void UnitSelected(object o, RoutedEventArgs rea)
    {
      if(checking) return;
      checking = true;
      foreach(object mio in ContextMenu.Items) {
        if(mio is MenuItem mi) {
          if(mi.IsChecked && mi.Tag is string) {
            mi.IsChecked = false;
            mi.IsCheckable = true;
          }
        }
      }
      MenuItem smi = (MenuItem)o;
      smi.IsChecked = true;
      smi.IsCheckable = false;
      Unit = Unit.Copy((string)smi.Tag);
      bool b = suppressToolTip;
      suppressToolTip = true;
      ChangeValue(value);
      suppressToolTip = b;
      checking = false;
    }
    void OtherUnitSelected(object o, RoutedEventArgs rea)
    {
      if(checking) return;
      checking = true;

      MenuItem smi = (MenuItem)o;
      Unit = (Unit)smi.Tag;
      checking = false;
      UpdateContextMenu();
    }

    protected override string MathText {
      get => Unit.FormatUnit(base.MathText);
      set => base.MathText = Unit.UnFormat(value);
    }
    override protected string CalculationResultsPreview {
      get { return $"{base.CalculationResultsPreview} {Unit.DefID}"; }
    }
  }

}
