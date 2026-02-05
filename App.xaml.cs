using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Linq; // Necesario para cerrar ventanas
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
            // ... (Tu código de servicios se queda igual) ...
            var services = new ServiceCollection();

            services.AddSingleton<ThemeService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<FilterService>();
            services.AddSingleton<BusinessLogicService>();
            services.AddSingleton<SucursalesService>();
            services.AddSingleton<SecureStorageService>(); // <--- Asegúrate de registrar este si usas inyección, o instáncialo manual

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

        // =========================================================
        // AQUÍ ESTÁ EL CAMBIO CLAVE
        // =========================================================
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. Validar Config Auth
            string authIp = WPF_PAR.Properties.Settings.Default.Auth_Server;
            var secure = new SecureStorageService();
            string authPass = secure.RecuperarPassword(SecureStorageService.KeyAuth);

            // 2. Validar Config Data
            string dataIp = WPF_PAR.Properties.Settings.Default.Data_Server;
            string dataPass = secure.RecuperarPassword(SecureStorageService.KeyData);

            // Si falta CUALQUIERA de los dos, forzamos configuración
            bool faltaConfig = string.IsNullOrEmpty(authIp) || string.IsNullOrEmpty(authPass) ||
                               string.IsNullOrEmpty(dataIp) || string.IsNullOrEmpty(dataPass);

            if ( faltaConfig )
            {
                AbrirMainWindow(modoConfiguracion: true);
            }
            else
            {
                var loginWindow = Services.GetRequiredService<LoginWindow>();
                loginWindow.Show();
            }
        }

        // Modificamos el método para aceptar un parámetro opcional
        public void AbrirMainWindow(bool modoConfiguracion = false)
        {
            var mainWindow = Services.GetRequiredService<MainWindow>();
            var mainViewModel = Services.GetRequiredService<MainViewModel>();

            if ( modoConfiguracion )
            {
                // Forzamos la vista de Settings
                mainViewModel.CurrentView = Services.GetRequiredService<SettingsViewModel>();

                // Mensaje de bienvenida
                mainViewModel.MessageQueue.Enqueue("Bienvenido. Configura la conexión al servidor para continuar.");
            }
            else
            {
                // Flujo normal (viene del Login)
                // 1. Obtenemos el DashboardViewModel
                var dashboardVM = Services.GetRequiredService<DashboardViewModel>();

                // 2. Lo asignamos como vista actual
                mainViewModel.CurrentView = dashboardVM;

                // 3. ¡ESTA ES LA LÍNEA QUE FALTA! 🚀
                // Disparamos la carga inicial para que busque las sucursales y datos
                dashboardVM.CargarDatosIniciales();
            }

            mainWindow.DataContext = mainViewModel;
            mainWindow.Show();

            // Asegurarnos de cerrar la ventana de Login si estaba abierta
            var loginWindow = Application.Current.Windows.OfType<LoginWindow>().FirstOrDefault();
            if ( loginWindow != null )
            {
                loginWindow.Close();
            }
        }
    }
}