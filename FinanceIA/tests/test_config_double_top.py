from pipeline.pipeline_double_top import config_double_top as cfg
from pathlib import Path

def test_config_constants_exist():
    assert hasattr(cfg, "LOOKBACK_WINDOW")
    assert hasattr(cfg, "LOOKAHEAD_WINDOW")
    assert cfg.LOOKBACK_WINDOW > 0
    assert cfg.LOOKAHEAD_WINDOW > 0

def test_paths_exist_or_can_be_created():
    if hasattr(cfg, "MODEL_DIR"):
        p = Path(cfg.MODEL_DIR)
        if not p.exists():
            p.mkdir(parents=True, exist_ok=True)
        assert p.exists()
