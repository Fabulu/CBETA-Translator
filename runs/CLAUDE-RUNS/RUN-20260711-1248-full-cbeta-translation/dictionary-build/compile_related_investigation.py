# -*- coding: utf-8 -*-
"""Make every unselected RelatedTerms lead an explicit investigation item."""
import csv
import json
import re
from pathlib import Path

HERE = Path(__file__).resolve().parent
POOL = HERE / "NEXT500_RELATED_POOL.tsv"

selected500 = set(re.findall(
    r"^\| \d+ \| `t_[0-9a-f]+` \| ([^|]+?) \|",
    (HERE / "NEXT500_TERMS.md").read_text(encoding="utf-8"), re.M,
))
sayings = set(re.findall(
    r"^- `t_[0-9a-f]+` ([^ ]+) \(",
    (HERE / "NEXT100_BUILD_PLAN.md").read_text(encoding="utf-8"), re.M,
))
roster_data = json.loads((HERE.parents[3] / "Assets" / "Data" / "master-dates.json").read_text(encoding="utf-8"))
roster_names = {
    name for master in roster_data.get("masters", []) for name in master.get("names", [])
    if re.fullmatch(r"[㐀-鿿]+", name)
}
with POOL.open(encoding="utf-8") as handle:
    rows = list(csv.DictReader(handle, delimiter="\t"))
for row in rows:
    if row["term"] in selected500:
        row["disposition"] = "SELECTED_NEXT500"
    elif row["term"] in sayings:
        row["disposition"] = "SELECTED_NEXT100"
    elif row["term"] in roster_names:
        row["disposition"] = "ROSTER_NOT_DICTIONARY"
    else:
        row["disposition"] = "UNREVIEWED_INVESTIGATION"
with POOL.open("w", encoding="utf-8", newline="") as handle:
    writer = csv.DictWriter(handle, fieldnames=list(rows[0]), delimiter="\t")
    writer.writeheader()
    writer.writerows(rows)

remaining = [row for row in rows if row["disposition"] == "UNREVIEWED_INVESTIGATION"]
roster_held = [row for row in rows if row["disposition"] == "ROSTER_NOT_DICTIONARY"]
remaining.sort(key=lambda row: (-int(row["hits"]), -int(row["files"]), row["term"]))
doc = [
    "# Related-term investigation backlog", "",
    f"**{len(remaining)} counted leads remain explicitly queued for investigation.** They are not rejected and must not disappear.",
    f"A further **{len(roster_held)} exact lineage-master names** are preserved in the TSV as `ROSTER_NOT_DICTIONARY`; by the standing scope rule they belong on the master roster, not in this build queue.",
    "They were proposed by completed dictionary entries but have not yet passed the independent-headword test.",
    "Before promotion, test: exact lexical unit rather than substring; distinct from graph/word-order variants; not better housed in an existing article; observable Chan deployment; and enough independent evidence for its own entry.",
    "The complete inherited interpretation text is retained in the matching row of `NEXT500_RELATED_POOL.tsv` under `inherited_lead`; guide §5 item 9 requires a keep/revise/reject disposition when investigated.", "",
    "| Priority | Term | Hits / files | Proposed by completed entries | Status |",
    "|---:|---|---:|---|---|",
]
for rank, row in enumerate(remaining, 1):
    sources = row["proposing_entries"].replace("|", "/")
    doc.append(f"| {rank} | {row['term']} | {int(row['hits']):,} / {int(row['files']):,} | {sources} | UNREVIEWED |")
(HERE / "RELATED_INVESTIGATION_BACKLOG.md").write_text("\n".join(doc) + "\n", encoding="utf-8")
print(f"selected500={sum(r['disposition']=='SELECTED_NEXT500' for r in rows)} selected100={sum(r['disposition']=='SELECTED_NEXT100' for r in rows)} roster={len(roster_held)} investigation={len(remaining)}")
