using System;
using System.Collections.Generic;
using System.Text;

using WPF_PAR.Core;

namespace WPF_PAR.Services
{
    public class FilterService : ObservableObject
    {
        // Evento para avisar a todos los ViewModels
        public event Action OnFiltrosCambiados;

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
                    NotificarCambio();
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
                    NotificarCambio();
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
                    NotificarCambio();
                }
            }
        }

        private void NotificarCambio()
        {
            OnFiltrosCambiados?.Invoke();
        }

        public FilterService()
        {
            DateTime hoy = DateTime.Now;
            FechaInicio = new DateTime(hoy.Year, hoy.Month, 1);
            FechaFin = hoy;
            SucursalId = Properties.Settings.Default.SucursalDefaultId;
            if ( SucursalId == 0 ) SucursalId = 1508; 
        }
    }
}
