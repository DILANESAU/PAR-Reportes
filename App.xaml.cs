using System.Configuration;
using System.Data;
using System.Windows;

namespace WPF_PAR
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var themeService = new WPF_PAR.Services.ThemeService();
            themeService.LoadSavedTheme();
        }
    }
}
