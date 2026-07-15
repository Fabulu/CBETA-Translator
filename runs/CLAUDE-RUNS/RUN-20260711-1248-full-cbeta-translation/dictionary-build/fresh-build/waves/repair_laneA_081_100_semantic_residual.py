import json
from pathlib import Path
R=Path(__file__).resolve().parents[2]
def load(i):p=R/'fresh-build/entries'/i/'evidence.draft.json';return p,json.loads(p.read_text())
def save(p,d):p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
p,d=load('t_4cc95950b59a');s=d['Entry']['Senses'][0];s['Occurrences']=[o for o in s['Occurrences'] if '一句' in o['Kwic']];s['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in s['Occurrences']));s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)];save(p,d)
p,d=load('t_81147ad4e8bf');o=d['Entry']['Senses'][1]['Occurrences'][0];o['ContextMasters']=[{'MasterName':'Hengshan Dengbing','Roles':['utterer','later-quoter','record-owner']},{'MasterName':'Yongming Yanshou','Roles':['person-discussed']}];o['AttributionNote']='Hengshan Dengbing, in the Recorded Sayings of Chan Master Hengshan (衡山禪師語錄), introduces Yongming Yanshou as the cited source of the named Four Selections; Yongming is not the current live speaker.';save(p,d)
p,d=load('t_f25cebd24730');s=d['Entry']['Senses'][0];op=s['ExplanationParts']['CorpusEarnedOpening'].replace('a teacher','named masters');s['ExplanationParts']['CorpusEarnedOpening']=op;s['ExplanationParts']['EvidenceBody']=[x.replace('a teacher','named masters') for x in s['ExplanationParts']['EvidenceBody']];s['DraftEvidence']['ZenBend']=op;save(p,d)
