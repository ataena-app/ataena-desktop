using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using InkStudio.Data;
using InkStudio.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Serilog;

namespace InkStudio.Services;

/// <summary>
/// Servicio para generar PDFs de consentimientos firmados.
/// </summary>
public static class ConsentimientoService
{
    /// <summary>
    /// Carga el texto de una plantilla de consentimiento.
    /// </summary>
    /// <param name="tipo">Tipo de consentimiento.</param>
    /// <returns>Texto de la plantilla, o null si no se encuentra.</returns>
    public static string? CargarPlantillaTexto(TipoConsentimiento tipo)
    {
        try
        {
            var rutaPlantillas = ConsentimientoPathService.ObtenerRutaPlantillas();
            var nombreArchivo = tipo switch
            {
                TipoConsentimiento.RGPD => "ConsentimientoRGPD.txt",
                TipoConsentimiento.RGPD_Menor => "ConsentimientoRGPD_Menor.txt",
                TipoConsentimiento.Imagenes => "ConsentimientoImagenes.txt",
                TipoConsentimiento.Trabajo => "ConsentimientoTrabajo.txt",
                TipoConsentimiento.Trabajo_Menor => "ConsentimientoTrabajo_Menor.txt",
                _ => null
            };

            if (string.IsNullOrEmpty(nombreArchivo))
            {
                Log.Warning("Tipo de consentimiento no reconocido: {Tipo}", tipo);
                return null;
            }

            var rutaCompleta = Path.Combine(rutaPlantillas, nombreArchivo);

            if (!File.Exists(rutaCompleta))
            {
                Log.Error("Plantilla no encontrada: {Ruta}", rutaCompleta);
                return null;
            }

            var texto = File.ReadAllText(rutaCompleta, Encoding.UTF8);
            Log.Debug("Plantilla cargada: {Archivo}", nombreArchivo);
            return texto;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al cargar plantilla de consentimiento: {Tipo}", tipo);
            return null;
        }
    }

    /// <summary>
    /// Reemplaza los placeholders en el texto con datos reales.
    /// </summary>
    /// <param name="texto">Texto con placeholders.</param>
    /// <param name="cliente">Cliente que firma.</param>
    /// <param name="trabajo">Trabajo asociado (opcional).</param>
    /// <param name="configuracion">Configuración del estudio.</param>
    /// <param name="fechaFirma">Fecha de la firma.</param>
    /// <returns>Texto con placeholders reemplazados.</returns>
    public static string ReemplazarPlaceholders(
        string texto,
        Cliente cliente,
        Trabajo? trabajo,
        Configuracion configuracion,
        DateTime fechaFirma)
    {
        var resultado = texto;

        // Datos del cliente
        resultado = resultado.Replace("{NOMBRE_CLIENTE}", cliente.NombreCompleto);
        resultado = resultado.Replace("{DNI_CLIENTE}", cliente.Dni ?? "No especificado");
        resultado = resultado.Replace("{FECHA_NACIMIENTO_CLIENTE}", 
            cliente.FechaNacimiento?.ToString("dd/MM/yyyy") ?? "No especificada");

        // Datos del tutor (para menores)
        resultado = resultado.Replace("{NOMBRE_TUTOR}", cliente.NombreCompletoTutor ?? "No especificado");
        resultado = resultado.Replace("{DNI_TUTOR}", cliente.DniTutor ?? "No especificado");
        resultado = resultado.Replace("{TELEFONO_TUTOR}", cliente.TelefonoTutor ?? "No especificado");

        // Datos del estudio y empresa
        resultado = resultado.Replace("{NOMBRE_ESTUDIO}", configuracion.NombreEstudio);
        resultado = resultado.Replace("{NOMBRE_EMPRESA}", configuracion.NombreEmpresa ?? configuracion.NombreEstudio);
        resultado = resultado.Replace("{DIRECCION_ESTUDIO}", configuracion.Direccion ?? "No especificada");
        resultado = resultado.Replace("{EMAIL_ESTUDIO}", configuracion.Email ?? "No especificado");
        resultado = resultado.Replace("{TELEFONO_ESTUDIO}", configuracion.Telefono ?? "No especificado");

        // Fecha y hora
        resultado = resultado.Replace("{FECHA_FIRMA}", fechaFirma.ToString("dd/MM/yyyy"));
        resultado = resultado.Replace("{HORA_FIRMA}", fechaFirma.ToString("HH:mm"));

        // Datos del trabajo (si aplica)
        if (trabajo != null)
        {
            resultado = resultado.Replace("{TIPO_TRABAJO}", trabajo.Tipo.ToString());
            resultado = resultado.Replace("{DESCRIPCION_TRABAJO}", trabajo.Descripcion ?? "No especificada");
            resultado = resultado.Replace("{ZONA_CUERPO}", trabajo.ZonaCuerpo ?? "No especificada");
            // Usar la duración real si existe, si no la estimada, si no indicar no especificada
            var minutosTrabajo = trabajo.DuracionRealMinutos
                                 ?? trabajo.DuracionEstimadaMinutos;
            resultado = resultado.Replace("{DURACION_MINUTOS}", minutosTrabajo.HasValue && minutosTrabajo.Value > 0
                ? minutosTrabajo.Value.ToString()
                : "No especificada");
            resultado = resultado.Replace("{PRECIO}", trabajo.Precio.ToString("F2"));
        }

        // Datos del profesional (desde configuración, si están disponibles)
        resultado = resultado.Replace("{NOMBRE_PROFESIONAL}", configuracion.NombreEstudio); // Por ahora usar nombre del estudio
        resultado = resultado.Replace("{NUMERO_COLEGIADO}", "No especificado"); // TODO: Agregar campo a Configuracion si es necesario

        return resultado;
    }

    /// <summary>
    /// Genera un PDF de consentimiento firmado.
    /// </summary>
    /// <param name="consentimiento">Consentimiento con datos.</param>
    /// <param name="imagenFirmaBase64">Imagen de la firma en base64.</param>
    /// <param name="rutaPdf">Ruta donde guardar el PDF.</param>
    /// <param name="imagenFirmaTutorBase64">Imagen de la firma del tutor en base64 (para menores).</param>
    /// <returns>True si se generó correctamente, False en caso contrario.</returns>
    public static async Task<bool> GenerarPdfConsentimiento(
        Consentimiento consentimiento,
        string imagenFirmaBase64,
        string rutaPdf,
        string? imagenFirmaTutorBase64 = null)
    {
        try
        {
            // Cargar datos necesarios
            using var context = new InkStudioDbContext();
            
            var cliente = await context.Clientes.FindAsync(consentimiento.ClienteId);
            if (cliente == null)
            {
                Log.Error("Cliente no encontrado: {ClienteId}", consentimiento.ClienteId);
                return false;
            }

            Trabajo? trabajo = null;
            if (consentimiento.TrabajoId.HasValue)
            {
                trabajo = await context.Trabajos.FindAsync(consentimiento.TrabajoId.Value);
            }

            var configuracion = await context.Configuracion.FindAsync(1);
            if (configuracion == null)
            {
                Log.Error("Configuración no encontrada");
                return false;
            }

            // Cargar plantilla
            var plantillaTexto = CargarPlantillaTexto(consentimiento.Tipo);
            if (string.IsNullOrEmpty(plantillaTexto))
            {
                Log.Error("No se pudo cargar la plantilla para tipo: {Tipo}", consentimiento.Tipo);
                return false;
            }

            // Reemplazar placeholders
            var textoFinal = ReemplazarPlaceholders(
                plantillaTexto,
                cliente,
                trabajo,
                configuracion,
                consentimiento.FechaFirma);

            // Convertir imagen base64 a bytes
            byte[]? imagenBytes = null;
            if (!string.IsNullOrEmpty(imagenFirmaBase64))
            {
                try
                {
                    // Si viene como data URL, extraer solo el base64
                    var base64 = imagenFirmaBase64;
                    if (base64.StartsWith("data:image"))
                    {
                        var index = base64.IndexOf(',');
                        if (index > 0)
                        {
                            base64 = base64.Substring(index + 1);
                        }
                    }
                    imagenBytes = Convert.FromBase64String(base64);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error al convertir imagen base64, se generará PDF sin firma");
                    imagenBytes = null;
                }
            }

            // Convertir imagen del tutor a bytes (para consentimientos de menores)
            byte[]? imagenTutorBytes = null;
            if (!string.IsNullOrEmpty(imagenFirmaTutorBase64))
            {
                try
                {
                    var base64Tutor = imagenFirmaTutorBase64;
                    if (base64Tutor.StartsWith("data:image"))
                    {
                        var indexTutor = base64Tutor.IndexOf(',');
                        if (indexTutor > 0)
                        {
                            base64Tutor = base64Tutor.Substring(indexTutor + 1);
                        }
                    }
                    imagenTutorBytes = Convert.FromBase64String(base64Tutor);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error al convertir imagen del tutor base64");
                    imagenTutorBytes = null;
                }
            }

            // Generar PDF
            QuestPDF.Settings.License = LicenseType.Community; // Licencia gratuita para uso no comercial

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Helvetica"));

                    page.Content()
                        .Column(column =>
                        {
                            column.Spacing(1f, Unit.Centimetre);

                            // Título
                            column.Item()
                                .PaddingBottom(0.5f, Unit.Centimetre)
                                .Text(text =>
                                {
                                    text.Span("CONSENTIMIENTO")
                                        .FontSize(18)
                                        .Bold();
                                });

                            // Línea separadora
                            column.Item()
                                .BorderBottom(1)
                                .PaddingBottom(0.5f, Unit.Centimetre);

                            // Texto del consentimiento
                            var parrafos = textoFinal.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var parrafo in parrafos)
                            {
                                if (!string.IsNullOrWhiteSpace(parrafo))
                                {
                                    column.Item()
                                        .PaddingBottom(0.3f, Unit.Centimetre)
                                        .Text(parrafo.Trim())
                                        .FontSize(11)
                                        .AlignLeft();
                                }
                            }

                            // Espacio antes de la firma
                            column.Item()
                                .Height(2f, Unit.Centimetre);

                            // Imagen de la firma (cliente o menor)
                            if (imagenBytes != null)
                            {
                                var tituloFirma = consentimiento.EsConsentimientoMenor 
                                    ? $"Firma del menor ({cliente.NombreCompleto}):" 
                                    : "Firma del cliente:";
                                
                                column.Item()
                                    .PaddingTop(1, Unit.Centimetre)
                                    .PaddingBottom(0.5f, Unit.Centimetre)
                                    .Text(tituloFirma)
                                    .FontSize(10)
                                    .Italic();

                                column.Item()
                                    .Height(3f, Unit.Centimetre)
                                    .Image(imagenBytes)
                                    .FitArea();
                            }

                            // Imagen de la firma del tutor (para consentimientos de menores)
                            if (imagenTutorBytes != null && consentimiento.EsConsentimientoMenor)
                            {
                                column.Item()
                                    .PaddingTop(0.5f, Unit.Centimetre)
                                    .PaddingBottom(0.5f, Unit.Centimetre)
                                    .Text($"Firma del tutor/representante legal ({consentimiento.NombreTutorFirmante}, DNI: {consentimiento.DniTutorFirmante}):")
                                    .FontSize(10)
                                    .Italic();

                                column.Item()
                                    .Height(3f, Unit.Centimetre)
                                    .Image(imagenTutorBytes)
                                    .FitArea();
                            }

                            // Datos de firma
                            column.Item()
                                .PaddingTop(1, Unit.Centimetre)
                                .Text(text =>
                                {
                                    text.Span($"Firmado el {consentimiento.FechaFirma:dd/MM/yyyy} a las {consentimiento.FechaFirma:HH:mm}")
                                        .FontSize(9)
                                        .Italic();
                                });
                        });
                });
            })
            .GeneratePdf();

            // Guardar PDF
            var directorio = Path.GetDirectoryName(rutaPdf);
            if (!string.IsNullOrEmpty(directorio) && !Directory.Exists(directorio))
            {
                Directory.CreateDirectory(directorio);
            }

            await File.WriteAllBytesAsync(rutaPdf, pdfBytes);

            Log.Information("PDF de consentimiento generado: {Ruta}", rutaPdf);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al generar PDF de consentimiento");
            return false;
        }
    }

    /// <summary>
    /// Valida si un cliente tiene los consentimientos requeridos.
    /// </summary>
    /// <param name="clienteId">ID del cliente.</param>
    /// <returns>True si tiene RGPD firmado, False en caso contrario.</returns>
    public static async Task<bool> ValidarConsentimientosRequeridos(int clienteId)
    {
        try
        {
            using var context = new InkStudioDbContext();
            
            var tieneRGPD = await context.Consentimientos
                .AnyAsync(c => c.ClienteId == clienteId && 
                              c.Tipo == TipoConsentimiento.RGPD && 
                              c.Firmado);

            return tieneRGPD;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al validar consentimientos del cliente: {ClienteId}", clienteId);
            return false;
        }
    }

    /// <summary>
    /// Guarda un consentimiento en la base de datos.
    /// </summary>
    /// <param name="consentimiento">Consentimiento a guardar.</param>
    /// <param name="rutaPdf">Ruta del PDF generado.</param>
    /// <returns>True si se guardó correctamente, False en caso contrario.</returns>
    public static async Task<bool> GuardarConsentimiento(Consentimiento consentimiento, string rutaPdf)
    {
        try
        {
            using var context = new InkStudioDbContext();
            
            consentimiento.RutaDocumento = rutaPdf;
            consentimiento.Firmado = true;
            consentimiento.FechaFirma = DateTime.Now;

            if (consentimiento.Id == 0)
            {
                context.Consentimientos.Add(consentimiento);
            }
            else
            {
                context.Consentimientos.Update(consentimiento);
            }

            await context.SaveChangesAsync();

            Log.Information("Consentimiento guardado: ID={Id}, Tipo={Tipo}, Cliente={ClienteId}", 
                consentimiento.Id, consentimiento.Tipo, consentimiento.ClienteId);
            
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al guardar consentimiento");
            return false;
        }
    }
}

