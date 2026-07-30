#!/usr/bin/env python3
"""Deterministic adjudication-to-constructor actor representations."""
from __future__ import annotations

import json
from functools import lru_cache
from pathlib import Path

ROOT=Path(__file__).resolve().parents[5]
CLOSED_ROLES={
    "utterer","respondent","questioner","interlocutor","addressee",
    "section-subject","record-owner","person-described","person-discussed",
    "commentator","later-raiser","later-quoter","teacher","student",
    "compiler","verse-author","case-figure",
}
UNLINKED_STATUSES={"identified-unlinked-master","identified-non-master","reviewed-unnamed"}


@lru_cache(maxsize=1)
def roster_names() -> frozenset[str]:
    rows=json.loads((ROOT/"Assets/Data/lineage-masters.json").read_text(encoding="utf-8"))
    return frozenset(row["names"][0] for row in rows if row.get("names"))

@lru_cache(maxsize=1)
def roster_aliases() -> frozenset[str]:
    rows=json.loads((ROOT/"Assets/Data/lineage-masters.json").read_text(encoding="utf-8"))
    return frozenset(str(name).strip().casefold() for row in rows for name in (row.get("names") or []) if str(name).strip())


def adapt_actor(*, kind: str, label: str, role: str,
                context_masters: list[dict] | None=None) -> dict:
    """Return the one closure-valid constructor representation for an adjudicated actor."""
    context_masters=context_masters or []
    if kind=="roster-master":
        if label not in roster_names():
            raise ValueError(f"roster actor is not canonical: {label}")
        row={"master":label,"status":"linked","actorLabel":None,"role":role}
    elif kind in {"quoted-nonroster-master","unlinked-master"}:
        if not label or label.casefold() in roster_aliases():
            raise ValueError("unlinked master must be explicit and absent from every roster alias")
        canonical_role="utterer" if role in {"quoted-speaker","quoted-case-figure"} else role
        row={"master":None,"status":"identified-unlinked-master","actorLabel":label,"role":canonical_role}
    elif kind=="quoted-nonmaster":
        if not label or label.casefold() in roster_aliases():
            raise ValueError("quoted non-master must be explicit and absent from every roster alias")
        row={"master":None,"status":"identified-non-master","actorLabel":label,"role":"utterer"}
    elif kind=="named-nonmaster-author":
        if not label or label.casefold() in roster_aliases():
            raise ValueError("named non-master author must be explicit and absent from every roster alias")
        row={"master":None,"status":"identified-non-master","actorLabel":label,"role":"utterer"}
    elif kind=="unnamed-questioner":
        if "unnamed" not in label.casefold():
            raise ValueError("unnamed questioner label must explicitly say unnamed")
        row={"master":None,"status":"reviewed-unnamed","actorLabel":label,"role":"questioner"}
    else:
        raise ValueError(f"unknown adjudicated actor kind: {kind}")
    if row["role"] not in CLOSED_ROLES:
        raise ValueError(f"exact actor role is not canonical: {row['role']}")
    for context in context_masters:
        name=context.get("MasterName")
        roles=context.get("Roles") or []
        if name not in roster_names():
            raise ValueError(f"context master is not roster-canonical: {name}")
        if not roles or set(roles)-CLOSED_ROLES:
            raise ValueError(f"context master roles are not closed: {name} {roles}")
    row["contextMasters"]=context_masters
    return row


def builder_use(actor: dict, rel: str, family: str, tier: int,
                review_meta: dict | None=None) -> tuple:
    """Convert an adapted actor to the tuple consumed by reviewed config builders."""
    meta={"contextMasters":actor["contextMasters"],**(review_meta or {})}
    if actor["actorLabel"]:
        meta["actorLabel"]=actor["actorLabel"]
    return (rel,actor["master"],family,tier,actor["role"],None,actor["status"],meta)


def merge_context_masters(base: list[dict], additions: list[dict]) -> list[dict]:
    """Merge context masters by identity while preserving every canonical role."""
    merged=[]
    for row in [*base,*additions]:
        name=row["MasterName"]
        target=next((item for item in merged if item["MasterName"]==name),None)
        if target is None:
            merged.append({"MasterName":name,"Roles":list(dict.fromkeys(row["Roles"]))})
        else:
            target["Roles"]=list(dict.fromkeys([*target["Roles"],*row["Roles"]]))
    return merged


def verify_builder_uses(specs: list[dict]) -> None:
    """Early assembled-use actor closure, before full config construction."""
    for spec in specs:
        for index,use in enumerate(spec.get("uses") or [],1):
            master=use[1]
            role=use[4]
            status=use[6] if len(use)>6 else "linked"
            meta=use[7] if len(use)>7 and isinstance(use[7],dict) else {}
            label=meta.get("actorLabel")
            if role not in CLOSED_ROLES:
                raise ValueError(f"{spec['id']} use {index}: invalid exact actor role {role}")
            if status=="linked":
                if master not in roster_names():
                    raise ValueError(f"{spec['id']} use {index}: linked actor is not canonical")
            elif status in UNLINKED_STATUSES:
                if master is not None or not label:
                    raise ValueError(f"{spec['id']} use {index}: unlinked actor XOR failed")
                if status in {"identified-unlinked-master","identified-non-master"} and label.casefold() in roster_aliases():
                    raise ValueError(f"{spec['id']} use {index}: roster identity left nonlinked")
                if status=="reviewed-unnamed" and "unnamed" not in label.casefold():
                    raise ValueError(f"{spec['id']} use {index}: unnamed label is not explicit")
            else:
                raise ValueError(f"{spec['id']} use {index}: unsupported actor status {status}")
            for context in meta.get("contextMasters") or []:
                if context.get("MasterName") not in roster_names():
                    raise ValueError(f"{spec['id']} use {index}: noncanonical context master")
                roles=context.get("Roles") or []
                if not roles or set(roles)-CLOSED_ROLES:
                    raise ValueError(f"{spec['id']} use {index}: invalid context roles")
