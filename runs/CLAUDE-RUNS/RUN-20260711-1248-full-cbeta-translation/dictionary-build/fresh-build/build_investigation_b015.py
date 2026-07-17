#!/usr/bin/env python3
"""Explicit evidence-first article for Lane B position 015: 心地印."""
import datetime, json, subprocess, sys
from pathlib import Path

DB = Path(__file__).resolve().parent.parent
ROOT = DB / 'fresh-build'
sys.path.insert(0, str(DB))
import zc

TERM = '心地印'
BASE = '42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a'
NOW = datetime.datetime.now(datetime.timezone.utc).isoformat()
M = json.loads((DB / 'maintenance/investigation-next300-construction-lane-b.json').read_text())
ID = next(x['id'] for x in M['rows'] if x['headword'] == TERM)

# Each row is an independently read deployment, not a concordance sample.
ROWS = [
    {
        'rel':'X/X71/X71n1414.xml', 'work':'work:X71n1414',
        'kwic':'拈拄杖云：不是心地印', 'label':"Recorded Sayings of Chan Master Liao'an Qingyu",
        'master':"Liao'an Qingyu", 'contexts':[],
        'decision':"Liao'an Qingyu raises his staff and explicitly denies that it is the mind-ground seal; he then likewise denies that striking it is the ancestral gate."
    },
    {
        'rel':'C/C077/C077n1710.xml', 'work':'work:guzunsu-yulu',
        'kwic':'師云喫茶時不是心地印', 'label':'Record of Chan Master Yunmen Kuangzhen',
        'master':'Yunmen Wenyan', 'contexts':[{'MasterName':'Yongjia Xuanjue','Roles':['verse-author']}],
        'decision':'After raising a line from Yongjia, Yunmen Wenyan says that while drinking tea it is not the mind-ground seal, then raises his staff and directs the assembly there.'
    },
    {
        'rel':'L/L158/L158n1652.xml', 'work':'chan:mingjue-cong-yulu',
        'kwic':'且如何是心地印良久云狗子尾巴書卍字野狐窟宅梵王宮', 'label':'Recorded Sayings of Chan Master Mingjue Cong',
        'master':'Mingjue Cong', 'contexts':[],
        'decision':'Mingjue Cong asks what the mind-ground seal is, pauses, and answers with a dog-tail drawing a swastika and a fox den as Brahma palace.'
    },
    {
        'rel':'X/X69/X69n1356.xml', 'work':'work:X69n1356',
        'kwic':'八解六通心地印。說什麼三千大千世界，山河大地，有情無情，一印印定', 'label':"Recorded Sayings of Chan Master Pu'an Yinsu",
        'master':'Yongjia Xuanjue', 'contexts':[{'MasterName':"Pu'an Yinsu",'Roles':['commentator']}],
        'decision':"Pu'an Yinsu quotes Yongjia Xuanjue's line, then says that mountains, rivers, lands, and beings and nonbeings are fixed by one seal; Yongjia Xuanjue is the utterer of the headword and Pu'an supplies the deployment."
    },
    {
        'rel':'X/X69/X69n1356.xml', 'work':'work:X69n1356',
        'kwic':'八解六通心地印，晃晃󳭪󳭪非遠近', 'label':"Recorded Sayings of Chan Master Pu'an Yinsu",
        'master':"Pu'an Yinsu", 'contexts':[{'MasterName':'Yongjia Xuanjue','Roles':['verse-author']}],
        'decision':"In his verse on Yongjia's song, Pu'an Yinsu calls the mind-ground seal shining and not a matter of far or near."
    },
    {
        'rel':'T/T48/T48n2014.xml', 'work':'work:T48n2014',
        'kwic':'八解六通心地印', 'label':'Song of Realizing the Way',
        'master':'Yongjia Xuanjue', 'contexts':[],
        'decision':'Yongjia Xuanjue places the mind-ground seal in the compact verse line alongside the eight liberations and six powers.'
    },
    {
        'rel':'T/T47/T47n1998A.xml', 'work':'work:T47n1998A',
        'kwic':'佛性戒珠心地印。霧露雲霞體上衣', 'label':'Recorded Sayings of Chan Master Dahui Pujue',
        'master':'Yongjia Xuanjue', 'contexts':[{'MasterName':'Dahui Zonggao','Roles':['commentator']}],
        'decision':'Dahui Zonggao raises Yongjia Xuanjue’s line and caps it critically: Yongjia deserves thirty blows for putting communal property under his robe and bowl.'
    },
]

def main():
    occ = []
    for row in ROWS:
        v = zc.verify(row['rel'], row['kwic'])
        assert v.get('ok'), (row['rel'], row['kwic'], v)
        contexts = [{'MasterName':row['master'], 'Roles':['utterer']}] + row['contexts']
        occ.append({
            'RelPath':row['rel'], 'FromLb':v['fromLb'], 'ToLb':v['toLb'], 'Kwic':row['kwic'],
            'MasterName':row['master'], 'Curated':True,
            'AttributionNote':f"Source record ({row['rel']}). {row['label']}: {row['decision']}",
            'ContextMasters':contexts,
            'DraftActorProof':{
                'ExactHeadwordClause':row['kwic'], 'GrammaticalSubject':row['master'],
                'SpeechFrame':row['decision'], 'FullCaseDecision':row['decision']
            }
        })
    sense = {
        'SenseKey':None, 'MasterName':None,
        'PreferredTarget':'the mind-ground seal',
        'AlternateTargets':['the seal of the ground of mind'],
        'SearchAliases':['mind-ground seal','mind seal','seal of mind','heart-ground seal'],
        'Status':'preferred', 'Validation':'multi-source',
        'Note':'Fresh concordance: 151 exact hits in 92 files representing 90 works. Seven read deployments across six works preserve the verse source, affirmative expansion, direct question, staff denials, and critical capping.',
        'Occurrences':occ, 'ClaimAnchors':[],
        'SourceTexts':list(dict.fromkeys(row['rel'] for row in ROWS)),
        'RelatedMasters':["Liao'an Qingyu",'Yunmen Wenyan','Mingjue Cong',"Pu'an Yinsu",'Yongjia Xuanjue','Dahui Zonggao'],
        'RelatedTerms':['祖師關','心印','拄杖'],
        'ExplanationParts':{
            'CorpusEarnedOpening':'The mind-ground seal is the stamp by which the ground of mind is made evident and decisively marked. In Chan records it is never left as an ornamental phrase: masters ask what it is, point with a staff and deny that the staff is it, or use the phrase as a line to cap and overturn.',
            'EvidenceBody':[
                "Yongjia Xuanjue's verse is the recurrent source. Pu'an Yinsu expands its sealing image: mountains, rivers, lands, sentient beings, and the nonsentient are fixed by one seal; in his own verse he calls it shining and not a matter of distance.",
                "The records refuse to turn that explanation into an object. Liao'an Qingyu raises his staff and says it is not the mind-ground seal. Yunmen says that drinking tea is not it, then raises his staff and tells the assembly to understand there.",
                'Mingjue Cong makes the phrase a public question—what is the mind-ground seal?—and answers with a dog tail drawing a swastika and a fox den as Brahma palace. The answer displays the seal through a deliberately dislocating image instead of defining a substance.',
                "Dahui raises Yongjia's line only to sentence its author to thirty blows for putting communal property under his robe and bowl. This critical capping prevents the verse source from becoming a protected formula.",
                'The corpus supports one thing: a seal or decisive stamp of the ground of mind. Its verse, question, gesture, denial, and capping uses are different deployments of that referent, not separate senses.'
            ]
        },
        'DraftEvidence':{
            'OpeningClaimEvidenceKeys':['o1','o2','o3','o4','o5','o6','o7'],
            'ZenBend':'A compact verse image becomes a live public instrument: masters demand its referent, point and deny, or cap the inherited line with blows.',
            'CounterexampleOrLimit':'The staff passages explicitly block identification with an implement, while Dahui blocks reverence for the inherited wording itself.',
            'DifferentThingTest':{'Decision':'one-thing','ComparedThings':['verse phrase','public question','staff denial','critical capping'],'Reason':'These are deployments of one sealing image; none establishes a second concrete or titular referent.'},
            'AliasRationale':'Mind-ground seal and seal of mind preserve the seal image while making English lookup possible.',
            'ModifierControls':[{'finding':'checked','reason':'心地 modifies 印 as the domain or ground marked by the seal; the corpus does not make it a material seal.'}],
            'FamilyControls':[{'finding':'checked','reason':'心印 is a close search relative; 祖師關 is paired in a recurrent formula but names the gate to be passed, not the seal itself.'}],
            'IndependentWorkIds':list(dict.fromkeys(row['work'] for row in ROWS))
        }
    }
    data={'SchemaVersion':1,'Entry':{'Id':ID,'SourceTerm':TERM,'CorpusBaselineSha256':BASE,'CreatedBy':'Codex investigation-next300 Lane B explicit author','WrittenUtc':NOW,'Senses':[sense]}}
    out=ROOT/'entries'/ID; out.mkdir(parents=True,exist_ok=True)
    draft=out/'evidence.draft.json'; draft.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n')
    (out/'WORK.md').write_text('''# 心地印 — construction Lane B position 015

- 151 hits / 90 works; seven read deployments across six independent works.
- Source verse, affirmative expansion, public question, staff denials, and Dahui's hostile capping were all retained.
- Different-thing test: one sealing image under several conversational jobs; no material-seal or title sense found.

feedback-inference-verdict: the mind-ground seal is a decisive stamp by which the ground of mind is made evident, deployed through question, gesture, denial, and capping.
feedback-observations: one seal seals the world; not the staff; not tea-drinking; dog-tail answer; thirty-blow capping.
feedback-falsification-searches: material seal; title/person use; protected-scripture reading; 心印 comparison; 祖師關 pairing.
feedback-counterexamples: Liao'an and Yunmen deny object-identification; Dahui attacks the inherited verse line.
feedback-scope: corpus-wide phrase with a recurring Yongjia source.
lookup-probes: mind-ground seal; seal of mind; heart-ground seal.
opening-interpretation-verdict: sealing predicates and the repeated public test license “decisive stamp”; the denials prevent reification.
sense-target-distinguishability: one referent, not noun/verb or inherited/live-use splits.
''')
    p=subprocess.run([sys.executable,str(DB/'compile_evidence_draft.py'),str(draft),'--output',str(out/'entry.v2.json'),'--report',str(out/'evidence-compile-report.json')],text=True,capture_output=True)
    assert p.returncode == 0, p.stdout+p.stderr

if __name__ == '__main__':
    main()
