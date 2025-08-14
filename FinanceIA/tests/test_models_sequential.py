from pipeline.pipeline_double_top import models_sequential as ms
import tensorflow as tf

def test_build_cnn(input_shape):
    model = ms.build_cnn(input_shape)
    assert isinstance(model, tf.keras.Model)

def test_build_lstm(input_shape):
    model = ms.build_lstm(input_shape)
    assert isinstance(model, tf.keras.Model)

def test_build_transformer(input_shape):
    model = ms.build_transformer(input_shape)
    assert isinstance(model, tf.keras.Model)
