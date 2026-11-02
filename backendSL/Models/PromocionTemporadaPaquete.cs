namespace ProjectSaborLatino.Models;

// Relación entre una promoción de temporada y los paquetes del catálogo incluidos en ella
public class PromocionTemporadaPaquete
{
    public int Id { get; set; }

    public int PromocionTemporadaId { get; set; }
    public PromocionTemporada? PromocionTemporada { get; set; }

    public int PaqueteId { get; set; }
    public Paquete? Paquete { get; set; }
}
