#!/usr/bin/env python3
import json, sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
rows={r['term']:r for r in json.loads((R/'fresh-build/waves/f002-laneB-401-500-preflight.json').read_text())['entries'][80:90]}

def load(term):
 p=R/'fresh-build/entries'/rows[term]['id']/'evidence.draft.json';return p,json.loads(p.read_text())
def actor(label,kind='compiler narration',role='compiler',status='narrated',proof='The complete-case grammar identifies the textual actor and does not assign the clause to a quoted master.'):
 z={'Status':status,'Kind':kind,'ActorLabel':label,'ActorRole':role,'GrammarEvidence':proof,'ReviewedBy':'Codex f002 B481-490 full-case review','ReviewedUtc':'2026-07-15T00:00:00Z'}
 if status=='reviewed-unnamed':z['RungsChecked']=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
 return z
def add_occ(term,rel,kw,master=None,a=None,contexts=None,proof=None):
 p,d=load(term);s=d['Entry']['Senses'][0];v=zc.verify(rel,kw);assert v['ok'],(term,rel,kw)
 o={'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True,'ContextMasters':contexts or []}
 if master:o['MasterName']=master
 else:o['ActorAttribution']=a
 o['AttributionNote']=f"Source text ({zc.title(rel)}). {proof}"
 if master:o['DraftActorProof']={'ExactHeadwordClause':kw,'SpeechFrame':proof,'FullCaseDecision':proof}
 else:o['DraftActorProof']={'GrammaticalSubject':a['ActorLabel'],'FullCaseDecision':proof}
 s.setdefault('Occurrences',[]).append(o);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
def add_anchor(term,rel,kw,claim,a,contexts=None,proof='The complete case anchors the quoted wording used in the explanation.'):
 p,d=load(term);s=d['Entry']['Senses'][0];v=zc.verify(rel,kw);assert v['ok'],(term,rel,kw)
 o={'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'ClaimText':claim,'Curated':True,'ActorAttribution':a,'ContextMasters':contexts or [],'AttributionNote':f"Source text ({zc.title(rel)}). {proof}",'DraftActorProof':{'GrammaticalSubject':a['ActorLabel'],'FullCaseDecision':proof}}
 s.setdefault('ClaimAnchors',[]).append(o);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')

# 茫然: two distinct narrated public failures to answer.
add_occ('茫然','X/X81/X81n1568.xml','僧以手便拂，師曰：作甚麼？僧茫然，師曰：賺却一船人。',a=actor('the unnamed monk in the recorded exchange','direct-exchange participant','interlocutor','reviewed-unnamed','The grammar names 僧 as the person left at a loss; the surrounding section does not supply his personal name.'),proof='The unnamed monk is explicitly the participant left at a loss after being asked what he is doing.')
add_occ('茫然','T/T51/T51n2076.xml','三藏曰。汝何不自觀自靜。彼僧茫然莫知其對。',a=actor('the lamp-record compiler'),contexts=[{'MasterName':'Kuduo Tripitaka','Roles':['case-figure']}],proof='The lamp-record compiler narrates that the unnamed monk was at a loss and did not know how to answer Kuduo Tripitaka.')

# 契悟: collective realization and a named official's realization remain the same event verb.
add_occ('契悟','X/X84/X84n1580.xml','一女曰：作麼，作麼？諸姊諦觀，各各契悟。',a=actor('the case compiler'),proof='The case compiler narrates that each of the seven women realizes after one woman calls the others to look carefully.')
add_occ('契悟','X/X84/X84n1583.xml','堂曰：為甚麼却道開口不得？公乃契悟。',a=actor('the continuation-record compiler'),proof='The continuation-record compiler narrates the public official’s realization immediately after Nandang’s question.')
add_anchor('契悟','X/X82/X82n1571.xml','日舉靈雲悟道機語問之，師擬對，日曰：不是，不是。','悟道',actor('the lamp-record compiler'),proof='The lamp-record compiler explicitly labels Lingyun’s event with the related expression “realize the Way.”')

# 玄機: direct saying plus two independently narrated deployments.
add_occ('玄機','T/T51/T51n2076.xml','心若無事萬象不生。意絕玄機纖塵何立。','Panshan Baoji',contexts=[{'MasterName':'Panshan Baoji','Roles':['utterer']}],proof='Panshan Baoji owns the upper-hall saying that pairs ending subtle workings with no speck standing.')
add_occ('玄機','X/X78/X78n1556.xml','門庭峻捷，玄機莫湊。所印可者，皆為道器。',a=actor('the continuation-lamp compiler'),proof='The continuation-lamp compiler describes the master’s steep public front and says subtle workings could not approach it.')
add_occ('玄機','X/X81/X81n1568.xml','師以玄機一發，雜務俱捐，振錫南邁，抵福州參長慶，不大發明。',a=actor('the lamp-record compiler'),contexts=[{'MasterName':'Fayan Wenyi','Roles':['person-described','section-subject']}],proof='The lamp-record compiler narrates Fayan Wenyi’s subtle working becoming active and his abandonment of miscellaneous affairs.')

# 接引: reception in exchange, explicit challenge, and a teacher's own classification.
add_occ('接引','X/X81/X81n1571.xml','士出禮曰：謝師接引。師便打。',a=actor('the unnamed Daoist in the recorded exchange','direct-exchange participant','respondent','reviewed-unnamed','The preceding sentence identifies the actor only as a Daoist; he thanks the master for receiving and leading him.'),proof='The unnamed Daoist explicitly thanks the master for receiving and leading him immediately before the master strikes.')
add_occ('接引','C/C077/C077n1710.xml','師云接引鈍根人語未可依憑','Huangbo Xiyun',contexts=[{'MasterName':'Huangbo Xiyun','Roles':['utterer']}],proof='Huangbo Xiyun calls inherited talk about receiving people language for dull faculties and says it cannot be relied upon.')
add_occ('接引','X/X72/X72n1437.xml','然此亦是門庭施設，接引中下。若有箇漢於堂奧之中向上關棙，一脚踏翻','Yongjue Yuanxian',contexts=[{'MasterName':'Yongjue Yuanxian','Roles':['utterer']}],proof='Yongjue Yuanxian classifies the arrangement as a public-front device for receiving middling and lower people, then contrasts an upward overturning in the inner room.')
add_anchor('接引','J/J39/J39nB471.xml','進云：「某甲亦望接引。」師云：「把手牽人行不得，為人自肯乃方親。」','某甲亦望接引；把手牽人行不得；為人自肯乃方親',actor('the recorded questioner and Konggu Daocheng','mixed public exchange','interlocutor','narrated','The exchange explicitly assigns the request to the questioner and the reply to Konggu Daocheng.'),contexts=[{'MasterName':'Konggu Daocheng','Roles':['respondent']}],proof='The public exchange anchors the questioner’s request and Konggu Daocheng’s complete two-line reply.')
add_anchor('接引','J/J34/J34nB311.xml','牛首融悟、接引閣體玄、淨業堂見樸三上座，暨傅仁宇、鍾弘儀、孫啟南、胡耳門諸居士請上堂','接引閣',actor('the sayings-record compiler'),proof='The sayings-record compiler lists the monk identified with Reception Pavilion among those requesting the upper-hall address; this anchors the excluded building name.')

for term in ('茫然','契悟','玄機','接引'):
 p,d=load(term);s=d['Entry']['Senses'][0];s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s.get('Occurrences') or [])+1)];s['DraftEvidence']['IndependentWorkIds']=sorted({zc.work_id(o['RelPath']) for o in s.get('Occurrences') or []});p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
