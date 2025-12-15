using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using WPF_PAR.Core;
using WPF_PAR.MVVM.Models;
using WPF_PAR.Services;
using WPF_PAR.Services.Interfaces;

namespace WPF_PAR.MVVM.ViewModels
{
    public class ClientesViewModel : ObservableObject
    {
        private readonly ClientesService _clientesService;
        private readonly IDialogService _dialogService;
        public FilterService Filters { get; }
        public List<int> AñosDisponibles { get; set; }
        private int _anioSeleccionado;
        public int AnioSeleccionado
        {
            get => _anioSeleccionado;
            set { _anioSeleccionado = value; OnPropertyChanged(); /*CargarDatos();*/ }
        }

        public ObservableCollection<ClienteRankingModel> ListaClientes { get; set; }

        // --- KPIs ---
        private int _clientesEnRiesgo;
        public int ClientesEnRiesgo
        {
            get => _clientesEnRiesgo;
            set { _clientesEnRiesgo = value; OnPropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }
        public RelayCommand ActualizarCommand { get; set; }

        public ClientesViewModel(IDialogService dialogService, FilterService filterService)
        {
            _clientesService = new ClientesService();
            _dialogService = dialogService;
            Filters = filterService;
            int actual = DateTime.Now.Year;
            AñosDisponibles = new List<int> { actual, actual - 1, actual - 2, actual - 3, actual - 4 };
            AnioSeleccionado = actual;
            ActualizarCommand = new RelayCommand(o => CargarDatos());
            ListaClientes = new ObservableCollection<ClienteRankingModel>();
            CargarDatos();
        }

        public async void CargarDatos()
        {
            IsLoading = true;
            try
            {
                // Llamamos al nuevo método por Año
                var datos = await _clientesService.ObtenerReporteAnualClientes(
                    Filters.SucursalId,
                    AnioSeleccionado
                );

                ListaClientes.Clear();
                foreach ( var c in datos ) ListaClientes.Add(c);

                // KPI simple
                ClientesEnRiesgo = datos.Count(c => c.Tendencia == "DOWN");
            }
            catch ( Exception ex )
            {
                _dialogService.ShowError("Error: " + ex.Message, "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}