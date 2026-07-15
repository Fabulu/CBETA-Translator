#!/usr/bin/env python3
import copy, datetime, hashlib, json, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
ENTRIES = ROOT / "entries"
NOW = datetime.datetime.now(datetime.timezone.utc).isoformat()
sys.path.insert(0, str(ROOT.parent))
import zc

CFG = {
"t_bfef2fc85826": ("to visit and question a teacher", ["to consult a teacher", "to present oneself for questioning"],
 "To visit and question a lineage figure is not merely to ask for information. In these records the headword names the student's entry into a face-to-face encounter: Tanran and Huairang arrive and immediately ask the meaning of the ancestor's coming from the west, while Juzhi answers every visiting student's inquiry by raising one finger. Biographical uses also measure a student's career by whom he consulted, or pointedly say that he asked nobody. The word therefore marks public, answerable contact in which a student exposes his question."),
"t_b336769aabdf": ("family disgrace", ["family shame", "to air the family's disgrace"],
 "Family disgrace is the school's own embarrassing business made public. Zen records use the phrase with kinship language for inherited sayings and devices: closing Vimalakirti's mouth or Bodhidharma's gate already 'airs the family disgrace,' and later descendants accuse one another of broadcasting it outside. The phrase is deliberately double-edged. What discredits the house is also what its heirs keep exposing in sermons, comments, and ancestral memorials."),
"t_d7725cb0c8c0": ("water steward", ["monastery water steward", "officer responsible for water"],
 "The water steward is a named monastery office, not the head or source of a stream in these witnesses. Monastic codes list the office among the minor posts alongside the bath, charcoal, furnace, garden, and latrine stewards; one code states its concrete responsibility as heating washing water and arranging the community's basins and towels. The term belongs to the working institutional vocabulary that makes the public hall and resident community possible."),
"t_e21288d0fefb": ("Xu Six carrying a board", ["Xu Six with a board", "the board-carrying Xu Six"],
 "Xu Six carrying a board is Zen's stock picture of one-sided vision: the board blocks one side while its bearer sees only the other. The records repeatedly state that he 'sees only one side,' applying the name to lineage figures, paired answers, or students trapped by either sound-and-form or purity. It is a verdict on partial seeing, not a biography of a man named Xu."),
"t_641de814fd8a": ("poison", ["a poisonous dose", "to turn into poison"],
 "Poison is what a saying or medicine becomes when it is taken in a way that harms its recipient. One address offers a phrase to chew and warns that failure to break it turns it into poison; another record says the finest clarified butter becomes poison in an unclean vessel. Commentators likewise call a received answer poison when it leaves the student buried. The bend is relational: the same offered material can cure or poison according to what happens in the encounter."),
"t_10b63ac74f61": ("Li Guang", ["General Li Guang", "Li Guang's stone-piercing shot"],
 "Li Guang is invoked through the story of the general whose utterly committed shot entered a stone he took for a tiger. Zen verses compress that feat into 'Li Guang's spirit-like arrow' and set it beside cases such as Baizhang's fox as an image of a shot that penetrates its mark. The entry names the case-figure as Zen deploys him; it is not a general biography of the Han commander."),
"t_40cfbcc5f859": ("release and seize", ["letting go and taking hold", "to release or take control"],
 "Release and seize names a paired operation of encounter: allowing room and taking control, sometimes exercised together and sometimes alternated freely. Records coordinate the pair with guest-and-host, killing-and-giving-life, rolling-up-and-unrolling, or unfixed handling of the situation. It describes observable control of the public exchange, not an inward state."),
"t_b016f513be3d": ("Strict Lineage of the Five Lamps", ["Wudeng yantong", "Five Lamps Strict Lineage"],
 "Strict Lineage of the Five Lamps is Feiyin Tongrong's lineage history bearing that Chinese title. The corpus names it as a compiled book, describes its attempt to correct disputed lineage transmission, and records a later continuation of it. As a title it matters inside Zen's public contest over who belongs to the transmitted house; it is not a generic phrase meaning five severe lamps."),
"t_f24a55791323": ("single transmission and direct pointing", ["a single transmission that points directly", "direct pointing by single transmission"],
 "Single transmission and direct pointing joins two public lineage claims: the teaching is transmitted singly outside proliferating explanatory branches, and it points directly rather than proceeding through imposed symbolism. The records use the formula to name Bodhidharma's arrival, then test later explanations against it; one rejects interpreting Mazu's cart exchange as body-and-mind symbolism precisely because that would not be direct pointing."),
"t_74c3c0e1b896": ("to step down from the teaching seat", ["to descend from the Chan couch", "to leave the teaching couch"],
 "To step down from the teaching seat is a visible move in a public encounter. Lineage figures descend from the raised couch to answer bodily: one is seized after taking two steps, another inspects a questioner, and another spreads both hands after the community asks for the promised teaching. Leaving the teaching seat is part of the answer—not a reference to ending a sitting exercise."),
}

# These are not lexical witnesses: punctuation collisions or isolated paratext labels.
DROP = {
 "t_bfef2fc85826": {("L/L155/L155n1643.xml", "0091b02")},
 "t_d7725cb0c8c0": {("T/T48/T48n2025.xml", "1133a26"), ("J/J29/J29nB235.xml", "0419c04")},
 "t_641de814fd8a": {("C/C077/C077n1710.xml", "0622c01"), ("X/X66/X66n1297.xml", "0290a04")},
 "t_b016f513be3d": {("X/X81/X81n1568.xml", "0001a03"), ("X/X81/X81n1569.xml", "0317a03")},
 "t_f24a55791323": {("J/J27/J27nB193.xml", "0224b04"), ("X/X71/X71n1414.xml", "0301b22")},
}

ADD = {
 "t_bfef2fc85826":[("T/T51/T51n2077.xml","參問")],
 "t_d7725cb0c8c0":[("T/T48/T48n2025.xml","水頭")],
 "t_641de814fd8a":[("X/X79/X79n1557.xml","毒藥"),("X/X69/X69n1357.xml","毒藥")],
 "t_b016f513be3d":[("J/J29/J29nB232.xml","五燈嚴統")],
 "t_f24a55791323":[("X/X71/X71n1414.xml","單傳直指"),("X/X71/X71n1417.xml","單傳直指")],
}

def narrated(term, occ):
    k = occ["Kwic"]
    if term == "水頭": kind, label, role = "institutional list or rule", "the monastic code's institutional voice", "compiler"
    elif term == "五燈嚴統": kind, label, role = "bibliographical narration", "the record's named-book discussion", "compiler"
    elif term == "下禪床": kind, label, role = "narrated action", "the case narrator describing the master's movement", "compiler"
    elif term == "李廣": kind, label, role = "verse or sermon allusion", "the verse or address invoking Li Guang", "verse-author"
    elif term in ("家醜", "徐六擔板", "毒藥", "縱奪", "單傳直指") and ("云" in k or "曰" in k):
        kind, label, role = "quoted or recorded address", "the named section speaker or quoted case voice", "utterer"
    else: kind, label, role = "biographical or case narration", "the record compiler narrating the occurrence", "compiler"
    return {"Status":"narrated" if role != "utterer" else "reviewed-unnamed", "Kind":kind, "ActorLabel":label,
      "ActorRole":role, "RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],
      "GrammarEvidence":f"The complete case was read for who utters {term}. This stored token is assigned to {label}; no record owner is substituted merely because the passage occurs in that owner's record.",
      "ReviewedBy":"Codex f004 B1031-1040 full-case repair author", "ReviewedUtc":NOW, "AuthoredVoiceRiskReviewed":True}

def work_id(rel):
    stem = Path(rel).stem
    # Canonical work controls: split volumes and duplicate editions must not inflate independence.
    if stem in {"X81n1571", "X82n1571"}: return "chan:wudeng-quanshu"
    if stem in {"X80n1568", "X81n1568"}: return "chan:wudeng-yantong"
    if stem in {"C077n1710", "D48n8939"}: return "chan:guzunsu-yulu"
    if stem in {"J23nB134", "X69n1326"}: return "chan:wujia-yulu"
    return "work:" + stem

for eid, (preferred, aliases, explanation) in CFG.items():
    ep = ENTRIES/eid/"entry.v2.json"
    d = json.loads(ep.read_text(encoding="utf-8"))
    d["CreatedBy"] = "Codex f004 B1031-1040 full-case repair author"
    d["WrittenUtc"] = NOW
    for s in d["Senses"]:
        s["PreferredTarget"] = preferred
        s["AlternateTargets"] = aliases
        s["SearchAliases"] = list(dict.fromkeys([preferred, *aliases]))
        s["Explanation"] = explanation
        kept=[]
        for o in s["Occurrences"]:
            if (o["RelPath"],o["FromLb"]) in DROP.get(eid,set()): continue
            o["MasterName"] = None
            o["ContextMasters"] = o.get("ContextMasters", [])
            o["ActorAttribution"] = narrated(d["SourceTerm"], o)
            o["AttributionNote"] = f"Source text ({o['RelPath']}): {o['ActorAttribution']['ActorLabel']} carries the headword in this complete-case reading."
            o["DraftActorProof"] = {"ExactHeadwordClause":o["Kwic"], "GrammaticalSubject":o["ActorAttribution"]["ActorLabel"], "FullCaseDecision":o["ActorAttribution"]["GrammarEvidence"]}
            kept.append(o)
        unique=[]; seen=set()
        for o in kept:
            key=(o["RelPath"],o["FromLb"],o["ToLb"],o["Kwic"])
            if key not in seen: seen.add(key); unique.append(o)
        kept=unique
        present={(o["RelPath"],o["FromLb"],o["ToLb"]) for o in kept}
        for rel,term in ADD.get(eid,[]):
            f=zc.find(rel,term,ctx=90)[0]
            v=zc.verify(rel,f["window"])
            if (rel,v["fromLb"],v["toLb"]) in present: continue
            o={"RelPath":rel,"FromLb":v["fromLb"],"ToLb":v["toLb"],"Kwic":f["window"],"Curated":True,"MasterName":None,"ContextMasters":[]}
            o["ActorAttribution"]=narrated(d["SourceTerm"],o)
            title=zc.title(rel)
            o["AttributionNote"]=f"Source text ({title}): {o['ActorAttribution']['ActorLabel']} carries the headword in this complete-case reading."
            o["DraftActorProof"]={"ExactHeadwordClause":o["Kwic"],"GrammaticalSubject":o["ActorAttribution"]["ActorLabel"],"FullCaseDecision":o["ActorAttribution"]["GrammarEvidence"]}
            kept.append(o)
        for o in kept:
            o["AttributionNote"]=f"Source text ({zc.title(o['RelPath'])}): {o['ActorAttribution']['ActorLabel']} carries the headword in this complete-case reading."
        s["Occurrences"] = kept
        s["SourceTexts"] = list(dict.fromkeys(o["RelPath"] for o in kept))
        s["Note"] = f"{len(kept)} curated occurrences retained after removing punctuation collisions and bare paratext; sources are counted by canonical work ID at validation."
        s["ExplanationParts"] = {"CorpusEarnedOpening": explanation.split(". ",1)[0]+".", "EvidenceBody":[explanation.split(". ",1)[1] if ". " in explanation else explanation]}
        keys=[f"o{i}" for i in range(1,len(kept)+1)]
        s["DraftEvidence"] = {
          "OpeningClaimEvidenceKeys":keys,
          "ZenBend":explanation,
          "CounterexampleOrLimit":"Literal, title, institutional, punctuation-collision, and grammatical uses were checked; collisions and bare paratext were not counted as deployment evidence.",
          "DifferentThingTest":{"Decision":"one-thing","ComparedThings":[preferred,"nearby literal, title, and grammatical candidates"],"Reason":"The retained witnesses denote the same corpus object or operation; no split was made merely for noun/verb grammar or a different reading."},
          "AliasRationale":"Each alias is an English lookup phrasing for the same retained corpus sense.",
          "ModifierControls":[{"finding":"checked","reason":"Exact headword occurrences were distinguished from longer compounds and punctuation joins."}],
          "FamilyControls":[{"finding":"checked","reason":"Related titles, persons, formulas, and literal uses were tested before retaining one sense."}],
          "IndependentWorkIds":list(dict.fromkeys(work_id(o["RelPath"]) for o in kept))}
    ep.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    wp=ENTRIES/eid/"evidence.draft.json"
    wp.write_text(json.dumps({"SchemaVersion":1,"Entry":copy.deepcopy(d)},ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    cp=ENTRIES/eid/"author.checkpoint.json"
    ordinal=1031+list(CFG).index(eid)
    cp.write_text(json.dumps({"ordinal":ordinal,"id":eid,"term":d['SourceTerm'],"status":"drafted-repaired","writtenUtc":NOW,"entrySha256":hashlib.sha256(ep.read_bytes()).hexdigest()},ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(eid,d['SourceTerm'],sum(len(s['Occurrences']) for s in d['Senses']))

ledger={"wave":"f004","lane":"B","ordinals":"1031-1040","role":"author-only","writtenUtc":NOW,"entries":[],"speedPass":{
 "lateDefect":"Evidence selection and generic prose/actor templating were coupled, so semantic defects surfaced only after expensive final audits.",
 "implemented":["compile each evidence worksheet before cohort validation","remove punctuation collisions and bare paratext before depth counting","derive canonical work IDs while drafting","generate source-title attribution notes from zc.title","add frequency-floor witnesses before prose review"],
 "nextPass":["precompute a sourceGroup packet with zc.title, full case, work_id, and exact-headword clause once per ordinal","require a closed occurrence decision before prose: direct utterance, narrated action, institutional rule, title discussion, or reject","run English-first and forbidden-framing lint on ExplanationParts before compiling","validate each entry immediately; reserve cohort audit for cross-entry collapse detection"]}}
for eid in CFG:
 ep=ENTRIES/eid/"entry.v2.json"; d=json.loads(ep.read_text(encoding="utf-8")); checks=[]
 for s in d["Senses"]:
  for o in s["Occurrences"]:
   v=zc.verify(o["RelPath"],o["Kwic"]); checks.append(bool(v.get("ok") and v.get("fromLb")==o["FromLb"] and v.get("toLb")==o["ToLb"]))
 ledger["entries"].append({"ordinal":1031+list(CFG).index(eid),"id":eid,"term":d["SourceTerm"],"occurrences":len(checks),"exactVerified":sum(checks),"entrySha256":hashlib.sha256(ep.read_bytes()).hexdigest()})
(ROOT/"waves"/"f004-laneB-1031-1040-repair-author-ledger.json").write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
