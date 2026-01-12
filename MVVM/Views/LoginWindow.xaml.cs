using Microsoft.Extensions.DependencyInjection; // Necesario para acceder a App.Services si fuera necesario
using System;
using System.Windows;
using System.Windows.Input;

using WPF_PAR.Core;
using WPF_PAR.Services;

namespace WPF_PAR
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _authService;

        // CAMBIO 1: Inyección de Dependencias en el Constructor
        // En lugar de hacer 'new', pedimos el servicio al constructor.
        // El contenedor de App.xaml.cs se encargará de pasártelo automáticamente.
        public LoginWindow(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Sugerencia: Permitir dar Enter en la caja de contraseña para entrar
        private void TxtPass_KeyDown(object sender, KeyEventArgs e)
        {
            if ( e.Key == Key.Enter )
            {
                BtnLogin_Click(sender, e);
            }
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string user = txtUser.Text;
            string pass = txtPass.Password;

            if ( string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass) )
            {
                MessageBox.Show("Por favor ingresa usuario y contraseña.", "Datos Incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnLogin.IsEnabled = false;
            BtnLogin.Content = "Verificando...";

            try
            {
                // Usamos el servicio inyectado (ya no el 'new')
                var usuario = await _authService.ValidarLoginAsync(user, pass);

                if ( usuario != null )
                {
                    // Guardamos sesión
                    Session.UsuarioActual = usuario;

                    // CAMBIO 2: Usar el método maestro de App.xaml.cs
                    // Esto asegura que el Dashboard se abra con ViewModel, Snackbar y todo conectado.
                    if ( Application.Current is App myApp )
                    {
                        myApp.AbrirMainWindow();
                    }

                    // Cerramos el login
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Usuario o contraseña incorrectos.", "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch ( Exception ex )
            {
                MessageBox.Show($"Error de conexión: {ex.Message}", "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnLogin.IsEnabled = true;
                BtnLogin.Content = "INICIAR SESIÓN";
            }
        }
    }
}