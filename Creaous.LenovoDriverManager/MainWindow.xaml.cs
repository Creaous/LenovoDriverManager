using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Windows;
using Newtonsoft.Json.Linq;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;
using MessageBox = System.Windows.MessageBox;

namespace Creaous.LenovoDriverManager;

/// <summary>
///     Interaction logic for LenovoUpdateCatalog.xaml
/// </summary>
public partial class LenovoUpdateCatalog : UiWindow
{
    public LenovoUpdateCatalog()
    {
        InitializeComponent();

        Categories = new ObservableCollection<Category>();
        DownloadData = new ObservableCollection<Download>();

        CheckLenovoServiceBridge();
    }

    // Use auto-implemented properties
    public ObservableCollection<Category> Categories { get; set; }
    public ObservableCollection<Download> DownloadData { get; set; }

    private void UpdateList()
    {
        foreach (var category in Categories)
        foreach (var update in category.Updates)
            update.SetValue(ItemHelper.ParentProperty, category);
    }

    private async void CheckLenovoServiceBridge()
    {
        try
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync("http://localhost:50131/Detect");

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();
                    var data = JObject.Parse(responseData);

                    if (data["DetectData"]?["Sn"] != null)
                    {
                        TbProduct.Text = (string)data?["DetectData"]?["Sn"]!;
                        CheckCatalog();

                        MessageBox.Show(
                            "We detected Lenovo Service Bridge on this device and have automatically retrieved the serial number and download catalog for you.",
                            "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }


    private async Task SendRequest(string url)
    {
        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Referrer = new Uri("https://pcsupport.lenovo.com");

            try
            {
                var response = await httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine(1);
                    var json = JObject.Parse(await response.Content.ReadAsStringAsync());

                    ProcessJsonResponse(json);
                }
                else
                {
                    Debug.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception: {ex.Message}");
            }
        }
    }

    private void ProcessJsonResponse(JObject json)
    {
        var allCategories = (JArray)json?["body"]?["AllCategories"]!;
        var categoriesArray = allCategories.ToObject<string[]>();

        foreach (var category in categoriesArray!)
            Categories.Add(new Category { Name = category, Updates = new List<Update>() });

        var downloadItems = (JArray)json?["body"]?["DownloadItems"]!;

        foreach (var item in downloadItems!) ProcessDownloadItem(item);

        Categories.RemoveAt(Categories.IndexOf(Categories.FirstOrDefault(z => z.Name == "Tool")!));
    }

    private void ProcessDownloadItem(JToken item)
    {
        var files = (JArray)item?["Files"]!;
        var category =
            Categories.IndexOf(Categories.FirstOrDefault(z => z.Name == item?["Category"]?["Name"]?.ToString())!);

        foreach (var file in files)
            if (file?["TypeString"]?.ToString() != "TXT README" && file?["TypeString"]?.ToString() != "PDF")
                Categories[category].Updates.Add(new Update
                {
                    Name = file?["Name"] + " (" + file?["Version"] + ")",
                    MD5 = file?["MD5"]?.ToString()!,
                    Size = file?["Size"]?.ToString()!,
                    URL = file?["URL"]?.ToString()!,
                    Version = file?["Version"]?.ToString()!
                });
    }

    private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var category in Categories)
            ItemHelper.SetIsChecked(category, true);
    }

    private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var category in Categories)
        foreach (var update in category.Updates)
            ItemHelper.SetIsChecked(update, false);
    }

    private void BtnSelectSuggested_Click(object sender, RoutedEventArgs e)
    {
        string[] suggested =
        {
            "Audio Driver", "Bluetooth Driver", "CardReader Driver", "AMD Graphics Driver", "Chipset Driver",
            "WLAN Driver", "Lenovo Energy Management", "Legion Space", "AMD VGA Driver", "NVIDIA VGA Driver",
            "Camera Driver", "LAN Driver", "Lenovo Fn and Function Keys", "HDR Monitor Driver"
        };

        foreach (var category in Categories)
        foreach (var suggestion in suggested)
        {
            var update =
                category.Updates.IndexOf(category.Updates.FirstOrDefault(z => z.Name.Contains(suggestion))!);

            if (update != -1) ItemHelper.SetIsChecked(category.Updates[update], true);
        }
    }

    private async void BtnInstallSelected_Click(object sender, RoutedEventArgs e)
    {
        if (DataView.Items.Count > 0)
        {
            var result = MessageBox.Show("Do you want to clear the existing downloads?", "Lenovo Driver Manager",
                MessageBoxButton.YesNo, MessageBoxImage.Asterisk);

            if (result == MessageBoxResult.Yes) DataView.Items.Clear();
        }

        foreach (var category in Categories)
        foreach (var update in category.Updates)
            if (ItemHelper.GetIsChecked(update) == true)
            {
                BtnDeselectAll.IsEnabled = false;
                BtnSelectAll.IsEnabled = false;
                BtnSelectSuggested.IsEnabled = false;
                BtnInstallSelected.IsEnabled = false;
                Status.Text = $"Downloading ({DownloadData.Count(item => item.Enabled)}/{DownloadData.Count})";
                await Task.Run(() => Download(update.URL, category.Name, update.Version, update.MD5, update.Name));
            }
    }

    private void Download(string url, string category, string version, string md5, string name)
    {
        Dispatcher.Invoke(() =>
        {
            var dir = $"downloads/{TbProduct.Text}/{category.Replace(':', '-').Replace('/', '-')}/{version}";

            Directory.CreateDirectory(dir);

            using var web = new WebClient();
            var file = $"{dir}/{url.Split('?').First().Split('/').Last().Split('\\').Last()}";

            Dispatcher.Invoke(() =>
            {
                DownloadData.Add(new Download { Name = name, Progress = 0, File = file, Enabled = false });
            });

            web.DownloadFileCompleted += (sender, e) => HandleDownloadCompleted(e, md5, name, file);
            web.DownloadProgressChanged += (sender, e) => HandleDownloadProgressChanged(e, name);

            try
            {
                web.DownloadFileAsync(new Uri(url), file);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception during download: " + ex.Message);
            }
        });
    }

    private void HandleDownloadCompleted(AsyncCompletedEventArgs e, string md5, string name, string file)
    {
        if (e.Error == null)
        {
            var currentMd5 = CalculateMD5(file);

            if (currentMd5.Equals(md5, StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine("Download completed. MD5 hashes match. The file is unchanged.");
                var dlId = DownloadData.IndexOf(DownloadData.FirstOrDefault(z => z.Name.Contains(name))!);

                Dispatcher.Invoke(() =>
                {
                    DownloadData[dlId].Enabled = true;
                    DataView.Items.Refresh();

                    if (DownloadData.Count(item => item.Enabled) == DownloadData.Count)
                    {
                        Status.Text = "Ready";
                        BtnDeselectAll.IsEnabled = true;
                        BtnSelectAll.IsEnabled = true;
                        BtnSelectSuggested.IsEnabled = true;
                        BtnInstallSelected.IsEnabled = true;
                    }
                    else
                    {
                        Status.Text = $"Downloading ({DownloadData.Count(item => item.Enabled)}/{DownloadData.Count})";
                    }
                });
            }
            else
            {
                MessageBox.Show(
                    $"The downloaded file: {file} does not match the given MD5. The file may be corrupted, please proceed with caution.",
                    "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            MessageBox.Show($"Download failed: {e.Error.Message}", "Critical Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void HandleDownloadProgressChanged(DownloadProgressChangedEventArgs e, string name)
    {
        var dlId = DownloadData.IndexOf(DownloadData.FirstOrDefault(z => z.Name.Contains(name))!);
        Dispatcher.Invoke(() =>
        {
            DownloadData[dlId].Progress = e.ProgressPercentage;
            DataView.Items.Refresh();
        });
    }

    private static string CalculateMD5(string filePath)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream = File.OpenRead(filePath))
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
    }

    private void Open_Click(object sender, EventArgs e)
    {
        var button = sender as Button;
        var tag = button?.Tag as Download;
        var filename = tag?.File;
        Process.Start(filename!);
    }

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        var settings = new SettingsWindow();
        settings.ShowDialog();
    }

    private void BtnWhatIsThis_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "You can typically find your serial number on the back of your device. Alternatively, you can install Lenovo Service Bridge and we'll automatically fill it in for you on restart of this program." +
            "\n\n" +
            "To get the product ID, visit the Lenovo PC Support website and type in the product you want. Once done, look at the URL bar and copy everything after 'products/' but before '?'." +
            "\n\n" +
            "Example product ID: laptops-and-netbooks/legion-series/legion-go-8apu1" +
            "\n\n" +
            "You only need either one of these (not both)." +
            "\n\n" +
            "Do you want to open the Lenovo support site right now?",
            "What is this?", MessageBoxButton.YesNo, MessageBoxImage.Information);

        if (result == MessageBoxResult.Yes) Process.Start("cmd", "/C start https://pcsupport.lenovo.com");
    }

    private async void CheckCatalog()
    {
        try
        {
            Categories.Clear();

            Status.Text = "Retrieving the update catalog from the Lenovo server...";

            var url = "https://pcsupport.lenovo.com/us/en/api/v4/downloads/drivers?productId=" + TbProduct.Text;

            await SendRequest(url);

            UpdateList();

            Status.Text = "Ready";
        }
        catch (Exception ex)
        {
            MessageBox.Show("Sorry but the application has encountered a critical error.\n\n" + ex.Message,
                "Critical Error");
        }
    }

    private void BtnCheck_Click(object sender, RoutedEventArgs e)
    {
        CheckCatalog();
    }

    private void BtnClearDownloads_Click(object sender, RoutedEventArgs e)
    {
        var result =
            MessageBox.Show(
                "Are you sure you want to clear the download list? (this will not remove the downloaded files, you can choose to delete them in the settings)",
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Information);

        if (result == MessageBoxResult.Yes) DownloadData.Clear();
    }
}
