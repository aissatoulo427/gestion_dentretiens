using Gestion_dentretiens.Api.Dtos;
using Gestion_dentretiens.Models;

namespace Gestion_dentretiens.Api.Mapping;

/// <summary>Conversions entités du domaine → DTOs exposés par l'API.</summary>
public static class DtoMappings
{
    public static CandidatDto ToDto(this Candidat c) =>
        new(c.Id, c.Nom, c.Prenom, c.Email, c.Telephone);

    public static AdminDto ToDto(this Admin a) =>
        new(a.Id, a.Nom, a.Email);

    public static RHDto ToDto(this RH r) =>
        new(r.Id, r.Nom, r.Email);

    public static EvaluateurTechniqueDto ToDto(this EvaluateurTechnique e) =>
        new(e.Id, e.Nom, e.Email);

    public static ManagerDto ToDto(this Manager m) =>
        new(m.Id, m.Nom, m.Email);

    public static DemandeDto ToDto(this DemandeEntretien d) =>
        new(d.Id, d.Poste, d.DateCreation, d.Statut, d.RhId, d.CandidatId);

    public static CreneauDto ToDto(this Creneau c) =>
        new(c.Id, c.DateDebut, c.DateFin, c.Disponible, c.EmployeId, c.DemandeEntretienId);

    /// <summary>
    /// Les évaluateurs ne sont présents que si l'appelant a fait un Include :
    /// sans lui, la liste est vide plutôt que null.
    /// </summary>
    public static EntretienDto ToDto(this Entretien e) =>
        new(e.Id, e.DateHeure, e.LieuOuLien, e.Statut, e.Modalite, e.TypeEntretien,
            e.DemandeEntretienId, e.CandidatId,
            e.Evaluateurs?.Select(x => x.Id).ToList() ?? new List<int>(),
            e.CreneauId);

    public static FeedbackDto ToDto(this Feedback f) =>
        new(f.Id, f.Note, f.Commentaire, f.Decision, f.DateSaisie, f.EntretienId, f.AuteurId);
}
