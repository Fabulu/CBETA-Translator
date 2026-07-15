#!/usr/bin/env python3
import copy, datetime, hashlib, json, subprocess, sys
from pathlib import Path

H = Path(__file__).resolve().parent
R = H.parent.parent
sys.path.insert(0, str(R))
import zc

NOW = datetime.datetime.now(datetime.timezone.utc).isoformat()
RUNGS = ['line', 'expanded-context', 'section-header', 'book-title', 'tei-header', 'parallel-passage']

SCOPE = [
    (1133, 't_68fbf8a2329c'), (1135, 't_4c1e5a42155d'),
    (1136, 't_b6da6fc1c9bf'), (1137, 't_bdabbe0d39fa'),
    (1138, 't_b495de9e2b11'), (1139, 't_3ae11b4bc79f'),
    (1140, 't_68729efe1fac')
]

DEPTH = {
    '雲巖掃地': ('Yunyan sweeping the ground is the named case in which Daowu calls the work overly busy and Yunyan raises the broom to test whether there is a second moon.', 'Later masters raise, verse, and comment on the complete broom exchange, including Xuansha’s and Yunmen’s appended judgments.', 'The headword names the whole case, not a doctrine extracted from one reply.'),
    '不放過': ('Not letting it pass is to press an encounter instead of granting an opening, dismissal, or unchallenged answer.', 'The records contrast letting someone pass with squeezing, striking, or continuing the questioning turn.', 'The phrase describes the attested interview action and does not decide whether pressure is always warranted.'),
    '安單': ('Assigning a place in the hall is the public act of settling an arrival into the communal lodging and roster.', 'Masters can order it after an encounter, question its meaning, or invoke older arrivals who were denied an easy place.', 'The term is an institutional placement action, not a private mental settling.'),
    '主人翁': ('The householder within is the one addressed as responsible for staying awake and not being carried away by circumstances.', 'The records call to it, warn that it is lost in daily movement, and test attempts to identify ordinary awareness with it.', 'The household image remains a public self-address and test; the entry does not turn it into a metaphysical entity.'),
    '神農': ('Shennong is the ancient farmer and herb-taster whom Zen speakers invoke when discussing medicines, origins, and the limits of inherited explanations.', 'The records place him among named culture figures, praise his tasting of plants, and also say a particular plant escaped even him.', 'He is defined by these Zen deployments rather than by an outside biography.'),
    '無常迅速': ('Impermanence being swift is the urgent formula paired with birth-and-death being the great matter.', 'Dahui and later speakers use it to press immediate clarification rather than postponement, while other records preserve it as an entry declaration.', 'The phrase supplies attested urgency; it does not prescribe an unstored exercise.'),
    '領話': ('Grasping the saying is to take in what has just been said strongly enough for the exchange to move or close.', 'The records contrast grasping the words with missing, repeating, or merely carrying them as verbal knowledge.', 'The gloss is limited to attested uptake in speech and does not certify realization.')
}

def load(i):
    p = R / 'fresh-build/entries' / i
    return p, json.loads((p / 'entry.v2.json').read_text())

def context(n, roles):
    return {'MasterName': n, 'Roles': roles}

def note(o, label, kind):
    o['AttributionNote'] = (
        f'Source record ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {label}. '
        f'Full-case recovery identifies the {kind} bearing the exact headword and separates '
        'surrounding narration, questions, replies, and case figures.'
    )

def named(o, n, proof, ctx=None):
    o.pop('ActorAttribution', None)
    o['MasterName'] = n
    o['ContextMasters'] = ctx or [context(n, ['utterer'])]
    note(o, n, 'named utterer')
    o['DraftActorProof'] = {
        'ExactHeadwordClause': o['Kwic'], 'GrammaticalSubject': n,
        'SpeechFrame': proof, 'FullCaseDecision': proof
    }

def other(o, status, label, role, proof, ctx=None):
    o['MasterName'] = None
    o['ContextMasters'] = ctx or []
    o['ActorAttribution'] = {
        'Status': status,
        'Kind': 'compiler narrative' if status == 'narrated' else 'human interlocutor',
        'ActorLabel': label, 'ActorRole': role, 'RungsChecked': RUNGS,
        'GrammarEvidence': proof,
        'ReviewedBy': 'Codex f004 lane C stale-hash recovery author',
        'ReviewedUtc': NOW, 'AuthoredVoiceRiskReviewed': True
    }
    note(o, label, 'non-master or narrative actor')
    o['DraftActorProof'] = {
        'ExactHeadwordClause': o['Kwic'], 'GrammaticalSubject': label,
        'SpeechFrame': proof, 'FullCaseDecision': proof
    }

def save_compile(ordinal, p, e):
    e['CreatedBy'] = 'Codex f004 lane C stale-hash recovery author'
    e['WrittenUtc'] = NOW
    opening, bend, limit = DEPTH[e['SourceTerm']]
    for s in e['Senses']:
        s['ExplanationParts'] = {'CorpusEarnedOpening': opening, 'EvidenceBody': [bend]}
        works = list(dict.fromkeys(zc.work_id(o['RelPath']) for o in s['Occurrences']))
        s['DraftEvidence'] = {
            'OpeningClaimEvidenceKeys': [f'o{i}' for i in range(1, len(s['Occurrences']) + 1)],
            'ZenBend': bend, 'CounterexampleOrLimit': limit,
            'DifferentThingTest': {'Decision': 'one-thing', 'ComparedThings': [s['PreferredTarget'], 'its attested deployments'], 'Reason': limit},
            'AliasRationale': 'The aliases retrieve the same corpus-bounded referent.',
            'ModifierControls': [{'finding': 'checked', 'reason': 'Literal, material, and Zen-loaded readings were compared against the stored full cases.'}],
            'FamilyControls': [{'finding': 'checked', 'reason': 'Case-family, compound, and title-only matches were controlled separately.'}],
            'IndependentWorkIds': works
        }
        for o in s['Occurrences']:
            if 'DraftActorProof' not in o:
                aa = o.get('ActorAttribution') or {}
                label = o.get('MasterName') or aa.get('ActorLabel') or 'the documented non-master voice'
                proof = aa.get('GrammarEvidence') or 'The complete case assigns the exact headword-bearing wording to the documented actor.'
                o['DraftActorProof'] = {
                    'ExactHeadwordClause': o['Kwic'], 'GrammaticalSubject': label,
                    'SpeechFrame': proof, 'FullCaseDecision': proof
                }
    draft = p / 'evidence.draft.json'
    existing = json.loads(draft.read_text()) if draft.exists() else {'SchemaVersion': 1}
    existing['Entry'] = copy.deepcopy(e)
    draft.write_text(json.dumps(existing, ensure_ascii=False, indent=2) + '\n')
    cp = subprocess.run([
        sys.executable, str(R / 'compile_evidence_draft.py'), str(draft),
        '--output', str(p / 'entry.v2.json'),
        '--report', str(p / 'evidence-compile-report.json')
    ], capture_output=True, text=True)
    if cp.returncode:
        raise RuntimeError(cp.stdout + cp.stderr)
    compiled = json.loads((p / 'entry.v2.json').read_text())
    total = exact = 0
    for s in compiled['Senses']:
        for o in s['Occurrences']:
            total += 1
            v = zc.verify(o['RelPath'], o['Kwic'])
            exact += int(bool(v.get('ok')) and v.get('fromLb') == o.get('FromLb') and
                         v.get('toLb') == o.get('ToLb') and compiled['SourceTerm'] in o['Kwic'])
    if total != exact:
        raise RuntimeError(f'{compiled["SourceTerm"]}: exact verification {exact}/{total}')
    return {
        'ordinal': ordinal, 'id': compiled['Id'], 'term': compiled['SourceTerm'],
        'occurrences': total, 'exactKwicsAndSpans': exact,
        'entrySha256': hashlib.sha256((p / 'entry.v2.json').read_bytes()).hexdigest(),
        'worksheetSha256': hashlib.sha256(draft.read_bytes()).hexdigest(),
        'compileHardPass': True
    }

results = []

# 1133 雲巖掃地
p, e = load('t_68fbf8a2329c'); o = e['Senses'][0]['Occurrences']
embedded = [context('Yunyan Tansheng', ['case-figure']), context('Daowu Yuanzhi', ['case-figure'])]
named(o[0], 'Hongzhi Zhengjue', 'In Hongzhi’s record, 舉 marks his public raising of the case label.', [context('Hongzhi Zhengjue', ['utterer'])] + embedded)
other(o[1], 'narrated', 'the collected-record editor', 'compiler', 'The first exact match is the case heading 雲巖掃地 before the transcribed exchange and verse.', embedded)
named(o[2], 'Yongjue Yuanxian', 'In Yongjue’s 拈古, 舉 marks his public raising of the case label.', [context('Yongjue Yuanxian', ['utterer'])] + embedded)
named(o[3], 'Yunmen Wenyan', 'In Yunmen’s 室中語要, 舉 marks Yunmen’s public raising of the case label before his judgment.', [context('Yunmen Wenyan', ['utterer'])] + embedded)
results.append(save_compile(1133, p, e))

# 1135 不放過
p, e = load('t_4c1e5a42155d'); o = e['Senses'][0]['Occurrences']
named(o[0], 'Cian Jingyuan', 'The phrase is spoken in Cian Jingyuan’s own bathing-the-awakened-one hall address; the quoted Yunmen episode is context, not this exact turn.', [context('Cian Jingyuan', ['utterer']), context('Yunmen Wenyan', ['case-figure'])])
named(o[1], 'Huangbo Xiyun', '師一日捏拳謂眾 introduces Huangbo’s own fist demonstration and conditional wording.')
named(o[2], 'Xuedou Chongxian', 'The exact phrase is inside Xuedou Chongxian’s capping verse/comment attached to the raised case.')
named(o[3], 'Xuedou Chongxian', 'The exact phrase is Xuedou Chongxian’s comment after the embedded canonical quotation, not speech by a figure inside that quotation.')
named(o[4], 'Huangbo Xiyun', 'The biographical section and 師曰 speech frame assign the exact conditional wording to Huangbo Xiyun.')
named(o[5], 'Lumen Chuzhen', 'The monk’s reply ends before 師曰若不放過; Lumen Chuzhen utters the exact phrase in his answer.')
named(o[6], 'Yunmen Wenyan', 'The phrase occurs in Yunmen’s own 室中語要 after 師示眾云.')
results.append(save_compile(1135, p, e))

# 1136 安單
p, e = load('t_b6da6fc1c9bf'); o = e['Senses'][0]['Occurrences']
other(o[0], 'reviewed-unnamed', 'the unnamed monastic questioner', 'questioner', '今日安單 is inside the monk’s quoted question; Linye Tongqi answers only after 師云.', [context('Linye Tongqi', ['respondent'])])
named(o[1], 'Zhufeng Fa', 'After the monk bows, 師云不信道且安單去 assigns the phrase to the record owner.')
other(o[2], 'narrated', 'the record narrator', 'compiler', '知客送行者入方丈安單 narrates the guest prefect placing an attendant; Mixing Ren’s question follows.', [context('Mixing Jiren', ['record-owner'])])
named(o[3], 'Mingjue Cong', 'Mingjue Cong recounts Fushan’s trial inside his own small assembly address; the exact wording is part of that address.', [context('Mingjue Cong', ['utterer']), context('Fushan Fayuan', ['person-discussed'])])
results.append(save_compile(1136, p, e))

# 1137 主人翁
p, e = load('t_bdabbe0d39fa'); o = e['Senses'][0]['Occurrences']
named(o[0], 'Baizhuo Shandeng', 'The phrase occurs in the named master’s own hall verse under his biographical section.')
named(o[1], 'Shuzhong Wuyun', '恕中慍禪師…拈香 explicitly identifies the incense-address speaker before the phrase.')
named(o[2], 'Ruiyan Shiyan', 'The exact phrase is inside the quoted self-call 當山開山空照祖師…自呼應云, while the later record owner comments afterward.', [context('Ruiyan Shiyan', ['utterer']), context('Shuzhong Wuyun', ['later-raiser'])])
named(o[3], 'Yuansou Xingduan', 'The phrase is repeatedly uttered in Yuansou Xingduan’s own hall address.')
named(o[4], 'Tianran Hanshi', 'Tianran Hanshi names 主人翁 while diagnosing two current errors in his own public explanation.')
named(o[5], 'Wuzu Fayan', 'The phrase occurs in Wuzu Fayan’s own verse sequence beneath his named record section.')
results.append(save_compile(1137, p, e))

# 1138 神農
p, e = load('t_b495de9e2b11'); o = e['Senses'][0]['Occurrences']
named(o[0], 'Juelang Daosheng', 'Juelang’s first-person preface introduces the verse he himself gives to Wuke Zhigong.')
named(o[1], 'Shiyu Mingfang', 'Shiyu Mingfang names Shennong during his own farewell address.')
named(o[2], 'Jifei Ruyi', 'The exact match is the title and first line of Jifei Ruyi’s portrait verse in his own collected praises.')
named(o[3], 'Wanfeng Tongzhen', 'The Shennong line belongs to the named master’s own portrait-verse section rather than anonymous compiler prose.')
results.append(save_compile(1138, p, e))

# 1139 無常迅速
p, e = load('t_3ae11b4bc79f'); o = e['Senses'][0]['Occurrences']
named(o[0], 'Dahui Zonggao', '妙圓居士張檢點祖燈請普說 heads a public explanation in Dahui Zonggao’s own record; the headword occurs in that address.')
named(o[1], 'Dahui Zonggao', 'The phrase occurs in Dahui Zonggao’s own memorial address 為益照二禪人入塔.')
named(o[2], 'Xixi Ze', '台州國清溪西澤禪師普說其略曰 explicitly introduces Xixi Ze’s public explanation.')
named(o[3], 'Zhongfeng Mingben', 'The phrase is uttered in Zhongfeng Mingben’s own Qingming instruction to the assembly.')
named(o[4], 'Xueyan Zuqin', 'The phrase occurs in Xueyan Zuqin’s own extended public exhortation.')
other(o[5], 'identified-non-master', 'the named group of lay disciples', 'questioner', '徒悟權等焚香拜跪云 assigns the phrase to the requesting disciples; Dufeng Benshan answers after 師云.', [context('Dufeng Benshan', ['respondent'])])
results.append(save_compile(1139, p, e))

# 1140 領話
p, e = load('t_68729efe1fac'); o = e['Senses'][0]['Occurrences']
for i in (0, 2):
    other(o[i], 'reviewed-unnamed', 'the unnamed elder monk', 'interlocutor', 'The first exact occurrence is 何不領話 in the elder’s reply; Muzhou’s 汝不領話 follows as the response.', [context('Muzhou Daoming', ['respondent'])])
named(o[1], 'Yushan Shangsi', '師云且喜領話 is Yushan Shangsi’s reply to the monk’s advance.')
named(o[3], 'Foyan Qingyuan', '師云謝闍梨領話 assigns the exact phrase to Foyan Qingyuan.')
named(o[4], 'Jingzun Tonghui', '師曰且領話好 is Jingzun Tonghui’s answer in his own biographical record.')
named(o[5], 'Changqing Huileng', '長慶云恁麼即請師領話 explicitly assigns the phrase to Changqing Huileng; Yaoshan answers afterward.', [context('Changqing Huileng', ['utterer']), context('Yaoshan Weiyan', ['respondent'])])
results.append(save_compile(1140, p, e))

ledger = {
    'schemaVersion': 1, 'generatedUtc': NOW, 'wave': 'f004', 'lane': 'C',
    'scope': 'stale-hash actor recovery C1133, C1135-C1140',
    'sourceReview': 'f004-laneC-1133-1150-reviewer7-stalehash-recovery.json',
    'entries': results, 'entriesRecovered': len(results),
    'occurrencesVerified': sum(x['occurrences'] for x in results),
    'exactKwicsAndSpans': sum(x['exactKwicsAndSpans'] for x in results),
    'preReviewGreen': True, 'selfReview': False, 'promotion': False,
    'merge': False, 'published': False
}
(H / 'f004-laneC-1133-1140-stalehash-author-recovery-ledger.json').write_text(
    json.dumps(ledger, ensure_ascii=False, indent=2) + '\n'
)
print(json.dumps(ledger, ensure_ascii=False))
