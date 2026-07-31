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

            // Pas de mot de passe SMTP configuré : on trace le message dans la console
            // au lieu de planter. Pratique en dev/démo, sans rien changer au code appelant.
            if (string.IsNullOrWhiteSpace(_motDePasse))
            {
                Console.WriteLine($"[MAIL non envoyé — SMTP non configuré]\nÀ : {destinataire}\nSujet : {sujet}\n{corps}\n");
                return;
            }

            using (var message = new MailMessage(_expediteur, destinataire, sujet, corps) { IsBodyHtml = false })
            using (var client = new SmtpClient(_hote, _port))
            {
                client.EnableSsl = _ssl;
                client.Credentials = new NetworkCredential(_expediteur, _motDePasse);
                client.Send(message);
            }
        }

        public void EnvoyerCodeReinitialisation(string destinataire, string code, int heuresValidite)
        {
            const string sujet = "Réinitialisation de votre mot de passe";
            string corps =
                $"Bonjour,\n\nVoici votre code de réinitialisation : {code}\n\n" +
                $"Il est valable {heuresValidite} heures et ne peut servir qu'une seule fois.\n" +
                "Si vous n'êtes pas à l'origine de cette demande, ignorez cet e-mail.";

            Envoyer(destinataire, sujet, corps);
        }

        public void EnvoyerCodeActivation(string destinataire, string code, int joursValidite)
        {
            const string sujet = "Activation de votre compte";
            string corps =
                $"Bonjour,\n\nUn compte vient d'être créé pour vous sur l'application de " +
                $"gestion des entretiens.\n\nVoici votre code d'activation : {code}\n\n" +
                $"Il est valable {joursValidite} jours et ne peut servir qu'une seule fois. " +
                "Saisissez-le avec le mot de passe de votre choix pour activer votre compte.\n" +
                "Passé ce délai, utilisez « mot de passe oublié » pour en recevoir un nouveau.";

            Envoyer(destinataire, sujet, corps);
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
