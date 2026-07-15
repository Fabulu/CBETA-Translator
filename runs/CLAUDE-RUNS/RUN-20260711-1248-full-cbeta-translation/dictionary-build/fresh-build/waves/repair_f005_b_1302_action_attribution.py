from pathlib import Path
import datetime
import hashlib
import json
import os
import tempfile


R = Path(__file__).resolve().parents[2]
ENTRY_1301 = R / "fresh-build/entries/t_df028fd6bd35/entry.v2.json"
ENTRY_1302 = R / "fresh-build/entries/t_705aabe99572/entry.v2.json"
DRAFT_1302 = R / "fresh-build/entries/t_705aabe99572/evidence.draft.json"
PENDING = R / "fresh-build/waves/f005-laneB-1301-1302-pending-roster.json"
EXPECTED_1301 = "525987c476729e770717f41d5bf51d85884790666025262fe375dc2d1b414de8"
RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]


def atomic_json(path, payload):
    fd, tmp = tempfile.mkstemp(prefix=path.name + ".", dir=path.parent)
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as handle:
            json.dump(payload, handle, ensure_ascii=False, indent=2)
            handle.write("\n")
        os.replace(tmp, path)
    finally:
        if os.path.exists(tmp):
            os.unlink(tmp)


assert hashlib.sha256(ENTRY_1301.read_bytes()).hexdigest() == EXPECTED_1301
draft = json.loads(DRAFT_1302.read_text(encoding="utf-8"))
sense = draft["Entry"]["Senses"][0]
performers = [
    "Wuzhun Shifan",
    "Dahui Zonggao",
    "Yuanwu Keqin",
    "Dahui Zonggao",
    "Hongjue Min",
    "Zhean Jingfan",
    "Jinshan Tanying",
]
titles = [
    "無準師範禪師語錄", "大慧普覺禪師語錄", "圓悟佛果禪師語錄", "續燈正統",
    "弘覺忞禪師語錄", "蔗菴範禪師語錄", "續傳燈錄",
]
now = datetime.datetime.now(datetime.timezone.utc).isoformat()
for occurrence, performer, title in zip(sense["Occurrences"], performers, titles):
    occurrence["MasterName"] = None
    occurrence["ContextMasters"] = [{"MasterName": performer, "Roles": ["action-performer"]}]
    occurrence["ActorAttribution"] = {
        "Status": "narrated",
        "Kind": "narrated stage direction",
        "ActorLabel": "the source compiler or recorder",
        "ActorRole": "compiler",
        "RungsChecked": RUNGS,
        "GrammarEvidence": (
            f"The narrator's stage direction reports {performer} performing 卓一下; "
            "the physical action is not an utterance of the headword."
        ),
        "ReviewedBy": "Codex f005 lane B attribution repair author",
        "ReviewedUtc": now,
    }
    occurrence["AttributionNote"] = (
        f"Source text ({title}; {occurrence['RelPath']}). The source compiler or recorder narrates the stage direction translated as ‘strike once’; "
        f"{performer} is the named action performer, not the utterer of the headword."
    )
    occurrence["DraftActorProof"] = {
        "GrammaticalSubject": "the source compiler or recorder's narrative voice",
        "FullCaseDecision": (
            f"Full-case review identifies {performer} as the physical action performer, "
            "while 卓一下 remains a narrated stage direction with no headword utterer."
        ),
    }

sense["RelatedMasters"] = list(dict.fromkeys(performers))
sense["ExplanationParts"]["EvidenceBody"] = [
    "The records narrate Wuzhun Shifan striking once after telling the assembly to hear the staff speak, and Dahui Zonggao striking once before asking whether they hear.",
    "A stage direction narrates Zhean Jingfan raising the staff, calling it the fine finger, striking once, and then calling the sound the fine note.",
    "The narrators place the same one-blow action by Yuanwu Keqin, Hongjue Min, Jinshan Tanying, and Dahui Zonggao at a turn in their formal addresses; these masters perform the action but do not utter the headword.",
]
atomic_json(DRAFT_1302, draft)

pending = json.loads(PENDING.read_text(encoding="utf-8"))
pending["generatedUtc"] = now
for candidate in pending["candidates"]:
    if candidate["canonicalName"] == "Yunju Shouyi":
        candidate["canonicalName"] = "Jinshan Tanying"
        candidate["aliases"] = ["Jinshan Tanying", "Daguan Tanying"]
        candidate["reviewedBy"] = "Codex f005 lane B attribution repair author"
atomic_json(PENDING, pending)
assert hashlib.sha256(ENTRY_1301.read_bytes()).hexdigest() == EXPECTED_1301
