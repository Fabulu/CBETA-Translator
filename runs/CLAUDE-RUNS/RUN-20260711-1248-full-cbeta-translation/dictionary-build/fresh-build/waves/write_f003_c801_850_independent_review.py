#!/usr/bin/env python3
import datetime, hashlib, json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PACKET = ROOT / "fresh-build/waves/f003-laneC-801-850-current-semantic-review-packet.json"
OUT = ROOT / "fresh-build/waves/f003-laneC-801-850-independent-exact-review.json"

# A KEEP means the complete entry survived an occurrence-by-occurrence read for
# actor ownership, referent/sense boundaries, English glosses, and explanatory prose.
KEEP = {
    829: "All seven witnesses narrate the same visible act; compiler ownership is correct and the fly-whisk teaching-seat deployment is explicit.",
    830: "All four are documentary descriptions of the same guardian-spirit referent; no master is falsely made utterer.",
    834: "The interrogative has one stable referent-function and every stored turn is assigned to its actual named speaker.",
    843: "The witnesses consistently concern entrusting the teaching/transmission; speech and narration are distinguished correctly.",
    845: "The technical referent is stable across attainment, question, and direct-description witnesses; narrated uses remain narrator-owned.",
}

NOTES = {
801:"Action narration is repeatedly assigned to a master as though the master uttered the headword; reread each sleeve-sweeping event.",
802:"Catalogue/record-title strings are used as speakers in O5-O6; normalize or narrate after full-case review.",
803:"O1/O4/O5 put the questioner's words under the master, and catalogue strings appear as speakers.",
804:"At least O5 is narrator wording before a quoted reply, not Fayan uttering the headword; review all borderline narration.",
805:"This is a narrated incense action, but multiple occurrences make record titles or masters its utterer.",
806:"A catalogue/record-title string is used as speaker and repeated compiler quotations need source-speaker recovery.",
807:"O4 is a longer-word substring (相見處), not secure evidence for 見處; O5 speaker ownership also needs correction.",
808:"A record title is used as the utterer in O2; full actor normalization is required.",
809:"O1 is the master's challenge, not an unnamed questioner; O4/O6 are record-title speakers.",
810:"Under-split: physical eyes/eye movement and the Zen organ of discernment are different referents.",
811:"The gloss conflates the generic lamp-record class with several distinct titled works and misleadingly presents one singular book.",
812:"O3 assigns the headword to a table-of-contents title rather than the actual turn or narrator.",
813:"Several table-of-contents/record-title strings are used as utterers; retain the chair sense but repair actors.",
814:"O3 is in a monk's question, while quoted Xinxin Ming lines need the quoted speaker/text attribution rather than generic compiler ownership.",
815:"O2 and other biographical clauses are narration, not utterances by the named master.",
816:"Under-split: 人天眼目 is both the eyes/discernment of humans and gods and the title of a specific book.",
817:"O4 begins 師問 and is the master's utterance, not an unnamed questioner.",
818:"A record title is used as utterer and narrated robe actions need exact actor-role cleanup.",
819:"O1 uses a table-of-contents title as speaker; the remaining documentary guardian uses do not cure that attribution defect.",
820:"O5 is a questioner's 如何是… turn, not the named master's utterance; narration also needs rereading.",
821:"Record-title and sequence-heading strings are used as utterers in O4/O7.",
822:"O5/O6 are occasion headings or narrated offices, not utterances by record-title strings.",
823:"A table-of-contents title is used as speaker in O4; reread quoted/narrated 'single eye' clauses.",
824:"Named non-master actors such as the merchant/questioner are collapsed into compiler narration; O5 is an embedded questioner's turn.",
825:"Bare 曰 clauses O2/O6 have recoverable speakers and should not default to compiler narration.",
826:"Under-split: institutional west-hall senior and Xitang/the named master or title are different referents.",
827:"Several MasterName values are record-title strings rather than normalized roster names.",
828:"Two occurrences retain 'named speaker preserved only in abbreviated form' instead of resolving the named master.",
831:"The gloss 'relative and true' overstates 正 as truth and blurs the attested Caodong positional pair; revise from predicates.",
832:"The gloss 'answering words' is unnatural and multiple abbreviated-speaker placeholders remain unresolved.",
833:"O5 explicitly names Hong Juefan/Huihong but leaves an abbreviated-speaker placeholder.",
835:"Severely under-split: ceremonial scepter, 如意院 place name, wish-fulfilling jewel, 如意子, and adjectival 'as desired' are different things.",
836:"Test and split the physical/administrative office from the office staff or administration; catalogue evidence currently blurs them.",
837:"O3's 曰 introduces a human appraisal ('what a pity'), but ownership is left with the compiler.",
838:"Under-split: O2 means leave the monastery to seek elsewhere, while the other witnesses concern retirement from abbacy.",
839:"Abbreviated-speaker placeholders remain; distinguish the responsive accord from the derivative label 'accord verse' in prose.",
840:"Under-split/lossy gloss: literal heel and Zen-loaded footing/under-one's-feet uses are collapsed into 'one's footing'.",
841:"The title use 'temple master/head' and the case-role temple administrator require adjudication; 'temple rector' does not transparently cover both.",
842:"O6 places the phrase inside Hu's explicit question, not compiler narration.",
844:"Adjudicate physical tea/hot water against the institutional tea service/rite; current single gloss fuses referents.",
846:"O2 leaves an abbreviated named speaker unresolved despite the hard name-the-master rule.",
847:"PreferredTarget should distinguish reciting/expounding precepts from merely 'announcing' them; reread the formal rite witnesses.",
848:"O2/O7 use title/compiler strings as utterers; keep noun/verb grammar together but repair actor ownership and English gloss.",
849:"O4 retains an abbreviated-speaker placeholder instead of resolving the master.",
850:"O6's headword lies in the monk's question, but the entry assigns it to Mingjue Cong.",
}

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

packet = json.loads(PACKET.read_text(encoding="utf-8"))
rows = []
for item in packet["items"]:
    n = item["ordinal"]
    path = ROOT / item["path"]
    actual = sha(path)
    if actual != item["sha256"]:
        raise SystemExit(f"hash drift before review: {n} {actual} != {item['sha256']}")
    verdict = "KEEP" if n in KEEP else "REVISE"
    rows.append({
        "ordinal": n, "id": item["id"], "term": item["term"],
        "entrySha256": actual, "verdict": verdict,
        "reviewNotes": KEEP.get(n) or NOTES[n],
    })

assert len(rows) == 50 and sum(r["verdict"] == "KEEP" for r in rows) == 5
out = {
    "generatedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
    "scope": "f003 Lane C 801-850 independent exact-hash semantic and actor review",
    "reviewer": "Codex independent reviewer /root/fresh_semantic_reviewer/f003_c801_850_author",
    "readOnly": True,
    "entries": 50, "occurrencesRead": 331,
    "summary": {"KEEP": 5, "REVISE": 45},
    "rows": rows,
}
OUT.write_text(json.dumps(out, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(OUT)
