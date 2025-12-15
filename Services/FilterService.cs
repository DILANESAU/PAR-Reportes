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
            // 1. Cargamos TODAS las sucursales del CSV/BD
            var todas = sucursalesService.CargarSucursales();

            // 2. Aplicamos el FILTRO de Seguridad
            if ( Session.UsuarioActual.SucursalesPermitidas == null )
            {
                // Si es NULL (Admin), mostramos todas
                ListaSucursales = todas;
            }
            else
            {
                // Si tiene lista (aunque sea vacía), filtramos
                ListaSucursales = todas
                    .Where(s => Session.UsuarioActual.SucursalesPermitidas.Contains(s.Key))
                    .ToDictionary(k => k.Key, v => v.Value);
            }

            // 3. Configurar Fechas
            DateTime hoy = DateTime.Now;
            FechaInicio = new DateTime(hoy.Year, hoy.Month, 1);
            FechaFin = hoy;

            // 4. Seleccionar Sucursal por Defecto de forma segura
            int defaultGuardada = Properties.Settings.Default.SucursalDefaultId;

            if ( ListaSucursales.ContainsKey(defaultGuardada) )
            {
                SucursalId = defaultGuardada;
            }
            else if ( ListaSucursales.Count > 0 )
            {
                // Si la guardada no está permitida, seleccionamos la primera disponible
                SucursalId = ListaSucursales.Keys.First();
            }
            else
            {
                // Caso extremo: Usuario sin permisos a NADA
                SucursalId = 0;
            }
        }
    }
}
