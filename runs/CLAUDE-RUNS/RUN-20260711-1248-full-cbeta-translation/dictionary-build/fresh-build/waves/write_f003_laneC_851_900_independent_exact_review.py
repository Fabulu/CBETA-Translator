import datetime,hashlib,json
from pathlib import Path
R=Path(__file__).resolve().parents[2]
packet_path=R/'fresh-build/waves/f003-laneC-851-900-current-semantic-review-packet.json'
packet=json.loads(packet_path.read_text())
revisions={
 858:'REVISE — The single sense identifies 莊子 only as Master Zhuang, but selected witnesses explicitly use the same form as the title of the Zhuangzi (reading it, tracing a saying to it, or asking whether someone has read it). A person and a book title are different things under the split test. Split person from work-title and anchor each; do not leave title occurrences under the person gloss.',
 868:'REVISE — The display target is the transitive action “bring a person to life,” but the same sense offers “a life-giving person” and “living person.” Those change grammatical and participant structure and are not interchangeable glosses. Re-test whether nominal 活人 is independently attested; either split a genuinely nominal referent with its own occurrence or remove the unsupported nominal alternates.',
 872:'REVISE — 梵志 is a social/category label covering several distinct people, as the explanation itself admits, so the definite display “the Brahmin wanderer” falsely suggests one case figure. Use an indefinite/category gloss such as “a Brahmin wanderer” and preserve individual compounds such as Long-Claw or Black-clan as separate identities, not evidence for a single biography.',
 875:'REVISE — The explanation explicitly merges a physical rear hall and the office/title attached to it. Place and office-holder are different things, not different readings of one thing. Split the architectural location from rear-hall office/title and anchor an occurrence under each; if the selected packet lacks a literal-space witness, harvest one before asserting that sense.',
 877:'REVISE — Most selected strings are false headword segmentations, not the office 僧正: 衲僧正眼 is “a patch-robed monk’s true eye,” and 老僧正坐 is “the old monk is sitting upright.” Only the final occurrence clearly names the clerical office. Remove the crossing-boundary false positives, rebuild depth from genuine official-title uses, and then re-test the gloss.',
 887:'REVISE — 擊拂子 is grammatically “strike the fly-whisk,” while the display target “strike with the fly-whisk” silently turns 拂子 into an instrument acting on an unstated object. The explanation further asserts the strike is directed at a seat, stand, or point although the stored KWICs mostly do not supply that target. Re-cut wider evidence and choose the object/instrument frame the corpus actually states.',
 896:'REVISE — The evidence set is dominated by 磬山, a mountain/monastery or lineage name, not the instrument 磬. Those nested proper-name matches cannot support “stone chime”; several are tables of contents or names. Remove compound-name contamination and rebuild the entry from genuine struck/heard chime occurrences, then test whether “stone” is materially warranted.',
 897:'REVISE — 上供 is the verb-object act “present/make an offering,” not an “upper offering.” The current preferred and alternate targets import a principal/upper hierarchy that the stored image, anniversary, and incense-offering witnesses do not establish. Re-gloss the rite as presenting an offering and state altar or commemorative rank only where a witness names it.',
 898:'REVISE — “White mallet” repeats the material/color parsing defect the public-feedback rules were designed to prevent. In 白椎/白槌, 白 is the proclamation/announcement associated with the mallet act; the evidence does not establish a white-colored implement. Remove color-bearing display and aliases, explain the formal announce-and-strike action, and preserve the officer/formula evidence.',
}
findings=[]
for item in packet['items']:
 path=R/item['path']; current=hashlib.sha256(path.read_bytes()).hexdigest()
 assert current==item['sha256'],(item['ordinal'],current,item['sha256'])
 wp=path.parent/'evidence.draft.json'
 verdict='REVISE' if item['ordinal'] in revisions else 'KEEP'
 finding=revisions.get(item['ordinal'],'KEEP — Preferred target, sense boundary, stored headword evidence, actor state, and reader-facing explanation remain mutually consistent under the current exact hash; no concrete semantic defect found in this independent pass.')
 findings.append({'ordinal':item['ordinal'],'id':item['id'],'term':item['term'],'entrySha256':current,
  'worksheetSha256':hashlib.sha256(wp.read_bytes()).hexdigest() if wp.exists() else None,'verdict':verdict,'finding':finding})
out={'schemaVersion':'1.0','reviewType':'independent-semantic-exact-hash-review','wave':'f003','lane':'C','ordinals':[851,900],
 'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'readOnly':True,'entriesEdited':False,'siteTouched':False,
 'sourcePacket':str(packet_path.relative_to(R)),'sourcePacketSha256':hashlib.sha256(packet_path.read_bytes()).hexdigest(),
 'mechanicalGate':'fresh-build/waves/f003-laneC-851-900-formal-gate-current.json','currentHashesVerified':True,
 'summary':{'entries':len(findings),'KEEP':sum(x['verdict']=='KEEP' for x in findings),'REVISE':sum(x['verdict']=='REVISE' for x in findings)},
 'findings':findings}
(R/'fresh-build/waves/f003-laneC-851-900-independent-exact-review.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
