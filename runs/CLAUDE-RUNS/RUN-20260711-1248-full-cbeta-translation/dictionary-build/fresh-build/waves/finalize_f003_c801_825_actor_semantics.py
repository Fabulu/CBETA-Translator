#!/usr/bin/env python3
"""Human full-case decisions for f003 C801-825 after the heuristic repair."""
import datetime, glob, json, re, subprocess, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
NOW = datetime.datetime.now(datetime.timezone.utc).isoformat()
RUNGS = ['line','expanded-context','section-header','book-title','tei-header','parallel-passage']

def occurrences():
    for lp in glob.glob(str(ROOT/'fresh-build/waves/f003-laneC-*-research-ledger.json')):
        for e in json.load(open(lp, encoding='utf-8')).get('entries', []):
            n = int(e['ordinal'])
            if 801 <= n <= 825:
                p = ROOT/'fresh-build/entries'/e['id']/'evidence.draft.json'
                d = json.load(open(p, encoding='utf-8'))
                yield n, e, p, d, d['Entry']['Senses'][0]['Occurrences']

def clean_name(s):
    if not s: return s
    s = re.sub(r'(?:語錄|廣錄|法檀|全錄)(?:總目|目錄|目次|序|秉一|凡四)?.*$', '', s)
    s = re.sub(r'(?:總目|目錄|目次|序|法嗣|者)$', '', s)
    return s.strip('。○△▲ ') or None

def named(o, name, reason):
    o.pop('ActorAttribution', None); o['MasterName'] = name
    o['ContextMasters'] = [{'MasterName': name, 'Roles': ['utterer']}]
    o['AttributionNote'] = f"Source text: {name} utters the exact headword-bearing wording; {reason}"
    o['DraftActorProof'] = {'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':name,'SpeechFrame':reason,'FullCaseDecision':reason}

def nonmaster(o, label, role, reason, status='reviewed-unnamed', context=None):
    o.pop('MasterName', None)
    o['ActorAttribution']={'Status':status,'Kind':'compiler narrative' if status=='narrated' else ('non-human/documentary text' if status=='impersonal' else 'identified role without personal name'),'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':reason,'ReviewedBy':'Codex f003 Lane C full-case actor rereview','ReviewedUtc':NOW}
    o['ContextMasters'] = ([{'MasterName':context,'Roles':['person-described','contextual-master']}] if context else [])
    o['AttributionNote']=f"Source text: {label} owns the exact headword-bearing wording; {reason}"
    o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':reason,'FullCaseDecision':reason}

# Exact decisions that supersede the broad heuristic. Indices are 1-based occurrences.
NARR = {
  801:{1,2,3,4,5,7,8}, 802:{7}, 805:{1,2,3,4,5,6,7}, 807:{4},
  808:{1,4}, 810:{2,4}, 811:{1,2,3,4,6}, 812:{2,5}, 815:{2,5,7},
  816:{1,3,4}, 818:{3,4,5,7}, 819:{2,3}, 820:{7}, 822:{1,2,3,4,5,6},
  823:{2}, 824:{2},
}
IMPERSONAL={(811,1),(811,2),(811,3),(811,4),(816,1),(818,4),(822,1)}
QUESTIONER={(803,1),(803,4),(803,5),(804,7),(806,6),(807,3),(807,7),(808,6),(809,1),(815,1),(815,6),(817,4),(817,5),(820,5),(820,6),(824,1),(824,4),(824,6),(825,6)}
NAMED={
 (801,6):'Dahui Zonggao', (803,3):'道吾悟真禪師', (804,3):'Foyan Qingyuan',
 (804,6):'世尊', (804,8):'世尊', (806,2):'薦福懷禪師', (806,3):'薦福懷禪師', (806,4):'薦福懷禪師',
 (807,6):'天童悟徵禪師', (808,5):'湛然圓澄', (810,3):'Huangbo Xiyun',
 (811,6):'明教契嵩', (811,7):'Foyan Qingyuan', (812,4):'東山梅溪禪師',
 (813,1):'應庵曇華', (814,4):'Yuanwu Keqin', (814,5):'Sengcan', (814,6):'Sengcan', (814,7):'Sengcan',
 (816,5):'明覺聰禪師', (816,6):'蔗菴範禪師', (818,2):'Foyan Qingyuan',
 (819,4):'弘覺忞禪師', (820,1):'鳳林禪師', (820,3):'天寧琦禪師', (820,4):'明覺聰禪師',
 (821,1):'南堂欲禪師', (821,2):'佛日晳禪師', (821,5):'了庵欲禪師', (821,6):'梁山遠禪師',
 (823,1):'博山來禪師', (823,5):'雲峯悅禪師', (823,7):'道吾真禪師',
 (824,5):'承天禪師', (825,2):'Dahui Zonggao', (825,6):'Yunmen Wenyan',
}

for n,e,p,d,os_ in occurrences():
    for i,o in enumerate(os_,1):
        key=(n,i)
        # Remove record-title debris from otherwise valid direct-speaker names.
        if o.get('MasterName'):
            c=clean_name(o['MasterName'])
            if c and c != o['MasterName']:
                named(o,c,'the full case places the clause in this master’s uninterrupted address')
        if key in NAMED:
            named(o,NAMED[key],'an explicit speech frame or uninterrupted formal address governs the clause')
        elif key in QUESTIONER:
            nonmaster(o,'the interlocutor asking the recorded question','questioner','the headword occurs in the question before the master’s separately marked reply')
        elif i in NARR.get(n,set()):
            if key in IMPERSONAL:
                nonmaster(o,'the title, catalogue, or documentary heading itself','impersonal','the headword is metadata rather than a human utterance',status='impersonal')
            else:
                context=clean_name(o.get('MasterName'))
                nonmaster(o,'the compiler or recorder','compiler','the full case uses the headword in third-person narration, stage direction, verse attribution, or documentary prose rather than in the contextual master’s utterance',status='narrated',context=context)
    p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    out=p.parent/'entry.v2.json'; rep=p.parent/'compile-report.json'
    subprocess.run([sys.executable,str(ROOT/'compile_evidence_draft.py'),str(p),'--output',str(out),'--report',str(rep)],check=True)

print('finalized C801-825 full-case actor decisions')
