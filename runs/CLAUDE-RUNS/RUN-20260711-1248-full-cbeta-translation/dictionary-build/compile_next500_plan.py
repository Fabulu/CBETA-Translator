# -*- coding: utf-8 -*-
"""Compile the two reviewed discovery lanes and 140 curated family gaps.

The output is deterministic and refuses collisions with the finished termbase,
requested queue, or companion sayings list.  It does not register terms.
"""
from __future__ import annotations

import csv
import hashlib
import json
import re
from pathlib import Path

HERE = Path(__file__).resolve().parent

ROOT_GROUPS = {
    "seat/institution": """
拄杖 陞座 鉢盂 竹篦 洗鉢盂 晚參 未審 護生 大戒 因果 作務 業識 剎竿 道得 堂奧 堂頭和尚 商量 喫飯 闍黎 薦得
""",
    "public interview/verdict": """
一喝不作一喝用 法語 曹洞 向上一著 五位君臣 末後一句 三玄 坐斷天下人舌頭 且道作麼生 且喜沒交涉 衲僧家 且道畢竟如何 第一句 末後一著 提綱 如何是無寒暑處 參學事畢 如何是禪 入門便棒 把住放行 主中主 三要 一喝分賓主 賓主歷然 試道看 唯嫌揀擇 目前無法 臨機 有甚麼交涉 大機 偏正 阿那箇 投機 廓然無聖 無生法忍 君臣道合 言下有省 當機一句 法身邊事 祖師意 棒下無生忍 棒喝交馳 第二句 全提正令 賓中賓 世尊良久 通身是口 佛向上事 箭鋒相拄 一問一答 頂門正眼 向上機
""",
    "lineage/case": """
不是心不是佛不是物 諸佛出身處 殺生 趙州戴草鞋 祖印 雲門三句 頭首 立地成佛 正位 千聖不傳 祖師門下 向上宗乘 倒卻門前剎竿 不二法門 本參話頭 祖祖相傳 至道無難 一切眾生皆有佛性 燈燈相續 見色明心 聞聲悟道 百尺竿頭須進步 陝府鐵牛 本命元辰 擬心 髑髏裏眼睛 鐵牛之機 豐干 我今不是渠 不思善不思惡 虛空粉碎 法眼宗 野狐身 梁武帝 參問 徐六擔板 單傳直指 啐啄 殺佛殺祖
""",
    "bent ordinary/Buddhist language": """
自己 行住坐臥 生死 威音那畔 一念萬年 全機大用 十方世界 披毛戴角 遇緣即宗 心要 眼睛 宗匠 法身向上事 大悲 舍利 清淨法身 微塵 薰風自南來 道中人 老婆心 無心是道 十方世界是全身 空劫已前 接物利生 體露金風 無常迅速 心地法門 知有底人 物物頭頭
""",
}

GROUP_REASON = {
    "seat/institution": "A concrete office, implement, room, schedule, or teaching-seat act acquires an observable job in Chan institutional life.",
    "public interview/verdict": "The expression functions in public questioning, answering, testing, handling, or judgement rather than as free-standing abstraction.",
    "lineage/case": "The phrase names or compresses a transmitted case, lineage relation, or repeatedly raised figure deployment.",
    "bent ordinary/Buddhist language": "Completed entries expose a recurrent Chan deployment that must be distinguished from ordinary or generic Buddhist use.",
}


def term_id(term: str) -> str:
    return "t_" + hashlib.sha256(term.encode("utf-8")).hexdigest()[:12]


def parse_lane(path: Path, lane: str) -> list[dict]:
    rows = []
    for line in path.read_text(encoding="utf-8").splitlines():
        if not re.match(r"\|\s*\d+\s*\|", line):
            continue
        cells = [cell.strip() for cell in line.strip().strip("|").split("|")]
        if len(cells) != 5 or not cells[0].isdigit():
            continue
        count = re.fullmatch(r"([\d,]+)\s*/\s*([\d,]+)", cells[2])
        if not count:
            raise SystemExit(f"bad count in {path.name}: {line}")
        rows.append({
            "term": cells[1], "hits": int(count.group(1).replace(",", "")),
            "files": int(count.group(2).replace(",", "")), "lane": lane,
            "lane_rank": int(cells[0]), "rationale": cells[3], "provenance": cells[4],
        })
    if len(rows) != 180 or len({row["term"] for row in rows}) != 180:
        raise SystemExit(f"{path.name}: expected 180 unique rows, got {len(rows)}")
    return rows


def main() -> None:
    a = parse_lane(HERE / "NEXT500_CANDIDATES_A.md", "A")
    b = parse_lane(HERE / "NEXT500_CANDIDATES_B.md", "B")
    related = {
        row["term"]: row
        for row in csv.DictReader((HERE / "NEXT500_RELATED_POOL.tsv").open(encoding="utf-8"), delimiter="\t")
    }
    root = []
    for group, block in ROOT_GROUPS.items():
        for term in block.split():
            if term in {row["term"] for row in root}:
                raise SystemExit(f"duplicate root term: {term}")
            if term not in related:
                raise SystemExit(f"root term absent from counted related pool: {term}")
            source = related[term]
            root.append({
                "term": term, "hits": int(source["hits"]), "files": int(source["files"]),
                "lane": "R", "lane_rank": len(root) + 1, "rationale": GROUP_REASON[group],
                "provenance": f"Completed entries: {source['proposing_entries']}. Inherited lead: {source['inherited_lead'] or 'the recorded RelatedTerms family relation'} → KEEP FOR FULL RETEST.",
            })
    if len(root) != 140:
        raise SystemExit(f"expected 140 root-curated terms, got {len(root)}")

    rows = a + b + root
    terms = [row["term"] for row in rows]
    if len(rows) != 500 or len(set(terms)) != 500:
        dup = sorted({term for term in terms if terms.count(term) > 1})
        raise SystemExit(f"expected 500 unique rows; duplicates={dup}")

    done = set()
    for entry_path in (HERE / "terms").glob("t_*/entry.v2.json"):
        done.add(json.loads(entry_path.read_text(encoding="utf-8"))["SourceTerm"])
    requested = set(re.findall(r"`t_[0-9a-f]+`\s+([^\s`]+)", (HERE / "REQUESTED_BUILD_PLAN.md").read_text(encoding="utf-8")))
    sayings = set(re.findall(r"^\d+\. \*\*([^*—]+?)(?:\s*—|\*\*)", (HERE / "NEXT100_SAYINGS_CANDIDATES.md").read_text(encoding="utf-8"), re.M))
    collisions = (set(terms) & done) | (set(terms) & requested) | (set(terms) & sayings)
    if collisions:
        raise SystemExit(f"existing/requested/sayings collisions: {sorted(collisions)}")

    # Interleave equally reviewed lanes; frequency breaks equal normalized ranks.
    rows.sort(key=lambda row: (row["lane_rank"] / (180 if row["lane"] in "AB" else 140), -row["hits"], row["term"]))
    for rank, row in enumerate(rows, 1):
        row["rank"] = rank
        row["id"] = term_id(row["term"])

    terms_doc = [
        "# Curated next 500 dictionary terms", "",
        "Exactly 500 new headwords selected jointly by allowlist frequency and observable Chan-specific deployment.",
        "Lanes A and B are independently concordance-screened; lane R consists only of gaps proposed by completed entries.",
        "Every inherited interpretation is a research lead governed by `DICTIONARY_ENTRY_GUIDE.md` §5 item 9, not an authority.",
        "The separate `NEXT100_SAYINGS_CANDIDATES.md` companion queue is excluded from these 500.", "",
        "| Rank | ID | Term | Hits / files | Lane | Zen-deployment rationale | Provenance lead |",
        "|---:|---|---|---:|---|---|---|",
    ]
    for row in rows:
        clean = lambda text: text.replace("|", "/").replace("\n", " ")
        terms_doc.append(f"| {row['rank']} | `{row['id']}` | {row['term']} | {row['hits']:,} / {row['files']:,} | {row['lane']} | {clean(row['rationale'])} | {clean(row['provenance'])} |")
    (HERE / "NEXT500_TERMS.md").write_text("\n".join(terms_doc) + "\n", encoding="utf-8")

    plan = [
        "# Next-500 build plan", "",
        "Build only after the remaining requested waves. Each batch must pass guide §5 #0–#0g, item 9 provenance, exact zc verification, depth/sense QA, registration, and merge.",
        "The final wave contains five terms; all preceding waves contain fifteen, split five per worker.", "",
    ]
    for start in range(0, 500, 15):
        wave = start // 15 + 1
        plan.extend([f"## n{wave:03d}", ""])
        chunk = rows[start:start + 15]
        for batch_index, label in enumerate("ABC"):
            part = chunk[batch_index * 5:(batch_index + 1) * 5]
            if not part:
                continue
            plan.extend([f"### Batch {label}"] + [f"- `{row['id']}` {row['term']} ({row['hits']:,}/{row['files']:,})" for row in part] + [""])
    (HERE / "NEXT500_BUILD_PLAN.md").write_text("\n".join(plan), encoding="utf-8")
    print(f"wrote {len(rows)} unique terms in {(len(rows) + 14) // 15} waves")


if __name__ == "__main__":
    main()
