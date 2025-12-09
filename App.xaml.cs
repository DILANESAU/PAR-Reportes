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

            // --- 1. REGISTRAR SERVICIOS (Singleton = Una única instancia para toda la app) ---

            // Servicios de Infraestructura
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<ISnackbarService, SnackbarService>();

            // Servicios de Negocio (Stateful services como FilterService deben ser Singleton)
            services.AddSingleton<FilterService>();
            services.AddSingleton<BusinessLogicService>();
            services.AddSingleton<SucursalesService>();

            // Estos crean conexiones nuevas, pueden ser Transient (se crean cuando se piden)
            services.AddTransient<VentasServices>();
            services.AddTransient<ClientesService>();
            services.AddTransient<ReportesService>();
            services.AddTransient<CatalogoService>();
            services.AddTransient<AuthService>();

            // --- 2. REGISTRAR VIEWMODELS ---
            // MainViewModel depende de los otros, así que el contenedor los inyectará automáticamente
            services.AddTransient<MainViewModel>();

            services.AddTransient<DashboardViewModel>();
            services.AddTransient<FamiliaViewModel>();
            services.AddTransient<ClientesViewModel>();
            services.AddTransient<SettingsViewModel>();

            // --- 3. REGISTRAR VENTANAS ---
            services.AddTransient<MainWindow>();
            services.AddTransient<LoginWindow>();

            return services.BuildServiceProvider();
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var themeService = new WPF_PAR.Services.ThemeService();
            themeService.LoadSavedTheme();
            // Pedimos el LoginWindow al contenedor (ya vendrá con sus dependencias si tuviera)
            var loginWindow = Services.GetRequiredService<LoginWindow>();
            loginWindow.Show();
        }
    }
}
