using Gestion_dentretiens.Models;
using Microsoft.EntityFrameworkCore;

namespace Gestion_dentretiens.Data
{
    /// <summary>
    /// Contexte EF Core. L'héritage Personne est mappé en Table-Per-Hierarchy
    /// (une seule table Personnes avec une colonne discriminante).
    /// </summary>
    public class AppDbContext : DbContext
    {
        static AppDbContext()
        {
            // PostgreSQL/Npgsql n'accepte que de l'UTC pour "timestamp with time zone".
            // L'application manipule des heures locales simples (entretiens, créneaux) sans fuseau :
            // ce réglage utilise "timestamp without time zone" et accepte DateTime.Now.
            // Placé dans le constructeur statique pour être actif partout : au démarrage de l'API
            // ET pour les outils EF (dotnet ef) qui construisent le modèle sans exécuter Program.cs.
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Personne> Personnes => Set<Personne>();
        // Sous-types de Personne (mappés en TPH dans la même table Personnes) :
        // exposés en DbSet pour que les services puissent écrire _db.Candidats, etc.
        public DbSet<Candidat> Candidats => Set<Candidat>();
        public DbSet<Recruteur> Recruteurs => Set<Recruteur>();
        public DbSet<Manager> Managers => Set<Manager>();
        public DbSet<DemandeEntretien> Demandes => Set<DemandeEntretien>();
        public DbSet<Creneau> Creneaux => Set<Creneau>();
        public DbSet<Entretien> Entretiens => Set<Entretien>();
        public DbSet<Feedback> Feedbacks => Set<Feedback>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Sous-types de Personne (TPH).
            modelBuilder.Entity<Candidat>();
            modelBuilder.Entity<Recruteur>();
            modelBuilder.Entity<Manager>();

            // --- DemandeEntretien ---
            modelBuilder.Entity<DemandeEntretien>()
                .HasOne(d => d.Recruteur).WithMany(r => r.Demandes)
                .HasForeignKey(d => d.RecruteurId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DemandeEntretien>()
                .HasOne(d => d.Candidat).WithMany(c => c.Demandes)
                .HasForeignKey(d => d.CandidatId).OnDelete(DeleteBehavior.Restrict);

            // --- Creneau ---
            modelBuilder.Entity<Creneau>()
                .HasOne(c => c.Recruteur).WithMany(r => r.Creneaux)
                .HasForeignKey(c => c.RecruteurId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Creneau>()
                .HasOne(c => c.DemandeEntretien).WithMany(d => d.Creneaux)
                .HasForeignKey(c => c.DemandeEntretienId).OnDelete(DeleteBehavior.Restrict);

            // --- Entretien ---
            modelBuilder.Entity<Entretien>()
                .HasOne(e => e.DemandeEntretien).WithMany()
                .HasForeignKey(e => e.DemandeEntretienId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Entretien>()
                .HasOne(e => e.Candidat).WithMany(c => c.Entretiens)
                .HasForeignKey(e => e.CandidatId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Entretien>()
                .HasOne(e => e.Recruteur).WithMany(r => r.Entretiens)
                .HasForeignKey(e => e.RecruteurId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Entretien>()
                .HasOne(e => e.Creneau).WithMany()
                .HasForeignKey(e => e.CreneauId).OnDelete(DeleteBehavior.Restrict);

            // --- Feedback ---
            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.Entretien).WithMany(e => e.Feedbacks)
                .HasForeignKey(f => f.EntretienId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.Auteur).WithMany()
                .HasForeignKey(f => f.AuteurId).OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);
        }
    }
}
