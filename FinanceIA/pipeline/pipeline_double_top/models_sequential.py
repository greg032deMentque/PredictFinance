#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
models_sequential.py

Module pour construire plusieurs modèles de réseaux de neurones séquentiels
(en CNN, LSTM, et Transformer) à l'aide de TensorFlow/Keras.

Cette version est entièrement commentée pour un débutant en Python et en IA :
chaque import, chaque couche, et chaque étape de construction de modèle
est expliquée pas à pas.
"""

# Import de TensorFlow, la bibliothèque de référence pour l'apprentissage profond
import tensorflow as tf
# Import des couches spécifiques MultiHeadAttention et LayerNormalization pour le Transformer
from tensorflow.keras.layers import MultiHeadAttention, LayerNormalization


def build_cnn(input_shape):
    """
    Construit un modèle de réseau de neurones convolutif 1D (CNN) adapté à des données séquentielles.

    Args:
        input_shape (tuple): forme des données d'entrée, par exemple (timesteps, features).

    Returns:
        tf.keras.Model: modèle CNN prêt à être compilé et entraîné.
    """
    # Déclaration de la couche d'entrée avec la forme fournie
    inp = tf.keras.Input(shape=input_shape)

    # 1. Convolution 1D : 32 filtres, taille de noyau 3, activation ReLU
    #    Cette couche va apprendre des motifs locaux dans la séquence.
    x = tf.keras.layers.Conv1D(32, kernel_size=3, activation="relu")(inp)

    # 2. Pooling : réduit la dimension temporelle / séquentielle de moitié
    x = tf.keras.layers.MaxPool1D(2)(x)

    # 3. Deuxième couche de convolution : 64 filtres, noyau de taille 3
    x = tf.keras.layers.Conv1D(64, 3, activation="relu")(x)

    # 4. Global Max Pooling : prend le maximum sur toute la dimension temporelle
    #    pour obtenir un vecteur fixe quelle que soit la longueur d'entrée.
    x = tf.keras.layers.GlobalMaxPool1D()(x)

    # 5. Couche Dense (Fully Connected) : 32 neurones, activation ReLU
    #    pour combiner les caractéristiques extraites.
    x = tf.keras.layers.Dense(32, activation="relu")(x)

    # 6. Couche de sortie : 1 neurone, activation sigmoïde
    #    adaptée à un problème de classification binaire (valeurs entre 0 et 1).
    out = tf.keras.layers.Dense(1, activation="sigmoid")(x)

    # Création et retour du modèle en reliant l'entrée à la sortie
    return tf.keras.Model(inp, out)


def build_lstm(input_shape):
    """
    Construit un modèle à base de LSTM (Long Short-Term Memory),
    adapté aux séries temporelles ou données séquentielles.

    Args:
        input_shape (tuple): forme des données d'entrée, par exemple (timesteps, features).

    Returns:
        tf.keras.Model: modèle LSTM prêt à être compilé et entraîné.
    """
    # Couche d'entrée
    inp = tf.keras.Input(shape=input_shape)

    # 1ère couche LSTM : 64 unités, return_sequences=True pour renvoyer la séquence entière
    #    nécessaire si on empile plusieurs LSTM.
    x = tf.keras.layers.LSTM(64, return_sequences=True)(inp)

    # 2e couche LSTM : 32 unités, par défaut return_sequences=False (on ne renvoie que le dernier état)
    x = tf.keras.layers.LSTM(32)(x)

    # Couche de sortie : 1 neurone, activation sigmoïde pour classification binaire
    out = tf.keras.layers.Dense(1, activation="sigmoid")(x)

    # Création et retour du modèle
    return tf.keras.Model(inp, out)


def build_transformer(
    input_shape,
    head_size=32,
    num_heads=2,
    ff_dim=64
):
    """
    Construit un modèle de type Transformer simple pour des données séquentielles.

    Args:
        input_shape (tuple): forme des données d'entrée, par exemple (timesteps, features).
        head_size (int): dimension de chaque tête d'attention.
        num_heads (int): nombre de têtes dans le mécanisme d'attention multi-têtes.
        ff_dim (int): dimension de la partie feed-forward interne.

    Returns:
        tf.keras.Model: modèle Transformer prêt à être compilé et entraîné.
    """
    # Couche d'entrée
    inp = tf.keras.Input(shape=input_shape)

    # 1. Normalisation de la couche d'entrée pour stabiliser l'entraînement
    x = LayerNormalization()(inp)

    # 2. Attention multi-têtes : calcule des poids d'attention entre les positions de la séquence
    attn = MultiHeadAttention(num_heads=num_heads, key_dim=head_size)(x, x)

    # 3. Ajout & Normalisation : connexion résiduelle entre l'entrée et la sortie de l'attention
    x = LayerNormalization()(attn + inp)

    # 4. Feed-Forward : une couche Dense interne avec activation ReLU
    ff = tf.keras.layers.Dense(ff_dim, activation="relu")(x)

    # 5. Deuxième connexion résiduelle et normalisation
    x = LayerNormalization()(ff + x)

    # 6. Global Average Pooling 1D : moyenne sur la dimension temporelle
    #    pour obtenir un vecteur de taille fixe.
    x = tf.keras.layers.GlobalAveragePooling1D()(x)

    # 7. Couche de sortie : 1 neurone, activation sigmoïde pour classification binaire
    out = tf.keras.layers.Dense(1, activation="sigmoid")(x)

    # Création et retour du modèle
    return tf.keras.Model(inp, out)
