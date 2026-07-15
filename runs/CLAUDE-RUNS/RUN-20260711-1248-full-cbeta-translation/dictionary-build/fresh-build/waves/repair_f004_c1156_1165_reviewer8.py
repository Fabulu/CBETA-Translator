from pathlib import Path
from datetime import datetime, timezone
import hashlib, json, subprocess, sys

H=Path(__file__).resolve().parent; R=H.parent.parent
sys.path.insert(0,str(R)); import zc
NOW=datetime.now(timezone.utc).isoformat(); RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
W={x['ordinal']:x for x in json.loads((H/'f004.json').read_text(encoding='utf-8'))['entries']}

# None means a case-specific non-master/anonymous authored voice, never a fallback.
M={
1156:[None,'Shending Hongyin','Shiyu Mingfang','Zhihai Benyi','Fuyuan Fubao','Juelang Daosheng'],
1157:['Longhua Ti','Weishan Xing','Shengke','Jiean Jin','Baiyun Xianglin Zhen'],
1158:['Huangbo Qi','Tianyi Yihuai','Huangbo Qi','Tianyi Yihuai','Tianyi Yihuai','Yunmen Wenyan'],
1159:['Zhufeng Min','Yuanwu Keqin','Weilin Daopei','Xueyan Zuqin',None,'Buhui'],
1160:['Shizhu Master','Shizhu Master','Shizhu Master',None],
1161:['Ruibai Mingxue','Linquan Conglun',None,'Yezhu Fusheng','Yunsou Zhu'],
1162:[None,'Wuyi Yuanlai',None,None],
1163:['Zhuyu','Gaofeng Miao','Yuejiang Zhengyin','Gaofeng Miao','Zhuyu','Linji Yixuan'],
1164:['Huitang Zuxin','Linji Yixuan',None,'Linji Yixuan',None,'Langye Huijue',None],
1165:['Mizang Kai','Chushi Fanqi','Jirisan Master','Muzhou Daozong','Yezhu Fusheng'],
}
LABELS={
(1156,1):('the capping-verse author','verse-author'),
(1159,5):('an anonymous verse author in the Tongji collection','verse-author'),
(1160,4):('the unnamed monastic questioner','questioner'),
(1161,3):('an anonymous case-verse author in the Tongji collection','verse-author'),
(1162,1):('the unnamed monastic questioner','questioner'),(1162,3):('the unnamed monastic questioner','questioner'),(1162,4):('the unnamed monastic questioner','questioner'),
(1164,3):('the later verse commentator on Guizong','commentator'),(1164,5):('an anonymous verse author in the Tongji collection','verse-author'),(1164,7):('an anonymous author of the three-mysteries verse sequence','verse-author'),
}
CONTEXT={
(1160,4):[('Shizhu Master','respondent')],
(1162,1):[('Yungai Yongqing','respondent')],(1162,3):[('Yungai Yongqing','respondent')],(1162,4):[('Yungai Yongqing','respondent')],
(1164,3):[('Guizong Zhichang','case-figure')],(1164,5):[('Buddha','case-figure'),('Manjusri','case-figure')],
}

def named(o,name,ordinal,index):
    o['MasterName']=name; o.pop('ActorAttribution',None)
    o['ContextMasters']=[{'MasterName':name,'Roles':['utterer']}]
    note=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). {name} utters the exact headword in the complete case; reviewer8 source-by-source repair C{ordinal} occurrence {index}.'
    o['AttributionNote']=note; o['DraftActorProof']={'GrammaticalSubject':name,'SpeechFrame':note,'FullCaseDecision':note,'ExactHeadwordClause':o['Kwic']}

def other(o,label,role,ordinal,index):
    o.pop('MasterName',None)
    status='reviewed-unnamed' if label.startswith('the unnamed') else 'identified-non-master'
    note=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). {label} owns the exact headword-bearing unit; this is a case-specific full-context decision, not compilation fallback.'
    o['ActorAttribution']={'Status':status,'Kind':label,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':note,'ReviewedBy':'Codex reviewer8 repair author','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True}
    o['ContextMasters']=[{'MasterName':n,'Roles':[r]} for n,r in CONTEXT.get((ordinal,index),[])]
    o['AttributionNote']=note; o['DraftActorProof']={'GrammaticalSubject':label,'SpeechFrame':note,'FullCaseDecision':note}

# Register source-proven missing masters as temporary link keys.
pending=R/'fresh-build/pending-roster.json'; pd=json.loads(pending.read_text(encoding='utf-8')); have={x['canonicalName'] for x in pd['candidates']}
for ordinal,names in M.items():
    d=json.loads((R/'fresh-build/entries'/W[ordinal]['id']/'evidence.draft.json').read_text(encoding='utf-8'))['Entry']
    for i,name in enumerate(names,1):
        if not name or name in have: continue
        o=d['Senses'][0]['Occurrences'][i-1]
        pd['candidates'].append({'canonicalName':name,'aliases':[name],'evidence':[{'RelPath':o['RelPath'],'FromLb':o['FromLb'],'ToLb':o['ToLb'],'Kwic':o['Kwic']}],'reviewedBy':'Codex reviewer8 source repair','reviewReport':'fresh-build/waves/f004-laneC-1156-1165-reviewer8-independent.json','status':'awaiting-roster-integration'}); have.add(name)
for vals in CONTEXT.values():
    for name,_ in vals:
        if name in have: continue
        pd['candidates'].append({'canonicalName':name,'aliases':[name],'evidence':[],'reviewedBy':'Codex reviewer8 source repair','reviewReport':'fresh-build/waves/f004-laneC-1156-1165-reviewer8-independent.json','status':'awaiting-roster-integration'}); have.add(name)
# A context-only master still needs source evidence for the strict pending-roster gate.
for c in pd['candidates']:
    if c['canonicalName']=='Yungai Yongqing' and not c.get('evidence'):
        src=json.loads((R/'fresh-build/entries'/W[1162]['id']/'evidence.draft.json').read_text(encoding='utf-8'))['Entry']['Senses'][0]['Occurrences'][0]
        c['evidence']=[{'RelPath':src['RelPath'],'FromLb':src['FromLb'],'ToLb':src['ToLb'],'Kwic':src['Kwic']}]
pending.write_text(json.dumps(pd,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

out=[]
for ordinal in range(1156,1166):
    base=R/'fresh-build/entries'/W[ordinal]['id']; wp=base/'evidence.draft.json'; wrapper=json.loads(wp.read_text(encoding='utf-8')); e=wrapper['Entry']; occ=e['Senses'][0]['Occurrences']
    # Remove contained-only Zongjing lu witness before indexing mapping.
    if ordinal==1160 and occ and occ[0]['RelPath']=='T/T48/T48n2016.xml': occ.pop(0)
    for i,o in enumerate(occ,1):
        name=M[ordinal][i-1]
        if name: named(o,name,ordinal,i)
        else:
            label,role=LABELS[(ordinal,i)]; other(o,label,role,ordinal,i)
    sense=e['Senses'][0]
    sense['SourceTexts']=[o['RelPath'] for o in occ]
    sense['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(occ)+1)]
    if ordinal==1160:
        sense['Note']='Four stored witnesses contain two principal Chan deployments: three recensions of the Shizhu exchange plus one separate question-answer case; the contained-only Zongjing lu analogy was removed.'
        sense['DraftEvidence']['CounterexampleOrLimit']='Three witnesses are recensions of one Shizhu exchange, not three independent deployments; the removed Zongjing lu analogy merely contained the phrase outside a Chan encounter.'
        sense['DraftEvidence']['IndependentWorkIds']=list(dict.fromkeys(zc.work_id(o['RelPath']) for o in occ))
    if ordinal==1162:
        sense['Note']='Four storage witnesses contain two principal deployments: three recensions of the Yungai Yongqing exchange and Wuyi Yuanlai’s separate use.'
        sense['DraftEvidence']['CounterexampleOrLimit']='Three records repeat the same Yungai exchange; they count as one principal deployment, alongside Wuyi Yuanlai’s independent use.'
    wp.write_text(json.dumps(wrapper,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    p=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(wp),'--output',str(base/'entry.v2.json'),'--report',str(base/'compile-report.json')],text=True,capture_output=True)
    if p.returncode: raise SystemExit(p.stdout+p.stderr)
    chk={'ordinal':ordinal,'id':W[ordinal]['id'],'term':W[ordinal]['term'],'state':'reviewer8-repair-compiled-awaiting-pre-review','occurrences':len(occ),'entrySha256':hashlib.sha256((base/'entry.v2.json').read_bytes()).hexdigest(),'defaultActorLabels':0,'selfReview':False,'promoted':False}
    (H/f'f004-laneC-{ordinal}-reviewer8-repair-checkpoint.json').write_text(json.dumps(chk,ensure_ascii=False,indent=2)+'\n',encoding='utf-8'); out.append(chk)
(H/'f004-laneC-1156-1165-reviewer8-repair-ledger.json').write_text(json.dumps({'schemaVersion':1,'generatedUtc':NOW,'state':'compiled-awaiting-pre-review','entries':out,'occurrences':sum(x['occurrences'] for x in out),'defaultActorLabels':0,'selfReview':False,'promoted':False},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'entries':len(out),'occurrences':sum(x['occurrences'] for x in out)},ensure_ascii=False))
