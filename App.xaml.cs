using Microsoft.Extensions.DependencyInjection;

using System.Windows;

using WPF_PAR.MVVM.ViewModels;
using WPF_PAR.MVVM.Views; // Asegúrate de tener este using
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

            // 1. SERVICIOS
            // Registramos ThemeService y NotificationService como Singleton
            services.AddSingleton<ThemeService>();
            services.AddSingleton<INotificationService, NotificationService>(); // Importante: interfaz e implementación

            services.AddSingleton<IDialogService, DialogService>();
            // services.AddSingleton<ISnackbarService, SnackbarService>(); // Si ya usas NotificationService, quizás este sobre

            services.AddSingleton<FilterService>();
            services.AddSingleton<BusinessLogicService>();
            services.AddSingleton<SucursalesService>();

            services.AddTransient<FamiliaLogicService>();
            services.AddTransient<ChartService>();
            services.AddTransient<ClientesService>();
            services.AddTransient<ReportesService>();
            services.AddTransient<CatalogoService>();
            services.AddTransient<AuthService>();

            // 2. VIEWMODELS
            // El contenedor se encargará de inyectar todo lo que piden sus constructores
            services.AddSingleton<MainViewModel>(); // Main suele ser Singleton porque vive toda la app
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<FamiliaViewModel>();
            services.AddTransient<ClientesViewModel>();
            services.AddTransient<SettingsViewModel>();

            // 3. VISTAS (WINDOWS)
            services.AddTransient<MainWindow>();
            services.AddTransient<LoginWindow>();

            return services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // A. Cargar Tema (Usando DI para obtener el servicio)
            var themeService = Services.GetRequiredService<ThemeService>();
            themeService.LoadSavedTheme();

            // B. Mostrar Login
            /* NOTA: Lo ideal es mostrar Login, y si es exitoso, abrir MainWindow.
               Para este ejemplo rápido, asumiremos que quieres probar MainWindow directamente
               o que el Login llama al MainWindow después.
            */

            // Opción 1: Abrir Login
            var loginWindow = Services.GetRequiredService<LoginWindow>();
            loginWindow.Show();

            // Opción 2: Abrir MainWindow DIRECTAMENTE (Para probar tus cambios de hoy)
            // Si quieres probar el dashboard ya, comenta las lineas del Login y usa esto:
            // AbrirMainWindow(); 
        }

        // Método auxiliar para abrir la ventana principal correctamente
        public void AbrirMainWindow()
        {
            var mainWindow = Services.GetRequiredService<MainWindow>();
            var mainViewModel = Services.GetRequiredService<MainViewModel>();

            // 1. Asignar ViewModel
            mainWindow.DataContext = mainViewModel;

            // 2. OBTENER EL SERVICIO DE NOTIFICACIONES (Ya instanciado como Singleton)
            var notifService = Services.GetRequiredService<INotificationService>() as NotificationService;

            if ( notifService != null )
            {
                // 3. ¡AQUÍ ESTÁ LA MAGIA!
                // Le decimos al Snackbar visual que use la cola lógica del servicio.
                mainWindow.MainSnackbar.MessageQueue = notifService.MessageQueue;
            }

            mainWindow.Show();
        }
    }
}