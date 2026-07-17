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
        IEnumerable<DemandeEntretien> GetDemandes();
        Entretien GetEntretien(int entretienId);
        IEnumerable<Entretien> GetEntretiens();

        DemandeEntretien CreerDemande(int recruteurId, int candidatId, string poste, TypeEntretien type);
        Creneau DefinirDisponibilite(int recruteurId, DateTime debut, DateTime fin);
        void ProposerCreneau(int demandeId, int creneauId);
        Creneau GetCreneau(int creneauId);
        IEnumerable<Creneau> GetCreneaux();
        IEnumerable<Creneau> ConsulterCreneauxDisponibles(int demandeId);

        Entretien PlanifierEntretien(int demandeId, int creneauId, DateTime dateHeure, Modalite modalite, string lieuOuLien);
        void ConfirmerEntretien(int entretienId);
        void Reprogrammer(int entretienId, int nouveauCreneauId, DateTime nouvelleDateHeure);
        void AnnulerDemande(int demandeId);
        void EnvoyerRappel(int entretienId);
    }
}
