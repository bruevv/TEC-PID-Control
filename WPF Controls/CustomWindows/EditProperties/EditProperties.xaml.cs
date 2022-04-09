using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CustomWindows
{
  using InternalClasses;
  using WPFUtils;

  /// <summary> Property Window </summary>
  public partial class EditProperties : SimpleToolWindow
  {
    private bool showAllMenus = true;

    public PropertyBundle PropertyBundle { get; set; }

    public bool ShowAllProperties { get; set; } = true;
    public bool ShowAllMenus {
      get => showAllMenus;
      set {
        if (ShowAllMenus != value) {
          showAllMenus = value;
          PropertyBundle.ShowAdvanced = value;
          PropertyBundle.PropertyBundles.Refresh();
          treeView.Items.Refresh();
          treeView.UpdateLayout();
        }
      }
    }
    public EditProperties(object obj, object defobj = null)
    {
      PropertyBundle = PropertyBundle.BuildPropertyBundle(obj, defobj);

      InitializeComponent();

      DataContext = this;

      treeView.ItemsSource = PropertyBundle;
    }

    void EditE_GotFocus(object s, RoutedEventArgs e) => listBox.SelectedItem = (s as Control)?.Tag;

    void ComboBox_Loaded(object sender, RoutedEventArgs e) => PopulateComboBox((ComboBox)sender);

    void PopulateComboBox(ComboBox cb)
    {
      if (cb?.Tag is not PropertyEntry pe) return;

      foreach (EnumDecoration item in EnumDecoration.GetEnumDecorations(pe.Type))
        cb.Items.Add(item);
    }

    void SaveCanExecute(object o, CanExecuteRoutedEventArgs e) => e.CanExecute = PropertyBundle.HasAnyPropertyChanged;

    void SaveExecuted(object o, ExecutedRoutedEventArgs e) => PropertyBundle.ApplyChnages();

    void CloseExecuted(object sender, ExecutedRoutedEventArgs e) => Close();

    protected override void OnClosing(CancelEventArgs e)
    {
      if (PropertyBundle.HasAnyPropertyChanged) {
        MessageBoxResult mbr = MessageBox.Show(this, "Would you like to save changes before closing?",
          "Warning", MessageBoxButton.YesNoCancel);

        switch (mbr) {
          case MessageBoxResult.Cancel: e.Cancel = true; break;
          case MessageBoxResult.Yes: PropertyBundle.ApplyChnages(); break;
          default: break;
        }
      }

      base.OnClosing(e);
    }
    void UndoAllExecuted(object sender, ExecutedRoutedEventArgs e) => PropertyBundle.Restore();

    void RevertAllExecuted(object sender, ExecutedRoutedEventArgs e) => PropertyBundle.RestoreDefault();
    void UndoChangeExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      PropertyEntry pe = (e.OriginalSource as ListBoxItem)?.DataContext as PropertyEntry;
      if (pe != null) pe.Restore();
    }
    void RevertExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      PropertyEntry pe = (e.OriginalSource as ListBoxItem)?.DataContext as PropertyEntry;
      if (pe != null) {
        pe.RestoreDefault();
        pe.ApplyChnages();
      }
    }

    void ApplyExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      PropertyEntry pe = (e.OriginalSource as ListBoxItem)?.DataContext as PropertyEntry;
      if (pe != null) pe.ApplyChnages();
    }
    void PropertyChangedCE(object o, CanExecuteRoutedEventArgs e)
    {
      PropertyEntry pe = (e.OriginalSource as ListBoxItem)?.DataContext as PropertyEntry;
      e.CanExecute = (pe == null) ? false : pe.HasChanged;
    }
    void PropertyNotDefaultCE(object o, CanExecuteRoutedEventArgs e)
    {
      PropertyEntry pe = (e.OriginalSource as ListBoxItem)?.DataContext as PropertyEntry;
      e.CanExecute = (pe == null) ? false : !pe.IsDefaultValue;
    }
  }

  public class FilePathStringAttribute : Attribute { }
  public class DirectoryPathStringAttribute : Attribute { }
  public class EnumAsRadioButtonGroupAttribute : Attribute { }
}
