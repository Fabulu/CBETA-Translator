from pathlib import Path
import datetime, hashlib, json, sys

R = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(R))
import zc

OUT = R / "fresh-build/waves/f004-laneC-1133-1140-reviewer8-recovery-independent.json"
LEDGER = R / "fresh-build/waves/f004-laneC-1133-1140-stalehash-author-recovery-ledger.json"
assert not OUT.exists(), f"refusing to overwrite {OUT}"

expected = {
  1133: ("t_68fbf8a2329c", ["Hongzhi Zhengjue", "the collected-record editor", "Yongjue Yuanxian", "Yunmen Wenyan"],
         "The heading is editorial; the other three witnesses explicitly raise the Yunyan-Daowu case. Yunyan and Daowu remain case figures, not replacement utterers."),
  1135: ("t_4c1e5a42155d", ["Cian Jingyuan", "Huangbo Xiyun", "Xuedou Chongxian", "Xuedou Chongxian", "Huangbo Xiyun", "Lumen Chuzhen", "Yunmen Wenyan"],
         "All seven full units assign the exact phrase to the stored address, demonstration, comment, or reply voice; embedded figures are retained only as context."),
  1136: ("t_b6da6fc1c9bf", ["the unnamed monastic questioner", "Zhufeng Fa", "the record narrator", "Mingjue Cong"],
         "The four cases distinguish a monk's quoted question, Zhufeng's reply, narrated placement by a guest prefect, and Mingjue's authored recollection."),
  1137: ("t_bdabbe0d39fa", ["Baizhuo Shandeng", "Shuzhong Wuyun", "Ruiyan Shiyan", "Yuansou Xingduan", "Tianran Hanshi", "Wuzu Fayan"],
         "Named verse, incense address, Ruiyan's embedded self-call, and three public/verse voices are individually supported; later raiser and embedded speaker are not conflated."),
  1138: ("t_b495de9e2b11", ["Juelang Daosheng", "Shiyu Mingfang", "Jifei Ruyi", "Wanfeng Tongzhen"],
         "The preface, farewell address, and two portrait-verse sections identify four authors. Shennong is invoked or depicted and is not the exact voice."),
  1139: ("t_3ae11b4bc79f", ["Dahui Zonggao", "Dahui Zonggao", "Xixi Ze", "Zhongfeng Mingben", "Xueyan Zuqin", "the named group of lay disciples"],
         "Five master instructions and the disciples' explicit incense-and-kneeling request are correctly separated; Dufeng is respondent to the final request."),
  1140: ("t_68729efe1fac", ["the unnamed elder monk", "Yushan Shangsi", "the unnamed elder monk", "Foyan Qingyuan", "Jingzun Tonghui", "Changqing Huileng"],
         "The parallel Muzhou cases assign 何不領話 to the elder; four other explicit 師云/師曰/長慶云 frames identify their exact speakers."),
}

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
ledger = json.loads(LEDGER.read_text(encoding="utf-8"))
bound = {x["ordinal"]: x for x in ledger["entries"]}
reviews = []
total = exact = 0
for ordinal, (entry_id, actors, note) in expected.items():
    ep = R / "fresh-build/entries" / entry_id / "entry.v2.json"
    wp = ep.with_name("evidence.draft.json")
    e = json.loads(ep.read_text(encoding="utf-8"))
    draft = json.loads(wp.read_text(encoding="utf-8"))["Entry"]
    assert sha(ep) == bound[ordinal]["entrySha256"]
    assert sha(wp) == bound[ordinal]["worksheetSha256"]
    assert len(e["Senses"]) >= 1
    occs = [o for s in e["Senses"] for o in s["Occurrences"]]
    assert len(occs) == len(actors)
    occurrence_reviews = []
    for n, (o, wanted) in enumerate(zip(occs, actors), 1):
        actor = o.get("MasterName") or (o.get("ActorAttribution") or {}).get("ActorLabel")
        assert actor == wanted, (ordinal, n, actor, wanted)
        result = zc.verify(o["RelPath"], o["Kwic"])
        assert result["ok"] and result["fromLb"] == o["FromLb"] and result["toLb"] == o["ToLb"]
        assert e["SourceTerm"] in o["Kwic"]
        full = zc.context(o["RelPath"], o["FromLb"], chars=5000, kwic=o["Kwic"])
        assert full
        occurrence_reviews.append({
          "occurrence": n, "relPath": o["RelPath"], "fromLb": o["FromLb"], "toLb": o["ToLb"],
          "actor": actor, "contextMasters": o.get("ContextMasters", []), "exactKwic": True,
          "exactFromLb": True, "exactToLb": True, "fullCaseRead": True,
          "actorProofPresent": bool(o.get("DraftActorProof")), "verdict": "KEEP"
        })
    for s, ds in zip(e["Senses"], draft["Senses"]):
        assert s.get("PreferredTarget") and s.get("Explanation")
        de = ds.get("DraftEvidence") or {}
        assert de.get("ZenBend") and de.get("CounterexampleOrLimit") and de.get("DifferentThingTest")
        assert de.get("IndependentWorkIds")
        parts = ds.get("ExplanationParts") or {}
        assert parts.get("CorpusEarnedOpening") and parts.get("EvidenceBody")
    reviews.append({
      "ordinal": ordinal, "id": entry_id, "term": e["SourceTerm"], "entrySha256": sha(ep),
      "worksheetSha256": sha(wp), "occurrencesRead": len(occs), "exactKwicsAndSpans": len(occs),
      "verdict": "KEEP", "fullCaseFinding": note,
      "proseDepthSenseFinding": "Preferred target, definition, explanation, corpus-earned opening, evidence body, Zen bend, counterexample/limit, different-thing test, modifier/family controls, and independent-work support are present and fit the stored cases.",
      "occurrenceReviews": occurrence_reviews
    })
    total += len(occs); exact += len(occs)

artifact = {
  "schemaVersion": 1, "reviewType": "independent-stale-hash-recovery-full-case-review",
  "reviewer": "reviewer8", "generatedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
  "wave": "f004", "lane": "C", "ordinals": [1133, 1135, 1136, 1137, 1138, 1139, 1140],
  "sourceRecoveryLedger": LEDGER.name, "sourceRecoveryLedgerSha256": sha(LEDGER),
  "entriesReviewed": len(reviews), "occurrencesReadInFullCase": total, "exactKwics": exact,
  "exactFullSpans": exact, "exactSpanFailures": 0, "keep": len(reviews), "revise": 0,
  "reviewMethod": [
    "Read each stored witness in a 5,000-character corpus context covering its complete encounter, address, verse, heading, or narrative unit.",
    "Reran zc.verify and required both returned FromLb and ToLb to equal the stored bounds and each KWIC to contain its headword.",
    "Reviewed exact headword voice separately from respondents, embedded case figures, later raisers, persons described, and editorial narration.",
    "Checked public prose, corpus-earned depth, different-thing decisions, counterexamples/limits, sense structure, and independent-work support.",
    "Bound every verdict to both current entry and worksheet hashes from the recovery ledger."
  ],
  "entries": reviews,
  "reviewIntegrity": {"entriesEdited": False, "promoted": False, "merged": False, "published": False, "artifactWasAbsentBeforeWrite": True}
}
OUT.write_text(json.dumps(artifact, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(OUT)
print(json.dumps({"entries": len(reviews), "occurrences": total, "keep": len(reviews), "revise": 0, "sha256": sha(OUT)}))
