namespace Ataena.Models;

/// <summary>
/// Perfil normativo para tratamiento del DNI según CCAA del estudio.
/// </summary>
public enum PerfilNormativoDni
{
    /// <summary>
    /// Castilla-La Mancha y resto: datos en texto, sin imagen archivada en ficha.
    /// </summary>
    Estandar = 0,

    /// <summary>
    /// Comunidad de Madrid: imagen solo en consentimiento de trabajo (EPIC-0-02).
    /// </summary>
    Madrid = 1
}
