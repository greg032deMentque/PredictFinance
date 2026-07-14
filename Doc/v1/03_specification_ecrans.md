# 03 — Spécification écran par écran (V1)

> **Propriétaire de** : pour **chaque écran**, sa question dominante, ses données affichées, **chaque action utilisateur**, ses états (vide / chargement / erreur / non-exécutable), ses **relations** entrantes et sortantes, et ses règles verrouillées.
> Enums et wording → [02](02_glossaire_et_taxonomies.md). Règles métier (RM-xx) → [01](01_specification_produit.md). Données/API → [05](05_contrats_donnees_api.md). État réel du front → [06](06_ecarts_doc_code.md).

---

## A. Cadre commun à tous les écrans

### A.1 Contrat de page

1. **Une question dominante** par page. La page y répond avant tout.
2. **Une action primaire** au plus. Les actions secondaires ne concurrencent pas la primaire.
3. **Ordre visuel imposé** : (1) ce que c'est → (2) ce que le produit sait → (3) ce que ça veut dire → (4) ce que l'utilisateur peut faire ensuite.
4. **Recommandation en aval** : visuellement après la lecture marché, la lecture support et la vérité PEA (RM-09). Elle n'écrase jamais le reste.
5. **Détenue / non détenue explicite** partout où une recommandation apparaît (RM-10).
6. **Responsive** : desktop / tablette / mobile peuvent changer la densité, **jamais** le sens (RM consigne).

### A.2 Familles d'états (obligatoires sur toute page canonique)

| État | Définition | Ne pas confondre avec |
|---|---|---|
| **Vide** | Pas encore de donnée pour cet utilisateur (ex. watchlist vide). | Erreur. |
| **Chargement** | Données en cours d'assemblage. | Vide. |
| **Erreur récupérable** | Échec technique temporaire, action « réessayer ». | État métier. |
| **Non-exécutable métier** | État de premier rang : l'analyse/la requête est comprise mais non exécutable (`NoCrediblePattern`, `InsufficientData`, `UnsupportedInstrument`, `UnsupportedContext`…) (RM-24). | Erreur technique. |

### A.3 Architecture d'information & routes réelles

```
Anonymous (non authentifié — aucun shell métier, RM-21)
  /login                         Connexion
  /forgot-password               Mot de passe oublié
  /reset-password                Réinitialisation
  /register                      Inscription [CIBLE 🔴]

User (espace /client, guards Auth+Client)
  /client/dashboard              Home / centre de décision        ◄ route d'atterrissage User
  /client/watchlist              Watchlist
  /client/portfolio              Portefeuille
  /client/analysis               Lancement d'analyse
  /client/analysis/:analysisId   Résultat d'analyse (détail)
  /client/instruments/:symbol    Détail instrument
  /client/history                Historique
  /client/simulation             Simulation
  /client/notifications          Notifications
  /client/account/profile        Compte — profil
  /client/account/security       Compte — sécurité
  [cible non encore routée]      parameter-detail · snapshot-comparison · learn · help-center · onboarding-empty

Admin (espace /admin, guards Auth+Admin)
  /admin/dashboard               Vue d'ensemble                   ◄ route d'atterrissage Admin
  /admin/users                   Utilisateurs (+ /add, /edit/:id)
  /admin/instrument-registry     Registre instruments
  /admin/pea-registry            Registre PEA
  /admin/scoring-policy          Politique de scoring
  /admin/parameter-dictionary    Dictionnaire de paramètres (+ /detail/:parameterId)
  /admin/wording-versions        Versions de wording (+ /detail/:wordingVersionId)
  /admin/snapshot-audit          Audit de snapshots (+ /detail/:analysisRunId, /compare)
  /admin/data-quality            Qualité des données
  [cible non encore routée]      admin-signal-quality · admin-engagement (pilotage KPI)
```

> **Légende de statut** (rappelée sous chaque écran) : 🟢 construit · 🟡 partiel · 🔴 cible non construite. Détail en [06](06_ecarts_doc_code.md).

---

## B. Espace Anonymous

### B.1 — Connexion (`/login`) 🟢

- **Question dominante** : comment entrer dans le bon espace ?
- **Objectif** : authentifier et router vers l'espace cohérent selon le rôle (RM-22).

**Données / champs visibles**
- Identité produit (logo, nom, baseline rassurante).
- Formulaire : **email**, **mot de passe**, (option) « se souvenir de moi ».
- Lien « Mot de passe oublié ».
- Aucune navigation métier user/admin (RM-21).

**Actions**
| Action | Type | Effet |
|---|---|---|
| Se connecter | **Primaire** | Authentifie ; si succès, route selon rôle. |
| Ouvrir « mot de passe oublié » | Secondaire | → `B.2`. |

**États**
- Chargement : authentification en cours.
- Erreur récupérable : identifiants invalides / échec auth temporaire.
- Non-exécutable métier : compte existant mais accès indisponible dans ce contexte (ex. `DISABLED`).

**Relations**
- **Sortantes** : `User` → `/client/dashboard` (`C.1`) ; `Admin` → `/admin/dashboard` (`D.1`) ; lien → `/forgot-password` (`B.2`).
- **Entrantes** : route par défaut et fallback `**` ; déconnexion depuis tout écran.

**Règles verrouillées** : pas de « bascule admin » en mode anonyme — l'admin est un **espace post-login distinct**, pas un toggle.

---

### B.2 — Mot de passe oublié (`/forgot-password`) 🟢

- **Question dominante** : comment démarrer la récupération simplement ?
- **Objectif** : initier la récupération de façon calme et rassurante.

**Données / champs** : explication de la procédure ; champ **email** ; retour à la connexion.

**Actions**
| Action | Type | Effet |
|---|---|---|
| Envoyer la demande | **Primaire** | Déclenche l'envoi de l'email de récupération. |
| Retour à la connexion | Secondaire | → `B.1`. |

**États** : chargement (envoi) ; erreur récupérable (envoi temporairement impossible) ; non-exécutable (identité inconnue gérée **sans divulguer** l'existence du compte).

**Relations** : entrante depuis `B.1` ; sortante → `B.1` ; l'email mène à `B.3`.

**Règle verrouillée** : la réponse ne révèle **jamais** si le compte existe.

---

### B.3 — Réinitialisation (`/reset-password`) 🟢

- **Question dominante** : comment définir un nouveau mot de passe en sécurité ?
- **Objectif** : finaliser la récupération avec un parcours clair et à faible friction.

**Données / champs** : nouveau mot de passe ; confirmation ; lien d'annulation/retour.

**Actions**
| Action | Type | Effet |
|---|---|---|
| Enregistrer le nouveau mot de passe | **Primaire** | Valide la concordance et applique le nouveau mot de passe. |
| Annuler / retour | Secondaire | → `B.1`. |

**États** : chargement (enregistrement) ; erreur récupérable (échec temporaire) ; non-exécutable (lien expiré / contexte de reset invalide).

**Relations** : entrante via le lien email (`B.2`) ; sortante → `B.1` après succès.

**Règle verrouillée** : `newPassword` et `confirmPassword` doivent concorder à la validation ; la gestion du cycle de vie du token est technique, pas un wording produit.

---

### B.4 — Inscription (`/register`) 🔴 *(cible — non construit)*

- **Question dominante** : comment créer un compte en quelques secondes, sans friction, sans donnée bancaire ?
- **Objectif** : convertir un visiteur anonyme en compte `PENDING`, puis `ACTIVE` après confirmation email.

**Données / champs visibles**
- Identité produit (logo, baseline rassurante).
- Formulaire : **email**, **mot de passe**, **confirmation mot de passe**.
- Case à cocher : acceptation des **CGU** et de la **politique de confidentialité** (liens obligatoires — [08](08_analyse_critique_et_legal.md)).
- Retour vers `/login` (`B.1`).
- Aucun champ bancaire, aucun numéro de téléphone, aucune vérification d'identité.

**Actions**
| Action | Type | Effet |
|---|---|---|
| Créer le compte | **Primaire** | Valide les champs → crée le compte `PENDING` → envoie email de confirmation. |
| Retour à la connexion | Secondaire | → `B.1`. |

**États**
- Chargement : création en cours.
- Erreur récupérable : service temporairement indisponible.
- Non-exécutable métier (plusieurs sous-cas distincts) :
  - **Email déjà enregistré** : réponse identique sans révéler l'existence du compte (posture sécurité, identique à `B.2`).
  - **Mot de passe trop faible** : critères visibles dès la saisie (feedback inline).
  - **Champs discordants** (`password ≠ confirmPassword`) : validation frontend avant soumission.
  - **CGU non cochées** : bouton désactivé, message d'invite.
  - **Token de confirmation expiré** (retour depuis l'email) : proposition de renvoi d'un nouveau lien.

**Relations**
- **Entrante** : lien CTA depuis `B.1` (`« Pas encore de compte ? S'inscrire »`).
- **Sortantes** : `B.1` (après envoi de l'email de confirmation) ; activation → `B.1` → `C.1` via FLUX-C-02.

**Règles verrouillées**
- Les **CGU, politique de confidentialité et mentions légales** doivent exister avant que cette page soit ouverte en production ([08](08_analyse_critique_et_legal.md), pré-requis légaux).
- Compte créé en statut `PENDING` — **accès refusé** à toute route protégée tant que la confirmation n'est pas faite.
- **Aucun prélevement de données sensibles** à l'inscription : email + mot de passe seulement. Aucune synchronisation bancaire (périmètre produit, RM-24).

**Référence flux** : FLUX-C-01 dans [07](07_flux_metier_client_admin.md).

---

## C. Espace User

### C.1 — Home / centre de décision (`/client/dashboard`) 🟢 *(= « user-home » canonique)*

- **Question dominante** : qu'est-ce qui mérite mon attention maintenant ?
- **Objectif** : agir comme **centre de décision quotidien** dès la connexion. Ce n'est **pas** une page de menu générique.

**Données / zones visibles** (ordre mobile imposé)
1. Contexte utilisateur courant.
2. **Bloc priorités** (urgent en premier).
3. **Implications des positions détenues** (résumé orienté portefeuille).
4. **Analyses récentes**.
5. **Bloc non-évaluable / incomplet** (instruments hors périmètre, données manquantes…).
6. Prochaine meilleure action + raccourci pour lancer une analyse.

**Actions**
| Action | Type | Effet |
|---|---|---|
| Lancer une analyse | **Primaire** | → `C.4`. |
| Ouvrir watchlist / portefeuille / historique | Secondaires | → `C.2` / `C.3` / `C.9`. |
| Ouvrir aide / compte | Secondaires | → `C.13` / `C.15`. |
| Ouvrir un élément priorisé | Secondaire | route contextuelle (résultat, instrument…). |

**États** : vide (ni watchlist, ni portefeuille, ni historique → bascule vers onboarding `C.14`) ; chargement (assemblage) ; erreur récupérable (fetch partiel échoué) ; non-exécutable (certaines surfaces indisponibles pour ce contexte).

**Relations** : entrante post-login (`User`) ; sortantes vers la quasi-totalité des écrans user. Premier viewport = priorisation déjà visible.

---

### C.2 — Watchlist (`/client/watchlist`) 🟢

- **Question dominante** : quels instruments **non détenus** méritent mon attention ?
- **Objectif** : liste filtrée et priorisée de valeurs à suivre.

**Données par ligne**
- Identité de l'instrument.
- Résumé condensé **lecture marché**.
- Résumé condensé **support / PEA**.
- Résumé recommandation (verbe **non détenu** uniquement : `Surveiller`/`Attendre`/`Acheter`).
- Indice de fraîcheur/récence si pertinent.

**Actions**
| Action | Type | Effet |
|---|---|---|
| Lancer une analyse sur une valeur | **Primaire** | → `C.4` pré-rempli. |
| Filtrer / trier | Secondaires | réorganise la liste. |
| Ajouter une valeur | Secondaire | recherche + ajout (POST watchlist). |
| Retirer une valeur | Secondaire | suppression (DELETE watchlist). |
| Ouvrir le détail instrument | Secondaire | → `C.6`. |
| Ouvrir le résultat | Secondaire | → `C.5`. |

**États** : vide (aucune valeur ou aucun résultat pour le filtre) ; chargement ; erreur récupérable ; non-exécutable (valeur présente mais hors périmètre V1 → `UnsupportedInstrument`).

**Relations** : entrante depuis `C.1` et la nav ; sortantes → `C.4`, `C.5`, `C.6`, aide `C.13`.

**Règles verrouillées** : le résumé recommandation **n'efface pas** la distinction marché/support/PEA ; une ligne ne doit **jamais** paraître « positive » par la seule couleur ; verbes **non détenus** uniquement (c'est une liste de valeurs non détenues).

---

### C.3 — Portefeuille (`/client/portfolio`) 🟢

- **Question dominante** : qu'est-ce que cela signifie pour les positions que je **détiens** ?
- **Objectif** : aide à la décision sur positions détenues.

**Données par position**
- Identité de l'instrument.
- Quantité détenue (lignes ouvertes FIFO), **PRU dérivé** (RM-08).
- Résumé condensé **lecture marché** et **lecture support**.
- **Verbe détenu** uniquement : `Conserver`/`Renforcer`/`Alléger`/`Vendre`/`Attendre`.
- Résumé risque.

**Actions**
| Action | Type | Effet |
|---|---|---|
| Inspecter une position | **Primaire** | → `C.6` détail instrument. |
| Enregistrer une transaction (achat/vente) | Secondaire | formulaire transaction (POST). |
| Supprimer une transaction | Secondaire | DELETE transaction. |
| Ouvrir une simulation | Secondaire | → `C.8`. |
| Ouvrir l'historique | Secondaire | → `C.9`. |

**États** : vide (aucune position) ; chargement ; erreur récupérable ; non-exécutable (position existante mais support détaillé non productible).

**Relations** : entrante depuis `C.1`/nav ; sortantes → `C.6`, `C.8`, `C.9`.

**Règles verrouillées** : `Surveiller` n'est **jamais** la reco finale d'une position détenue ; le contrat détenue/non détenue reste explicite ; la page est orientée action mais **n'avale pas** la vérité analytique.

---

### C.4 — Lancement d'analyse (`/client/analysis`) 🟢

- **Question dominante** : comment lancer une analyse correctement ?
- **Objectif** : préparer et lancer une requête avec le bon contexte.

**Données / champs**
- Recherche / saisie de l'instrument.
- Choix de l'état de détention (détenue / non détenue) — posé **en amont** pour éviter une mauvaise interprétation de la reco.
- Aide contextuelle ; rappel de limite produit si pertinent.

**Actions**
| Action | Type | Effet |
|---|---|---|
| Analyser | **Primaire** | Lance l'analyse (POST run) → `C.5`. |
| Demander de l'aide | Secondaire | → `C.13`. |
| Partir vers un autre flux | Secondaire | nav. |

**États** : vide (aucune saisie) ; chargement (lancement) ; erreur récupérable (échec de lancement) ; non-exécutable (requête comprise mais non exécutable en V1).

**Relations** : entrante depuis `C.1`, `C.2`, nav ; sortante → `C.5` (résultat).

**Règle verrouillée** : page simple, ton calme et éducatif ; le contexte de détention est demandé **avant** l'analyse.

---

### C.5 — Résultat d'analyse (`/client/analysis/:analysisId`) 🟡 *(existe surtout comme sous-composant)*

- **Question dominante** : que conclut le produit pour cette analyse ?
- **Objectif** : présenter un résultat consolidé et compréhensible.

**Sections fixes (ordre imposé)**
1. **Bandeau d'issue** (`AnalysisOutcome` → wording FR, [02](02_glossaire_et_taxonomies.md#analysisoutcome)).
2. **Lecture marché** (avec **confiance expliquée**).
3. **Lecture support**.
4. **Lecture situation personnelle**.
5. **Rail de synthèse contextuel**.
6. **Plan d'action** (« Vos prochaines étapes »).
7. Liens de suivi (historique, détail paramètre, aide…).

**Champs lecture marché** : pattern principal visible · alternatifs compatibles · `PatternStatus` · confiance (si dispo) · **décomposition de la confiance en critères** (RM-27, voir bloc ci-dessous) · résumé/indice d'invalidation · indice de risque principal (si dispo).

**Bloc « confiance expliquée »** (RM-27) : sous le `ConfidenceLabel`, une mini-grille de critères dérivés de `detection` / `validation` / `invalidation` ([05](05_contrats_donnees_api.md#25-patternassessment)), chacun avec un état ✅ rempli / ⚠️ partiel / ❌ absent et un libellé gouverné. Exemple : « ✅ Impulsion claire · ✅ Consolidation ordonnée · ⚠️ Volume non confirmé · ❌ Pas encore de cassure ». N'augmente ni ne minore la confiance ; il l'explique.

**Champs lecture support** : disponibilité / score composite (si autorisé) · complétude support · statut PEA · conditions bloquantes ou partielles.

**Recommandation** : préserve explicitement **pas de position** vs **position existante** ; verbes selon contexte (RM-10).

**Bloc « plan d'action »** (RM-26) : 2-3 étapes concrètes **déterministes** reformulant des vérités déjà affichées — niveau à noter (depuis `riskHints.invalidationPrice`), horizon de revue (depuis `reviewHorizonDays`), alerte suggérée (seuil `riskHints` + déclencheur `LEVEL_CROSSED`), rappel d'action selon détention. **N'introduit aucun nouveau chiffre** ; chaque étape est traçable à son élément source.

**Actions**
| Action | Type | Effet |
|---|---|---|
| Activer une alerte sur un niveau | **Primaire** | crée une alerte `LEVEL_CROSSED` (RM-25) sur le seuil suggéré. |
| Ouvrir l'historique | Secondaire | → `C.9`. |
| Ouvrir le détail d'un paramètre | Secondaire | → `C.7`. |
| Ouvrir l'aide | Secondaire | → `C.13`. |
| Ouvrir le détail instrument | Secondaire | → `C.6`. |

**États** : chargement ; erreur récupérable ; non-exécutable (ex. `NoCrediblePattern`, `InsufficientData`, `UnsupportedInstrument`, support incomplet, PEA `Unknown` bloquant). En non-exécutable, le plan d'action propose une étape adaptée (ex. « revenez quand l'historique sera suffisant ») plutôt qu'un plan de trade.

**Relations** : entrante depuis `C.4`, `C.1`, `C.2`, notifications `C.12` (clic sur alerte) ; sortantes → `C.6`, `C.7`, `C.9`, `C.13`.

**Règles verrouillées** : issue d'abord ; marché avant support ; support avant recommandation ; la reco **n'avale pas** le reste ; les alternatifs **restent visibles** (RM-06) ; un bloc « pourquoi cette reco » doit rester traçable aux vérités visibles, **sans inventer** de vérité backend (RM-01) ; le **plan d'action** vient en dernier et ne reformule que des vérités déjà affichées (RM-26) ; la **confiance expliquée** explique sans recalculer (RM-27).

---

### C.6 — Détail instrument (`/client/instruments/:symbol`) 🟢

- **Question dominante** : comment comprendre cet instrument en profondeur ?
- **Objectif** : **page instrument centrale** de la V1.

**Résumé instrument** : nom · ticker · type d'actif · périmètre/marché · statut PEA · fraîcheur des données · disponibilité d'analyse.

**Lecture marché** : pattern principal · alternatifs · `PatternStatus` · confiance · **décomposition de la confiance en critères** (RM-27) · résumé validation/invalidation · niveau d'invalidation (si dispo) · indice risque/rendement (si dispo) · résumé pédagogique.

**Lecture support** : version de scoring · univers actif · statut PEA · scores par catégorie (si affichés) · ratio de couverture · score composite (si autorisé) · catégories/métriques manquantes (si pertinent).

**Accès lecture paramètre** : pour chaque paramètre visible, l'utilisateur peut atteindre les 4 couches (définition → lecture valeur → pourquoi → implication) (RM-16) → `C.7`. Les termes affichés portent un **glossaire inline** (info-bulle « ? ») tiré du dictionnaire gouverné (RM-28).

**Bloc « ce que ça signifie pour moi »** : branche **explicitement** sur l'état de détention, et se conclut par un **mini plan d'action** (RM-26) cohérent avec celui du résultat (`C.5`).

**Actions**
| Action | Type | Effet |
|---|---|---|
| Lancer une analyse sur l'instrument | **Primaire** | → `C.4`/`C.5`. |
| Activer une alerte sur l'instrument | Secondaire | crée une alerte `PATTERN_STATE_CHANGE` ou `LEVEL_CROSSED` (RM-25). |
| Ouvrir le détail d'un paramètre | Secondaire | → `C.7`. |
| Ouvrir une simulation | Secondaire | → `C.8`. |
| Ouvrir l'historique de l'instrument | Secondaire | → `C.9` filtré. |

**États** : chargement ; erreur récupérable ; non-exécutable ; lecture support partielle.

**Relations** : entrante depuis watchlist, portefeuille, résultat, notifications (clic sur alerte), historique ; sortantes → `C.4`, `C.7`, `C.8`, `C.9`.

**Règles verrouillées** : page centrale ; la reco **n'avale pas** la lecture marché ; alternatifs visibles ; deux contextes de détention explicites ; confiance expliquée et glossaire inline tirés de vérités gouvernées, jamais reconstruits côté frontend (RM-27, RM-28).

---

### C.7 — Détail paramètre (`parameter-detail`) 🔴 *(cible — non encore routé côté user)*

- **Question dominante** : que veut dire ce chiffre et comment le lire ?
- **Objectif** : interprétation pédagogique **d'un seul paramètre** (RM-16/17/18).

**Sections visibles** : nom du paramètre · définition simple · rôle dans la catégorie · comment lire la valeur courante · limites d'interprétation · ce que la valeur **soutient** · ce qu'elle **ne prouve pas** · implication **sans** position · implication **avec** position.

**Actions**
| Action | Type | Effet |
|---|---|---|
| Lire la signification | **Primaire** | consultation. |
| Revenir au contexte analytique précédent | Secondaire | → `C.5` ou `C.6`. |

**États** : chargement ; erreur récupérable ; non-exécutable (uniquement si le paramètre ne peut être rendu de façon signifiante).

**Relations** : entrante depuis `C.5`, `C.6` ; sortante → écran d'origine.

**Règles verrouillées** : forte lisibilité ; **un paramètre seul n'est jamais une reco finale** (RM-18) ; ton plus simple que le jargon ; tout le texte vient du **dictionnaire gouverné** backend (RM-17), gouverné en `D.6`.

---

### C.8 — Simulation (`/client/simulation`) 🟢

- **Question dominante** : que se passe-t-il si je teste un scénario ?
- **Objectif** : explorer un scénario **sans** le confondre avec la vérité persistée.

**Données** : entrées (prix d'entrée, taille, invalidation, cible, frais, état courant) ; sorties (baisse potentielle, hausse potentielle, ratio R/R) ; distinction explicite simulation ≠ historique.

**Actions**
| Action | Type | Effet |
|---|---|---|
| Modifier les entrées du scénario | **Primaire** | recalcule. |
| Inspecter le résultat simulé | Secondaire | consultation. |
| Revenir à un autre flux analytique | Secondaire | nav. |

**États** : vide (aucun scénario) ; chargement ; erreur récupérable ; non-exécutable (scénario non pris en charge en V1).

**Relations** : entrante depuis `C.3`, `C.6` ; sortantes → flux analytiques.

**Règles verrouillées** : la simulation **ne ressemble pas** à un historique persisté ; aucun wording laissant croire que la sortie simulée est une vérité historique observée ; **aucune prédiction de prix**.

---

### C.9 — Historique (`/client/history`) 🟢

- **Question dominante** : qu'est-ce qui a été persisté dans le temps ?
- **Objectif** : montrer les états analytiques persistés et rendre explicite leur nature de **snapshot** (RM-20).

**Données** : timeline ; identité du snapshot ; horodatage ; résumé du sens persisté ; route vers comparaison ; distinction historique ≠ lecture live courante.

**Actions**
| Action | Type | Effet |
|---|---|---|
| Inspecter une entrée | **Primaire** | → détail snapshot / `C.5`. |
| Comparer deux snapshots | Secondaire | → `C.10`. |

**États** : vide (aucun historique persisté) ; chargement ; erreur récupérable ; non-exécutable (historique conceptuel mais non rendu dans le périmètre courant).

**Relations** : entrante depuis `C.1`, `C.3`, `C.6`, `C.5` ; sortantes → `C.5`, `C.10`.

**Règles verrouillées** : se lit comme **vérité persistée**, pas comme reconstruction de l'état courant ; structure de timeline renforçant la nature datée.

---

### C.10 — Comparaison de snapshots (`snapshot-comparison`) 🔴 *(cible user — équivalent admin existe en `D.8`)*

- **Question dominante** : qu'est-ce qui a changé entre deux états persistés ?
- **Objectif** : comparer fiablement deux snapshots.

**Données** : résumé snapshot gauche · résumé snapshot droit · résumé des changements · retour de **non-comparabilité** si pertinent · guidage de lecture.

**Sorties** : ce qui a changé en lecture marché · en lecture support · en recommandation · pourquoi la comparaison peut être limitée.

**Actions**
| Action | Type | Effet |
|---|---|---|
| Comparer | **Primaire** | affiche le diff. |
| Retour à l'historique | Secondaire | → `C.9`. |

**États** : vide (pas de paire valide) ; chargement ; erreur récupérable ; non-exécutable (snapshots non comparables en conditions V1).

**Relations** : entrante depuis `C.9` ; sortante → `C.9`.

**Règles verrouillées** : aucune approximation frontend inventée ; distinguer **cause** et **conséquence** ; la non-comparabilité est **explicite**, jamais masquée.

---

### C.11 — Learn (`learn`) 🔴 *(cible — non construit)*

- **Question dominante** : comment comprendre les concepts produit hors flux d'exécution ?
- **Objectif** : contenu éducatif sans le mélanger aux écrans orientés action.

**Données** : points d'entrée par sujet · explications conceptuelles (périmètre V1, patterns, lecture support, PEA, limites) · retours vers les écrans de tâche.

**Actions** : parcourir un sujet (**primaire**) ; revenir aux flux produit (secondaire).

**Relations** : accessible depuis la nav et l'aide ; sortantes vers les écrans de tâche pertinents.

**Règle verrouillée** : pédagogique, calme, non urgent ; **ne remplace pas** l'aide contextuelle en cours de tâche. **Pas de doublon avec le glossaire inline** (RM-28) : `Learn` porte le contenu conceptuel **long** ; le glossaire inline (info-bulles, présent sur les écrans de tâche comme `C.6`/`C.7`) répond au **doute ponctuel** au moment où il survient. Les deux puisent dans le dictionnaire gouverné (RM-17).

---

### C.12 — Notifications & alertes (`/client/notifications`) 🟢

- **Question dominante** : qu'est-ce qui requiert mon attention et où aller ?
- **Objectif** : couche de **priorisation et de routage** (RM-23), incluant les **alertes proactives** générées par la boucle de feedback (RM-25).

**Données** : liste unifiée notifications + alertes ; chaque item porte sa catégorie/statut et, pour les alertes, son **déclencheur** (`AlertTrigger` : changement d'état, niveau franchi, données obsolètes — [02](02_glossaire_et_taxonomies.md#d%C3%A9clencheurs-dalerte)) ; filtre par catégorie/statut/déclencheur ; route claire depuis chaque item ; gestion lu/non-lu.

**Actions**
| Action | Type | Effet |
|---|---|---|
| Ouvrir l'écran lié | **Primaire** | route selon `NotificationTargetScreen` (ex. alerte de niveau → `C.6`, changement d'état → `C.5`). |
| Filtrer | Secondaire | par catégorie / statut / déclencheur. |
| Marquer comme traité | Secondaire | bascule lu/non-lu. |
| Gérer mes alertes | Secondaire | → préférences de notification en `C.15` (pas ici). |

**États** : vide (aucune notification/alerte) ; chargement ; erreur récupérable ; non-exécutable (item existant mais destination non exécutable dans le périmètre courant).

**Relations** : entrante : alertes générées par la boucle de feedback (RM-25) et notifications produit ; sortantes → `InstrumentDetail` (`C.6`), `AnalysisResult` (`C.5`), `HelpCenter` (`C.13`), `Account` (`C.15`).

**Règles verrouillées** : notifications et alertes **routent, n'expliquent pas** ; la **page de destination** détient la vérité (RM-25b) ; une alerte **n'est pas une prédiction** ; dédoublonnage par (instrument × déclencheur × jour) ; ce n'est **pas** un paramétrage de compte — les préférences/activation d'alertes se gèrent en `C.15` (RM-23).

---

### C.13 — Centre d'aide (`help-center`) 🔴 *(cible — non construit)*

- **Question dominante** : comment comprendre le produit, ses limites et mon doute du moment ?
- **Objectif** : explication contextuelle + routage vers la bonne surface de tâche (RM-23).

**Données** : concepts produit · limites produit · routes des doutes fréquents vers les écrans de tâche · explications contextuelles (sens d'un résultat, d'un historique, d'une comparaison, des flux compte/aide).

**Actions** : ouvrir un sujet d'aide (**primaire**) ; revenir à l'écran de tâche (secondaire) ; comprendre les limites produit (secondaire).

**Relations** : accessible depuis la plupart des écrans et depuis les notifications ; sortantes → écrans de tâche.

**Règles verrouillées** : l'aide **explique**, elle **ne remplace pas** la page de tâche ; contextuelle quand possible ; déterministe, pas un chat (RM-23).

---

### C.14 — Onboarding (vide) (`onboarding-empty`) 🔴 *(cible — non construit)*

- **Question dominante** : comment obtenir une première valeur quand je n'ai encore (presque) aucune donnée ?
- **Objectif** : guider le premier usage.

**Données** : explication de la première valeur · premières étapes suggérées · **2-3 exemples d'actions FR connues** pour une démonstration immédiate (ex. « Analysez TotalEnergies pour voir comment ça marche ») · route vers watchlist / analyse / portefeuille selon le contexte.

**Actions** : suivre la suggestion d'onboarding (**primaire**, lance une analyse d'exemple → `C.5`) ; aller vers la première tâche utile (secondaire).

**Relations** : déclenché depuis `C.1` quand l'utilisateur n'a ni watchlist, ni portefeuille, ni historique ; sortantes → `C.2`/`C.3`/`C.4`/`C.5`.

**Règle verrouillée** : concret, **pas** de remplissage motivationnel vague ; **première valeur en < 30 s** ; une première action claire. Les exemples restent dans le périmètre V1 (actions FR) — jamais d'ETF/crypto (RM-24).

---

### C.15 — Compte (`/client/account/profile` + `/client/account/security`) 🟢

- **Question dominante** : comment gérer mes informations et préférences ?
- **Objectif** : **self-service** pour l'utilisateur courant. **Ce n'est pas** la gestion admin des utilisateurs.

**Sous-écran Profil** (`/profile`) : profil (nom, email…) · préférences · **préférences de notification et d'alertes** (activation par catégorie/déclencheur, [02](02_glossaire_et_taxonomies.md#d%C3%A9clencheurs-dalerte)).
**Sous-écran Sécurité** (`/security`) : résumé sécurité (`hasPassword`, dernière modification…) · changement de mot de passe · routes de récupération utiles.

**Actions**
| Action | Type | Effet |
|---|---|---|
| Modifier son profil | **Primaire (profil)** | met à jour le profil. |
| Gérer notifications & alertes | Secondaire | active/désactive par catégorie ; conditionne les alertes proactives (RM-25b). |
| Changer son mot de passe | **Primaire (sécurité)** | met à jour le mot de passe. |
| Ouvrir le centre de notifications | Secondaire | → `C.12`. |

**États** : chargement ; erreur récupérable ; non-exécutable selon contexte.

**Relations** : entrante depuis `C.1`, nav, notifications ; sortantes → `C.12`, sécurité ↔ profil.

**Règle verrouillée** : segmentation explicite profil / préférences / notifications / sécurité ; **aucune action admin ici** ; les **préférences et l'activation des alertes** se gèrent **ici**, pas dans le centre de notifications (RM-23) ; si une catégorie est désactivée, aucune alerte de cette catégorie n'est générée (RM-25b).

---

## D. Espace Admin

> Posture : **gouvernance de la vérité**, jamais une simple utilité technique. Aucune page admin ne masque la traçabilité pour la commodité. Le front admin est globalement **plus avancé** que ne le décrivait l'ancienne doc (détails, comparaison) — voir [06](06_ecarts_doc_code.md).

### D.1 — Vue d'ensemble (`/admin/dashboard`) 🟡 *(compteurs présents ; cartes de tendance à construire)*

- **Question dominante** : qu'est-ce qui requiert gouvernance ou attention opérationnelle ?
- **Objectif** : tableau d'entrée admin **+ pilotage de haut niveau**.
- **Données** :
  - **4 cartes de tendance** (une par famille de KPI, RM-29), avec sparkline 30 j et variation vs. période précédente : *Qualité signaux* (taux d'atteinte de cible) · *Engagement* (utilisateurs actifs WAU + rétention J+7) · *Activation* (taux de complétion du funnel) · *Santé* (taux d'échec d'analyse + couverture PEA).
  - Compteurs de gouvernance existants (utilisateurs, analyses, PEA, paramètres publiés…).
  - Priorités admin et routes vers les surfaces.
- **Actions** : ouvrir les écrans de pilotage (`D.10`) · gestion utilisateurs (`D.2`) · registres (`D.3`/`D.4`) · politique (`D.5`/`D.6`/`D.7`) · audit/qualité (`D.8`/`D.9`).
- **États** : chaque carte respecte la disponibilité KPI ([02](02_glossaire_et_taxonomies.md#disponibilit%C3%A9-dun-kpi)) — « données insuffisantes » / « fenêtre trop récente » sont des états métier, pas des erreurs.
- **Relations** : route d'atterrissage `Admin` ; sortantes vers toutes les surfaces admin, dont `D.10`.

### D.2 — Utilisateurs (`/admin/users`, `/add`, `/edit/:id`) 🟢

- **Question dominante** : comment gérer les utilisateurs structurellement ?
- **Données par utilisateur** : identité · rôle ([02](02_glossaire_et_taxonomies.md#userrole)) · statut · dernière activité (si pertinent).
- **Actions** : rechercher · filtrer · **créer un utilisateur** (`/add`) · **gérer/éditer** (`/edit/:id`).
- **États** : vide · chargement · erreur.
- **Règle verrouillée** : appartient à l'admin **seulement** ; le self-service utilisateur reste en `C.15`.

### D.3 — Registre instruments (`/admin/instrument-registry`) 🟢

- **Question dominante** : quelle est la vérité produit gouvernée pour chaque instrument ?
- **Données** : recherche/filtre · identité instrument · mapping fournisseur · appartenance à l'univers actif · état support · état de fraîcheur.
- **Actions** : rechercher · filtrer · inspecter une ligne.
- **Règle verrouillée** : clarté de gouvernance d'abord ; pas de masquage de traçabilité.

### D.4 — Registre PEA (`/admin/pea-registry`) 🟢

- **Question dominante** : quelle est la vérité PEA gouvernée pour chaque instrument ?
- **Données** : identité · statut PEA · type/référence de source · date de vérification · version de politique · notes / historique de statut si utile ([02](02_glossaire_et_taxonomies.md#peaeligibilitystatus)).
- **Actions** : inspecter · rechercher/filtrer · (gouverner le statut plus tard, quand les workflows d'édition seront implémentés).
- **Règles verrouillées** : `Unknown` reste **visiblement distinct** de `ConfirmedIneligible` ; une vérité manquante n'est **jamais** rendue comme éligibilité implicite (RM-15).

### D.5 — Politique de scoring (`/admin/scoring-policy`) 🟢

- **Question dominante** : quelles règles de scoring sont actives ?
- **Données** : version de scoring active · univers actifs · catégories actives · règles d'inclusion des métriques · règles de couverture · historique des versions.
- **Actions** : inspecter la politique · comparer des versions plus tard.

### D.6 — Dictionnaire de paramètres (`/admin/parameter-dictionary`, `/detail/:parameterId`) 🟢

- **Question dominante** : quelle vérité de wording et d'interprétation existe pour chaque paramètre ?
- **Objectif** : exposer le **dictionnaire gouverné** alimentant la pédagogie des paramètres (`C.7`).
- **Données par paramètre** : id stable · libellé UI · définition simple · définition avancée (si utile) · catégorie · sémantique de direction de lecture · garde-fous d'interprétation · limites · ce que le paramètre **ne prouve pas** · gabarits d'implication **sans** position · gabarits d'implication **avec** position · statut de version de wording.
- **Actions** : liste + **détail** (`/detail/:parameterId`).
- **Règle verrouillée** : gouvernance d'abord ; **aucun wording frontend hors de la zone gouvernée** (RM-17).

### D.7 — Versions de wording (`/admin/wording-versions`, `/detail/:wordingVersionId`) 🟢

- **Question dominante** : quel wording pédagogique et de recommandation est actif ?
- **Données** : jeu de verbes d'action · forces de recommandation · codes de scénario de conseil · résumé des gabarits de texte déterministes · état de publication.
- **Actions** : inspecter une version · **détail** (`/detail/:wordingVersionId`) · vérifier l'état de publication.
- **Règle verrouillée** : gouvernance de wording traçable ; les cartes de résumé n'inventent pas un sens différent du wording gouverné.

### D.8 — Audit de snapshots (`/admin/snapshot-audit`, `/detail/:analysisRunId`, `/compare`) 🟢

- **Question dominante** : qu'est-ce qui a été persisté exactement, et sous quel contexte de version ?
- **Données** : identité snapshot · horodatage · versions de règles · résumé payload lecture marché · résumé payload lecture support · résumé payload recommandation · outils de comparaison d'audit.
- **Actions** : inspecter (`/detail/:analysisRunId`) · **comparer** (`/compare`).
- **Règle verrouillée** : auditabilité d'abord ; lien vers le modèle de vérité persistée ; pas d'abstraction de commodité masquant le contexte de version.

### D.9 — Qualité des données (`/admin/data-quality`) 🟢

- **Question dominante** : où la vérité produit est-elle dégradée ou incomplète ?
- **Données** : métriques manquantes par catégorie · instruments non pris en charge ou obsolètes · problèmes de fraîcheur fournisseur · incomplétude du registre PEA · tendances de dégradation de couverture · lien d'impact utilisateur · prochaine action admin suggérée.
- **Actions** : inspecter · filtrer · ouvrir la surface produit affectée · aller à la vue registre impactée.
- **Règles verrouillées** : distinguer fraîcheur source vs règles de scoring ; distinguer incomplétude de registre vs univers non pris en charge ; prioriser par impact utilisateur probable.

### D.10 — Pilotage (KPI)

> Capacité de gouvernance V1 (RM-29). Posture inchangée : formule explicite, source traçable, **aucune vérité métier inventée** — les KPI agrègent la vérité déjà persistée. États : tout KPI respecte la **disponibilité KPI** ([02](02_glossaire_et_taxonomies.md#disponibilit%C3%A9-dun-kpi)). Le détail des formules et sources est dans [05 §3.bis](05_contrats_donnees_api.md#3bis-kpi--familles-formules-et-sources).

#### Les 4 familles de KPI

| Famille | Question | KPI principaux | Écran |
|---|---|---|---|
| **A. Qualité des signaux** (ex post) ⭐ | Le moteur a-t-il raison dans le temps ? | taux de confirmation / d'invalidation · **taux d'atteinte de cible** (`SignalOutcome`) · **calibration de la confiance** (atteinte par bucket `LOW/MEDIUM/HIGH`) · perf. modèle par version · perf. par pattern | `D.10a` |
| **B. Engagement & rétention** | Les utilisateurs reviennent-ils ? | inscriptions/jour · DAU/WAU/MAU · stickiness · **rétention par cohorte** (J+1/J+7/J+30) · analyses par actif · taux de lecture des notifications | `D.10b` |
| **C. Usage & funnel** | Les utilisateurs s'activent-ils et que consomment-ils ? | **funnel d'activation** (inscription → 1ère watchlist → 1ère analyse → 1ère transaction) · patterns/instruments les plus analysés · distribution des issues · écrans les plus visités (logs `Analytic`) | `D.10b` |
| **D. Santé opérationnelle & data** | Où la vérité est-elle dégradée ? | taux d'échec d'analyse · latence p50/p95 · couverture PEA · complétude des snapshots · fraîcheur des données | `D.1` + `D.9` (tendances) |

#### D.10a — Qualité des signaux (`admin-signal-quality`) 🔴 *(cible — non construit)*

- **Question dominante** : le moteur a-t-il raison dans le temps ?
- **Données / sections** : taux d'atteinte de cible global + courbe ; **tableau de calibration de la confiance** (`LOW/MEDIUM/HIGH` vs. réalité `SignalOutcome`) ; **performance par pattern** (`PatternId` × taux d'atteinte) ; **courbe de performance modèle** par `ModelVersion` (Precision/F1/RocAuc).
- **Actions** : filtrer par période / pattern / version de règles (**primaire**) ; ouvrir un snapshot d'audit → `D.8`.
- **États** : disponibilité KPI (ex. `KPI_WINDOW_TOO_YOUNG` si la fenêtre d'évaluation ex post n'est pas écoulée).
- **Relations** : entrante depuis `D.1` ; sortante → `D.8` (audit), `D.5` (politique de scoring, si recalibration nécessaire).
- **Règles verrouillées** : décisions traçables — si « confiance HIGH » n'atteint pas la cible plus souvent que « LOW », le scoring est mal calibré et doit être révisé en `D.5` ; aucun KPI ne modifie la vérité persistée.

#### D.10b — Engagement & activation (`admin-engagement`) 🔴 *(cible — non construit)*

- **Question dominante** : les utilisateurs reviennent-ils et progressent-ils ?
- **Données / sections** : inscriptions/jour ; DAU/WAU/MAU + stickiness ; **grille de rétention par cohorte** (heatmap) ; **funnel d'activation** (entonnoir) ; usage du contenu (patterns/instruments) ; écrans les plus visités (logs `Analytic`) ; taux de lecture des notifications/alertes.
- **Actions** : filtrer par cohorte / période (**primaire**) ; exporter.
- **États** : disponibilité KPI (cohorte trop jeune pour J+30 → `KPI_WINDOW_TOO_YOUNG`).
- **Relations** : entrante depuis `D.1`.
- **Règles verrouillées** : définition de « actif » canonique et fixée ([06](06_ecarts_doc_code.md#4-d%C3%A9cisions-%C3%A0-arbitrer)) ; les KPI nominatifs respectent la règle d'anonymisation (RM-29b).

---

## E. Règles transversales verrouillées (récapitulatif)

1. Une même vérité métier garde la **même famille de wording** sur toutes les pages.
2. Tout texte visible utilisateur est en **français** ; le nommage interne reste en anglais sans fuiter brut.
3. La recommandation reste **visuellement en aval** de la vérité analytique.
4. La distinction **détenue / non détenue** reste explicite partout où une reco existe.
5. Historique et comparaison utilisent la **vérité persistée** des snapshots.
6. Les pages admin **ne masquent pas** la traçabilité.
7. **Aucun écran** n'implique le support runtime ETF en V1.
8. Le wording de résumé reste traçable à la même famille de verbes que la page source.
9. Le mobile simplifie la densité, **jamais** le sens.
10. `Notifications` route, `Help` explique, `Account` configure (self), `Admin` gouverne — sans empiétement (RM-23).
11. Le **plan d'action** et la **confiance expliquée** reformulent/expliquent des vérités déjà calculées, **sans en inventer** (RM-26, RM-27).
12. Une **alerte** route et n'explique pas, n'est **pas une prédiction**, et respecte les préférences utilisateur (RM-25, RM-25b).
13. Le **glossaire inline** et le `Learn` puisent dans le **dictionnaire gouverné**, sans doublon ni texte frontend libre (RM-28).
14. Tout **KPI** a une formule explicite et une source traçable, **n'invente aucune vérité**, et expose un état de disponibilité (RM-29).

---

## F. Écrans support exclus de cette spec

Artefacts non canoniques (conservés en `_legacy` pour référence) : `design-system.html`, `page-states.html`, `ARCHITECTURE_REVIEW.md`, `UI_DISPLAY_OBJECTS.ts.md`, `UX_UI_EVOLUTION_MATRIX.md`, `PERSONAS.md`, `UX_SCORED_MATRIX.md`, `DIRECT_CORRECTIONS_APPLIED.md`.
