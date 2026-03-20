import logging
import os
import json
from pathlib import Path
from logging.handlers import TimedRotatingFileHandler

try:
    import structlog
    from structlog.stdlib import ProcessorFormatter
except ModuleNotFoundError:  # pragma: no cover - depends on local environment
    structlog = None
    ProcessorFormatter = None

DEFAULT_LOG_DIR = Path(__file__).resolve().parent / "Logs"


def _resolve_log_dir() -> Path:
    return Path(os.environ.get("FINANCE_IA_LOG_DIR", str(DEFAULT_LOG_DIR)))


def _is_console_logging_enabled() -> bool:
    return os.environ.get("FINANCE_IA_LOG_CONSOLE", "").strip().lower() in {"1", "true", "yes", "on"}

def _build_timed_handler(base_path: Path, level: int) -> TimedRotatingFileHandler:
    base_name_no_ext = base_path.with_suffix("").name
    base_ext = base_path.suffix
    h = TimedRotatingFileHandler(
        filename=str(base_path),
        when="midnight",
        interval=1,
        backupCount=30,
        encoding="utf-8",
        utc=False,
    )
    h.setLevel(level)
    h.suffix = "%Y-%m-%d"

    def namer(default_name: str) -> str:
        p = Path(default_name)
        date_part = p.name.split(".")[-1]
        return str(p.with_name(f"{base_name_no_ext}_{date_part}{base_ext}"))

    h.namer = namer
    return h


class _JsonFallbackFormatter(logging.Formatter):
    def format(self, record: logging.LogRecord) -> str:
        payload = {
            "timestamp": self.formatTime(record, datefmt="%Y-%m-%dT%H:%M:%S"),
            "level": record.levelname.lower(),
            "logger": record.name,
            "message": record.getMessage(),
        }
        if record.exc_info:
            payload["exception"] = self.formatException(record.exc_info)
        return json.dumps(payload, ensure_ascii=False)


def setup_logging(level: int = logging.INFO) -> None:
    """
    Configure le logging avec rotation quotidienne.
    Les fichiers sont créés dans LOG_DIR.
    """
    log_dir = _resolve_log_dir()
    log_dir.mkdir(parents=True, exist_ok=True)

    file_all = _build_timed_handler(log_dir / "app_all.log", level=level)
    file_errors = _build_timed_handler(log_dir / "app_errors.log", level=logging.ERROR)

    if structlog is not None and ProcessorFormatter is not None:
        human_formatter = ProcessorFormatter(
            processors=[
                ProcessorFormatter.remove_processors_meta,
                structlog.dev.ConsoleRenderer(colors=True),
            ],
            foreign_pre_chain=[
                structlog.stdlib.filter_by_level,
                structlog.stdlib.add_log_level,
                structlog.processors.TimeStamper(fmt="iso", utc=False),
            ],
        )
        json_formatter = ProcessorFormatter(
            processors=[
                ProcessorFormatter.remove_processors_meta,
                structlog.processors.JSONRenderer(),
            ],
            foreign_pre_chain=[
                structlog.stdlib.filter_by_level,
                structlog.stdlib.add_log_level,
                structlog.processors.TimeStamper(fmt="iso", utc=False),
            ],
        )
    else:
        human_formatter = logging.Formatter("%(asctime)s %(levelname)s %(name)s %(message)s")
        json_formatter = _JsonFallbackFormatter()

    file_all.setFormatter(json_formatter)
    file_errors.setFormatter(json_formatter)

    root = logging.getLogger()
    root.handlers.clear()
    root.setLevel(level)
    root.addHandler(file_all)
    root.addHandler(file_errors)

    if _is_console_logging_enabled():
        console = logging.StreamHandler()
        console.setLevel(level)
        console.setFormatter(human_formatter)
        root.addHandler(console)

    if structlog is not None:
        structlog.configure(
            processors=[
                structlog.contextvars.merge_contextvars,
                structlog.processors.StackInfoRenderer(),
                structlog.processors.format_exc_info,
                structlog.stdlib.ProcessorFormatter.wrap_for_formatter,
            ],
            logger_factory=structlog.stdlib.LoggerFactory(),
            wrapper_class=structlog.make_filtering_bound_logger(level),
            cache_logger_on_first_use=True,
        )

    logging.getLogger("urllib3").setLevel(logging.WARNING)
    logging.getLogger("mlflow").setLevel(logging.WARNING)
