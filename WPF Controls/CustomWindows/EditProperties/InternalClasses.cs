using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;
#pragma warning disable CS8632

namespace CustomWindows.InternalClasses
{
  using System.ComponentModel;
  using WPFControls;

  /// <summary>This class is needed only to Have Accessor 'Properties' 
  /// just as in PropertyBundleBundle</summary>
  class PropertyTypeSelector : DataTemplateSelector
  {
    public DataTemplate StringTemplate { get; set; } = null;
    public DataTemplate FilePathTemplate { get; set; } = null;
    public DataTemplate DierctoryTemplate { get; set; } = null;
    public DataTemplate UnitClassTemplate { get; set; } = null;
    public DataTemplate EnumTemplate { get; set; } = null;
    public DataTemplate EnumRadioTemplate { get; set; } = null;
    public DataTemplate IntTemplate { get; set; } = null;
    public DataTemplate DoubleTemplate { get; set; } = null;
    public DataTemplate UnitBoxTemplate { get; set; } = null;
    public DataTemplate BoolTemplate { get; set; } = null;
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
      if (!(item is PropertyEntry pe)) throw new ArgumentException();

      switch (pe.Value) {
        case string:
          if (pe.HasAttributr<FilePathStringAttribute>()) return FilePathTemplate ?? StringTemplate;
          if (pe.HasAttributr<DirectoryPathStringAttribute>()) return DierctoryTemplate ?? StringTemplate;
          return StringTemplate;
        case Enum:
          return EnumTemplate ?? StringTemplate;
        case int:
          return IntTemplate ?? StringTemplate;
        case Unit:
          return UnitClassTemplate ?? StringTemplate;
        case double:
          if (pe.Unit.IsUnit)
            return UnitBoxTemplate ?? StringTemplate;
          else
            return DoubleTemplate ?? StringTemplate;
        case bool:
          return BoolTemplate ?? StringTemplate;
      }
      return StringTemplate ?? base.SelectTemplate(item, container);
    }
  }

  public static class TypeExtension
  {
    const BindingFlags PropBindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty;
    public static IEnumerable<PropertyInfo> GetEditableProperties(this Type t)
    {
      foreach (PropertyInfo pi in t.GetProperties(PropBindingFlags)) {
        if (pi.Name == "Item" ||
          pi.GetCustomAttribute<EditableAttribute>()?.AllowEdit == false ||
          !(pi.GetCustomAttribute<EditorBrowsableAttribute>()?.State != EditorBrowsableState.Never) ||
        /*pi.GetCustomAttribute<XmlIgnoreAttribute>() != null ||*/
          pi.GetSetMethod() == null) continue;
        yield return pi;
      }
    }
  }
}