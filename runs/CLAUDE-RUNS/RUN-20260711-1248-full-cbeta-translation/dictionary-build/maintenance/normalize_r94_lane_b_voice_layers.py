#!/usr/bin/env python3
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
STAGE = ROOT / "fresh-build/r94-correction2-stage/entries"

# Closed, reviewed exceptional layers. Every other retained authored/speech
# family is a direct turn; this table does not make a new actor decision.
EXCEPTIONS = {
    ("t_250794fa9636", 1): "transmitted-verse",
    ("t_255626770dcc", 0): "quoted-original",
    ("t_25fb43689d5e", 0): "quoted-original",
    ("t_25fb43689d5e", 1): "transmitted-verse",
    ("t_25fb43689d5e", 2): "transmitted-verse",
    ("t_26a41c6b0def", 0): "quoted-original",
    ("t_26a41c6b0def", 3): "transmitted-verse",
}

IDS = [
    "t_240ea0594a5f", "t_2455261d9696", "t_2488565d7fba",
    "t_24adbdf51a15", "t_250794fa9636", "t_255626770dcc",
    "t_25fb43689d5e", "t_26818ad3df57", "t_2684c756a929",
    "t_26a41c6b0def",
]

for entry_id in IDS:
    path = STAGE / entry_id / "source-dossier.json"
    data = json.loads(path.read_text(encoding="utf-8"))
    for index, case in enumerate(data["retainedCompleteCases"]):
        case["voiceLayer"] = EXCEPTIONS.get((entry_id, index), "direct-turn")
    path.write_text(
        json.dumps(data, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
