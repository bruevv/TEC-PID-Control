using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.CompilerServices;
#pragma warning disable CS8632

namespace CustomWindows.InternalClasses
{
  using CSSettings;
  using CSUtils;
  using System.Windows;
  using WPFControls;
  using WPFUtils;

  public class PropertyEntry : INotifyPropertyChanged, IFormattable
  {
    public event PropertyChangedEventHandler PropertyChanged;
    void NotifyPropertyChanged([CallerMemberName] string mn = "") =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(mn));

    object pValue;
    PropertyInfo pInfo;
    readonly object propertySource;
    Unit propertyUnit;

    public object Value {
      get => pValue;
      set {
        if (pValue != value) {
          pValue = value;
          NotifyPropertyChanged();
          if (pValue is Enum) NotifyPropertyChanged(nameof(EnumValue));
          NotifyPropertyChanged(nameof(HasChanged));
        }
      }
    }

    public EnumDecoration EnumValue {
      get => new EnumDecoration((Enum)pValue);
      set {
        bool hpc = HasChanged;
        if (pValue != value.EValue) {
          pValue = value.EValue;
          NotifyPropertyChanged();
          NotifyPropertyChanged(nameof(Value));
          if (hpc == false) NotifyPropertyChanged(nameof(HasChanged));
          NotifyPropertyChanged(nameof(HasChanged));
        }
      }
    }
    public Type Type => pInfo.PropertyType;
    public string Name { get; init; }
    public object InitialValue { get; private set; }
    public object DefaultValue { get; init; }
    public string Description { get; init; }
    public bool HasChanged => !(InitialValue?.Equals(Value) ?? true);
    public bool IsDefaultValue => DefaultValue?.Equals(Value) ?? false;
    public bool IsShared { get; init; }
    public Unit unit = null;
    public Unit Unit {
      get {
        if (Value is Unit u) return u;
        if (propertyUnit == null) {
          var attr = pInfo.GetCustomAttribute<UnitBoxAttribute>();
          if (attr == null || attr.Unit == typeof(NoUnit)) {
            propertyUnit = Unit.NoUnit;
          } else {
            propertyUnit = Unit.FromType(attr.Unit, attr.Designator);
            format = attr.Format;
          }
        }
        return propertyUnit;
      }
      set { if (Value is Unit) Value = value; }
    }
    public string UnitDes => Unit?.Invariant;
    string format = null;
    public string Format => format;
    double minValue = double.NaN, maxValue = double.NaN;
    public double? MinValue {
      get {
        if (double.IsNaN(minValue)) InitMinMax();
        if (double.IsInfinity(minValue)) return null;
        return minValue;
      }
    }
    public double? MaxValue {
      get {
        if (double.IsNaN(maxValue)) InitMinMax();
        if (double.IsInfinity(maxValue)) return null;
        return maxValue;
      }
    }
    void InitMinMax()
    {
      if (pInfo.GetCustomAttribute<UnitBoxAttribute>() is UnitBoxAttribute attr) {
        minValue = attr.MinValue;
        maxValue = attr.MaxValue;
      } else if (pInfo.GetCustomAttribute<RangeAttribute>() is RangeAttribute attr2) {
        minValue = Convert.ToDouble(attr2.Minimum);
        maxValue = Convert.ToDouble(attr2.Maximum);
      } else {
        minValue = double.NegativeInfinity;
        maxValue = double.PositiveInfinity;
      }
    }
    public override string ToString() => ToString(null, null);
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
      object ret;
      switch (format) {
        case "S": ret = Description; break;
        case "D" when DefaultValue is string s: ret = s == "" ? null : s; break;
        case "D": ret = DefaultValue; break;
        case "I": ret = InitialValue; break;
        case "N": ret = Name; break;
        case "U": ret = Unit; break;
        case "T": ret = Type; break;
        default: ret = Value; break;
      }
      switch (ret) {
        case Enum e: return ((EnumDecoration)e).ToString();
        case double d when Unit.IsUnit: return Unit.UnFormat((d / Unit.DefVal).ToString());
        case null: return null;
        default: return ret.ToString();
      }
    }

    public static object Read(PropertyInfo pi, object o) => o == null ? null : pi.GetMethod.Invoke(o, null);
    public static void Write(PropertyInfo pi, object o, object v)
    {
      if (!v.GetType().IsAssignableTo(pi.PropertyType)) {
        TypeConverter tc = TypeDescriptor.GetConverter(pi.PropertyType);
        if (tc.CanConvertFrom(v.GetType())) v = tc.ConvertFrom(v);
        else throw new ArgumentException();
      }
      pi.SetMethod.Invoke(o, new object[] { v });
    }

    public void ApplyChnages()
    {
      if (HasChanged) {
        try {
          Write(pInfo, propertySource, Value);
          InitialValue = Value;
        } catch (Exception e) {
#if DEBUG
          MessageBox.Show($"Property '{Name}' cannot be set to '{Value}'.\n\n" +
            $"Exception '{e}'", "Error");
#else
          MessageBox.Show($"Property '{Name}' cannot be set to '{Value}'.\n\n" +
            $"Exception '{e.Message}'", "Error");
#endif
          Value = InitialValue;
        }
        NotifyPropertyChanged(nameof(HasChanged));
      }
    }
    public void Restore() { if (HasChanged) { Value = InitialValue; } }
    public void RestoreDefault() => Value = DefaultValue ?? GUtils.GetDefault(Type);
    public PropertyEntry() : this(null, null, "", "", false, null, null) { }
    PropertyEntry(object iV, object dV, string pD, string pN, bool sh, object ps, PropertyInfo pI)
    {
      pInfo = pI;
      Name = pN + (sh ? " (shared)" : "");
      Description = pD;
      DefaultValue = dV;
      InitialValue = pValue = iV;
      PropertyChanged = null;
      propertyUnit = null;
      IsShared = sh;
      propertySource = ps;
    }
    public static PropertyEntry Generate(PropertyInfo pI, object obj, object defobj)
    {
      if (defobj != null && obj.GetType() != defobj.GetType()) throw new ArgumentException();
      if (!pI.DeclaringType.IsInstanceOfType(obj)) throw new ArgumentException();

      object iV = Read(pI, obj);

      object dV = (defobj != null) ? Read(pI, defobj) :
        GUtils.GetDefault(pI.PropertyType);
      string pN, pD;
      if (pI.GetCustomAttribute<DisplayAttribute>() is DisplayAttribute da) {
        pN = da.Name;
        pD = da.Description;
        //     pG = da.
      } else {
        pN = pI.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
        pD = pI.GetCustomAttribute<DescriptionAttribute>()?.Description;
      }
      pN ??= pI.Name;
      bool sH = pI.GetCustomAttribute<SharedPropertyAttribute>() != null;

      return new PropertyEntry(iV, dV, pD, pN, sH, obj, pI);
    }
    public Attr GetAttribute<Attr>() where Attr : Attribute => pInfo.GetCustomAttribute<Attr>();
    public bool HasAttributr<Attr>() where Attr : Attribute => GetAttribute<Attr>() != null;
  }
}