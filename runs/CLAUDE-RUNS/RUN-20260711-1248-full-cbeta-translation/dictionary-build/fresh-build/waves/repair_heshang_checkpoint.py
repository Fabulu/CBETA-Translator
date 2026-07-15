import json,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];lp=ROOT/'fresh-build/waves/f001-laneA.json';led=json.loads(lp.read_text());e=next(x for x in led['entries'] if x['term']=='和尚');p=ROOT/'fresh-build/entries'/e['id']/'entry.v2.json';z=json.loads(p.read_text());s1,s2=z['Senses']
RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
def nonmaster(o,label,kind,role='utterer'):
 o['MasterName']=None;o['ActorAttribution']={'Status':'identified-non-master','Kind':kind,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':'The surrounding source identifies the named non-master as author or speaker of the headword-bearing clause.','ReviewedBy':'Codex fresh f001 lane A root review','ReviewedUtc':'2026-07-15T02:30:00Z'};o['AttributionNote']+=f' The exact headword-bearing actor is the identified non-master {label}.'
def narrated(o,context_name=None,heading=False):
 old=o.get('MasterName');o['MasterName']=None;o['ActorAttribution']={'Status':'impersonal' if heading else 'narrated','Kind':'editorial heading' if heading else 'biographical narration','ActorLabel':'an impersonal textual heading' if heading else 'the compiler','ActorRole':'compiler','GrammarEvidence':'The title belongs to heading or narrator grammar and is not uttered by the named section figure.','ReviewedBy':'Codex fresh f001 lane A root review','ReviewedUtc':'2026-07-15T02:30:00Z'}
 cms=o.setdefault('ContextMasters',[]);name=context_name or old
 if name and not any(c.get('MasterName')==name for c in cms):cms.append({'MasterName':name,'Roles':['person-described'] if not heading else ['section-subject']})
 o['AttributionNote']+=(' This is an impersonal textual heading.' if heading else ' This is compiler narration.')
nonmaster(s1['Occurrences'][1],'Huang Tingjian','lay author')
narrated(s1['Occurrences'][3],'Wuzu Fayan')
o=s1['Occurrences'][6];narrated(o,'Budai')
# The compound 諸和尚子 addresses a collective of monastics rather than naming a master.
collective=s1['Occurrences'].pop(8)
z['Senses'].append({'SenseKey':'collective-address','MasterName':'Xuefeng Yicun','PreferredTarget':'you monks','AlternateTargets':['all you monastics'],'SearchAliases':['you monks','monastic collective address'],'Status':'preferred','Explanation':'In the nested address form, Xuefeng Yicun addresses the standing assembly collectively as “you monks.” This plural vocative is not evidence for the singular title “master.”','Validation':'single-source-explicit','Note':'One explicit nested-compound address, separated from the singular master title.','Occurrences':[collective],'ClaimAnchors':[],'SourceTexts':[collective['RelPath']],'RelatedMasters':['Xuefeng Yicun'],'RelatedTerms':['諸和尚子','僧']})
narrated(s2['Occurrences'][0],heading=True)
nonmaster(s2['Occurrences'][1],'He Yizi','preface author')
# Wuzu Jie is a personal name in a raised case, not an ordination-preceptor construction.
wuzu=s2['Occurrences'].pop(4);wuzu['AttributionNote']+=' Here 戒 identifies Master Wuzu Jie; it does not mean precept conferral.';s1['Occurrences'].append(wuzu)
s1['Explanation']=s1['Explanation'].replace('和尚','the title')
s2['Explanation']=s2['Explanation'].replace('得戒和尚','ordination preceptor').replace('受戒','receiving precepts').replace('得戒','precept reception').replace('和尚','preceptor')
p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');e['entrySha256']=hashlib.sha256(p.read_bytes()).hexdigest();led['updatedUtc']='2026-07-15T02:30:00Z';lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n')
