# 07 — Flux métier client et admin (V1)

> **Propriétaire de** : pour **chaque flux métier**, son déclencheur, ses préconditions, ses étapes séquentielles (acteur/système), ses chemins alternatifs (erreurs + états non-exécutables), ses post-conditions et ses références croisées. Ce document **relie dans le temps** les écrans ([03](03_specification_ecrans.md)) et les user stories ([04](04_user_stories.md)) ; il ne les duplique pas.
>
> **Règles de cohérence** : les références d'écrans (`B.x`, `C.x`, `D.x`) correspondent aux sections de [03](03_specification_ecrans.md). Les règles métier (`RM-xx`) sont dans [01](01_specification_produit.md). Les enums sont dans [02](02_glossaire_et_taxonomies.md). L'état de construction (🟢/🟡/🔴) est cohérent avec [06](06_ecarts_doc_code.md).

---

## A. Conventions

### A.1 Notation des acteurs

| Acteur | Description |
|---|---|
| `[Visiteur]` | Utilisateur non authentifié (espace Anonymous) |
| `[Investisseur]` | Utilisateur authentifié avec rôle `User` (espace `/client`) |
| `[Admin]` | Utilisateur authentifié avec rôle `Admin` (espace `/admin`) |
| `[Système]` | Backend ou logique applicative |

### A.2 Format des flux

Chaque flux est décrit en six rubriques :
1. **Déclencheur** : ce qui initie le flux (action utilisateur ou événement système)
2. **Préconditions** : état requis avant le début du flux
3. **Étapes** : séquence numérotée acteur/système
4. **Chemins alternatifs** : erreurs techniques et états non-exécutables métier (distingués, cf. RM-24)
5. **Post-conditions** : état du système après le flux
6. **Références** : écrans, règles RM-xx, stories US-xx

### A.3 Statuts de construction

| Symbole | Sens |
|---|---|
| 🟢 | Construit et fonctionnel |
| 🟡 | Partiellement construit |
| 🔴 | Cible non encore construite |

### A.4 Décisions en attente

Les décisions non encore arbitrées sont signalées `[À-ARBITRER A-xx]` avec référence à [06 §4](06_ecarts_doc_code.md#4-d%C3%A9cisions-%C3%A0-arbitrer).

---

## B. Flux clients (investisseurs)

### FLUX-C-01 — Inscription & onboarding 🔴

**Déclencheur** : un visiteur non authentifié clique sur un CTA d'inscription (bouton ou lien depuis `B.1`).

**Préconditions** : aucune session active ; aucun shell métier visible (RM-21).

**Étapes**

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Visiteur]` | Accède à l'écran d'inscription (futur `B.4`). |
| 2 | `[Visiteur]` | Saisit email, mot de passe, confirmation de mot de passe. Coche la case d'acceptation des CGU et de la politique de confidentialité. |
| 3 | `[Système]` | Valide le format d'email, la force du mot de passe, la concordance des champs et l'unicité de l'email. |
| 4 | `[Système]` | Crée le compte avec statut `PENDING`. Envoie un email de confirmation avec un token horodaté. |
| 5 | `[Visiteur]` | Clique sur le lien de confirmation dans l'email. |
| 6 | `[Système]` | Valide le token. Bascule le statut du compte vers `ACTIVE`. Crée la session JWT. |
| 7 | `[Système]` | Détecte l'absence de watchlist, portefeuille et historique → route vers l'écran d'onboarding (`C.14`). |

**Chemins alternatifs**

| Cas | Comportement |
|---|---|
| Email déjà enregistré | Réponse identique sans divulguer l'existence du compte (même posture que `B.2`, RM). |
| Mots de passe non concordants | Validation frontend avant soumission ; message d'erreur localisé. |
| Token de confirmation expiré | État non-exécutable : « Lien invalide ou expiré. » Proposition de renvoyer un email. |
| Inscription sans cliquer la confirmation email | Compte reste en `PENDING`. Accès refusé à la connexion tant que non confirmé. |

**Post-conditions** : compte `ACTIVE`, session JWT initialisée, route vers `C.14`.

**Références** : à créer — écran B.4, US-AUTH-00 ; lié à FLUX-C-04, FLUX-C-02.

---

### FLUX-C-02 — Connexion 🟢

**Déclencheur** : un visiteur accède à `/login` (`B.1`) et saisit ses identifiants.

**Préconditions** : aucune session active valide.

**Étapes**

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Visiteur]` | Saisit email et mot de passe. Option « se souvenir de moi ». |
| 2 | `[Système]` | Vérifie le rate limiting et le statut de verrouillage du compte. |
| 3 | `[Système]` | Authentifie les identifiants (BCrypt). Génère JWT + refresh token. |
| 4 | `[Système]` | Route selon le rôle (RM-22) : rôle `User` → `/client/dashboard` (`C.1`) ; rôle `Admin` → `/admin/dashboard` (`D.1`). |

**Chemins alternatifs**

| Cas | Comportement |
|---|---|
| Identifiants invalides | Message générique sans révéler quel champ est erroné. |
| Compte `DISABLED` | État non-exécutable métier : « Votre accès est indisponible. » (≠ erreur technique, RM-24). |
| Compte `PENDING` (non confirmé) | État non-exécutable : « Confirmez votre adresse email avant de vous connecter. » |
| Compte verrouillé (trop de tentatives) | État non-exécutable avec délai d'attente avant réessai. |
| JWT expiré en cours de session | Refresh token utilisé de façon transparente ; si refresh expiré → retour à `B.1`. |

**Post-conditions** : session active, navigation de l'espace rôle débloquée.

**Références** : `B.1` ; RM-21, RM-22 ; US-AUTH-01.

---

### FLUX-C-03 — Récupération de mot de passe 🟢

**Déclencheur** : l'investisseur clique sur « Mot de passe oublié » depuis `B.1`.

**Préconditions** : aucune session active.

**Étapes — Phase 1 : demande de réinitialisation (`B.2`)**

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Visiteur]` | Accède à `B.2`. Saisit son adresse email. |
| 2 | `[Système]` | Traite la demande sans révéler si le compte existe. Envoie le lien de réinitialisation si le compte est trouvé. |
| 3 | `[Visiteur]` | Reçoit l'email. Clique sur le lien → `B.3`. |

**Étapes — Phase 2 : saisie du nouveau mot de passe (`B.3`)**

| # | Acteur | Action / Réponse système |
|---|---|---|
| 4 | `[Visiteur]` | Saisit le nouveau mot de passe et sa confirmation. |
| 5 | `[Système]` | Valide la concordance et la force. Vérifie la validité du token. |
| 6 | `[Système]` | Applique le nouveau mot de passe. Invalide tous les tokens de reset. Redirige vers `B.1`. |

**Chemins alternatifs**

| Cas | Comportement |
|---|---|
| Token expiré ou invalide | État non-exécutable : « Ce lien est expiré ou invalide. » Lien pour recommencer le flux. |
| Nouveau mot de passe identique à l'ancien | Refus avec message. |
| `newPassword ≠ confirmPassword` | Validation frontend avant soumission. |

**Post-conditions** : mot de passe mis à jour, tokens de session existants invalidés. L'investisseur est sur `B.1`.

**Références** : `B.2`, `B.3` ; US-AUTH-02, US-AUTH-03.

---

### FLUX-C-04 — Première valeur (onboarding vide → première analyse) 🔴

> Ce flux dépend de la construction de `C.14` (C-05 dans [06](06_ecarts_doc_code.md#3-%C3%A9crans--capacit%C3%A9s-cible-%C3%A0-construire)). En son absence, l'investisseur arrive sur une home vide sans guidage — **risque de décrochage au premier jour**.

**Déclencheur** : connexion d'un investisseur sans watchlist, portefeuille, ni historique → détection automatique par `C.1`.

**Préconditions** : session active ; aucune donnée utilisateur.

**Étapes**

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Système]` | Détecte l'état vide à l'atterrissage sur `C.1`. |
| 2 | `[Système]` | Redirige vers `C.14` (onboarding vide). Affiche 2-3 exemples d'actions françaises connues (ex. TotalEnergies, L'Oréal, LVMH). |
| 3 | `[Investisseur]` | Choisit un exemple ou saisit manuellement un instrument. |
| 4 | `[Système]` | Pré-remplit `C.4` avec l'instrument et le contexte `NOT_HELD`. |
| 5 | `[Investisseur]` | Lance l'analyse. |
| 6 | `[Système]` | Exécute l'analyse → route vers `C.5`. |
| 7 | `[Système]` | Persiste le snapshot (RM-19). Propose d'ajouter l'instrument à la watchlist. |

**Chemins alternatifs**

| Cas | Comportement |
|---|---|
| Investisseur ignore l'onboarding | Navigation directe vers `C.1` ; état vide affiché sans crash. |
| Instrument d'exemple non supporté en V1 | État non-exécutable `UnsupportedInstrument` (RM-24). Les exemples proposés sont toujours dans le périmètre V1. |
| Analyse échoue (`NoCrediblePattern`) | État non-exécutable affiché (RM-05) ; plan d'action adapté (« revenez quand l'historique sera suffisant »). |

**Post-conditions** : au moins un snapshot persisté ; watchlist alimentée si l'investisseur l'a voulu.

**Références** : `C.14`, `C.4`, `C.5` ; C-05, C-09 dans [06](06_ecarts_doc_code.md).

---

### FLUX-C-05 — Gestion de la watchlist 🟡

**Déclencheur** : l'investisseur accède à `/client/watchlist` (`C.2`) depuis la nav ou `C.1`.

**Préconditions** : session active avec rôle `User`.

#### Sous-flux A : Ajout d'un instrument

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Investisseur]` | Clique sur « Ajouter ». Saisit un nom ou ticker dans le champ de recherche. |
| 2 | `[Système]` | Retourne les instruments correspondants (actions françaises V1 uniquement). |
| 3 | `[Investisseur]` | Sélectionne un instrument. |
| 4 | `[Système]` | POST watchlist → ajoute l'instrument ; rafraîchit la liste. |

#### Sous-flux B : Retrait d'un instrument

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Investisseur]` | Sélectionne une ligne. Clique sur « Retirer ». |
| 2 | `[Système]` | DELETE watchlist → retire l'instrument immédiatement. |

#### Sous-flux C : Consultation et filtrage

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Investisseur]` | Accède à `C.2`. |
| 2 | `[Système]` | GET watchlist → affiche les lignes avec résumés marché/support/PEA (verbes `NOT_HELD` uniquement : Surveiller/Attendre/Acheter). |
| 3 | `[Investisseur]` | Applique des filtres ou tris. |
| 4 | `[Système]` | Réorganise la liste. Si aucun résultat → état vide (≠ erreur). |

**Chemins alternatifs**

| Cas | Comportement |
|---|---|
| Ajout d'un instrument hors périmètre V1 | Refus avec `UnsupportedInstrument` (RM-24). |
| Watchlist entièrement vide | État vide (≠ erreur). Proposition d'ajouter un premier instrument. |
| Données d'un instrument `STALE` | Indicateur de fraîcheur visible sur la ligne. |

**Post-conditions** : watchlist mise à jour ; liste affichée avec résumés à jour.

**Références** : `C.2` ; RM-10 (verbes non détenus).

---

### FLUX-C-06 — Gestion du portefeuille 🟡

**Déclencheur** : l'investisseur accède à `/client/portfolio` (`C.3`) depuis la nav ou `C.1`.

**Préconditions** : session active.

#### Sous-flux A : Enregistrement d'une transaction d'achat

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Investisseur]` | Clique sur « Ajouter une transaction ». Saisit quantité, prix unitaire, date, frais, devise. Sélectionne « Achat ». |
| 2 | `[Système]` | Valide les champs (quantité > 0, date ≤ aujourd'hui, etc.). |
| 3 | `[Système]` | POST transaction → reconstruit le FIFO (RM-08). Dérive et affiche le PRU mis à jour. |

#### Sous-flux B : Enregistrement d'une vente

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Investisseur]` | Saisit les paramètres de la vente (quantité, prix, date, frais). |
| 2 | `[Système]` | Vérifie que la quantité vendue ≤ quantité ouverte en FIFO strict. |
| 3 | `[Système]` | Consomme les lignes les plus anciennes en FIFO. Recalcule le PRU dérivé. |
| 4 | `[Système]` | Si quantité ouverte résiduelle = 0 → la position bascule vers `NOT_HELD` sur le dashboard `C.1`. |

#### Sous-flux C : Suppression d'une transaction

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Investisseur]` | Sélectionne une transaction. Clique sur « Supprimer ». Confirme. |
| 2 | `[Système]` | DELETE transaction → recalcul FIFO complet. Affiche le PRU mis à jour. |

> **Cas-limite [À-ARBITRER]** : si l'investisseur supprime une transaction d'achat déjà partiellement absorbée par une vente FIFO, le comportement attendu (recalcul rétroactif, refus, ou avertissement) n'est pas encore spécifié. Ce cas est à arbitrer avant d'industrialiser la suppression.

#### Sous-flux D : Consultation

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Investisseur]` | Accède à `C.3`. |
| 2 | `[Système]` | GET portfolio → affiche les lignes ouvertes avec PRU dérivé, résumés analytiques, verbes `HELD` uniquement. |

**Chemins alternatifs**

| Cas | Comportement |
|---|---|
| Portefeuille vide | État vide (≠ erreur). Proposition d'enregistrer une première transaction. |
| Vente excédant la quantité ouverte | Validation refusée : « Quantité vendue supérieure à la position ouverte. » |
| Instrument acheté devenu `isActive=false` | La position reste affichée dans le portefeuille ; les nouvelles analyses sont refusées avec `UnsupportedInstrument`. |

**Post-conditions** : transactions persistées, PRU dérivé recalculé, affichage rafraîchi.

**Références** : `C.3` ; RM-08, RM-10 (verbes détenus) ; US-PORT-xx.

---

### FLUX-C-07 — Demande d'analyse 🟡

**Déclencheur** : l'investisseur accède à `/client/analysis` (`C.4`) depuis `C.1`, `C.2`, ou la nav.

**Préconditions** : session active.

**Étapes**

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Investisseur]` | Saisit ou sélectionne un instrument (recherche live). |
| 2 | `[Investisseur]` | Déclare le contexte de détention : `NOT_HELD` ou `HELD`. Ce choix est demandé **avant** l'analyse (RM-07 : la détection est indépendante du contexte ; la reco en dépend). |
| 3 | `[Investisseur]` | Clique sur « Analyser ». |
| 4 | `[Système]` | POST `/ClientFinance/analysis/run` → exécute le moteur déterministe. |
| 5 | `[Système]` | Persiste le snapshot (RM-19). Route vers `C.5` avec l'`analysisId`. |

**Chemins alternatifs**

| Cas | Comportement |
|---|---|
| `NoCrediblePattern` | État non-exécutable de premier rang (RM-05) : « Aucun pattern crédible détecté. » Plan d'action adapté. |
| `InsufficientData` | État non-exécutable (RM-24) : « Historique insuffisant pour l'analyse. » |
| `UnsupportedInstrument` | État non-exécutable (RM-24) : instrument hors périmètre V1. |
| `MultipleCompatiblePatterns` | Tous les patterns compatibles affichés (RM-06) ; le principal a la priorité d'affichage. |
| Erreur réseau / timeout | Erreur récupérable avec bouton « Réessayer ». |
| Mauvais contexte de détention saisi | Le produit ne peut pas détecter l'erreur ; **avertissement suggéré à l'écran `C.4`** lors du choix du contexte. |

**Post-conditions** : snapshot persisté si analyse exécutable ; route vers `C.5`.

**Références** : `C.4`, `C.5` ; RM-05, RM-06, RM-07, RM-09, RM-10, RM-19.

---

### FLUX-C-08 — Consultation du résultat d'analyse 🟡

**Déclencheur** : arrivée sur `C.5` après une analyse (`C.4`) ou depuis `C.1`/`C.2`/`C.12`.

**Préconditions** : snapshot existant et accessible.

**Parcours de lecture (ordre imposé par la spec, RM-09)**

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Système]` | Affiche le bandeau d'issue (`AnalysisOutcome` → wording FR). |
| 2 | `[Investisseur]` | Lit la **lecture marché** : pattern principal, alternatifs, `PatternStatus`, confiance. |
| 3 | `[Investisseur]` | Consulte la **confiance expliquée** (RM-27) : grille de critères ✅/⚠️/❌ sous le label de confiance. |
| 4 | `[Investisseur]` | Lit la **lecture support** : score composite (si disponible), statut PEA, complétude. |
| 5 | `[Investisseur]` | Lit la **lecture situation personnelle** (contexte détenu/non détenu). |
| 6 | `[Investisseur]` | Consulte le **rail de synthèse** et la **recommandation** (verbe contextualisé, RM-10). |
| 7 | `[Investisseur]` | Lit le **plan d'action** (RM-26) : 2-3 étapes déterministes traçables aux vérités déjà affichées. |
| 8 | `[Investisseur]` | Choisit une action parmi les liens de suivi. |

**Actions disponibles depuis `C.5`**

| Action | Destination |
|---|---|
| Activer une alerte sur un niveau | Crée une alerte `LEVEL_CROSSED` (RM-25) |
| Ouvrir l'historique | → `C.9` |
| Ouvrir le détail d'un paramètre | → `C.7` |
| Ouvrir l'aide | → `C.13` |
| Ouvrir le détail instrument | → `C.6` |

**Chemins alternatifs**

| Cas | Comportement |
|---|---|
| Résultat non-exécutable (`NoCrediblePattern`, etc.) | Plan d'action adapté (ex. « revenez dans X jours ») ; aucune reco de trade (RM-05). |
| Support incomplet (PEA `Unknown`) | Score composite indisponible (`PEA_UNKNOWN_BLOCKING`), affiché explicitement (RM-14, RM-15). |
| Confiance expliquée (C-08) non encore construite | Affichage du label de confiance seul ; les critères détaillés sont à construire. |
| Plan d'action (C-09) non encore construit | Section absente ou placeholder ; les données sources sont présentes. |

**Post-conditions** : snapshot lu ; alertes créées si demandé.

**Références** : `C.5`, `C.6`, `C.7`, `C.9`, `C.13` ; RM-01, RM-06, RM-09, RM-10, RM-11, RM-14, RM-15, RM-19, RM-26, RM-27 ; C-06, C-08, C-09 dans [06](06_ecarts_doc_code.md).

---

### FLUX-C-09 — Consultation de l'historique de snapshots 🟢

**Déclencheur** : l'investisseur accède à `/client/history` (`C.9`) depuis `C.1`, `C.3`, `C.5`, ou `C.6`.

**Préconditions** : session active.

**Étapes**

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Système]` | GET historique → liste datée de snapshots persistés. |
| 2 | `[Investisseur]` | Sélectionne un snapshot. |
| 3 | `[Système]` | Affiche la vérité persistée du snapshot (jamais reconstruite depuis l'état courant, RM-20). |
| 4 | `[Investisseur]` | Optionnel : sélectionne un deuxième snapshot pour comparaison → `C.10`. |

**Chemins alternatifs**

| Cas | Comportement |
|---|---|
| Historique vide | État vide (≠ erreur). Proposition de lancer une première analyse. |
| Snapshot d'un instrument devenu `isActive=false` | Snapshot valide et lisible (vérité persistée) ; un nouveau lancement d'analyse serait refusé (`UnsupportedInstrument`). |

**Post-conditions** : aucun changement d'état ; lecture seule.

**Références** : `C.9`, `C.10` ; RM-20.

---

### FLUX-C-10 — Comparaison de deux snapshots 🔴

> Cible non construite côté user (C-02 dans [06](06_ecarts_doc_code.md)). L'équivalent admin existe en `D.8`.

**Déclencheur** : l'investisseur sélectionne deux snapshots depuis `C.9` et demande la comparaison.

**Préconditions** : au moins deux snapshots persistés pour un même instrument.

**Étapes**

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Investisseur]` | Sélectionne snapshot gauche + snapshot droit. |
| 2 | `[Investisseur]` | Lance la comparaison. |
| 3 | `[Système]` | Évalue la comparabilité (versions de moteur, contexte de détention). |
| 4 | `[Système]` | Affiche le diff structuré : changements en lecture marché / lecture support / recommandation. |
| 5 | `[Investisseur]` | Identifie les causes vs les conséquences des changements. |

**Chemins alternatifs**

| Cas | Comportement |
|---|---|
| Snapshots non comparables (versions de moteur incompatibles ou contextes différents) | Non-comparabilité explicite (RM-20) : affichée clairement, jamais masquée. |
| Un seul snapshot disponible | État non-exécutable : « Deux snapshots sont requis pour la comparaison. » |

**Post-conditions** : lecture seule ; aucun snapshot modifié.

**Références** : `C.9`, `C.10` ; RM-20.

---

### FLUX-C-11 — Simulation d'un scénario 🟡

**Déclencheur** : l'investisseur accède à `/client/simulation` (`C.8`) depuis `C.3` ou `C.6`.

**Préconditions** : session active. Aucune donnée préalable requise.

**Étapes**

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Investisseur]` | Saisit les paramètres du scénario : prix d'entrée, taille de position, niveau d'invalidation, niveau cible, frais. |
| 2 | `[Système]` | Valide la cohérence locale des entrées (ex. cible > entrée pour une position longue). |
| 3 | `[Système]` | Calcule les sorties : baisse potentielle, hausse potentielle, ratio R/R. |
| 4 | `[Investisseur]` | Modifie les paramètres pour explorer différents scénarios (recalcul à chaque changement). |

**Chemins alternatifs**

| Cas | Comportement |
|---|---|
| Paramètres incohérents (ex. cible ≤ entrée pour achat) | Validation frontend avec message avant soumission. |
| Scénario hors périmètre V1 | État non-exécutable. |

**Post-conditions** : aucun snapshot persisté (la simulation n'est jamais une vérité enregistrée) ; distinction visuelle explicite simulation ≠ historique (RM-08).

**Références** : `C.8` ; RM-08 (simulation ≠ persistance) ; US-SIM-xx.

---

### FLUX-C-12 — Centre de notifications et alertes proactives 🔴 (alertes proactives)

> La surface de notification est 🟢 construite. Les **alertes proactives** (déclenchées par la boucle ex post, cf. FLUX-T-01) sont 🔴 non construites (C-10 dans [06](06_ecarts_doc_code.md)).

**Déclencheur** : l'investisseur accède à `/client/notifications` (`C.12`) ou reçoit une notification dans la barre d'en-tête.

**Préconditions** : session active.

**Étapes**

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Système]` | GET notifications → liste unifiée (notifications produit + alertes proactives). |
| 2 | `[Investisseur]` | Filtre par catégorie / statut / déclencheur (`PATTERN_STATE_CHANGE`, `LEVEL_CROSSED`, `DATA_STALE`). |
| 3 | `[Investisseur]` | Clique sur une notification. |
| 4 | `[Système]` | Route vers la `NotificationTargetScreen` correspondante : alerte de niveau → `C.6` ; changement d'état → `C.5` ; données obsolètes → `C.6`. |
| 5 | `[Système]` | Marque la notification comme lue. |

**Chemins alternatifs**

| Cas | Comportement |
|---|---|
| Centre de notifications vide | État vide (≠ erreur). |
| Catégorie d'alertes désactivée dans `C.15` | Les alertes de cette catégorie ne sont pas générées (RM-25b). |
| Destination non exécutable (ex. instrument supprimé du périmètre) | État non-exécutable sur l'écran destination (RM-24). |

**Post-conditions** : notifications marquées comme lues ; investisseur routé vers la surface de vérité.

**Règles** : les alertes **routent, n'expliquent pas** (RM-25b) ; dédoublonnage par (instrument × déclencheur × jour) ; les alertes ne sont **pas des prédictions** (RM-25b).

**Références** : `C.12`, `C.15` ; RM-23, RM-25, RM-25b ; C-10 dans [06](06_ecarts_doc_code.md) ; FLUX-T-01.

---

### FLUX-C-13 — Détail paramètre (4 couches) 🔴

> Surface non construite côté user (C-01 dans [06](06_ecarts_doc_code.md)).

**Déclencheur** : l'investisseur clique sur un paramètre ou une info-bulle de glossaire depuis `C.5` ou `C.6`.

**Préconditions** : paramètre accessible et publié dans le dictionnaire gouverné.

**Étapes**

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Investisseur]` | Clique sur le paramètre ou l'info-bulle. |
| 2 | `[Système]` | GET `parameter-detail/:parameterId` → charge les 4 couches depuis le dictionnaire gouverné (RM-17). |
| 3 | `[Investisseur]` | Lit les 4 couches dans l'ordre : (1) définition simple → (2) valeur courante → (3) pourquoi ce paramètre compte → (4) implication pour ma situation. |
| 4 | `[Investisseur]` | Revient à l'écran d'origine (`C.5` ou `C.6`). |

**Post-conditions** : lecture seule ; aucun effet de bord.

**Règles** : un paramètre seul ne constitue jamais une recommandation (RM-18) ; tout le texte vient du dictionnaire gouverné backend (RM-17), jamais du frontend.

**Références** : `C.7` ; RM-16, RM-17, RM-18 ; C-01 dans [06](06_ecarts_doc_code.md).

---

### FLUX-C-14 — Learn, centre d'aide et glossaire inline 🔴

> Trois surfaces distinctes. Toutes non construites (C-03, C-04, C-13 dans [06](06_ecarts_doc_code.md)).

#### Surface A : Learn (`C.11`)

**Déclencheur** : accès depuis la nav ou un lien contextuel.

**Objectif** : contenu éducatif long sur les concepts (patterns, lecture support, PEA, limites du produit). Pas un chat, pas un formulaire.

**Règle** : **ne pas dupliquer** le glossaire inline (RM-28) — `Learn` = contenu conceptuel long ; glossaire = réponse au doute ponctuel en cours de tâche.

#### Surface B : Centre d'aide (`C.13`)

**Déclencheur** : accès depuis la plupart des écrans, la nav, ou une notification.

**Objectif** : aide contextuelle — explique et route vers la bonne surface de tâche. Déterministe, pas un chat (RM-23).

#### Surface C : Glossaire inline (info-bulles)

**Déclencheur** : apparaît sur les termes techniques des écrans `C.5`, `C.6` au moment du doute.

**Objectif** : réponse au doute ponctuel sans quitter le flux en cours.

**Règle commune** : les trois surfaces puisent dans le **dictionnaire gouverné** (RM-17, RM-28), jamais dans du texte frontend libre.

**Références** : `C.11`, `C.13` ; RM-17, RM-23, RM-28 ; C-03, C-04, C-13 dans [06](06_ecarts_doc_code.md).

---

### FLUX-C-15 — Gestion du compte 🟡

**Déclencheur** : l'investisseur accède à `/client/account/profile` ou `/client/account/security` (`C.15`).

**Préconditions** : session active.

#### Sous-flux A : Modification du profil

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Investisseur]` | GET profil → affichage des informations actuelles. |
| 2 | `[Investisseur]` | Modifie les champs souhaités (nom d'affichage, etc.). |
| 3 | `[Système]` | PUT profil → confirme la mise à jour. |

#### Sous-flux B : Préférences de notification et d'alertes

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Investisseur]` | Accède aux préférences de notification. |
| 2 | `[Investisseur]` | Active ou désactive des catégories d'alertes (`PATTERN_STATE_CHANGE`, `LEVEL_CROSSED`, `DATA_STALE`). |
| 3 | `[Système]` | PATCH préférences → effet immédiat sur la génération d'alertes futures (RM-25b). |

#### Sous-flux C : Changement de mot de passe

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Investisseur]` | Accède à l'onglet « Sécurité ». |
| 2 | `[Investisseur]` | Saisit mot de passe actuel, nouveau mot de passe, confirmation. |
| 3 | `[Système]` | Vérifie l'actuel (BCrypt). Valide le nouveau (force, concordance, ≠ actuel). |
| 4 | `[Système]` | PUT sécurité → met à jour ; invalide les sessions existantes sur les autres appareils. |

**Règles** : aucune action admin ici (RM-23) ; la gestion des alertes se fait **ici**, pas dans `C.12`.

**Références** : `C.15`, `C.12` ; RM-23, RM-25b.

---

### FLUX-C-16 — Déconnexion 🟢

**Déclencheur** : l'investisseur clique sur « Se déconnecter » depuis n'importe quelle page.

**Étapes**

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Investisseur]` | Clique sur « Se déconnecter ». |
| 2 | `[Système]` | Invalide le refresh token côté serveur. Supprime le JWT du stockage local. |
| 3 | `[Système]` | Redirige vers `B.1`. L'historique de navigation (retour arrière) ne donne plus accès aux pages protégées. |

**Post-conditions** : aucune session active ; toutes les routes `/client/` et `/admin/` inaccessibles.

**Références** : `B.1` ; US-AUTH-04.

---

## C. Flux admin

### FLUX-A-01 — Connexion admin 🟢

**Déclencheur** : un administrateur accède à `/login` (`B.1`) et saisit ses identifiants.

**Préconditions** : aucune session active ; compte avec rôle `Admin`.

**Étapes**

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Admin]` | Accède à `/login` (`B.1`) et saisit email + mot de passe. |
| 2 | `[Système]` | Vérifie rate limiting, statut du compte, authentifie (BCrypt). |
| 3 | `[Système]` | Génère JWT + refresh token. Détecte le rôle `Admin`. |
| 4 | `[Système]` | Route vers `/admin/dashboard` (`D.1`) — **jamais** vers `/client/dashboard` (RM-22). |

**Chemins alternatifs** : identiques à FLUX-C-02 (identifiants invalides, compte désactivé, rate limiting, JWT expiré).

**Post-conditions** : session active dans l'espace `/admin/*`.

**Références** : `B.1`, `D.1` ; RM-21, RM-22 ; US-AUTH-01.

### FLUX-A-02 — Gestion des utilisateurs 🟢

**Déclencheur** : l'admin accède à `/admin/users` (`D.2`).

#### Sous-flux A : Consultation et recherche

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Admin]` | GET users → liste avec identité, rôle, statut, dernière activité. |
| 2 | `[Admin]` | Filtre par rôle / statut / recherche textuelle. |

#### Sous-flux B : Création d'un utilisateur

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Admin]` | Accède à `/admin/users/add`. Saisit email et rôle. |
| 2 | `[Système]` | POST user → crée avec statut `PENDING`. Envoie un email de bienvenue. |

#### Sous-flux C : Édition du rôle ou du statut

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Admin]` | Ouvre `/admin/users/edit/:id`. Modifie le rôle ou le statut. |
| 2 | `[Système]` | PUT user → applique immédiatement. Un utilisateur dont le rôle change voit ses permissions mises à jour à sa prochaine requête. |

#### Sous-flux D : Désactivation d'un utilisateur

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Admin]` | Sélectionne un utilisateur. Clique sur « Désactiver ». Confirme. |
| 2 | `[Système]` | PATCH user → statut `DISABLED`. L'utilisateur ne peut plus se connecter (FLUX-C-02, chemin alternatif). |

**Règle** : le self-service utilisateur reste en `C.15` ; `D.2` est réservé à l'administration structurelle.

**Références** : `D.2` ; RM-22 (rôles).

---

### FLUX-A-03 — Registre instruments 🟢

**Déclencheur** : l'admin accède à `/admin/instrument-registry` (`D.3`).

#### Sous-flux A : Consultation

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Admin]` | GET instrument-registry → liste avec mapping fournisseur, univers actif, fraîcheur. |
| 2 | `[Admin]` | Filtre / recherche. |

#### Sous-flux B : Activation d'un nouvel instrument

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Admin]` | POST instrument → saisit `symbol`, `marketCode`, `assetType`, mapping fournisseur. |
| 2 | `[Système]` | Vérifie l'unicité de la clé (`symbol + marketCode + assetType`). |
| 3 | `[Système]` | Crée l'instrument avec `isActive=true`. L'instrument est désormais analysable. |

#### Sous-flux C : Désactivation d'un instrument

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Admin]` | PATCH instrument → `isActive=false`. Confirme. |
| 2 | `[Système]` | Les snapshots existants restent valides (RM-20). Les nouvelles demandes d'analyse retournent `UnsupportedInstrument` (RM-24). |

> **Comportement attendu sur les instruments en watchlist/portefeuille désactivés** : si un instrument suivi par un investisseur est désactivé, il reste dans sa watchlist et son portefeuille mais les nouvelles analyses échouent avec `UnsupportedInstrument`. Une alerte `DATA_STALE` peut se déclencher si les données deviennent obsolètes (RM-25). Ce comportement est à confirmer et à documenter côté UI.

**Références** : `D.3` ; RM-20, RM-24, RM-25.

---

### FLUX-A-04 — Registre PEA 🟢

**Déclencheur** : l'admin accède à `/admin/pea-registry` (`D.4`).

#### Sous-flux A : Consultation

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Admin]` | GET pea-registry → liste avec statut PEA, source, date de vérification. |

#### Sous-flux B : Mise à jour du statut d'éligibilité

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Admin]` | Sélectionne un instrument. Modifie le statut PEA (`Unknown` / `ConfirmedEligible` / `ConfirmedIneligible`). Renseigne la source et la date. |
| 2 | `[Système]` | PUT eligibility → enregistre le nouveau statut. |
| 3 | `[Système]` | **Impact en cascade** : les futures analyses de cet instrument reflèteront le nouveau statut. Les snapshots passés restent inchangés (RM-20). |

> **Règle critique** : `Unknown` reste **visiblement distinct** de `ConfirmedIneligible` (RM-15). Un passage de `Unknown` à `ConfirmedIneligible` peut rendre le score composite indisponible (`PEA_UNKNOWN_BLOCKING` → `CONFIRMED_INELIGIBLE`) sur les futures analyses.

**Références** : `D.4` ; RM-15, RM-20.

---

### FLUX-A-05 — Politique de scoring 🟢

**Déclencheur** : l'admin accède à `/admin/scoring-policy` (`D.5`).

#### Sous-flux A : Consultation de la politique active

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Admin]` | GET scoring-policy → version active, univers, catégories, règles d'inclusion. |

#### Sous-flux B : Création d'une nouvelle version (DRAFT)

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Admin]` | POST policy-version → paramètre les nouvelles règles. Statut initial : `DRAFT`. |
| 2 | `[Admin]` | Valide les règles dans l'interface. |

#### Sous-flux C : Publication

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Admin]` | PUT policy-version → bascule vers `ACTIVE`. Ancienne version archivée. |
| 2 | `[Système]` | Les futures analyses utilisent la nouvelle version. Les snapshots passés portent leur `policyVersion` et ne sont pas recalculés (RM-19). |

#### Sous-flux D : Rollback

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Admin]` | Sélectionne une version archivée. La repasse en `ACTIVE`. |

**Références** : `D.5` ; RM-19.

---

### FLUX-A-06 — Dictionnaire de paramètres 🟢

**Déclencheur** : l'admin accède à `/admin/parameter-dictionary` (`D.6`).

#### Sous-flux A : Consultation

GET parameter-dictionary → liste. GET `/detail/:parameterId` → 4 couches.

#### Sous-flux B : Ajout d'un paramètre

POST parameter → id stable, libellé UI, définition, garde-fous, gabarits d'implication.

#### Sous-flux C : Édition et versioning

PUT parameter → incrémentation de version → publication.

**Règle** : tout paramètre publié alimente automatiquement le glossaire inline côté user (RM-17, RM-28). Les paramètres non publiés restent invisibles côté user.

**Références** : `D.6`, `C.7` ; RM-17, RM-28.

---

### FLUX-A-07 — Versions de wording 🟢

**Déclencheur** : l'admin accède à `/admin/wording-versions` (`D.7`).

Les sous-flux (consultation, édition, publication, rollback) suivent le même pattern que FLUX-A-05, appliqué aux verbes d'action et gabarits de texte pédagogique.

**Règle** : une version publiée s'applique aux futures analyses. Les snapshots passés portent leur version de wording et ne sont pas mis à jour (RM-19, RM-20).

**Références** : `D.7` ; RM-19, RM-20.

---

### FLUX-A-08 — Audit de snapshots 🟢

**Déclencheur** : l'admin accède à `/admin/snapshot-audit` (`D.8`).

#### Sous-flux A : Navigation dans l'audit

GET snapshot-audit → liste avec filtres (instrument, date, version de moteur, utilisateur).

#### Sous-flux B : Inspection d'un snapshot

GET `/detail/:analysisRunId` → payload complet (marché, support, recommandation, contexte de détention, versions RM/scoring/wording).

#### Sous-flux C : Comparaison admin

GET `/compare` → sélection de deux snapshots → diff structuré. Non-comparabilité affichée explicitement si pertinent (RM-20).

**Règle** : auditabilité d'abord ; aucun masquage du contexte de version pour la commodité.

**Références** : `D.8` ; RM-03, RM-20.

---

### FLUX-A-09 — Surveillance qualité des données 🟢

**Déclencheur** : l'admin accède à `/admin/data-quality` (`D.9`).

**Étapes**

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Système]` | GET data-quality → vue priorisée par impact utilisateur probable. |
| 2 | `[Admin]` | Filtre par catégorie de problème (fraîcheur, incomplétude PEA, métriques manquantes, instruments hors univers). |
| 3 | `[Admin]` | Ouvre la surface impactée pour correction (`D.3`, `D.4`, `D.6`). |

**Types de problèmes distingués**

| Type | Exemple |
|---|---|
| Fraîcheur source dégradée | Instrument en `STALE` ou `MISSING` |
| Incomplétude registre PEA | Instruments en `Unknown` en masse |
| Métriques fondamentales manquantes | Catégorie de scoring sans couverture suffisante |
| Instruments hors univers | Instrument actif mais sans mapping fournisseur |

**Références** : `D.9`, `D.3`, `D.4` ; RM-25 (déclencheur `DATA_STALE`).

---

### FLUX-A-10 — Consultation des KPI de pilotage 🔴

> Capacité non construite (C-12 dans [06](06_ecarts_doc_code.md)). Les données brutes sont présentes ; les surfaces d'agrégation et d'affichage sont à construire.

**Déclencheur** : l'admin accède aux écrans de pilotage depuis `D.1` ou la nav admin.

**Famille A — Qualité des signaux** (écran `D.10a`, 🔴)

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Admin]` | GET signal-quality → taux d'atteinte de cible global, courbe, tableau de calibration par bucket de confiance. |
| 2 | `[Admin]` | Filtre par période / pattern / version de règles. |
| 3 | `[Admin]` | Si calibration défaillante (confiance HIGH n'atteint pas la cible plus souvent que LOW) → lien vers `D.5` pour recalibration. |

> **[À-ARBITRER A-07]** : la fenêtre d'évaluation ex post (`evaluationWindowDays`) n'est pas encore fixée. Ce flux dépend de FLUX-T-01 (boucle ex post).

**Famille B+C — Engagement, activation, usage** (écran `D.10b`, 🔴)

GET engagement → inscriptions/DAU/WAU/MAU, stickiness, grille de rétention cohortes, funnel d'activation (inscription → watchlist → analyse → transaction), patterns les plus analysés, écrans les plus visités.

> **[À-ARBITRER A-09]** : la définition canonique de « actif » (login ? requête ? analyse ?) conditionne toutes les métriques d'engagement.

**Famille D — Santé opérationnelle** (intégrée à `D.1` et `D.9`)

Taux d'échec d'analyse, latence p50/p95, couverture PEA, complétude des snapshots, fraîcheur des données.

**Règle transversale** : tout KPI expose son état de disponibilité (`KPI_AVAILABLE` / `KPI_INSUFFICIENT_DATA` / `KPI_WINDOW_TOO_YOUNG`). Aucun KPI n'invente une vérité (RM-29).

**Références** : `D.1`, `D.10a`, `D.10b` ; RM-29, RM-29b ; C-12, A-07, A-09 dans [06](06_ecarts_doc_code.md) ; FLUX-T-01.

---

### FLUX-A-11 — Déconnexion admin 🔴

**Déclencheur** : l'admin clique sur « Se déconnecter » depuis n'importe quelle page de l'espace `/admin`.

**Étapes** : identiques à FLUX-C-16 côté client.

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Admin]` | Clique sur « Se déconnecter ». |
| 2 | `[Système]` | Invalide le refresh token côté serveur. Supprime le JWT du stockage local. |
| 3 | `[Système]` | Redirige vers `B.1`. L'historique de navigation ne donne plus accès aux pages protégées. |

**Post-conditions** : aucune session active ; routes `/admin/*` et `/client/*` inaccessibles.

**Références** : `B.1` ; FLUX-C-16 (même implémentation).

---

### FLUX-A-12 — Suppression de compte utilisateur (RGPD Art. 17) 🔴

> ⚠️ **Bloquant légal** : la suppression de compte est une obligation RGPD Art. 17. Son absence est un blocage de lancement commercial (voir [08 §2](08_analyse_critique_et_legal.md)). Ce flux doit exister **avant mise en production**. L'accès est ouvert à tout `Admin`, voir [10 §E](10_personas.md#e-persona-a01--ladministrateur).

**Déclencheur** : un utilisateur demande la suppression de son compte (auto-service, RGPD Art. 17) **ou** un admin exécute la suppression sur demande.

**Préconditions** : session active ; pour l'auto-service User → écran `C.15` ; pour l'admin → `D.2 edit/:id`.

#### Sous-flux A : Auto-service utilisateur (depuis C.15)

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Investisseur]` | Accède à `C.15` → section « Supprimer mon compte ». |
| 2 | `[Investisseur]` | Confirme la suppression (double confirmation : saisie email + bouton). |
| 3 | `[Système]` | Anonymise ou supprime les données personnelles selon la politique de rétention. |
| 4 | `[Système]` | Invalide toutes les sessions. Route → `B.1` avec message de confirmation. |

#### Sous-flux B : Suppression par un admin (depuis D.2)

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Admin]` | Accède à `D.2 edit/:id`. |
| 2 | `[Admin]` | Déclenche la suppression avec justification tracée. |
| 3 | `[Système]` | Idem sous-flux A étapes 3–4. Journalise l'action admin avec `userId` et motif. |

**Chemins alternatifs**

| Cas | Comportement |
|---|---|
| Snapshots historiques | Anonymisation (pseudonymisation) possible plutôt que suppression brute — à définir dans la politique de rétention. |
| Données analytics > 13 mois | Anonymisation obligatoire indépendamment de la suppression du compte (A-10). |

**Post-conditions** : compte supprimé ou anonymisé ; `userId` non récupérable ; sessions invalidées.

**Références** : `C.15`, `D.2` ; RGPD Art. 17 ; [08 §2.2](08_analyse_critique_et_legal.md) ; décision A-10 dans [06](06_ecarts_doc_code.md).

---

## D. Flux transversal — Boucle de feedback ex post

### FLUX-T-01 — Boucle de feedback ex post 🔴

> Ce flux est un **mécanisme système** (pas un flux utilisateur direct). Il est le **prérequis structurant** de FLUX-C-12 (alertes proactives) et FLUX-A-10 famille A (KPI qualité des signaux). Chantier C-11 dans [06](06_ecarts_doc_code.md).

**Déclencheur V1** : connexion d'un investisseur ou ouverture des surfaces de suivi (watchlist, portefeuille, historique). `[À-ARBITRER A-11]` : cadence exacte non encore fixée.

**Déclencheur V2** : batch nocturne automatisé (à mutualiser avec le même mécanisme).

**Étapes**

| # | Acteur | Action / Réponse système |
|---|---|---|
| 1 | `[Système]` | Identifie les snapshots avec `status=STILL_OPEN` et des niveaux persistés (`TargetPrice`, `InvalidationPrice`). |
| 2 | `[Système]` | Compare avec `PriceHistory` postérieure à la date du snapshot. |
| 3 | `[Système]` | Attribue un `SignalOutcome` : `TARGET_HIT` / `INVALIDATION_HIT` / `STILL_OPEN` / `NOT_EVALUABLE`. |
| 4 | `[Système]` | Si `TARGET_HIT` ou `INVALIDATION_HIT` : génère une alerte `LEVEL_CROSSED` pour l'investisseur concerné (FLUX-C-12). |
| 5 | `[Système]` | Met à jour les données source des KPI famille A (FLUX-A-10). |

> **[À-ARBITRER A-07]** : la fenêtre d'évaluation (`evaluationWindowDays`) n'est pas encore fixée — horizon fixe (20j / 60j) ou `reviewHorizonDays` du signal ?

> **[À-ARBITRER A-08]** : le stockage de `SignalOutcome` — colonne sur `PatternAssessment` vs table dédiée (table dédiée recommandée pour les multi-horizons futurs).

**Règle de mutualisation** : C-10 (alertes) et C-11 (ex post) partagent ce mécanisme. Le construire **une seule fois** (cf. [06 §3](06_ecarts_doc_code.md#3-%C3%A9crans--capacit%C3%A9s-cible-%C3%A0-construire) — mutualisation clé).

**Références** : RM-25, RM-25b, RM-29 ; C-10, C-11, C-12 dans [06](06_ecarts_doc_code.md) ; A-07, A-08, A-11 ; FLUX-C-12, FLUX-A-10.

---

## E. Matrice de couverture des flux

| Flux | Écran(s) | Règles RM | Statut | Gaps liés ([06](06_ecarts_doc_code.md)) | Décisions ([06 §4](06_ecarts_doc_code.md)) |
|---|---|---|---|---|---|
| FLUX-C-01 | B.4 | RM-21 | 🔴 | US-AUTH-00 manquante ([04](04_user_stories.md)) | — |
| FLUX-C-02 | B.1 | RM-21, RM-22 | 🟢 | — | — |
| FLUX-C-03 | B.2, B.3 | — | 🟢 | — | — |
| FLUX-C-04 | C.14, C.4, C.5 | RM-05, RM-19, RM-24 | 🔴 | C-05, C-09 | — |
| FLUX-C-05 | C.2 | RM-10, RM-24 | 🟡 | — | — |
| FLUX-C-06 | C.3 | RM-08, RM-10 | 🟡 | — | A-02 (HoldingContext) |
| FLUX-C-07 | C.4, C.5 | RM-05, RM-06, RM-07, RM-09, RM-10, RM-19, RM-24 | 🟡 | C-06, C-07 | — |
| FLUX-C-08 | C.5, C.6, C.7, C.9, C.13 | RM-01, RM-06, RM-09, RM-10, RM-11, RM-14, RM-15, RM-19, RM-26, RM-27 | 🟡 | C-06, C-08, C-09 | — |
| FLUX-C-09 | C.9, C.10 | RM-20 | 🟢 | — | — |
| FLUX-C-10 | C.10 | RM-20 | 🔴 | C-02 | — |
| FLUX-C-11 | C.8 | RM-08 | 🟡 | — | — |
| FLUX-C-12 | C.12, C.15 | RM-23, RM-25, RM-25b | 🔴 (alertes proactives) | C-10 | A-04 (persistance notifs) |
| FLUX-C-13 | C.7 | RM-16, RM-17, RM-18 | 🔴 | C-01 | A-06 |
| FLUX-C-14 | C.11, C.13 | RM-17, RM-23, RM-28 | 🔴 | C-03, C-04, C-13 | — |
| FLUX-C-15 | C.15 | RM-23, RM-25b | 🟡 | — | — |
| FLUX-C-16 | B.1 | — | 🟢 | — | — |
| FLUX-A-01 | B.1, D.1 | RM-22 | 🟢 | — | — |
| FLUX-A-02 | D.2 | RM-22 | 🟢 | — | — |
| FLUX-A-03 | D.3 | RM-20, RM-24, RM-25 | 🟢 | — | — |
| FLUX-A-04 | D.4 | RM-15, RM-20 | 🟢 | — | — |
| FLUX-A-05 | D.5 | RM-19 | 🟢 | — | — |
| FLUX-A-06 | D.6 | RM-17, RM-28 | 🟢 | — | — |
| FLUX-A-07 | D.7 | RM-19, RM-20 | 🟢 | — | — |
| FLUX-A-08 | D.8 | RM-03, RM-20 | 🟢 | — | — |
| FLUX-A-09 | D.9 | RM-25 | 🟢 | — | — |
| FLUX-A-10 | D.1, D.10a, D.10b | RM-29, RM-29b | 🔴 | C-12 | A-07, A-09, A-11 |
| FLUX-A-11 | B.1 | — | 🔴 | — | — |
| FLUX-A-12 | C.15, D.2 | RGPD Art. 17 | 🔴 | — | A-10 |
| FLUX-T-01 | — (mécanisme système) | RM-25, RM-25b, RM-29 | 🔴 | C-10, C-11 | A-07, A-08, A-11 |
