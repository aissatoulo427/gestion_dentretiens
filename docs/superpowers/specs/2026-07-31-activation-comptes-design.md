# Comptes : rôle Admin, création par invitation, activation par e-mail

**Date :** 2026-07-31
**Statut :** design validé, non implémenté.
**Prérequis :** la refonte des rôles (`2026-07-31-refonte-roles-design.md`) est en place.

## Problème

Les trois endpoints de création de compte employé sont en `[AllowAnonymous]` :

```
POST /api/personnes/rh                       [public]
POST /api/personnes/evaluateurs-techniques   [public]
POST /api/personnes/managers                 [public]
```

N'importe qui, sans compte, peut donc se créer un compte RH avec le mot de passe de son
choix, se connecter, et accéder à tout — dont les demandes et les créneaux dont l'accès vient
d'être verrouillé. Ce verrouillage perd l'essentiel de sa valeur tant que la porte d'entrée
reste ouverte : contrôler qui agit ne sert à rien si n'importe qui peut se fabriquer une
identité valide.

Deux défauts distincts se cachent derrière cet `[AllowAnonymous]` :

1. **L'amorçage.** Il faut bien créer le premier compte sur une base vide. La solution
   actuelle ouvre la porte en permanence au lieu de l'ouvrir une fois.
2. **Le mot de passe choisi par un tiers.** Même une fois l'accès restreint, laisser
   quelqu'un fixer le mot de passe d'un autre est une mauvaise pratique : le créateur connaît
   le secret, et celui-ci transite par un canal non maîtrisé.

## Décisions

| Question | Décision |
|---|---|
| Amorçage | un compte `Admin` créé au démarrage de l'application |
| Mot de passe de l'admin | clés de configuration `Admin:Email` et `Admin:MotDePasse`, obligatoires quand aucun admin n'existe |
| Qui crée les comptes employés | l'admin seul |
| Qui crée les candidats | le RH — un candidat n'est pas un compte |
| Mot de passe des comptes créés | aucun à la création ; l'employé le choisit lui-même |
| Preuve de possession de l'adresse | le mécanisme OTP existant, réutilisé tel quel |
| Endpoint d'activation | `POST /auth/activer`, alias de la logique de réinitialisation |
| Validité du code d'activation | 7 jours (la réinitialisation reste à 72 h) |
| Renvoi d'un code | aucun endpoint nouveau — `/auth/mot-de-passe-oublie` fait déjà le travail |

Conséquence directe : **plus aucun endpoint de création de compte n'est public.**

## Le rôle Admin

```
Employe (abstraite)
├── Admin                   ← nouveau : gère les comptes, ne recrute pas
├── RH
├── EvaluateurTechnique
└── Manager
```

L'admin gère les comptes et **ne participe à aucun recrutement** : ni demande, ni créneau, ni
panel, ni compte-rendu. C'est un périmètre réellement distinct des trois autres rôles — ce
n'est donc pas une classe vide ajoutée pour le nom.

### Admin hérite d'Employe : deux conséquences à traiter

`Admin` dérive d'`Employe` parce que c'est là que vivent le mot de passe et les champs OTP.
Cela entraîne deux effets qu'il faut neutraliser explicitement, sans quoi ils passeraient
inaperçus.

**L'admin hérite de `Creneaux` et `Entretiens`.** Il ne s'en sert jamais. C'est une impureté
assumée du modèle : la seule alternative serait de remonter le mot de passe et l'OTP sur
`Personne`, ce qui les donnerait aussi au `Candidat`, qui n'a pas de compte. Le moindre mal
est du côté de l'admin.

**L'admin deviendrait éligible aux panels.** `PlanifierEntretien` valide les `evaluateurIds`
contre `_db.Employes`, qui contient désormais les admins. La règle de composition exige un
rôle précis, mais elle est en « au moins un, pas seulement » : rien n'empêcherait donc
d'ajouter un admin en second évaluateur, qui pourrait ensuite saisir un compte-rendu. Il faut
un refus explicite :

```csharp
if (evaluateurs.Any(e => e is Admin))
    throw new InvalidOperationException("Un administrateur ne peut pas évaluer un entretien.");
```

De même, `POST /creneaux` doit passer de « tout employé connecté » à
`[Authorize(Roles = "RH,EvaluateurTechnique,Manager")]` : un admin n'a pas de disponibilité à
déclarer, puisqu'il ne fait jamais passer d'entretien.

## Création de l'admin au démarrage

Dans `Program.cs`, juste après `db.Database.Migrate()` :

- si un `Admin` existe déjà, ne rien faire ;
- sinon, lire `Admin:Email` et `Admin:MotDePasse` dans la configuration et créer le compte,
  mot de passe haché comme tous les autres ;
- si l'une des deux clés manque **alors qu'aucun admin n'existe**, refuser le démarrage avec
  un message explicite nommant les clés attendues.

Deux points sur ce choix. La configuration n'est exigée **que** lorsque l'amorçage est
réellement nécessaire : une application déjà initialisée démarre sans elle. Et l'échec est
bruyant plutôt que silencieux — un démarrage réussi sans admin laisserait l'application
inutilisable sans que rien n'explique pourquoi.

Ces clés ne vont **pas** dans `appsettings.json` versionné : secrets utilisateur en
développement, variables d'environnement en déploiement. `appsettings.json` peut porter les
clés vides, à titre de documentation.

## Le parcours d'activation

```
Admin connecté ──POST /personnes/rh { nom, email }──►  compte créé SANS mot de passe
                                                                │
                                                code à 6 chiffres, valable 7 j
                                                                │
                                                        e-mail d'activation
                                                                │
Nouvel employé ──POST /auth/activer { email, code, motDePasse }──►  compte utilisable
```

Trois mécanismes déjà présents rendent ce parcours peu coûteux :

**L'état « compte inactif » existe gratuitement.** `AuthService.Login` renvoie `null` quand
`MotDePasse` est vide. Un compte sans mot de passe est donc déjà inutilisable. Aucun champ
`EstActif` à ajouter, aucun état supplémentaire à maintenir cohérent.

**Le code expiré n'est pas un cul-de-sac.** `DemanderReinitialisation` ne vérifie pas que le
compte possède un mot de passe : un employé dont le code d'activation a expiré passe par
`/auth/mot-de-passe-oublie` et en reçoit un neuf. Rien à écrire.

**Le durcissement est acquis.** Code haché en base, expiration, usage unique, plafond de 5
tentatives, message d'échec uniforme : l'activation hérite de tout, puisque c'est le même
mécanisme.

## Changements de code

| Fichier | Changement |
|---|---|
| `Models/Admin.cs` | nouvelle sous-classe d'`Employe` |
| `Data/AppDbContext.cs` | `DbSet<Admin> Admins`, `modelBuilder.Entity<Admin>()` |
| `Services/Dtos/PersonneDtos.cs` | les trois `Create*Request` perdent `MotDePasse` ; ajout de `AdminDto` |
| `IPersonneService` / `PersonneService` | créations sans mot de passe ; `bool ExisteUnAdmin()` ; `Admin CreerAdmin(nom, email, motDePasse)` réservée à l'amorçage |
| `IAuthService` / `AuthService` | `DemanderActivation(email)` — même code, validité 7 jours, e-mail d'activation. `Activer(email, code, motDePasse)` partage la logique de `Reinitialiser` via une méthode privée commune ; seul le message de succès diffère. Constante `JoursValiditeActivation = 7` |
| `IEmailService` / `SmtpEmailService` | `EnvoyerCodeActivation(destinataire, code, joursValidite)` |
| `Controllers/AuthController.cs` | `POST /auth/activer`, `[AllowAnonymous]` |
| `Controllers/PersonnesController.cs` | les trois créations de compte passent en `[Authorize(Roles = "Admin")]` ; appel à `DemanderActivation` après création |
| `Controllers/CreneauxController.cs` | `POST /creneaux` restreint aux trois rôles non-admin |
| `Services/PlanificationService.cs` | refus explicite d'un admin dans un panel |
| `Program.cs` | amorçage de l'admin après `Migrate()` |

Une migration EF est nécessaire : `Admin` ajoute une valeur au discriminateur. Aucun
changement de schéma au-delà (TPH), donc aucune donnée touchée.

## Limite assumée : création et envoi ne sont pas atomiques

Le contrôleur crée le compte, puis demande l'envoi du code. Si le SMTP échoue entre les deux,
le compte existe sans que personne n'ait reçu de code.

Ce n'est pas bloquant : l'admin voit le compte dans la liste, et l'employé récupère un code
par `/auth/mot-de-passe-oublie`. La situation est rattrapable sans intervention technique.

L'alternative — faire dépendre `PersonneService` d'`IAuthService` pour enchaîner les deux
dans une même méthode — couplerait deux services sans supprimer le risque, puisqu'un envoi de
mail ne se rejoue pas dans une transaction. On garde l'orchestration dans le contrôleur et on
documente le comportement.

L'admin, lui, échappe entièrement à cette dépendance : son mot de passe vient de la
configuration, aucun e-mail n'entre en jeu. C'est ce qui garantit qu'une panne SMTP ne peut
jamais enfermer personne dehors.

## Impact sur le contrat d'API

- `POST /api/personnes/rh`, `/evaluateurs-techniques`, `/managers` — corps réduit à
  `{ nom, email }`, réservés à l'`Admin`, `403` sinon. Ils ne sont plus publics.
- `POST /api/auth/activer` — nouveau, public. Corps `{ email, code, nouveauMotDePasse }`,
  identique à `/auth/reinitialiser`. `200` → `{ succes: true, message: "Compte activé…" }`,
  `400` → `{ succes: false, message: "Code invalide ou expiré." }`.
- `POST /api/auth/login` — `role` peut désormais valoir `"Admin"`.
- `GET /api/personnes/{id}` — `type` peut valoir `"Admin"`.
- `POST /api/creneaux` — `403` pour un admin.
- `POST /api/entretiens` — nouveau message `400` si un admin figure dans `evaluateurIds`.
- Endpoints publics : `login`, `mot-de-passe-oublie`, `reinitialiser`, `activer`. Quatre, et
  aucun ne crée de compte.

Côté écran, prévoir une page « Bienvenue, choisissez votre mot de passe » distincte de la page
de réinitialisation : c'est ce que l'alias `/auth/activer` permet de distinguer.

## Critères de réussite

1. Sur une base vide sans `Admin:MotDePasse` configuré, l'application refuse de démarrer avec
   un message nommant les clés attendues.
2. Les clés configurées, l'application démarre et crée un admin utilisable.
3. Un second démarrage ne crée pas de doublon.
4. `POST /personnes/rh` sans token renvoie `401`, et `403` avec un token non-admin.
5. L'admin crée un RH ; un e-mail d'activation part avec un code à 6 chiffres.
6. Ce RH ne peut pas se connecter tant qu'il n'a pas activé son compte.
7. `POST /auth/activer` avec le bon code pose le mot de passe et permet la connexion.
8. Le même code rejoué une seconde fois est refusé.
9. Un employé jamais activé obtient un code neuf via `/auth/mot-de-passe-oublie` et s'en sert
   pour activer son compte.
10. L'admin reçoit `403` sur `POST /creneaux`, et `400` s'il est placé dans `evaluateurIds`.
