using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
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
      if(StyleKey == null || Property == null)
        return null;
      object value = GetValueFromStyle(StyleKey, Property);
      if(value is MarkupExtension) {
        return ((MarkupExtension)value).ProvideValue(serviceProvider);
      }
      return value;
    }

    private static object GetValueFromStyle(object styleKey, DependencyProperty property)
    {
      Style style = Application.Current.TryFindResource(styleKey) as Style;
      while(style != null) {
        var setter =
            style.Setters
                .OfType<Setter>()
                .FirstOrDefault(s => s.Property == property);

        if(setter != null) {
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
      if(ret < 0) ret = 0;
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
      if((bool)value) return int.Parse((string)parameter);
      else return DependencyProperty.UnsetValue;
    }
  }
  public class VisibilityHiddenConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return (Visibility)value == Visibility.Visible;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return ((bool)value) ? Visibility.Visible : Visibility.Hidden;
    }
  }
  public class VisibilityCollapsedConverter : IValueConverter
  {
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return (Visibility)value == Visibility.Visible;
    }
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return ((bool)value) ? Visibility.Visible : Visibility.Collapsed;
    }
  }
  public class ColorConverter : IValueConverter
  {
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var c = (System.Windows.Media.Color)value;
      return System.Windows.Media.Color.FromArgb(c.A, c.R, c.G, c.B);
    }
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var c = (System.Drawing.Color)value;
      if(c.IsEmpty) {
      }
      return System.Windows.Media.Color.FromArgb(c.A, c.R, c.G, c.B);
    }
  }
  public class EnumBooleanConverter : IValueConverter
  {
    public object Convert(object v, Type tt, object p, CultureInfo c)
    {
      string parameterString = p as string;
      if(parameterString == null)
        return DependencyProperty.UnsetValue;

      if(Enum.IsDefined(v.GetType(), v) == false)
        return DependencyProperty.UnsetValue;

      object parameterValue = Enum.Parse(v.GetType(), parameterString);

      return parameterValue.Equals(v);
    }
    public object ConvertBack(object v, Type tt, object p, CultureInfo c)
    {
      if((bool?)v == true) {
        string parameterString = p as string;
        if(parameterString == null)
          return DependencyProperty.UnsetValue;

        return Enum.Parse(tt, parameterString);
      } else {
        return null;
      }
    }
  }
  public class IconFromDrawing : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return new Rectangle {
        Width = 16, Height = 16,
        Fill = (Brush)value
      };
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return (value as Rectangle)?.Fill as Brush;
    }
  }
  public class BrushFromBool : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if(value as bool? ?? true) return null;
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
      if(!(value as bool? ?? false)) return null;
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
}
