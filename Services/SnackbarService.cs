using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

using WPF_PAR.Services.Interfaces;

namespace WPF_PAR.Services
{
    public class SnackbarService : ISnackbarService
    {
        public void Show(string message)
        {
            // Buscamos la ventana principal de forma segura
            if ( Application.Current.MainWindow is MainWindow mainWindow )
            {
                // Buscamos el control por su nombre (definido en el XAML de MainWindow)
                var snackbar = mainWindow.FindName("MainSnackbar") as Snackbar;
                snackbar?.MessageQueue?.Enqueue(message);
            }
        }
    }
}
