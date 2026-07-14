# 04 — User stories V1

> **Propriétaire de** : épopées, user stories, critères d'acceptation testables, priorités, dépendances.
> Comportement détaillé des écrans → [03](03_specification_ecrans.md). Règles métier (RM-xx) → [01](01_specification_produit.md). Enums → [02](02_glossaire_et_taxonomies.md).

---

## Conventions

- **Identifiant** : `US-<épopée>-<n°>` (ex. `US-ANALYSE-03`).
- **Format** : *En tant que `<rôle>`, je veux `<capacité>`, afin de `<bénéfice>`.*
- **Critères d'acceptation** : Given / When / Then (Étant donné / Quand / Alors), **testables**.
- **Priorité** : `P0` (indispensable au socle V1) · `P1` (V1 attendu) · `P2` (V1 souhaitable / peut glisser).
- **Rôles** : `Visiteur` ([PERSONA-ANON](10_personas.md#b-persona-anon--le-visiteur-anonyme)) · `Investisseur` ([PERSONA-U01](10_personas.md#c-persona-u01--linvestisseur-découvrant) / [PERSONA-U02](10_personas.md#d-persona-u02--linvestisseur-actif)) · `Admin` ([PERSONA-A01](10_personas.md#e-persona-a01--ladministrateur)).
- **Statut cible** : 🟢 supporté · 🟡 partiel · 🔴 cible non construite (voir [06](06_ecarts_doc_code.md)).

### Définition de « terminé » (Definition of Done) commune

Une story est « terminée » si : (a) ses critères d'acceptation passent ; (b) les états vide/chargement/erreur/non-exécutable de l'écran concerné sont gérés (RM-24) ; (c) le wording visible est en français et conforme à [02](02_glossaire_et_taxonomies.md) ; (d) aucune vérité métier n'est inventée côté frontend (RM-01) ; (e) si l'analyse est exécutable, un snapshot versionné est persisté (RM-19).

---

## Épopée AUTH — Accès et récupération de compte

### US-AUTH-00 — S'inscrire (P0, 🔴)
**En tant que** Visiteur, **je veux** créer un compte avec email + mot de passe, **afin d'**accéder aux fonctionnalités de PredictFinance.
- **Quand** je saisis un email valide, un mot de passe conforme et accepte les CGU, **alors** un compte est créé en statut `PENDING` et un email de confirmation est envoyé.
- **Quand** je clique sur le lien de confirmation, **alors** le compte bascule en `ACTIVE` et je suis redirigé vers `/login`.
- **Quand** l'email est déjà enregistré, **alors** la réponse est identique sans révéler l'existence du compte (aucune divulgation, identique à `US-AUTH-02`).
- **Quand** les mots de passe sont discordants (`password ≠ confirmPassword`), **alors** la validation échoue **avant soumission** (validation frontend).
- **Quand** le mot de passe est trop faible, **alors** des critères de force sont affichés en feedback inline dès la saisie.
- **Quand** les CGU ne sont pas cochées, **alors** le bouton de création est désactivé.
- **Étant donné** un compte `PENDING`, **quand** je tente de me connecter, **alors** un état non-exécutable « confirmez votre adresse email » s'affiche (RM-24, ≠ erreur technique).
- **Quand** le lien de confirmation est expiré ou invalide, **alors** un état non-exécutable s'affiche avec proposition de renvoi d'un nouveau lien.

**Écran** : `B.4` ([03](03_specification_ecrans.md)). **Flux** : FLUX-C-01 dans [07](07_flux_metier_client_admin.md).

### US-AUTH-01 — Se connecter (P0, 🟢)
**En tant que** Visiteur, **je veux** me connecter avec email + mot de passe, **afin d'**accéder à mon espace.
- **Étant donné** un compte `ACTIVE`, **quand** je saisis des identifiants valides et valide, **alors** je suis routé vers `/client/dashboard` si rôle `User`, ou `/admin/dashboard` si `Admin` (RM-22).
- **Étant donné** des identifiants invalides, **quand** je valide, **alors** un message d'erreur récupérable s'affiche **sans** révéler quel champ est faux.
- **Étant donné** que je ne suis pas connecté, **alors** aucune navigation métier user/admin n'est visible (RM-21).
- **Étant donné** un compte `DISABLED`, **quand** je me connecte avec de bons identifiants, **alors** un état non-exécutable « accès indisponible » s'affiche (pas une erreur technique).

### US-AUTH-02 — Demander une réinitialisation (P0, 🟢)
**En tant que** Visiteur, **je veux** demander un email de réinitialisation, **afin de** récupérer l'accès à mon compte.
- **Quand** je soumets un email, **alors** la réponse est identique que le compte existe ou non (aucune divulgation).
- **Étant donné** un email associé à un compte, **alors** un email de réinitialisation est envoyé.

### US-AUTH-03 — Réinitialiser le mot de passe (P0, 🟢)
**En tant que** Visiteur muni d'un lien valide, **je veux** définir un nouveau mot de passe, **afin de** retrouver l'accès.
- **Quand** `newPassword` ≠ `confirmPassword`, **alors** la validation échoue avant soumission.
- **Quand** le token est expiré/invalide, **alors** un état non-exécutable « lien invalide ou expiré » s'affiche.
- **Quand** le reset réussit, **alors** je suis renvoyé vers `/login`.

### US-AUTH-04 — Se déconnecter (P0, 🟢)
**En tant qu'**Investisseur connecté, **je veux** me déconnecter, **afin de** sécuriser ma session.
- **Quand** je me déconnecte, **alors** la session/refresh token est invalidée et je reviens à `/login`, sans accès aux routes protégées (retour arrière inclus).

---

## Épopée WATCHLIST — Suivi des valeurs non détenues

### US-WATCH-01 — Ajouter une valeur à la watchlist (P0, 🟢)
**En tant qu'**Investisseur, **je veux** ajouter une action française à ma watchlist, **afin de** la suivre.
- **Quand** je recherche puis ajoute une action FR active, **alors** elle apparaît dans ma watchlist.
- **Quand** je tente d'ajouter un instrument hors périmètre (ETF/crypto), **alors** l'ajout est refusé/marqué `UnsupportedInstrument` (RM-24), pas ajouté silencieusement.

### US-WATCH-02 — Retirer une valeur (P0, 🟢)
**En tant qu'**Investisseur, **je veux** retirer une valeur de ma watchlist, **afin de** garder une liste pertinente.
- **Quand** je retire une valeur, **alors** elle disparaît de la liste immédiatement.

### US-WATCH-03 — Lire les résumés priorisés (P1, 🟢)
**En tant qu'**Investisseur, **je veux** voir par ligne un résumé marché + support/PEA + recommandation, **afin de** prioriser sans ouvrir chaque valeur.
- **Alors** chaque ligne montre marché, support/PEA et un verbe **non détenu** uniquement (`Surveiller`/`Attendre`/`Acheter`).
- **Alors** aucune ligne ne paraît « positive » par la seule couleur ; le résumé reco n'écrase pas la distinction marché/support/PEA.

### US-WATCH-04 — Filtrer et trier (P2, 🟢)
**En tant qu'**Investisseur, **je veux** filtrer/trier ma watchlist, **afin de** me concentrer sur l'essentiel.
- **Quand** un filtre ne renvoie rien, **alors** un état vide explicite s'affiche (≠ erreur).

---

## Épopée PORTEFEUILLE — Décision sur positions détenues

### US-PORT-01 — Enregistrer une transaction (P0, 🟢)
**En tant qu'**Investisseur, **je veux** enregistrer un achat/vente (quantité, prix, date, frais, devise), **afin de** refléter mes positions réelles.
- **Quand** j'enregistre un achat, **alors** une ligne ouverte candidate est créée (RM-08).
- **Quand** j'enregistre une vente, **alors** elle consomme en **FIFO strict** les plus anciennes lignes ouvertes.
- **Quand** la `buyDate` est postérieure à l'`asOfDate` d'analyse, **alors** la validation refuse la ligne.

### US-PORT-02 — Voir mes positions et leur PRU dérivé (P0, 🟢)
**En tant qu'**Investisseur, **je veux** voir mes lignes ouvertes et le PRU, **afin de** comprendre ma position.
- **Alors** le PRU est **dérivé** des lignes ouvertes FIFO, jamais une valeur stockée comme vérité (RM-08).
- **Étant donné** aucune position, **alors** un état vide « aucune position » s'affiche.

### US-PORT-03 — Recommandation contexte détenu (P0, 🟢)
**En tant qu'**Investisseur détenant une valeur, **je veux** une recommandation adaptée à la détention, **afin d'**agir correctement.
- **Alors** seuls les verbes détenus sont proposés (`Conserver`/`Renforcer`/`Alléger`/`Vendre`/`Attendre`) (RM-10).
- **Alors** `Surveiller` n'est **jamais** la recommandation finale d'une position détenue.

---

## Épopée ANALYSE — Cœur technique

### US-ANALYSE-01 — Lancer une analyse avec contexte de détention (P0, 🟢)
**En tant qu'**Investisseur, **je veux** lancer une analyse en précisant si je détiens la valeur, **afin d'**obtenir une recommandation correctement contextualisée.
- **Quand** je choisis un instrument et un état de détention puis lance, **alors** une analyse journalière à la demande s'exécute.
- **Alors** le contexte de détention est demandé **avant** l'analyse.

### US-ANALYSE-02 — Détection multi-patterns (P0, 🟢)
**En tant qu'**Investisseur, **je veux** voir tous les patterns crédibles, **afin de** ne pas manquer de lecture alternative.
- **Étant donné** plusieurs patterns crédibles, **alors** l'issue est `MultipleCompatiblePatterns` et **tous** sont affichés (RM-06).
- **Alors** un **pattern principal** est mis en avant **sans effacer** les alternatifs.
- **Alors** seuls les patterns actifs sont exécutables ; `DOUBLE_TOP` est rejeté comme non pris en charge.

### US-ANALYSE-03 — Absence de pattern crédible (P0, 🟢)
**En tant qu'**Investisseur, **je veux** un message explicite quand aucun pattern n'est fiable, **afin de** ne pas être induit en erreur.
- **Quand** aucun pattern n'est crédible, **alors** l'issue `NoCrediblePattern` s'affiche comme **état métier de premier rang** (RM-05, RM-24), pas un faux signal ni une erreur.

### US-ANALYSE-04 — Données insuffisantes / instrument hors périmètre (P0, 🟢)
**En tant qu'**Investisseur, **je veux** comprendre pourquoi une analyse n'est pas exécutable, **afin de** savoir quoi faire.
- **Quand** l'historique est trop court, **alors** issue `InsufficientData`.
- **Quand** l'instrument n'est pas une action FR active, **alors** issue `UnsupportedInstrument`.
- **Alors** chaque cas affiche un wording FR explicite ([02](02_glossaire_et_taxonomies.md#analysisoutcome)).

### US-ANALYSE-05 — Détection indépendante du portefeuille (P0, 🟢)
**En tant que** Produit, **je veux** que la détection ignore la détention, **afin de** garder la vérité technique pure (RM-07).
- **Étant donné** deux utilisateurs (l'un détenant, l'autre non) analysant le même instrument à la même date, **alors** la **lecture marché** (patterns, statut, confiance) est **identique** ; seules recommandation/explication diffèrent (RM-09).

### US-ANALYSE-06 — Risque restitué séparément (P1, 🟢)
**En tant qu'**Investisseur, **je veux** voir l'invalidation, le stop loss, le take profit et le ratio R/R, **afin d'**évaluer le risque.
- **Alors** ces niveaux apparaissent dans la section risque, **jamais** dans la recommandation (RM-11), et seulement selon disponibilité.

### US-ANALYSE-07 — Lire le résultat consolidé (P0, 🟡)
**En tant qu'**Investisseur, **je veux** un résultat ordonné (issue → marché → support → situation → reco → plan d'action), **afin de** le comprendre d'un coup d'œil.
- **Alors** l'ordre visuel est respecté ; la reco est en aval et n'avale pas le reste (RM-09).
- **Alors** un bloc « pourquoi cette reco » reste **traçable** aux vérités visibles, sans inventer de vérité backend.

### US-ANALYSE-08 — Comprendre pourquoi cette confiance (P1, 🟡)
**En tant qu'**Investisseur, **je veux** voir les critères qui justifient le niveau de confiance, **afin de** ne pas prendre « confiance moyenne » au pied de la lettre.
- **Alors** sous le `ConfidenceLabel`, une décomposition de critères issus de `detection`/`validation`/`invalidation` s'affiche, chacun ✅/⚠️/❌ (RM-27).
- **Alors** cette décomposition **explique** la confiance sans la recalculer ni la modifier.
- **Alors** chaque critère a un libellé gouverné ([02](02_glossaire_et_taxonomies.md#confidencelabel)) ; le frontend n'invente aucun critère (RM-01).

### US-ANALYSE-09 — Repartir avec un plan d'action (P0, 🟡)
**En tant qu'**Investisseur, **je veux** un bloc « Vos prochaines étapes » à la fin de l'analyse, **afin de** savoir concrètement quoi faire.
- **Alors** le plan propose 2-3 étapes déterministes : niveau à noter (`invalidationPrice`), horizon de revue (`reviewHorizonDays`), alerte suggérée, rappel d'action selon détention (RM-26).
- **Alors** le plan **n'introduit aucun nouveau chiffre** ; chaque étape est traçable à son élément source.
- **Quand** l'issue est non-exécutable, **alors** le plan propose une étape adaptée (ex. « revenez quand l'historique sera suffisant »), pas un plan de trade.

---

## Épopée SUPPORT — Lecture fondamentale & PEA

### US-SUP-01 — Score fondamental relatif (P1, 🟡)
**En tant qu'**Investisseur, **je veux** un score fondamental relatif par catégorie, **afin de** juger la solidité de la valeur.
- **Alors** le classement est **non paramétrique** (percentiles) sur un univers explicite (RM-13).
- **Alors** la lecture support est clairement **séparée** de la lecture marché (RM-12), jamais fusionnée en un score unique.

### US-SUP-02 — Indisponibilité explicite du composite (P1, 🟡)
**En tant qu'**Investisseur, **je veux** comprendre pourquoi un score composite est indisponible, **afin de** ne pas surinterpréter.
- **Quand** la couverture < 3 catégories valides, **alors** « couverture de données insuffisante » (RM-14).
- **Quand** l'éligibilité PEA n'est pas confirmée, **alors** « éligibilité PEA non confirmée » bloquante.
- **Alors** l'indisponibilité est un **état métier**, pas une erreur.

### US-SUP-03 — Statut PEA explicite (P0, 🟡)
**En tant qu'**Investisseur, **je veux** un statut PEA clair, **afin de** savoir si la valeur est éligible.
- **Alors** le statut est l'un des 3 états ([02](02_glossaire_et_taxonomies.md#peaeligibilitystatus)).
- **Alors** `Unknown` n'apparaît **jamais** comme implicitement positif et n'est **jamais** traité comme éligible (RM-15).
- **Alors** la vérité PEA provient du **registre interne gouverné**, pas d'un fournisseur de marché.

---

## Épopée PÉDAGOGIE — Comprendre les paramètres

### US-PEDA-01 — Déplier un paramètre en 4 couches (P1, 🔴)
**En tant qu'**Investisseur, **je veux** comprendre un indicateur (définition → valeur → pourquoi → implication), **afin d'**apprendre en contexte.
- **Quand** j'ouvre le détail d'un paramètre, **alors** les 4 couches sont présentes (RM-16), plus « ce que ça soutient » / « ce que ça ne prouve pas » et l'implication **avec** / **sans** position.
- **Alors** tout le texte vient du **dictionnaire gouverné** backend (RM-17) ; le frontend n'en synthétise aucun.
- **Alors** un paramètre seul n'est **jamais** présenté comme une recommandation (RM-18).

### US-PEDA-02 — Comprendre un terme au moment du doute (glossaire inline) (P1, 🔴)
**En tant qu'**Investisseur, **je veux** une info-bulle « ? » sur chaque terme technique, **afin de** lever un doute sans quitter ma tâche.
- **Quand** je survole/ouvre l'aide d'un terme (PRU, invalidation, percentile, couverture…), **alors** une explication courte tirée du **dictionnaire gouverné** s'affiche (RM-28).
- **Alors** le contenu n'est **jamais** du texte frontend libre (RM-17).
- **Alors** il n'y a **pas de doublon** avec `Learn` : le glossaire répond au doute ponctuel, `Learn` au contenu conceptuel long (RM-28, RM-23).

---

## Épopée HISTORIQUE — Traçabilité dans le temps

### US-HIST-01 — Consulter l'historique persisté (P0, 🟢)
**En tant qu'**Investisseur, **je veux** voir mes analyses passées datées, **afin de** suivre l'évolution.
- **Alors** chaque entrée se lit comme **vérité persistée** (snapshot), pas une reconstruction de l'état courant (RM-20).
- **Étant donné** aucun historique, **alors** un état vide explicite.

### US-HIST-02 — Comparer deux snapshots (P1, 🔴 user / 🟢 admin)
**En tant qu'**Investisseur, **je veux** comparer deux snapshots, **afin de** voir ce qui a changé.
- **Alors** le diff distingue lecture marché, support et recommandation, et **cause** vs **conséquence**.
- **Quand** les snapshots ne sont pas comparables, **alors** la non-comparabilité est **explicite** (RM-20), jamais masquée.

---

## Épopée SIMULATION

### US-SIM-01 — Tester un scénario (P1, 🟢)
**En tant qu'**Investisseur, **je veux** simuler entrée/taille/invalidation/cible/frais, **afin d'**évaluer un scénario.
- **Alors** les sorties montrent baisse/hausse potentielles et ratio R/R.
- **Alors** la simulation est **visuellement distincte** de l'historique persisté ; aucun wording ne laisse croire à une vérité observée ; **aucune prédiction de prix**.

---

## Épopée ORIENTATION — Notifications, aide, onboarding

### US-NOTIF-01 — Prioriser et router via notifications (P1, 🟢)
**En tant qu'**Investisseur, **je veux** un centre de notifications et d'alertes, **afin de** savoir quoi traiter et où aller.
- **Quand** j'ouvre une notification ou une alerte, **alors** je suis routé vers son `NotificationTargetScreen` ([02](02_glossaire_et_taxonomies.md#notificationtargetscreen)).
- **Alors** la **page de destination** détient la vérité ; l'item ne l'explique pas (RM-23, RM-25b).
- **Alors** je **ne peux pas** y modifier mes préférences (cela se fait dans le compte).

### US-HELP-01 — Obtenir une aide contextuelle (P2, 🔴)
**En tant qu'**Investisseur, **je veux** une aide contextuelle, **afin de** lever un doute et revenir à ma tâche.
- **Alors** l'aide **explique** et **route** vers l'écran de tâche ; elle ne le remplace pas ; elle est déterministe (pas un chat) (RM-23).

### US-ONB-01 — Démarrer sans données (P1, 🔴)
**En tant que** nouvel Investisseur sans données, **je veux** un onboarding concret, **afin d'**obtenir une première valeur.
- **Étant donné** ni watchlist, ni portefeuille, ni historique, **quand** j'arrive sur la home, **alors** l'onboarding propose une première action claire (watchlist / analyse / portefeuille), sans remplissage motivationnel.
- **Alors** il propose 2-3 **exemples d'actions FR** pour une démo immédiate (première valeur en < 30 s), jamais d'ETF/crypto (RM-24).

---

## Épopée ALERTES — Le produit veille pour moi

### US-ALERT-01 — Être prévenu d'un changement d'état (P0, 🔴)
**En tant qu'**Investisseur, **je veux** être prévenu quand une valeur que je suis change d'état de pattern, **afin d'**agir au bon moment sans surveiller en permanence.
- **Étant donné** une valeur en watchlist/portefeuille, **quand** son `PatternStatus` passe `Monitoring`→`Confirmed` ou `*`→`Invalidated`, **alors** une alerte `PATTERN_STATE_CHANGE` est générée et route vers `AnalysisResult` (RM-25).
- **Alors** l'alerte respecte mes préférences (si catégorie `Analysis` désactivée, pas d'alerte) et est dédoublonnée par (instrument × déclencheur × jour) (RM-25b).
- **Alors** l'alerte **route, n'explique pas** et **n'est pas une prédiction**.

### US-ALERT-02 — Être prévenu d'un franchissement de niveau (P1, 🔴)
**En tant qu'**Investisseur, **je veux** être prévenu quand le prix franchit un niveau clé d'un signal suivi, **afin de** réagir.
- **Quand** le prix franchit `InvalidationPrice` ou `TargetPrice` (issu de l'évaluation ex post, RM-29), **alors** une alerte `LEVEL_CROSSED` route vers `InstrumentDetail` (RM-25).
- **Quand** j'active une alerte depuis un résultat (`C.5`) ou un détail instrument (`C.6`), **alors** elle est créée sur le seuil suggéré.

### US-ALERT-03 — Être prévenu de données obsolètes (P2, 🔴)
**En tant qu'**Investisseur, **je veux** savoir quand les données d'une valeur suivie deviennent obsolètes, **afin de** ne pas décider sur des données périmées.
- **Quand** une valeur suivie bascule en fraîcheur `STALE`, **alors** une alerte `DATA_STALE` est générée (RM-25).

### US-ALERT-04 — Gérer mes alertes (P1, 🟢→🟡)
**En tant qu'**Investisseur, **je veux** activer/désactiver mes alertes par catégorie, **afin de** maîtriser ce que je reçois.
- **Alors** la gestion se fait dans le compte (`C.15`), pas dans le centre de notifications (RM-23).

---

## Épopée COMPTE — Self-service

### US-ACC-01 — Gérer mon profil et mes préférences (P1, 🟢)
**En tant qu'**Investisseur, **je veux** éditer mon profil et mes préférences (dont notifications), **afin de** personnaliser mon expérience.
- **Alors** profil / préférences / notifications / sécurité sont **segmentés explicitement**.
- **Alors** **aucune action admin** n'est disponible ici (RM-23).

### US-ACC-02 — Gérer ma sécurité (P1, 🟢)
**En tant qu'**Investisseur, **je veux** voir un résumé sécurité et changer mon mot de passe, **afin de** protéger mon compte.
- **Alors** le résumé montre `hasPassword` et la dernière modification ; le changement de mot de passe applique la nouvelle valeur après validation.

---

## Épopée ADMIN — Gouvernance

### US-ADM-01 — Gérer les utilisateurs (P0, 🟢)
**En tant qu'**Admin, **je veux** lister, filtrer, créer et éditer des utilisateurs, **afin de** gouverner les accès.
- **Alors** chaque ligne montre identité, rôle, statut, dernière activité.
- **Alors** cette surface est **réservée admin** ; le self-service reste dans le compte.

### US-ADM-02 — Gouverner le registre PEA (P0, 🟢)
**En tant qu'**Admin, **je veux** consulter la vérité PEA gouvernée, **afin de** garantir l'exactitude des statuts.
- **Alors** `Unknown` reste **visiblement distinct** de `ConfirmedIneligible` ; une vérité manquante n'est jamais rendue comme éligibilité implicite (RM-15).
- **Alors** source, date de vérification et version de politique sont visibles.

### US-ADM-03 — Consulter la politique de scoring (P1, 🟢)
**En tant qu'**Admin, **je veux** voir la politique de scoring active et son historique, **afin de** comprendre comment les scores sont produits.

### US-ADM-04 — Gouverner le dictionnaire de paramètres (P1, 🟢)
**En tant qu'**Admin, **je veux** gérer les explications de paramètres gouvernées, **afin que** la pédagogie reste exacte et versionnée.
- **Alors** chaque paramètre déclare définition, garde-fous, limites, « ne prouve pas », et gabarits d'implication avec/sans position (alimente `US-PEDA-01`).

### US-ADM-05 — Gouverner les versions de wording (P1, 🟢)
**En tant qu'**Admin, **je veux** consulter/inspecter les versions de wording et leur état de publication, **afin de** maîtriser le vocabulaire produit.

### US-ADM-06 — Auditer les snapshots (P0, 🟢)
**En tant qu'**Admin, **je veux** inspecter et comparer les snapshots persistés et leurs versions de règles, **afin de** garantir l'auditabilité (RM-19).
- **Alors** identité, horodatage, versions de règles, payloads marché/support/reco sont visibles, sans abstraction masquant le contexte de version.

### US-ADM-07 — Suivre la qualité des données (P1, 🟢)
**En tant qu'**Admin, **je veux** voir les problèmes de qualité priorisés par impact, **afin de** corriger ce qui dégrade la vérité produit.
- **Alors** la vue distingue fraîcheur source vs règles de scoring, et incomplétude de registre vs univers non pris en charge.

---

## Épopée PILOTAGE — KPI admin

> Toutes ces stories : KPI à formule explicite et source traçable, **n'inventant aucune vérité** (RM-29), avec état de disponibilité ([02](02_glossaire_et_taxonomies.md#disponibilit%C3%A9-dun-kpi)).

### US-KPI-01 — Évaluer la qualité des signaux ex post (P0, 🔴) ⭐
**En tant qu'**Admin, **je veux** mesurer si les signaux passés ont atteint leur cible ou été invalidés, **afin de** savoir si le moteur a raison.
- **Étant donné** des snapshots passés, **quand** le job ex post s'exécute, **alors** chaque signal reçoit un `SignalOutcome` ([02](02_glossaire_et_taxonomies.md#signaloutcome-issue-r%C3%A9alis%C3%A9e-ex-post)) en comparant `PriceHistory` aux niveaux persistés.
- **Alors** le taux d'atteinte de cible est calculé globalement et **par pattern**.
- **Quand** la fenêtre d'évaluation n'est pas écoulée, **alors** `KPI_WINDOW_TOO_YOUNG` (pas une erreur).

### US-KPI-02 — Vérifier la calibration de la confiance (P1, 🔴)
**En tant qu'**Admin, **je veux** comparer le taux d'atteinte de cible par bucket de confiance, **afin de** savoir si le scoring est bien calibré.
- **Alors** un tableau croise `LOW/MEDIUM/HIGH` × taux d'atteinte réel.
- **Alors** si `HIGH` ≤ `LOW`, l'écran signale une **calibration à revoir** et route vers la politique de scoring (`D.5`).

### US-KPI-03 — Suivre l'engagement et la rétention (P1, 🔴)
**En tant qu'**Admin, **je veux** suivre inscriptions, DAU/WAU/MAU, rétention par cohorte et lecture des notifications, **afin de** piloter la fidélisation.
- **Alors** la rétention par cohorte (J+1/J+7/J+30) est affichée en heatmap, avec `KPI_WINDOW_TOO_YOUNG` pour les cohortes récentes.

### US-KPI-04 — Suivre le funnel d'activation et l'usage (P1, 🔴)
**En tant qu'**Admin, **je veux** voir le funnel inscription → 1ère watchlist → 1ère analyse → 1ère transaction et le contenu le plus consommé, **afin d'**identifier les points de friction.
- **Alors** le taux de passage et le délai médian par étape sont calculés.
- **Alors** les écrans les plus visités sont dérivés des logs `Analytic`, dans le respect de l'anonymisation (RM-29b).

### US-KPI-05 — Suivre la santé opérationnelle (P1, 🟡)
**En tant qu'**Admin, **je veux** suivre taux d'échec d'analyse, latence, couverture PEA, complétude des snapshots et fraîcheur, **afin de** garder la vérité produit saine.
- **Alors** ces indicateurs apparaissent en tendance (pas seulement en instantané) sur `D.1`/`D.9`.

---

## Tableau de synthèse (priorité × statut cible)

| Épopée | P0 | P1 | P2 | Stories 🔴 (cible à construire) |
|---|---|---|---|---|
| AUTH | 4 | — | — | — |
| WATCHLIST | 2 | 1 | 1 | — |
| PORTEFEUILLE | 3 | — | — | — |
| ANALYSE | 6 | 3 | — | US-ANALYSE-08/09 (🟡) |
| SUPPORT | 1 | 2 | — | (toutes 🟡) |
| PÉDAGOGIE | — | 2 | — | US-PEDA-01, US-PEDA-02 |
| HISTORIQUE | 1 | 1 | — | US-HIST-02 (user) |
| SIMULATION | — | 1 | — | — |
| ORIENTATION | — | 1 | 1 | US-HELP-01, US-ONB-01 |
| ALERTES | 1 | 2 | 1 | US-ALERT-01/02/03/04 |
| COMPTE | — | 2 | — | — |
| ADMIN | 4 | 3 | — | — |
| PILOTAGE | 1 | 4 | — | US-KPI-01→05 |

> Les statuts 🔴/🟡 sont dérivés de l'état réel du code consigné en [06](06_ecarts_doc_code.md). Ils indiquent l'effort restant, pas un changement de périmètre cible — **tout le tableau est V1**.

### Séquencement recommandé d'implémentation

Dérivé du diagnostic produit (le socle analyse mais n'accompagne pas) et des dépendances techniques :

1. **US-ANALYSE-09 (plan d'action) + US-ANALYSE-08 (confiance expliquée)** — meilleur ratio valeur/effort : les données existent déjà, zéro dépendance batch.
2. **US-KPI-01 (qualité des signaux ex post)** — chantier structurant : le job ex post conditionne la confiance dans tout le reste et débloque US-ALERT-02.
3. **US-ALERT-01/02 (alertes proactives)** — transforment le produit en assistant ; partagent la ré-évaluation périodique avec US-KPI-01 (mutualiser).
4. **US-KPI-03/04 (engagement, funnel)** — données déjà prêtes, pure agrégation.
5. **US-ONB-01 + US-PEDA-02 (onboarding + glossaire)** — améliorent l'entrée et la pédagogie au doute.

> Note d'architecture transverse : US-KPI-01 et US-ALERT-* reposent sur la **même ré-évaluation périodique** des instruments suivis. Construire ce mécanisme une seule fois (en V1 : à la connexion / ouverture des surfaces ; en V2 : batch nocturne, [01 §9](01_specification_produit.md#9-roadmap-v1--v3)).
