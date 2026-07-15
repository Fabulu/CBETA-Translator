#!/usr/bin/env python3
"""Apply the XML-body-reviewed 2026-07-14 Chan corpus admissions."""

from __future__ import annotations

import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[3]
MANIFEST = REPO / "Assets" / "Data" / "zen-corpus.json"
CORPUS = Path("/mnt/c/temp/NewTranslationrepos/CbetaZenTexts/xml-p5")

ADMIT = {
    "X/X66/X66n1297.xml": "chan:zongjian-falin",
    "M/M59/M59n1540.xml": "chan:dahui-pushuo",
    "X/X67/X67n1301.xml": "chan:yuanwu-jijie-lu",
    "P/P154/P154n1519.xml": "chan:zongmen-tongyao-zhengxu",
    "P/P155/P155n1519.xml": "chan:zongmen-tongyao-zhengxu",
    "X/X67/X67n1302.xml": "chan:tianqi-zhu-xuedou-songgu",
    "X/X67/X67n1306.xml": "chan:tianqi-zhu-hongzhi-songgu",
    "L/L155/L155n1643.xml": "chan:hongjue-min-yulu",
    "L/L154/L154n1639.xml": "chan:tianyin-xiu-yulu",
    "L/L158/L158n1652.xml": "chan:mingjue-cong-yulu",
    "L/L153/L153n1637.xml": "chan:huanyou-chuan-yulu",
    "L/L154/L154n1640.xml": "chan:miyun-wu-yulu",
    "L/L153/L153n1638.xml": "chan:xueqiao-xin-yulu",
    "L/L154/L154n1638.xml": "chan:xueqiao-xin-yulu",
    "L/L157/L157n1649.xml": "chan:shanci-ji-yulu",
    "L/L155/L155n1642.xml": "chan:mingdao-zhengjue-sen-yulu",
    "J/J29/J29nB230.xml": "chan:shuijian-hai-hui-lu",
    "X/X83/X83n1578.xml": "chan:zhiyue-lu",
    "X/X84/X84n1580.xml": "chan:jiaowai-biezhuan",
    "X/X84/X84n1579.xml": "chan:xu-zhiyue-lu",
    "X/X85/X85n1592.xml": "chan:anheidou-ji",
    "X/X85/X85n1587.xml": "chan:zhengyuan-lueji",
    "X/X85/X85n1588.xml": "chan:zhengyuan-lueji",
    "X/X67/X67n1309.xml": "chan:dahui-zhengfa-yancang",
    "X/X79/X79n1563.xml": "chan:daguangming-zang",
    "X/X67/X67n1310.xml": "chan:nian-bafang-zhuyu-ji",
    "X/X78/X78n1554.xml": "chan:wujia-zhengzong-zan",
    "X/X67/X67n1308.xml": "chan:jingshi-diru-ji",
    "X/X87/X87n1620.xml": "chan:xianjue-zongcheng",
    "X/X87/X87n1624.xml": "chan:huihong-linjian-lu",
    "X/X83/X83n1577.xml": "chan:luohu-yelu",
    "X/X86/X86n1601.xml": "chan:chandeng-shipu",
}

missing = [rel for rel in ADMIT if not (CORPUS / rel).is_file()]
if missing:
    raise SystemExit(f"missing admitted XML files: {missing}")
data = json.loads(MANIFEST.read_text(encoding="utf-8-sig"))
texts = set(data.get("texts") or [])
work_ids = dict(data.get("work_ids") or {})
for rel, work in ADMIT.items():
    texts.add(rel)
    work_ids[rel] = work
data["texts"] = sorted(texts)
data["work_ids"] = {rel: work_ids[rel] for rel in data["texts"]}
data["admissionAudit"] = str(HERE / "ALLOWLIST_ADMISSION_20260714.md")
MANIFEST.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(f"{len(data['texts'])} files; {len(set(data['work_ids'].values()))} independent works; admitted {len(ADMIT)} files/{len(set(ADMIT.values()))} works")
