using System;
using System.Collections.Generic;
using System.Text;

using WPF_PAR.Core;

namespace WPF_PAR.Services
{
    public class FilterService : ObservableObject
    {

        private int _sucursalId;
        public int SucursalId
        {
            get => _sucursalId;
            set
            {
                if ( _sucursalId != value )
                {
                    _sucursalId = value;
                    OnPropertyChanged();
                }
            }
        }

        private DateTime _fechaInicio;
        public DateTime FechaInicio
        {
            get => _fechaInicio;
            set
            {
                if ( _fechaInicio != value )
                {
                    _fechaInicio = value;
                    OnPropertyChanged();
                }
            }
        }

        private DateTime _fechaFin;
        public DateTime FechaFin
        {
            get => _fechaFin;
            set
            {
                if ( _fechaFin != value )
                {
                    _fechaFin = value;
                    OnPropertyChanged();
                }
            }
        }
        // REFACTORIZACIÓN: Movemos la lista aquí para facilitar el binding
        private Dictionary<int, string> _listaSucursales;
        public Dictionary<int, string> ListaSucursales
        {
            get => _listaSucursales;
            set { _listaSucursales = value; OnPropertyChanged(); }
        }
        public FilterService(SucursalesService sucursalesService)
        {
            // Cargar sucursales al iniciar el servicio
            ListaSucursales = sucursalesService.CargarSucursales();

            DateTime hoy = DateTime.Now;
            FechaInicio = new DateTime(hoy.Year, hoy.Month, 1);
            FechaFin = hoy;

            // Lógica para seleccionar sucursal por defecto
            int defaultId = Properties.Settings.Default.SucursalDefaultId;

            // Si la sucursal guardada existe en la lista, úsala; si no, usa la primera disponible o 0
            if ( ListaSucursales.ContainsKey(defaultId) )
                SucursalId = defaultId;
            else if ( ListaSucursales.Count > 0 )
                SucursalId = ListaSucursales.Keys.First();
            else
                SucursalId = 0;
        }
    }
}
