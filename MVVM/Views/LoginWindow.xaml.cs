using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using WPF_PAR.Core;
using WPF_PAR.Services;

namespace WPF_PAR
{
    /// <summary>
    /// Lógica de interacción para LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private AuthService _authService;
        public LoginWindow()
        {
            InitializeComponent();
           _authService = new ();
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
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string user = txtUser.Text;
            string pass = txtPass.Password;
            // 1. Validar credenciales
            var usuario = _authService.ValidarLogin(user, pass);

            if ( usuario != null )
            {
                Session.UsuarioActual = usuario;

                // CAMBIO: Pedimos la MainWindow al contenedor de la App
                // Esto crea MainWindow -> crea MainViewModel -> crea DashboardViewModel -> etc.
                var mainWindow = ( ( App ) Application.Current ).Services.GetRequiredService<MainWindow>();
                mainWindow.Show();

                this.Close();
            }
            else
            {
                // FALLO
                MessageBox.Show("Usuario o contraseña incorrectos.", "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
