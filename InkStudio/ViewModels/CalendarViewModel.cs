using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkStudio.Models;

namespace InkStudio.ViewModels;

/// <summary>
/// ViewModel para el control de calendario visual usando el control Calendar nativo de Avalonia.
/// Muestra un mes completo con los días y las citas marcadas.
/// </summary>
public partial class CalendarViewModel : ViewModelBase
{
    #region Propiedades

    /// <summary>
    /// Fecha del mes actual mostrado (DateTime para el control Calendar nativo).
    /// </summary>
    [ObservableProperty]
    private DateTime _mesActual = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

    /// <summary>
    /// Fecha seleccionada en el calendario.
    /// </summary>
    [ObservableProperty]
    private DateTime? _fechaSeleccionada;

    /// <summary>
    /// Lista de citas para el mes actual.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Cita> _citas = new();

    /// <summary>
    /// Diccionario de fechas con número de citas para personalización visual.
    /// </summary>
    public Dictionary<DateTime, int> CitasPorFecha { get; private set; } = new();

    #endregion

    #region Comandos

    /// <summary>
    /// Navega al mes anterior.
    /// </summary>
    [RelayCommand]
    private void MesAnterior()
    {
        MesActual = MesActual.AddMonths(-1);
        ActualizarCitasPorFecha();
    }

    /// <summary>
    /// Navega al mes siguiente.
    /// </summary>
    [RelayCommand]
    private void MesSiguiente()
    {
        MesActual = MesActual.AddMonths(1);
        ActualizarCitasPorFecha();
    }

    /// <summary>
    /// Vuelve al mes actual.
    /// </summary>
    [RelayCommand]
    private void IrAMesActual()
    {
        MesActual = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        ActualizarCitasPorFecha();
    }

    #endregion

    #region Métodos Públicos

    /// <summary>
    /// Carga el calendario para el mes especificado.
    /// </summary>
    /// <param name="mes">Mes a mostrar.</param>
    /// <param name="citas">Lista de citas para el mes.</param>
    public void CargarMes(DateTimeOffset mes, IEnumerable<Cita> citas)
    {
        MesActual = new DateTime(mes.Year, mes.Month, 1);
        Citas = new ObservableCollection<Cita>(citas);
        ActualizarCitasPorFecha();
    }

    /// <summary>
    /// Obtiene el número de citas para una fecha específica.
    /// </summary>
    public int ObtenerNumeroCitas(DateTime fecha)
    {
        return CitasPorFecha.TryGetValue(fecha.Date, out var count) ? count : 0;
    }

    /// <summary>
    /// Indica si una fecha tiene citas.
    /// </summary>
    public bool TieneCitas(DateTime fecha)
    {
        return CitasPorFecha.ContainsKey(fecha.Date);
    }

    #endregion

    #region Métodos Privados

    /// <summary>
    /// Actualiza el diccionario de citas por fecha.
    /// </summary>
    private void ActualizarCitasPorFecha()
    {
        CitasPorFecha.Clear();
        foreach (var cita in Citas)
        {
            var fecha = cita.Fecha.Date;
            if (CitasPorFecha.ContainsKey(fecha))
            {
                CitasPorFecha[fecha]++;
            }
            else
            {
                CitasPorFecha[fecha] = 1;
            }
        }
        OnPropertyChanged(nameof(CitasPorFecha));
    }

    /// <summary>
    /// Se ejecuta cuando cambian las citas.
    /// </summary>
    partial void OnCitasChanged(ObservableCollection<Cita> value)
    {
        ActualizarCitasPorFecha();
    }

    #endregion
}

