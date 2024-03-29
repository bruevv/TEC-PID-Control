﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace WPFControls
{
  using static WPFControls;

  [TypeConverter(typeof(UnitToStringConverter))]
  public abstract class Unit : IEnumerable<string>
  {
    public static Unit FromType(Type utype, string des = null)
    {
      if(des == null) {
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
      if(utsc == null) utsc = new UnitToStringConverter();
      return (Unit)utsc.ConvertFromString(des);
    }

    protected readonly string defid;

    /// <summary>
    /// The one should have value of "1"
    /// </summary>
    protected abstract string Invariant { get; }

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

    protected abstract Dictionary<string, double> dic { get; }

    public IEnumerator<string> GetEnumerator() => dic.Keys.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => dic.Keys.GetEnumerator();

    public double this[string s] => dic[s];
    public string this[double d] {
      get {
        foreach(var item in dic) {
          if(item.Value == d) return item.Key;
        }
        return null;
      }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public int Length => dic.Count;

    protected Unit()
    {
      defid = Invariant;
      DefVal = dic[defid];
    }

    protected Unit(string def)
    {
      try {
        defid = this[this[def]];
        DefVal = dic[defid];
      } catch(KeyNotFoundException e) {
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
      if(!UnipPairs.ContainsKey(to)) return null;
      return UnipPairs[to].Count != 0;
    }

    internal virtual void InitUnitsToConvertFrom()
        => InitUnitsToConvertFrom(this.GetType());

    static void InitUnitsToConvertFrom(Type to)
    {
      UnipPairs.Add(to, new List<Unit>());

      var atrs = to.GetCustomAttributes(typeof(ConvertsFromAttribute));

      foreach(ConvertsFromAttribute atr in atrs) {
        Type from = atr.Unit;
        UnipPairs[to].Add(GetDefaulUnit(from));
        MethodInfo mi = to.GetMethod(atr.CName);
        Converter c = (Converter)mi.CreateDelegate(typeof(Converter), null);
        Converters.Add(new UnitPair(from, to), c);
      }
    }
    string ConvertOwnUnits(string ExpWithUnits)
    {
      string unitp = "";
      foreach(string u in this) {
        if(ExpWithUnits.EndsWith(u) && u.Length > unitp.Length)
          unitp = u;
      }
      if(unitp != "") {
        string sout = ExpWithUnits.Substring(0, ExpWithUnits.LastIndexOf(unitp));
        return $"{sout}*{ConvertToInvariant(this[unitp]):R}";
      }
      return null;
    }

    string ConvertOtherUnits(string ExpWithUnits)
    {
      Unit unit = null;
      string unitp = "";
      if(HasConverters == null) InitUnitsToConvertFrom();
      foreach(Unit unitfrom in UnipPairs[this.GetType()]) {
        foreach(string u in unitfrom) {
          if(ExpWithUnits.EndsWith(u) && u.Length > unitp.Length) {
            unitp = u;
            unit = unitfrom;
          }
        }
      }
      if(unitp != "") {
        string sout = unit.FormatUnit(ExpWithUnits);
        Converter c = Converters[new UnitPair(unit.GetType(), this.GetType())];
        double res = c(mathParser.Parse(sout));
        return $"{res:R}*{ConvertToInvariant(1):R}";
      }
      return null;
    }

    public double ConvertFromOtherUnits(double val, Type other)
    {
      if(HasConverters == null) InitUnitsToConvertFrom();
      UnitPair up = new UnitPair(other, this.GetType());
      try {
        return Converters[up].Invoke(ConvertFromInvariant(val));
      } catch(KeyNotFoundException e) {
        throw new Exception($"No conversion exists from {other} to {this.GetType()}", e);
      }
    }
    public double ConvertToOtherUnits(double val, Type other)
    {
      if(CheckConverters(other) == null) InitUnitsToConvertFrom(other);
      UnitPair up = new UnitPair(this.GetType(), other);
      try {
        return ConvertToInvariant(Converters[up].Invoke(val));
      } catch(KeyNotFoundException e) {
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
      if(sout != null) return sout;
      sout = ConvertOtherUnits(ExpWithUnits);
      if(sout != null) return sout;
      return ExpWithUnits;
    }
    public string UnFormat(string ExpWithoutUnits, string u = null)
    {
      if(string.IsNullOrEmpty(u)) u = defid;
      if(ExpWithoutUnits == "NaN") return "NaN";
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
      hashCode = hashCode * -1521134295 + defid.GetHashCode();
      hashCode = hashCode * -1521134295 + Name.GetHashCode();
      return hashCode;
    }

    public static bool operator ==(Unit unit1, Unit unit2) => EqualityComparer<Unit>.Default.Equals(unit1, unit2);
    public static bool operator !=(Unit unit1, Unit unit2) => !(unit1 == unit2);
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
    public Type Unit { get; }
    public string Designator;
    public double MinValue;
    public double MaxValue;

    public UnitBoxAttribute(Type unit, string designator,
      double min = double.MinValue, double max = double.MaxValue)
    {
      Unit = unit;
      Designator = designator;
      MinValue = min;
      MaxValue = max;
    }
  }

  public class UnitToStringConverter : TypeConverter
  {
    static Dictionary<string, Type> UnitClasses = new Dictionary<string, Type>();

    static UnitToStringConverter()
    {
      List<Type> lUnitClasses = new List<Type>();

      Assembly assembly = typeof(Unit).Assembly;
      foreach(Type type in assembly.GetTypes()) {
        if(type.IsSubclassOf(typeof(Unit)))
          lUnitClasses.Add(type);
      }

      foreach(Type ut in lUnitClasses) {
        UnitClasses.Add(ut.Name, ut);
        UnitClasses.Add(ut.FullName, ut);
        var atr = (DesignatorsAttribute)ut.GetCustomAttribute(typeof(DesignatorsAttribute));
        foreach(string name in atr.Names) {
          try {
            UnitClasses.Add(name, ut);
          } catch(ArgumentException) { }
        }
      }
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
      if(!(value is string strval))
        return base.ConvertFrom(context, culture, value);
      try {
        string[] spl = strval.Split(':');
        if(spl.Length == 1 || string.IsNullOrEmpty(spl[1]))
          return UnitClasses[spl[0]]
              .GetConstructor(Type.EmptyTypes)
              .Invoke(null);
        else if(spl.Length == 2)
          return UnitClasses[spl[0]]
              .GetConstructor(new Type[] { typeof(string) })
              .Invoke(new object[] { spl[1] });
        else return null;
      } catch(Exception) {
        return null;
      }

    }
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
      if(destinationType == typeof(string) && value is Unit ut)
        return ut.ToString();

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
}