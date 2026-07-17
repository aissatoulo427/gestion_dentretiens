# Conception — Architecture API mono-projet (Gestion des entretiens)

**Date :** 2026-07-17
**Auteur :** Aïssatou Lo
**Statut :** Validé (prêt pour le plan d'implémentation)

---

## 1. Contexte et objectif

Le projet expose une **API ASP.NET Core Web API** (.NET 10, EF Core 10, PostgreSQL)
pour la planification d'entretiens de recrutement, en remplacement des interfaces
WinForms initiales.

L'architecture actuelle est correcte mais contient des couches que l'étudiante
**n'a pas apprises en cours** (Repository générique, Unit of Work). Le vrai objectif
n'est pas d'avoir l'architecture la plus sophistiquée, mais **une architecture que
l'on comprend et que l'on peut expliquer/défendre devant un professeur**.

Compétences maîtrisées et défendables :
- **Classes C# / POO** (héritage, enums) — terrain solide.
- **Spring Boot en Java** (Controller → Service → Repository → Entity + DTO).
- **JPA** → donc EF Core est compris (c'est le même concept : un ORM).
- **LINQ** → donc les requêtes dans les services sont explicables.

**Critère de succès :** chaque fichier de l'architecture correspond à un concept que
l'étudiante connaît déjà et peut justifier par analogie avec Java/Spring Boot.

## 2. Principe directeur

> Simplicité défendable > sophistication. On retire tout ce qui ne peut pas être
> expliqué, on garde une vraie séparation des responsabilités.

Argument clé à retenir : **le `DbContext` d'EF Core est déjà un Repository + Unit of
Work intégré.** On injecte donc le `DbContext` directement dans les services, sans
couche Repository maison.

## 3. Architecture cible : projet unique à dossiers

On **fusionne** les 3 projets actuels (`Domain`, `Infrastructure`, `Api`) en **un
seul projet** `GestionEntretiens.Api`, organisé en dossiers — exactement comme un
module Spring Boot unique avec ses packages.

```
GestionEntretiens.Api/
├── Models/                  → classes + enums          (= package @Entity)
│   ├── Personne.cs, Candidat.cs, Recruteur.cs, Manager.cs
│   ├── DemandeEntretien.cs, Creneau.cs, Entretien.cs, Feedback.cs
│   └── Enums/               → StatutDemande, StatutEntretien, TypeEntretien,
│                              Modalite, Decision, TypeNotification
├── Data/
│   ├── AppDbContext.cs      → le « JPA du .NET »        (= EntityManager)
│   └── Migrations/
├── Services/                → logique métier + LINQ     (= package @Service)
│   ├── IPersonneService.cs / PersonneService.cs
│   ├── IPlanificationService.cs / PlanificationService.cs
│   ├── IFeedbackService.cs / FeedbackService.cs
│   ├── IEmailService.cs / SmtpEmailService.cs
│   ├── Dtos/               → les DTOs                   (= package dto)
│   └── Mapping/            → conversion entité ↔ DTO
├── Controllers/            → endpoints REST             (= package @RestController)
│   ├── PersonnesController.cs, DemandesController.cs, CreneauxController.cs
│   ├── EntretiensController.cs, FeedbacksController.cs
├── Program.cs             → configuration + injection de dépendances (= @Configuration)
├── AppDbContextFactory.cs → factory design-time pour les outils EF
├── appsettings.json       → connexion PostgreSQL + paramètres SMTP
└── GestionEntretiens.Api.csproj
```

**Correspondance avec Spring Boot (à réciter au prof) :**

| Couche .NET | Équivalent Spring Boot | Justification |
|---|---|---|
| `Models` | `@Entity` (JPA) | mes entités mappées en base |
| `Data/AppDbContext` | `EntityManager` / couche JPA | l'ORM du .NET |
| `Services` | `@Service` | la logique métier |
| `Services/Dtos` + `Mapping` | dto + mapper | ne pas exposer les entités directement |
| `Controllers` | `@RestController` | expose les endpoints REST |
| `Program.cs` (DI) | `@Configuration` / `@Autowired` | branche les dépendances |

**Règle des dépendances** (réponse à « pourquoi cette organisation ? ») : dans un
projet unique, les dossiers respectent le sens `Models ← Data ← Services ←
Controllers`. Un controller appelle un service ; un service utilise le `DbContext` ;
le `DbContext` connaît les `Models`. Jamais l'inverse.

## 4. Ce qu'on retire

Ces fichiers ne correspondent à rien d'appris et ne sont pas défendables :

| Fichier supprimé | Pourquoi |
|---|---|
| `Repositories/IRepository.cs` | Repository **générique** ≠ Spring Data (une interface par entité) |
| `Repositories/Ef/EfRepository.cs` | implémentation du générique, non apprise |
| `Repositories/IUnitOfWork.cs` | pattern Unit of Work non appris |
| `Repositories/Ef/UnitOfWork.cs` | idem |

## 5. Flux d'une requête (avec LINQ)

Exemple : `GET /api/personnes/candidats`

```
PersonnesController        reçoit la requête HTTP
      │ appelle
PersonneService            _db.Candidats.ToList()        ← LINQ, explicable
      │ utilise (injecté)
AppDbContext (EF Core)     traduit le LINQ en SQL
      │
PostgreSQL                 renvoie les lignes
      │
entités → DTO (Mapping) → JSON renvoyé au client
```

Chaque service reçoit son `AppDbContext` par **injection de dépendances**, configurée
dans `Program.cs` (l'équivalent de `@Autowired`).

## 6. Refactorisation des services (avant → après)

Les services passent de `IUnitOfWork` à `AppDbContext` injecté directement, avec LINQ.

**Avant** (non défendable) :
```csharp
public PersonneService(IUnitOfWork uow) { _uow = uow; }

public Candidat CreerCandidat(...) {
    var candidat = new Candidat { ... };
    _uow.Candidats.Add(candidat);
    _uow.Complete();
    return candidat;
}
public IEnumerable<Candidat> GetCandidats() => _uow.Candidats.GetAll();
```

**Après** (défendable, LINQ + EF Core) :
```csharp
public PersonneService(AppDbContext db) { _db = db; }

public Candidat CreerCandidat(...) {
    var candidat = new Candidat { ... };
    _db.Candidats.Add(candidat);
    _db.SaveChanges();
    return candidat;
}
public IEnumerable<Candidat> GetCandidats() => _db.Candidats.ToList();
public Personne GetPersonne(int id) => _db.Personnes.Find(id);
```

Correspondance des appels :

| Ancien (UnitOfWork) | Nouveau (DbContext) |
|---|---|
| `_uow.Candidats.Add(x)` | `_db.Candidats.Add(x)` |
| `_uow.Candidats.GetAll()` | `_db.Candidats.ToList()` |
| `_uow.Personnes.GetById(id)` | `_db.Personnes.Find(id)` |
| `_uow.Complete()` | `_db.SaveChanges()` |

> Note : `_db.Candidats` fonctionne car les sous-types de `Personne` sont mappés en
> TPH ; EF Core expose `Set<Candidat>()`. Si un `DbSet<Candidat>` explicite est requis,
> on l'ajoutera au `AppDbContext`. À vérifier au moment de l'implémentation.

## 7. Adaptation de `Program.cs`

- **Supprimer** l'enregistrement `AddScoped<IUnitOfWork, UnitOfWork>()`.
- **Conserver** `AddDbContext<AppDbContext>(...UseNpgsql...)` : les services le reçoivent
  désormais directement.
- **Conserver** l'enregistrement des services métier, la config SMTP, CORS, Swagger,
  les enums JSON en texte, et `db.Database.Migrate()` au démarrage.
- Mettre à jour les `using` (plus de `Gestion_dentretiens.Repositories*`).

## 8. Namespaces

Le projet unifié adopte un namespace racine unique (proposé : `GestionEntretiens`,
avec sous-namespaces `.Models`, `.Models.Enums`, `.Data`, `.Services`,
`.Services.Dtos`, `.Services.Mapping`, `.Controllers`). Choix définitif à confirmer
au moment du plan (option de repli : conserver les namespaces `Gestion_dentretiens.*`
existants pour minimiser les modifications).

## 9. Migrations EF Core

- Les migrations existantes sont **déplacées** dans le projet unique (dossier
  `Data/Migrations/`) — le schéma de base **ne change pas**, on ne fait que déplacer du
  code entre projets.
- `AppDbContextFactory` (IDesignTimeDbContextFactory) est conservée pour que les outils
  `dotnet-ef` fonctionnent sans exécuter `Program.cs`.
- Commandes EF adaptées au projet unique :
  `dotnet ef migrations add <Nom> --project src/GestionEntretiens.Api`

## 10. Vérification (definition of done)

1. `dotnet build` → **0 erreur**.
2. Plus aucun fichier `Repositories/` ni référence à `IUnitOfWork` / `IRepository`.
3. Un seul projet dans la solution (les projets `Domain` et `Infrastructure` retirés).
4. Le lancement applique les migrations et démarre l'API (avec mot de passe PostgreSQL
   + SMTP renseignés dans `appsettings.json`).
5. Test d'un endpoint (ex. `GET /api/personnes/candidats`) via Swagger → réponse 200.

## 11. Hors périmètre (YAGNI)

- Pas de changement du **modèle de données** ni du schéma SQL.
- Pas d'ajout de fonctionnalités métier (authentification, pagination, etc.).
- Pas de tests automatisés dans ce lot (l'objectif est la restructuration).
- Pas de réintroduction d'une couche Repository sous une autre forme.
```