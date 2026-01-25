using MaterialDesignThemes.Wpf;

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using WPF_PAR.MVVM.Models;
using WPF_PAR.Services.Interfaces;

namespace WPF_PAR.Services
{
    public class NotificationService : INotificationService
    {
        // Esta es la ÚNICA cola de mensajes de toda la app
        public SnackbarMessageQueue MessageQueue { get; }

        public NotificationService(IDialogService dialogService)
        {
            // Inicializamos la cola aquí
            MessageQueue = new SnackbarMessageQueue(TimeSpan.FromSeconds(3));
        }

        // Método SetMessageQueue: YA NO ES NECESARIO si el servicio crea la cola.
        // Pero para compatibilidad con la interfaz, lo dejamos vacío o lo quitamos de la interfaz.
        public void SetMessageQueue(SnackbarMessageQueue queue)
        {
            // No hacemos nada, porque ya tenemos nuestra propia cola.
        }

        public void ShowSuccess(string message)
        {
            var alerta = new NotificationAlert
            {
                Title = "Éxito",
                Message = message,
                Type = AlertType.Success
            };
            MessageQueue.Enqueue(alerta);
        }

        public void ShowError(string message)
        {
            var alerta = new NotificationAlert
            {
                Title = "Error",
                Message = message,
                Type = AlertType.Error
            };
            // Los errores duran un poco más
            MessageQueue.Enqueue(alerta, null, null, null, false, true, TimeSpan.FromSeconds(5));
        }

        public void ShowInfo(string message)
        {
            var alerta = new NotificationAlert
            {
                Title = "Información",
                Message = message,
                Type = AlertType.Info
            };
            MessageQueue.Enqueue(alerta);
        }

        public async Task ShowErrorDialog(string message)
        {
            var view = new StackPanel { Margin = new Thickness(20), MaxWidth = 400 };

            view.Children.Add(new MaterialDesignThemes.Wpf.PackIcon
            {
                Kind = PackIconKind.AlertCircleOutline,
                Width = 50,
                Height = 50,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.Crimson,
                Margin = new Thickness(0, 0, 0, 10)
            });

            view.Children.Add(new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                FontSize = 14
            });

            var btn = new Button
            {
                Content = "ENTENDIDO",
                Command = DialogHost.CloseDialogCommand,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0),
                Width = 120
            };
            view.Children.Add(btn);

            await DialogHost.Show(view, "RootDialog");
        }
    }
}