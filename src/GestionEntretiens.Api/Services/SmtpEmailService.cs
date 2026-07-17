using System;
using System.Net;
using System.Net.Mail;
using Gestion_dentretiens.Models;
using Gestion_dentretiens.Models.Enums;

namespace Gestion_dentretiens.Services
{
    /// <summary>
    /// Envoi réel via SMTP (System.Net.Mail). Les paramètres viennent de la configuration.
    /// </summary>
    public class SmtpEmailService : IEmailService
    {
        private readonly string _hote;
        private readonly int _port;
        private readonly string _expediteur;
        private readonly string _motDePasse;
        private readonly bool _ssl;

        public SmtpEmailService(string hote, int port, string expediteur, string motDePasse, bool ssl = true)
        {
            _hote = hote;
            _port = port;
            _expediteur = expediteur;
            _motDePasse = motDePasse;
            _ssl = ssl;
        }

        public void Envoyer(string destinataire, string sujet, string corps)
        {
            if (string.IsNullOrWhiteSpace(destinataire))
            {
                return;
            }

            using (var message = new MailMessage(_expediteur, destinataire, sujet, corps) { IsBodyHtml = false })
            using (var client = new SmtpClient(_hote, _port))
            {
               

            }
        }

        public void NotifierEntretien(Entretien entretien, TypeNotification type)
        {
            if (entretien?.Candidat == null)
            {
                return;
            }

            string sujet;
            string corps;
            ConstruireMessage(entretien, type, out sujet, out corps);
            Envoyer(entretien.Candidat.Email, sujet, corps);
        }

        private static void ConstruireMessage(Entretien e, TypeNotification type, out string sujet, out string corps)
        {
            string date = e.DateHeure.ToString("dddd d MMMM yyyy 'à' HH'h'mm");
            string lieu = string.IsNullOrEmpty(e.LieuOuLien) ? "(à préciser)" : e.LieuOuLien;

            switch (type)
            {
                case TypeNotification.Invitation:
                    sujet = "Invitation à un entretien";
                    corps = $"Bonjour,\n\nVous êtes invité(e) à un entretien le {date}.\nModalité : {e.Modalite} — {lieu}.\n\nMerci de confirmer votre présence.";
                    break;
                case TypeNotification.Rappel:
                    sujet = "Rappel : votre entretien approche";
                    corps = $"Bonjour,\n\nPetit rappel : votre entretien a lieu le {date}.\nModalité : {e.Modalite} — {lieu}.";
                    break;
                case TypeNotification.Confirmation:
                    sujet = "Confirmation de votre entretien";
                    corps = $"Bonjour,\n\nVotre entretien du {date} est confirmé.\nModalité : {e.Modalite} — {lieu}.";
                    break;
                case TypeNotification.Reprogrammation:
                    sujet = "Votre entretien a été reprogrammé";
                    corps = $"Bonjour,\n\nVotre entretien a été reprogrammé au {date}.\nModalité : {e.Modalite} — {lieu}.";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
