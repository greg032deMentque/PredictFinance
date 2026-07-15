# PredictFinance — Documentation V1 (pack consolidé)

> **Statut** : pack canonique de la V1. Il **remplace** l'ancienne documentation (déplacée dans `Doc/_legacy/`).
> **Date de refonte** : 2026-05-21 · **Dernière mise à jour** : 2026-07-13 (réconciliation code↔doc : moteur 8 patterns, boucle ex post, fondamentaux enrichis, multi-portefeuille, accompagnement et RGPD livrés — voir [06](06_ecarts_doc_code.md)).
> **Principe** : un sujet = un seul fichier propriétaire. Pas de redondance. Toute décision est écrite une seule fois.

---

## 1. Pourquoi cette refonte

L'ancienne documentation comptait ~60 fichiers et souffrait de quatre défauts majeurs :

1. **Redondance massive** : le périmètre V1 était répété dans 12 fichiers, la règle FIFO dans 5, les verbes de recommandation dans 6. Changer une décision imposait d'éditer jusqu'à 12 fichiers.
2. **Méta-documentation circulaire** : plusieurs fichiers n'existaient que pour arbitrer quel fichier fait autorité quand deux se contredisaient.
3. **Confusion des genres** : spec produit, contrats techniques, plans d'exécution d'agents, registres de dette, audits de repo — tout mélangé sans hiérarchie de durabilité.
4. **Doublons de numérotation** (deux `07_`, deux `08_`, deux `09_`).

Ce pack résout ces défauts : **7 documents**, chacun propriétaire exclusif de son domaine, et un registre honnête des écarts entre la cible documentée et le code réel.

---

## 2. Les 10 documents et leur propriétaire de sujet

| # | Fichier | Propriétaire **unique** de | À lire si vous êtes… |
|---|---|---|---|
| 00 | [`00_INDEX.md`](00_INDEX.md) | Index, ordre de lecture, règles de cohérence | tout le monde (point d'entrée) |
| 01 | [`01_specification_produit.md`](01_specification_produit.md) | Vision, besoin métier, personas, périmètre, règles métier structurantes, modèle de domaine, contrat d'analyse, roadmap V1→V3 | produit, métier, nouveaux arrivants |
| 02 | [`02_glossaire_et_taxonomies.md`](02_glossaire_et_taxonomies.md) | **Tous** les enums et taxonomies fermées + le vocabulaire FR/EN | dev, design, contenu |
| 03 | [`03_specification_ecrans.md`](03_specification_ecrans.md) | Chaque écran : question dominante, données affichées, **chaque action**, états, **relations** entrantes/sortantes, règles | design, frontend, QA |
| 04 | [`04_user_stories.md`](04_user_stories.md) | Épopées, user stories, critères d'acceptation testables (Given/When/Then), priorités, dépendances | PO, dev, QA |
| 05 | [`05_contrats_donnees_api.md`](05_contrats_donnees_api.md) | Structures de données canoniques + surface API écran par écran + référentiel KPI | backend, frontend, intégration |
| 06 | [`06_ecarts_doc_code.md`](06_ecarts_doc_code.md) | Écarts réels entre **cible documentée** et **code actuel** — priorités P0/P1/P2 et blocages légaux | tech lead, planification |
| 07 | [`07_flux_metier_client_admin.md`](07_flux_metier_client_admin.md) | Flux métier séquentiels client et admin : déclencheur → étapes → branchements → post-conditions (27 flux, dont 1 transversal) | produit, dev, QA, onboarding |
| 08 | [`08_analyse_critique_et_legal.md`](08_analyse_critique_et_legal.md) | Analyse critique du produit (lacunes, UX, scalabilité, positionnement) + cadre légal (AMF/MIF 2, RGPD, droit conso, responsabilité PEA, matrice des risques) + recommandations stratégiques §9 | produit, dirigeants, juridique |
| 09 | [`09_diagnostic_strategique.md`](09_diagnostic_strategique.md) | Diagnostic stratégique multi-perspective : forces/faiblesses, matrice de risques (17 risques), positionnement concurrentiel, valorisation indicative (€1,6M EV), recommandations P0/P1/P2 | produit, dirigeants, investisseurs |

> Tout le périmètre V1 est décrit dans ces 10 documents — **socle d'analyse** et **accompagnement/pilotage** (alertes, plan d'action, confiance expliquée, onboarding/glossaire, boucle de feedback ex post, KPI admin) sont intégrés au même niveau, sans annexe séparée. Ce qui n'est pas encore construit côté code est consigné dans [06](06_ecarts_doc_code.md), pas exclu du périmètre.

---

## 3. Ordre de lecture recommandé

**Pour comprendre le produit (métier / produit)** : 01 → 03 → 07 → 04.

**Pour implémenter (dev)** : 02 → 05 → 03 → 07 → 06.

**Pour tester (QA)** : 04 → 07 → 03 → 02.

**Pour piloter (produit / data)** : 01 (§6.1, RM-29) → 03 §D.10 → 05 §3.bis.

**Pour la gouvernance et les risques** : 08 → 07 → 06.

---

## 4. Règles de cohérence du pack (à respecter pour toute modification future)

1. **Une décision, un propriétaire.** Une règle métier vit dans 01. Un enum vit dans 02. Un comportement d'écran vit dans 03. Ne jamais recopier le contenu d'un fichier dans un autre : **lier** au fichier propriétaire.
2. **Référencer, ne pas dupliquer.** Quand un document a besoin d'une notion d'un autre, il y renvoie (`voir [02](02_glossaire_et_taxonomies.md#…)`), il ne la réécrit pas.
3. **Cible vs. réel séparés.** Les documents 01→05 décrivent le **produit cible V1 cohérent**. Tout écart avec le code existant est consigné **uniquement** dans 06. Aucun document cible n'est bridé par la dette technique ; aucun écart n'est caché.
4. **Identifiants techniques en anglais, UI en français.** Les enums (`CrediblePatternFound`) restent en anglais dans le code et le doc 02 ; le texte visible utilisateur est toujours en français. Un identifiant interne ne doit jamais fuiter brut dans l'UI.
5. **Pas de méta-doc.** Ce pack n'a pas besoin d'un « index des index ». Si une contradiction apparaît, on la corrige à la source, on n'ajoute pas un fichier d'arbitrage.

---

## 5. Périmètre V1 en une phrase

> Application web pédagogique d'aide à l'investissement pour particulier débutant, analysant des **actions françaises** en données **journalières** via un **moteur déterministe explicable** (détection de patterns + lecture fondamentale + éligibilité PEA), avec historisation versionnée, **accompagnement** (alertes proactives, plan d'action, confiance expliquée, onboarding) et **pilotage** (KPI admin sur la qualité des signaux et l'usage). **Aucun ordre passé, aucun accès bancaire, aucune IA obligatoire.**

Le détail du périmètre (inclus / hors scope) est dans [01 §4](01_specification_produit.md).

---

## 6. État de la base de code (résumé)

Le code est **substantiellement implémenté**, pas un squelette :

- **Backend** (.NET 10, `FinanceBack/`) : 7 projets, moteur de **8 patterns** (continuation + retournement) avec contexte de risque ATR, **boucle ex post** (`SignalOutcome` + job quotidien), auth JWT + refresh, **30 contrôleurs**, persistance EF Core, gouvernance + KPI admin, RGPD (suppression compte / export données), tests d'intégration.
- **Frontend** (Angular 21, `FinanceFront/`) : SPA avec routage par rôle (auth / client / admin), écrans user (dont détail d'analyse, plan d'action, confiance expliquée, multi-portefeuille, comparaison de snapshots, learn/help/glossaire/onboarding) et **tous** les écrans admin construits.

Les écarts précis entre ce code et la cible documentée sont dans [06](06_ecarts_doc_code.md).
