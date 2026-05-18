using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ataena.Services;

namespace Ataena.ViewModels;

/// <summary>
/// Modal de novedades / changelog (tras actualizar o consulta manual).
/// </summary>
public partial class ChangelogViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _esVisible;

    [ObservableProperty]
    private string _titulo = "Novedades";

    [ObservableProperty]
    private string _subtitulo = string.Empty;

    [ObservableProperty]
    private string _textoCuerpo = string.Empty;

    /// <summary>
    /// Si true, al cerrar se marca la versión actual como vista.
    /// </summary>
    [ObservableProperty]
    private bool _esTrasActualizacion;

    public string TextoBotonCerrar => EsTrasActualizacion ? "Entendido" : "Cerrar";

    partial void OnEsTrasActualizacionChanged(bool value) =>
        OnPropertyChanged(nameof(TextoBotonCerrar));

    /// <summary>
    /// Muestra solo los cambios desde la última versión vista.
    /// </summary>
    public void AbrirTrasActualizacion()
    {
        var desde = ChangelogService.ObtenerUltimaVersionVista();
        var entradas = ChangelogService.ObtenerEntradasNuevas(desde);
        if (entradas.Count == 0)
            return;

        EsTrasActualizacion = true;

        if (entradas.Count == 1)
            Titulo = $"Novedades en v{entradas[0].VersionTexto}";
        else
            Titulo = "Novedades de la actualización";

        Subtitulo = desde is null
            ? "Bienvenido a esta versión de Ataena:"
            : "Cambios desde tu versión anterior:";

        TextoCuerpo = ChangelogService.CombinarEntradasParaUi(entradas);
        EsVisible = true;
        OnPropertyChanged(nameof(TextoBotonCerrar));
    }

    /// <summary>
    /// Historial completo del changelog empaquetado.
    /// </summary>
    public void AbrirHistorial()
    {
        var entradas = ChangelogService.CargarTodasLasEntradas();
        if (entradas.Count == 0)
        {
            TextoCuerpo = "No se encontró el archivo de novedades (CHANGELOG.md).";
            Subtitulo = string.Empty;
        }
        else
        {
            var actual = ActualizacionService.ObtenerVersionActual();
            Subtitulo = $"Versión instalada: {actual.Major}.{actual.Minor}.{actual.Build}";
            TextoCuerpo = ChangelogService.CombinarEntradasParaUi(entradas);
        }

        EsTrasActualizacion = false;
        Titulo = "Historial de cambios";
        EsVisible = true;
        OnPropertyChanged(nameof(TextoBotonCerrar));
    }

    [RelayCommand]
    private void Cerrar()
    {
        if (EsTrasActualizacion)
            ChangelogService.MarcarVersionActualComoVista();

        EsVisible = false;
        TextoCuerpo = string.Empty;
    }
}
