using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
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
        private readonly AppDbContext _db;
        private readonly IPasswordHasher<Personne> _hasher;
        private readonly IConfiguration _config;

        /// <summary>Reçoit le DbContext, le hacheur de mot de passe et la configuration (JWT) par injection.</summary>
        public AuthService(AppDbContext db, IPasswordHasher<Personne> hasher, IConfiguration config)
        {
            _db = db;
            _hasher = hasher;
            _config = config;
        }

        /// <summary>
        /// Vérifie l'email + le mot de passe. La personne doit exister, être un Recruteur
        /// ou un Manager, et le mot de passe doit correspondre au hash stocké.
        /// Renvoie un JWT en cas de succès, sinon null.
        /// </summary>
        public LoginResponse Login(string email, string motDePasse)
        {
            var personne = _db.Personnes.FirstOrDefault(p => p.Email == email);

            // Compte inexistant, ou candidat (pas de compte), ou mot de passe non défini.
            if (personne == null) return null;
            if (!(personne is Recruteur || personne is Manager)) return null;
            if (string.IsNullOrEmpty(personne.MotDePasse)) return null;

            // Vérifie le mot de passe fourni contre le hash stocké.
            var resultat = _hasher.VerifyHashedPassword(personne, personne.MotDePasse, motDePasse);
            if (resultat == PasswordVerificationResult.Failed) return null;

            var role = personne.GetType().Name; // "Recruteur" ou "Manager"
            return GenererToken(personne, role);
        }

        /// <summary>Construit et signe le JWT (claims : id, email, rôle).</summary>
        private LoginResponse GenererToken(Personne personne, string role)
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
            return new LoginResponse(tokenTexte, expiration, role);
        }
    }
}
