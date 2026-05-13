using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Ataena.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Ataena.Views;

public partial class SplashView : Window
{
    public SplashView()
    {
        InitializeComponent();
    }

    public async Task CargarConfiguracionAsync()
    {
        try
        {
            ActualizarEstado("Conectando con la base de datos...");
            await Task.Delay(300);

            using var db = new AtaenaDbContext();
            
            ActualizarEstado("Cargando configuración...");
            var config = await db.Configuracion.FirstOrDefaultAsync();

            if (config != null)
            {
                // Mostrar nombre del estudio
                var nombreEstudio = this.FindControl<TextBlock>("NombreEstudioText");
                if (nombreEstudio != null)
                {
                    nombreEstudio.Text = config.NombreEstudio;
                }

                // Cargar logo si existe
                if (!string.IsNullOrEmpty(config.LogoPath) && File.Exists(config.LogoPath))
                {
                    ActualizarEstado("Cargando logo...");
                    await CargarLogoAsync(config.LogoPath);
                }
            }

            ActualizarEstado("Preparando interfaz...");
            await Task.Delay(500);

            ActualizarEstado("¡Listo!");
            await Task.Delay(300);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al cargar configuración en splash");
            ActualizarEstado("Iniciando...");
            await Task.Delay(500);
        }
    }

    private void ActualizarEstado(string mensaje)
    {
        var estadoText = this.FindControl<TextBlock>("EstadoCargaText");
        if (estadoText != null)
        {
            estadoText.Text = mensaje;
        }
    }

    private async Task CargarLogoAsync(string rutaLogo)
    {
        try
        {
            await Task.Run(() =>
            {
                using var stream = File.OpenRead(rutaLogo);
                var bitmap = new Bitmap(stream);

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    var logoImage = this.FindControl<Image>("LogoImage");
                    var placeholder = this.FindControl<StackPanel>("PlaceholderLogo");

                    if (logoImage != null && placeholder != null)
                    {
                        logoImage.Source = bitmap;
                        logoImage.IsVisible = true;
                        placeholder.IsVisible = false;
                    }
                });
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo cargar el logo: {Ruta}", rutaLogo);
        }
    }
}
