using System.Windows;
using System.Windows.Input;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace WPFControls.PropertyGrid
{
  /// <summary>
  /// Interaction logic for PropertyWindow.xaml
  /// </summary>
  public partial class PropertyWindow : Window
  {
    #region Commands
    public static readonly RoutedUICommand Command_Ok = new RoutedUICommand(
                   "Ok", nameof(Command_Ok),
                   typeof(PropertyWindow));
    public static readonly RoutedUICommand Command_Apply = new RoutedUICommand(
                   "Apply", nameof(Command_Apply),
                   typeof(PropertyWindow));
    public static readonly RoutedUICommand Command_Cancel = new RoutedUICommand(
                "Cancel", nameof(Command_Cancel),
                typeof(PropertyWindow));
    #endregion Commands

    object Object {
      get => propertygrid.SelectedObject;
      set => propertygrid.SelectedObject = value;
    }
    

    public PropertyWindow(object o = null)
    {
      InitializeComponent();

      Object = o;
    }

    bool modified = false;

    void CommandBinding_OkCanExecute(object sender, CanExecuteRoutedEventArgs e)
     => e.CanExecute = true;
    void CommandBinding_ApplyCanExecute(object sender, CanExecuteRoutedEventArgs e)
     => e.CanExecute = modified;
    void CommandBinding_CancelCanExecute(object sender, CanExecuteRoutedEventArgs e)
     => e.CanExecute = true;


  }
}
