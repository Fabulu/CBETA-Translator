#!/usr/bin/env python3
"""Cheap advisory lint for semantic defects that mechanical gates miss.

It never assigns an actor or rewrites prose. It blocks handoff for a human
full-case decision when worksheet fields contradict each other or exhibit a
known high-risk construction.
"""
import argparse,json,re,time
from pathlib import Path

BASE=Path(__file__).resolve().parent
RISKY_CLAIM=re.compile(r"\b(?:demands?|discloses?|reveals?|symboli[sz]es?|proves?|guarantees?|means enlightenment)\b",re.I)
ACTION_TARGET=re.compile(r"^to\s+(?:strike|hit|raise|lift|shout|slam|knock|beat|brandish|throw|point)\b",re.I)
STAGE_TERM=re.compile(r"(?:一下|一喝|一棒|一掌|卓|拈|舉|竪|豎|打|喝)$")
QUESTIONER_FRAME=re.compile(r"(?:僧問|進云)[^。！？；]*")

def resolve(raw):
 p=Path(raw)
 if not p.exists() and raw.startswith('t_'):p=BASE/'fresh-build/entries'/raw/'evidence.draft.json'
 elif p.is_dir():p=p/'evidence.draft.json'
 return p

def lint(path):
 root=json.loads(path.read_text(encoding='utf-8-sig'));e=root.get('Entry',root);term=str(e.get('SourceTerm') or '');flags=[]
 for si,s in enumerate(e.get('Senses') or [],1):
  opening=' '.join([str((s.get('ExplanationParts') or {}).get('CorpusEarnedOpening') or ''),*map(str,(s.get('ExplanationParts') or {}).get('EvidenceBody') or [])])
  for m in RISKY_CLAIM.finditer(opening):flags.append({'kind':'unsupported-prose-claim-risk','sense':si,'token':m.group(0),'action':'anchor the inference to a stored occurrence or narrow the wording before review'})
  action_risk=bool(ACTION_TARGET.search(str(s.get('PreferredTarget') or '')) or STAGE_TERM.search(term))
  for oi,o in enumerate(s.get('Occurrences') or [],1):
   master=str(o.get('MasterName') or '');proof=o.get('DraftActorProof') or {};subject=str(proof.get('GrammaticalSubject') or '')
   if master and subject and master.casefold() not in subject.casefold() and subject.casefold() not in master.casefold():
    flags.append({'kind':'master-proof-subject-mismatch','sense':si,'occurrence':oi,'masterName':master,'proofSubject':subject,'action':'read the full case; MasterName must be the utterer of the headword'})
   if master and action_risk and proof.get('ActionPerformerRiskReviewed') is not True:
    flags.append({'kind':'action-performer-mastername-risk','sense':si,'occurrence':oi,'masterName':master,'action':'decide whether the text narrator states a physical action; if so MasterName is null and the performer belongs in ContextMasters'})
   kwic=str(o.get('Kwic') or ''); headword_at=kwic.find(term)
   if master and headword_at >= 0 and any(m.start() < headword_at for m in QUESTIONER_FRAME.finditer(kwic)):
    flags.append({'kind':'questioner-frame-mastername-risk','sense':si,'occurrence':oi,'masterName':master,'action':'read the full exchange; 僧問 is anonymous and 進云 normally continues the interlocutor, while the respondent belongs in ContextMasters'})
 return {'id':e.get('Id'),'term':term,'path':str(path),'flags':flags,'passes':not flags}

def main():
 ap=argparse.ArgumentParser();ap.add_argument('entries',nargs='+');ap.add_argument('--report',type=Path);ap.add_argument('--advisory',action='store_true');a=ap.parse_args();start=time.perf_counter();rows=[lint(resolve(x)) for x in a.entries];payload={'schemaVersion':'authoring-risk-preflight-v1','elapsedSeconds':round(time.perf_counter()-start,4),'entries':len(rows),'passing':sum(x['passes'] for x in rows),'flagged':sum(not x['passes'] for x in rows),'flags':sum(len(x['flags']) for x in rows),'results':rows};text=json.dumps(payload,ensure_ascii=False,indent=2)+'\n';
 if a.report:a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(text,encoding='utf-8')
 print(json.dumps({k:payload[k] for k in ('entries','passing','flagged','flags','elapsedSeconds')},indent=2));return 0 if a.advisory or not payload['flagged'] else 1
if __name__=='__main__':raise SystemExit(main())
