import hashlib, json, sys
from datetime import datetime, timezone
from pathlib import Path

B=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(B))
import zc

HERE=Path(__file__).parent
STAMP=datetime.now(timezone.utc).isoformat()
IDS=['t_79e00cdbc129','t_7c5f24652dfa','t_84e490b1773f','t_d0d82a2681a0','t_e17068150613']

def ep(i): return B/'fresh-build'/'entries'/i/'entry.v2.json'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
before={i:sha(ep(i)) for i in IDS}

# 撫掌: name every supplied action performer structurally and remove prose claims
# that lost their evidence when the mixed-action KWICs were correctly recut.
i='t_84e490b1773f'; p=ep(i); d=json.loads(p.read_text()); o=d['Senses'][0]['Occurrences']
performers=[
    ('Jinniu Heshang','Jinniu claps, dances, laughs, and calls the assembly to eat.'),
    ('Xiyuan Tanzang','Xiyuan claps three times after the monk asks why he heats his own bath.'),
    ('Yangqi Fanghui','Yangqi claps and laughs before judging the supply master’s answer.'),
    ('Hongzhou Shuilao','Shuilao rises after Mazu’s kick, claps, laughs, and speaks.'),
    (None,'An unnamed monk claps once and then strikes.'),
    ('Foyan Qingyuan','Foyan claps and laughs before addressing the assembly.'),
    ('Baoshou Fang','Baoshou Fang claps, laughs, and leaves after his appraisal.'),
    (None,'The unnamed verse author imagines an immortal clapping and laughing.'),
]
for occ,(name,detail) in zip(o,performers):
    occ['ContextMasters']=([{'MasterName':name,'Roles':['person-described','record-owner']}] if name else [])
    occ['AttributionNote']=f"Source text ({zc.title(occ['RelPath'])}). Exact headword actor: the case narrator. {detail} The performer does not utter the action phrase."
d['Senses'][0]['Explanation']=(
    'Clapping the hands is a narrated audible gesture that can accompany laughter, speech, departure, or a blow. '
    'The stored cases name Jinniu, Xiyuan Tanzang, Yangqi Fanghui, Hongzhou Shuilao, Foyan Qingyuan, and Baoshou Fang '
    'as performers; another case reports an unnamed monk, and one anonymous verse imagines an immortal clapping. '
    'The gesture occurs 746 times in 231 allowlisted texts, while “clapped his hands and laughed loudly” occurs 98 '
    'times in 60 texts. The surrounding exchange establishes the local force; the physical gesture alone does not '
    'encode celebration, challenge, appraisal, or dismissal.'
)
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')

# 第一句: isolate Linji's own first-phrase formulation from the later monk's
# separately uttered repetition in the same complete case.
i='t_d0d82a2681a0'; p=ep(i); d=json.loads(p.read_text()); occ=d['Senses'][0]['Occurrences'][2]
kwic='若第一句中薦得，堪與祖佛為師'
v=zc.verify(occ['RelPath'],kwic)
if not v.get('ok'): raise ValueError((kwic,v))
occ['Kwic'],occ['FromLb'],occ['ToLb']=kwic,v['fromLb'],v['toLb']
occ['AttributionNote']='Source text (五燈嚴統(第10卷-第25卷)). Exact headword actor: Linji Yixuan. The recut isolates Linji’s formulation and excludes the unnamed monk’s later separate question repeating 第一句.'
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')

after={i:sha(ep(i)) for i in IDS}
rows=[]
terms={i:json.loads(ep(i).read_text())['SourceTerm'] for i in IDS}
for i in IDS:
    changed=before[i]!=after[i]
    rows.append({'id':i,'term':terms[i],'disposition':'REVISE' if changed else 'KEEP','reviewedSha256':before[i],
                 'outputSha256':after[i],'selfApproved':not changed,'requiresIndependentReview':changed})
payload={'generatedUtc':STAMP,'reviewScope':'full definitions and all 36 complete cases','rows':rows,
         'changedFindings':[
             '撫掌: restored named action performers in ContextMasters and removed unanchored prose left behind by prior KWIC recuts.',
             '第一句: recut Linji’s occurrence to exclude the later unnamed monk’s separate repetition of the headword.'
         ]}
(HERE/'promotion-rereview-five-repair-ledger.json').write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(payload,ensure_ascii=False))
