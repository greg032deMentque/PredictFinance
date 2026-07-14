# Besoin métier V1 — résumé de travail

## Objectif produit
Construire une application pédagogique d’aide à l’investissement pour particulier débutant.

## Ce que l’application doit permettre
- gérer une watchlist
- gérer un portefeuille
- enregistrer plusieurs lignes d’achat par valeur
- analyser des cours de marché en journalier
- détecter des patterns graphiques
- comprendre plusieurs scénarios possibles
- recevoir des recommandations pédagogiques
- consulter l’historique des analyses

## Positionnement produit
- outil d’analyse et de conseil pédagogique
- pas un broker
- pas de passage d’ordre en V1
- pas d’accès aux comptes bancaires
- pas de tick-by-tick temps réel

## Scope V1
- utilisateur cible : particulier débutant
- actifs : actions françaises
- granularité : bougies journalières
- IA : optionnelle et périphérique
- source de vérité métier : API

## Décision métier V1 complémentaire
- la reconstruction des lignes d’achat ouvertes après ventes suit une règle FIFO stricte
- chaque achat crée une ligne
- chaque vente consomme d’abord les quantités restantes des lignes les plus anciennes encore ouvertes
- le contexte portefeuille utilisé pour la recommandation et l’explication est construit à partir de ces lignes ouvertes restantes
- cette règle V1 sert à la contextualisation portefeuille et ne vaut pas politique fiscale/comptable générale


## Besoin pédagogique complémentaire
- lecture paramètre par paramètre
- définition simple de chaque métrique visible
- explication de la valeur actuelle
- implication pédagogique selon que l'utilisateur détient déjà ou non le support
- aucun paramètre ne doit être présenté comme un signal de timing autonome
