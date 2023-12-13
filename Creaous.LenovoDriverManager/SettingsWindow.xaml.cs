using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace Creaous.LenovoDriverManager;

/// <summary>
///     Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        LbName.Content = Assembly.GetExecutingAssembly().GetName().Name;
        LbVersion.Content = string.Format("v{0}", Assembly.GetExecutingAssembly().GetName().Version);
        LbAuthor.Content = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location).CompanyName;
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Settings reset!\n\nDo you want to restart the application?", "Settings",
            MessageBoxButton.YesNo, MessageBoxImage.Information);

        if (result == MessageBoxResult.Yes)
        {
            System.Windows.Application.Current.Shutdown();
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