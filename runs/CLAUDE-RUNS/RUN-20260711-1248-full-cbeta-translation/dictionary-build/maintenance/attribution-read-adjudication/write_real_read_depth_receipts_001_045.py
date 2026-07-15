"""Serialize the explicit all-stored-witness definition cross-check for repaired 001-045 entries."""
import datetime, hashlib, json
from pathlib import Path
HERE=Path(__file__).resolve().parent; BUILD=HERE.parents[1]; ENTRIES=BUILD/"fresh-build"/"entries"
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat()
# Each reason below was authored after reading every stored KWIC for that entry.
DECISIONS={
"t_708834b4cb89":("HOLDS","All eight witnesses denote the institutional teaching hall: building, entering, sitting in, or assembling at it; the changed actor metadata does not alter the referent."),
"t_712ca8b5bf06":("SEMANTIC_REVIEW_REQUIRED","The witnesses predominantly label the rear-hall officer, while the target fuses 'office or officer'. The actor repair is sound, but the target must be adjudicated against the different-thing split rule before a depth receipt can issue."),
"t_75a477117870":("HOLDS","All six witnesses retain purple fungus as the lexical element, whether gathered literally or named in Purple Fungus Song; song context is a compound deployment, not a second meaning of 紫芝 itself."),
"t_76ee526a2b16":("HOLDS","All five witnesses now contain genuine contiguous references to novice precepts, their conferral, breach, or direct interrogation; the normalization artifact was replaced rather than counted."),
"t_81d0d434f560":("HOLDS","All seven witnesses ask, cite, or discuss the patriarch's meaning; naming Huangbo as respondent changes no lexical claim."),
"t_85fd3b19165c":("HOLDS","All seven witnesses use 陞堂 for ascending the teaching hall to address the assembly; identifying Huairang as the instructor preserves that action sense."),
"t_8aa9485f0650":("HOLDS","All six witnesses deploy the iron steamed bun as the same deliberately unbiteable or obstructive object/image; the corrected questioner does not create another referent."),
"t_8eeed0b7412a":("HOLDS","All seven witnesses quote or invoke the same Trust-in-Mind formula 'only avoid choosing'; speaker differences do not alter its lexical content."),
"t_935452e7a2c6":("SEMANTIC_REVIEW_REQUIRED","Most witnesses denote Guanyin, but the final roster-like witness embeds 觀音 in Zhaozhou's title/place designation. That different referent must be split or removed before claiming the single definition covers all evidence."),
"t_937f63a4fb51":("HOLDS","All nine witnesses use 目前 spatially or deictically for what is immediately before one; Huangbo's corrected utterance is fully consistent with that target."),
"t_a6754d726742":("HOLDS","All six witnesses use the balance pointer as the same fixed indicator, usually in warnings not to mistake it for the intended point; the Shoushan correction changes attribution only."),
"t_aa9e5467d247":("HOLDS","All three witnesses preserve Changsha's same verse line and each lexical component is represented in the English target; correcting the author strengthens rather than changes the definition."),
"t_aced87de5b30":("HOLDS","All six witnesses use the same paired formula 'kill buddhas and patriarchs', directly or as a raised case; actor changes do not alter the phrase."),
"t_b15eaab0dc3c":("HOLDS","All six witnesses denote or figuratively play the same stringless lute; no stored occurrence requires a distinct object sense."),
"t_b4c37e2f25c3":("HOLDS","All seven witnesses denote the latrine, including Zhaozhou's case and monastic-procedure prose; the recut isolates one truthful speaker without changing the referent."),
"t_bbee6625a4d5":("HOLDS","All six witnesses use the red-flesh lump phrase in the Linji or Nanyuan formulas; the recut distinguishes speakers while retaining the same headword-level body phrase."),
"t_c051d6f277af":("HOLDS","All eight witnesses denote the Buddha-birthday occasion, including sermon headings, verses, and biographical dating; editorial versus spoken ownership does not split the occasion sense."),
"t_c8f127c46d44":("HOLDS","All eight witnesses narrate that a participant had no reply; Mazu's contextual role does not change the predicate."),
"t_cb44465faa59":("HOLDS","All ten witnesses use 侍者 for an attendant in institutional, dialogic, or biographical roles; the cross-record KWIC was recut to a single Mazu scene."),
"t_ccc39a4559bf":("HOLDS","All six witnesses use 僧正 for the chief monastic official as title, appointment, requester, or visitor; the speaker-label correction preserves the office."),
"t_cd69e0f9c10a":("SEMANTIC_REVIEW_REQUIRED","Most witnesses denote an ancient buddha, but at least one applies 'heaven's ancient buddha' honorifically to an emperor. The attribution fix is valid; honorific versus referential use needs an explicit different-thing adjudication."),
"t_d1ca36839312":("HOLDS","All six witnesses deploy 全提正令 as fully raising or exercising the true command; naming Yun'e Xi does not alter that job."),
"t_d1e06fd225fa":("HOLDS","The existing two-sense split remains supported: seven interview/chamber-entry witnesses differ from two ordinary room-entry actions; the corrected narrated visitor belongs under the interview sense."),
"t_d2c3f40d45c6":("HOLDS","All six witnesses refer to Buddha Great Penetrating Wisdom Victory by name or invoke his ten-eon case; adding Baizhang as respondent changes no referent."),
"t_d4673502b2d2":("HOLDS","All eight witnesses use 未審 as a deferential interrogative, 'may I ask / I do not yet know'; the Foyan context correction is attribution-only."),
"t_dd5f8d8801d2":("HOLDS","All seven witnesses denote a fan, including the rhinoceros fan and Yunmen's leaping fan; naming Yanguan as speaker does not alter the object sense.")
}
outdir=HERE/"depth-review-receipts";outdir.mkdir(parents=True,exist_ok=True);rows=[]
for tid,(status,reason) in DECISIONS.items():
 p=ENTRIES/tid/"entry.v2.json";e=json.loads(p.read_text(encoding="utf-8"));sha=hashlib.sha256(p.read_bytes()).hexdigest();count=sum(len(s.get("Occurrences",[])) for s in e.get("Senses",[]))
 row={"entryId":tid,"sourceTerm":e["SourceTerm"],"entrySha256":sha,"reviewedUtc":NOW,"reviewedStoredOccurrences":count,"reviewedSenseCount":len(e.get("Senses",[])),"status":status,"reason":reason,"reviewMethod":"Read every stored KWIC against every PreferredTarget and re-applied the different-things sense-split rule after attribution repair."}
 rows.append(row)
 if status=="HOLDS": (outdir/f"{tid}-{sha}.json").write_text(json.dumps(row,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
manifest={"schemaVersion":"depth-definition-crosscheck-v1","generatedUtc":NOW,"entries":len(rows),"receiptsIssued":sum(x["status"]=="HOLDS" for x in rows),"semanticReviewRequired":sum(x["status"]!="HOLDS" for x in rows),"rows":rows}
(HERE/"cohorts-4-6-real-read-depth-crosscheck-001-045.json").write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print(json.dumps({"entries":len(rows),"receiptsIssued":manifest["receiptsIssued"],"semanticReviewRequired":manifest["semanticReviewRequired"]},indent=2))
