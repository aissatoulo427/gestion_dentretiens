# Refonte des rôles : RH, EvaluateurTechnique, Manager

**Date :** 2026-07-31
**Statut :** design validé, découpé en deux étapes. Cette spec couvre l'**étape 1**.

## Problème

Le modèle a deux rôles d'employé, `Recruteur` et `Manager`, alors que les entretiens
connaissent trois types de tours : `RH`, `Technique`, `Managerial`. Le tour technique n'a
donc pas d'évaluateur attitré — il est aujourd'hui mené par un `Manager`, ce que la doc de
la classe assume, mais qui range un développeur senior sous une étiquette de responsable
hiérarchique.

Deux conséquences :

1. Aucune règle ne garantit que le panel d'un tour contient quelqu'un capable de le juger.
   On peut planifier un entretien technique avec le seul RH au panel.
2. Les créneaux appartiennent au seul `Recruteur` (`Creneau.RecruteurId`). Les personnes qui
   font réellement passer les entretiens n'ont aucun moyen de déclarer leurs disponibilités,
   alors que ce sont elles dont le temps est bloqué.

## Décisions

| Question | Décision |
|---|---|
| Qui ouvre la demande | le RH |
| Qui pose des créneaux | tous les employés |
| Acceptation par les évaluateurs | aucune — poser un créneau vaut acceptation |
| Distinction entre les rôles d'évaluateur | le type de tour qu'ils permettent d'ouvrir |
| Nom de l'ex-`Recruteur` | `RH` |
| Nom du nouveau rôle | `EvaluateurTechnique` |
| Découpage | étape 1 = rôles ; étape 2 = disponibilités croisées |

`EvaluateurTechnique` a été préféré à `TechLead` : le tour technique peut être mené par un
développeur senior, un architecte ou un référent data, sans responsabilité hiérarchique. Le
nom décrit le rôle tenu dans le recrutement, pas le poste dans l'organigramme.

## Modèle de classes

```
Personne (abstraite)
├── Candidat                    ← pas de compte
└── Employe (abstraite)         ← compte + mot de passe + OTP
    ├── RH                      ← ex-Recruteur
    ├── EvaluateurTechnique     ← nouveau
    └── Manager                 ← inchangé
```

Deux propriétés changent de classe :

| Propriété | Avant | Après | Raison |
|---|---|---|---|
| `Creneaux` | `Recruteur` | `Employe` | tout le monde pose ses disponibilités |
| `Demandes` | `Recruteur` | `RH` | seul le RH ouvre un recrutement |

`EvaluateurTechnique` et `Manager` n'ont pas de propriété propre. Elles ne sont pas
interchangeables pour autant : la règle de composition du panel donne à chacune un pouvoir
que l'autre n'a pas.

Persistance en TPH — une seule table `Personnes`, une colonne `Discriminator`. Ajouter un
rôle n'ajoute pas de table.

## Droits par endpoint

| Endpoint | RH | EvaluateurTechnique | Manager |
|---|---|---|---|
| `POST /personnes/candidats` | ✅ | ❌ | ❌ |
| `POST /demandes` | ✅ | ❌ | ❌ |
| `POST /creneaux` | ✅ | ✅ | ✅ |
| `POST /creneaux/{id}/proposer` | ✅ | ❌ | ❌ |
| `POST /entretiens` | ✅ | ❌ | ❌ |
| `POST /feedbacks` | ✅¹ | ✅¹ | ✅¹ |

¹ à condition de siéger au panel de cet entretien. Règle existante, inchangée.

Mise en œuvre : `[Authorize(Roles = "RH")]` sur les quatre premières lignes. Le claim de rôle
provient de `employe.GetType().Name` et vaudra donc `"RH"`, `"EvaluateurTechnique"` ou
`"Manager"`.

`POST /personnes/candidats` passe de « tout employé connecté » à « RH seulement », puisque le
RH pilote l'administratif du recrutement.

## Règle de composition du panel

Dans `PlanificationService.PlanifierEntretien`, après le chargement des évaluateurs :

```csharp
bool panelValide = type switch
{
    TypeEntretien.RH         => evaluateurs.Any(e => e is RH),
    TypeEntretien.Technique  => evaluateurs.Any(e => e is EvaluateurTechnique),
    TypeEntretien.Managerial => evaluateurs.Any(e => e is Manager),
    _ => false
};
if (!panelValide)
    throw new InvalidOperationException(
        $"Un entretien {type} exige au moins un évaluateur du rôle correspondant.");
```

**« Au moins un », pas « seulement ».** Un tour technique peut réunir un évaluateur
technique, le manager et le RH. La règle impose une présence, elle n'exclut personne — ce qui
correspond à la pratique : le RH assiste souvent au tour technique sans juger la technique.

`is` plutôt que `GetType() == typeof(...)` : si les proxies de lazy loading EF Core étaient
activés un jour, `GetType()` renverrait `EvaluateurTechniqueProxy` et la règle rejetterait
tout le monde. `is` traverse le proxy.

La vérification existante « au moins un évaluateur » reste **avant** celle-ci : sur un panel
vide elle produit un message clair, là où la nouvelle règle en produirait un obscur.

## Migration

Une seule migration EF, quatre opérations :

| # | Opération | Effet sur les données |
|---|---|---|
| 1 | `Discriminator` : `'Recruteur'` → `'RH'` | les recruteurs existants deviennent des RH — exact, la doc de la classe disait déjà « Le RH » |
| 2 | `Creneaux.RecruteurId` → `EmployeId` | renommage de colonne, propriétaires conservés |
| 3 | `Demandes.RecruteurId` → `RhId` | idem |
| 4 | `EvaluateurTechnique` | aucun changement de schéma — nouvelle valeur du discriminateur |

Aucune perte de données attendue. Deux précautions :

**Forcer `RenameColumn`.** EF Core génère parfois `DropColumn` + `AddColumn` au lieu d'un
renommage, ce qui viderait la colonne. La migration doit appeler `migrationBuilder.RenameColumn`
explicitement, et le fichier généré doit être relu avant application.

**Les tokens en cours deviennent caducs.** Le claim de rôle passe de `"Recruteur"` à `"RH"` :
une session ouverte avant la migration reçoit un `403` sur les endpoints RH. Se reconnecter
suffit.

## Impact sur le contrat d'API

Changements cassants pour le front, à répercuter dans `docs/contrat-api-front.md`,
`README-FRONT.md` et la collection Postman :

- `POST /auth/login` — le champ `role` vaut `"RH"` au lieu de `"Recruteur"`, et peut valoir
  `"EvaluateurTechnique"`.
- `DemandeDto` — `recruteurId` devient `rhId`.
- `CreneauDto` — `recruteurId` devient `employeId`.
- `POST /personnes/evaluateurs-techniques` — nouvel endpoint de création, sur le modèle de
  `POST /personnes/recruteurs`. `POST /personnes/recruteurs` devient
  `POST /personnes/rh`.

Le nouvel endpoint de création reprend le `[AllowAnonymous]` des deux endpoints de compte
existants, par cohérence. Cet `[AllowAnonymous]` est un problème connu — n'importe qui peut
se créer un compte d'employé sans être authentifié — mais le traiter dépasse cette refonte :
il touche l'amorçage du premier compte, question indépendante des rôles.

> **Suite donnée :** `2026-07-31-activation-comptes-design.md` referme ce trou. Un rôle
> `Admin` créé au démarrage devient le seul à pouvoir créer des comptes, et les employés
> choisissent eux-mêmes leur mot de passe via un code d'activation reçu par e-mail.
- `POST /entretiens` — nouveau message d'erreur `400` quand le panel ne contient pas le rôle
  exigé par le `typeEntretien`.

## Hors périmètre de l'étape 1

Le croisement des disponibilités et la réservation multi-créneaux font l'objet de l'étape 2.
`Entretien.CreneauId` reste une clé unique dans l'étape 1.

Rappel de la décision prise pour l'étape 2, pour mémoire : `Creneau.EntretienId` remplacera
`Entretien.CreneauId`, un entretien réservera le créneau de chaque membre du panel, et la
correspondance se fera sur une égalité stricte des `DateDebut`. Sans cela, vérifier les
disponibilités du panel ne servirait à rien : les créneaux non réservés resteraient
réservables par un autre entretien à la même heure.

## Critères de réussite de l'étape 1

1. La solution compile et l'application démarre, migration appliquée.
2. Un `EvaluateurTechnique` peut être créé, se connecter, poser un créneau et saisir un
   compte-rendu sur un entretien où il siège.
3. Un `Manager` reçoit un `403` sur `POST /demandes` et `POST /personnes/candidats`.
4. Planifier un entretien `Technique` sans `EvaluateurTechnique` au panel renvoie un `400`.
5. Les données existantes (recruteurs, créneaux, demandes) sont intactes après migration.
6. La collection Postman rejoue le scénario complet de bout en bout.
