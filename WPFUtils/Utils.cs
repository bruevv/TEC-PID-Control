using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace WPFUtils
{
  public class ValueFromStyleExtension : MarkupExtension
  {
    public ValueFromStyleExtension() { }

    public object StyleKey { get; set; }
    public DependencyProperty Property { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      if (StyleKey == null || Property == null)
        return null;
      object value = GetValueFromStyle(StyleKey, Property);
      if (value is MarkupExtension) {
        return ((MarkupExtension)value).ProvideValue(serviceProvider);
      }
      return value;
    }

    private static object GetValueFromStyle(object styleKey, DependencyProperty property)
    {
      Style style = Application.Current.TryFindResource(styleKey) as Style;
      while (style != null) {
        var setter =
            style.Setters
                .OfType<Setter>()
                .FirstOrDefault(s => s.Property == property);

        if (setter != null) {
          return setter.Value;
        }

        style = style.BasedOn;
      }
      return null;
    }
  }
  public class WidthReductorConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      double ret = (double)value - int.Parse((string)parameter);
      if (ret < 0) ret = 0;
      return ret;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      => throw new NotImplementedException();
  }
  public class RadioButtonListConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
      => (int)value == int.Parse((string)parameter);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if ((bool)value) return int.Parse((string)parameter);
      else return DependencyProperty.UnsetValue;
    }
  }
  public class VisibilityConverter : IValueConverter
  {
    ///<summary>Visibility returned for False or null. Default: <c>Visibility.Hidden</c></summary>
    public Visibility FalseVisibility { get; set; } = Visibility.Hidden;
    ///<summary>Visibility returned for True or not null. Default: <c>Visibility.Visible</c></summary>
    public Visibility TrueVisibility { get; set; } = Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return (Visibility)value == TrueVisibility;
    }
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is bool b)
        return b ? TrueVisibility : FalseVisibility;
      return value == null ? FalseVisibility : TrueVisibility;
    }
  }
  public class ColorConverter : IValueConverter
  {
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var c = (Color)value;
      return Color.FromArgb(c.A, c.R, c.G, c.B);
    }
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var c = (System.Drawing.Color)value;
      if (c.IsEmpty) {
      }
      return Color.FromArgb(c.A, c.R, c.G, c.B);
    }
  }
  public class EnumBooleanConverter : IValueConverter
  {
    public object Convert(object v, Type tt, object p, CultureInfo c)
    {
      string parameterString = p as string;
      if (parameterString == null)
        return DependencyProperty.UnsetValue;

      if (Enum.IsDefined(v.GetType(), v) == false)
        return DependencyProperty.UnsetValue;

      object parameterValue = Enum.Parse(v.GetType(), parameterString);

      return parameterValue.Equals(v);
    }
    public object ConvertBack(object v, Type tt, object p, CultureInfo c)
    {
      if ((bool?)v == true) {
        string parameterString = p as string;
        if (parameterString == null)
          return DependencyProperty.UnsetValue;

        return Enum.Parse(tt, parameterString);
      } else {
        return null;
      }
    }
  }
  public class IconFromBrush : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return new Rectangle {
        Width = 16,
        Height = 16,
        Fill = (Brush)value
      };
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return (value as Rectangle)?.Fill;
    }
  }
  public class BrushFromBool : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value as bool? ?? true) return null;
      else return parameter as Brush;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return !(value as Brush).Equals(parameter as Brush);
    }
  }
  public class BrushFromNOTBool : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (!(value as bool? ?? false)) return null;
      else return parameter as Brush;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return (value as Brush).Equals(parameter as Brush);
    }
  }
  [ValueConversion(typeof(bool), typeof(bool))]
  public class InverseBooleanConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return !(value as bool?) ?? throw new InvalidOperationException("The target must be a boolean");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return !(value as bool?) ?? throw new InvalidOperationException("The target must be a boolean");
    }
  }
  public class ResistanceConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      double V = values[0] as double? ?? double.NaN;
      double I = values[1] as double? ?? double.NaN;
      return V / I;
    }

    public object[] ConvertBack(object v, Type[] tt, object p, CultureInfo c) => throw new NotImplementedException();
  }
  public class PowerConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      double V = values[0] as double? ?? double.NaN;
      double I = values[1] as double? ?? double.NaN;
      return V * I;
    }

    public object[] ConvertBack(object v, Type[] tt, object p, CultureInfo c) => throw new NotImplementedException();
  }
  public class CommandGestureConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var gestures = (value as RoutedCommand)?.InputGestures;
      if (gestures != null) {
        foreach (var ges in gestures) {
          if (ges is KeyGesture kg)
            return kg.GetDisplayStringForCulture(CultureInfo.InvariantCulture);
        }
      }

      return null;
    }
    public object ConvertBack(object v, Type tt, object p, CultureInfo c)
      => throw new NotImplementedException();
  }

  public class DescriptionExtension : MarkupExtension
  {
    public object Source { get; set; }
    public string Path { get; set; }

    public DescriptionExtension() { }
    public DescriptionExtension(string path) => Path = path;

    string description = "";

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      if (description != "") return description;

      if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget ipvt
        && ipvt.TargetObject is FrameworkElement fe) {

        if (DesignerProperties.GetIsInDesignMode(fe)) goto returnnull;
        object s;

        if (Source != null) s = Source;
        else if (fe.DataContext != null) s = fe.DataContext;
        else goto returnnull;

        ICustomAttributeProvider icap;

        if (string.IsNullOrEmpty(Path?.Trim()) || Path.Trim() == ".") {
          icap = s.GetType();
        } else {
          string[] paths = Path.Split(".");
          int i;
          for (i = 0; i < paths.Length - 1; i++) {
            s = s.GetType().GetProperty(paths[i]).GetValue(s);
          }
          icap = s.GetType().GetProperty(paths[i]);
        }
        object[] attr = icap.GetCustomAttributes(typeof(DisplayAttribute), false);
        if (attr?.Length > 0) {
          DisplayAttribute da = (DisplayAttribute)attr[0];
          description = da.Description;
        } else {
          attr = icap.GetCustomAttributes(typeof(DescriptionAttribute), false);
          if (attr?.Length > 0) description = ((DescriptionAttribute)attr[0]).Description;
          else goto returnnull;
        }
        return description;
      }
      returnnull:
      return description = null;
    }
  }

}
