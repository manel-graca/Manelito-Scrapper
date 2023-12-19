using HtmlAgilityPack;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;

namespace ManelitoScrapper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        HtmlDocument? doc;
        string? tempDir;
        bool waitingReset;

        public MainWindow() => InitializeComponent();

        private void OnInputChanged(object sender, RoutedEventArgs e)
        {
            
        }

        private async void ScrapButtonClick(object sender, RoutedEventArgs e)
        {
            if (!waitingReset)
            {
                var url = input_text.Text;
                if (!url.Contains('.') || !url.Contains("www") && !url.Contains("://") || !URLValidator.ValidateURL(url, out var updated_url) || string.IsNullOrWhiteSpace(url))
                {
                    MessageBox.Show("Please enter a valid URL", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    var web = new HtmlWeb();
                    var progress = new Progress<int>(value => { progress_bar.Value = value; });
                    doc = await LoadWebPageAsync(updated_url, web, progress);
                    ScrapUI();
                    waitingReset = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error occurred: {ex.Message}");
                    status_label.Content = "An error has occured. Please check if your input is correct.";
                    scrap_button.IsEnabled = false;
                    ResetUI();
                }

            }
            else
            {
                ResetUI();
                waitingReset = false;
            }
        }

        private void ScrapUI()
        {
            progress_bar.Visibility = Visibility.Visible;
            var progress = new Progress<int>(value => { progress_bar.Value = value; });
            status_label.Content = "Status: Scraping finished";
            scrap_button.Content = "Reset";
            save_button.Visibility = Visibility.Visible;
            open_button.Visibility = Visibility.Visible;
            var cache_url = input_text.Text;
            input_text.FontStyle = FontStyles.Italic;
            input_text.Text = cache_url;
            input_text.IsReadOnly = true;
        }

        private void ResetUI()
        {
            progress_bar.Value = 0;
            progress_bar.Visibility = Visibility.Hidden;
            input_text.Text = "";
            status_label.Content = "Status: Awaiting orders...";
            scrap_button.Content = "Scrap!";
            save_button.Visibility = Visibility.Hidden;
            open_button.Visibility = Visibility.Hidden;
            input_text.FontStyle = FontStyles.Normal;
            input_text.IsReadOnly = false;
        }

        private async Task<HtmlDocument> LoadWebPageAsync(string url, HtmlWeb web, IProgress<int> progress)
        {
            var document = await Task.Run(() =>
            {
                var doc = web.Load(url);

                for (int i = 0; i <= 100; i += 10)
                {
                    progress?.Report(i);
                    Thread.Sleep(20);
                }

                return doc;
            });

            return document;
        }

        private void Open(object sender, RoutedEventArgs e)
        {
            if (doc == null) return;

            string appDataTempPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string tempDir = Path.Combine(appDataTempPath, "Temp");
            Directory.CreateDirectory(tempDir);

            string filename = "manelitoscrapper_temp.txt";
            string filePath = Path.Combine(tempDir, filename);

            try
            {
                doc.Save(filePath);

                string htmlContent = File.ReadAllText(filePath);

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(htmlContent);

                string formattedHtml = htmlDocument.DocumentNode.OuterHtml;

                File.WriteAllText(filePath, formattedHtml);

                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving or opening TXT file: {ex.Message}");
            }
        }

            private void SaveAs(object sender, RoutedEventArgs e)
        {
            if (doc == null) return;

            Microsoft.Win32.SaveFileDialog dlg = new()
            {
                FileName = "Document",
                DefaultExt = ".html",
                Filter = "Text documents (.html)|*.html"
            };

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                string filename = dlg.FileName;
                Console.WriteLine(filename);
                doc.Save(filename);
                status_label.Content = "Status: Awaiting orders...";
                save_button.Visibility = Visibility.Hidden;
            }
        }
    }



    class URLValidator
    {
        public static bool ValidateURL(string url, out string updatedURL)
        {
            updatedURL = string.Empty;

            try
            {
                if (!url.StartsWith("http://") && !url.StartsWith("https://")) updatedURL = "http://" + url;
                else updatedURL = url;

                using Ping ping = new();
                PingReply reply = ping.Send(new Uri(updatedURL).Host);
                return (reply != null && reply.Status == IPStatus.Success);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
                return false;
            }
        }
    }
}
