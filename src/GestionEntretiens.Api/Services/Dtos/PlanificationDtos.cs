using Gestion_dentretiens.Models.Enums;

namespace Gestion_dentretiens.Api.Dtos;

// --- Demande ---
// Le type de tour n'est plus porté par la demande : il l'est par chaque entretien.
public record DemandeDto(int Id, string Poste, DateTime DateCreation,
    StatutDemande Statut, int RhId, int CandidatId);
// Pas de RhId : l'organisateur de la demande est l'utilisateur connecté, lu dans le JWT.
public record CreateDemandeRequest(int CandidatId, string Poste);
// Seul le poste se corrige : le RH, le candidat et la date identifient la demande.
public record UpdateDemandeRequest(string Poste);

// --- Creneau ---
public record CreneauDto(int Id, DateTime DateDebut, DateTime DateFin, bool Disponible,
    int EmployeId, int? DemandeEntretienId);
// Pas d'EmployeId : le propriétaire du créneau est l'utilisateur connecté, lu dans
// le JWT. Le laisser dans le corps revenait à laisser l'appelant déclarer qui il est.
public record CreateCreneauRequest(DateTime DateDebut, DateTime DateFin);

// --- Entretien ---
// EvaluateurIds = le panel. L'organisateur n'est plus recopié ici : il se lit via la demande.
public record EntretienDto(int Id, DateTime DateHeure, string LieuOuLien, StatutEntretien Statut,
    Modalite Modalite, TypeEntretien TypeEntretien, int DemandeEntretienId, int CandidatId,
    IEnumerable<int> EvaluateurIds, int? CreneauId);
// La date n'est pas dans la requête : elle est déduite du créneau choisi. La renseigner
// à part ouvrait la porte à un entretien daté un jeudi sur un créneau du mardi.
public record PlanifierRequest(int DemandeId, int CreneauId, Modalite Modalite,
    string LieuOuLien, TypeEntretien TypeEntretien, IEnumerable<int> EvaluateurIds);
public record ReprogrammerRequest(int NouveauCreneauId);

// --- Feedback ---
public record FeedbackDto(int Id, int Note, string Commentaire, Decision Decision, DateTime DateSaisie,
    int EntretienId, int AuteurId);
// Pas d'AuteurId : le compte-rendu est signé par l'utilisateur connecté, lu dans le JWT.
public record SaisirFeedbackRequest(int EntretienId, int Note, string Commentaire, Decision Decision);
