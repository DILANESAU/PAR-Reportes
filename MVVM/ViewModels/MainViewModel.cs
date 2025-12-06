using System;
using System.Collections.Generic;
using System.Text;
using WPF_PAR.Core;
using WPF_PAR.Services;
using WPF_PAR.Services.Interfaces;

namespace WPF_PAR.MVVM.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public RelayCommand SettingsViewCommand {  get; set; }
        public RelayCommand DashboardViewCommand { get; set; }
        public RelayCommand FamiliaViewCommand { get; set; }
        public RelayCommand NavegarLineaCommand {  get; set; }

        public SettingsViewModel SettingsVM { get; set; }
        public DashboardViewModel DashboardVM { get; set; }
        public FamiliaViewModel FamiliaVM { get; set; }

        private object _currentView;
        public object CurrentView
        {
            get {  return _currentView; }
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }
        private bool _isMenuOpen = true;
        public bool IsMenuOpen
        {
            get => _isMenuOpen;
            set
            {
                _isMenuOpen = value;
                OnPropertyChanged();
            }
        }
        public RelayCommand ToggleMenuCommand { get; set; }
        public MainViewModel()
        {
            IDialogService dialogService = new DialogService();
            ISnackbarService snackbarService = new SnackbarService();
            BusinessLogicService businessLogic = new();

            DashboardVM = new DashboardViewModel(dialogService);
            SettingsVM = new SettingsViewModel(dialogService);
            FamiliaVM = new FamiliaViewModel(dialogService, snackbarService, businessLogic);

            CurrentView = DashboardVM;

            SettingsViewCommand = new (o =>
            {
                CurrentView = SettingsVM;
            });
            DashboardViewCommand = new(o =>
            {
                CurrentView = DashboardVM;
            });

            FamiliaViewCommand = new(o =>
            {
                CurrentView = FamiliaVM;
            });
            NavegarLineaCommand = new(parametro =>
            {
                if(parametro is string linea )
                {
                    CurrentView = FamiliaVM;
                    FamiliaVM.CargarPorLinea(linea);
                }
            });
            ToggleMenuCommand = new(o => IsMenuOpen = !IsMenuOpen);
        }
    }
}
