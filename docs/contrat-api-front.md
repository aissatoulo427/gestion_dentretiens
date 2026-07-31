# API Gestion d'entretiens — contrat front

**Version du 31/07/2026**

> ⚠️ **Deux changements majeurs sont en place.** Les rôles ont été refondus (`Recruteur`
> devient `RH`, `EvaluateurTechnique` et `Admin` apparaissent), et **l'inscription publique
> a disparu** : c'est l'admin qui crée les comptes, et leur titulaire choisit son mot de
> passe via un code d'activation reçu par e-mail. Si tu intégrais contre la version
> précédente, va d'abord au [Récapitulatif des changements](#récapitulatif-des-changements).

| | |
|---|---|
| URL de base (dev) | `http://localhost:5062/api` |
| HTTPS | `https://localhost:7277/api` |
| Swagger | `http://localhost:5062/swagger` |
| Content-Type | `application/json` |

---

## Authentification — règle générale

Tous les endpoints exigent un token JWT :

```
Authorization: Bearer <token>
```

**Sauf ces quatre-là, qui sont publics :**

- `POST /api/auth/login`
- `POST /api/auth/mot-de-passe-oublie`
- `POST /api/auth/reinitialiser`
- `POST /api/auth/activer`

**Aucun d'eux ne crée de compte.** L'inscription publique n'existe plus : seul un `Admin`
connecté crée des comptes.

Sans token valide : `401`. Le token dure 120 minutes.

---

## Formats

**Dates** : `"yyyy-MM-ddTHH:mm:ss"` — par exemple `"2026-08-12T14:00:00"`. Pas de fuseau
horaire, heure locale.

**Enums** : envoyés et reçus **en texte**, jamais en nombre.

| Enum | Valeurs |
|---|---|
| `StatutDemande` | `Creee` `Planifiee` `Annulee` `Terminee` |
| `StatutEntretien` | `Planifie` `Confirme` `Reprogramme` `Termine` `Annule` |
| `TypeEntretien` | `RH` `Technique` `Managerial` |
| `Modalite` | `Presentiel` `Distanciel` `Telephone` |
| `Decision` | `Favorable` `Defavorable` `ARevoir` |

### Format uniforme des messages

**Toute réponse qui ne renvoie pas de ressource a exactement la même forme**, quel que soit
l'endpoint et quel que soit le code HTTP :

```json
{ "succes": false, "message": "La note doit être comprise entre 0 et 5." }
```

Cela couvre les erreurs (`400`, `401`, `404`) **et** les accusés de réception (`200` sur une
annulation, une confirmation, un rappel…). Un seul gestionnaire côté front suffit :

```js
if (!res.ok) {
  const { message } = await res.json();   // toujours présent
  afficherErreur(message);
}
```

Les endpoints qui renvoient une **ressource** (demande, créneau, entretien, feedback, login)
renvoient l'objet directement, sans enveloppe.

---

## Les règles métier à connaître

**1. Une demande donne lieu à plusieurs tours d'entretien.**
Un candidat enchaîne plusieurs entretiens pour une même demande : `RH`, puis `Technique`,
puis `Managerial`. Le type est choisi **à chaque planification**, pas à la création de la
demande.

**2. Chaque entretien a un panel d'évaluateurs.**
Un entretien réunit 1 à n évaluateurs, et le panel change d'un tour à l'autre. Conséquence
directe : **seul un évaluateur présent à l'entretien peut en saisir le compte-rendu**. Un
employé absent du tour reçoit un `400`.

**3. Chaque type de tour exige un rôle précis au panel.**
Un tour `Technique` réclame au moins un `EvaluateurTechnique`, un `Managerial` au moins un
`Manager`, un `RH` au moins un `RH`. **« Au moins un », pas « seulement »** : d'autres rôles
peuvent s'ajouter au panel, la règle impose une présence et n'exclut personne. Sinon `400`.

**4. Quatre rôles.**

| | Créer des comptes | Candidats, demandes, planification | Poser ses créneaux | Évaluer |
|---|---|---|---|---|
| `Admin` | ✅ | ❌ | ❌ | ❌ |
| `RH` | ❌ | ✅ | ✅ | ✅ |
| `EvaluateurTechnique` | ❌ | ❌ | ✅ | ✅ |
| `Manager` | ❌ | ❌ | ✅ | ✅ |

L'admin gère les comptes et **rien d'autre** : il ne recrute pas, ne pose pas de créneau et
ne peut pas figurer dans un panel. Le RH pilote le recrutement de bout en bout. Les deux
derniers n'interviennent que comme évaluateurs — mais tous trois posent leurs disponibilités,
puisqu'un entretien bloque le temps de celui qui le fait passer.

**5. Un compte se crée sans mot de passe.** L'admin saisit seulement nom et e-mail ; le
titulaire reçoit un code et choisit lui-même son mot de passe via `/auth/activer`. Tant qu'il
ne l'a pas fait, le compte existe mais **ne peut pas se connecter** — le login renvoie `401`
comme pour un mot de passe faux.

---

## Authentification

### `POST /api/auth/login`

```json
{ "email": "recruteur@exemple.com", "motDePasse": "MonMotDePasse123!" }
```

`200` :

```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiration": "2026-07-30T12:34:56",
  "id": 36,
  "nom": "Lo",
  "email": "recruteur@exemple.com",
  "role": "RH"
}
```

`401` → `{ "succes": false, "message": "Email ou mot de passe invalide." }`

**`id` est l'identifiant de l'utilisateur connecté — à stocker avec le token.** C'est lui
qu'on renvoie ensuite dans `evaluateurIds` (planification), et qui sert à savoir si
l'utilisateur fait partie d'un panel. Inutile de décoder le JWT pour le récupérer.

> **Aucun endpoint d'écriture ne demande plus « qui es-tu ? »** dans le corps de la requête.
> `POST /api/demandes`, `POST /api/creneaux` et `POST /api/feedbacks` lisent l'auteur dans
> le token. Les champs `rhId`, `employeId` et `auteurId` restent présents **dans les
> réponses**.

`role` vaut `"Admin"`, `"RH"`, `"EvaluateurTechnique"` ou `"Manager"`, utilisable pour
l'affichage conditionnel. Voir le tableau des droits dans « Les règles métier à connaître ».

> Le `401` est volontairement identique que l'email soit inconnu ou le mot de passe faux.
> Ne pas afficher « cet email n'existe pas » : ça permettrait d'énumérer les comptes.

### `POST /api/auth/mot-de-passe-oublie`

```json
{ "email": "recruteur@exemple.com" }
```

- `200` **toujours** → `{ "succes": true, "message": "Si un compte existe pour cet e-mail, un code vient d'être envoyé." }`

Le code fait 6 chiffres, vaut 72 heures, ne sert qu'une fois. Rappeler cet endpoint écrase le
code précédent — c'est ce qu'il faut utiliser pour un bouton « Renvoyer le code ».

### `POST /api/auth/reinitialiser`

```json
{ "email": "recruteur@exemple.com", "code": "246621", "nouveauMotDePasse": "Nouveau123!" }
```

- `200` → `{ "succes": true, "message": "Mot de passe réinitialisé. Vous pouvez vous connecter." }`
- `400` → `{ "succes": false, "message": "Code invalide ou expiré." }`
- `400` → `{ "succes": false, "message": "Trop de tentatives. Demande un nouveau code." }`
- `400` → `{ "succes": false, "message": "Le nouveau mot de passe est obligatoire." }`

Le corps a la même forme en `200` et en `400` : lire `succes`, afficher `message`.
Après **5 codes faux**, le code est détruit — proposer alors « Renvoyer un code ».

L'`email` saisi doit être conservé côté front entre les deux étapes : il n'y a pas de token
intermédiaire.

### `POST /api/auth/activer`

Pour un compte qui vient d'être créé par l'admin et dont le titulaire n'a jamais eu de mot
de passe.

```json
{ "email": "nouveau@exemple.com", "code": "246621", "nouveauMotDePasse": "MonMotDePasse1!" }
```

- `200` → `{ "succes": true, "message": "Compte activé. Vous pouvez vous connecter." }`
- `400` → mêmes messages que `/auth/reinitialiser`

Corps et vérifications **identiques** à `/auth/reinitialiser` — c'est le même code et la même
preuve de possession de l'adresse. L'endpoint est distinct pour que tu puisses afficher un
écran de bienvenue (« Choisissez votre mot de passe ») plutôt qu'un écran de réinitialisation.

Le code d'activation vaut **7 jours**, contre 72 h pour une réinitialisation. Passé ce délai,
`/auth/mot-de-passe-oublie` en renvoie un neuf — il fonctionne aussi sur un compte jamais
activé, c'est le chemin de rattrapage à proposer.

---

## Personnes

### Créer les comptes

```
POST /api/personnes/rh                       [Admin seulement]
POST /api/personnes/evaluateurs-techniques   [Admin seulement]
POST /api/personnes/managers                 [Admin seulement]
```

```json
{ "nom": "Ndiaye", "email": "x@y.com" }
```

`201` → `{ "id": 32, "nom": "Ndiaye", "email": "x@y.com" }`

**Pas de `motDePasse` dans la requête.** Le compte est créé sans mot de passe et un code
d'activation part par e-mail. Son titulaire choisit son mot de passe via `/auth/activer`.
Tant qu'il ne l'a pas fait, le compte existe mais ne peut pas se connecter.

`401` sans token, `403` si le compte connecté n'est pas un `Admin`.

> **Il n'y a pas d'endpoint pour créer un admin.** Le premier est créé au démarrage de
> l'application à partir de sa configuration. C'est ce qui permet de partir d'une base vide
> sans laisser d'inscription ouverte à tous — et de ne dépendre d'aucun e-mail pour la toute
> première connexion.

```
POST /api/personnes/candidats                [RH seulement]
```

```json
{ "nom": "Diop", "prenom": "Awa", "email": "awa@x.com", "telephone": "770000000" }
```

`201` → `{ "id": 35, "nom": "Diop", "prenom": "Awa", "email": "awa@x.com", "telephone": "770000000" }`

### Corriger et supprimer un candidat

```
PUT    /api/personnes/candidats/{id}     [RH seulement]
DELETE /api/personnes/candidats/{id}     [RH seulement]
```

Le `PUT` prend le même corps que la création et renvoie `200` avec la fiche à jour. Les
quatre champs sont remplacés : renvoie aussi ceux que tu ne modifies pas.

Sers-t'en surtout pour une **adresse e-mail mal saisie** : sans correction possible, le
candidat ne reçoit jamais ses invitations ni ses rappels.

Le `DELETE` répond `200` `{ succes, message }`, et refuse en `400` si le candidat est déjà
engagé :

- `Ce candidat a des demandes d'entretien, il ne peut pas être supprimé.`
- `Ce candidat a des entretiens, il ne peut pas être supprimé.`

C'est volontaire : une fiche liée à un recrutement fait partie de l'historique. Le `DELETE`
ne sert qu'à effacer une fiche créée par erreur.

### Lire

| Endpoint | Réponse |
|---|---|
| `GET /api/personnes/rh` | `[ {id, nom, email}, … ]` |
| `GET /api/personnes/evaluateurs-techniques` | `[ {id, nom, email}, … ]` |
| `GET /api/personnes/managers` | `[ {id, nom, email}, … ]` |
| `GET /api/personnes/candidats` | `[ {id, nom, prenom, email, telephone}, … ]` |
| `GET /api/personnes/{id}` | `{ id, nom, email, type }` — `type` = `"Admin"` / `"RH"` / `"EvaluateurTechnique"` / `"Manager"` / `"Candidat"`, ou `404` |

Les trois listes d'employés alimentent le sélecteur d'évaluateurs de l'écran de
planification. Pense à afficher le rôle à côté du nom : sans lui, l'utilisateur ne peut pas
savoir quel évaluateur satisfait la règle de composition du panel.

---

## Demandes d'entretien

### `POST /api/demandes`

```json
{ "candidatId": 35, "poste": "Dev .NET" }
```

`201` :

```json
{
  "id": 8,
  "poste": "Dev .NET",
  "dateCreation": "2026-07-30T11:00:00",
  "statut": "Creee",
  "rhId": 32,
  "candidatId": 35
}
```

**Pas d'identifiant d'organisateur dans la requête.** C'est le **RH connecté**, identifié par
le token. Le champ s'appelle `rhId` dans la réponse — il s'appelait `recruteurId`.

`400` si le candidat est introuvable. `403` si le compte connecté n'est pas un RH.

> **Changement :** la demande ne porte **plus** de `typeEntretien`. Il est fixé à chaque
> planification.

### `PUT /api/demandes/{id}`

```json
{ "poste": "Développeur .NET senior" }
```

`200` → la demande à jour. `400` si le poste est vide ou la demande introuvable.
`403` si le compte connecté n'est pas un `RH`.

**Seul le poste est modifiable.** Le RH, le candidat et la date de création identifient la
demande — les changer en ferait une autre. Pour tout le reste : annuler et recréer.

### Autres

| Endpoint | Réponse |
|---|---|
| `GET /api/demandes` | `[ DemandeDto, … ]` |
| `GET /api/demandes/{id}` | `DemandeDto` ou `404` |
| `GET /api/demandes/{id}/creneaux-disponibles` | `[ CreneauDto, … ]` — uniquement les libres |
| `POST /api/demandes/{id}/annuler` | `200` `{ succes, message }` ou `400` — annule **tous** les tours de la demande et libère leurs créneaux |

Cycle du statut : `Creee` → `Planifiee` (à la première planification) → `Annulee` ou `Terminee`.

---

## Créneaux

### `POST /api/creneaux`

```json
{ "dateDebut": "2026-08-10T09:00:00", "dateFin": "2026-08-10T10:00:00" }
```

`201` :

```json
{ "id": 8, "dateDebut": "2026-08-10T09:00:00", "dateFin": "2026-08-10T10:00:00",
  "disponible": true, "employeId": 32, "demandeEntretienId": null }
```

**Pas d'identifiant de propriétaire dans la requête.** Le créneau appartient à l'**employé
connecté**, identifié par le token — c'était le moyen de créer des disponibilités au nom de
quelqu'un d'autre. Le champ s'appelle `employeId` dans la réponse : il s'appelait
`recruteurId`, et il ne désigne plus un recruteur mais n'importe quel employé.

**Les trois rôles qui font passer des entretiens peuvent poser des créneaux** — RH,
évaluateur technique et manager. L'admin en est écarté : il ne recrute pas.

`400` si `dateFin <= dateDebut`. `403` pour un admin.

### Autres

### `DELETE /api/creneaux/{id}`

Supprime une de ses **propres** disponibilités — erreur de saisie, ou indisponibilité.

- `200` → `{ "succes": true, "message": "Créneau supprimé." }`
- `404` → le créneau n'existe pas
- `403` → `Ce créneau ne vous appartient pas.` (seul son auteur peut le supprimer)
- `400` → `Ce créneau est réservé par un entretien. Utilisez la reprogrammation pour le libérer.`
- `400` → `Ce créneau reste rattaché à un entretien passé ou annulé, il ne peut pas être supprimé.`

**Il n'y a pas de `PUT` sur les créneaux.** Pour corriger un horaire : supprimer, puis
recréer. Un créneau ne porte que des dates, il n'y a rien à conserver.

Les deux `400` sont distincts et ne veulent pas dire la même chose. Le premier signifie
qu'un entretien s'y tient **actuellement** : proposer à l'utilisateur de passer par
`POST /entretiens/{id}/reprogrammer`. Le second arrive quand un entretien a été annulé — le
créneau redevient « disponible » mais reste lié à cet entretien, et ne peut plus être effacé.
Dans ce cas il n'y a rien à faire, mieux vaut le dire clairement que proposer une action
qui échouera.

### Autres

| Endpoint | Réponse |
|---|---|
| `POST /api/creneaux/{id}/proposer?demandeId=8` | `200` `{ succes, message }` — rattache le créneau à la demande |
| `GET /api/creneaux` | `[ CreneauDto, … ]` |
| `GET /api/creneaux/{id}` | `CreneauDto` ou `404` |

Un créneau est proposé par le RH. Il passe `"disponible": false` une fois réservé par un
entretien.

---

## Entretiens

**C'est ici que se concentrent les changements.**

### `POST /api/entretiens` — planifier un tour

```json
{
  "demandeId": 8,
  "creneauId": 9,
  "modalite": "Presentiel",
  "lieuOuLien": "Salle A",
  "typeEntretien": "Technique",
  "evaluateurIds": [32, 33]
}
```

`201` :

```json
{
  "id": 6,
  "dateHeure": "2026-08-12T14:00:00",
  "lieuOuLien": "Salle A",
  "statut": "Planifie",
  "modalite": "Presentiel",
  "typeEntretien": "Technique",
  "demandeEntretienId": 8,
  "candidatId": 35,
  "evaluateurIds": [32, 33],
  "creneauId": 9
}
```

`400` `{ "succes": false, "message": … }`, messages possibles :

- `Un entretien doit compter au moins un évaluateur.`
- `Un évaluateur est introuvable ou n'est pas un employé.`
- `Un entretien Technique exige au moins un évaluateur du rôle correspondant dans le panel.`
  (idem pour `RH` et `Managerial`)
- `Un administrateur ne peut pas évaluer un entretien.`
- `Le créneau n'est plus disponible.`
- `Demande introuvable.` / `Créneau introuvable.` / `Demande annulée.`

`403` si le compte connecté n'est pas un RH.

**Trois changements dans la requête :**

- `typeEntretien` — obligatoire, c'est le tour.
- `evaluateurIds` — obligatoire, tableau **non vide** d'ids de recruteurs et/ou managers.
  Un id de candidat est refusé.
- **`dateHeure` a disparu de la requête.** L'horaire de l'entretien est **déduit du créneau** :
  il vaut toujours le `dateDebut` de `creneauId`. L'envoyer devenait le moyen de créer un
  entretien daté un jeudi sur un créneau du mardi. Le front n'a donc plus rien à pré-remplir :
  il choisit un créneau, l'API en tire la date. `dateHeure` reste bien sûr **dans la réponse**.

**`recruteurId` a disparu de la réponse.** L'organisateur se lit sur la demande :
`GET /api/demandes/{id}` puis son champ `recruteurId`.

On appelle cet endpoint **plusieurs fois sur la même `demandeId`**, une fois par tour, avec
un créneau et un panel différents.

### Autres

| Endpoint | Réponse |
|---|---|
| `GET /api/entretiens` | `[ EntretienDto, … ]` |
| `GET /api/entretiens/{id}` | `EntretienDto` ou `404` |
| `POST /api/entretiens/{id}/confirmer` | `200` `{ succes, message }` — le candidat confirme sa présence |
| `POST /api/entretiens/{id}/reprogrammer` | `200` `{ succes, message }` — corps : `{ "nouveauCreneauId": 10 }`. `nouvelleDateHeure` a disparu, pour la même raison : la nouvelle date est celle du nouveau créneau |
| `POST /api/entretiens/{id}/rappel` | `200` `{ succes, message }` — renvoie l'e-mail de rappel au candidat |

Pour afficher le parcours d'un candidat : filtrer sur `demandeEntretienId` et trier par
`dateHeure`.

---

## Comptes-rendus (feedbacks)

### `POST /api/feedbacks`

```json
{
  "entretienId": 6,
  "note": 4,
  "commentaire": "Bon niveau technique.",
  "decision": "Favorable"
}
```

`200` :

```json
{ "id": 2, "note": 4, "commentaire": "Bon niveau technique.", "decision": "Favorable",
  "dateSaisie": "2026-07-30T11:04:20", "entretienId": 6, "auteurId": 33 }
```

`400` `{ "succes": false, "message": … }`, messages possibles :

- `Seul un évaluateur présent à l'entretien peut saisir un compte-rendu.`
- `La note doit être comprise entre 0 et 5.`
- `Entretien introuvable.`

**`auteurId` a disparu de la requête.** Le compte-rendu est signé par l'**utilisateur
connecté** : on ne dépose pas un avis au nom de quelqu'un d'autre. Il reste **dans la réponse**.

> **Règle :** l'utilisateur connecté doit figurer dans le tableau `evaluateurIds` de
> l'entretien visé. Le front devrait n'ouvrir le formulaire de compte-rendu qu'aux
> évaluateurs de cet entretien-là — sinon l'utilisateur se prend un `400` après avoir
> tout saisi. Comparer l'`id` reçu au login avec `evaluateurIds` suffit pour le savoir.

La note va de 0 à 5 inclus.

### `GET /api/feedbacks?entretienId=6`

`200` → `[ FeedbackDto, … ]`

Il y a **un feedback par évaluateur** : un entretien à trois personnes peut en avoir trois.
Les afficher tous, pas seulement le premier.

---

## Parcours complet à implémenter

| # | Étape | Appel |
|---|---|---|
| 1 | Login | `POST /auth/login` |
| 2 | Créer le candidat | `POST /personnes/candidats` |
| 3 | Créer la demande | `POST /demandes` |
| 4 | Définir des créneaux | `POST /creneaux` |
| 5 | Les proposer à la demande | `POST /creneaux/{id}/proposer?demandeId=` |
| 6 | Consulter les libres | `GET /demandes/{id}/creneaux-disponibles` |
| 7 | **Tour 1** : planifier | `POST /entretiens` — `typeEntretien: "RH"`, `evaluateurIds: [idRH]` |
| 8 | Le candidat confirme | `POST /entretiens/{id}/confirmer` |
| 9 | Compte-rendu du tour 1 | `POST /feedbacks` — auteur = `idRH` |
| 10 | **Tour 2** : planifier | `POST /entretiens` — **même `demandeId`**, `typeEntretien: "Technique"`, `evaluateurIds: [idRH, idManager]` |
| 11 | Comptes-rendus du tour 2 | `POST /feedbacks` — un par évaluateur |
| 12 | Synthèse | `GET /feedbacks?entretienId=` pour chaque tour |

---

## Récapitulatif des changements

*Pour qui avait déjà commencé l'intégration.*

**L'inscription publique a disparu.** Les trois endpoints de création de compte étaient
accessibles sans token : n'importe qui pouvait se créer un compte RH et accéder à tout.
Désormais seul un `Admin` crée des comptes, et le titulaire choisit lui-même son mot de passe.

| Endroit | Avant | Après |
|---|---|---|
| Créer un compte employé | public, `{ nom, email, motDePasse }` | `Admin` seulement, `{ nom, email }` |
| Choix du mot de passe | par le créateur du compte | par le titulaire, via `POST /auth/activer` |
| Premier compte | inscription publique | `Admin` créé au démarrage depuis la configuration |
| Endpoints publics | 6, dont 3 créaient des comptes | 4, aucun ne crée de compte |

`POST /auth/activer` est nouveau. Corps et erreurs identiques à `/auth/reinitialiser`, code
valable 7 jours. Prévoir un écran de bienvenue distinct de la réinitialisation.

Un compte non activé se connecte avec un `401`, indiscernable d'un mot de passe faux. Si un
utilisateur dit ne pas pouvoir se connecter, le chemin de rattrapage est
`/auth/mot-de-passe-oublie` : il fonctionne aussi sur un compte jamais activé.

**Quatre rôles au lieu de deux.** `Recruteur` est renommé `RH` ; `EvaluateurTechnique`
apparaît — le tour technique est mené par un développeur senior ou un architecte, qui n'est
ni RH ni responsable hiérarchique — et `Admin` gère les comptes sans participer au
recrutement.

| Endroit | Avant | Après |
|---|---|---|
| `POST /auth/login` → `role` | `"Recruteur"` \| `"Manager"` | `"Admin"` \| `"RH"` \| `"EvaluateurTechnique"` \| `"Manager"` |
| `DemandeDto` | `recruteurId` | `rhId` |
| `CreneauDto` | `recruteurId` | `employeId` |
| Compte RH | `POST` / `GET /personnes/recruteurs` | `POST` / `GET /personnes/rh` |
| Compte évaluateur technique | — | `POST` / `GET /personnes/evaluateurs-techniques` |
| `GET /personnes/{id}` → `type` | `"Recruteur"` | `"RH"`, `"EvaluateurTechnique"` ou `"Admin"` |

Nouveaux refus à gérer côté écran :

- `403` sur `POST /personnes/candidats`, `POST /demandes`,
  `POST /creneaux/{id}/proposer` et `POST /entretiens` si le compte connecté n'est pas un `RH`.
- `400` sur `POST /entretiens` si le panel ne contient pas le rôle exigé par le `typeEntretien`.

En revanche `POST /creneaux` **s'ouvre** : `RH`, `EvaluateurTechnique` et `Manager` posent
tous leurs disponibilités, alors que c'était réservé au recruteur. Seul l'`Admin` en est
exclu (`403`), puisqu'il ne fait jamais passer d'entretien.

**L'identité de l'auteur ne s'envoie plus.** Trois endpoints d'écriture demandaient à
l'appelant de déclarer qui il est. Ils le lisent maintenant dans le token, ce qui rend
impossible d'agir au nom d'un collègue. Ces champs restent **dans les réponses** :

| Endpoint | Champ retiré de la requête |
|---|---|
| `POST /api/demandes` | `− recruteurId` — l'organisateur est le **RH** connecté |
| `POST /api/creneaux` | `− recruteurId` — le créneau est celui de l'**employé** connecté |
| `POST /api/feedbacks` | `− auteurId` — le compte-rendu est signé par l'utilisateur connecté |

Voir plus haut pour les `403` : `POST /api/demandes` exige un `RH`, `POST /api/creneaux`
accepte les trois rôles qui évaluent mais refuse l'`Admin`.

**`POST /api/demandes`**
- champ `typeEntretien` **supprimé** de la requête et de la réponse

**`POST /api/entretiens`**
- `+ typeEntretien` — obligatoire
- `+ evaluateurIds` — obligatoire, tableau non vide
- `− dateHeure` — l'horaire vient du créneau (`dateDebut`), il n'est plus envoyé

**`POST /api/entretiens/{id}/reprogrammer`**
- `− nouvelleDateHeure` — idem, la date est celle du nouveau créneau

**`EntretienDto`** (toutes les lectures d'entretien)
- `− recruteurId` supprimé
- `+ typeEntretien`
- `+ evaluateurIds` — tableau d'ids

**`POST /api/feedbacks`**
- la règle d'autorisation change : l'auteur doit être un évaluateur **de cet entretien**, et
  non plus n'importe quel recruteur ou manager

**`POST /api/auth/login`**
- `+ id`, `+ nom`, `+ email` dans la réponse — plus besoin de décoder le JWT pour connaître
  l'utilisateur connecté
- le `401` passe du **texte brut** au JSON `{ succes, message }`

**Format des réponses — uniformisé sur toute l'API**
- toutes les erreurs (`400`, `401`, `404`) renvoient désormais `{ succes: false, message }` ;
  elles étaient en texte brut hors des endpoints d'auth
- les actions sans ressource en retour (`annuler`, `confirmer`, `reprogrammer`, `rappel`,
  `proposer`) passent de `204` sans corps à `200` `{ succes: true, message }`
- `POST /api/creneaux` passe de `200` à `201`, comme les autres créations

