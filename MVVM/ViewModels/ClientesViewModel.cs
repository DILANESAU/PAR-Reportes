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
        private readonly FilterService _filters;

        public ObservableCollection<ClienteRankingModel> ListaClientes { get; set; }
        private List<ClienteRankingModel> _datosOriginales;

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

        // --- COMANDOS ---
        public RelayCommand OrdenarMejoresCommand { get; set; }
        public RelayCommand OrdenarPeoresCommand { get; set; }

        public ClientesViewModel(IDialogService dialogService, FilterService filterService)
        {
            _clientesService = new ClientesService();
            _dialogService = dialogService;
            _filters = filterService;

            ListaClientes = new ObservableCollection<ClienteRankingModel>();

            OrdenarMejoresCommand = new RelayCommand(o => AplicarOrden("MEJORES"));
            OrdenarPeoresCommand = new RelayCommand(o => AplicarOrden("RIESGO"));

            // Suscripción al filtro global
            _filters.OnFiltrosCambiados += CargarDatos;

            CargarDatos();
        }

        public async void CargarDatos()
        {
            IsLoading = true;
            try
            {
                // Usamos filtro global (Rango exacto)
                var datos = await _clientesService.ObtenerRankingClientes(
                    _filters.SucursalId,
                    _filters.FechaInicio,
                    _filters.FechaFin
                );

                _datosOriginales = datos;

                // KPI: Clientes que han bajado su compra vs año anterior
                ClientesEnRiesgo = datos.Count(c => c.Diferencia < 0);

                AplicarOrden("MEJORES");
            }
            catch ( Exception ex )
            {
                _dialogService.ShowError("Error cargando clientes: " + ex.Message, "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AplicarOrden(string tipo)
        {
            if ( _datosOriginales == null ) return;

            ListaClientes.Clear();
            List<ClienteRankingModel> ordenados;

            if ( tipo == "RIESGO" )
            {
                // Ordenar por diferencia ascendente (los más negativos primero)
                ordenados = _datosOriginales.OrderBy(c => c.Diferencia).ToList();
            }
            else
            {
                // Ordenar por venta actual descendente (los mejores primero)
                ordenados = _datosOriginales.OrderByDescending(c => c.VentaActual).ToList();
            }

            foreach ( var c in ordenados ) ListaClientes.Add(c);
        }
    }
}