using Gestion_dentretiens.Api.Dtos;
using Gestion_dentretiens.Models;

namespace Gestion_dentretiens.Api.Mapping;

/// <summary>Conversions entités du domaine → DTOs exposés par l'API.</summary>
public static class DtoMappings
{
    public static CandidatDto ToDto(this Candidat c) =>
        new(c.Id, c.Nom, c.Prenom, c.Email, c.Telephone);

    public static RecruteurDto ToDto(this Recruteur r) =>
        new(r.Id, r.Nom, r.Email);

    public static ManagerDto ToDto(this Manager m) =>
        new(m.Id, m.Nom, m.Email);

    public static DemandeDto ToDto(this DemandeEntretien d) =>
        new(d.Id, d.Poste, d.TypeEntretien, d.DateCreation, d.Statut, d.RecruteurId, d.CandidatId);

    public static CreneauDto ToDto(this Creneau c) =>
        new(c.Id, c.DateDebut, c.DateFin, c.Disponible, c.RecruteurId, c.DemandeEntretienId);

    public static EntretienDto ToDto(this Entretien e) =>
        new(e.Id, e.DateHeure, e.LieuOuLien, e.Statut, e.Modalite, e.DemandeEntretienId, e.CandidatId, e.RecruteurId, e.CreneauId);

    public static FeedbackDto ToDto(this Feedback f) =>
        new(f.Id, f.Note, f.Commentaire, f.Decision, f.DateSaisie, f.EntretienId, f.AuteurId);
}
