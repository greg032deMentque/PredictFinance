# Spécification produit détaillée
## Application d’aide à l’investissement par analyse de patterns

Version de cadrage produit — fondée sur les besoins métier exprimés et les décisions de conception déjà validées.

- Produit cible : application pédagogique d’aide à l’investissement pour particulier débutant.
- Portée V1 : actions françaises, analyse journalière, moteur déterministe explicable, sans IA obligatoire.
- Principe directeur : l’API est la source de vérité métier ; l’IA reste facultative et périphérique.
- Objectif de conception : architecture ouverte pour ajouter facilement de nouveaux actifs, marchés, patterns et règles.

## 1. Résumé exécutif

Le produit vise à aider un investisseur particulier débutant à suivre son portefeuille et sa watchlist, à comprendre l’état technique d’une valeur, et à recevoir une aide pédagogique à la décision d’achat, d’attente, de conservation, d’allègement ou de vente.

Le produit ne passe aucun ordre et n’accède pas aux comptes bancaires. Il fournit des analyses, des recommandations pédagogiques et des alertes contextuelles basées sur des règles déterministes auditées.

Le cœur du système est un moteur d’analyse versionné, explicable et historisé. Chaque analyse doit pouvoir être rejouée, comprise et comparée dans le temps.

## 2. Vision produit

Transformer des données de marché complexes en conclusions compréhensibles, pédagogiques et traçables pour un utilisateur non expert.

Permettre à l’utilisateur de suivre les valeurs qui l’intéressent, de contextualiser les signaux au regard de son portefeuille, et de capitaliser sur un historique d’analyses pour apprendre et progresser.

Construire un socle technique durable, extensible et maintenable, capable d’évoluer sans refonte majeure lors de l’ajout de nouvelles familles d’actifs, de nouveaux marchés ou de nouveaux patterns.

## 3. Problème métier adressé

Un particulier débutant manque souvent de méthode pour lire un graphique, identifier des configurations techniques crédibles et relier ces observations à une décision d’investissement prudente.

Les informations disponibles sur le marché sont abondantes, hétérogènes et souvent difficiles à interpréter sans expérience. Le produit doit réduire cette complexité sans promettre de résultat financier.

Le besoin n’est pas d’automatiser le trading mais de fournir un cadre d’analyse pédagogique, cohérent et traçable.

## 4. Positionnement produit

Outil d’aide pédagogique à l’investissement.

Outil d’analyse technique centrée patterns.

Outil de recommandation pédagogique et contextualisée.

Le produit n’est pas un robot d’exécution ni un système de trading haute fréquence.

## 5. Parties prenantes et personas

### 5.1 Persona principal

| Champ | Spécification |
|---|---|
| Type d’utilisateur | Particulier débutant |
| Motivation | Comprendre quand surveiller, acheter, attendre, conserver ou vendre une valeur sans devoir maîtriser seul l’analyse technique. |
| Niveau d’expertise | Faible à intermédiaire en lecture de graphique et gestion du risque. |
| Attentes clés | Clarté, pédagogie, justification explicite, conservation de l’historique, simplicité de navigation. |
| Tolérance au risque | Variable, mais non personnalisée en V1 ; les recommandations sont d’abord issues de l’analyse marché. |

### 5.2 Parties prenantes secondaires

- Équipe produit : définit les règles métier, les priorités de version et les garde-fous de communication.
- Équipe technique : implémente le moteur d’analyse, l’historisation, le front et les interfaces d’administration/monitoring.
- Équipe contenu / métier : formalise les patterns à partir de sources de référence et rédige les descriptions pédagogiques.

## 6. Objectifs produit

| Catégorie | Objectif détaillé |
|---|---|
| Compréhension | Rendre l’analyse technique lisible par un débutant sans simplifier à l’excès ni masquer les incertitudes. |
| Décision | Fournir une recommandation pédagogique contextualisée, séparée de la détection technique et de l’évaluation du risque. |
| Traçabilité | Conserver chaque analyse, ses hypothèses, ses scores, sa recommandation et sa performance ex post. |
| Extensibilité | Permettre l’ajout de nouveaux patterns, nouveaux actifs et nouveaux marchés sans refonte du noyau. |
| Confiance | Assurer l’explicabilité de tous les résultats présentés et rendre auditable la chaîne de calcul. |

## 7. Périmètre fonctionnel

### 7.1 Inclus en cible V1

- Création de compte et espace utilisateur.
- Gestion d’une watchlist de valeurs suivies.
- Gestion d’un portefeuille avec plusieurs lignes d’achat par actif.
- Stockage des attributs de position : quantité, prix d’achat, date d’achat, PRU, frais, devise.
- Analyse journalière à la demande dans un espace dédié.
- Historisation complète des analyses.
- Détection déterministe de patterns extensibles.
- Évaluation du risque : invalidation, stop loss, take profit, ratio risque/rendement, volatilité, drawdown potentiel si applicable.
- Affichage du pattern principal et des patterns alternatifs compatibles.
- Résumé pédagogique expliquant le résultat.

### 7.2 Hors scope explicite V1

- Passage d’ordres.
- Accès direct aux comptes bancaires.
- Trading temps réel tick par tick.
- Fiscalité et gestion détaillée PEA/CTO.
- Personnalisation avancée par profil investisseur.
- IA générative comme source primaire de vérité.

## 8. Couverture marché et actifs

| Dimension | Décision produit |
|---|---|
| Actifs V1 | Actions françaises |
| Vision long terme | Architecture compatible avec l’ajout d’ETF, autres actions, puis autres familles d’actifs. |
| Granularité V1 | Journalière |
| Élargissement futur | Ajout de nouveaux marchés et nouvelles devises sans casser le modèle métier central. |

## 9. Modèle métier central

- Surveiller un portefeuille et une watchlist.
- Analyser une valeur sur données journalières.
- Détecter plusieurs patterns compatibles si le marché le justifie.
- Séparer trois couches métier : détection technique, évaluation du risque, recommandation pédagogique.
- Conserver une trace versionnée de chaque analyse et mesurer sa performance ex post.

## 10. Règles métier structurantes

| Règle | Description détaillée |
|---|---|
| R1 | L’API porte la vérité métier : données de marché normalisées, détection, scoring, risque, historique et traçabilité. |
| R2 | L’IA n’est pas nécessaire en V1 ; si elle est ajoutée plus tard, elle reste une couche d’assistance pédagogique et interprétative. |
| R3 | Chaque pattern déclare son propre contrat : prérequis, fenêtre d’analyse minimale, règles de détection, validation, invalidation et scoring. |
| R4 | Si aucun pattern n’est fiable, le système doit l’indiquer explicitement au lieu de forcer un faux 100 %. |
| R5 | Si plusieurs patterns sont compatibles, ils sont tous affichés avec leurs informations et l’explication de cette coexistence. |
| R6 | La recommandation dépend de l’analyse marché et du contexte de détention utilisateur. |
| R7 | Tous les résultats affichés doivent être explicables, auditables et historisés. |
| R8 | En V1, la reconstruction des lignes d’achat ouvertes après ventes suit une règle FIFO stricte pour la contextualisation portefeuille. |

## 11. Contrat de sortie d’une analyse

Chaque analyse doit fournir au minimum les éléments suivants.

| Bloc | Contenu attendu |
|---|---|
| Identification | Actif, marché, date/heure d’analyse, version du moteur, fenêtre réellement utilisée. |
| Patterns | Pattern principal, patterns alternatifs compatibles, niveau de confiance par pattern, justification. |
| État | État d’avancement du pattern dans un vocabulaire lisible par un débutant. |
| Recommandation | Acheter / surveiller / attendre confirmation / conserver / renforcer / alléger / vendre selon le cas. |
| Risque | Niveau d’invalidation, stop loss suggéré, take profit suggéré, ratio risque/rendement, volatilité, drawdown potentiel si disponible. |
| Explication | Résumé pédagogique clair, sans jargon non expliqué, explicitant le raisonnement. |
| Contexte portefeuille | Présence ou non d’une position, synthèse des lignes détenues utiles à la contextualisation. |

## 12. États recommandés pour l’état d’avancement du pattern

Pour rester compréhensible par un débutant, l’état d’avancement doit utiliser une taxonomie finie et stable.

| État | Signification produit |
|---|---|
| Pattern envisagé | Des éléments précurseurs existent mais la configuration reste préliminaire. |
| En formation | Le pattern se structure avec suffisamment d’indices pour être suivi. |
| Proche de validation | Le pattern est avancé mais pas encore confirmé. |
| Confirmé | Les critères de validation définis par le pattern sont atteints. |
| Invalidé | Les conditions rendent le pattern non exploitable ou contredisent sa lecture. |

## 13. Taxonomie de recommandations

| Recommandation | Usage métier |
|---|---|
| Surveiller | L’actif mérite un suivi mais le signal est insuffisant pour agir. |
| Attendre confirmation | La thèse est intéressante mais une validation objective manque encore. |
| Acheter | Le cadre de marché et de risque soutient une entrée pédagogique. |
| Conserver | Le détenteur en portefeuille peut maintenir sa position selon les paramètres actuels. |
| Renforcer | Le détenteur peut envisager une augmentation de position dans un cadre jugé cohérent. |
| Alléger | Le détenteur peut réduire son exposition au regard du risque ou du potentiel résiduel. |
| Vendre | Le contexte invalide ou dégrade suffisamment la situation pour justifier une sortie pédagogique. |

## 14. Portefeuille utilisateur

### 14.1 Données minimales par ligne d’achat

| Champ | Obligatoire | Commentaire |
|---|---|---|
| Actif / valeur | Oui | Référence de l’instrument suivi ou détenu. |
| Quantité | Oui | Nombre d’actions détenues. |
| Prix d’achat | Oui | Prix unitaire d’exécution. |
| Date d’achat | Oui | Date de la ligne d’achat. |
| PRU | Oui | Peut être dérivé mais reste affichable et historisable. |
| Frais | Oui | Intégrés au coût de revient. |
| Devise | Oui | Permet la cohérence future multi-marchés. |

### 14.2 Règle de contextualisation

- Un utilisateur non exposé peut recevoir une recommandation de type surveiller, attendre confirmation ou acheter.
- Un utilisateur déjà exposé peut recevoir une recommandation de type conserver, renforcer, alléger ou vendre.
- La détection du pattern et l’évaluation du risque restent identiques pour tous ; seule la formulation finale de la recommandation est contextualisée par la position.

### 14.3 Règle V1 de reconstruction des lignes ouvertes après ventes

En V1, la reconstruction des lignes d’achat restantes suit une règle FIFO stricte.

Règles applicables :

- chaque transaction d’achat crée une ligne d’achat ;
- chaque transaction de vente consomme d’abord les quantités restantes des lignes d’achat les plus anciennes encore ouvertes ;
- une ligne est ouverte si sa quantité restante est strictement positive ;
- une ligne est fermée si sa quantité restante est nulle ;
- `PortfolioContext.openLines` contient uniquement les lignes encore ouvertes après application de cette consommation FIFO ;
- cette règle FIFO est utilisée en V1 pour la contextualisation portefeuille et pour reconstruire les lignes ouvertes nécessaires à l’analyse ;
- la détection du pattern et l’évaluation du risque ne dépendent pas de cette règle de consommation ;
- les frais de vente ne modifient pas la quantité restante des lignes ouvertes ; ils relèvent d’un traitement ultérieur de performance réalisée et ne changent pas la reconstruction des lignes ouvertes en V1.

## 15. Analyse journalière et déclenchement

| Mode | Description |
|---|---|
| À la demande | Mode prioritaire V1 ; l’utilisateur lance une analyse depuis un espace dédié. |
| Batch nocturne | Mode prévu à moyen terme ; permet de pré-calculer des analyses consultables le lendemain. |

Le système doit être conçu pour supporter les deux modes sans dupliquer la logique métier.

## 16. Historisation, versionnage et auditabilité

- Chaque analyse est un snapshot versionné.
- Le snapshot doit référencer l’actif, la date/heure, la source de données, la fenêtre d’analyse, la version du moteur, les patterns détectés, les scores, la recommandation et le contexte portefeuille.
- Les modifications futures du moteur ne doivent pas altérer rétroactivement les analyses déjà produites sans trace explicite.
- L’historique doit permettre des comparaisons inter-temporelles et des diagnostics de régression fonctionnelle.

## 17. Performance ex post des signaux

Le produit doit mesurer la qualité des signaux avec un protocole stable et comparable dans le temps.

| Mesure | Principe V1 recommandé |
|---|---|
| Performance à J+5 | Variation observée par rapport au prix de référence de l’analyse. |
| Performance à J+20 | Mesure de prolongement moyen terme du signal. |
| Stop / invalidation | Indicateur binaire et date d’atteinte si le niveau d’invalidation a été touché. |
| Take profit | Indicateur binaire et date d’atteinte si l’objectif a été touché. |
| Drawdown max | Pire excursion défavorable observée sur la période de suivi. |
| Confirmation du pattern | Évaluation ex post selon les règles de validation du pattern. |

## 18. Explicabilité

- Tout résultat présenté doit être relié à des règles identifiables.
- Le produit doit pouvoir expliquer pourquoi un pattern est proposé, pourquoi plusieurs patterns coexistent et pourquoi une recommandation est retenue.
- Le résumé pédagogique doit rester aligné avec la vérité calculée et ne pas inventer de causalité non prouvée.

## 19. Place de l’IA

| Périmètre | Décision |
|---|---|
| V1 | Aucune IA obligatoire. La V1 peut être entièrement déterministe. |
| Rôle possible futur | Reformulation pédagogique, synthèse textuelle, aide à la compréhension. |
| Rôle explicitement exclu comme vérité primaire | Détection seule de pattern, scoring seul, recommandation seule, niveaux de risque seuls. |

## 20. Exigences non fonctionnelles

| Axe | Exigence |
|---|---|
| Maintenabilité | Ajout simple de nouveaux patterns, nouvelles valeurs et nouvelles sources de données. |
| Traçabilité | Journalisation et historisation suffisantes pour auditer un résultat. |
| Testabilité | Règles déterministes testables unitairement et fonctionnellement. |
| Sécurité | Protection des comptes utilisateurs et des données de portefeuille. |
| Performance | Réponse acceptable sur analyse journalière à la demande et support futur des batchs nocturnes. |
| Clarté UX | Résultats compréhensibles par un débutant sans sacrifier la rigueur. |

## 21. Critères d’acceptation produit V1

- Un utilisateur peut créer et maintenir son portefeuille avec plusieurs lignes par actif.
- Une analyse journalière à la demande peut être déclenchée sur une valeur prise en charge.
- Le système affiche un pattern principal et, si nécessaire, plusieurs patterns alternatifs compatibles.
- Chaque résultat inclut confiance, état d’avancement, recommandation, invalidation, stop loss, take profit, ratio risque/rendement et résumé pédagogique.
- L’explication des résultats est alignée sur des règles auditées.
- Chaque analyse est historisée et versionnée.
- Le modèle est ouvert à l’ajout de nouveaux patterns sans recoder le noyau de toute l’application.
- Pour un actif avec historique d’achats et de ventes, le système est capable de reconstruire les lignes ouvertes en appliquant la règle FIFO V1 définie au présent document.

## 22. Questions encore ouvertes à affiner en spécification détaillée de conception

- Liste priorisée des patterns à implémenter en premier.
- Description formelle et sources de référence pour chaque pattern.
- Choix précis des fournisseurs de données de marché.
- Format exact du snapshot d’analyse et de l’évaluation ex post.
- Règles fines de calcul du niveau de confiance.
- Stratégie d’ouverture à d’autres actifs et marchés après la V1.
