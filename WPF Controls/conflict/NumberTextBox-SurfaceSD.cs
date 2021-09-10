using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static WPFControls.WPFControls;
using MathParserTK;
using System.Windows.Input;
using System.ComponentModel;

namespace WPFControls
{
    /// <summary>
    /// Global function class
    /// </summary>
    static class WPFControls {
        static public MathParser mathParser = new MathParser('.');
    }

    public abstract class NumberTextBox<T> : TextBox
        where T : struct, IConvertible, IComparable
        , IEquatable<T>, IComparable<T>
    {
        #region Binding
        public static DependencyProperty ValProperty
         = DependencyProperty.Register(
             "Val",
             typeof(T),
             typeof(NumberTextBox<T>),
             new PropertyMetadata(new T(), propertyChanged));

        [EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Common")]
        [Bindable(true, BindingDirection.TwoWay)]
        public T Val {
            get { return (T)GetValue(ValProperty); }
            set {
                if (value.CompareTo(minValue) < 0)
                    SetValue(ValProperty, minValue);
                else if (value.CompareTo(maxValue) > 0)
                    SetValue(ValProperty, maxValue);
                else SetValue(ValProperty, value);
            }
        }
        
        static void propertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NumberTextBox<T> ntb = (NumberTextBox<T>)d;
            ntb.OnUpdate();
        }
        #endregion

        T Value = new T();

        T minValue;
        T maxValue;
        [EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Common")]
        protected T MinValue {
            get => minValue; 
            set {
                minValue = value;
                if (Val.CompareTo(minValue) < 0)
                    Val = minValue;
                if (maxValue.CompareTo(minValue) < 0)
                    maxValue = minValue;
            }
        }
        protected T MaxValue {
            get => maxValue;
            set {
                maxValue = value;
                if (Val.CompareTo(maxValue) > 0)
                    Val = maxValue;
                if (maxValue.CompareTo(minValue) < 0)
                    minValue = maxValue;
            }
        }
             
        public enum NTBState { Normal, Error, Correct };
        NTBState state = NTBState.Normal;

        public NTBState State {
            get { return state; }
            private set {
                state = value;
                switch (state) {
                    case NTBState.Normal:
                        Background = Brushes.White;
                        break;
                    case NTBState.Error:
                        Background = Brushes.LavenderBlush;
                        break;
                    case NTBState.Correct:
                        Background = Brushes.MintCream;
                        break;
                }
            }
        }

 
        bool updating = false;
        void OnUpdate()
        {
            updating = true;
            Value = Val;
            Text = Value.ToString();
            updating = false;
            State = NTBState.Normal;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!e.Handled) {
                if (e.Key == Key.Enter) {
                    if (Val.Equals(Value))
                        OnUpdate();
                    else
                        Val = Value;
                    CaretIndex = Text.Length;
                    e.Handled = true;
                }
            }
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if (!updating) {
                try {
                    Value = (T)Convert.ChangeType(Text, typeof(T));
                    State = NTBState.Normal;
                    ToolTip = null;
                } catch (Exception) {
                    try {
                        Value = (T)Convert.ChangeType(mathParser.Parse(Text), typeof(T));
                        State = NTBState.Correct;
                    } catch {
                        State = NTBState.Error;
                    }
                }
            }
            base.OnTextChanged(e);
        }
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            if (Val.Equals(Value))
                OnUpdate();
            else
                Val = Value;
            base.OnLostFocus(e);
        }
    }

    public class DoubleTextBox : NumberTextBox<double>
    {
       [EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Common")]
        [DefaultValue(double.MinValue)]
        public new double MinValue { get => base.MinValue; set => base.MinValue = value; }
        [DefaultValue(double.MaxValue)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Common")]
        public new double MaxValue { get => base.MaxValue; set => base.MaxValue = value; }
        public DoubleTextBox() : base()
        {
            MinValue = double.MinValue;
            MaxValue = double.MaxValue;
        }
    }
    public class DecimalTextBox : NumberTextBox<decimal>
    {
        [DefaultValue(typeof(decimal),"-79228162514264337593543950335")]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Common")]
        public new decimal MinValue { get => base.MinValue; set => base.MinValue = value; }
        [DefaultValue(typeof(decimal), "79228162514264337593543950335")]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Common")]
        public new decimal MaxValue { get => base.MaxValue; set => base.MaxValue = value; }
        public DecimalTextBox() : base()
        {
            MinValue = decimal.MinValue;
            MaxValue = decimal.MaxValue;
        }
    }
    public class IntTextBox : NumberTextBox<int>
    {
        [DefaultValue(int.MinValue)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Common")]
        public new int MinValue { get => base.MinValue; set => base.MinValue = value; }
       [EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Common")]
        [DefaultValue(int.MaxValue)]
        public int MaxValue1 { get => base.MaxValue; set => base.MaxValue = value; }
        public IntTextBox() : base()
        {
            MinValue = int.MinValue;
            MaxValue = int.MaxValue;
        }
    }
}
