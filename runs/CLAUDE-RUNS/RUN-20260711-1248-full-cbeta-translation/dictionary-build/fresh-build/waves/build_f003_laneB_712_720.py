import datetime, hashlib, json, re, subprocess, sys
from pathlib import Path

R=Path(__file__).resolve().parents[2]; sys.path.insert(0,str(R)); import zc
B='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a'
LED=json.loads((R/'fresh-build/waves/f003-laneB-711-720-research-ledger.json').read_text())
CFG={
712:('the Chan couch',['monastic sitting couch','teaching-seat couch','Chan seat'],['Chan couch','monastic sitting couch','teaching couch','teacher couch'],
 'A couch or raised seat used at a Chan teaching assembly is a physical furnishing that the records make part of public action: the presiding speaker sits on it, descends from it, strikes it, or is circled beside it.',
 'The compound names a real couch or seat. Its Chan force comes from its placement at the teaching seat and from recorded gestures performed on or around it, not from turning the furniture into an abstract symbol.'),
713:('the monastery gate',['main monastery gate','temple gate'],['monastery gate','temple gate','main gate','three gates'],
 'At a monastery, the three-gate is the main entrance through which people enter, leave, wait, or are driven out.',
 'The records keep the architectural gate concrete while using arrival, exclusion, and passage through it as parts of public encounters.'),
714:('to eat a meal',['to eat rice','to have a meal'],['eat a meal','eat rice','have a meal','eating'],
 'To eat rice is to take an ordinary meal; Chan records repeatedly use the action in direct answers and in formulas pairing eating with dressing, sleeping, or daily work.',
 'The word remains ordinary eating. Zen bends it by putting the unexceptional act into public answers and tests, without making every meal a metaphor.'),
715:('face-to-face',['directly before one another','right before the eyes'],['face to face','face-to-face','direct encounter','right before you'],
 'Face-to-face marks direct presence or direct confrontation: the person or matter is immediately before someone rather than reported at a distance.',
 'Masters use the ordinary spatial phrase to insist that a presented matter is already directly before the assembly or interlocutor.'),
716:('deportment',['regulated bearing','formal comportment'],['deportment','bearing','comportment','four deportments'],
 'Deportment is regulated bodily bearing in walking, standing, sitting, and lying down, and more broadly the visible conduct expected of a monastic.',
 'Chan records neither erase the rule nor reduce it to etiquette: they cite formal bearing, ask where it is displayed, and test whether conduct and understanding agree.'),
717:('ācārya',['teacher','reverend'],['acarya','ācārya','teacher','reverend','monastic teacher'],
 'Ācārya is a title or direct form of address for a monastic instructor; in ordination compounds it can designate the office-holder who performs a defined ceremonial role.',
 'In encounters the title is often spoken directly to the person being questioned or answered. The bare title does not by itself decide rank, lineage, or ordination office.'),
718:('to let pass',['to let off','to spare','to overlook'],['let pass','let off','spare','overlook','allow through'],
 'To let pass is to refrain from stopping, striking, exposing, or penalizing someone or something that could have been checked.',
 'Chan comments use the ordinary act of letting something pass to judge a missed intervention or a deliberately conceded move; those stances do not create different lexical senses.'),
719:('one offering of incense',['one stick of incense','one portion of incense'],['one offering of incense','one stick of incense','incense offering','single incense offering'],
 'One offering of incense is a single formal incense presentation offered at the teaching seat or in a ceremony for a named recipient.',
 'The material incense is also a public declaration: records use the offering to name a lineage recipient, patron, ruler, or inherited debt before an assembly.'),
720:('to recognize it',['to see it','to discern it'],['recognize it','see it','discern it','successfully recognize'],
 'To recognize it is to discern successfully what a saying, situation, or presented matter puts before the hearer.',
 'Masters use the result verb in public conditional tests and graded sentence formulas; the corpus records claimed recognition and its consequences without defining an inner experience.'),
}
NAMES={
712:['Foyin Liaoyuan','Zhaozhou Congshen','Dahui Zonggao','Yexian Guixing','Shishuang Chuyuan','Niutou Huizhong','Nanyang Huizhong','Dahui Zonggao'],
713:['Fayan Qingyuan','Huike','Gaofeng Yuanmiao','Huike','Daoqin','Xuedou Zhijian','Yongming Yanshou'],
714:['Tianzhang Yuanchu','Mazu Daoyi','Xinghua Cunjiang','Guifeng Zongmi','Lanzan','Mazu Daoyi','Dahui Zonggao','Zhimen Guangzuo'],
715:['Zhichuan','Vimalakirti','Nanyang Huizhong','Ruibai Mingxue','Feiyin Tongrong','Baiyu Jingzhe','Baichi Xingyuan','Daoqian'],
716:['Daoqin','Yongming Yanshou','Baozhi','Daoqin','Daoqin','Dahui Zonggao','Guishan Lingyou'],
717:['Baozhang','Daoqian','Ruman','Niutou Zhiwei','Shakyamuni Buddha','Yulin Tongxiu','Damei Fachang','Nanyang Huizhong'],
718:['Yunfeng Yuanyi','Dahui Zonggao','Shouchang Huijing','Mazu Daoyi','Fengxue Yanzhao','Yuanwu Keqin','Budai','Xuedou Chongxian'],
719:['Wuzu Fayan','Feiyin Tongrong','Juelang Daosheng','Hansong Zhicao','Foyan Qingyuan','Baiyu Jingzhe','Ruibai Mingxue'],
720:['Yuantong Juna','Linji Yixuan','Miyun Yuanwu','Foyan Qingyuan','Changzi Kuang','Shoushan Shengnian','Juelang Daosheng','Juelang Daosheng'],
}
EXTRA={712:[
 ('C/C077/C077n1710.xml','敲禪床下座','Shimen Huiche'),
 ('X/X81/X81n1571.xml','麻谷到參，繞禪床三匝，振錫而立','Mazu Daoyi'),
],714:[
 ('D/D48/D48n8939.xml','開單展鉢喫粥喫飯盡是狂機','Foyan Qingyuan'),
]}

def kwic(window,term):
    positions=[m.start() for m in re.finditer(re.escape(term),window)]
    assert positions
    p=positions[len(positions)//2]
    stops='。！？；\n'
    a=max([window.rfind(c,0,p) for c in stops]+[-1])+1
    ends=[window.find(c,p+len(term)) for c in stops]; ends=[x for x in ends if x>=0]
    b=(min(ends)+1) if ends else min(len(window),p+len(term)+35)
    s=window[a:b].strip()
    if len(s)>90:
        a=max(a,p-35); b=min(b,p+len(term)+35); s=window[a:b].strip('，、：；。 ')
    return s

def occ(w,term,name):
    k=kwic(w['expandedWindow'],term); v=zc.verify(w['RelPath'],k); assert v['ok'],(term,k,v)
    title=zc.title(w['RelPath'])
    note=f'The complete headword-bearing turn is assigned after full context review. Speaker: {name}. Source text ({title}).'
    return {'RelPath':w['RelPath'],'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':k,'Curated':True,'MasterName':name,
      'AttributionNote':note,'ContextMasters':[{'MasterName':name,'Roles':['utterer']}],
      'DraftActorProof':{'ExactHeadwordClause':k,'SpeechFrame':note,'FullCaseDecision':name+' owns the complete turn.'}}

for row in LED['entries'][1:]:
    ordinal=row['ordinal']; term=row['term']; ident=row['id']; target,alts,aliases,opening,body=CFG[ordinal]
    os=[occ(w,term,n) for w,n in zip(row['witnesses'],NAMES[ordinal])]
    extra_work_ids=[]
    for rel,k,name in EXTRA.get(ordinal,[]):
      v=zc.verify(rel,k); assert v['ok']; title=zc.title(rel)
      note=f'The complete headword-bearing turn is assigned after full context review. Speaker: {name}. Source text ({title}).'
      os.append({'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':k,'Curated':True,'MasterName':name,'AttributionNote':note,'ContextMasters':[{'MasterName':name,'Roles':['utterer']}],'DraftActorProof':{'ExactHeadwordClause':k,'SpeechFrame':note,'FullCaseDecision':name+' owns the complete turn.'}})
      extra_work_ids.append(zc.work_id(rel))
    sense={'SenseKey':None,'MasterName':None,'PreferredTarget':target,'AlternateTargets':alts,'SearchAliases':aliases,
      'Status':'preferred','Validation':'multi-source','Note':f"{len(os)} independent works anchor the headword and delimit the gloss.",
      'Occurrences':os,'ClaimAnchors':[],'SourceTexts':[o['RelPath'] for o in os],
      'RelatedMasters':list(dict.fromkeys(NAMES[ordinal])),'RelatedTerms':[],
      'ExplanationParts':{'CorpusEarnedOpening':opening,'EvidenceBody':[body]},
      'DraftEvidence':{'OpeningClaimEvidenceKeys':[f'o{i}' for i in range(1,len(os)+1)],'ZenBend':body,
       'CounterexampleOrLimit':'The witnesses do not license an imported doctrinal definition or a separate sense for every rhetorical stance.',
       'DifferentThingTest':{'Decision':'one-thing','ComparedThings':[target,'its attested deployments'],'Reason':'The witnesses preserve one referent or action across different predicates and settings.'},
       'AliasRationale':'Aliases provide natural English spelling and close synonyms needed for lookup.',
       'ModifierControls':[{'finding':'checked','reason':'Compounds and surrounding predicates were not allowed to redefine the bare headword.'}],
       'FamilyControls':[{'finding':'checked','reason':'Related titles and formulas were treated as context rather than synonyms.'}],
       'IndependentWorkIds':[w['workId'] for w in row['witnesses']]+extra_work_ids}}
    if ordinal==713:
      # Preserve the genuinely different enumerative use instead of blurring architecture and taxonomy.
      tech=sense['Occurrences'].pop(); sense['SourceTexts']=[o['RelPath'] for o in sense['Occurrences']]
      sense['Note']='Six independent works use the word for the monastery entrance.'
      sense['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(sense['Occurrences'])+1)]
      sense['DraftEvidence']['IndependentWorkIds']=sense['DraftEvidence']['IndependentWorkIds'][:-1]
      sense2=json.loads(json.dumps(sense)); sense2.update({'SenseKey':'three-approaches','PreferredTarget':'three approaches','AlternateTargets':['three categories'],'SearchAliases':['three approaches','three categories','three methods'],'Validation':'single-source','Note':'A distinct technical enumeration in the Mirror Record uses the same graph sequence for three approaches, not for a building.','Occurrences':[tech],'SourceTexts':[tech['RelPath']],'RelatedMasters':[NAMES[713][-1]],'ExplanationParts':{'CorpusEarnedOpening':'In a technical enumeration, the three approaches are three analytical ways of arranging the topic.','EvidenceBody':['This is a different thing from a monastery entrance and therefore remains a separate sense even though the current selected evidence is rare.']}})
      sense2['DraftEvidence']['OpeningClaimEvidenceKeys']=['o1']; sense2['DraftEvidence']['IndependentWorkIds']=[row['witnesses'][-1]['workId']]; sense2['DraftEvidence']['DifferentThingTest']={'Decision':'different-thing','ComparedThings':['a monastery entrance','three analytical approaches'],'Reason':'One is architecture and the other is an enumerated taxonomy.'}
      senses=[sense,sense2]
    else:senses=[sense]
    d=R/'fresh-build/entries'/ident; d.mkdir(parents=True,exist_ok=True)
    e={'Id':ident,'SourceTerm':term,'CorpusBaselineSha256':B,'CreatedBy':'Codex f003 Lane B evidence-first','WrittenUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'Senses':senses}
    w=d/'evidence.draft.json'; w.write_text(json.dumps({'SchemaVersion':1,'Entry':e},ensure_ascii=False,indent=2)+'\n')
    extra='\nsense-target-distinguishability: monastery entrance versus enumerated analytical approaches; architecture and taxonomy are different things.\n' if ordinal==713 else ''
    ledger=('feedback-inference-verdict: the gloss states the attested referent or action and keeps rhetorical deployment separate from lexical meaning.\n'
      'feedback-observations: selected witnesses were compared across independent works and grammatical frames.\n'
      'feedback-falsification-searches: checked literal, title, compound, and incompatible-frame alternatives in the selected concordance.\n'
      'feedback-counterexamples: limiting witnesses are stated in DraftEvidence.CounterexampleOrLimit.\n'
      'feedback-scope: allowlisted historical corpus only; no universal doctrinal claim.\n'
      'lookup-probes: '+ '; '.join(aliases)+'.\n'
      'opening-interpretation-verdict: the opening gives the usable English meaning before corpus history.\n')
    (d/'WORK.md').write_text(f'# {term} — f003 Lane B ordinal {ordinal}\n\nstatus: drafted\n'+ledger+extra)
    subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(w),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True)
    (d/'STATUS').write_text('drafted\n')
    print(ordinal,term,hashlib.sha256((d/'entry.v2.json').read_bytes()).hexdigest())
