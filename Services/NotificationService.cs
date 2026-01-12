using MaterialDesignThemes.Wpf;
using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using WPF_PAR.MVVM.Models; // ASEGÚRATE DE TENER ESTE USING

namespace WPF_PAR.Services
{
    public interface INotificationService
    {
        void ShowSuccess(string message);
        void ShowError(string message);
        Task ShowErrorDialog(string message);
        void ShowInfo(string message);
    }

    public class NotificationService : INotificationService
    {
        // 1. PROPIEDAD DE LA COLA
        // Solo 'get' porque el servicio es el DUEÑO. Nadie más debe cambiarla.
        public SnackbarMessageQueue MessageQueue { get; }

        public NotificationService()
        {
            // Inicializamos la cola aquí. Esta es la ÚNICA cola que existirá.
            MessageQueue = new SnackbarMessageQueue(TimeSpan.FromSeconds(3));
        }

        // NOTA: Borré el método 'SetMessageQueue'. No lo necesitas.
        // La conexión la hacemos en App.xaml.cs asignando ESTA cola a la ventana.

        // 2. TOAST VERDE (Éxito)
        public void ShowSuccess(string message)
        {
            // ENVIAMOS UN OBJETO, NO UN STRING
            // Esto activará el DataTemplate que hicimos en MainWindow.xaml
            var alerta = new NotificationAlert
            {
                Title = "Éxito",
                Message = message,
                Type = AlertType.Success
            };

            // "OK" es el botón, null es la acción
            MessageQueue.Enqueue(alerta);
        }

        // 3. TOAST ROJO (Error)
        public void ShowError(string message)
        {
            var alerta = new NotificationAlert
            {
                Title = "Atención",
                Message = message,
                Type = AlertType.Error
            };
            MessageQueue.Enqueue(alerta);
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

        // 4. DIÁLOGO (Bloqueante)
        public async Task ShowErrorDialog(string message)
        {
            // Tu código anterior estaba vacío aquí, lo rellené para que se vea bien
            var view = new StackPanel { Margin = new Thickness(20), MaxWidth = 400 };

            // Icono
            view.Children.Add(new MaterialDesignThemes.Wpf.PackIcon
            {
                Kind = PackIconKind.AlertCircleOutline,
                Width = 50,
                Height = 50,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.Crimson,
                Margin = new Thickness(0, 0, 0, 10)
            });

            // Texto
            view.Children.Add(new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 14
            });

            // Botón
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