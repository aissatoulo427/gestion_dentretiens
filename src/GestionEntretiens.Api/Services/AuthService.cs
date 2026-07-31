using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Gestion_dentretiens.Api.Dtos;
using Gestion_dentretiens.Data;
using Gestion_dentretiens.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Gestion_dentretiens.Services
{
    public class AuthService : IAuthService
    {
        /// <summary>Durée de validité d'un code OTP de réinitialisation.</summary>
        private const int HeuresValiditeCode = 72;

        /// <summary>
        /// Durée de validité d'un code d'activation, en jours. Plus longue que celle d'une
        /// réinitialisation : une activation n'a pas la même urgence, un nouvel arrivant
        /// peut ne relever ses messages que plusieurs jours plus tard.
        /// </summary>
        private const int JoursValiditeActivation = 7;

        /// <summary>Nombre d'essais ratés au-delà duquel le code est refusé.</summary>
        private const int TentativesMax = 5;

        private readonly AppDbContext _db;
        private readonly IPasswordHasher<Personne> _hasher;
        private readonly IConfiguration _config;
        private readonly IEmailService _email;

        /// <summary>Reçoit le DbContext, le hacheur de mot de passe, la configuration (JWT) et l'envoi d'e-mails par injection.</summary>
        public AuthService(AppDbContext db, IPasswordHasher<Personne> hasher, IConfiguration config, IEmailService email)
        {
            _db = db;
            _hasher = hasher;
            _config = config;
            _email = email;
        }

        /// <summary>
        /// Vérifie l'email + le mot de passe et renvoie un JWT en cas de succès, sinon null.
        /// La requête porte sur _db.Employes : les candidats n'ont pas de compte et sont
        /// donc écartés par le DbSet lui-même, sans test de type.
        /// </summary>
        public LoginResponse Login(string email, string motDePasse)
        {
            var employe = _db.Employes.FirstOrDefault(e => e.Email == email);

            // Compte inexistant (ou candidat), ou mot de passe non défini.
            if (employe == null) return null;
            if (string.IsNullOrEmpty(employe.MotDePasse)) return null;

            // Vérifie le mot de passe fourni contre le hash stocké.
            var resultat = _hasher.VerifyHashedPassword(employe, employe.MotDePasse, motDePasse);
            if (resultat == PasswordVerificationResult.Failed) return null;

            // "RH", "EvaluateurTechnique" ou "Manager". Fiable tant que les proxies de
            // lazy loading EF Core ne sont pas activés : ils renverraient "RHProxy".
            var role = employe.GetType().Name;
            return GenererToken(employe, role);
        }

        /// <summary>
        /// Génère un code OTP à 6 chiffres, le stocke HACHÉ avec sa date d'expiration,
        /// et l'envoie par e-mail. Une nouvelle demande écrase le code précédent.
        /// Sort en silence si le compte n'existe pas ou n'est pas un compte connectable :
        /// l'appelant répond 200 dans tous les cas pour ne pas révéler quels e-mails
        /// sont enregistrés.
        /// </summary>
        public void DemanderReinitialisation(string email)
        {
            var employe = _db.Employes.FirstOrDefault(e => e.Email == email);

            if (employe == null) return;

            var code = PoserNouveauCode(employe, TimeSpan.FromHours(HeuresValiditeCode));

            // Le code en clair n'existe qu'ici et dans l'e-mail : il n'est jamais persisté.
            _email.EnvoyerCodeReinitialisation(employe.Email, code, HeuresValiditeCode);
        }

        /// <summary>
        /// Envoie le code permettant à un employé qui vient d'être créé de choisir son
        /// mot de passe. Même mécanisme que la réinitialisation — code haché, expiration,
        /// usage unique, plafond de tentatives — avec une durée plus longue et un autre
        /// texte d'e-mail.
        /// Sort en silence si le compte n'existe pas, comme DemanderReinitialisation.
        /// </summary>
        public void DemanderActivation(string email)
        {
            var employe = _db.Employes.FirstOrDefault(e => e.Email == email);

            if (employe == null) return;

            var code = PoserNouveauCode(employe, TimeSpan.FromDays(JoursValiditeActivation));

            _email.EnvoyerCodeActivation(employe.Email, code, JoursValiditeActivation);
        }

        /// <summary>
        /// Génère un code, le stocke HACHÉ avec son expiration, remet le compteur d'essais
        /// à zéro, et renvoie le code en clair à l'appelant pour qu'il l'envoie. Une nouvelle
        /// demande écrase le code précédent.
        /// </summary>
        private string PoserNouveauCode(Employe employe, TimeSpan validite)
        {
            var code = GenererCode();

            employe.CodeReinitialisation = _hasher.HashPassword(employe, code);
            employe.ExpirationCode = DateTime.UtcNow.Add(validite);
            employe.TentativesCode = 0;
            _db.SaveChanges();

            return code;
        }

        /// <summary>
        /// Vérifie le code OTP puis remplace le mot de passe d'un compte existant.
        /// </summary>
        public ApiMessage Reinitialiser(string email, string code, string nouveauMotDePasse) =>
            PoserMotDePasse(email, code, nouveauMotDePasse,
                "Mot de passe réinitialisé. Vous pouvez vous connecter.");

        /// <summary>
        /// Active un compte : même vérification que la réinitialisation, puisque c'est le
        /// même code et la même preuve de possession de l'adresse. Seul le message de succès
        /// diffère — l'endpoint existe pour que le front sache afficher « Bienvenue, choisissez
        /// votre mot de passe » plutôt que « Réinitialisation ».
        /// </summary>
        public ApiMessage Activer(string email, string code, string motDePasse) =>
            PoserMotDePasse(email, code, motDePasse,
                "Compte activé. Vous pouvez vous connecter.");

        /// <summary>
        /// Logique commune à la réinitialisation et à l'activation : vérifier le code, puis
        /// poser le mot de passe. Refuse si aucun code n'est en cours, s'il est expiré, si le
        /// nombre d'essais est dépassé ou si le code est faux. En cas de succès le code est
        /// effacé (usage unique).
        /// </summary>
        private ApiMessage PoserMotDePasse(string email, string code, string nouveauMotDePasse,
            string messageSucces)
        {
            if (string.IsNullOrWhiteSpace(nouveauMotDePasse))
                return ApiMessage.Erreur("Le nouveau mot de passe est obligatoire.");

            var employe = _db.Employes.FirstOrDefault(e => e.Email == email);

            // Message volontairement identique dans tous les cas d'échec liés au code :
            // il ne doit pas permettre de deviner si l'e-mail existe.
            const string echec = "Code invalide ou expiré.";

            if (employe == null) return ApiMessage.Erreur(echec);
            if (string.IsNullOrEmpty(employe.CodeReinitialisation)) return ApiMessage.Erreur(echec);

            if (employe.ExpirationCode == null || employe.ExpirationCode < DateTime.UtcNow)
            {
                EffacerCode(employe);
                _db.SaveChanges();
                return ApiMessage.Erreur(echec);
            }

            if (employe.TentativesCode >= TentativesMax)
            {
                EffacerCode(employe);
                _db.SaveChanges();
                return ApiMessage.Erreur("Trop de tentatives. Demande un nouveau code.");
            }

            var resultat = _hasher.VerifyHashedPassword(employe, employe.CodeReinitialisation, code ?? string.Empty);
            if (resultat == PasswordVerificationResult.Failed)
            {
                employe.TentativesCode++;
                _db.SaveChanges();
                return ApiMessage.Erreur(echec);
            }

            employe.MotDePasse = _hasher.HashPassword(employe, nouveauMotDePasse);
            EffacerCode(employe);
            _db.SaveChanges();

            return ApiMessage.Ok(messageSucces);
        }

        /// <summary>Code numérique à 6 chiffres tiré d'un générateur cryptographique (pas de Random).</summary>
        private static string GenererCode()
        {
            return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        }

        /// <summary>Invalide le code en cours (après usage, expiration ou trop d'essais).</summary>
        private static void EffacerCode(Employe employe)
        {
            employe.CodeReinitialisation = null;
            employe.ExpirationCode = null;
            employe.TentativesCode = 0;
        }

        /// <summary>
        /// Construit et signe le JWT (claims : id, email, rôle), et renvoie avec lui
        /// l'identité de l'utilisateur : le front en a besoin et n'a pas à décoder le token.
        /// </summary>
        private LoginResponse GenererToken(Employe personne, string role)
        {
            var cle = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var identifiants = new SigningCredentials(cle, SecurityAlgorithms.HmacSha256);

            var minutes = int.TryParse(_config["Jwt:ExpireMinutes"], out var m) ? m : 120;
            var expiration = DateTime.UtcNow.AddMinutes(minutes);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, personne.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, personne.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, personne.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expiration,
                signingCredentials: identifiants);

            var tokenTexte = new JwtSecurityTokenHandler().WriteToken(token);
            return new LoginResponse(tokenTexte, expiration,
                personne.Id, personne.Nom, personne.Email, role);
        }
    }
}
