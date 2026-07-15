#!/usr/bin/env python3
import copy, datetime, json, subprocess, sys
from pathlib import Path

R = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(R))
import zc

REVIEW = json.loads((R / 'fresh-build/waves/f003-laneB-751-800-fresh-independent-exact-review.json').read_text())
ROWS = {r['ordinal']: r for r in REVIEW['rows']}
TARGETS = [764]
RUNGS = ['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
ROLES = {'utterer','respondent','questioner','interlocutor','addressee','section-subject','record-owner','person-described','person-discussed','commentator','later-raiser','later-quoter','teacher','student','compiler','verse-author','case-figure'}
NOW = datetime.datetime.now(datetime.timezone.utc).isoformat()

def clean(o):
    for k in ('MasterName','ActorAttribution','DraftActorProof'): o.pop(k, None)

def named(o, name, contexts=(), proof=None):
    clean(o)
    proof = proof or f'The complete speech frame assigns the exact headword to {name}.'
    o['MasterName'] = name
    cms = [{'MasterName': name, 'Roles': ['utterer']}]
    cms += [{'MasterName': n, 'Roles': [r]} for n,r in contexts if n and n != name]
    o['ContextMasters'] = cms
    o['AttributionNote'] = f'Source text ({zc.title(o["RelPath"])}): {name} utters the exact headword. {proof}'
    o['DraftActorProof'] = {'ExactHeadwordClause': o['Kwic'], 'GrammaticalSubject': name, 'SpeechFrame': proof, 'FullCaseDecision': proof}

def exc(o, status, label, kind, role, grammar, contexts=()):
    clean(o); assert role in ROLES
    o['ActorAttribution'] = {'Status': status, 'Kind': kind, 'ActorLabel': label, 'ActorRole': role,
      'RungsChecked': RUNGS, 'ReviewedBy': 'Codex fresh B751-800 repair author', 'ReviewedUtc': NOW,
      'GrammarEvidence': grammar}
    o['ContextMasters'] = [{'MasterName': n, 'Roles': [r]} for n,r in contexts if n]
    o['AttributionNote'] = f'Source text ({zc.title(o["RelPath"])}): {label}. {grammar}'
    o['DraftActorProof'] = {'ExactHeadwordClause': o['Kwic'], 'GrammaticalSubject': label, 'SpeechFrame': grammar, 'FullCaseDecision': grammar}

def narrated(o, label, contexts=(), grammar='The compiler governs the exact headword clause; the nearby master does not utter it.'):
    exc(o, 'narrated', label, 'compiler narrative', 'compiler', grammar, contexts)

def questioner(o, respondent=None):
    ctx = [(respondent,'respondent')] if respondent else []
    exc(o, 'reviewed-unnamed', 'the unnamed monastic questioner', 'monastic questioner', 'questioner',
        'The explicit question frame assigns the exact headword to the unnamed monk before the separately marked response.', ctx)

def identified(o, label, grammar):
    exc(o, 'identified-non-master', label, 'identified preface author', 'compiler', grammar)

def refresh(E):
    for s in E['Senses']:
        s['SourceTexts'] = sorted({o['RelPath'] for o in s['Occurrences']})
        s['RelatedMasters'] = sorted({o['MasterName'] for o in s['Occurrences'] if o.get('MasterName')} | {c['MasterName'] for o in s['Occurrences'] for c in o.get('ContextMasters',[])})
        s['DraftEvidence']['OpeningClaimEvidenceKeys'] = [f'o{i}' for i in range(1,len(s['Occurrences'])+1)]
        s['DraftEvidence']['IndependentWorkIds'] = sorted({zc.work_id(o['RelPath']) for o in s['Occurrences']})

for n in TARGETS:
    row = ROWS[n]; d = R/'fresh-build/entries'/row['id']; p=d/'evidence.draft.json'
    x=json.loads(p.read_text()); E=x['Entry']
    def O(si,oi): return E['Senses'][si-1]['Occurrences'][oi-1]
    if n == 751:
        narrated(O(1,4), 'the encounter narrator', [('Nanquan Puyuan','respondent')], 'The narrator says Wenyuan was bowing in the Buddha hall; Nanquan speaks only in the following marked 師曰 turn.')
    elif n == 752:
        named(O(1,3), 'Yuanwu Keqin', proof='The occurrence lies in Yuanwu Keqin’s continuous court-requested hall address; the compilation transmits rather than authors the wording.')
        identified(O(1,5), 'Zhao Yuan, the preface author', 'The signed preface (東海學人興翱趙㟲謹識) owns the statement about Medicine-Earth; the discussed master does not utter it.')
    elif n == 754:
        named(O(1,2), 'Langshan Yu', proof='The inline marker 浪山嶼云 assigns the exact judgment to Langshan Yu.')
        named(O(1,7), 'Baozhang Bai', proof='The inline marker 寶掌白云 assigns the exact judgment to Baozhang Bai.')
    elif n == 756:
        named(O(1,4), 'Cian Jingyuan', proof='This is the compilation’s parallel witness of Cian Jingyuan’s same 南明 hall address, not compiler prose.')
    elif n == 757:
        named(O(1,6), 'Huangbo Xiyun', [('Baizhang Huaihai','respondent')], 'Inside Huangbo Xiyun’s record, 師問百丈 marks Huangbo as questioner and Baizhang as respondent.')
    elif n == 759:
        named(O(1,3), 'Hongzhi Zhengjue', proof='T48n2001 is Hongzhi Zhengjue’s Extensive Record; the small address belongs to Hongzhi, not Dahui.')
    elif n == 760:
        base=E['Senses'][0]; topic_occ=base['Occurrences'].pop(3)
        second=copy.deepcopy(base)
        second['PreferredTarget']='Chan, as the topic'
        second['AlternateTargets']=['as for Chan']
        second['SearchAliases']=['Chan as topic','as for Chan','Chan itself']
        second['Validation']='provisional'
        second['Note']='One exact witness uses 者 as a topic marker after 禪 rather than as the person-forming suffix.'
        second['Occurrences']=[topic_occ]
        second['ExplanationParts']={'CorpusEarnedOpening':'In 故禪者, the final graph marks Chan itself as the topic: “as for Chan.”','EvidenceBody':['Yanshen follows it with 誠去執之虛名, speaking about the name Chan rather than identifying a Chan practitioner. This different referent is kept separate from the person label.']}
        second['DraftEvidence']['CounterexampleOrLimit']='This topical construction has one stored witness and remains provisional; it is not projected onto person-label occurrences.'
        E['Senses'].append(second)
        base['ExplanationParts']['EvidenceBody']=['The person label names students, visitors, or addressees involved in Chan; it excludes the distinct topical construction 故禪者, where the phrase means “as for Chan.”']
        base['DraftEvidence']['DifferentThingTest']={'Decision':'split','ComparedThings':['a person identified as a Chan student','Chan itself introduced as a topic'],'Reason':'The first denotes a human participant; the second topicalizes Chan and denotes no person.'}
    elif n == 761:
        questioner(O(1,9), 'Shoushan Shengnian')
    elif n == 762:
        old=E['Senses'][0]; occs=old['Occurrences']
        named(occs[3], 'Linji Yixuan', proof='Foyan quotes Linji’s own 一與山門作境致; the embedded quoted actor, not Foyan, owns the clause.')
        narrated(occs[6], 'the lamp-record biographer', [('Guangsheng Shihu','person-described')], 'The biographer says the monastery elders wrote names for the examination; Guangsheng does not utter 山門.')
        questioner(occs[7], 'Yuejiang Zhengyin')
        physical=[occs[i] for i in (2,5,7)]
        institution=[occs[i] for i in (1,3,4,6)]
        lineage=[occs[0]]
        old['PreferredTarget']='the monastery gate'; old['AlternateTargets']=['monastery entrance']; old['Occurrences']=physical
        old['ExplanationParts']={'CorpusEarnedOpening':'The monastery gate is the physical entrance to the monastic compound.','EvidenceBody':['The records place burials beside it, describe someone outside it, and personify the gate as having no mouth.']}
        old['DraftEvidence']['DifferentThingTest']={'Decision':'split','ComparedThings':['physical gate','monastery or community','Dongshan lineage'],'Reason':'A structure, an institution/community, and a teaching lineage are different referents.'}
        s2=copy.deepcopy(old); s2['PreferredTarget']='the monastery or monastic community'; s2['AlternateTargets']=['the monastery']; s2['SearchAliases']=['monastery','monastic community','monastery establishment']; s2['Occurrences']=institution
        s2['ExplanationParts']={'CorpusEarnedOpening':'As an institutional term, the monastery gate names the monastery or its resident community rather than the entrance structure.','EvidenceBody':['Codes speak of monastery hospitality, records credit work that benefits the monastery, and biographies call its senior residents 山門老宿.']}
        s3=copy.deepcopy(old); s3['PreferredTarget']='the Dongshan lineage'; s3['AlternateTargets']=['Dongshan’s school']; s3['SearchAliases']=['Dongshan lineage','Dongshan school','Dongshan house']; s3['Validation']='provisional'; s3['Occurrences']=lineage
        s3['ExplanationParts']={'CorpusEarnedOpening':'In 洞山門下, the “gate” is Dongshan’s lineage or house, not a building entrance.','EvidenceBody':['Zhenjing Kewen contrasts how people act “under Dongshan’s gate,” using the compound as lineage affiliation.']}
        E['Senses']=[old,s2,s3]
    elif n == 764:
        named(O(1,3), 'Hongzhi Zhengjue', proof='The inline 正覺云 introduces Hongzhi Zhengjue’s exact comment and question.')
        named(O(1,5), 'Xuedou Chongxian', proof='The headword sits inside Xuedou Chongxian’s quoted comment ending with 擲拂子，下座, before Nantang Yu’s separately marked response.')
    refresh(E)
    p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n')
    subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True)
print(json.dumps({'repaired':TARGETS,'count':len(TARGETS)},ensure_ascii=False))
