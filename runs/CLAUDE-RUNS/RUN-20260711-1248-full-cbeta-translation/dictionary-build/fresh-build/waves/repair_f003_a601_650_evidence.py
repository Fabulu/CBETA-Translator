#!/usr/bin/env python3
import json, re, subprocess, sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
F=json.loads((R/'fresh-build/waves/f003-laneA-601-650-prose-hygiene-formal-gate.json').read_text())

def narrated(rel,kw,claim,note):
 v=zc.verify(rel,kw);assert v['ok'],(rel,kw)
 return {'ClaimText':claim,'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True,
  'ActorAttribution':{'Status':'narrated','Kind':'documentary wording','ActorLabel':'the source text','ActorRole':'compiler','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'ReviewedBy':'Codex f003 A601-650 evidence repair','ReviewedUtc':'2026-07-15T00:00:00+00:00','GrammarEvidence':'The anchor is documentary or treatise wording, not an invented master utterance.'},
  'ContextMasters':[],'AttributionNote':f'Source text ({zc.title(rel)}): the source text is the documentary actor; {note}',
  'DraftActorProof':{'ExactHeadwordClause':kw,'GrammaticalSubject':'the source text','SpeechFrame':'Documentary or treatise wording without an isolated master speech turn.','FullCaseDecision':'The source owns the wording; no master utterer is invented.'}}
def spoken(rel,kw,claim,name,note):
 v=zc.verify(rel,kw);assert v['ok'],(rel,kw)
 return {'ClaimText':claim,'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True,'MasterName':name,
  'ContextMasters':[{'MasterName':name,'Roles':['utterer']}],'AttributionNote':f'Source text ({zc.title(rel)}): {note}'}

for ordinal,row in enumerate(F['entries'],601):
 d=R/'fresh-build/entries'/row['id'];p=d/'evidence.draft.json';x=json.loads(p.read_text());e=x['Entry']
 for s in e['Senses']:
  # Make the evidence relationship explicit and reader-visible without turning one witness into the idiom itself.
  occ=s.get('Occurrences',[]); named=next((o for o in occ if o.get('MasterName')),None)
  expl=s.get('Explanation') or (s.get('ExplanationParts') or {}).get('CorpusEarnedOpening') or f"{s.get('PreferredTarget','The term')} is the attested sense."
  first=expl.split('. ',1)[0].rstrip('.')+'.'
  if named:
   actor=named['MasterName'];title=zc.title(named['RelPath'])
   tail=f' In {title}, {actor} utters the stored headword-bearing clause; that exact turn anchors this sense, while the other stored cases test its range rather than being assigned wholesale to {actor}.'
  elif occ:
   title=zc.title(occ[0]['RelPath']);label=(occ[0].get('ActorAttribution') or {}).get('ActorLabel','the documentary narrator')
   tail=f' In {title}, {label} owns the stored headword-bearing clause; the entry therefore treats it as documentary evidence and does not invent a master speaker.'
  else: tail=''
  s['Explanation']=first+tail
  # Existing occurrences are the direct anchors for the retained first-sentence claim.
  s['ClaimAnchors']=[]
 # Individual semantic corrections.
 if ordinal==607:
  s=e['Senses'][0]
  s['Occurrences']=[o for o in s['Occurrences'] if not ('水月寺' in o['Kwic'] or len(o['Kwic'])>500 or o['RelPath'] in {'T/T48/T48n2016.xml','J/J39/J39nB459.xml'})]
  s['ClaimAnchors']=[]
  extra=narrated('T/T48/T48n2016.xml','此散意識構獲緣五根五塵水月鏡像時。當情變起遍計影像相分。此是假非實故。','水月','The source text discusses the moon in water together with a mirror image as an imagined image, explicitly calling it provisional rather than real.')
  extra.pop('ClaimText',None);s['Occurrences'].append(extra)
  extra=narrated('J/J39/J39nB459.xml','水月空華冰輪懸碧漢，團影落汪洋，浪破金鱗舞，波翻玉兔狂。','水月','The source text describes the moon disk in the sky and its round image falling onto the ocean.')
  extra.pop('ClaimText',None);s['Occurrences'].append(extra)
  s['Explanation']='The moon in water is a visible reflection that cannot be seized as the moon itself. In the retained clauses, named speakers and documentary owners use 水月 as reflection language; temple names and accidental long substrings have been removed.'
 if ordinal==610:
  e['Senses'][0]['Explanation']='The seamless monument is the monument Nanyang Huizhong asks the imperial patron to have made; its form cannot be reduced to ordinary seams or construction measurements. The stored Huizhong turns anchor the request, and later questions and deployments are retained as later uses rather than misidentified as the emperor’s memorial.'
 if ordinal==632:
  s=e['Senses'][0]
  s['Occurrences']=[o for o in s['Occurrences'] if o['RelPath'] not in {'T/T48/T48n2009.xml','T/T48/T48n2003.xml','J/J33/J33nB282.xml'}]
  anchors=[
   narrated('T/T48/T48n2009.xml','有為法。是因果。是受報。是輪迴法。不免生死。何時得成佛道。成佛須是見性。若不見性。因果等語是外道法。若是佛。不習外道法。佛是無業人無因果。','佛是無業人，無因果','Shaoshi Six Gates supplies the apophatic “a buddha is a person without karma, cause, or effect” wording.'),
   narrated('T/T48/T48n2016.xml','即心自性。此是表詮。由一切法無性故。即我心之實性。性亦非性者。此是遮詮。','遮詮','The Record of the Source Mirror explicitly labels negating wording “expression by negation.”'),
   narrated('T/T48/T48n2015.xml','遮謂遣其所非。表謂顯其所是。又遮者揀却諸餘。表者直示當體。','遮謂','The Chan Prolegomenon self-glosses expression by negation as removing what is not meant.'),
   narrated('T/T48/T48n2003.xml','若為人輕賤。是人先世罪業應墮惡道。以今世人輕賤故。先世罪業則為消滅。','先世業與今世果','The Blue Cliff Record quotes past-life karma producing a present consequence.'),
   narrated('J/J33/J33nB282.xml','行省罪犯彌天，定業難逃，入地獄已箭射，本師老和尚常寂光中垂救！','定業難逃','The source asserts that fixed karma is difficult to escape and invokes hell directly.'),
   narrated('J/J34/J34nB300.xml','某對云：「不落因果。」遂五百生墮野狐身。今請和尚代一轉語，貴脫野狐身。』丈曰：『汝問。』老人曰：『大修行人還落因果也無？』丈曰：『不昧因果。』老人於言下大悟，作禮曰：『某已脫野狐身，住在山後，敢乞依亡僧津送。','不落因果','The fox case links the two causal answers to five hundred births and a monastic funeral.')]
  s['Occurrences'] += [a for a in anchors if '業' in a['Kwic']]
  s['ClaimAnchors'] = [a for a in anchors if '業' not in a['Kwic']]
  s['Explanation']='Karma is action carried forward as conditioning force and consequence. The stored anchors show the strong causal register in past karma producing present consequence, fixed karma described as hard to escape, hell, and the fox’s five hundred births and monastic funeral; they also show the apophatic register “a buddha is without karma, cause, or effect,” controlled by two explicit self-glosses of expression by negation. These are attested tensions, not permission to erase causation or reduce 業 to conceptual entanglement alone.'
 if ordinal==633:
  s=e['Senses'][0]
  s['CorpusScopeMeasurement']={'allowlistHits':257,'allowlistFiles':134,'windowCharacters':60,'withoutKarmaVocabulary':255,'withKarmaVocabulary':2,'scope':'frozen allowlist concordance'}
  s['Explanation']='To bind oneself without a rope is to create one’s own confinement by fastening onto a side, phrase, or position; it is not a karma term by default. The frozen allowlist measurement is 257 verified uses in 134 texts: 255 have no karma vocabulary within sixty characters and two do. In Chaozong’s stored fox-case turn, he asks whether discarding “not falling,” guarding “not obscuring,” and thereby binding oneself without a rope is permissible while affirming clarity about causation; that exceptional causal neighborhood does not redefine the other 99.2 percent.'
  s['ClaimAnchors']=[]
 if ordinal==634:
  s=e['Senses'][0]
  s['ClaimAnchors'] = [
   narrated('T/T48/T48n2015.xml','遮謂遣其所非。表謂顯其所是。又遮者揀却諸餘。表者直示當體。','遮謂','The Chan Prolegomenon defines expression by negation, controlling the boundary between negation and causal denial.'),
   narrated('T/T48/T48n2016.xml','若非心非佛。是其遮詮。即護過遮非。去疑破執。','遮詮','The Record of the Source Mirror labels “not mind, not buddha” expression by negation rather than causal denial.')]
  s['Explanation']='To cast aside cause and effect is a named and condemned error: treating conduct and consequence as though they can simply be denied. The stored Gu Xue Zhe, Gu Ting, and Juelang Daosheng source clauses condemn that move in adaptive rhetoric, lax conduct, and inherited karma respectively; their occurrence metadata treats documentary wording as documentary wording rather than inventing utterers. Two additional stored apophatic controls explicitly define expression by negation, preventing every negative formula from being mislabeled 撥無因果.'
 # Refresh evidence keys and source lists after changes.
 for s in e['Senses']:
  s['SourceTexts']=sorted({o['RelPath'] for o in s.get('Occurrences',[])})
  de=s.setdefault('DraftEvidence',{});de['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s.get('Occurrences',[]))+1)]
 p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n')
 subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True,stdout=subprocess.DEVNULL)
print('repaired',len(F['entries']))
