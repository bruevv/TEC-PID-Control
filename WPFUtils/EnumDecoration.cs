using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Data;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace WPFUtils
{
  /// <summary>Provides Enum Presentation for WPF Binding with DescriptionAttribute</summary>
  public struct EnumDecoration
  {
    public Enum EValue;

    public string ShortName => EValue.ToString();
    public string Name => ConvertToName(EValue);
    public string Description {
      get {
        Type t = EValue.GetType();
        if (!dic.ContainsKey(t)) CreateDic(t);

        return dic[t][EValue].desc;
      }
    }
    public IEnumerable<EnumDecoration> EnumDecorations => GetEnumDecorations(EValue.GetType());

    static Dictionary<Type, Dictionary<Enum, (string name, string desc)>> dic = new();

    public static string ConvertToName(Enum en)
    {
      Type t = en.GetType();
      if (!dic.ContainsKey(t)) CreateDic(t);

      return dic[t][en].name;
    }
    public static Enum ConvertFromName(string str, Type t)
    {
      if (t.BaseType != typeof(Enum)) throw new ArgumentException();
      if (!dic.ContainsKey(t)) CreateDic(t);

      return dic[t].FirstOrDefault(x => x.Value.name == str).Key;
    }

    public EnumDecoration(Enum e)
    {
      Type t = e.GetType();
      if (!dic.ContainsKey(t)) CreateDic(t);

      EValue = e;
    }

    static void CreateDic(Type t)
    {
      string[] names = Enum.GetNames(t);
      Array values = Enum.GetValues(t);
      FieldInfo[] fields = t.GetFields(BindingFlags.Static | BindingFlags.Public);
      Dictionary<Enum, (string name, string desc)> d = new(names.Length);
      for (int i = 0; i < names.Length; i++) {
        string name = null;
        string desc = null;
        FieldInfo fi = fields[i];
        if (fi.GetCustomAttribute<DisplayAttribute>() is DisplayAttribute da) {
          name = da.Name;
          desc = da.Description;
        }
        if (fi.GetCustomAttribute<DescriptionAttribute>() is DescriptionAttribute dsa)
          desc = dsa.Description;
        if (fi.GetCustomAttribute<DisplayNameAttribute>() is DisplayNameAttribute dna)
          name = dna.DisplayName;

        name ??= desc;
        name ??= names[i];

        d.Add((Enum)values.GetValue(i), (name, desc));
      }
      dic.Add(t, d);
    }

    public static IEnumerable<EnumDecoration> GetEnumDecorations(Type t)
    {
      foreach (Enum val in Enum.GetValues(t)) yield return new EnumDecoration(val);
    }

    public static implicit operator EnumDecoration(Enum e) => new(e);
    public static implicit operator Enum(EnumDecoration ed) => ed.EValue;

    public override string ToString() => Name;
  }
  public static class EnumExtention
  {
    public static string GetDName(this Enum en) => ((EnumDecoration)en).Name;
    public static string GetDescription(this Enum en) => ((EnumDecoration)en).Description;

    public static string GetName(this Enum e) => $"{e.GetType().Name}:{e}";
  }

  public class EnumToStringConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (targetType != typeof(string) || !(value is Enum en)) throw new ArgumentException();

      return EnumDecoration.ConvertToName(en);
    }
    public object ConvertBack(object value, Type t, object parameter, CultureInfo culture)
    {
      if (t.BaseType != typeof(Enum) || !(value is string str)) throw new ArgumentException();

      return EnumDecoration.ConvertFromName(str, t);
    }
  }

}
