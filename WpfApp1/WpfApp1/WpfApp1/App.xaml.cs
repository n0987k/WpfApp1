using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WpfApp1
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var candidates = new[]
            {
                Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Assets", "AppLogo.png"),
                Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Мастер пол.ico"),
                @"C:\Users\admin\OneDrive\Desktop\Ресурсы\Ресурсы\Мастер пол.ico",
            };

            foreach (var path in candidates)
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    continue;
                }

                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new System.Uri(path, System.UriKind.Absolute);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze();

                Resources["AppLogoImage"] = image;
                Resources["AppIconImage"] = image;
                break;
            }
        }
    }
}
