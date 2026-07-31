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
        // Employe est abstraite : le DbSet sert à interroger « tous les comptes »
        // (RH + évaluateurs techniques + managers) sans tester les sous-types.
        public DbSet<Employe> Employes => Set<Employe>();
        public DbSet<Admin> Admins => Set<Admin>();
        public DbSet<RH> RHs => Set<RH>();
        public DbSet<EvaluateurTechnique> EvaluateursTechniques => Set<EvaluateurTechnique>();
        public DbSet<Manager> Managers => Set<Manager>();
        public DbSet<DemandeEntretien> Demandes => Set<DemandeEntretien>();
        public DbSet<Creneau> Creneaux => Set<Creneau>();
        public DbSet<Entretien> Entretiens => Set<Entretien>();
        public DbSet<Feedback> Feedbacks => Set<Feedback>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Sous-types de Personne (TPH) : Candidat d'un côté, Employe (abstraite)
            // et ses trois sous-types de l'autre. Tout reste dans la table Personnes.
            modelBuilder.Entity<Candidat>();
            modelBuilder.Entity<Employe>();
            modelBuilder.Entity<Admin>();
            modelBuilder.Entity<RH>();
            modelBuilder.Entity<EvaluateurTechnique>();
            modelBuilder.Entity<Manager>();

            // --- DemandeEntretien ---
            modelBuilder.Entity<DemandeEntretien>()
                .HasOne(d => d.RH).WithMany(r => r.Demandes)
                .HasForeignKey(d => d.RhId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DemandeEntretien>()
                .HasOne(d => d.Candidat).WithMany(c => c.Demandes)
                .HasForeignKey(d => d.CandidatId).OnDelete(DeleteBehavior.Restrict);

            // --- Creneau ---
            modelBuilder.Entity<Creneau>()
                .HasOne(c => c.Employe).WithMany(e => e.Creneaux)
                .HasForeignKey(c => c.EmployeId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Creneau>()
                .HasOne(c => c.DemandeEntretien).WithMany(d => d.Creneaux)
                .HasForeignKey(c => c.DemandeEntretienId).OnDelete(DeleteBehavior.Restrict);

            // --- Entretien ---
            // Une demande donne lieu à plusieurs tours d'entretien (1-n).
            modelBuilder.Entity<Entretien>()
                .HasOne(e => e.DemandeEntretien).WithMany(d => d.Entretiens)
                .HasForeignKey(e => e.DemandeEntretienId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Entretien>()
                .HasOne(e => e.Candidat).WithMany(c => c.Entretiens)
                .HasForeignKey(e => e.CandidatId).OnDelete(DeleteBehavior.Restrict);

            // Le panel : N-N entre l'entretien et les employés qui l'évaluent.
            // EF génère la table de jointure, il n'y a pas de classe d'association à écrire.
            modelBuilder.Entity<Entretien>()
                .HasMany(e => e.Evaluateurs).WithMany(p => p.Entretiens)
                .UsingEntity(j => j.ToTable("EntretienEvaluateurs"));

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
