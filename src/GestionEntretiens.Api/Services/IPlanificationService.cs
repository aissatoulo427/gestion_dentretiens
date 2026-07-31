using System;
using System.Collections.Generic;
using Gestion_dentretiens.Models;
using Gestion_dentretiens.Models.Enums;

namespace Gestion_dentretiens.Services
{
    /// <summary>
    /// Orchestration du cycle de vie d'un entretien : demande → créneaux → planification
    /// → confirmation / reprogrammation / annulation, avec envoi des e-mails.
    /// </summary>
    public interface IPlanificationService
    {
        DemandeEntretien GetDemande(int demandeId);

        /// <summary>Corrige l'intitulé du poste d'une demande — seul champ modifiable.</summary>
        DemandeEntretien ModifierPoste(int demandeId, string poste);
        IEnumerable<DemandeEntretien> GetDemandes();
        Entretien GetEntretien(int entretienId);
        IEnumerable<Entretien> GetEntretiens();

        DemandeEntretien CreerDemande(int rhId, int candidatId, string poste);
        Creneau DefinirDisponibilite(int employeId, DateTime debut, DateTime fin);
        void ProposerCreneau(int demandeId, int creneauId);
        Creneau GetCreneau(int creneauId);

        /// <summary>
        /// Supprime un créneau libre. Lève une exception s'il est réservé ou s'il reste
        /// rattaché à un entretien. Le contrôle de propriété appartient à l'appelant.
        /// </summary>
        void SupprimerCreneau(int creneauId);
        IEnumerable<Creneau> GetCreneaux();
        IEnumerable<Creneau> ConsulterCreneauxDisponibles(int demandeId);

        /// <summary>
        /// Planifie un tour d'entretien. <paramref name="evaluateurIds"/> désigne le panel :
        /// les employés qui seront les seuls autorisés à saisir un compte-rendu sur cet
        /// entretien. Le panel doit contenir au moins un évaluateur du rôle exigé par
        /// <paramref name="type"/> : un RH pour un tour RH, un EvaluateurTechnique pour un
        /// tour Technique, un Manager pour un tour Managerial.
        /// L'horaire n'est pas un paramètre : il vient du créneau désigné par
        /// <paramref name="creneauId"/>.
        /// </summary>
        Entretien PlanifierEntretien(int demandeId, int creneauId,
            Modalite modalite, string lieuOuLien, TypeEntretien type, IEnumerable<int> evaluateurIds);
        void ConfirmerEntretien(int entretienId);
        void Reprogrammer(int entretienId, int nouveauCreneauId);
        void AnnulerDemande(int demandeId);
        void EnvoyerRappel(int entretienId);
    }
}
