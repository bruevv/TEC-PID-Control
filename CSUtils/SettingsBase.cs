using CSUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Web.UI.WebControls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace CSSettings
{
  [Serializable]
  public abstract class SettingsBase : INotifyPropertyChanged, IXmlSerializable
  {
    public event PropertyChangedEventHandler PropertyChanged;
    [XmlIgnore]
    public bool IsDefault { get; protected internal set; } = false;

    protected void ChangeProperty<T>(ref T field, T newval, [CallerMemberName] string pname = "")
    {
      if (newval == null && field != null || !(newval?.Equals(field) ?? true)) {
        if (IsDefault)
          throw new System.Data.ReadOnlyException("The Default instace cannot be modified");
        field = newval;
        if (field is SettingsBase sb) sb.RaisePropertyChanged();
        OnPropertyChanged(this, pname);
        sharedPropertyManager?.ChangeProperty(pname, this, newval);
      }
    }
    protected virtual void OnPropertyChanged(object o, string pname) => PropertyChanged?.Invoke(o, new PropertyChangedEventArgs(pname));

    public static void RaisePropertiesChanged(SettingsBase sb) => sb.RaisePropertyChanged("");
    public void RaisePropertyChanged(string pname = "") => OnPropertyChanged(this, pname);
    [XmlIgnore]
    public string FileName { get; protected set; } = null;
    public static S LoadSettingsFile<S>(string filename = null)
      where S : SettingsBase, new()
    {
      if (string.IsNullOrEmpty(filename)) filename = GetDefaultSaveFileName(typeof(S));

      Logger.Log($"Opening Settings {filename}", Logger.Mode.Debug);

      S newSettings;

      if (File.Exists(filename)) {
        try {
          XmlSerializer ser = new XmlSerializer(typeof(S));
          using (Stream fs = new FileStream(filename, FileMode.Open)) {
            using (XmlReader reader = new XmlTextReader(fs)) {
              if (ser.CanDeserialize(reader))
                newSettings = (S)ser.Deserialize(reader);
              else newSettings = new S();
            }
          }
        } catch (Exception e) {
          Logger.Log($"Error reading settings file{filename}", e, Logger.Mode.Error);
          newSettings = new S();
        }
      } else {
        //        throw new IOException($"File <{filename}> do not exist");
        newSettings = new S();
      }

      newSettings.FileName = filename;

      if (typeof(S).GetCustomAttribute<SharedPropertyAttribute>() != null) newSettings.CreateSPM();
      return newSettings;
    }

    static string GetDefaultSaveFileName(Type T)
    {
      string path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
      string appname = Process.GetCurrentProcess().ProcessName;
      string settingsname = T.Name;

      return Path.Combine(path, appname, settingsname + ".settings");
    }
    protected internal abstract IEnumerable<(PropertyInfo pi, SettingsBase o)> GetRecursiveProperties();
    public void Save(string filename = "")
    {
      if (string.IsNullOrEmpty(filename)) filename = GetDefaultSaveFileName(GetType());

      string path = Path.GetDirectoryName(filename);
      if (!Directory.Exists(path)) Directory.CreateDirectory(path);

      XmlSerializer ser = new XmlSerializer(GetType());
      XmlWriterSettings xws = new XmlWriterSettings() {
        Indent = true,
        NewLineOnAttributes = false,
      };
      using (XmlWriter xw = XmlWriter.Create(filename, xws)) {
        ser.Serialize(xw, this);
      }
    }

    [XmlIgnore]
    public abstract object this[string pname] { get; set; }

    protected internal abstract class PropertyAccessors
    {
      public PropertyInfo PI;

      public abstract object Get(object der);
      public abstract void Set(object der, object o);
    }

    protected static void CopyProps(SettingsBase from, SettingsBase to)
    {
      if (!from.GetType().IsAssignableFrom(to.GetType()))
        throw new AbortException($"Cannot copy form {from} to {to}");

      foreach (PropertyAccessors pa in from.GenericPADict.Values) pa.Set(to, pa.Get(from));
    }

    SharedPropertyManager sharedPropertyManager = null;
    void CreateSPM() => sharedPropertyManager = new SharedPropertyManager(this);

    public XmlSchema GetSchema() => null;

    //static Dictionary<Type, object> TypeConverters = new Dictionary<Type, object>();
    object GetConverter(PropertyInfo pi, bool failed = false)
    {
      Type t = pi.PropertyType;
      if (!failed) {
        if (t.IsPrimitive) {
          return null;
        } else {
          ValueSerializer vs = ValueSerializer.GetSerializerFor(t);
          if (vs != null) return vs;
        }
      }

      List<Type> includedtypes = new List<Type>();

      foreach (var xia in pi.GetCustomAttributes<XmlIncludeAttribute>())
        includedtypes.Add(xia.Type);
      foreach (var xia in pi.DeclaringType.GetCustomAttributes<XmlIncludeAttribute>())
        includedtypes.Add(xia.Type);

      try {
        var xmlSerializer = new XmlSerializer(pi.PropertyType, includedtypes.ToArray());
        return xmlSerializer;
      } catch (Exception e) {
        return e;
      }
    }
    //void FindConverter(PropertyInfo pi, bool failed = false)
    //{
    //  object output;
    //  Type t = pi.PropertyType;
    //  if (failed) goto failed;

    //  if (t.IsPrimitive) {
    //    output = null;
    //    goto finish;
    //  } else {
    //    ValueSerializer vs = ValueSerializer.GetSerializerFor(t);
    //    if (vs != null) {
    //      output = vs;
    //      goto finish;
    //    } else goto failed;
    //  }

    //  failed:
    //  List<Type> includedtypes = new List<Type>();

    //  foreach (var xia in pi.GetCustomAttributes<XmlIncludeAttribute>())
    //    includedtypes.Add(xia.Type);
    //  foreach (var xia in pi.DeclaringType.GetCustomAttributes<XmlIncludeAttribute>())
    //    includedtypes.Add(xia.Type);

    //  try {
    //    var xmlSerializer = new XmlSerializer(pi.PropertyType, includedtypes.ToArray());
    //    output = xmlSerializer;
    //  } catch (Exception e) {
    //    output = e;
    //  }

    //  finish:
    //  if (TypeConverters.ContainsKey(t)) TypeConverters[t] = output;
    //  else TypeConverters.Add(t, output);
    //}
    public void ReadXml(XmlReader reader)
    {
      reader.MoveToContent();
      reader.ReadStartElement();
      reader.MoveToContent();
      do {
        string name = reader.Name;
        if (reader.IsEmptyElement) {
          reader.ReadStartElement();
        } else if (GenericPADict.Contains(name)) {
          try {
            PropertyAccessors pa = (PropertyAccessors)GenericPADict[name];
            Type t = pa.PI.PropertyType;
            object o;
            if (typeof(SettingsBase).IsAssignableFrom(t)) {
              XmlSerializer ser = new XmlSerializer(t);
              using (XmlReader intreader = reader.ReadSubtree()) {
                ConstructorInfo c = pa.PI.PropertyType.GetConstructor(new Type[] { });
                o = c.Invoke(new object[] { });
                ((IXmlSerializable)o).ReadXml(intreader);
                reader.ReadEndElement();
              }
            } else {
              object converter = GetConverter(pa.PI);
              TypeConverterSwitch:
              switch (converter) {
                case null:
                  o = Convert.ChangeType(reader.ReadElementContentAsString(), t);
                  if (o == null)
                    Logger.Default?.log($"Cannot convert property '{name}'", Logger.Mode.Error);
                  break;
                case ValueSerializer vs:
                  try {
                    o = vs.ConvertFromString(reader.ReadElementContentAsString(), null);
                    if (o == null)
                      Logger.Default?.log($"Cannot convert property '{name}'", Logger.Mode.Error);
                  } catch {
                    converter = GetConverter(pa.PI, true);
                    goto TypeConverterSwitch;
                  }
                  break;
                case XmlSerializer xs:
                  try {
                    o = xs.Deserialize(reader);
                    reader.ReadEndElement();
                  } catch (Exception e) {
                    converter = e;
                    goto TypeConverterSwitch;
                  }
                  break;
                case Exception e:
                  reader.Skip();
                  Logger.Default?.log($"Cannot deserialize property '{name}'", e, Logger.Mode.Error);
                  goto default;
                default:
                  o = null;
                  break;
              }
            }
            if (o != null) pa.Set(this, o);
            else Logger.Default?.log($"Property '{name}' set to default <{pa.Get(this)}>", Logger.Mode.NoAutoPoll);
          } catch { }
        } else {
          reader.Skip();
        }
        reader.MoveToContent();

      } while (reader.NodeType != XmlNodeType.EndElement && !reader.EOF);
    }
    public void WriteXml(XmlWriter writer)
    {
      foreach (PropertyAccessors pa in GenericPADict.Values) {
        object prop = pa.Get(this);
        Type t = pa.PI.PropertyType;

        string name = null; string desc = null;
        if (pa.PI.GetCustomAttribute<DisplayAttribute>() is DisplayAttribute da) {
          name = da.Name;
          desc = da.Description;
        } else {
          name = pa.PI.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
          desc = pa.PI.GetCustomAttribute<DescriptionAttribute>()?.Description;
        }
        name = name ?? "";
        desc = desc ?? "";
        string comment = name + ((name != "" && desc != "") ? ": " : "") + desc;
        if (comment != "") writer.WriteComment(comment);
        writer.WriteStartElement(pa.PI.Name);
        writer.WriteAttributeString("Type", t.Name);
        if (t != (prop?.GetType() ?? t)) writer.WriteAttributeString("DerivedType", prop.GetType().Name);

        if (prop is IXmlSerializable ixml) {
          ixml.WriteXml(writer);
        } else {
          object converter = GetConverter(pa.PI);
          TypeConverterSwitch:
          switch (converter) {
            case null:
              writer.WriteString(prop?.ToString() ?? "");
              break;
            case ValueSerializer vs:
              if (vs?.CanConvertToString(prop, null) == true) {
                writer.WriteString(vs.ConvertToString(prop, null));
              } else {
                converter = GetConverter(pa.PI, true);
                goto TypeConverterSwitch;
              }
              break;
            case XmlSerializer xs:
              try {
                xs.Serialize(writer, prop);
              } catch (Exception e) {
                converter = e;
                goto TypeConverterSwitch;
              }
              break;
            case Exception e:
              Logger.Default?.log($"Cannot write property '{pa.PI.Name}'", e, Logger.Mode.Error);
              break;
          }
        }
        writer.WriteEndElement();

      }
    }
    protected class SharedPropertyManager
    {
      Dictionary<string, List<(object o, PropertyAccessors pa)>> AccessorsDic = null;
      internal SharedPropertyManager(SettingsBase sb)
      {
        Type t = sb.GetType();
        if (AccessorsDic == null) {
          AccessorsDic = new Dictionary<string, List<(object o, PropertyAccessors pa)>>();

          foreach (var pio in sb.GetRecursiveProperties()) {
            if (pio.pi.GetCustomAttribute<SharedPropertyAttribute>() != null) {
              string name = pio.pi.Name;
              List<(object o, PropertyAccessors pa)> piol;
              if (!AccessorsDic.ContainsKey(name)) {
                piol = new List<(object o, PropertyAccessors pa)>();
                AccessorsDic.Add(name, piol);
              } else {
                piol = AccessorsDic[name];
              }
              PropertyAccessors pa = (PropertyAccessors)pio.o.GenericPADict[name];
              piol.Add((pio.o, pa));
              if (pio.o != sb) pio.o.sharedPropertyManager = this;
            }
          }
        }
        //foreach (var keyValuePair in SharedPAccessorsDic) {
        //  int len = keyValuePair.Value.Length;
        //  (object o, PropertyAccessors pa)[] Accessors = new (object, PropertyAccessors)[len];
        //  for (int j = 0; j < len; j++) {
        //    PropertyAccessors[] paa = keyValuePair.Value[j];
        //    object o = this;
        //    for (int i = 0; i < paa.Length - 1; i++) o = paa[i].Get(o);

        //    Accessors[j].o = o;
        //    Accessors[j].pa = paa[paa.Length - 1];
        //  }
        //  AccessorsDic.Add(keyValuePair.Key, Accessors);
        //}
      }
      public void ChangeProperty(string name, object source, object val)
      {
        if (AccessorsDic.TryGetValue(name, out var ps)) {
          foreach ((object o, PropertyAccessors pa) in ps) {
            if (o != source) pa.Set(o, val);
          }
        }
      }
    }
    [XmlIgnore]
    protected internal abstract IDictionary GenericPADict { get; }
  }
  /// <summary>Base class for application settings</summary>
  /// <typeparam name="Der">This parameter should be set to the deriving class itself</typeparam>
  public abstract class GenericSB<Der> : SettingsBase where Der : GenericSB<Der>, new()
  {
    static Der instance = null;
    /// <summary>Default static instance</summary>
    /// <remarks>Normally it should never change reference</remarks>
    public static Der Instance {
      get => instance ?? (instance = LoadSettingsFile<Der>());
      protected set => instance = value;
    }

    static Der defInst = null;
    public static Der Default {
      get => defInst ?? (defInst = new Der() { IsDefault = true });
    }

    protected internal override IEnumerable<(PropertyInfo pi, SettingsBase o)> GetRecursiveProperties()
    {
      foreach (PropertyAccessors pa in PADict.Values) {
        if (typeof(SettingsBase).IsAssignableFrom(pa.PI.PropertyType)) {
          foreach (var tu in ((SettingsBase)pa.Get(this)).GetRecursiveProperties())
            yield return tu;
        }
        yield return (pa.PI, this);
      }
    }

    class PropertyAccessorsG : PropertyAccessors
    {
      Action<Der, object> aSet;
      Func<Der, object> aGet;

      public override object Get(object der) => aGet((Der)der);
      public override void Set(object der, object o) => aSet((Der)der, o);

      public PropertyAccessorsG(PropertyInfo pi)
      {
        PI = pi;

        aSet = FastInvoke.BuildUntypedSetter<Der>(pi);
        aGet = FastInvoke.BuildUntypedGetter<Der>(pi);
      }
    }

    /// <summary>Dictionary<string, PropertyAccessors></summary>
    [XmlIgnore]
    static IDictionary PADict = null;
    [XmlIgnore]
    protected internal override IDictionary GenericPADict => PADict;
    public GenericSB()
    {
      if (PADict == null) {
        var props = typeof(Der).GetProperties(
          BindingFlags.Public | BindingFlags.Instance |
          BindingFlags.GetProperty | BindingFlags.SetProperty);

        if (props.Length > 10)
          PADict = new Dictionary<string, PropertyAccessors>(props.Length);
        else
          PADict = new ListDictionary();

        foreach (var prop in props) {
          if (prop.Name != "Item" &&
              prop.GetCustomAttribute<XmlIgnoreAttribute>() == null &&
              prop.GetSetMethod() != null) {
            try {
              PADict.Add(prop.Name, new PropertyAccessorsG(prop));
            } catch { }
          }
        }
      }
    }
    [XmlIgnore]
    public override object this[string pname] {
      get => ((PropertyAccessors)PADict[pname]).Get((Der)this);
      set => ((PropertyAccessors)PADict[pname]).Set((Der)this, value);
    }

    public void Load(string filename = null)
    {
      Der load = LoadSettingsFile<Der>(filename);
      CopyProps(load, (Der)this);
    }
  }
  /// <summary>Indicate that the property is shared in the object hierachy (by name)</summary>
  /// <remarks>
  /// This Attribute should be applyed both on property and the tompost class of
  /// the structure that has shared properties
  /// </remarks>
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property,
    AllowMultiple = false, Inherited = false)]
  public class SharedPropertyAttribute : Attribute { }



}
