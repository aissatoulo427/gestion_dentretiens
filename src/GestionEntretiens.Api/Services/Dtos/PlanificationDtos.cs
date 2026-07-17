using Gestion_dentretiens.Models.Enums;

namespace Gestion_dentretiens.Api.Dtos;

// --- Demande ---
public record DemandeDto(int Id, string Poste, TypeEntretien TypeEntretien, DateTime DateCreation,
    StatutDemande Statut, int RecruteurId, int CandidatId);
public record CreateDemandeRequest(int RecruteurId, int CandidatId, string Poste, TypeEntretien TypeEntretien);

// --- Creneau ---
public record CreneauDto(int Id, DateTime DateDebut, DateTime DateFin, bool Disponible,
    int RecruteurId, int? DemandeEntretienId);
public record CreateCreneauRequest(int RecruteurId, DateTime DateDebut, DateTime DateFin);

// --- Entretien ---
public record EntretienDto(int Id, DateTime DateHeure, string LieuOuLien, StatutEntretien Statut,
    Modalite Modalite, int DemandeEntretienId, int CandidatId, int RecruteurId, int? CreneauId);
public record PlanifierRequest(int DemandeId, int CreneauId, DateTime DateHeure, Modalite Modalite, string LieuOuLien);
public record ReprogrammerRequest(int NouveauCreneauId, DateTime NouvelleDateHeure);

// --- Feedback ---
public record FeedbackDto(int Id, int Note, string Commentaire, Decision Decision, DateTime DateSaisie,
    int EntretienId, int AuteurId);
public record SaisirFeedbackRequest(int EntretienId, int AuteurId, int Note, string Commentaire, Decision Decision);
