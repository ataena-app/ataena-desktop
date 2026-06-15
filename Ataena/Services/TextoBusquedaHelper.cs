using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Ataena.Models;

namespace Ataena.Services;

/// <summary>
/// Utilidades para búsquedas insensibles a mayúsculas y tildes.
/// </summary>
public static class TextoBusquedaHelper
{
    /// <summary>
    /// Normaliza texto para comparación: minúsculas sin diacríticos.
    /// </summary>
    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        var descompuesto = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);
        foreach (var c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    /// <summary>
    /// Solo dígitos (útil para teléfono y DNI).
    /// </summary>
    public static string SoloDigitos(string? texto) =>
        string.IsNullOrEmpty(texto) ? string.Empty : new string(texto.Where(char.IsDigit).ToArray());

    /// <summary>
    /// Indica si un cliente coincide con el término de búsqueda ya normalizado.
    /// </summary>
    public static bool ClienteCoincide(Cliente cliente, string terminoNormalizado, string? terminoDigitos = null)
    {
        if (string.IsNullOrEmpty(terminoNormalizado) && string.IsNullOrEmpty(terminoDigitos))
            return true;

        var nombre = Normalizar(cliente.Nombre);
        var apellidos = Normalizar(cliente.Apellidos);
        var nombreCompleto = $"{nombre} {apellidos}".Trim();

        if (!string.IsNullOrEmpty(terminoNormalizado))
        {
            if (nombre.Contains(terminoNormalizado, StringComparison.Ordinal) ||
                apellidos.Contains(terminoNormalizado, StringComparison.Ordinal) ||
                nombreCompleto.Contains(terminoNormalizado, StringComparison.Ordinal))
                return true;

            if (!string.IsNullOrEmpty(cliente.Email) &&
                Normalizar(cliente.Email).Contains(terminoNormalizado, StringComparison.Ordinal))
                return true;

            if (!string.IsNullOrEmpty(cliente.Dni) &&
                Normalizar(cliente.Dni).Contains(terminoNormalizado, StringComparison.Ordinal))
                return true;

            if (!string.IsNullOrEmpty(cliente.Telefono) &&
                Normalizar(cliente.Telefono).Contains(terminoNormalizado, StringComparison.Ordinal))
                return true;
        }

        if (!string.IsNullOrEmpty(terminoDigitos))
        {
            var telDigitos = SoloDigitos(cliente.Telefono);
            var dniDigitos = SoloDigitos(cliente.Dni);
            if ((!string.IsNullOrEmpty(telDigitos) && telDigitos.Contains(terminoDigitos, StringComparison.Ordinal)) ||
                (!string.IsNullOrEmpty(dniDigitos) && dniDigitos.Contains(terminoDigitos, StringComparison.Ordinal)))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Indica si un trabajo coincide con el término (cliente + descripción/zona/estilo).
    /// </summary>
    public static bool TrabajoCoincide(Trabajo trabajo, string terminoNormalizado, string? terminoDigitos = null)
    {
        if (string.IsNullOrEmpty(terminoNormalizado) && string.IsNullOrEmpty(terminoDigitos))
            return true;

        if (ClienteCoincide(trabajo.Cliente, terminoNormalizado, terminoDigitos))
            return true;

        if (string.IsNullOrEmpty(terminoNormalizado))
            return false;

        if (Normalizar(trabajo.Descripcion).Contains(terminoNormalizado, StringComparison.Ordinal))
            return true;

        if (!string.IsNullOrEmpty(trabajo.ZonaCuerpo) &&
            Normalizar(trabajo.ZonaCuerpo).Contains(terminoNormalizado, StringComparison.Ordinal))
            return true;

        if (!string.IsNullOrEmpty(trabajo.Estilo) &&
            Normalizar(trabajo.Estilo).Contains(terminoNormalizado, StringComparison.Ordinal))
            return true;

        if (!string.IsNullOrEmpty(trabajo.Tamano) &&
            Normalizar(trabajo.Tamano).Contains(terminoNormalizado, StringComparison.Ordinal))
            return true;

        return false;
    }
}
