using MathParserTK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static WPFControls.WPFControls;
using System.Globalization;

namespace WPFControls
{
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

    /// <summary>
    /// Global function class
    /// </summary>
    internal static class WPFControls
    {
        internal static MathParser mathParser = new MathParser('.');
    }
    [System.Windows.Markup.ContentProperty()]
    public abstract class NumberTextBox<T> : TextBox
        where T : struct, IConvertible, IComparable
        , IEquatable<T>, IComparable<T>
    {
        #region Binding
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected abstract T value { get; set; }

        static protected void propertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NumberTextBox<T> ntb = (NumberTextBox<T>)d;
            if(!ntb.updating)
                ntb.ChangeValue((T)e.NewValue);
        }
        #endregion

        protected T val = new T();

        protected T minValue;
        protected T maxValue;

        [EditorBrowsable(EditorBrowsableState.Never)]
        protected T MinVal {
            get => minValue;
            set {
                minValue = value;
                if(this.value.CompareTo(minValue) < 0)
                    this.value = minValue;
                if(maxValue.CompareTo(minValue) < 0)
                    maxValue = minValue;
            }
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected T MaxVal {
            get => maxValue;
            set {
                maxValue = value;
                if(this.value.CompareTo(maxValue) > 0)
                    this.value = maxValue;
                if(maxValue.CompareTo(minValue) < 0)
                    minValue = maxValue;
            }
        }

        public enum NTBState { Normal, Error, Correct };
        NTBState state = NTBState.Normal;

        public bool IsValueValid { get => state != NTBState.Error; }

        public Brush normalBackground = Brushes.White;
        public Brush errorBackground = Brushes.LavenderBlush;
        public Brush correctBackground = Brushes.MintCream;

        [EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Appearance")]
        public Brush NormalBackground {
            get => normalBackground;
            set { normalBackground = value; State = state; }
        }
        [EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Appearance")]
        public Brush ErrorBackground {
            get => errorBackground;
            set { errorBackground = value; State = state; }
        }
        [EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Appearance")]
        public Brush CorrectBackground {
            get => correctBackground;
            set { correctBackground = value; State = state; }
        }

        public NTBState State {
            get { return state; }
            private set {
                state = value;
                switch(state) {
                case NTBState.Normal:
                    Background = NormalBackground;
                    break;
                case NTBState.Error:
                    Background = ErrorBackground;
                    break;
                case NTBState.Correct:
                    Background = CorrectBackground;
                    break;
                }
            }
        }

        bool updating = false;

        T LimitValue(T val)
        {
            if(val.CompareTo(minValue) < 0)
                return minValue;
            else if(val.CompareTo(maxValue) > 0)
                return maxValue;
            else return val;
        }
        bool LimitValue(ref T val)
        {
            if(val.CompareTo(minValue) < 0) {
                val = minValue;
                return true;
            } else if(val.CompareTo(maxValue) > 0) {
                val = maxValue;
                return true;
            } else {
                return false;
            }
        }

        public event EventHandler<ValueChangingEventArgs<T>> ValueChanging;
        public event EventHandler ValueChanged;

        T oldVal = new T();
        protected void ChangeValue(T v)
        {
            updating = true;
            LimitValue(ref v);
            val = v;
            bool valchanged = !v.Equals(oldVal);
            if(valchanged) {
                ValueChangingEventArgs<T> vcea = new ValueChangingEventArgs<T>(v, oldVal);
                ValueChanging?.Invoke(this, vcea);
                LimitValue(ref vcea.NewVal);
                oldVal = v = vcea.NewVal;
            }
            if(!value.Equals(v)) value = v;
            MathText = v.ToString();
            updating = false;
            State = NTBState.Normal;
            toolTip.IsOpen = false;
            ToolTip = null;
            if(valchanged)
                ValueChanged?.Invoke(this, new EventArgs());
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if(!e.Handled) {
                if(e.Key == Key.Enter) {
                    ChangeValue(val);
                    CaretIndex = Text.Length;
                    e.Handled = true;
                } else if(e.Key == Key.Escape) {
                    ChangeValue(oldVal);
                    CaretIndex = Text.Length;
                    e.Handled = true;
                }
            }
        }

        virtual protected string MathText { get => Text; set => Text = value; }

        ToolTip toolTip = new ToolTip();

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
            get { return "Calculates to\n" + LimitValue(val).ToString(); }
        }

        bool designTime = DesignerProperties.GetIsInDesignMode(new DependencyObject());

        protected override void OnTextChanged(TextChangedEventArgs ea)
        {
            if(!updating) {
                try {
                    if(MathText == "")
                        val = new T();
                    else
                        val = (T)Convert.ChangeType(MathText, typeof(T));
                    toolTip.IsOpen = false;
                    ToolTip = null;
                    State = NTBState.Correct;
                } catch(Exception e1) {
                    try {
                        val = (T)Convert.ChangeType(mathParser.Parse(MathText), typeof(T));
                        toolTip.Content = CalculationResultsPreview;
                        toolTip.Background = CorrectBackground;
                        ToolTip = toolTip;
                        if(!designTime) toolTip.IsOpen = true;
                        State = NTBState.Correct;
                    } catch(Exception e2) {
                        State = NTBState.Error;
                        if(e1.Message == e2.Message)
                            toolTip.Content = e1.Message;
                        else
                            toolTip.Content = e1.Message + "\n" + e2.Message;
                        toolTip.Background = ErrorBackground;
                        ToolTip = toolTip;
                        if(!designTime) toolTip.IsOpen = true;

                    }
                }
            }
            base.OnTextChanged(ea);
        }
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            ChangeValue(val);
            base.OnLostFocus(e);
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


    public class DoubleTextBox : NumberTextBox<double>
    {
        #region Binding

        public static DependencyProperty ValueProperty
             = DependencyProperty.Register("Value",
             typeof(double),
             typeof(NumberTextBox<double>),
             new PropertyMetadata(new double(), propertyChanged/*, coerceValueCallback*/));
        [EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Common")]
        [Bindable(true, BindingDirection.TwoWay)]
        public double Value {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }
        protected override double value { get => Value; set => Value = value; }

        #endregion

        [EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Common")]
        [DefaultValue(-1e100)]
        public double MinValue { get => base.MinVal; set => base.MinVal = value; }
        [DefaultValue(1e100)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Common")]
        public double MaxValue { get => base.MaxVal; set => base.MaxVal = value; }
        public DoubleTextBox() : base()
        {
            MinVal = double.MinValue;
            MaxVal = double.MaxValue;
        }
    }
    public class DecimalTextBox : NumberTextBox<decimal>
    {
        #region Binding

        public static DependencyProperty ValueProperty
             = DependencyProperty.Register("Value",
             typeof(decimal),
             typeof(NumberTextBox<decimal>),
             new PropertyMetadata(new decimal(), propertyChanged));

        [EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Common")]
        [Bindable(true, BindingDirection.TwoWay)]
        public decimal Value {
            get { return (decimal)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }
        protected override decimal value { get => Value; set => Value = value; }

        #endregion

        [DefaultValue(typeof(decimal), "-79228162514264337593543950335")]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Common")]
        public decimal MinValue { get => base.MinVal; set => base.MinVal = value; }
        [DefaultValue(typeof(decimal), "79228162514264337593543950335")]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Common")]
        public decimal MaxValue { get => base.MaxVal; set => base.MaxVal = value; }
        public DecimalTextBox() : base()
        {
            MinVal = decimal.MinValue;
            MaxVal = decimal.MaxValue;
        }
    }
    public class IntTextBox : NumberTextBox<int>
    {
        #region Binding

        public static DependencyProperty ValueProperty
             = DependencyProperty.Register("Value",
             typeof(int),
             typeof(NumberTextBox<int>),
             new PropertyMetadata(new int(), propertyChanged));

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
        public IntTextBox() : base()
        {
            MinVal = int.MinValue;
            MaxVal = int.MaxValue;
        }
    }

    public class UnitTextBox : NumberTextBox<double>
    {
        #region Binding

        public static DependencyProperty ValueProperty
             = DependencyProperty.Register("Value",
             typeof(double),
             typeof(UnitTextBox),
             new PropertyMetadata(new double(), propertyChanged));
        [EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Common")]
        [Bindable(true, BindingDirection.TwoWay)]
        public double Value {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }
        protected override double value { get => Value; set => Value = value; }
        #endregion

        [EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Common")]
        [DefaultValue(-1e100)]
        public double MinValue { get => base.MinVal; set => base.MinVal = value; }
        [DefaultValue(1e100)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Common")]
        public double MaxValue { get => base.MaxVal; set => base.MaxVal = value; }


        protected Unit unit;

        [EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Common")]
        public Unit Unit {
            get => unit;
            set {
                unit = value;
                ChangeValue(val);
            }
        }

        public UnitTextBox() : base()
        {
            unit = new UnitTime("ms");
            InitValues();
        }

        protected override string MathText {
            get { return unit.FormatUnit(Text); }
            set { Text = unit.UnFormat(value.ToString()); }
        }

        override protected string CalculationResultsPreview {
            get { return $"{base.CalculationResultsPreview} {unit.DefID}"; }
        }
    }

    [TypeConverter(typeof(UnitTypeConverter))]
    public abstract class Unit : IEnumerable<string>
    {
        protected string[] ids;
        protected double[] vals;

        protected string defid;
        [EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Common")]
        public string DefID {
            get => defid;
            set {
                try {
                    DefVal = dic[value];
                } catch(KeyNotFoundException e) {
                    throw new ArgumentException("Wrong Default Designator", e);
                }
                defid = value;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public double DefVal { get; protected set; }

        protected Dictionary<string, double> dic;

        public IEnumerator<string> GetEnumerator()
        {
            return ((IEnumerable<string>)ids).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<string>)ids).GetEnumerator();
        }

        public double this[string s] { get => dic[s]; }
        public double this[int i] { get => vals[i]; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public int Length { get => ids.Length; }

        public string FormatUnit(string ExpWithUnits)
        {
            string sout = ExpWithUnits.TrimEnd();
            string unitp = "";
            foreach(string u in this) {
                if(sout.EndsWith(u) && u.Length > unitp.Length)
                    unitp = u;
            }
            if(unitp == "")
                return sout;
            sout = sout.Substring(0, sout.LastIndexOf(unitp));
            return sout + "*" + (this[unitp] / DefVal).ToString();
        }
        public string UnFormat(string ExpWithoutUnits, string u = "")
        {
            if(u == "") u = defid;
            return ExpWithoutUnits + " " + u;
        }

        //     static Unit Load()
        //    {
        ///         return new UnitDB();
        //     }
    }

    public class UnitTypeConverter : TypeConverter
    {
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if(!(value is string))
                return base.ConvertFrom(context, culture, value);
            switch((string)value) {
            case "Time":
            case "WPFControls.UnitTime":
                return new UnitTime();
            case "Wavelength":
            case "WPFControls.UnitWavelength":
                return new UnitWavelength();
            }
            return null;
        }
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if(destinationType == typeof(string) && value is Unit)
                switch(value.GetType().ToString()) {
                case "WPFControls.UnitTime":
                    return "Time";
                case "WPFControls.UnitWavelength":
                    return "Wavelength";
                }
            return base.ConvertTo(context, culture, value, destinationType);
        }
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if(sourceType == typeof(string))
                return true;
            return base.CanConvertFrom(context, sourceType);
        }
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if(destinationType == typeof(string))
                return true;
            return base.CanConvertTo(context, destinationType);
        }
    }
    public class UnitTime : Unit
    {
        //       public static readonly UnitTime DefaultUT = new UnitTime();
        static string[] d_ids = new string[]
            { "as", "fs", "ps", "ns", "us", "ms", "s",
                    "ks", "Ms", "m", "h", "d" };
        static double[] d_vals = new double[]
            { 1e-18, 1e-15, 1e-12, 1e-9, 1e-6, 1e-3, 1.0,
                    1e3, 1e6, 60.0, 60.0*60, 60.0*60*24};
        static Dictionary<string, double> d_dic;

        static UnitTime()
        {
            d_dic = new Dictionary<string, double>(d_ids.Length);
            for(int i = 0; i < d_ids.Length; i++)
                d_dic.Add(d_ids[i], d_vals[i]);
        }

        public UnitTime()
        {
            ids = d_ids;
            vals = d_vals;
            dic = d_dic;
            defid = "s";
            DefVal = dic[defid];
        }

        public UnitTime(string def)
        {

            ids = d_ids;
            vals = d_vals;
            dic = d_dic;
            defid = def;
            try {
                DefVal = dic[defid];
            } catch(KeyNotFoundException e) {
                throw new ArgumentException("Wrong Default Designator", e);
            }
        }
    }
    public class UnitWavelength : Unit
    {
        static string[] d_ids = new string[]
            { "A", "nm", "um", "m"};
        static double[] d_vals = new double[]
            { 1e-1, 1.0, 1e3, 1e9};
        static Dictionary<string, double> d_dic;

        static UnitWavelength()
        {
            d_dic = new Dictionary<string, double>(d_ids.Length);
            for(int i = 0; i < d_ids.Length; i++)
                d_dic.Add(d_ids[i], d_vals[i]);
        }

        public UnitWavelength()
        {
            ids = d_ids;
            vals = d_vals;
            dic = d_dic;
            defid = "nm";
            DefVal = dic[defid];
        }

        public UnitWavelength(string def)
        {

            ids = d_ids;
            vals = d_vals;
            dic = d_dic;
            defid = def;
            try {
                DefVal = dic[defid];
            } catch(KeyNotFoundException e) {
                throw new ArgumentException("Wrong Default Designator", e);
            }
        }
    }
}
