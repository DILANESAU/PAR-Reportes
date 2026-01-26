using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using WPF_PAR.MVVM.ViewModels;
using WPF_PAR.MVVM.Views;
using WPF_PAR.Services;
using WPF_PAR.Services.Interfaces;

namespace WPF_PAR
{
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

            services.AddSingleton<ThemeService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<FilterService>();
            services.AddSingleton<BusinessLogicService>();
            services.AddSingleton<SucursalesService>();

            services.AddTransient<FamiliaLogicService>();
            services.AddTransient<ClientesLogicService>();
            services.AddTransient<ChartService>();
            services.AddTransient<ClientesService>();
            services.AddTransient<ReportesService>();
            services.AddTransient<CatalogoService>();
            services.AddTransient<AuthService>();

            services.AddSingleton<MainViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<FamiliaViewModel>();
            services.AddTransient<ClientesViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<LoginViewModel>();


            services.AddTransient<MainWindow>();
            services.AddTransient<LoginWindow>();

            return services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var themeService = Services.GetRequiredService<ThemeService>();
            themeService.LoadSavedTheme();
            var loginWindow = Services.GetRequiredService<LoginWindow>();
            loginWindow.Show();
        }

        public void AbrirMainWindow()
        {
            var mainWindow = Services.GetRequiredService<MainWindow>();
            var mainViewModel = Services.GetRequiredService<MainViewModel>();

            mainWindow.DataContext = mainViewModel;

            var notifService = Services.GetRequiredService<INotificationService>() as NotificationService;

            if ( notifService != null )
            {
                mainWindow.MainSnackbar.MessageQueue = notifService.MessageQueue;
            }

            mainWindow.Show();
        }
    }
}