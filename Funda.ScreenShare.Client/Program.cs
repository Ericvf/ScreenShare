using System;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CopyClipboard
{
    class Program
    {
        static readonly string[] extensions = new[] { "bmp", "png", "jpg", "gif" };
        static readonly string server = ConfigurationManager.AppSettings["ServerUri"];

        static HttpClient httpClient = new HttpClient();

        [STAThread]
        static void Main(string[] args)
        {
            if (string.IsNullOrWhiteSpace(server))
                throw new ArgumentNullException("ServerUri");

            MainAsync().Wait();
        }

        static async Task MainAsync()
        {
            try
            {
                await GetImageFromClipboard();
                SystemSounds.Asterisk.Play();
            }
            catch (HttpRequestException)
            {
                SystemSounds.Exclamation.Play();
                MessageBox.Show(server);
            }
            catch
            {
                SystemSounds.Exclamation.Play();
            }
        }

        static async Task GetImageFromClipboard()
        {
            var clipboardData = Clipboard.GetDataObject();
            if (clipboardData != null)
            {
                if (clipboardData.GetDataPresent(DataFormats.Bitmap))
                {
                    var image = (Image)clipboardData.GetData(DataFormats.Bitmap, true);
                    await PostImage(image);
                }
                else if (clipboardData.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[])clipboardData.GetData(DataFormats.FileDrop);
                    var file = files.FirstOrDefault(f => extensions.Any(e => f.ToLower().EndsWith(e)));
                    if (file != null)
                        await PostFile(file);
                }
                else if (clipboardData.GetDataPresent(DataFormats.StringFormat))
                {
                    if (Uri.TryCreate((string)clipboardData.GetData(DataFormats.StringFormat), UriKind.Absolute, out Uri uri))
                        await PostUri(uri);
                }
            }
        }

        static async Task PostUri(Uri uri)
        {
            using (var stream = await httpClient.GetStreamAsync(uri))
            {
                var content = new StreamContent(stream);
                await httpClient.PostAsync(server, content);
            }
        }

        static async Task PostFile(string file)
        {
            var content = new ByteArrayContent(File.ReadAllBytes(file));
            await httpClient.PostAsync(server, content);
        }

        static async Task PostImage(Image image)
        {
            using (var ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Png);

                var content = new ByteArrayContent(ms.ToArray());
                await httpClient.PostAsync(server, content);
            }
        }
    }
}
