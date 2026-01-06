using Microsoft.Extensions.DependencyInjection;

using System.Configuration;
using System.Data;
using System.Windows;

using WPF_PAR.MVVM.ViewModels;

using WPF_PAR.Services;

using WPF_PAR.Services.Interfaces;

namespace WPF_PAR
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider Services { get; }
        public App()
        {
            Services = ConfigureServices();
        }
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<ISnackbarService, SnackbarService>();

            services.AddSingleton<FilterService>();
            services.AddSingleton<BusinessLogicService>();
            services.AddSingleton<SucursalesService>();

            services.AddTransient<FamiliaLogicService>();
            services.AddTransient<ChartService>();
            //services.AddTransient<VentasServices>();
            services.AddTransient<ClientesService>();
            services.AddTransient<ReportesService>();
            services.AddTransient<CatalogoService>();
            services.AddTransient<AuthService>();
            //services.AddTransient<ClientesLogicService>(); 

            services.AddTransient<MainViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<FamiliaViewModel>();
            services.AddTransient<ClientesViewModel>();
            services.AddTransient<SettingsViewModel>();

            services.AddTransient<MainWindow>();
            services.AddTransient<LoginWindow>();

            return services.BuildServiceProvider();
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var themeService = new WPF_PAR.Services.ThemeService();
            themeService.LoadSavedTheme();
            var loginWindow = Services.GetRequiredService<LoginWindow>();
            loginWindow.Show();
        }
    }
}
