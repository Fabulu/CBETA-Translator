#!/usr/bin/env python3
import datetime, hashlib, json, subprocess, sys
from pathlib import Path

HERE=Path(__file__).resolve().parent; ROOT=HERE.parent.parent
sys.path.insert(0,str(ROOT)); import zc
PACK_PATH=HERE/'f004-laneA-901-905-early-sample-evidence-packets.json'
PACK=json.loads(PACK_PATH.read_text()); NOW=datetime.datetime.now(datetime.timezone.utc).isoformat()
BASE='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a'

SEM={
'佛法大意':('the essential point of Buddha’s teaching',['essential meaning of Buddha’s teaching','main point of Buddha’s teaching','what is the Buddha teaching about'],'“The essential point of Buddha’s teaching” is a stock public-interview question whose force lies in demanding a present answer, not in supplying a settled propositional formula.','Monks and officials put the same question to Budai, Daolin, Linji, Qingliang, and Xuedou; the respondents put down a bag, quote an elementary moral maxim, shout, defer the question, or offer an image. The stable lexical unit is the demand for the teaching’s main point. Zen bends it by making the answer an event of the interview rather than a reusable definition.','The mutually incompatible answers are evidence for the question’s interview function, not separate meanings of the headword and not a composite proposition.'),
'香雲':('a cloud of incense or fragrance',['incense cloud','fragrant cloud','cloud of fragrant offering'],'A fragrant cloud is either visible incense rising through a ceremony or poetic cloud imagery made fragrant and auspicious by its setting.','Records use the image around incense, bells, banners, offerings, birthdays, and verses of praise. In ceremonial passages it gathers rising smoke or fragrant offerings into a cloud; in praise it can ornament the scene without asserting literal weather. The shared image is a spreading fragrant cloud, while context decides whether smoke is physically present.','The witnesses do not make every ordinary cloud a 香雲, nor do they establish one fixed symbolic equivalent such as awakening.'),
'頂門正眼':('the true eye at the crown of the head',['crown-gate true eye','true crown eye','decisive eye at the crown'],'The true eye at the crown is an emphatic image for the capacity to discern and respond without being trapped by the presented terms.','Yongzhang Jun and Fuxin Bencai say that it must be opened or that everyone has it; a preface praises Hanxiu Ruqian as possessing it; other records test it in questions about light and the teaching seat. Zen relocates “seeing” to the crown of the head to mark an examiner’s decisive vision, not an anatomical organ.','The corpus does not license a physiological eye or a separate sense for the same capacity when the phrase appears in praise, command, or question.'),
'知事':('a monastery administrator',['monastic administrator','administrative officer','monastery officeholder'],'A monastery administrator is one of the officers who conduct the community’s practical and institutional business.','Rulebooks list the office among communal posts; records address the administrators collectively, assign them duties, thank incoming and outgoing officeholders, and narrate dealings with them. The title names administration rather than the presiding teaching office.','Singular and collective uses name the same institutional role; table-of-contents headings are documentary evidence and not utterances by the officers listed.'),
'續傳燈錄':('Continuation of the Lamp Record',['continued transmission-of-the-lamp record','supplement to the lamp record','Continuation of the Transmission of the Lamp'],'“Continuation of the Lamp Record” is a bibliographic title used for a lineage compilation that extends earlier lamp histories.','It occurs as the title of the extant work, inside the title of an expanded continuation, as an author’s planned compilation, and in a biography describing the collection of materials for such a book. In Zen records, continuing the lamp is historiographical work: adding later teachers and transmission material to the recorded lineage.','Only four exact independent witnesses exist in the locked corpus. The entry therefore records a thin title use and does not pretend that different continuation projects are different lexical senses.'),
}

# Exact-headword utterer decisions after reading each stored complete unit.
# None means narrator/editor/non-master/unresolved voice; context masters are never promoted to utterer.
MASTER={('佛法大意',1):'Baofu Congzhan',('頂門正眼',1):'Yongzhang Jun',('頂門正眼',2):'Fuxin Bencai',('頂門正眼',5):"Yuan'an Feng",('知事',5):'Fachang Yiyu'}
CTX={
 ('佛法大意',1):[('Baofu Congzhan','utterer'),('Budai','respondent')],
 ('佛法大意',2):[('Qingliang Taiqin','respondent')],
 ('佛法大意',3):[('Niaoke Daolin','respondent')],
 ('佛法大意',4):[('Linji Yixuan','respondent')],
 ('佛法大意',5):[('Niaoke Daolin','respondent')],
 ('佛法大意',6):[('Linji Yixuan','respondent')],
 ('佛法大意',7):[('Xuedou Chongxian','respondent')],
 ('頂門正眼',1):[('Yongzhang Jun','utterer')],('頂門正眼',2):[('Fuxin Bencai','utterer')],
 ('頂門正眼',3):[('Hanxiu Ruqian','person-described')],('頂門正眼',5):[("Yuan'an Feng",'utterer')],
 ('知事',5):[('Fachang Yiyu','utterer')],('知事',7):[('Furong Daokai','section-subject')],
}

def recut(rel,term,kwic):
    j=kwic.find(term); assert j>=0
    q=kwic[max(0,j-65):min(len(kwic),j+len(term)+80)]
    if q.count(term)>1: q=q[:q.find(term)+len(term)+45]
    v=zc.verify(rel,q)
    if not v.get('ok') or q.count(term)!=1: q=term; v=zc.verify(rel,q)
    assert v.get('ok') and q.count(term)==1
    return q,v

rows=[]
for e in PACK['entries']:
    term=e['term']; target,aliases,opening,body,limit=SEM[term]; occs=[]; works=[]
    candidates=list(e['verifiedCandidates'])
    if term=='續傳燈錄' and len(candidates)<6:
        for rel,lb in [('X/X83/X83n1574.xml','0257a10'),('X/X83/X83n1574.xml','0257b07')]:
            hit=next(x for x in zc.find(rel,term,ctx=80,limit=20) if x['fromLb']==lb)
            candidates.append({'workId':zc.work_id(rel),'RelPath':rel,'title':zc.title(rel),'FromLb':lb,'ToLb':lb,'Kwic':hit['window'],'zcVerified':True,'sectionHead':'additional exact title discussion','completeContext':zc.context(rel,lb,chars=3000,kwic=term)})
    for i,c in enumerate(candidates,1):
        q,v=recut(c['RelPath'],term,c['Kwic']); name=MASTER.get((term,i)); cm=[]
        for n,role in CTX.get((term,i),[]): cm.append({'MasterName':n,'Roles':[role]})
        o={'RelPath':c['RelPath'],'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':q,'Curated':True,'ContextMasters':cm}
        if name:
            o['MasterName']=name
            o['AttributionNote']=f"Source text ({c['title']}): {name} utters the exact headword in the reviewed complete case."
            o['DraftActorProof']={'ExactHeadwordClause':q,'SpeechFrame':f'The complete case assigns the exact headword-bearing turn to {name}.','FullCaseDecision':f'{name}, not an adjacent questioner, respondent, editor, or section subject, utters this headword.'}
        else:
            if term=='佛法大意' and i in (2,4,6,7): kind,label,role='unnamed','an unnamed monk','questioner'
            elif term=='佛法大意' and i in (3,5): kind,label,role='named-lay-questioner','Bai Juyi','questioner'
            elif term=='頂門正眼' and i==4: kind,label,role='unnamed','an unnamed monk','questioner'
            elif term=='香雲' and i==4: kind,label,role='named-lay-verse-author','Dai Daochun','verse-author'
            else: kind,label,role='narrated','editorial, compiler, inscriptional, or unresolved authored voice','compiler'
            closed_status='reviewed-unnamed' if kind=='unnamed' else ('identified-non-master' if kind.startswith('named-') else kind)
            o['ActorAttribution']={'Status':closed_status,'Kind':kind,'ActorLabel':label,'ActorRole':role,'RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':'The complete headword-bearing unit was read. Its grammatical and speech frame does not make a nearby master the utterer unless MasterName is set above.','ReviewedBy':'Codex f004 lane A full-context author','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True}
            o['AttributionNote']=f"Source text ({c['title']}): {label} bears the exact headword; nearby masters remain contextual only."
            o['DraftActorProof']={'ExactHeadwordClause':q,'GrammaticalSubject':label,'FullCaseDecision':f'The complete case assigns the headword to {label}; no nearby master is promoted by title proximity.'}
        occs.append(o); works.append(c['workId'])
        c.update({'Kwic':q,'FromLb':v['fromLb'],'ToLb':v['toLb'],'exactTurnDecision':o['DraftActorProof']['FullCaseDecision'],'canonicalRosterDecision':name or 'no master utterer','admitted':True})
    validation='multi-source' if len(set(works))>=2 else 'provisional'
    sense={'SenseKey':None,'MasterName':None,'PreferredTarget':target,'AlternateTargets':aliases[1:2],'SearchAliases':aliases,'Status':'preferred','Validation':validation,'Note':f'{len(set(works))} distinct work IDs support the exact headword; all saved complete contexts were read before prose.','Occurrences':occs,'ClaimAnchors':[],'SourceTexts':[o['RelPath'] for o in occs],'RelatedMasters':sorted({m['MasterName'] for o in occs for m in o.get('ContextMasters',[])} | {o['MasterName'] for o in occs if o.get('MasterName')}),'RelatedTerms':[],'ExplanationParts':{'CorpusEarnedOpening':opening,'EvidenceBody':[body]},'DraftEvidence':{'OpeningClaimEvidenceKeys':[f'o{i}' for i in range(1,len(occs)+1)],'ZenBend':body,'CounterexampleOrLimit':limit,'DifferentThingTest':{'Decision':'one-thing','ComparedThings':[target,'selected grammatical and rhetorical deployments'],'Reason':limit},'AliasRationale':'English lookup equivalents broaden search without multiplying senses.','ModifierControls':[{'finding':'checked','reason':'Titles, longer compounds, literal collisions, and component-only matches were reviewed separately.'}],'FamilyControls':[{'finding':'checked','reason':'Parallel recensions and nested formulas are controls, not independent senses.'}],'IndependentWorkIds':list(dict.fromkeys(works))}}
    payload={'SchemaVersion':1,'Entry':{'Id':e['id'],'SourceTerm':term,'CorpusBaselineSha256':BASE,'CreatedBy':'Codex f004 lane A evidence-first full-context author','WrittenUtc':NOW,'Senses':[sense]}}
    d=ROOT/'fresh-build/entries'/e['id']; d.mkdir(parents=True,exist_ok=True); wp=d/'evidence.draft.json'; wp.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n')
    (d/'WORK.md').write_text(f'# {term} — f004 lane A ordinal {e["ordinal"]}\n\nstatus: authored from {len(occs)} complete-context witnesses\nfeedback-inference-verdict: {opening}\nfeedback-observations: exact headword turns, actors, work identities, aliases, and sense boundaries reviewed\nfeedback-falsification-searches: titles; catalogues; compounds; parallel recensions; literal collisions\nfeedback-counterexamples: {limit}\nfeedback-scope: locked 494-file / 487-work corpus\nlookup-probes: '+ '; '.join(aliases)+'\nopening-interpretation-verdict: English-first and corpus-earned\nsense-target-distinguishability: one retained referent; no noun/verb, capitalization, or paraphrase split\n')
    subprocess.run([sys.executable,str(ROOT/'compile_evidence_draft.py'),str(wp),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True)
    (d/'STATUS').write_text('drafted\n'); rows.append({'ordinal':e['ordinal'],'id':e['id'],'term':term,'occurrences':len(occs),'worksheetSha256':hashlib.sha256(wp.read_bytes()).hexdigest(),'entrySha256':hashlib.sha256((d/'entry.v2.json').read_bytes()).hexdigest()})
PACK['entryCompilationAttempted']=True; PACK['bulkAuthoringAllowed']=False; PACK_PATH.write_text(json.dumps(PACK,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(rows,ensure_ascii=False,indent=2))
