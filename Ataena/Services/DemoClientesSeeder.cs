using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ataena.Data;
using Ataena.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Ataena.Services;

/// <summary>
/// Inserta clientes de demostración en la base de datos local (sin consentimientos).
/// </summary>
public static class DemoClientesSeeder
{
    public const string MarcaNotas = "Cliente demo (seed)";

    private static readonly char[] LetrasDni = "TRWAGMYFPDXBNJZSQVHLCKE".ToCharArray();

    private static readonly string[] Nombres =
    [
        "María", "José", "Carmen", "Antonio", "Isabel", "Francisco", "Laura", "David",
        "Ana", "Javier", "Lucía", "Daniel", "Elena", "Carlos", "Marta", "Pablo",
        "Sofía", "Álvaro", "Paula", "Sergio", "Cristina", "Rubén", "Nuria", "Iván",
        "Beatriz", "Raúl", "Silvia", "Óscar", "Rocío", "Héctor"
    ];

    private static readonly string[] Apellidos =
    [
        "García", "Fernández", "González", "Rodríguez", "López", "Martínez", "Sánchez",
        "Pérez", "Gómez", "Martín", "Jiménez", "Ruiz", "Hernández", "Díaz", "Moreno",
        "Muñoz", "Álvarez", "Romero", "Alonso", "Gutiérrez", "Navarro", "Torres",
        "Domínguez", "Vázquez", "Ramos", "Gil", "Ramírez", "Serrano", "Blanco", "Suárez"
    ];

    /// <summary>
    /// Añade clientes demo si aún no existen en la BD.
    /// </summary>
    public static async Task<int> SembrarAsync(AtaenaDbContext db, int cantidad = 100)
    {
        var yaExisten = await db.Clientes.CountAsync(c => c.Notas == MarcaNotas);
        if (yaExisten >= cantidad)
        {
            Log.Information("Ya hay {Count} clientes demo en la BD; no se insertan más.", yaExisten);
            return 0;
        }

        var faltan = cantidad - yaExisten;
        var dnisUsados = await db.Clientes
            .Where(c => c.Dni != null)
            .Select(c => c.Dni!)
            .ToListAsync();

        var conjuntoDnis = new HashSet<string>(dnisUsados, StringComparer.OrdinalIgnoreCase);
        var random = new Random(42);
        var insertados = 0;
        var baseNumero = 48_000_000;

        for (var i = 0; insertados < faltan; i++)
        {
            var numero = baseNumero + yaExisten + i;
            var dni = GenerarDni(numero);
            if (!conjuntoDnis.Add(dni))
                continue;

            var nombre = Nombres[random.Next(Nombres.Length)];
            var apellido1 = Apellidos[random.Next(Apellidos.Length)];
            var apellido2 = Apellidos[random.Next(Apellidos.Length)];

            var anioNac = random.Next(1975, 2004);
            var mesNac = random.Next(1, 13);
            var diaNac = random.Next(1, DateTime.DaysInMonth(anioNac, mesNac) + 1);

            db.Clientes.Add(new Cliente
            {
                Nombre = nombre,
                Apellidos = $"{apellido1} {apellido2}",
                Dni = dni,
                Telefono = $"6{random.Next(10, 99)} {random.Next(100, 999)} {random.Next(100, 999)}",
                Email = $"{TextoBusquedaHelper.Normalizar(nombre).Replace(" ", "")}.{TextoBusquedaHelper.Normalizar(apellido1)}@demo.local",
                FechaNacimiento = new DateTime(anioNac, mesNac, diaNac),
                PermiteFotosTrabajo = random.Next(0, 10) > 2,
                Notas = MarcaNotas,
                FechaRegistro = DateTime.Now.AddDays(-random.Next(1, 365)),
                Activo = true
            });

            insertados++;
        }

        await db.SaveChangesAsync();
        Log.Information("Insertados {Count} clientes demo.", insertados);
        return insertados;
    }

    private static string GenerarDni(int numero)
    {
        var n = Math.Abs(numero) % 100_000_000;
        return $"{n:D8}{LetrasDni[n % 23]}";
    }
}
