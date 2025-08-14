import logging
import logging.config
import os
from logging.handlers import TimedRotatingFileHandler

import structlog
from structlog.stdlib import ProcessorFormatter


def setup_logging(log_dir="logs", level=logging.DEBUG):
    if not os.path.exists(log_dir):
        os.makedirs(log_dir)
    log_file_path = os.path.join(log_dir, "app.log")

    file_handler = TimedRotatingFileHandler(
        log_file_path, when='midnight', interval=1, backupCount=30
    )

    console_handler = logging.StreamHandler()
    console_handler.setLevel(level)

    fmt = ProcessorFormatter(
        processor=structlog.processors.JSONRenderer(),
        foreign_pre_chain=[
            structlog.stdlib.filter_by_level,
            structlog.stdlib.add_log_level,
            structlog.processors.TimeStamper(fmt="iso"),
        ],
    )
    file_handler.setFormatter(fmt)
    console_handler.setFormatter(fmt)

    logging.basicConfig(
        handlers=[file_handler, console_handler],
        level=level,
        format="%(message)s",
    )

    structlog.configure(
        logger_factory=structlog.stdlib.LoggerFactory(),
        processors=[
            structlog.stdlib.filter_by_level,
            structlog.stdlib.add_log_level,
            structlog.processors.TimeStamper(fmt="iso"),
            structlog.processors.JSONRenderer(),
        ],
    )


if __name__ == '__main__':
    setup_logging()
    logger = structlog.get_logger(__name__)
    logger.info('Logging configured')
