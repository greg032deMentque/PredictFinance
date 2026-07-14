# Planning de versioning — résumé de travail

## V1
- API = source de vérité
- détection, risque, recommandation, historisation côté API
- frontend = affichage
- Python = optionnel et non central

## V2
- nouveaux patterns
- nouveaux instruments / actifs
- batch nocturne
- début du suivi ex post

Règle de lecture :
- la V1 doit être conçue pour pouvoir ajouter le batch nocturne sans dupliquer la logique métier ;
- le batch nocturne lui-même n’est pas une fonctionnalité livrée en V1.

## V3
- supervision plus riche
- moteur ex post plus mature
- assistance IA pédagogique optionnelle
