#!/usr/bin/env python3
"""Build the durable semantic ledger from the frozen batch11 navigation selection."""
import hashlib
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))
import zc

SOURCE = ROOT / "maintenance/non-iriya-v2-frequency-batch11-navigation-selection.json"
TARGET = ROOT / "maintenance/non-iriya-v2-frequency-batch11-semantic-ledger-c19-e8.json"

RULINGS = {
"二六時中": ("KEEP", "component", "The twelve double-hours, hence at every hour of day and night. Masters deploy it for uninterrupted present functioning or scrutiny rather than for a calendrical count.", "No covered parent or child; the four-graph time formula is independently usable."),
"四十九年": ("KEEP", "component", "The Buddha’s forty-nine-year speaking career, used by masters as a fixed rhetorical span against which the flower sermon, silence, or a present case is measured.", "The number-plus-year formula has a stable Chan referent; it is not retained as the ordinary duration ‘forty-nine years.’"),
"臨濟大師": ("REJECT", "nested title", "The witnesses simply name Master Linji with the ordinary honorific 大師; all Chan information belongs to the covered 臨濟 identity and its sayings.", "Contains covered 臨濟. A second headword for title attachment adds no lexical sense or search value beyond an alias."),
"大千沙界": ("REJECT", "generic doctrinal phrase", "The witnesses contain the conventional cosmological ‘great-thousand world of sands’ as scale imagery but do not give the four-graph string a distinct interview job.", "No nested covered term, but high frequency is doctrinal containment; another generic world-scale synonym yields diminishing returns."),
"三家村裏": ("REJECT", "nested fragment", "Bare ‘in a three-house village’ is a locative fragment. The socially pointed lexical units are the already covered parent phrases 三家村裏漢 and 三家村裏省事漢.", "Strict substring of two covered parents; retaining the location alone would duplicate their evidence while losing the person-type meaning."),
"一棒打殺": ("KEEP", "component", "‘Kill with one blow’ is a complete encounter action and verdict. Masters use it counterfactually to recast how an earlier figure should have been handled, most famously Yunmen on the newborn Buddha.", "Contains covered 一棒, but adding 打殺 changes the object from a counted blow to the distinct lethal verdict/action."),
"諸佛出世": ("REJECT", "generic doctrinal phrase", "‘The buddhas appear in the world’ is generic doctrinal narration in the selected witnesses, not a stable Chan answer, test, or capping phrase.", "Contains covered 出世, and the added generic subject does not produce a distinct lexical deployment."),
"乾坤大地": ("REJECT", "generic world phrase", "The combined heaven-earth/world phrase supplies totality imagery, but the cases do not distinguish a lexical job beyond the separately covered 乾坤 and 大地.", "Contains two covered components; recombination is transparent and yields diminishing returns."),
"觀世音菩薩": ("KEEP", "figure", "Guanyin as invoked by Zen masters: the hearer of sounds is brought directly into cases about hearing, seeing, compassionate response, and Yunmen’s cake-versus-bun turn.", "Not generic biography. Guide #0g makes an invoked pre-Zen figure a Zen figure defined by this corpus deployment."),
"人境兩俱奪": ("KEEP", "component", "‘Person and environment both taken away’ is one named operation in Linji’s fourfold person/environment roster and is repeatedly posed for a master’s live answer.", "No covered parent; the whole formula is not reducible to the ordinary graphs 人 or 境."),
"一頭水牯牛": ("KEEP", "component", "‘One head of water buffalo’ is a complete transformation/identification formula in encounters, including Nanquan’s answer about where he will go after death.", "Contains covered 水牯牛, but the classifier construction is the recurrent saying-form raised and answered as a whole."),
"斷天下人舌頭": ("KEEP", "component", "‘Cut off everyone’s tongue under heaven’ is a verdict for a move that leaves no verbal foothold or reply, used to test whether an answer truly silences the assembly.", "Contains covered 舌頭 and sits inside longer 坐斷 variants, but this causative verbal unit recurs independently and carries the silencing job."),
"三生六十劫": ("KEEP", "component", "‘Three lives and sixty kalpas’ is a fixed hyperbolic delay verdict: follow the criticized conceptual route and even that span will not reach or glimpse the matter.", "No covered parent/child; the exact number formula functions as a recurring Chan rebuke, not chronology."),
"新年頭佛法": ("KEEP", "component", "‘The Buddha-dharma at the head of the new year’ is the object of a recurring public-interview question and New Year’s teaching-seat dilemma—call it that and one error follows; refuse the name and another follows.", "Contains covered 佛法, but the seasonal compound defines a distinct recurrent question gate."),
"放汝三十棒": ("KEEP", "component", "‘I spare/allow you thirty blows’ is a complete master-to-respondent sentence of suspended punishment, often closing an exchange after a mistake or partial success.", "Contains covered 三十棒, but 放汝 supplies the distinct performative ruling, not merely the blow count."),
"圖天下太平": ("REJECT", "nested saying fragment", "‘In order to secure peace under heaven’ occurs as the tail of Yunmen’s longer one-blow/killing saying and contributes no independent Chan action outside that parent deployment.", "Contains covered 天下太平; the prefixed purpose verb is transparent and should remain inside the full saying."),
"萬歲萬萬歲": ("REJECT", "institutional boilerplate", "The phrase is an imperial longevity acclamation in court-facing incense and birthday formulas. Its presence in masters’ records is institutional containment, not a Chan lexical deployment.", "No useful nested relation; rejection prevents ceremonial frequency from masquerading as Zen vocabulary."),
"正當十五日": ("KEEP", "institutional interview hinge", "‘Right on the fifteenth day’ is the hinge of the recurring first-half/second-half/month-full teaching-seat formula, where a master demands the word for neither side.", "Not ordinary dating in these cases; the whole phrase marks the public monthly interview position."),
"一人發真歸": ("KEEP", "raised formula", "‘If one person realizes truth and returns to the source’ is a raised scripture formula that masters actively recast: emptiness collapses, strikes against things, or becomes flowers laid on brocade.", "No covered parent/child; variant consequences preserve this opening as the stable test formula."),
"窟裏作活計": ("REJECT", "nested fragment", "‘Do business in a cave’ is only the tail of the already covered 鬼窟裏作活計 / 向鬼窟裏作活計 warning; without 鬼 the selected lexical image is incomplete.", "Strict substring of two covered parents; duplicate evidence with reduced specificity."),
"天下衲僧": ("REJECT", "generic group fragment", "‘Monastics under heaven’ is a generic collective subject. The selected pointed saying is the covered parent 天下衲僧跳不出, while 衲僧 itself is already covered.", "Both substring of a covered parent and container of a covered child; the middle fragment adds no distinct job."),
"金剛王寶": ("REJECT", "truncated compound", "The evidence consistently continues into 金剛王寶劍/劒, the ‘vajra-king precious sword.’ Stopping before ‘sword’ creates no independently deployed object.", "Strict substring of two orthographic forms of the covered parent; reject rather than manufacture a generic ‘treasure.’"),
"時添意氣": ("REJECT", "truncated saying fragment", "The corpus deploys the full formulas 有意氣時添意氣 or 龍得水時添意氣. This four-graph tail is not independently raised or glossed.", "Strict substring of two covered parents; retaining it would multiply entries for the same saying."),
"三百六十": ("REJECT", "generic number fragment", "The number occurs in heterogeneous counts—days, assemblies, bones—and has no stable lexical referent or Chan job by itself.", "No nested covered term, but mixed referents prove frequency inflation; reject on lexical-unit and diminishing-return grounds."),
"南北東西": ("REJECT", "ordinary directional list", "The four directions are ordinary compositional location language across unrelated verses and descriptions; the selected cases do not stabilize a special Chan sense.", "No nested covered term; generic directional containment fails the deployment gate."),
}

source = json.loads(SOURCE.read_text(encoding="utf-8"))
assert len(source["rows"]) == 25 and set(RULINGS) == {row["term"] for row in source["rows"]}
counts = zc.batch_count([row["term"] for row in source["rows"]])
decisions = []
for row in source["rows"]:
    disposition, unit, reason, boundary = RULINGS[row["term"]]
    actual = counts[row["term"]]
    expected = {"hits": actual["hits"], "files": actual["files"], "distinctWorks": actual["works"]}
    assert row["zcExact"] == expected
    evidence = row["canonicalDistinctWorkEvidence"]
    assert len(evidence) == 2 and len({item["workId"] for item in evidence}) == 2
    for witness in evidence:
        verified = zc.verify(witness["source"], witness["kwic"])
        assert verified["ok"] and verified["fromLb"] == witness["hitFromLb"] and verified["toLb"] == witness["hitToLb"]
        assert zc.work_id(witness["source"]) == witness["workId"]
        assert zc.title(witness["source"]) == witness["title"]
    decisions.append({
        "batchOrdinal": row["batchOrdinal"], "reservoirRank": row["reservoirRank"],
        "reservoirBand": row["reservoirBand"], "stratum": row["stratum"],
        "term": row["term"], "graphs": row["graphs"], "indexNavigationHits": row["indexNavigationHits"],
        "disposition": disposition, "unit": unit, "reason": reason,
        "nestedBoundaryAndDiminishingReturns": boundary,
        "zcExact": expected, "evidence": evidence
    })

ledger = {
    "schemaVersion": "non-iriya-frequency-semantic-ledger.v1", "author": "c19-e8", "batch": 11,
    "selection": str(SOURCE.relative_to(ROOT)).replace("\\", "/"),
    "selectionSha256": hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
    "reviewedCount": 25, "method": "manual full-case Chan-deployment and distinct-lexical-unit adjudication",
    "decisions": decisions,
    "summary": {"KEEP": sum(x["disposition"] == "KEEP" for x in decisions), "PROVISIONAL": 0, "REJECT": sum(x["disposition"] == "REJECT" for x in decisions)},
    "identityCountEvidenceReverified": True, "allFiftyWindowsVerified": True, "allEvidencePairsCanonicalDistinct": True,
    "selfReviewPerformed": False, "authorityMutationPerformed": False, "queueMutationPerformed": False,
    "registryMutationPerformed": False, "buildRun": False, "lineageTouched": False
}
TARGET.write_text(json.dumps(ledger, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
