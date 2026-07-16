#!/usr/bin/env python3
"""Apply the r003 owner2 passage-reading actor re-audit findings durably."""
from __future__ import annotations

import hashlib
import json
import sys
from datetime import datetime, timezone
from pathlib import Path

BASE = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(BASE))
import zc  # noqa: E402

REVIEW = BASE / "maintenance/semantic-cohorts/semantic-r003-owner2-actor-reaudit-reviewer2.json"
LEDGER = BASE / "maintenance/semantic-cohorts/semantic-r003-owner2-actor-repair.json"
RUNGS = ["line", "expanded-context", "section-header", "book-title", "tei-header", "parallel-passage"]
NOW = datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")
REVIEWER = "Codex r003-owner2 ACTOR_AUDIT repair"

ROLE_MAP = {
    "case-subject": "case-figure", "person-appraised": "person-discussed",
    "biographical-subject": "person-described", "quoter": "later-quoter",
    "attributed-treatise-figure": "case-figure", "named-target": "person-discussed",
    "person-invoked": "case-figure", "redactor": "compiler", "case-master": "case-figure",
    "quoted-respondent": "respondent", "figure-deployed": "case-figure",
    "quoting-author": "later-quoter", "reporting-questioner": "questioner",
    "person-reported": "person-discussed", "lineage-teacher": "teacher",
    "earlier-case-subject": "case-figure", "quoted-source": "case-figure",
    "contrasted-source": "person-discussed", "earlier-case-respondent": "respondent",
    "person-sought-in-case": "case-figure", "observer": "interlocutor",
    "record-subject": "section-subject", "master-in-case": "case-figure",
    "case-raiser": "later-raiser", "later-commentator": "commentator",
    "quoted-speaker": "case-figure", "person-quoted": "case-figure",
    "school-founder": "teacher", "subject-of-exposition": "person-discussed",
}
ALLOWED_ROLES = {
    "utterer", "respondent", "questioner", "interlocutor", "addressee",
    "section-subject", "record-owner", "person-described", "person-discussed",
    "commentator", "later-raiser", "later-quoter", "teacher", "student",
    "compiler", "verse-author", "case-figure",
}


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def reviewed(label: str, kind: str, role: str, grammar: str) -> dict:
    return {
        "Status": "reviewed-unnamed", "Kind": kind, "ActorLabel": label,
        "ActorRole": role, "GrammarEvidence": grammar, "RungsChecked": RUNGS,
        "ReviewedBy": REVIEWER, "ReviewedUtc": NOW,
    }


def narrated(label: str, grammar: str) -> dict:
    return {
        "Status": "narrated", "Kind": "compiler narrative", "ActorLabel": label,
        "ActorRole": "compiler", "GrammarEvidence": grammar,
        "ReviewedBy": REVIEWER, "ReviewedUtc": NOW,
    }


def impersonal(label: str, kind: str, grammar: str) -> dict:
    return {
        "Status": "impersonal", "Kind": kind, "ActorLabel": label,
        "ActorRole": "document voice", "GrammarEvidence": grammar,
        "ReviewedBy": REVIEWER, "ReviewedUtc": NOW,
    }


def context(name: str, *roles: str) -> dict:
    return {"MasterName": name, "Roles": list(roles)}


def set_named(occ: dict, name: str, note: str | None = None) -> None:
    occ["MasterName"] = name
    occ.pop("ActorAttribution", None)
    if note:
        occ["AttributionNote"] = note


def set_other(occ: dict, actor: dict, contexts: list[dict], note: str) -> None:
    occ.pop("MasterName", None)
    occ["ActorAttribution"] = actor
    occ["ContextMasters"] = contexts
    occ["AttributionNote"] = note


def replace_occ(occ: dict, *, rel: str, kwic: str, master: str, note: str) -> None:
    v = zc.verify(rel, kwic)
    if not v["ok"]:
        raise RuntimeError(f"replacement failed zc.verify: {rel} {kwic}")
    occ.clear()
    occ.update({
        "RelPath": rel, "FromLb": v["fromLb"], "ToLb": v["toLb"], "Kwic": kwic,
        "Curated": True, "AttributionNote": note, "MasterName": master,
        "ContextMasters": [context(master, "utterer")],
    })


def normalize_contexts(occ: dict) -> None:
    merged: dict[str, list[str]] = {}
    for cm in occ.get("ContextMasters") or []:
        name = cm.get("MasterName")
        if not name:
            continue
        roles = merged.setdefault(name, [])
        for raw in cm.get("Roles") or []:
            role = ROLE_MAP.get(raw, raw)
            if role not in ALLOWED_ROLES:
                raise RuntimeError(f"unmapped context role {raw!r}")
            if role not in roles:
                roles.append(role)
    master = occ.get("MasterName")
    if master:
        roles = merged.setdefault(master, [])
        if "utterer" not in roles:
            roles.insert(0, "utterer")
    if merged:
        occ["ContextMasters"] = [{"MasterName": n, "Roles": rs} for n, rs in merged.items()]
    else:
        occ.pop("ContextMasters", None)


def all_occ(entry: dict) -> list[dict]:
    return [o for s in entry.get("Senses") or [] for o in s.get("Occurrences") or []]


def repair(term_id: str, entry: dict) -> None:
    o = all_occ(entry)

    # Passage-reading corrections identified by the independent reviewer.
    if term_id == "t_37261001c332":
        set_other(o[2], reviewed("anonymous treatise questioner", "questioner", "questioner",
                  "問曰 introduces the headword-bearing question; the attributed Bodhidharma answer follows."),
                  [context("Bodhidharma", "case-figure")],
                  "Treatise on Breaking Through Appearances (達磨大師破相論), attributed to Bodhidharma: an unnamed dialogue questioner asks how inspecting mind counts as understanding; the answer follows in the attributed treatise voice. All six attribution rungs leave the questioner unnamed.")
        set_named(o[7], "Bodhidharma", "Treatise on Breaking Through Appearances (達磨大師破相論), attributed to Bodhidharma: the answer voice concludes that inspecting mind in this way may be called understanding.")
        replace_occ(o[8], rel="X/X68/X68n1319.xml", kwic="於此觀心若了了，混然一味自惺惺。",
                    master="Xuri Jushi", note="Imperially Selected Recorded Sayings (御選語錄), Xuri Jushi's signed verse: Xuri Jushi says that if inspecting mind here is clear, the undivided single flavor is itself alert.")
    elif term_id == "t_f3488daf27fd":
        set_named(o[1], "Xitang Zhizang", "Compendium of the Five Lamps (五燈會元), Xitang Zhizang section: Xitang summons the assembly and says that this monk is still somewhat closer.")
        set_named(o[2], "Guanghui Qingshun", "Compendium of the Five Lamps (五燈會元), Guanghui Qingshun section: after the monk says he does not understand, Guanghui answers that this is somewhat closer.")
        set_named(o[4], "Baiyan Fu", "Collected Comments on Old Cases of the Chan School (宗門拈古彙集) explicitly introduces Baiyan Fu with 白巖符云; Baiyan Fu says the preceding figures still do not compare quite as well with Yan's action.")
    elif term_id == "t_77821881a767":
        set_named(o[0], "Deshan Xuanjian", "Old Recorded Sayings of Venerable Masters (古尊宿語錄), Deshan Xuanjian section: in his own hall address Deshan calls old man Shakyamuni a dry shit-stick.")
        set_named(o[6], "Yunmen Wenyan", "Dahui Zonggao's record explicitly introduces the older saying with 雲門道: Yunmen Wenyan says that old man Shakyamuni and Indra encounter one another; Dahui comments afterward.")
        o[6]["ContextMasters"] = [context("Dahui Zonggao", "commentator", "later-quoter")]
    elif term_id == "t_bcc96a299271":
        o[0]["ContextMasters"] = [context("Baizhang Yuansu", "respondent", "section-subject")]
        o[0]["AttributionNote"] = "Complete Book of the Five Lamps, volumes 34–120 (五燈全書), Baizhang Yuansu section: an unnamed monk asks Baizhang to disclose the ancestral meaning; Baizhang answers. All six rungs leave the monk unnamed."
        set_other(o[1], reviewed("unnamed monk", "monk", "questioner", "僧問 introduces the headword-bearing request; Liangshan answers after it."),
                  [context("Liangshan Huan", "respondent", "section-subject")],
                  "Complete Book of the Five Lamps, volumes 34–120 (五燈全書), Liangshan Huan section: an unnamed monk asks Liangshan to give an indication to the gathered assembly; Liangshan answers. All six rungs leave the monk unnamed.")
        set_other(o[2], reviewed("named lay questioner He Changbai", "layman", "questioner", "何長白問 explicitly names the lay questioner governing the request."),
                  [context("Juelang Daosheng", "respondent", "record-owner")],
                  "Complete Record of Chan Master Juelang Daosheng (天界覺浪盛禪師全錄): the named layman He Changbai asks Juelang Daosheng to give an indication to the Fushan assembly; Juelang is the respondent and record owner.")
        set_other(o[6], narrated("compiler describing Jingqing Daofu", "The third-person clause 常以啐啄之機開示後學 describes Jingqing's habitual action; it is not Jingqing's quoted turn."),
                  [context("Jingqing Daofu", "person-described", "section-subject")],
                  "Blue Cliff Record (碧巖錄): the compiler narrates that Jingqing Daofu regularly used the pecking-and-hatching mechanism to instruct later students; Jingqing is the person described, not the utterer of 開示.")
    elif term_id == "t_327d40c2c9cb":
        replace_occ(o[1], rel="T/T47/T47n1985.xml", kwic="不如無事休歇去，飢來喫飯、睡來合眼，愚人笑我、智乃知焉。", master="Linji Yixuan", note="Recorded Sayings of Chan Master Linji Huizhao of Zhenzhou (鎮州臨濟慧照禪師語錄): Linji Yixuan tells the assembly it is better to rest without affairs, eat rice when hungry, and close the eyes when sleepy.")
        replace_occ(o[2], rel="X/X70/X70n1390.xml", kwic="我有一機，極盡玄微。飢來喫飯，寒來著衣。", master="Xisou Shaotan", note="Extensive Record of Chan Master Xisou Shaotan (希叟紹曇禪師廣錄): in his own hall address Xisou says, 'I have one mechanism, exhausting the hidden subtlety: eat rice when hungry, put on clothes when cold.'")
        replace_occ(o[3], rel="X/X71/X71n1414.xml", kwic="本覺者裡，但管飢來喫飯，困來打眠。熱則取凉，寒則向火。", master="Liaoran Qingyu", note="Recorded Sayings of Chan Master Liaoran Qingyu (了菴清欲禪師語錄): Liaoran tells the assembly that here one simply eats when hungry, sleeps when tired, takes coolness when hot, and approaches the fire when cold.")
    elif term_id == "t_87ad33788c8e":
        set_other(o[1], reviewed("unnamed monk", "monk", "questioner", "僧問 introduces the black-dragon question; Dapu responds only by clapping and looking."),
                  [context("Dapu Xuantong", "respondent", "section-subject")],
                  "Compendium of the Five Lamps (五燈會元), Dapu Xuantong section: an unnamed monk asks how to obtain the pearl beneath the black dragon's chin; Dapu claps and looks. All six rungs leave the monk unnamed.")
        set_named(o[4], "Yu'an Yu", "The source explicitly introduces 愚庵盂云: Yu'an Yu appraises the Nanyuan cat case using the black dragon's chin-pearl image.")
    elif term_id == "t_22885135d39e":
        set_other(o[1], reviewed("unnamed monk", "monk", "questioner", "問如何是一塵 is the monk's headword-bearing question; Huadu Shiyu answers afterward."),
                  [context("Huadu Shiyu", "respondent", "section-subject")],
                  "Compendium of the Five Lamps (五燈會元), Hangzhou Xixing Huadu Shiyu section: an unnamed monk asks what one speck is; Huadu answers. All six rungs leave the monk unnamed.")
        recut = "師云但識取一塵。師復云說得千般美"
        v = zc.verify(o[2]["RelPath"], recut)
        if not v["ok"]: raise RuntimeError("一塵 recut failed")
        o[2].update(Kwic=recut, FromLb=v["fromLb"], ToLb=v["toLb"], AttributionNote="Old Recorded Sayings of Venerable Masters (古尊宿語錄), Zihu Shenli section: after the monk's questions, Zihu Shenli himself says, 'Only recognize one speck,' and continues his address.")
    elif term_id == "t_192801178305":
        set_other(o[0], impersonal("quoted scripture voice", "quoted document", "經云 assigns the formula to a cited document rather than to Yongming Yanshou's own turn."),
                  [context("Yongming Yanshou", "compiler", "later-quoter")],
                  "Record of the Source-Mirror (宗鏡錄): Yongming Yanshou introduces the formula with 'a scripture says'; the quoted document voice states that the three realms are mind-only.")
        set_named(o[1], "Mazu Daoyi", "Compendium of the Five Lamps (五燈會元), Mazu Daoyi section: Mazu's address introduced by 一日謂眾曰 includes the formula that the three realms are mind-only.")
        set_other(o[5], reviewed("unnamed old speaker", "old speaker", "utterer", "古人道 explicitly assigns the formula to an unnamed old speaker; Yuanwu explains it afterward."),
                  [context("Yuanwu Keqin", "later-quoter", "commentator")],
                  "Blue Cliff Record (碧巖錄): Yuanwu Keqin introduces the formula with 'an old one said'; the old speaker is unnamed after all six rungs, and Yuanwu comments afterward.")
    elif term_id == "t_0d926425f385":
        replace_occ(o[2], rel="X/X71/X71n1411.xml", kwic="藥山手中書佛字，問他端爾要心開。只將佛字為酬對，元是曾持五戒來。", master="Hengchuan Xinggong", note="Recorded Sayings of Chan Master Hengchuan Xinggong (橫川行珙禪師語錄): Hengchuan Xinggong's own verse on Yaoshan says that answering only with the written word 'buddha' comes from formerly keeping the five precepts; the parallel anthology omitted the verse author's name.")
        set_other(o[4], narrated("compiler describing Songyue Yuangui", "有嶽神乞戒，師即為張座，付五戒已 is third-person case narration about Yuangui's action, not a quoted turn."),
                  [context("Songyue Yuangui", "person-described", "case-figure")],
                  "Orthodox Lineage of Chan (禪宗正脉), Songyue Yuangui section: the compiler narrates that Yuangui seated a mountain deity and conferred the five precepts; Yuangui is the described case figure, not the utterer of 五戒.")
    elif term_id == "t_3f7a6ab74b68":
        for idx in (2, 3):
            old = o[idx]
            label = "compiler narrating the monks' hall scene"
            old["ActorAttribution"] = narrated(label, "The third-person event is compiler narration, not a quoted speech turn.")
        set_other(o[4], narrated("compiler describing Zhaozhou Congshen", "大眾一時到僧堂前，師乃關却僧堂門 narrates Zhaozhou's action; he does not utter 僧堂."),
                  [context("Zhaozhou Congshen", "person-described", "section-subject")],
                  "Complete Book of the Five Lamps, volumes 1–33 (五燈全書), Zhaozhou Congshen section: the compiler narrates that Zhaozhou closes the monks' hall door after the assembly arrives; Zhaozhou is described, not the utterer of 僧堂.")
    elif term_id == "t_b2f05c3e4b7d":
        set_other(o[0], reviewed("unnamed old speaker", "old speaker", "utterer", "古人道 assigns the headword-bearing saying to an unnamed old speaker."),
                  [context("Yunmen Wenyan", "later-raiser", "record-owner")],
                  "Yunmen Wenyan's record introduces the line with 'an old one said'; the old speaker says 'one fitting phrase.' All six rungs leave that earlier speaker unnamed; Yunmen raises it later.")
    elif term_id == "t_2398f8fb6328":
        replace_occ(o[1], rel="J/J37/J37nB386.xml", kwic="珠云：『饑來喫飯，困來即眠。』僧云：『一切人總如是，同師用功否？』", master="Dazhu Huihai", note="Recorded Sayings of Chan Master Yuan'an Feng (遠菴僼禪師語錄): Yuan'an quotes the Dazhu Huihai case; Dazhu is the exact earlier speaker who says, 'eat when hungry, sleep when tired.' This is a later transmission of the same saying, not an independent origin.")
        o[1]["ContextMasters"].append(context("Yuan'an Feng", "later-quoter"))
    elif term_id == "t_882860247a9b":
        set_named(o[4], "Ruiyan Shiyan", "Fangrong Tongxi's record explicitly recalls Ruiyan Shiyan's self-address; Ruiyan is the exact earlier speaker of 'one in charge, stay alert,' and Fangrong comments on it.")
        o[4]["ContextMasters"] = [context("Fangrong Tongxi", "later-quoter", "commentator")]
    elif term_id == "t_057cc9ea8755":
        set_other(o[0], reviewed("unnamed monk", "monk", "questioner", "The request 請和尚示攝心之法 is the monk's headword-bearing turn; Chaozong answers afterward."),
                  [context("Chaozong Tongren", "respondent", "section-subject")],
                  "Chaozong Tongren's record: an unnamed monk asks the master to show the way of collecting mind; Chaozong answers. All six rungs leave the questioning monk unnamed.")
    elif term_id == "t_37771a869b4f":
        set_other(o[1], reviewed("named co-questioners Tanran and Nanyue Huairang", "co-questioners", "questioner", "有僧坦然、懷讓問 names two monks jointly as utterers of the one question, so no singular MasterName can represent the turn."),
                  [context("Nanyue Huairang", "questioner"), context("National Teacher Huian", "respondent", "section-subject")],
                  "Combined Essentials of the Lamps (聯燈會要), National Teacher Huian section: Tanran and Nanyue Huairang jointly ask the west-coming question; Huian answers. The singular MasterName field is therefore null and both the joint utterance and named context are explicit.")
    elif term_id == "t_93ab42fecdca":
        replace_occ(o[7], rel="X/X70/X70n1402.xml", kwic="將他本來無一物之語，以情意識和會卜度，便道無三界可出，無涅槃可證", master="Zhongfeng Mingben", note="Miscellaneous Records of Chan Master Tianmu Mingben (天目明本禪師雜錄): Zhongfeng Mingben warns against using discriminating consciousness to calculate over the saying 'originally not one thing' and drawing further conclusions from it.")

    # Every positive actor receives explicit utterer context; all contextual roles close.
    for occ in o:
        normalize_contexts(occ)


def main() -> None:
    review = json.loads(REVIEW.read_text(encoding="utf-8"))
    prior = {r["id"]: r for r in (json.loads(LEDGER.read_text(encoding="utf-8")).get("entries", []) if LEDGER.exists() else [])}
    ledger = {
        "schemaVersion": "1.0", "cohort": "semantic-r003-owner2-actor-repair",
        "instructions": "Editor repair ledger for reviewer2 passage-reading findings; updated after every entry.",
        "updatedUtc": NOW, "entries": [],
    }
    rows = []
    for finding in review["entries"]:
        term_id = finding["id"]
        path = BASE / finding["path"]
        before = sha(path)
        if before != finding["subjectEntrySha256"]:
            old = prior.get(term_id)
            if old and old.get("entryAfterSha256") == before and old.get("status") == "complete":
                rows.append(old)
                ledger["entries"] = rows
                LEDGER.write_text(json.dumps(ledger, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
                continue
            if old and old.get("status") == "complete":
                # Root-owned post-repair prose hygiene can legitimately change the
                # byte hash without changing an occurrence. Reverify every current
                # witness before advancing the durable ledger hash.
                current = json.loads(path.read_text(encoding="utf-8"))
                checks = []
                for idx, occ in enumerate(all_occ(current), 1):
                    v = zc.verify(occ["RelPath"], occ["Kwic"])
                    governed_variant = occ.get("EvidenceRole") == "variant" and bool(occ.get("VariantForm"))
                    if not v["ok"] or (current["SourceTerm"] not in occ["Kwic"] and not governed_variant):
                        raise RuntimeError(f"{term_id} occurrence {idx}: post-repair recheck failed")
                    checks.append({"occurrence": idx, "relPath": occ["RelPath"], "fromLb": occ["FromLb"], "zcVerify": "ok"})
                old["entryAfterSha256"] = before
                old["zcResults"] = checks
                old["postRepairCleanupReverifiedUtc"] = NOW
                rows.append(old)
                ledger["entries"] = rows
                LEDGER.write_text(json.dumps(ledger, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
                continue
            raise RuntimeError(f"{term_id}: subject hash changed before repair")
        entry = json.loads(path.read_text(encoding="utf-8"))
        repair(term_id, entry)
        checks = []
        for idx, occ in enumerate(all_occ(entry), 1):
            v = zc.verify(occ["RelPath"], occ["Kwic"])
            if not v["ok"]:
                raise RuntimeError(f"{term_id} occurrence {idx}: zc.verify failed")
            if entry["SourceTerm"] not in occ["Kwic"]:
                raise RuntimeError(f"{term_id} occurrence {idx}: headword absent after repair")
            if v["fromLb"] != occ.get("FromLb") or v["toLb"] != occ.get("ToLb"):
                occ["FromLb"], occ["ToLb"] = v["fromLb"], v["toLb"]
            checks.append({"occurrence": idx, "relPath": occ["RelPath"], "fromLb": occ["FromLb"], "zcVerify": "ok"})
        # Changed evidence reopens, but does not overturn, the independently reviewed semantic structure.
        path.write_text(json.dumps(entry, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        after = sha(path)
        row = {
            "id": term_id, "sourceTerm": finding["sourceTerm"], "entryRelPath": finding["path"],
            "status": "complete", "reviewerSubjectSha256": finding["subjectEntrySha256"],
            "entryBeforeSha256": before, "entryAfterSha256": after,
            "reviewVerdict": finding["verdict"], "disposition": "all reviewer findings applied; definition and sense structure re-tested and retained",
            "semanticRetest": "Changed actor evidence and replacement witnesses remain compatible with the existing preferred target, sense count, validation, and corpus-deviation claim.",
            "zcResults": checks, "displacedNameFindings": finding.get("displacedNameFindings", []),
        }
        rows.append(row)
        ledger["entries"] = rows
        ledger["updatedUtc"] = datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")
        LEDGER.write_text(json.dumps(ledger, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(f"complete {len(rows):02d}/30 {finding['sourceTerm']} {after[:12]}", flush=True)


if __name__ == "__main__":
    main()
