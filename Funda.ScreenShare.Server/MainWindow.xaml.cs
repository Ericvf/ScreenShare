using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfAnimatedGif;

namespace Funda.ScreenShare.Server
{
    public partial class MainWindow : Window
    {
        readonly DispatcherTimer dispatcherTimer = new DispatcherTimer();
        readonly HttpListener Listener = new HttpListener();
        int timerMillis = 0;

        readonly int defaultDuration = int.Parse(ConfigurationManager.AppSettings["defaultDuration"]);
        readonly string httpPrefix = ConfigurationManager.AppSettings["httpPrefix"];
        readonly NotifyIcon notifyIcon = new NotifyIcon();

        public MainWindow()
        {
            InitializeComponent();
            Hide();

            notifyIcon.Icon = new System.Drawing.Icon(@"icon.ico");
            notifyIcon.Visible = true;
            notifyIcon.MouseClick += (s, e) => System.Windows.Application.Current.Shutdown();

            MouseLeftButtonUp += (s, e) => Hide();
            Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            AllowsTransparency = true;
            Topmost = true;

            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(defaultDuration);
            dispatcherTimer.Tick += (s, e) =>
            {
                dispatcherTimer.Stop();
                Hide();
            };

            CompositionTarget.Rendering += (s, e) => pg.Value = (int)DateTime.Now.TimeOfDay.TotalMilliseconds - timerMillis;

            // netsh http add urlacl url="http://+:81/" user=everyone
            Listener.Prefixes.Add(httpPrefix);
            Listener.Start();
            Listen();
        }

        async void Listen()
        {
            while (true)
            {
                var context = await Listener.GetContextAsync();
                dispatcherTimer.Stop();

                var ms = new MemoryStream();
                context.Request.InputStream.CopyTo(ms);

                if (context.Request.UserAgent == "ShareX")
                {
                    var requestBytes = ms.ToArray();
                    for (int i = 0; i < requestBytes.Length - 4; i++)
                    {
                        if (requestBytes[i] == 0x0D &&
                            requestBytes[i + 1] == 0x0A &&
                            requestBytes[i + 2] == 0x0D &&
                            requestBytes[i + 3] == 0x0A)
                            ms = new MemoryStream(requestBytes.Skip(i + 4).ToArray());
                    }

                    context.Response.Headers.Add("Content-type", "text/html");
                    using (var writer = new StreamWriter(context.Response.OutputStream))
                    {
                        writer.WriteLine(string.Empty);
                        writer.Close();
                    }
                }

                context.Response.StatusCode = 200;
                context.Response.Close();
                HandleRequest(ms);
            }
        }

        void HandleRequest(MemoryStream ms)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var imageSource = new BitmapImage();
                    imageSource.BeginInit();
                    imageSource.StreamSource = ms;
                    imageSource.EndInit();

                    Show();

                    // https://github.com/XamlAnimatedGif/WpfAnimatedGif
                    ImageBehavior.SetAnimatedSource(image, imageSource);
                    int duration = GetDuration();

                    pg.Maximum = duration;
                    pg.Value = 0;

                    dispatcherTimer.Interval = TimeSpan.FromMilliseconds(duration);
                    dispatcherTimer.Start();

                    timerMillis = (int)DateTime.Now.TimeOfDay.TotalMilliseconds;
                }
                catch { }
            }));
        }

        int GetDuration()
        {
            var animationController = ImageBehavior.GetAnimationController(image);
            if (animationController != null)
            {
                var duration = (int)animationController.Duration.TotalMilliseconds;
                if (duration < defaultDuration)
                    duration = (defaultDuration / duration + 1) * duration;

                return duration;
            }
            else
                return defaultDuration;
        }

        //static int GetDuration(MemoryStream ms)
        //{
        //    try
        //    {
        //        var image = Image.FromStream(ms);
        //        ms.Seek(0, SeekOrigin.Begin);

        //        if (!image.RawFormat.Equals(ImageFormat.Gif) || !ImageAnimator.CanAnimate(image))
        //            return defaultDuration;

        //        var frameDimension = new FrameDimension(image.FrameDimensionsList[0]);
        //        int frameCount = image.GetFrameCount(frameDimension);

        //        int duration = 0;
        //        int index = 0;
        //        for (int f = 0; f < frameCount; f++)
        //        {
        //            // http://web.archive.org/web/20130820015012/http://madskristensen.net/post/Examine-animated-Gife28099s-in-C.aspx
        //            duration += BitConverter.ToInt32(image.GetPropertyItem(20736).Value, index) * 10;
        //            index += 4;
        //        }

        //        if (duration < defaultDuration)
        //            duration = (defaultDuration / duration + 1) * duration;

        //        return duration;
        //    }
        //    catch
        //    {
        //        return defaultDuration;
        //    }
        //}
    }
}
