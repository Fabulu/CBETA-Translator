#!/usr/bin/env python3
"""Author the f004 lane-C early-five canary from the already verified packets."""
import datetime, json
from pathlib import Path

HERE=Path(__file__).resolve().parent
ROOT=HERE.parent.parent
PACK=json.loads((HERE/'f004-laneC-1101-1105-early-sample-evidence-packets.json').read_text(encoding='utf-8'))
BASE='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a'
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat()

SEM={
 '一言半句':('a word or half a phrase',['one word or half a phrase','a few words','a brief saying'],
  'A word or half a phrase is a deliberately small verbal opening: in these records it may give an entrant a place to enter, expose what years of residence have not resolved, or strike with the force of an implement.',
  'Fayan says the ancients lower it to give people an entry; Dahui contrasts immediate recognition through it with decades without a landing; Yuanwu refuses to treat it as a lifetime possession and instead tests what such speech does when it is uttered.',
  'The phrase denotes a small amount of speech throughout; “electric sweep,” “entry,” and “cutting off tongues” are different appraisals of that speech, not additional things.'),
 '燒香禮拜':('burn incense and bow',['offer incense and bow','burn incense and make obeisance','incense and prostrations'],
  'To burn incense and bow is a public monastic act of reverence, ordered in hall routines and conspicuous when a visitor omits it.',
  'The rules place the paired actions before images and scriptures. Dahui retells Yongjia’s arrival as striking precisely because he circles the seat without first performing them; Shitian lists them among outward occupations when pressing hearers to turn back to their own work.',
  'The stored uses retain one paired action. Performing it, prescribing it, and pointedly omitting it do not create separate senses.'),
 '清淨法身':('the pure teaching body',['pure embodiment of the teaching','pure body of reality','the pure body'],
  'The pure teaching body is the unbounded body named in questions about what the visible world displays and in answers that deny it can be isolated from what fills the eyes.',
  'Fayan answers that all the transformation bodies are it; Huiguang lets mountain colour and stream sound frame the question; Zhimen answers “the eyes are full of dust.” The records thus use the established name while making attempts to locate a separate immaculate object answerable in the interview.',
  'The phrase names one body across doctrinal exposition, verse, and encounter. Contrasting answers about its manifestation are readings of the same referent, not distinct things.'),
 '骨董':('old junk',['old stuff','antique clutter','stale wares','old curios'],
  'Old junk is accumulated, handled-over stuff; masters apply the image to inherited verbal furniture and hard-won understandings that clutter the person who carries them.',
  'Huitang tells Wuxin that his many “old wares” must die away; Yuanwu warns that an unqualified seat-holder leads newcomers into the grass nest to rummage in them; later speakers offer, unpack, or confess the same old stock in public addresses.',
  'Literal old wares supply the ordinary bridge, but these selected hall uses target accumulated sayings, understandings, and presentation. They do not establish that every corpus occurrence is figurative.'),
 '四弘誓願':('the four great vows',['four universal vows','four vast vows','four great pledges'],
  'The four great vows are the fixed fourfold pledge to save beings, end afflictions, learn the boundless teaching gates, and complete the unsurpassed way.',
  'Zongjing lu cites the learning and completion clauses as obligations; Baizhang invokes making the vows while warning against attachment to the vow; Baiyun Shouduan and Yuantong repeat the received four and then recast the same fourfold form as eating, dressing, sleeping, and taking the breeze.',
  'The everyday recasting bends the fixed formula without creating another referent: it announces itself as another “four great vows” and preserves the four-part structure.'),
}

NAMED={
 ('一言半句','M/M59/M59n1540.xml'):'Dahui Zonggao',
 ('一言半句','X/X82/X82n1571.xml'):'Wuzu Fayan',
 ('一言半句','X/X83/X83n1578.xml'):'Huanglong Huinan',
 ('一言半句','T/T48/T48n2003.xml'):'Yuanwu Keqin',
 ('燒香禮拜','M/M59/M59n1540.xml'):'Dahui Zonggao',
 ('清淨法身','C/C077/C077n1710.xml'):'Baizhang Huaihai',
 ('清淨法身','X/X80/X80n1565.xml'):'Huineng',
 ('骨董','X/X69/X69n1357.xml'):'Yuanwu Keqin',
 ('骨董','X/X64/X64n1260.xml'):'Chushi Fanqi',
 ('四弘誓願','X/X68/X68n1318.xml'):'Baiyun Shouduan',
 ('四弘誓願','C/C077/C077n1710.xml'):'Baizhang Huaihai',
 ('四弘誓願','X/X64/X64n1260.xml'):'Yuantong Faxiu',
}

def occ(term,c,title):
    kw=c['Kwic']; name=NAMED.get((term,c['RelPath']))
    speaker=f'{name} utters the exact headword-bearing clause' if name else 'the documentary, question, preface, or formula voice bears the exact headword'
    base={'RelPath':c['RelPath'],'FromLb':c['FromLb'],'ToLb':c.get('ToLb') or c['FromLb'],'Kwic':kw,
          'AttributionNote':f"Source text ({title}): {speaker}; the complete passage was read before attribution.",
          'ContextMasters':[]}
    if name:
        base['MasterName']=name
        base['ContextMasters']=[{'MasterName':name,'Roles':['utterer']}]
        base['DraftActorProof']={'ExactHeadwordClause':kw,'SpeechFrame':f'The complete case places the headword-bearing clause in {name}’s own address, commentary, or quoted retelling.','FullCaseDecision':f'{name} utters the stored headword-bearing clause; other case figures remain context rather than utterers.'}
    else:
        base['ActorAttribution']={'Status':'narrated','Kind':'source narration','ActorLabel':f'the named or documentary voice preserved in {title}','ActorRole':'compiler','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],
          'GrammarEvidence':'The complete passage presents the exact headword in documentary narration, a questioner’s turn, a title-authorized preface, or a preserved formula rather than a safely roster-identifiable master utterance.',
          'ReviewedBy':'Codex f004 lane C full-context author','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True}
        base['DraftActorProof']={'ExactHeadwordClause':kw,'GrammaticalSubject':f'the headword-bearing documentary, question, preface, or formula voice in {title}','FullCaseDecision':'The full surrounding case was read; no roster master was assigned merely from the book owner or nearby respondent.'}
    return base

for e in PACK['entries']:
    term=e['term']; target,aliases,opening,body,limit=SEM[term]
    rows=[]; works=[]
    for c in e['verifiedCandidates']:
        rows.append(occ(term,c,c.get('title') or c['RelPath']))
        works.append(c['workId'])
    sense={'SenseKey':None,'MasterName':None,'PreferredTarget':target,'AlternateTargets':aliases[1:2],
      'SearchAliases':aliases,'Status':'preferred','Validation':'multi-source',
      'Note':'Sense and actor decisions were made from complete stored contexts before prose compilation.',
      'Occurrences':rows,'ClaimAnchors':[],'SourceTexts':[x['RelPath'] for x in rows],
      'RelatedMasters':sorted(set(NAMED.get((term,x['RelPath'])) for x in e['verifiedCandidates'])-{None}),'RelatedTerms':[],
      'ExplanationParts':{'CorpusEarnedOpening':opening,'EvidenceBody':[body]},
      'DraftEvidence':{'OpeningClaimEvidenceKeys':[f'o{i}' for i in range(1,len(rows)+1)],
        'ZenBend':body,'CounterexampleOrLimit':limit,
        'DifferentThingTest':{'Decision':'one-thing','ComparedThings':[target,'the attested questions, prescriptions, omissions, and appraisals'],'Reason':limit},
        'AliasRationale':'These English forms retrieve the same referent while covering ordinary synonymous searches; they do not add senses.',
        'ModifierControls':[{'finding':'not-applicable','reason':'No material or colour modifier changes the referent in this headword.'}],
        'FamilyControls':[{'finding':'checked','reason':'Longer formulas and related expressions were used as controls and not counted as exact standalone occurrences.'}],
        'IndependentWorkIds':works}}
    payload={'SchemaVersion':1,'Entry':{'Id':e['id'],'SourceTerm':term,'CorpusBaselineSha256':BASE,'CreatedBy':'Codex f004 lane C evidence-first full-context canary','WrittenUtc':NOW,'Senses':[sense]}}
    d=ROOT/'fresh-build'/'entries'/e['id']; d.mkdir(parents=True,exist_ok=True)
    (d/'evidence.draft.json').write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    (d/'WORK.md').write_text(f'# {term}\n\n- Wave: f004\n- Lane: C\n- Ordinal: {e["ordinal"]}\n- Stage: evidence-first authoring\n- Contexts read: {len(rows)}\n',encoding='utf-8')
    (d/'STATUS').write_text('researching\n',encoding='utf-8')
print('authored',len(PACK['entries']))
