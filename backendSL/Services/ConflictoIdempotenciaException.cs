namespace ProjectSaborLatino.Services;

// No revela a quién pertenece la clave ni el contenido de la solicitud existente.
public sealed class ConflictoIdempotenciaException : Exception
{
    public ConflictoIdempotenciaException()
        : base("La clave de esta solicitud ya se utilizó con otros datos o con otra sesión.")
    {
    }
}
