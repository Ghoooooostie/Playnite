import base64
import os
import uuid

from qtsymbols import QApplication

import gobject


def _log(message: str):
    with open(gobject.getconfig("myanki_v2.log"), "a", encoding="utf8") as ff:
        ff.write(message + "\n")


def AnkiFields(luna_default_anki_fields: list):
    _log("AnkiFields called")
    if "screenshot" not in luna_default_anki_fields:
        return luna_default_anki_fields + ["screenshot"]
    return luna_default_anki_fields


def ParseFieldsData(text_fields: dict, audios: list, pictures: list):
    _log("ParseFieldsData called")
    screens = QApplication.screens()
    if not screens:
        _log("No screens found")
        return text_fields, audios, pictures

    pixmap = screens[0].grabWindow(0)
    if pixmap.isNull():
        _log("grabWindow returned null pixmap")
        return text_fields, audios, pictures

    ext = "png"
    fname = gobject.gettempdir(f"{uuid.uuid4()}.{ext}")
    pixmap.save(fname)
    _log(f"Saved screenshot to {fname}")

    with open(fname, "rb") as ff:
        data = base64.b64encode(ff.read()).decode()

    pictures = [
        {
            "data": data,
            "filename": os.path.basename(fname),
            "fields": ["screenshot"],
        }
    ]
    _log(f"Prepared pictures payload with {len(pictures)} item(s)")

    return text_fields, audios, pictures
