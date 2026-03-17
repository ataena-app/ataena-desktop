using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Ataena.Data;
using Ataena.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Ataena.Services;

/// <summary>
/// Servicio para envío de emails mediante SMTP.
/// Utiliza la configuración almacenada en la base de datos.
/// </summary>
public class EmailService
{
    private readonly AtaenaDbContext _db;

    public EmailService(AtaenaDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Verifica si el SMTP está configurado correctamente.
    /// </summary>
    public async Task<bool> EstaConfiguradoAsync()
    {
        var cfg = await _db.Configuracion.FirstOrDefaultAsync(c => c.Id == 1);
        return cfg?.SmtpConfigurado ?? false;
    }

    /// <summary>
    /// Envía un email de recordatorio de cita al cliente.
    /// </summary>
    /// <param name="cita">Cita con Cliente incluido</param>
    /// <returns>True si se envió correctamente</returns>
    public async Task<(bool Exito, string Mensaje)> EnviarRecordatorioCitaAsync(Cita cita)
    {
        try
        {
            if (cita.Cliente == null)
            {
                return (false, "La cita no tiene cliente asociado.");
            }

            if (string.IsNullOrWhiteSpace(cita.Cliente.Email))
            {
                return (false, "El cliente no tiene email registrado.");
            }

            var cfg = await _db.Configuracion.FirstOrDefaultAsync(c => c.Id == 1);
            if (cfg == null || !cfg.SmtpConfigurado)
            {
                return (false, "El SMTP no está configurado. Ve a Configuración para configurarlo.");
            }

            // Cargar plantilla
            var cuerpoHtml = await GenerarCuerpoRecordatorioAsync(cita, cfg);

            // Crear mensaje
            var mensaje = new MailMessage
            {
                From = new MailAddress(cfg.SmtpUsuario!, cfg.NombreEstudio),
                Subject = $"📅 Recordatorio de tu cita en {cfg.NombreEstudio}",
                Body = cuerpoHtml,
                IsBodyHtml = true
            };
            mensaje.To.Add(new MailAddress(cita.Cliente.Email, cita.Cliente.NombreCompleto));

            // Configurar cliente SMTP (limpiar espacios de la contraseña por si viene de Google)
            var passwordLimpia = cfg.SmtpPassword?.Replace(" ", "") ?? "";
            using var smtp = new SmtpClient(cfg.SmtpServidor, cfg.SmtpPuerto)
            {
                EnableSsl = cfg.SmtpUsarSsl,
                Credentials = new NetworkCredential(cfg.SmtpUsuario, passwordLimpia),
                Timeout = 30000
            };

            await smtp.SendMailAsync(mensaje);

            Log.Information("Email de recordatorio enviado a {Email} para cita {CitaId}", 
                cita.Cliente.Email, cita.Id);

            return (true, $"Recordatorio enviado a {cita.Cliente.Email}");
        }
        catch (SmtpException ex)
        {
            Log.Error(ex, "Error SMTP al enviar recordatorio");
            return (false, $"Error de conexión SMTP: {ex.Message}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al enviar email de recordatorio");
            return (false, $"Error al enviar email: {ex.Message}");
        }
    }

    /// <summary>
    /// Genera el contenido HTML del email de recordatorio.
    /// </summary>
    private async Task<string> GenerarCuerpoRecordatorioAsync(Cita cita, Configuracion cfg)
    {
        var plantillaPath = Path.Combine(AppContext.BaseDirectory, "Plantillas", "RecordatorioCita.html");

        string plantilla;
        if (File.Exists(plantillaPath))
        {
            plantilla = await File.ReadAllTextAsync(plantillaPath);
        }
        else
        {
            plantilla = ObtenerPlantillaPorDefecto();
        }

        // Reemplazar placeholders
        var fechaCita = cita.Fecha.ToString("dddd, d 'de' MMMM 'de' yyyy", 
            new System.Globalization.CultureInfo("es-ES"));
        var horaCita = cita.HoraInicioFormateada;

        plantilla = plantilla
            .Replace("{NOMBRE_CLIENTE}", cita.Cliente.NombreCompleto)
            .Replace("{NOMBRE_ESTUDIO}", cfg.NombreEstudio)
            .Replace("{FECHA_CITA}", fechaCita)
            .Replace("{HORA_CITA}", horaCita)
            .Replace("{TIPO_CITA}", cita.TipoCita.ToString())
            .Replace("{ICONO_TIPO}", cita.IconoTipo)
            .Replace("{DURACION}", cita.DuracionFormateada)
            .Replace("{DESCRIPCION}", cita.Descripcion ?? "")
            .Replace("{DIRECCION_ESTUDIO}", cfg.Direccion ?? "")
            .Replace("{TELEFONO_ESTUDIO}", cfg.Telefono ?? "")
            .Replace("{EMAIL_ESTUDIO}", cfg.Email ?? cfg.SmtpUsuario ?? "");

        return plantilla;
    }

    /// <summary>
    /// Plantilla HTML por defecto si no existe el archivo.
    /// </summary>
    private static string ObtenerPlantillaPorDefecto()
    {
        return """
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Recordatorio de Cita</title>
</head>
<body style="font-family: 'Segoe UI', Arial, sans-serif; background-color: #0f172a; color: #e2e8f0; margin: 0; padding: 20px;">
    <div style="max-width: 600px; margin: 0 auto; background-color: #1e293b; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 24px rgba(0,0,0,0.3);">
        
        <!-- Header -->
        <div style="background: linear-gradient(135deg, #6366f1, #8b5cf6); padding: 30px; text-align: center;">
            <h1 style="margin: 0; font-size: 28px; color: white;">📅 Recordatorio de Cita</h1>
            <p style="margin: 10px 0 0; opacity: 0.9; color: white;">{NOMBRE_ESTUDIO}</p>
        </div>
        
        <!-- Content -->
        <div style="padding: 30px;">
            <p style="font-size: 18px; margin-bottom: 25px;">
                ¡Hola <strong>{NOMBRE_CLIENTE}</strong>! 👋
            </p>
            
            <p style="margin-bottom: 25px; line-height: 1.6;">
                Te recordamos que tienes una cita programada con nosotros:
            </p>
            
            <!-- Cita Card -->
            <div style="background-color: #334155; border-radius: 12px; padding: 20px; margin-bottom: 25px;">
                <div style="display: flex; align-items: center; margin-bottom: 15px;">
                    <span style="font-size: 32px; margin-right: 12px;">{ICONO_TIPO}</span>
                    <div>
                        <div style="font-size: 20px; font-weight: bold; color: #a78bfa;">{TIPO_CITA}</div>
                        <div style="opacity: 0.7; font-size: 14px;">{DESCRIPCION}</div>
                    </div>
                </div>
                
                <div style="border-top: 1px solid #475569; padding-top: 15px;">
                    <p style="margin: 8px 0;"><strong>📆 Fecha:</strong> {FECHA_CITA}</p>
                    <p style="margin: 8px 0;"><strong>⏰ Hora:</strong> {HORA_CITA}</p>
                    <p style="margin: 8px 0;"><strong>⏱️ Duración estimada:</strong> {DURACION}</p>
                </div>
            </div>
            
            <!-- Ubicación -->
            <div style="background-color: #334155; border-radius: 12px; padding: 20px; margin-bottom: 25px;">
                <p style="margin: 0 0 10px; font-weight: bold;">📍 Ubicación:</p>
                <p style="margin: 0; opacity: 0.9;">{DIRECCION_ESTUDIO}</p>
            </div>
            
            <!-- Contacto -->
            <div style="text-align: center; padding: 20px; background-color: #0f172a; border-radius: 12px;">
                <p style="margin: 0 0 10px; font-size: 14px; opacity: 0.7;">
                    ¿Necesitas cambiar o cancelar tu cita?
                </p>
                <p style="margin: 0;">
                    📞 <a href="tel:{TELEFONO_ESTUDIO}" style="color: #a78bfa; text-decoration: none;">{TELEFONO_ESTUDIO}</a>
                    &nbsp;&nbsp;|&nbsp;&nbsp;
                    ✉️ <a href="mailto:{EMAIL_ESTUDIO}" style="color: #a78bfa; text-decoration: none;">{EMAIL_ESTUDIO}</a>
                </p>
            </div>
        </div>
        
        <!-- Footer -->
        <div style="background-color: #0f172a; padding: 20px; text-align: center; font-size: 12px; opacity: 0.6;">
            <p style="margin: 0;">
                Este es un recordatorio automático de {NOMBRE_ESTUDIO}.<br>
                Por favor, no respondas a este correo.
            </p>
        </div>
    </div>
</body>
</html>
""";
    }

    /// <summary>
    /// Prueba la conexión SMTP con la configuración actual.
    /// </summary>
    public async Task<(bool Exito, string Mensaje)> ProbarConexionSmtpAsync()
    {
        try
        {
            var cfg = await _db.Configuracion.FirstOrDefaultAsync(c => c.Id == 1);
            if (cfg == null || !cfg.SmtpConfigurado)
            {
                return (false, "El SMTP no está configurado.");
            }

            // Limpiar espacios de la contraseña (Google las da con espacios)
            var passwordLimpia = cfg.SmtpPassword?.Replace(" ", "") ?? "";
            using var smtp = new SmtpClient(cfg.SmtpServidor, cfg.SmtpPuerto)
            {
                EnableSsl = cfg.SmtpUsarSsl,
                Credentials = new NetworkCredential(cfg.SmtpUsuario, passwordLimpia),
                Timeout = 10000
            };

            // Enviar email de prueba a uno mismo
            var mensaje = new MailMessage
            {
                From = new MailAddress(cfg.SmtpUsuario!, cfg.NombreEstudio),
                Subject = $"✅ Prueba de conexión SMTP - {cfg.NombreEstudio}",
                Body = $"<h2>¡Conexión exitosa!</h2><p>La configuración SMTP de {cfg.NombreEstudio} funciona correctamente.</p>",
                IsBodyHtml = true
            };
            mensaje.To.Add(cfg.SmtpUsuario!);

            await smtp.SendMailAsync(mensaje);

            Log.Information("Prueba de conexión SMTP exitosa");
            return (true, "Conexión SMTP exitosa. Se envió un email de prueba a tu correo.");
        }
        catch (SmtpException ex)
        {
            Log.Error(ex, "Error en prueba SMTP");
            return (false, $"Error SMTP: {ex.Message}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al probar conexión SMTP");
            return (false, $"Error: {ex.Message}");
        }
    }
}
