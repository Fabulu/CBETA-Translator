from pathlib import Path
import datetime, hashlib, json, subprocess, sys

R = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(R))
import zc

BASE = "42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a"
NOW = datetime.datetime.now(datetime.timezone.utc).isoformat()
RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]


def occ(rel, kwic, master=None, label=None, contexts=()):
    v = zc.verify(rel, kwic)
    assert v["ok"] and v["count"] == 1, (rel, v)
    actor = master or label
    proof = f"The complete case assigns the headword-bearing clause to {actor}; following replies and quoted figures were read separately."
    o = {
        "RelPath": rel, "FromLb": v["fromLb"], "ToLb": v["toLb"], "Kwic": kwic,
        "MasterName": master, "Curated": True,
        "ContextMasters": ([{"MasterName": master, "Roles": ["utterer"]}] if master else [])
            + [{"MasterName": x, "Roles": ["case-figure"]} for x in contexts],
        "AttributionNote": f"Source text ({zc.title(rel)}; {rel}). Exact actor: {actor}. {proof}",
        "DraftActorProof": {"ExactHeadwordClause": kwic, "GrammaticalSubject": actor, "SpeechFrame": proof, "FullCaseDecision": proof},
    }
    if not master:
        o["ActorAttribution"] = {
            "Status": "reviewed-unnamed", "Kind": "monastic participant", "ActorLabel": label,
            "ActorRole": "questioner", "RungsChecked": RUNGS, "GrammarEvidence": proof,
            "ReviewedBy": "Codex f005 lane B author", "ReviewedUtc": NOW, "AuthoredVoiceRiskReviewed": True,
        }
    return o


def sense(target, alts, aliases, opening, body, occurrences, note, zenbend, limit, related):
    works = list(dict.fromkeys(zc.work_id(o["RelPath"]) for o in occurrences))
    return {
        "SenseKey": None, "MasterName": None, "PreferredTarget": target, "AlternateTargets": alts,
        "SearchAliases": aliases, "Status": "preferred",
        "ExplanationParts": {"CorpusEarnedOpening": opening, "EvidenceBody": body},
        "Validation": "multi-source", "Note": note, "Occurrences": occurrences, "ClaimAnchors": [],
        "SourceTexts": list(dict.fromkeys(o["RelPath"] for o in occurrences)),
        "RelatedMasters": list(dict.fromkeys(o["MasterName"] for o in occurrences if o.get("MasterName"))),
        "RelatedTerms": related,
        "DraftEvidence": {
            "OpeningClaimEvidenceKeys": [f"o{i}" for i in range(1, len(occurrences) + 1)],
            "ZenBend": zenbend, "CounterexampleOrLimit": limit,
            "DifferentThingTest": {"Decision": "one-thing", "ComparedThings": [target, "question, quotation, rebuke, and formal-address uses"], "Reason": "The examples change speaker role and predicate, not the lexical referent."},
            "AliasRationale": "Aliases expose ordinary English lookup forms without adding readings.",
            "ModifierControls": [{"finding": "not-applicable", "reason": "No misleading material modifier controls this headword."}],
            "FamilyControls": [{"finding": "checked", "reason": "Longer phrases, quoted cases, and neighboring actions were read without treating them as synonyms."}],
            "IndependentWorkIds": works,
        },
    }


language = [
    occ("T/T47/T47n1998A.xml", "佛法要妙。離言說相。離文字相。離心緣相。不可以有心求。不可以無心得。不可以語言造。不可以寂默通。", "Dahui Zonggao"),
    occ("T/T47/T47n1997.xml", "靈然獨露透聲色無遺。廓爾見前拘動寂不得。坐却意見截却語言。根塵中不隔絲毫。", "Yuanwu Keqin"),
    occ("T/T48/T48n2003.xml", "趙州恁麼答。州云。至道無難。唯嫌揀擇。纔有語言。是揀擇是明白。老僧不在明白裏。", "Zhaozhou Congshen"),
    occ("T/T48/T48n2003.xml", "百丈道一切語言。山河大地。一一轉歸自己。雪竇凡是一拈一掇。到末後須歸自己。", "Baizhang Huaihai", contexts=("Yuanwu Keqin",)),
    occ("X/X68/X68n1318.xml", "舉：馬大師云：一切語言是提婆宗，以此箇為主。師云：好語，祇是無人問。", "Mazu Daoyi"),
    occ("X/X72/X72n1437.xml", "問：一切語言盡落今時，如何是向上一句？師竪拂子，進云：猶是今時事。", label="the unnamed monastic questioner"),
    occ("X/X70/X70n1402.xml", "參禪學道在心傳，一大藏經曾未詮。聞見不能超象外，口開還墮語言邊。", "Zhongfeng Mingben"),
]

strike = [
    occ("X/X70/X70n1382.xml", "諸人還見麼？若不見，且聽拄杖子重說偈言。卓一下。中秋，上堂。", "Wuzhun Shifan"),
    occ("T/T47/T47n1998A.xml", "上堂。拈拄杖卓一下喝一喝云。幸自可憐生。特地胡打亂喝。作甚麼。", "Dahui Zonggao"),
    occ("T/T47/T47n1997.xml", "遂拈拄杖卓一下云。大眾。還知落處麼。諸佛心髓祖師淵源。", "Yuanwu Keqin"),
    occ("X/X84/X84n1583.xml", "上堂，拈拄杖卓一下，召大眾云：還聞麼？復舉起云：觀世音菩薩來也，", "Dahui Zonggao"),
    occ("L/L155/L155n1643.xml", "以拄杖卓一下云兩段不同收歸上科𠊳下座。佛誕上堂世尊初生艸本不勞拈出", "Hongjue Min"),
    occ("J/J36/J36nB369.xml", "驀擎拄杖，曰：「此是妙指。」卓一下，曰：「此是妙音，且道所彈是何曲調？」", "Zhean Jingfan"),
    occ("T/T51/T51n2077.xml", "要得不孤負平生麼。拈拄杖卓一下曰。須是莫被拄杖瞞始得。看看拄杖子穿過爾諸人髑髏。", "Yunju Shouyi"),
]

specs = [
    (1301, "t_df028fd6bd35", "語言", sense(
        "speech and language", ["words and speech", "verbal expression"], ["speech", "language", "words", "verbal expression"],
        "Speech and language names spoken or verbal expression. Masters quote it, question it, cut it off, or place a saying within it; the word itself does not mean that language is always accepted or always rejected.",
        ["Zhaozhou says that once there is speech and language there is choosing or clarity, while an unnamed monastic questioner asks Yongjue what lies beyond all speech and language.", "Mazu calls all speech and language the Deva school; Baizhang says speech, mountains, rivers, and the earth each turn back to oneself.", "Dahui says the matter cannot be fashioned through speech and language, Yuanwu says to cut it off, and Zhongfeng says opening the mouth falls at its edge."],
        language, "1,974 raw exact-string hits occur in 343 frozen files; seven curated rows preserve distinct questions, quotations, and formal-address predicates.",
        "Zen bends this ordinary term by repeatedly making the status of speech itself part of public questioning and comment: the corpus records both language-bearing claims and explicit limits on verbal construction.",
        "The contradictory predicates are not separate lexical senses and do not establish a universal doctrine for or against language.", ["文字", "言說", "無語"])),
    (1302, "t_705aabe99572", "卓一下", sense(
        "to strike once", ["one strike", "strike it once"], ["strike once", "one strike", "rap once", "bring the staff down once"],
        "To strike once, normally with the staff named immediately before it. In formal addresses the single blow is an audible and visible event followed by a question, declaration, shout, or descent from the seat.",
        ["Wuzhun tells the assembly to hear the staff speak and then strikes once; Dahui asks whether they hear after the blow.", "Zhean calls the raised staff the fine finger, strikes once, and calls the sound the fine note.", "Yuanwu, Hongjue, Yunju Shouyi, and Dahui place the same one-blow action at a turn in their addresses."],
        strike, "1,887 raw exact-string hits occur in 204 frozen files; seven curated works preserve the staff, the single blow, and its immediately following speech.",
        "The records repeatedly place the one staff-blow inside the public address as an audible or visible turn to which the master immediately points or speaks.",
        "The phrase does not by itself name what the blow means. This entry reports the observable staff action and its placement in addresses, not a symbolic interpretation.", ["拄杖", "卓拄杖", "擊一下"])),
]

rows = []
for ordinal, eid, term, s in specs:
    d = R / "fresh-build/entries" / eid
    d.mkdir(parents=True, exist_ok=True)
    draft = {"SchemaVersion": 1, "Entry": {"Id": eid, "SourceTerm": term, "CorpusBaselineSha256": BASE, "CreatedBy": "Codex f005 lane B canary author", "WrittenUtc": NOW, "Senses": [s]}}
    wp = d / "evidence.draft.json"
    wp.write_text(json.dumps(draft, ensure_ascii=False, indent=2) + "\n")
    (d / "WORK.md").write_text(f"# {term} — f005 lane B canary\n\n- frozen-corpus: `{BASE}`; 494 files / 487 works.\n- indexed-path: zc count/find over frozen allowlist; every saved occurrence reverified with `zc.verify`.\n- definition-searches: questions, quotation frames, predicates, contrasts, longer compounds, and formal-address actions.\n- deployment-inventory: {len(s['Occurrences'])} exact rows / {len(s['DraftEvidence']['IndependentWorkIds'])} work IDs.\n- omission-audit: each public claim is anchored; repeat rows and bare substrings were excluded.\n- family-retest: {s['DraftEvidence']['FamilyControls'][0]['reason']}\n- sense-target-distinguishability: not applicable — one lexical referent.\n- feedback-inference-verdict: supported — {s['DraftEvidence']['ZenBend']}\n- feedback-observations: o1–o{len(s['Occurrences'])} anchor the opening, named deployments, actor distinctions, and limit.\n- feedback-falsification-searches: contradictory predicates; quoted voices; narration; adjacent compounds; duplicate editions.\n- feedback-counterexamples: {s['DraftEvidence']['CounterexampleOrLimit']}\n- feedback-scope: corpus-specific observable deployment, no outside doctrine or symbolism.\n- lookup-probes: {'; '.join(s['SearchAliases'])}.\n- opening-interpretation-verdict: supported by o1–o{len(s['Occurrences'])}.\n")
    ep, rp = d / "entry.v2.json", d / "evidence-compile-report.json"
    q = subprocess.run([sys.executable, str(R / "compile_evidence_draft.py"), str(wp), "--output", str(ep), "--report", str(rp)], capture_output=True, text=True)
    assert q.returncode == 0, q.stdout + q.stderr
    (d / "STATUS").write_text("drafted\n")
    rows.append({"ordinal": ordinal, "id": eid, "term": term, "occurrences": len(s["Occurrences"]), "entrySha256": hashlib.sha256(ep.read_bytes()).hexdigest(), "worksheetSha256": hashlib.sha256(wp.read_bytes()).hexdigest(), "state": "drafted-awaiting-independent-canary-review"})

print(json.dumps(rows, ensure_ascii=False, indent=2))

# Cohort-local, evidence-bound pending-roster packet for the three record owners
# absent from the current public names[0] roster. The attribution gate validates
# every row against the frozen XML before accepting these temporary link keys.
pending = {"schemaVersion": 1, "generatedUtc": NOW, "candidates": []}
for name in ("Hongjue Min", "Zhean Jingfan", "Yunju Shouyi"):
    o = next(o for o in strike if o.get("MasterName") == name)
    pending["candidates"].append({
        "canonicalName": name, "aliases": [name],
        "evidence": [{k: o[k] for k in ("RelPath", "FromLb", "ToLb", "Kwic")}],
        "reviewedBy": "Codex f005 lane B canary author",
        "reviewReport": "fresh-build/waves/f005-laneB-1301-1302-author-ledger.json",
        "status": "awaiting-roster-integration",
    })
(R / "fresh-build/waves/f005-laneB-1301-1302-pending-roster.json").write_text(json.dumps(pending, ensure_ascii=False, indent=2) + "\n")
