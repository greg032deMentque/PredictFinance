# 10 — Personas et profils utilisateurs (V1)

> **Propriétaire de** : personas, profils enrichis, mental models, freins, sessions types, critères succès/échec, parties prenantes. Ce document **fait autorité** sur le contenu de [01 §5](01_specification_produit.md#5-personas-et-parties-prenantes) — `01 §5` est désormais un renvoi vers ce fichier. Les enums de rôle sont dans [02](02_glossaire_et_taxonomies.md#userrole). Les flux détaillés sont dans [07](07_flux_metier_client_admin.md). Les parcours de navigation sont dans [11](11_carte_navigation_et_parcours.md).
>
> **Convention de lecture** : chaque persona est une **archétype**, pas un segment statistique. L'objectif est de rendre lisible le comportement attendu pour une équipe produit, technique ou un agent IA analysant les specs.
>
> **Décision A-01 fermée** : `Admin` est l'unique rôle d'administration actif en V1 et porte le persona produit `PERSONA-A01`. L'administrateur a accès à l'intégralité des flux admin.

---

## A. Conventions et table de synthèse

### A.1 Identifiants

| ID persona | Libellé court | Rôle système | Espace | Niveau expertise | Objectif primaire |
|---|---|---|---|---|---|
| `PERSONA-ANON` | Le Visiteur Anonyme | — (non authentifié) | `/login`, `/forgot-password`, `/reset-password` | N/A | Comprendre le produit ; créer un compte |
| `PERSONA-U01` | L'Investisseur Découvrant | `User` | `/client/*` | Faible | Lancer une première analyse et la comprendre |
| `PERSONA-U02` | L'Investisseur Actif | `User` | `/client/*` | Intermédiaire | Surveiller ses positions et détecter des opportunités |
| `PERSONA-A01` | L'Administrateur | `Admin` | `/admin/*` | Expert (domaine admin + gouvernance) | Gouverner la vérité, piloter la qualité, gérer les accès |

### A.2 Lecture des parcours

Chaque persona référence un ou plusieurs `PARCOURS-xxx` définis dans [11 §I](11_carte_navigation_et_parcours.md#i-parcours-par-persona-11-parcours). Un parcours est une séquence d'écrans avec objectif, préconditions et issues.

---

## B. PERSONA-ANON — Le Visiteur Anonyme

### Profil

| Champ | Valeur |
|---|---|
| Contexte d'arrivée | Recommandation d'un ami, article de presse, réseau social, recherche Google |
| Session type | 2–5 minutes, sur ordinateur ou mobile |
| Authentification | Aucune — aucun shell produit visible (RM-21) |
| Connaissance de l'application | Zéro à faible |

### Mental model

> « Je veux savoir ce que fait ce site avant de créer un compte. Est-ce que c'est sérieux ? Est-ce facile à utiliser ? »

Le visiteur arrive avec trois questions implicites :
1. **Confiance** : à qui est destiné ce produit ? est-ce fiable ?
2. **Valeur** : qu'est-ce que je vais pouvoir faire concrètement ?
3. **Friction** : l'inscription est-elle rapide ou compliquée ?

### Objectifs

- Comprendre la proposition de valeur en moins de 60 secondes.
- Créer un compte si convaincu.
- Retrouver son mot de passe s'il est déjà inscrit mais l'a oublié.

### Freins et peurs

- Méfiance vis-à-vis des applications financières qu'il ne connaît pas.
- Peur que l'application soit une interface de trading avec passage d'ordre (il ne veut pas ça).
- Frustration si l'inscription est longue ou demande des informations bancaires.

### Blocage structurel actuel

> ⚠️ **[CIBLE 🔴]** L'écran `B.4` (inscription) **n'existe pas**. Il n'y a pas de route `/register`. FLUX-C-01 est documenté comme comportement cible mais n'est pas construit. Le visiteur ne peut actuellement pas créer un compte en autonomie → **risque d'acquisition zéro en production**.

### Session type

```
PARCOURS-ANON-01 — Découverte et inscription [LACUNE B.4]
Voir : 11 §I
```

---

## C. PERSONA-U01 — L'Investisseur Découvrant

### Profil

| Champ | Valeur |
|---|---|
| Profil démographique (fictif) | Camille, 34 ans, cadre en reconversion, vient d'ouvrir un PEA |
| Contexte financier | A épargné 10 000 €, veut commencer à investir en bourse, jamais investi seul |
| Expertise bourse | Faible : sait ce qu'est une action et un PEA, ne lit pas un graphique |
| Expertise application | Nulle : premier usage, découvre l'interface |
| Fréquence de connexion | Quotidienne pendant les 2 premières semaines, puis hebdomadaire |

### Mental model

> « J'ai de l'argent sur mon PEA. J'entends parler de TotalEnergies. Est-ce que c'est le bon moment d'acheter ? Et pourquoi ? »

Camille ne cherche pas à "trader" — elle cherche **une méthode et des explications**. Elle veut comprendre ce qu'elle fait, pas juste suivre un signal aveugle. Ses attentes :
- Langage simple (pas de jargon Bloomberg).
- Justification explicite de chaque conclusion.
- Confiance que l'application ne va pas lui faire perdre de l'argent "par magie".

### Objectifs

1. Trouver et analyser une première action française.
2. Comprendre le résultat : que signifie ce pattern ? pourquoi ce niveau de confiance ?
3. Décider quoi faire : surveiller, acheter, attendre.
4. Enregistrer la valeur dans sa watchlist pour la suivre.

### Freins et peurs

- **Jargon technique** : "Bull Flag", "invalidation", "score composite" → découragement si non expliqué.
- **Peur de se tromper** : « Est-ce que je vais acheter au mauvais moment ? »
- **Paralysie face à une home vide** : sans données, l'écran `C.1` est vide et ne guide pas.
- **Confiance fragile** : une seule mauvaise expérience (erreur, jargon non expliqué) suffit à l'abandonner.

### Critères de succès

- A lancé au moins une analyse et compris ce que signifie le résultat.
- A ajouté au moins un instrument à sa watchlist.
- Revient J+3 de son plein gré.

### Critère d'échec (décrochage J+1)

> L'investisseur arrive sur `/client/dashboard` sans données. La home est vide. Il ne sait pas quoi faire. Il quitte l'application. **Cause directe : `C.14` (onboarding vide) non construit [CIBLE 🔴].**

### Sessions types

```
PARCOURS-U01-01 — Onboarding → première analyse [LACUNE C.14]
PARCOURS-U01-02 — Ajouter premier instrument à la watchlist
Voir : 11 §I
```

---

## D. PERSONA-U02 — L'Investisseur Actif

### Profil

| Champ | Valeur |
|---|---|
| Profil démographique (fictif) | Marc, 41 ans, ingénieur, utilise PredictFinance depuis 4 mois |
| Contexte financier | PEA avec 3 positions ouvertes, watchlist de 8 valeurs |
| Expertise bourse | Intermédiaire : comprend les tendances, suit son portefeuille, lit les ratios de base |
| Expertise application | Bonne : connaît les 4 lectures, a confiance dans le moteur |
| Fréquence de connexion | 3 à 5 fois par semaine, sessions courtes (10–15 min) |

### Mental model

> « Je veux vérifier si quelque chose a bougé sur mes positions et si une opportunité est apparue sur ma watchlist. »

Marc a intériorisé le modèle des 4 lectures. Il ne lit plus les descriptions pédagogiques à chaque analyse — il va directement au pattern status, au niveau de confiance et à la recommandation. Son rapport à l'application est devenu **instrumental** : un outil de veille et de support à la décision, pas un cours.

### Objectifs

1. Vérifier les alertes et notifications au démarrage.
2. Analyser une valeur de sa watchlist qui a bougé.
3. Relire un ancien snapshot pour comparer.
4. Enregistrer un achat/vente et voir l'impact sur son PRU et son contexte de détention.

### Freins et peurs

- **Fatigue des alertes** : trop de notifications peu importantes → désactivation des préférences.
- **Historique illisible** : si les snapshots sont listés sans synthèse, la comparaison est difficile.
- **Mouvements de marché rapides** : peur de manquer une alerte critique parce qu'elle était noyée dans d'autres.

### Critères de succès

- Alerte reçue → analyse relue → décision prise → transaction enregistrée : le cycle complet en moins de 15 minutes.
- Compare deux snapshots pour un instrument et comprend ce qui a changé.
- Confiance dans le moteur validée ex post (au moins un signal `TARGET_HIT` ou `INVALIDATION_HIT` historisé).

### Critère d'échec (churn)

> Marc a 6 signaux persistés. Aucun n'a reçu d'issue ex post (`STILL_OPEN` ou `NOT_EVALUABLE` partout). L'application ne lui prouve pas que le moteur a eu raison ou tort. **Il arrête de payer. Cause directe : évaluation ex post (C-11) non construite [CIBLE 🔴].**

### Note importante

> PERSONA-U02 **n'est pas une cible d'acquisition** — c'est la cible de **rétention**. Un U02 qui part représente 4–6 mois d'activation perdus. Les fonctionnalités P0 (plan d'action, confiance expliquée, ex post) visent à **transformer U01 en U02** et à garder U02.

### Sessions types

```
PARCOURS-U02-01 — Session quotidienne (alertes → analyse → décision)
PARCOURS-U02-02 — Analyse complète (instrument détenu, lecture support + PEA)
PARCOURS-U02-03 — Enregistrer une transaction + impact contexte détention
PARCOURS-U02-04 — Comparer deux snapshots (historique)
Voir : 11 §I
```

---

## E. PERSONA-A01 — L'Administrateur

> **Rôle système** : `Admin` — unique rôle technique d'accès à `/admin/*`. Décision A-01 fermée.

### Profil

| Champ | Valeur |
|---|---|
| Profil (fictif) | Sophie, 38 ans, data analyst dans l'équipe produit |
| Responsabilité | Vérité gouvernée : instruments, PEA, scoring, wording, snapshots, accès utilisateurs |
| Expertise | Excellente sur le domaine admin ; bonne compréhension du modèle métier |
| Fréquence de connexion | Quotidienne (5–10 min de contrôle qualité) + occasionnelle (maintenance) |
| Espace | `/admin/*` uniquement — n'utilise pas l'espace client en production |

### Mental model

> « Je suis la gardienne de la vérité. Si je ne maintiens pas les registres propres, les utilisateurs reçoivent de mauvaises analyses. »

Sophie distingue deux modes :
- **Mode veille** (quotidien) : vérifier que rien n'a dégradé (données stale, anomalies, instruments désynchronisés).
- **Mode gouvernance** (hebdomadaire/occasionnel) : ajouter un instrument, mettre à jour une politique, publier un wording.

Elle n'invente pas — elle gouverne. Chaque action laisse une trace versionnée.

### Objectifs

1. S'assurer qu'aucune anomalie de qualité ne passe inaperçue.
2. Mettre à jour les registres instruments et PEA quand les données sources changent.
3. Faire évoluer la politique de scoring sans casser les analyses existantes.
4. Auditer un snapshot suspect pour expliquer un résultat à un utilisateur.
5. Gérer les comptes utilisateurs (ajout, désactivation, suppression RGPD Art. 17).

### Freins et peurs

- **Données stale non détectées** : si le fournisseur de données est en retard et que l'app ne l'affiche pas, les analyses sont basées sur des données obsolètes.
- **Effet de bord d'un changement de politique** : modifier le scoring peut rendre les snapshots anciens incomparables → importance des versions (`policyVersion`).
- **Wording incohérent** : une mise à jour de texte sans versionnage casse la traçabilité audit.
- **Action irréversible non tracée** : suppression de compte, rollback de politique — chaque action sensible doit laisser une entrée d'audit.

### Critères de succès

- Tableau de bord data quality sans anomalie rouge.
- Tous les instruments actifs ont une fraîcheur `FRESH`.
- Les politiques de scoring sont versionnées avec un `policyVersion` traçable.
- Les KPI admin (familles A/B/C/D) sont accessibles et traçables.
- Les suppressions de compte RGPD Art. 17 sont exécutables depuis l'espace admin (FLUX-A-13).

### Sessions types

```
PARCOURS-A01-01 — Contrôle qualité quotidien (data-quality + anomalies)
PARCOURS-A01-02 — Ajouter et configurer un instrument (registre + PEA + scoring)
PARCOURS-A01-03 — Publier une mise à jour de wording (dictionnaire → version)
PARCOURS-A01-04 — Auditer un snapshot suspect (snapshot-audit + compare)
Voir : 11 §I
```

---

## F. Parties prenantes

| Partie prenante | Rôle |
|---|---|
| **Équipe produit** | Règles métier, priorités de version, garde-fous de communication. |
| **Équipe technique** | Moteur d'analyse, historisation, front, interfaces d'administration. |
| **Équipe contenu / métier** | Formalisation des patterns (sources de référence), descriptions pédagogiques, dictionnaire de paramètres. |

---

## G. Matrice persona × flux

Cette table indique quels FLUX (de [07](07_flux_metier_client_admin.md)) sont déclenchés ou subis par chaque persona.

| FLUX | PERSONA-ANON | PERSONA-U01 | PERSONA-U02 | PERSONA-A01 |
|---|:---:|:---:|:---:|:---:|
| FLUX-C-01 Inscription 🔴 | ✓ déclencheur | — | — | — |
| FLUX-C-02 Connexion | ✓ | ✓ | ✓ | — |
| FLUX-C-03 Récup. mot de passe | ✓ | ✓ | ✓ | — |
| FLUX-C-04 Onboarding 🔴 | — | ✓ déclencheur | — | — |
| FLUX-C-05 Watchlist | — | ✓ premier ajout | ✓ gestion courante | — |
| FLUX-C-06 Portefeuille | — | ✓ premier enregistrement | ✓ gestion courante | — |
| FLUX-C-07 Demande analyse | — | ✓ première analyse | ✓ analyse régulière | — |
| FLUX-C-08 Consultation résultat | — | ✓ | ✓ | — |
| FLUX-C-09 Historique & comparaison | — | — | ✓ | — |
| FLUX-C-10 Simulation | — | ✓ | ✓ | — |
| FLUX-C-11 Alertes proactives 🔴 | — | — | ✓ subi | — |
| FLUX-C-12 Notifications | — | ✓ | ✓ | — |
| FLUX-C-13 Compte & sécurité | — | ✓ profil | ✓ profil + préfs | — |
| FLUX-C-14 Learn / Help 🔴 | — | ✓ aide | ✓ aide ponctuelle | — |
| FLUX-C-15 Gestion compte | — | ✓ | ✓ | — |
| FLUX-C-16 Déconnexion | — | ✓ | ✓ | — |
| FLUX-A-01 Connexion admin | — | — | — | ✓ |
| FLUX-A-02 Gestion utilisateurs 🔴 | — | — | ✓ subi | ✓ |
| FLUX-A-03 Registre instruments 🔴 | — | — | ✓ subi | ✓ |
| FLUX-A-04 Registre PEA 🔴 | — | — | ✓ subi | ✓ |
| FLUX-A-05 Politique scoring 🔴 | — | — | ✓ subi | ✓ |
| FLUX-A-06 Dictionnaire paramètres 🔴 | — | — | ✓ subi | ✓ |
| FLUX-A-07 Versions wording 🔴 | — | — | ✓ subi | ✓ |
| FLUX-A-08 Audit snapshots 🔴 | — | — | ✓ subi | ✓ |
| FLUX-A-09 Qualité données 🔴 | — | — | ✓ subi | ✓ |
| FLUX-A-10 KPI qualité signaux 🔴 | — | — | ✓ subi | ✓ |
| FLUX-A-11 KPI engagement 🔴 | — | — | ✓ subi | ✓ |
| FLUX-A-12 Déconnexion admin | — | — | — | ✓ |
| FLUX-A-13 Suppression compte RGPD 🔴 | — | — | ✓ subi | ✓ |

> **Lecture** : `✓ déclencheur` = le persona initie ce flux ; `✓ subi` = le persona est impacté par ce flux (un utilisateur subit un changement de politique admin) ; `✓` sans précision = le persona est l'acteur principal.
