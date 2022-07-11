using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace WPFControls
{
  using static WPFControls;

  [TypeConverter(typeof(UnitToStringConverter))]
  public abstract class Unit : IXmlSerializable
  {
    public static Unit NoUnit => global::WPFControls.NoUnit.Default;
    public bool IsUnit => GetType() != typeof(NoUnit);
    public static Unit FromType(Type utype, string des = null)
    {
      if (utype == typeof(NoUnit)) return NoUnit;
      if (des == null) {
        ConstructorInfo ci = utype.GetConstructor(new Type[] { });
        return (Unit)ci.Invoke(new object[] { });

      } else {
        ConstructorInfo ci = utype.GetConstructor(new Type[] { typeof(string) });
        return (Unit)ci.Invoke(new object[] { des });
      }
    }
    static UnitToStringConverter utsc = null;
    public static Unit FromString(string des)
    {
      utsc ??= new();
      return (Unit)utsc.ConvertFromString(des);
    }

    protected readonly string defid;

    /// <summary>
    /// The one should have value of "1"
    /// </summary>
    internal protected abstract string invariant { get; }
    /// <summary>
    /// The one should have value of "1"
    /// </summary>
    public string Invariant => invariant;

    [EditorBrowsable(EditorBrowsableState.Always)]
    [Category("Common")]
    public string DefID => defid;
    //set {
    //  try {
    //    DefVal = dic[value];
    //  } catch(KeyNotFoundException e) {
    //    throw new ArgumentException("Wrong Default Designator", e);
    //  }
    //  defid = value;
    //}

    public string Name {
      get {
        var atr = (DesignatorsAttribute)GetType().GetCustomAttribute(typeof(DesignatorsAttribute));
        return atr.Names[0];
      }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public double DefVal { get; protected set; }

    internal protected abstract Dictionary<string, double> dic { get; }

    public IEnumerator<string> GetEnumerator() => dic?.Keys?.GetEnumerator();

    public IEnumerable<Unit> AllUniqueVariants => GetAllUniqueVariants(true);
    IEnumerable<Unit> GetAllUniqueVariants(bool incOther = false)
    {
      if (!IsUnit) yield break;
      double? d = null;
      foreach (string udes in this) {
        double nd = this[udes];
        if (nd != d) {
          d = nd;
          yield return Copy(udes);
        }
      }
      if (incOther) {
        if (HasConverters == null) InitUnitsToConvertFrom();
        foreach (Unit ou in UnipPairs[GetType()]) {
          foreach (Unit oudes in ou.GetAllUniqueVariants()) yield return oudes;
        }
      }
    }
    public double this[string s] => dic[s];
    public string this[double d] {
      get {
        foreach (var item in dic) {
          if (item.Value == d) return item.Key;
        }
        return null;
      }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public int Length => dic.Count;

    protected Unit()
    {
      if (GetType() == typeof(NoUnit)) return;
      defid = invariant;
      DefVal = dic[defid];
    }

    protected Unit(string def)
    {
      try {
        defid = this[this[def]];
        DefVal = dic[defid];
      } catch (KeyNotFoundException e) {
        throw new ArgumentException("Wrong Default Designator", e);
      }
    }

    public Unit Copy(string defid) =>
      (Unit)GetType().GetConstructor(new Type[] { typeof(string) }).
      Invoke(new object[] { defid });

    public delegate double Converter(double from);

    struct UnitPair : IEquatable<UnitPair>
    {
      Type From;
      Type To;

      public UnitPair(Type from, Type to) { From = from; To = to; }

      public bool Equals(UnitPair other) => From.Equals(other.From) && To.Equals(other.To);
      public override bool Equals(object obj) => obj is UnitPair up ? Equals(up) : false;

      public override int GetHashCode()
      {
        var hashCode = -1781160927;
        hashCode = hashCode * -1521134295 + From.GetHashCode();
        hashCode = hashCode * -1521134295 + To.GetHashCode();
        return hashCode;
      }

      public static bool operator ==(UnitPair pair1, UnitPair pair2) => pair1.Equals(pair2);
      public static bool operator !=(UnitPair pair1, UnitPair pair2) => !pair1.Equals(pair2);
    }

    static Dictionary<UnitPair, Converter> Converters = new Dictionary<UnitPair, Converter>();
    internal static Dictionary<Type, List<Unit>> UnipPairs = new Dictionary<Type, List<Unit>>();
    static Unit GetDefaulUnit(Type t) =>
        (Unit)t.GetField("Default").GetValue(null);

    internal virtual bool? HasConverters => CheckConverters(this.GetType());

    static bool? CheckConverters(Type to)
    {
      if (!UnipPairs.ContainsKey(to)) return null;
      return UnipPairs[to].Count != 0;
    }

    internal virtual void InitUnitsToConvertFrom()
        => InitUnitsToConvertFrom(this.GetType());

    static void InitUnitsToConvertFrom(Type to)
    {
      UnipPairs.Add(to, new List<Unit>());

      var atrs = to.GetCustomAttributes(typeof(ConvertsFromAttribute));

      foreach (ConvertsFromAttribute atr in atrs) {
        Type from = atr.Unit;
        UnipPairs[to].Add(GetDefaulUnit(from));
        MethodInfo mi = to.GetMethod(atr.CName);
        Converter c = (Converter)mi.CreateDelegate(typeof(Converter), null);
        Converters.Add(new UnitPair(from, to), c);
      }
    }
    string ConvertOwnUnits(string ExpWithUnits)
    {
      if (!IsUnit) return ExpWithUnits;
      string unitp = "";
      foreach (string u in this) {
        if (ExpWithUnits.EndsWith(u) && u.Length > unitp.Length)
          unitp = u;
      }
      if (unitp != "") {
        string sout = ExpWithUnits.Substring(0, ExpWithUnits.LastIndexOf(unitp));
        return $"{sout}*{ConvertToInvariant(this[unitp]):R}";
      }
      return null;
    }

    string ConvertOtherUnits(string ExpWithUnits)
    {
      Unit unit = null;
      string unitp = "";
      if (HasConverters == null) InitUnitsToConvertFrom();
      foreach (Unit unitfrom in UnipPairs[this.GetType()]) {
        foreach (string u in unitfrom) {
          if (ExpWithUnits.EndsWith(u) && u.Length > unitp.Length) {
            unitp = u;
            unit = unitfrom;
          }
        }
      }
      if (unitp != "") {
        string sout = unit.FormatUnit(ExpWithUnits);
        Converter c = Converters[new UnitPair(unit.GetType(), this.GetType())];
        double res = c(mathParser.Parse(sout));
        return $"{res:R}*{ConvertToInvariant(1):R}";
      }
      return null;
    }

    public double ConvertFromOtherUnits(double val, Type other)
    {
      if (!IsUnit) return val;
      if (HasConverters == null) InitUnitsToConvertFrom();
      UnitPair up = new UnitPair(other, this.GetType());
      try {
        return Converters[up].Invoke(ConvertFromInvariant(val));
      } catch (KeyNotFoundException e) {
        throw new Exception($"No conversion exists from {other} to {GetType()}", e);
      }
    }
    public double ConvertToOtherUnits(double val, Type other)
    {
      if (!IsUnit) return val;
      if (CheckConverters(other) == null) InitUnitsToConvertFrom(other);
      UnitPair up = new UnitPair(this.GetType(), other);
      try {
        return ConvertToInvariant(Converters[up].Invoke(val));
      } catch (KeyNotFoundException e) {
        throw new Exception($"No conversion exists to {other} from {this.GetType()}", e);
      }
    }

    internal virtual double ConvertToInvariant(double v) => v / DefVal;
    internal virtual double ConvertFromInvariant(double v) => v * DefVal;

    public string FormatUnit(string ExpWithUnits)
    {
      ExpWithUnits = ExpWithUnits.TrimEnd();
      string sout;
      sout = ConvertOwnUnits(ExpWithUnits);
      if (sout != null) return sout;
      sout = ConvertOtherUnits(ExpWithUnits);
      if (sout != null) return sout;
      return ExpWithUnits;
    }
    public string UnFormat(string ExpWithoutUnits, string u = null)
    {
      if (string.IsNullOrEmpty(u)) u = defid;
      if (ExpWithoutUnits == "NaN") return "NaN";
      else return $"{ExpWithoutUnits} {u}";
    }

    public override string ToString()
    {
      var atr = (DesignatorsAttribute)GetType().GetCustomAttribute(typeof(DesignatorsAttribute));
      return $"{atr.Names[0]}:{DefID}";
    }

    public override bool Equals(object o)
      => o.GetType() == GetType() && ((Unit)o).defid.Equals(defid);

    public override int GetHashCode()
    {
      var hashCode = 290876591;
      hashCode = hashCode * -1521134295 + defid?.GetHashCode() ?? 94838190;
      hashCode = hashCode * -1521134295 + Name?.GetHashCode() ?? 402570209;
      return hashCode;
    }

    public XmlSchema GetSchema() => null;
    public void ReadXml(XmlReader reader) => throw new NotImplementedException();
    public void WriteXml(XmlWriter writer) => writer.WriteString(ToString());

    public static bool operator ==(Unit unit1, Unit unit2) => EqualityComparer<Unit>.Default.Equals(unit1, unit2);
    public static bool operator !=(Unit unit1, Unit unit2) => !(unit1 == unit2);
  }

  [Designators("NoUnit")]
  public class NoUnit : Unit
  {
    public static readonly NoUnit Default = new NoUnit();

    private NoUnit() : base() { }

    internal protected override string invariant => "";
    internal protected override Dictionary<string, double> dic => null;
  }
  [AttributeUsage(AttributeTargets.Class)]
  internal class DesignatorsAttribute : Attribute
  {
    public readonly string[] Names;

    public DesignatorsAttribute(params string[] names) => this.Names = names;

  }
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
  [ImmutableObject(true)]
  internal class ConvertsFromAttribute : Attribute
  {
    public Type Unit { get; }
    public string CName { get; }

    public ConvertsFromAttribute(Type from, string converter)
    {
      Unit = from;
      CName = converter;
    }
  }

  public class UnitBoxAttribute : Attribute
  {
    public Type Unit { get; init; }
    public string Designator { get; init; }
    public double MinValue { get; init; }
    public double MaxValue { get; init; }
    public string Format { get; init; }

    public UnitBoxAttribute(Type unit, string designator,
      double min = double.MinValue, double max = double.MaxValue, string format = null)
    {
      Unit = unit;
      Designator = designator;
      MinValue = min;
      MaxValue = max;
      Format = format;
    }
  }

  public class UnitToStringConverter : TypeConverter
  {
    static readonly Dictionary<string, Type> UnitClasses = new();

    static UnitToStringConverter()
    {
      List<Type> lUnitClasses = new List<Type>();

      Assembly assembly = typeof(Unit).Assembly;
      foreach (Type type in assembly.GetTypes()) {
        if (type.IsSubclassOf(typeof(Unit)))
          lUnitClasses.Add(type);
      }

      foreach (Type ut in lUnitClasses) {
        UnitClasses.Add(ut.Name, ut);
        UnitClasses.Add(ut.FullName, ut);
        var atr = (DesignatorsAttribute)ut.GetCustomAttribute(typeof(DesignatorsAttribute));
        foreach (string name in atr.Names) {
          try {
            UnitClasses.Add(name, ut);
          } catch (ArgumentException) { }
        }
      }
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
      if (!(value is string strval))
        return base.ConvertFrom(context, culture, value);
      try {
        string[] spl = strval.Split(':');
        if (spl.Length == 1 || string.IsNullOrEmpty(spl[1].Trim()))
          return Unit.FromType(UnitClasses[spl[0].Trim()]);
        else if (spl.Length == 2)
          return Unit.FromType(UnitClasses[spl[0].Trim()], spl[1].Trim());
        else return null;
      } catch (Exception) {
        return null;
      }
    }
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
      if (destinationType == typeof(string) && value is Unit ut)
        return ut.ToString();

      return base.ConvertTo(context, culture, value, destinationType);
    }
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
      if (sourceType == typeof(string))
        return true;
      return base.CanConvertFrom(context, sourceType);
    }
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
      if (destinationType == typeof(string))
        return true;
      return base.CanConvertTo(context, destinationType);
    }

  }
}