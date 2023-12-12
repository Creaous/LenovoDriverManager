using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using Creaous.LenovoDriverManager.Properties;

namespace Creaous.LenovoDriverManager;

/// <summary>
///     Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow(bool developer)
    {
        InitializeComponent();

        if (!developer)
        {
            GbUpdates.Visibility = Visibility.Collapsed;
            GbDangerZone.Visibility = Visibility.Collapsed;
        }

        LbName.Content = Assembly.GetExecutingAssembly().GetName().Name;
        LbVersion.Content = string.Format("v{0}", Assembly.GetExecutingAssembly().GetName().Version);
        LbAuthor.Content = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location).CompanyName;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        Settings.Default.updateProduct = Convert.ToInt32(UpdProduct.Text);
        Settings.Default.updateBranch = UpdBranch.Text;
        Settings.Default.updateKey = UpdKey.Text;

        Settings.Default.Save();

        var result = MessageBox.Show("Settings saved!\n\nDo you want to restart the application?", "Settings",
            MessageBoxButton.YesNo, MessageBoxImage.Information);

        if (result == MessageBoxResult.Yes)
        {
            Application.Current.Shutdown();
            System.Windows.Forms.Application.Restart();
        }
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        Settings.Default.Reset();

        var result = MessageBox.Show("Settings reset!\n\nDo you want to restart the application?", "Settings",
            MessageBoxButton.YesNo, MessageBoxImage.Information);

        if (result == MessageBoxResult.Yes)
        {
            Application.Current.Shutdown();
            System.Windows.Forms.Application.Restart();
        }
    }

    private void BtnClearDownloads_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Are you sure you want to delete all the downloaded files?", "Settings",
            MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

        if (result == MessageBoxResult.Yes) Directory.Delete("downloads", true);
    }
}