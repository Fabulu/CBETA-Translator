import hashlib, json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]; E=ROOT/'fresh-build'/'entries'
rows=[]
ALLOWED={"utterer","respondent","questioner","interlocutor","addressee","section-subject","record-owner","person-described","person-discussed","commentator","later-raiser","later-quoter","teacher","student","compiler","verse-author","case-figure","action-performer"}
ROLE_MAP={"instructor":"teacher","case-teacher":"teacher","named-unrostered":"person-described","subsequent-speaker":"utterer","ceremony-patron":"person-discussed","address-speaker":"utterer","ceremony-master":"teacher","deceased-subject":"person-described","text-author":"person-described","hall-speaker":"utterer","person-appraised":"person-discussed","lineage-teacher":"teacher","person-called-great-lineage-master":"person-discussed","quoted-case-figure":"case-figure","acting-teacher":"teacher","case-source":"case-figure"}

def load(i):
 p=E/i/'entry.v2.json'; return p,json.loads(p.read_text(encoding='utf-8')),hashlib.sha256(p.read_bytes()).hexdigest()
def save(i,d,b,notes):
 p=E/i/'entry.v2.json';p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');rows.append({'entryId':i,'term':d['SourceTerm'],'beforeSha256':b,'afterSha256':hashlib.sha256(p.read_bytes()).hexdigest(),'changes':notes})
def normalize(d):
 for s in d.get('Senses',[]):
  for o in s.get('Occurrences',[]):
   a=o.get('ActorAttribution')
   if a and a.get('ActorRole') not in ALLOWED: a['ActorRole']={'narrator':'compiler','heading editor':'compiler','quoted-author':'verse-author'}.get(a.get('ActorRole'),'compiler')
   for c in o.get('ContextMasters',[]):
    rr=[]
    for r in c.get('Roles',[]):
     r=ROLE_MAP.get(r,r)
     if r=='action-performer': r='person-described'
     if r in ALLOWED and r not in rr: rr.append(r)
    c['Roles']=rr or ['person-discussed']

# Role-only entries.
for eid,notes in [
 ('t_7182bedf65d1',['Normalized instructor/case-teacher roles after full-case reread; exact turns unchanged.']),
 ('t_f74516e0ba71',['Mapped ceremony/address/deceased/author/hall metadata to the closed role vocabulary; fine detail remains in attribution prose.']),
 ('t_fa1b42d25280',['Mapped Yexian Guixing case-teacher to teacher while preserving the unnamed monk as utterer.']),
 ('t_97b566635d6c',['Mapped appraisal, lineage, quoted-case, and acting-teacher metadata to closed roles; repaired people and exact speakers preserved.']),
 ('t_37261001c332',['Removed named-unrostered from Zhang Tingyu roles while preserving verse-author and the corrected section ownership.'])]:
 p,d,b=load(eid);normalize(d);save(eid,d,b,notes)

# 安心: role plus orphaned prose fragment.
eid='t_79e00cdbc129';p,d,b=load(eid);normalize(d);s=d['Senses'][0]
s['Explanation']=s['Explanation'].replace('To set the mind at ease or to be at ease. To set the mind at ease or to be at ease.','To set the mind at ease or to be at ease.').replace(' To set the mind at ease or to be at ease, To set the mind at ease or to be at ease.',' To set the mind at ease or to be at ease.')
save(eid,d,b,['Mapped Gaofeng case-teacher to teacher.','Removed the duplicated orphan gloss fragment from the explanation.'])

# 法身向上事: closed roles and the named Qingshan/Miaowei context requested by complete-unit review.
eid='t_7c5f24652dfa';p,d,b=load(eid);normalize(d);o=d['Senses'][0]['Occurrences'][2]
o['ContextMasters']=[{'MasterName':'Qingshan','Roles':['respondent','case-figure']},{'MasterName':'Miaowei Jun','Roles':['later-raiser','commentator']}]
o['AttributionNote']=o.get('AttributionNote','')+' The complete unit retains Qingshan as respondent/case figure and Miaowei Jun as later commentator; the unnamed monk owns the question.'
save(eid,d,b,['Mapped Yunmen/Jingqing case-teacher roles to teacher.','Added Qingshan and Miaowei Jun to the previously empty contextual links for the unnamed question.'])

# 三句: normalize the raised Linji case metadata.
eid='t_830700de49fb';p,d,b=load(eid);normalize(d);save(eid,d,b,['Mapped Yunmen action-performer to person-described and Linji case-source to case-figure; later-raiser/questioner distinctions retained.'])

# 下禪床: read every unit. Feiyin voices the embedded narration; the Nanquan row is an anonymous capping verse, not a stage direction.
eid='t_74c3c0e1b896';p,d,b=load(eid);normalize(d);oc=d['Senses'][0]['Occurrences']
o=oc[2];o.pop('MasterName',None);o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'anonymous capping-verse voice','ActorLabel':'the unnamed verse author','ActorRole':'verse-author','GrammarEvidence':'頌曰 introduces the verse containing 跳下禪床; no personal author is supplied by the complete section.','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'ReviewedBy':'Codex replacement REVISE author full-case repair','ReviewedUtc':'2026-07-16T03:40:00Z'};o['ContextMasters']=[{'MasterName':'Nanquan Puyuan','Roles':['person-described','case-figure']}];o['AttributionNote']='禪宗頌古聯珠通集: an unnamed verse author comments on Nanquan leaving the seat; Nanquan is the action figure, not the verse utterer.'
o=oc[6];o['MasterName']='Feiyin Tongrong';o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':'Feiyin Tongrong','Roles':['utterer','later-raiser','record-owner']},{'MasterName':'Zhaozhou Congshen','Roles':['case-figure','person-discussed']}];o['AttributionNote']='費隱禪師語錄: Feiyin Tongrong, inside his own hall address, narrates the old woman paying Zhaozhou and says 趙州得財乃下禪床.'
save(eid,d,b,['Recovered Feiyin Tongrong as utterer of the embedded Zhaozhou narration.','Reclassified the Nanquan row as an anonymous capping verse and retained Nanquan as the described case figure.','Normalized action roles.'])

# 目前無法: add Jiashan self-link, close roles, and broaden 法 rather than forcing teaching-object.
eid='t_8060f979f21b';p,d,b=load(eid);normalize(d);s=d['Senses'][0];o=s['Occurrences'][5]
o['ContextMasters']=[{'MasterName':'Jiashan Shanhui','Roles':['utterer','section-subject']}]
s['PreferredTarget']='before the eyes, there is no dharma';s['AlternateTargets']=['before the eyes, there is no thing','before the eyes, there is no teaching']
s['Explanation']="The paired formula says, 'before the eyes there is no dharma; the meaning is before the eyes' and then, 'it is not a dharma before the eyes, and is not reached by eye or ear.' Here 法 remains deliberately broad: the records deploy it while asking what can be pointed out, obtained, or treated as present, but they do not restrict it to a teaching-object alone. 'Dharma,' 'thing,' and 'teaching' therefore remain available English handles, with the first preserving the formula's breadth."
save(eid,d,b,['Added Jiashan Shanhui as utterer/section-subject on the named direct occurrence.','Mapped case-teacher roles to teacher.','Broadened the English gloss of 法 and explained the corpus evidence instead of asserting teaching-object.'])

# 撫掌: Baoshou's clap is narrator-owned; Yangqi unit distinguishes his clap from the visiting supply master's clap.
eid='t_84e490b1773f';p,d,b=load(eid);normalize(d);oc=d['Senses'][0]['Occurrences']
o=oc[6];o.pop('MasterName',None);o['ActorAttribution']={'Status':'narrated','Kind':'compiler narration of Baoshou Fang action','ActorLabel':'the collection recorder narrating Baoshou Fang clapping','ActorRole':'compiler','GrammarEvidence':'After 寶壽方云 introduces Baoshou Fang’s quoted appraisal, 撫掌呵呵大笑、拂袖竟去 is recorder-governed action narration.','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'ReviewedBy':'Codex replacement REVISE author full-case repair','ReviewedUtc':'2026-07-16T03:40:00Z'};o['ContextMasters']=[{'MasterName':'Baoshou Fang','Roles':['person-described','section-subject']}];o['AttributionNote']='宗門拈古彙集: after Baoshou Fang’s quoted appraisal, the recorder narrates that he clapped, laughed, swept his sleeves, and left.'
o=oc[2];o['ActorAttribution']['GrammarEvidence']='The complete Yangqi case contains two distinct narrated claps: Yangqi Fanghui claps and laughs at the Dao-wu supply master’s answer; later the unnamed supply master claps once and Yangqi replies. The stored token is narrator-owned, not spoken.';o['ContextMasters']=[{'MasterName':'Yangqi Fanghui','Roles':['person-described','respondent','section-subject']}]
save(eid,d,b,['Moved Baoshou Fang from MasterName to narrated action performer context.','Made the Yangqi GrammarEvidence distinguish Yangqi’s clap from the unnamed supply master’s later clap.','Normalized action/quoted-author roles.'])

# 拂袖: normalize role vocabulary only; corrected actors survive.
eid='t_efbed6116e24';p,d,b=load(eid);normalize(d);save(eid,d,b,['Mapped named-unrostered/action/subsequent/instructor/case-teacher roles to closed roles while preserving all repaired people and exact-turn decisions.'])

out=Path(__file__).with_name('replacement-revise12-author-repair-ledger.json');out.write_text(json.dumps({'generatedUtc':'2026-07-16T03:40:00Z','rows':rows},ensure_ascii=False,indent=2)+'\n',encoding='utf-8');print(json.dumps({'entries':len(rows),'ledger':str(out)},ensure_ascii=False))
