using CSSettings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Windows.Data;
using WPFControls;
#pragma warning disable CS8632

namespace CustomWindows.InternalClasses
{
  public class PropertyBundle : IEnumerable
  {
    static bool showAdvanced = true;
    public static bool ShowAdvanced {
      get => showAdvanced;
      set {
        if (showAdvanced != value) {
          showAdvanced = value;
        }
      }
    }

    public string BundleName { get; init; }
    public string BundleDescription {
      get;
      init;
    }

    public PropertyCollection Properties { get; init; }
    public BundleCollection PropertyBundles { get; init; }

    PropertyBundle(string name, string desc, PropertyCollection props, BundleCollection bundles, bool iav)
    {
      BundleName = name;
      BundleDescription = desc;
      Properties = props;
      PropertyBundles = bundles;
      IsAdvancedVisible = iav;
    }
    public static PropertyBundle BuildPropertyBundle(object obj, object defobj)
    {
      if (defobj != null && obj.GetType() != defobj.GetType()) throw new ArgumentException();

      Type t = obj.GetType();

      string name = null, desc = null;
      if (t.GetCustomAttribute<DisplayAttribute>() is DisplayAttribute da) {
        name = da.Name;
        desc = da.Description;
      } else {
        if (t.GetCustomAttribute<DisplayNameAttribute>() is DisplayNameAttribute dna)
          name = dna.DisplayName;
        if (t.GetCustomAttribute<DescriptionAttribute>() is DescriptionAttribute dca)
          desc = dca.Description;
      }
      name ??= "General";
      return BuildPropertyBundle(obj, defobj, name, desc, null);
    }

    static Dictionary<Type, bool> CanUseTypeAsPropertyEntry = new() {
      { typeof(string), true }
    };

    static PropertyBundle BuildPropertyBundle(object obj, object defobj, string n, string d, Dictionary<string, PropertyEntry> sharedPE)
    {
      Type t = obj.GetType();
      IEnumerable<PropertyInfo> pia = t.GetEditableProperties();

      if (sharedPE == null) {
        if (t.GetCustomAttribute<SharedPropertyAttribute>() == null) sharedPE = null;
        else sharedPE = new();
      }

      PropertyCollection pel = new();
      List<PropertyBundle> pbl = new();

      foreach (PropertyInfo pi in pia) {
        Type pt = pi.PropertyType;

        if (!CanUseTypeAsPropertyEntry.ContainsKey(pt)) {
          if (pt.IsValueType && pt.IsPrimitive) {
            CanUseTypeAsPropertyEntry.Add(pt, true);
          } else if (pt.IsAssignableTo(typeof(SettingsBase))) {
            CanUseTypeAsPropertyEntry.Add(pt, false);
          } else {
            TypeConverter tc = TypeDescriptor.GetConverter(pt);
            CanUseTypeAsPropertyEntry.Add(pt, tc?.CanConvertFrom(typeof(string)) ?? false);
          }
        }

        if (CanUseTypeAsPropertyEntry[pt]) {
          PropertyEntry pe;
          if (pi.GetCustomAttribute<SharedPropertyAttribute>() != null && sharedPE != null) {
            if (!sharedPE.TryGetValue(pi.Name, out pe)) {
              pe = PropertyEntry.Generate(pi, obj, defobj);
              sharedPE.Add(pi.Name, pe);
            }
          } else {
            pe = PropertyEntry.Generate(pi, obj, defobj);
          }
          pel.Add(pe);
        } else {
          if (pt.GetEditableProperties().Count() > 0) {
            string name = null, desc = null;
            if (pi.GetCustomAttribute<DisplayAttribute>() is DisplayAttribute da) {
              name = da.Name;
              desc = da.Description;
            } else {
              if (pi.GetCustomAttribute<DisplayNameAttribute>() is DisplayNameAttribute dna)
                name = dna.DisplayName;
              if (pi.GetCustomAttribute<DescriptionAttribute>() is DescriptionAttribute dca)
                desc = dca.Description;
            }
            if (name == null) {
              if (pt.GetCustomAttribute<DisplayAttribute>() is DisplayAttribute tda) {
                name = tda.Name;
                desc = tda.Description;
              } else {
                if (pt.GetCustomAttribute<DisplayNameAttribute>() is DisplayNameAttribute tdna)
                  name = tdna.DisplayName;
                if (pt.GetCustomAttribute<DescriptionAttribute>() is DescriptionAttribute tdca)
                  desc = tdca.Description;
              }
            }
            name ??= pi.Name;
            var pb = BuildPropertyBundle(PropertyEntry.Read(pi, obj),
                                         PropertyEntry.Read(pi, defobj),
                                         name, desc, sharedPE);
            pbl.Add(pb);
          }
        }
      }

      bool isAdvancedVisible = (t.GetCustomAttribute<EditorBrowsableAttribute>()?.State
       ?? EditorBrowsableState.Always) == EditorBrowsableState.Advanced;

      return new(n, d, pel, new(pbl), isAdvancedVisible);
    }

    IEnumerable<PropertyEntry> GetRecursiveProperies(List<PropertyEntry> SharedInstances = null)
    {
      SharedInstances ??= new();
      if ((Properties?.Count ?? 0) > 0) {
        foreach (PropertyEntry pe in Properties) {
          if (pe.IsShared) {
            if (!SharedInstances.Contains(pe)) {
              SharedInstances.Add(pe);
              yield return pe;
            }
          } else { yield return pe; }
        }
      }
      if (PropertyBundles?.Length > 0)
        foreach (PropertyBundle pb in PropertyBundles.All)
          foreach (PropertyEntry pe in pb.GetRecursiveProperies(SharedInstances)) yield return pe;
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
      if ((Properties?.Count ?? 0) > 0) yield return Properties;
      foreach (PropertyBundle pb in PropertyBundles)
        if (pb.Properties.Count > 0 && (ShowAdvanced || !pb.IsAdvancedVisible)) yield return pb;
    }
    public override string ToString() => $"PropertyBundle[{Properties?.Count}] {BundleName}";
    public void ApplyChnages() { foreach (var pe in GetRecursiveProperies()) pe.ApplyChnages(); }
    public void Restore() { foreach (var pe in GetRecursiveProperies()) pe.Restore(); }
    public void RestoreDefault() { foreach (var pe in GetRecursiveProperies()) pe.RestoreDefault(); }

    public bool HasAnyPropertyChanged {
      get {
        bool changed = false;
        foreach (var pe in GetRecursiveProperies()) {
          if (pe.HasChanged) {
            changed = true;
            break;
          }
        }
        return changed;
      }
    }
    public readonly bool IsAdvancedVisible;

  }
  public class PropertyCollection : List<PropertyEntry>
  {
    public PropertyCollection() : base() { }
    public PropertyCollection(IEnumerable<PropertyEntry> il) : base(il) { }
    public IEnumerable<PropertyEntry> Properties => this;
    public string BundleDescription => "General Settings";
  }

  public class BundleCollection : IEnumerable, INotifyCollectionChanged, INotifyPropertyChanged
  {
    PropertyBundle[] PropertyBundles { get; init; }

    public PropertyBundle this[int i] {
      get => PropertyBundles[i];
      set => PropertyBundles[i] = value;
    }
    public int Length => PropertyBundles.Length;

    public BundleCollection(IList<PropertyBundle> pbs) => PropertyBundles = pbs?.ToArray();

    public IEnumerator GetEnumerator()
    {
      foreach (PropertyBundle pb in PropertyBundles)
        if (PropertyBundle.ShowAdvanced || !pb.IsAdvancedVisible) yield return pb;
    }
    public IEnumerable All => PropertyBundles;

    public void Refresh()
    {
      PropertyChanged?.Invoke(this, new(null));
      CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Reset));
    }

    // TODO: implement show-advanced funcionality
    public event NotifyCollectionChangedEventHandler CollectionChanged;
    public event PropertyChangedEventHandler PropertyChanged;
  }
}