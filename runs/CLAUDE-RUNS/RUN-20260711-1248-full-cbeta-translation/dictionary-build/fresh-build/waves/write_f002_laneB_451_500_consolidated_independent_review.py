import hashlib, json
from datetime import datetime, timezone
from pathlib import Path

H=Path(__file__).resolve().parent; R=H.parent.parent
P=H/'f002-laneB-451-500-provisional-independent-semantic-review.json'
E=H/'f002-laneB-451-500-provisional-eight-repair.json'
D=H/'f002-laneB-451-500-depth6-repair-ledger.json'
O=H/'f002-laneB-451-500-consolidated-independent-semantic-review.json'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
prior=json.loads(P.read_text()); eight=json.loads(E.read_text()); depth=json.loads(D.read_text())
rows=prior['findings']; changed={x['id'] for x in eight['entries']}|{x['id'] for x in depth['entries']}
assert len(changed)==8
notes={
't_0d6794766098':'KEEP: the repaired opening closes both English glosses and preserves one interrogative referent across objects, persons, places, and live exchanges; all eight full cases remain within “what is it/this?”',
't_ee57d3ff5e43':'KEEP: the prose now identifies Feiyin Tongrong, and the added Hanyue Fazang interview is a distinct-work live encounter in which the question explicitly asks how to meet the occasion; it broadens deployment without changing the occasion/trigger referent.',
't_21a3463bc0db':'KEEP: the repaired complete opening states the distributive location meaning, and Yuanwu’s added distinct-work passage independently uses the term for establishing the Dharma banner wherever one is; no new referent or split is introduced.',
't_bf4ad761840f':'KEEP: the malformed quotation is gone; the personal native-ground sense remains distinct from the administrative local adjective, and every case supports its assigned referent.',
't_133711ebf761':'KEEP: the repaired quotation closes cleanly; the three added distinct works use the same subtle-working/pivot referent in teaching-seat proclamation, marvelous function, and sole proclamation, adding breadth without a #0g split.',
't_beab8961fb55':'KEEP: the repaired actor sentence is exact, and the three added distinct works all concern receiving/leading students or arrivals; they do not manufacture a fixed method or a second referent.',
't_5f6e8c98ffe7':'KEEP: the repaired prose names the stored actors, and Baichi Yuan’s added interview uses the same prior-recognition formula before commentary; recognition and expectation remain one grammatical function.',
't_782f20a368c3':'KEEP: the reader prose now begins as complete sentences with exact narrated subjects; Tian’an Sheng’s added discourse explicitly repeats awakening/understanding across participants and remains within the same event predicate.',
}
out=[]; unchanged=0
for row in rows:
    base=R/'fresh-build/entries'/row['id']; ep=base/'entry.v2.json'; wp=base/'evidence.draft.json'
    eh,wh=sha(ep),sha(wp)
    if row['id'] not in changed:
        assert row['verdict']=='KEEP' and eh==row['entrySha256'] and wh==row['worksheetSha256']
        finding=row['finding']; unchanged+=1
    else:
        assert row['id'] in notes
        finding=notes[row['id']]
    entry=json.loads(ep.read_text()); senses=entry['Senses']
    out.append({'ordinal':row['ordinal'],'id':row['id'],'term':row['term'],
      'path':str(ep.relative_to(R)),'entrySha256':eh,'worksheetSha256':wh,
      'verdict':'KEEP','finding':finding,'senseCount':len(senses),
      'occurrenceCount':sum(len(s.get('Occurrences',[])) for s in senses),
      'claimAnchorCount':sum(len(s.get('ClaimAnchors',[])) for s in senses)})
assert unchanged==42 and len(out)==50
report={'schemaVersion':1,'reviewType':'consolidated-exact-hash-independent-semantic-review',
 'wave':'f002','lane':'B','ordinals':[451,500],
 'reviewer':'Codex independent reviewer (not Lane B author)',
 'reviewedUtc':datetime.now(timezone.utc).isoformat(),'state':'independent-KEEP-at-exact-current-hashes',
 'readOnly':True,'entryEditsMade':False,'formalGateRun':False,'promotionPerformed':False,'siteTouched':False,
 'inputs':{'priorProvisionalReview':{'path':str(P.relative_to(R)),'sha256':sha(P)},
  'eightProseRepair':{'path':str(E.relative_to(R)),'sha256':sha(E)},
  'sixDepthRepair':{'path':str(D.relative_to(R)),'sha256':sha(D)}},
 'verification':{'requestedChangedRows':14,'deduplicatedChangedEntries':8,
  'changedEntriesIndependentlyRereviewed':8,'priorKEEPHashesVerifiedUnchanged':42,
  'allCurrentHashesHaveIndependentKEEP':True},
 'summary':{'entries':50,'KEEP':50,'REVISE':0},'findings':out}
O.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'output':str(O.relative_to(R)),'sha256':sha(O),'summary':report['summary'],
 'verification':report['verification']}))
