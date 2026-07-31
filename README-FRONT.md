# API Gestion des entretiens — Guide pour l'équipe Front

> ⚠️ **Document supplanté par [`docs/contrat-api-front.md`](docs/contrat-api-front.md).**
> Les noms de rôles et d'endpoints ci-dessous ont été remis à jour, mais plusieurs autres
> passages datent d'avant les évolutions du modèle et sont **faux** : le corps d'une `400`
> n'est plus du texte brut mais `{ succes, message }` ; `EntretienDto` ne contient plus de
> `recruteurId` ; une demande donne lieu à **plusieurs** entretiens et non un seul ; les
> actions renvoient `200` et non `204`.
> En cas de désaccord entre les deux fichiers, **`docs/contrat-api-front.md` fait foi**.

Ce document décrit l'API REST à consommer depuis le front. Backend : **ASP.NET Core** (.NET 10) + PostgreSQL.

---

## 1. URL de base

| Environnement | URL de base |
|---|---|
| Local (HTTP) | `http://localhost:5062` |
| Local (HTTPS) | `https://localhost:7277` |

Tous les endpoints sont préfixés par `/api`. Exemple : `http://localhost:5062/api/personnes/candidats`.

**Documentation interactive (Swagger)** : http://localhost:5062/swagger
→ liste tous les endpoints et permet de les tester dans le navigateur.

**CORS** : l'API autorise **toutes les origines** (`AllowAnyOrigin`) en développement — vous pouvez appeler l'API depuis `localhost:xxxx` sans configuration.

---

## 1 bis. Authentification (JWT) 🔐

L'API est **protégée** : sauf exceptions ci-dessous, chaque requête doit envoyer un **token JWT** dans l'en-tête :

```
Authorization: Bearer <token>
```

**Endpoints publics (sans token) :**
- `POST /api/auth/login` — se connecter
- `POST /api/personnes/rh`, `POST /api/personnes/evaluateurs-techniques` et `POST /api/personnes/managers` — inscription d'un compte staff

**Tout le reste exige un token valide** (sinon `401 Unauthorized`).

### Se connecter
Seuls les employés — **RH**, **évaluateurs techniques** et **managers** — ont un compte.

`POST /api/auth/login`
```json
{ "email": "recruteur.ndiaye@example.com", "motDePasse": "Secret123!" }
```
Réponse `200` :
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiration": "2026-07-17T20:33:50",
  "role": "RH"
}
```
- Identifiants invalides / compte inexistant → `401 Unauthorized`.
- Stockez le `token` et renvoyez-le dans l'en-tête `Authorization` de toutes les requêtes protégées.
- Le token expire (durée configurée côté backend) : à expiration, refaites un login.

**Exemple `fetch` :**
```js
const res = await fetch(`${baseUrl}/api/entretiens`, {
  headers: { "Authorization": `Bearer ${token}` }
});
```

---

## 2. Conventions générales

- **Format** : JSON en entrée et en sortie. Mettez l'en-tête `Content-Type: application/json` sur les POST avec body.
- **Nommage JSON** : les propriétés sont en **camelCase** (`dateHeure`, `rhId`…).
- **Les enums sont envoyés/reçus en TEXTE**, pas en nombre. Ex. `"typeEntretien": "RH"` (voir §4).
- **Dates** : format ISO 8601, ex. `"2026-07-20T09:00:00"`.
- **Identifiants** : entiers auto-incrémentés (`id`).

### Codes de réponse
| Code | Signification |
|---|---|
| `200 OK` | Succès (lecture, ou action avec corps retourné) |
| `201 Created` | Ressource créée (retourne l'objet créé) |
| `204 No Content` | Action réussie sans corps (confirmer, annuler, rappel…) |
| `400 Bad Request` | Erreur métier — le **corps est un message texte** (ex. `"Recruteur introuvable."`) |
| `401 Unauthorized` | Token JWT absent, invalide ou expiré (voir §1 bis) |
| `404 Not Found` | Ressource inexistante |

> ⚠️ Sur une `400`, le corps est une **chaîne de caractères** (le message d'erreur), pas un objet JSON.

---

## 3. Endpoints

### 3.1 Personnes — `/api/personnes`

| Méthode | Chemin | Description |
|---|---|---|
| `GET` | `/api/personnes/candidats` | Liste des candidats |
| `POST` | `/api/personnes/candidats` | Créer un candidat |
| `GET` | `/api/personnes/rh` | Liste des RH |
| `GET` | `/api/personnes/evaluateurs-techniques` | Liste des évaluateurs techniques |
| `POST` | `/api/personnes/rh` | Créer un RH |
| `POST` | `/api/personnes/evaluateurs-techniques` | Créer un évaluateur technique |
| `GET` | `/api/personnes/managers` | Liste des managers |
| `POST` | `/api/personnes/managers` | Créer un manager |
| `GET` | `/api/personnes/{id}` | Lire une personne (n'importe quel type) |

**POST candidat** — body :
```json
{ "nom": "Diop", "prenom": "Awa", "email": "awa.diop@example.com", "telephone": "770000000" }
```
Réponse `201` :
```json
{ "id": 1, "nom": "Diop", "prenom": "Awa", "email": "awa.diop@example.com", "telephone": "770000000" }
```

**POST RH / évaluateur technique / manager** (inscription, **endpoint public**) — body :
```json
{ "nom": "Ndiaye", "email": "recruteur.ndiaye@example.com", "motDePasse": "Secret123!" }
```
Réponse `201` : `{ "id": 1, "nom": "Ndiaye", "email": "recruteur.ndiaye@example.com" }`
(le mot de passe n'est **jamais** renvoyé ; il est stocké haché.)

**GET `/api/personnes/{id}`** — réponse :
```json
{ "id": 1, "nom": "Diop", "email": "awa.diop@example.com", "type": "Candidat" }
```
(`type` = `Candidat` | `RH` | `EvaluateurTechnique` | `Manager`)

---

### 3.2 Demandes — `/api/demandes`

| Méthode | Chemin | Description |
|---|---|---|
| `GET` | `/api/demandes` | Liste de toutes les demandes |
| `POST` | `/api/demandes` | Créer une demande d'entretien |
| `GET` | `/api/demandes/{id}` | Lire une demande |
| `GET` | `/api/demandes/{id}/creneaux-disponibles` | Créneaux disponibles proposés pour la demande |
| `POST` | `/api/demandes/{id}/annuler` | Annuler la demande |

**POST** — body :
```json
{ "candidatId": 1, "poste": "Développeur .NET" }
```
Réponse `201` (DemandeDto) :
```json
{
  "id": 1, "poste": "Développeur .NET", "typeEntretien": "RH",
  "dateCreation": "2026-07-17T10:00:00", "statut": "Creee",
  "rhId": 1, "candidatId": 1
}
```

---

### 3.3 Créneaux — `/api/creneaux`

| Méthode | Chemin | Description |
|---|---|---|
| `GET` | `/api/creneaux` | Liste de tous les créneaux |
| `GET` | `/api/creneaux/{id}` | Lire un créneau |
| `POST` | `/api/creneaux` | Un employé définit une disponibilité (nouveau créneau) |
| `POST` | `/api/creneaux/{id}/proposer?demandeId={demandeId}` | Rattacher le créneau à une demande |

**POST** — body :
```json
{ "dateDebut": "2026-07-20T09:00:00", "dateFin": "2026-07-20T10:00:00" }
```
`employeId` n'est pas dans la requête : le créneau est celui de l'employé connecté (lu dans le token). Tous les rôles peuvent en poser.
Réponse `200` (CreneauDto) :
```json
{
  "id": 1, "dateDebut": "2026-07-20T09:00:00", "dateFin": "2026-07-20T10:00:00",
  "disponible": true, "employeId": 1, "demandeEntretienId": null
}
```

**Proposer** : `POST /api/creneaux/1/proposer?demandeId=1` (pas de body) → `204`.

---

### 3.4 Entretiens — `/api/entretiens`

| Méthode | Chemin | Description |
|---|---|---|
| `GET` | `/api/entretiens` | Liste de tous les entretiens |
| `GET` | `/api/entretiens/{id}` | Lire un entretien |
| `POST` | `/api/entretiens` | Planifier un entretien (envoie l'invitation) |
| `POST` | `/api/entretiens/{id}/confirmer` | Confirmer l'entretien |
| `POST` | `/api/entretiens/{id}/reprogrammer` | Reprogrammer sur un autre créneau |
| `POST` | `/api/entretiens/{id}/rappel` | Envoyer un rappel au candidat |

**POST (planifier)** — body :
```json
{
  "demandeId": 1, "creneauId": 1,
  "modalite": "Presentiel", "lieuOuLien": "Salle A",
  "typeEntretien": "Technique", "evaluateurIds": [1]
}
```
`dateHeure` n'est pas dans la requête : l'horaire est celui du `dateDebut` du créneau.
Réponse `201` (EntretienDto) :
```json
{
  "id": 1, "dateHeure": "2026-07-20T09:00:00", "lieuOuLien": "Salle A",
  "statut": "Planifie", "modalite": "Presentiel",
  "demandeEntretienId": 1, "candidatId": 1, "recruteurId": 1, "creneauId": 1
}
```

**POST (reprogrammer)** — body :
```json
{ "nouveauCreneauId": 2 }
```
→ `204`. **confirmer** et **rappel** : sans body → `204`.

> Règle métier : **un seul entretien par demande** (relation 1‑1). Re-planifier une demande déjà planifiée renvoie `400`.

---

### 3.5 Feedbacks — `/api/feedbacks`

| Méthode | Chemin | Description |
|---|---|---|
| `GET` | `/api/feedbacks?entretienId={id}` | Liste des feedbacks d'un entretien |
| `POST` | `/api/feedbacks` | Saisir un feedback |

**POST** — body :
```json
{
  "entretienId": 1, "note": 4,
  "commentaire": "Bon profil technique.", "decision": "Favorable"
}
```
Réponse `200` (FeedbackDto) :
```json
{
  "id": 1, "note": 4, "commentaire": "Bon profil technique.",
  "decision": "Favorable", "dateSaisie": "2026-07-17T11:00:00",
  "entretienId": 1, "auteurId": 1
}
```
> `note` doit être entre **0 et 5**. `auteurId` n'est pas dans la requête : l'auteur est
> l'utilisateur connecté, qui doit faire partie du panel de l'entretien (sinon `400`).

---

## 4. Valeurs des enums (à envoyer/afficher en texte)

| Enum | Valeurs possibles |
|---|---|
| `typeEntretien` | `RH`, `Technique`, `Managerial` |
| `modalite` | `Presentiel`, `Distanciel`, `Telephone` |
| `decision` | `Favorable`, `Defavorable`, `ARevoir` |
| `statut` (demande) | `Creee`, `Planifiee`, `Annulee`, `Terminee` |
| `statut` (entretien) | `Planifie`, `Confirme`, `Reprogramme`, `Termine`, `Annule` |

---

## 5. Scénario type (ordre d'appel)

```
1. POST /api/personnes/rh              → récupère rhId
2. POST /api/personnes/candidats       → récupère candidatId
3. POST /api/personnes/managers        → récupère managerId
4. POST /api/demandes                  → récupère demandeId  (le RH du token + candidatId)
5. POST /api/creneaux                  → récupère creneauId  (l'employé du token)
6. POST /api/creneaux/{creneauId}/proposer?demandeId={demandeId}
7. POST /api/entretiens                → récupère entretienId (utilise demandeId + creneauId)
8. POST /api/entretiens/{entretienId}/confirmer
9. POST /api/feedbacks                 (auteur = utilisateur du token, doit être du panel)
```

---

## 6. Tester rapidement

- **Collection Postman fournie** à la racine du dépôt :
  - `GestionEntretiens.postman_collection.json`
  - `GestionEntretiens.postman_environment.json`
  - Importez les deux dans Postman, sélectionnez l'environnement, puis lancez les requêtes dans l'ordre du §5 (les `id` sont capturés automatiquement).
- **Swagger** : http://localhost:5062/swagger

---

## 7. Démarrer le backend en local

Prérequis : **.NET 10 SDK** et **PostgreSQL** en marche.

```bash
dotnet run --project src/GestionEntretiens.Api
```

L'API crée automatiquement la base et les tables au démarrage (migrations EF Core). Elle écoute ensuite sur `http://localhost:5062`.

> Contact backend : voir l'équipe API pour les identifiants PostgreSQL et les paramètres SMTP si besoin.
