#!/usr/bin/env python3
import json,re
from pathlib import Path
R=Path(__file__).resolve().parents[2]
def ld(i):
 p=R/'fresh-build/entries'/i/'evidence.draft.json';return p,json.loads(p.read_text(encoding='utf-8'))
def sv(p,d):p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def wrap(text):
 out=[];depth=0;i=0
 while i<len(text):
  c=text[i]
  if c in '(（':depth+=1;out.append(c);i+=1;continue
  if c in ')）':depth=max(0,depth-1);out.append(c);i+=1;continue
  if depth==0 and re.match(r'[\u3400-\u9fff\uf900-\ufaff]',c):
   j=i+1
   while j<len(text) and re.match(r'[\u3400-\u9fff\uf900-\ufaff]',text[j]):j+=1
   out+=['(',text[i:j],')'];i=j;continue
  out.append(c);i+=1
 return ''.join(out)

p,d=ld('t_ddab56ede4ef');s=d['Entry']['Senses'][0];x=s['Occurrences'][0]
if not any(c.get('MasterName')=='Yantou Quanhuo' for c in x.get('ContextMasters',[])):x.setdefault('ContextMasters',[]).append({'MasterName':'Yantou Quanhuo','Roles':['respondent','interlocutor','teacher']})
for i,r in enumerate(s['Occurrences']):
 if r['RelPath']=='X/X80/X80n1565.xml' and '忽桶底脫' in r['Kwic'] and '桶底脫自合歡喜' in r['Kwic']:
  a=dict(r);a.update({'FromLb':'0297a04','ToLb':'0297a04','Kwic':'忽桶底脫。','AttributionNote':'Source text Five Lamps Meeting the Source (五燈會元): the compiler narrates the noodle bucket suddenly losing its bottom; Zhenxie Qingliao comments only in the following marked speech turn.','ActorAttribution':{'Status':'narrated','Kind':'narrated mechanical event','ActorLabel':"the noodle bucket's bottom",'ActorRole':'compiler','GrammarEvidence':'In 忽桶底脫, 桶底 is the grammatical subject of 脫; no speech marker assigns the event to a human voice.','ReviewedBy':'Codex semantic-review repair','ReviewedUtc':'2026-07-15T00:00:00Z'},'ContextMasters':[{'MasterName':'Zhenxie Qingliao','Roles':['commentator','section-subject']}],'DraftActorProof':{'GrammaticalSubject':"the noodle bucket's bottom",'FullCaseDecision':'The compiler narrates 忽桶底脫; the following 師曰 begins Zhenxie Qingliao’s separate comment.'}})
  b={'RelPath':'X/X80/X80n1565.xml','FromLb':'0297a04','ToLb':'0297a05','Kwic':'師曰。桶底脫自合歡喜。','MasterName':'Zhenxie Qingliao','Curated':True,'AttributionNote':'Source text Five Lamps Meeting the Source (五燈會元): Zhenxie Qingliao is the exact speaker. 師曰 marks his comment that the bucket-bottom falling out should itself occasion joy.','ContextMasters':[{'MasterName':'Zhenxie Qingliao','Roles':['utterer','commentator','section-subject']}],'DraftActorProof':{'ExactHeadwordClause':'桶底脫自合歡喜','SpeechFrame':'師曰 explicitly introduces Zhenxie Qingliao’s response.','FullCaseDecision':'Zhenxie owns the marked comment; the preceding event remains narration.'}}
  s['Occurrences'][i:i+1]=[a,b];break
s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{n}' for n in range(1,len(s['Occurrences'])+1)];sv(p,d)

p,d=ld('t_398a33955019');r=next(o for o in d['Entry']['Senses'][0]['Occurrences'] if o['RelPath']=='T/T47/T47n1998A.xml' and o.get('ActorAttribution'))
r['ActorAttribution'].update({'Kind':'identified lay scholar','ActorLabel':'Zheng Shangming','GrammarEvidence':'The exchange identifies 士人鄭尚明 before his question; 和尚 addresses Dahui, and 妙喜曰 opens Dahui’s reply.'});r['AttributionNote']='Source text Record of Dahui Pujue (大慧普覺禪師語錄): the identified lay scholar Zheng Shangming is the exact questioner; Dahui’s reply begins only at 妙喜曰.';r['DraftActorProof'].update({'GrammaticalSubject':'the identified lay scholar Zheng Shangming','FullCaseDecision':'The source names 士人鄭尚明, a lay scholar rather than a visiting monastic.'});sv(p,d)

p,d=ld('t_824cfb1434b1');s=d['Entry']['Senses'][0];r=next(o for o in s['Occurrences'] if o['RelPath']=='X/X64/X64n1260.xml');r['MasterName']="Yu'an Ji";r['AttributionNote']="Source text Recorded Principles of the Lineage Patriarchs (列祖提綱錄): Yu'an Ji is the exact speaker under the heading 愚庵及禪師.";r['ContextMasters']=[{'MasterName':"Yu'an Ji",'Roles':['utterer','section-subject']}]
for k in ('SpeechFrame','FullCaseDecision'):r['DraftActorProof'][k]=r['DraftActorProof'][k].replace("Yu'an Puji","Yu'an Ji")
r=next(o for o in s['Occurrences'] if o['RelPath']=='T/T48/T48n2006.xml');r['AttributionNote']='Source text Eyes of Humans and Devas (人天眼目): compiler narration by Huiyan Zhizhao under the Linji-school heading coordinates rolling/folding, capture/release, and kill/live; Linji Yixuan is discussed, not quoted.';sv(p,d)

p,d=ld('t_3bf26be0cd43');s=d['Entry']['Senses'][0];s['PreferredTarget']='a black bucket';s['AlternateTargets']=['a pitch-black bucket','a black-lacquer bucket'];s['SearchAliases']=['a black bucket','a pitch-black bucket','black lacquer bucket','black lacquered bucket']
for k in ('Note',):s[k]=s[k].replace('enter, break, overturn, and leap','enter, break, and leap')
s['ExplanationParts']['CorpusEarnedOpening']='This black bucket can be broken, entered, lose its hoop, lose its bottom, leap, or serve as a blunt answer. The fixed modifier 黑漆 names it consistently, but the stored cases do not settle whether 漆 describes material, coating, appearance, a conventional compound, or a figurative relation.'
s['DraftEvidence']['CounterexampleOrLimit']=s['DraftEvidence']['CounterexampleOrLimit'].replace('enter, break, overturn, and leap','enter, break, and leap')
s['DraftEvidence']['ModifierControls']=['Positive morphology: 黑漆桶底 occurs 10 times in 9 files and 黑漆桶邊篐子斷 twice; these establish bottom and hoop, not material.','Positive comparison: 如黑漆桶一樣 occurs once but predicates the compound as a whole.','Counterexample: 黑漆桶裏黃金色 occurs 5 times in 5 files, resisting a simple darkness-throughout claim.','Negative controls: 漆黑桶, 黑桶, 踏翻黑漆桶, and 黑漆桶踏翻 have zero allowlisted hits.','Verdict: unresolved fixed/conventional modifier; translate secure 黑 as black and retain lacquer wording only in aliases.']
s['DraftEvidence']['ModifierStudy']={'Modifier':'黑漆','Decision':'unresolved fixed or conventional modifier','PositiveControls':['黑漆桶底 10/9','黑漆桶邊篐子斷 2/2','如黑漆桶一樣 1/1'],'Counterexample':'黑漆桶裏黃金色 5/5','NegativeControls':['漆黑桶 0','黑桶 0','踏翻黑漆桶 0','黑漆桶踏翻 0'],'TranslationConsequence':'Prefer “a black bucket”; preserve lacquer aliases without material, opacity, or symbolism claims.'}
for k in ('ZenBend','CounterexampleOrLimit'):s['DraftEvidence'][k]=s['DraftEvidence'][k].replace('opaque black bucket','black bucket').replace('opacity and enclosure','bucket morphology and enclosure').replace('broken, entered, overturned, or made to leap','broken, entered, or made to leap')
sv(p,d)

p,d=ld('t_b26bfa9e399e');s=d['Entry']['Senses'][0];keep=[]
for r in s['Occurrences']:
 if '返照回光' in r['Kwic']:r.pop('EvidenceRole',None);r.pop('VariantForm',None);r['ClaimText']='返照回光';s.setdefault('ClaimAnchors',[]).append(r)
 else:keep.append(r)
s['Occurrences']=keep;s['Note']='回/迴 is a governed graph alternation. Zhongfeng explicitly names “the four characters 回光返照” and supplies a description plus a falsification. Reversed 返照回光 is a related-family ClaimAnchor, not a graphic variant.'
s['ExplanationParts']['CorpusEarnedOpening']='The imperative reverses illumination from outward pursuit toward shining back. Stored cases command return, looking beneath one’s own feet, or attach a complement such as “illuminating past and present.”'
extra='Zhongfeng Mingben explicitly singles out “the four characters 回光返照,” then says merely sitting quietly with closed eyes is not what he names. This is his attested description and falsification, not a universal prescription. Miyun’s reversed 返照回光 remains a related phrase only.'
if extra not in s['ExplanationParts']['EvidenceBody']:s['ExplanationParts']['EvidenceBody'].append(extra)
s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{n}' for n in range(1,len(s['Occurrences'])+1)];sv(p,d)

p,d=ld('t_2facdfa49dd9');s=d['Entry']['Senses'][0];s['PreferredTarget']='final formidable barrier';s['AlternateTargets']=['final severe checkpoint','the formidable final barrier'];s['SearchAliases']=['final formidable barrier','final severe checkpoint','last formidable barrier','final barrier'];s['ExplanationParts']['CorpusEarnedOpening']=s['ExplanationParts']['CorpusEarnedOpening'].replace('last severe checkpoint','final formidable barrier or severe checkpoint').replace('‘locked barrier’ its resistance','牢 marks its formidable resistance')
r=next(o for o in s['Occurrences'] if o['RelPath']=='J/J36/J36nB359.xml');r['MasterName']='Baiyu Jingsi';r['AttributionNote']=r['AttributionNote'].replace('Baiyu Si','Baiyu Jingsi');r['ContextMasters']=[{'MasterName':'Baiyu Jingsi','Roles':['utterer','record-owner']}]
for k in ('SpeechFrame','FullCaseDecision'):r['DraftActorProof'][k]=r['DraftActorProof'][k].replace('Baiyu Si','Baiyu Jingsi')
sv(p,d)

p,d=ld('t_dec67da1f076');d['Entry']['Senses'][0]['ExplanationParts']['CorpusEarnedOpening']='Speakers use this paired verb phrase for a criticized arrest: sinking into emptiness and remaining stuck in stillness. Stored cases call it an illness, place it among explicit prohibitions, describe self-only concern that neglects living beings, or contrast it with treating the many things as absent.';sv(p,d)

p,d=ld('t_12e8cba30de6');s=d['Entry']['Senses'][0];r=next(o for o in s['Occurrences'] if o['RelPath']=='X/X68/X68n1318.xml');r.pop('MasterName',None);r['AttributionNote']='Source text Continued Essential Sayings of Ancient Venerable Masters (續古尊宿語要): Yungai Zhiben is the section narrator who introduces “an old monk named Puhui.” Puhui is the person described, not the utterer.';r['ActorAttribution']={'Status':'narrated','Kind':'section narration','ActorLabel':'Yungai Zhiben','ActorRole':'compiler','GrammarEvidence':'The passage is inside 雲蓋本和尚嗣白雲; 普會 is introduced as the object of 有一老僧，號曰普會, not a speech subject.','ReviewedBy':'Codex semantic-review repair','ReviewedUtc':'2026-07-15T00:00:00Z'};r['ContextMasters']=[{'MasterName':'Yungai Zhiben','Roles':['section-subject','record-owner']},{'MasterName':'Puhui','Roles':['person-described','case-figure']}];r['DraftActorProof']={'GrammaticalSubject':'Yungai Zhiben as section narrator','FullCaseDecision':'The section narrator introduces Puhui in third person; Puhui is the referent of 老僧 but not the textual speaker.'};sv(p,d)

# Independent rereview residuals: make Zhongfeng's falsification reader-visible,
# and preserve the enclosing 師乃云 speech frame in the Puhui account.
p,d=ld('t_b26bfa9e399e');s=d['Entry']['Senses'][0]
anchor={'RelPath':'X/X70/X70n1402.xml','FromLb':'0732c04','ToLb':'0732c05','Kwic':'如今有等癡人，靜僻處収視聽、絕見聞，如木石相似，喚作回光返照。','MasterName':'Zhongfeng Mingben','ClaimText':'喚作回光返照','AttributionNote':'Source text Miscellaneous Record of Tianmu Mingben (天目明本禪師雜錄): Zhongfeng Mingben is the exact speaker. He criticizes fools who withdraw sight and hearing in quiet, become like wood and stone, and call that (回光返照).','ContextMasters':[{'MasterName':'Zhongfeng Mingben','Roles':['utterer','record-owner']}],'DraftActorProof':{'ExactHeadwordClause':'喚作回光返照','SpeechFrame':'The clause continues Zhongfeng Mingben’s direct instruction to Wudi Liren without an intervening speaker change.','FullCaseDecision':'Zhongfeng Mingben states the falsification directly; the unnamed fools are the people described, not speakers.'}}
prior=next((x for x in s.get('ClaimAnchors',[]) if x.get('RelPath')==anchor['RelPath'] and x.get('Kwic')==anchor['Kwic']),None)
if prior: prior.update(anchor)
else:s.setdefault('ClaimAnchors',[]).append(anchor)
sv(p,d)

p,d=ld('t_12e8cba30de6');s=d['Entry']['Senses'][0];r=next(o for o in s['Occurrences'] if o['RelPath']=='X/X68/X68n1318.xml')
r['MasterName']='Yungai Zhiben';r.pop('ActorAttribution',None)
r['AttributionNote']='Source text Continued Essential Sayings of Ancient Venerable Masters (續古尊宿語要): Yungai Zhiben is the exact utterer in the address opened by (師乃云). He raises the account of an old monk named Puhui; Puhui is the person described, not the utterer.'
r['ContextMasters']=[{'MasterName':'Yungai Zhiben','Roles':['utterer','record-owner','later-raiser']},{'MasterName':'Puhui','Roles':['person-described','case-figure']}]
r['DraftActorProof']={'ExactHeadwordClause':'石霜山中有一老僧，號曰普會','SpeechFrame':'The enclosing address begins (師乃云) and remains Yungai Zhiben’s uninterrupted direct speech through this clause.','FullCaseDecision':'Yungai Zhiben utters the headword-bearing narration; Puhui is its third-person referent.'}
sv(p,d)

for eid in ['t_ddab56ede4ef','t_398a33955019','t_824cfb1434b1','t_3bf26be0cd43','t_b26bfa9e399e','t_2facdfa49dd9','t_dec67da1f076','t_12e8cba30de6']:
 p,d=ld(eid)
 for s in d['Entry']['Senses']:
  s['Note']=wrap(s.get('Note',''))
  parts=s['ExplanationParts'];parts['CorpusEarnedOpening']=wrap(parts['CorpusEarnedOpening']);parts['EvidenceBody']=[wrap(x) for x in parts['EvidenceBody']]
  parts['EvidenceBody']=list(dict.fromkeys(parts['EvidenceBody']))
  for r in [*s.get('Occurrences',[]),*s.get('ClaimAnchors',[])]:
   r['AttributionNote']=wrap(r['AttributionNote']);pr=r.get('DraftActorProof') or {}
   for f in ('SpeechFrame','FullCaseDecision'):
    if pr.get(f):pr[f]=wrap(pr[f])
 sv(p,d)
