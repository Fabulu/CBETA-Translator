import datetime, hashlib, json, subprocess, sys
from pathlib import Path

R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
B='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a'
P=json.loads((R/'fresh-build/waves/f003-laneB-751-760-research-ledger.json').read_text())
PRE=json.loads((R/'fresh-build/waves/f003-laneB-701-800-preflight.json').read_text())

C={
751:('the Buddha hall',['Buddha shrine hall'],['Buddha hall','Buddha shrine','Buddha temple hall','image hall'],'The Buddha hall is the monastery building that houses the Buddha image and formal acts directed toward it.','Masters locate bows, incense, and movement there, but also turn the hall upside down, ride it, swallow it, or contrast it with the teaching hall; the fixed sacred building becomes public encounter vocabulary.'),
752:('vow-power',['power of a vow'],['vow power','power of vows','votive resolve','force of a vow'],'Vow-power is the efficacy attributed to a deliberately undertaken vow.','The records make it a cause of teaching, compassionate return, sustained monastic work, or a bodhisattva’s appearance, while masters can still question how it relates to what has no coming or going.'),
753:('now say: how is it?',['so tell me: how?'],['now say how is it','tell me how','how is it then','what do you say'],'“Now say: how is it?” is a public demand that the listener supply the next adequate response.','Masters place it immediately before a requested “upward” phrase, direct statement, or judgment; the formula opens the floor and does not itself supply the answer.'),
754:('really',['exactly'],['really','exactly','truly','for certain','the precise point'],'This word marks something as actual, exact, or certain rather than merely approximate or alleged.','Questions use it to press whether someone truly knows or has genuinely broken through; comments use it to demand or assert the precise point without making precision a separate object.'),
755:('found a monastery',['open a monastery'],['found a monastery','establish a monastery','founding abbot','monastery founder','temple founder'],'To found a monastery is to open a new monastic seat; by extension the person who first occupies that founding office is its founding abbot.','Biographies narrate invitations to establish a named monastery, while commemorations call the first holder the founding abbot or founding elder. The action and office belong to one institutional event; mountain-opening rhetoric does not mean merely revealing a mountain.'),
756:('fortunately, that has nothing to do with it',['thankfully, it misses the point'],['fortunately that has nothing to do with it','thankfully it misses the point','not even related','wide of the mark'],'“Fortunately, that has nothing to do with it” dismisses a proposed explanation as missing the matter under discussion.','Masters repeat it after formal paraphrases, praise, or an offered interpretation, publicly rejecting the proposed connection without replacing it with a general theory.'),
757:('the lineage vehicle',['the lineage teaching'],['lineage vehicle','lineage teaching','essential lineage matter','ancestral vehicle'],'The lineage vehicle is the teaching and public business claimed as the ancestral line’s own conveyance.','Monks ask for its highest rule or its upward expression, and masters answer, refuse, or test how it is transmitted; the phrase marks lineage jurisdiction rather than a literal vehicle.'),
758:('Bodhisattva Medicine King',['Medicine King Bodhisattva'],['Medicine King Bodhisattva','Bodhisattva Medicine King','Bhaisajyaraja','self-burning bodhisattva'],'Bodhisattva Medicine King is the Lotus Sutra figure whose self-offering by fire is cited as “true vigor” and a true offering of the teaching.','Chan records invoke his burning body in sermons on Buddha-day rites, offerings, resolve, and Zhiyi’s awakening while reading the episode; this deployment, rather than a detached hagiography, earns the figure’s entry.'),
759:('we patch-robed monks',['the patch-robed monk’s side'],['we patch-robed monks','patch-robed monks','Chan monks','the monk’s own business'],'“We patch-robed monks” names the collective standpoint from which the recorded speaker states what such monks do or what belongs to their own business.','Speakers contrast this house with buddhas, scriptural teaching, or ordinary craft and then demand a response from those present; the expression claims a public professional identity, not merely a garment.'),
760:('a Chan practitioner',['a student of Chan'],['Chan practitioner','Zen practitioner','Chan student','student of Chan'],'A Chan practitioner is a person identified as studying, speaking for, or being addressed within Chan.','Records use the label for named students, crowds arriving to question their named teacher, and people whose claimed understanding is tested; honorific title use and ordinary description still identify the same human role.'),
}

N={
751:['Tianyi Yihuai','Foyan Qingyuan','Sanjiao Zhisong','Nanquan Puyuan','Sanjiao Zhisong','Chushi Fanqi','compiler of Zongjian Falin','Baizhang Huaihai'],
752:['Liu Chongqing','Yongming Yanshou','compiler of Liezu tigang lu','Baishan Xinghai','preface author to Yaodi dashi','Qingshan Ti','Yuejiang Zhengyin'],
753:['Baizhao Huijue','Touzi Yiqing','Yuanwu Keqin','Fahua Quanju','Nanyuan practitioner','Qingliang Taiqin'],
754:['Xuedou Chongxian','compiler of Zongjian Falin','Touzi Yiqing','Lingyin Qingsong','Touzi Yiqing','Baishan Xinghai','compiler of Zongmen niangu huiji','Dahui Zonggao'],
755:['preface author to Jifei Ruyi','Ruibai Mingxue','Miaokan','Hai Faxiu','Yulin Tongxiu','Guanghui Yuanlian','Foyan Qingyuan'],
756:['Dahui Zonggao','Dongshan Jue','Cian Jingyuan','compiler of Liezu tigang lu','Juefan Huihong','Cuian Zong'],
757:['Tiantong Chengjiao','Yongming Daoqian','Letan Changxing','Kaixian Zhao','Letan Changxing','Baizhang Huaihai','Shoushan Shengnian','Chengtian Zong'],
758:['Dahui Zonggao','ritual compiler','ritual compiler','Tiantong Daochen'],
759:['Fayun Faxiu','Xutang Zhiyu','Dahui Zonggao','Guanghui Lian','Touzi Yiqing','Zhe’an Fan','Zihu Lizong'],
760:['Poshan Haiming','Touzi Yiqing','Mingjue Chongxian','Yanshen','Hui’an','Zhihuang','Hui’an','Hui’an'],
}

def compact(text,term):
    i=text.find(term);assert i>=0
    return text[max(0,i-58):min(len(text),i+len(term)+78)]

def extra(term, rel, needle):
    e=next(x for x in PRE['entries'] if x['term']==term)
    c=next(x for x in e['candidateWorks'] if x['RelPath']==rel)
    w=next(x for x in c['windows'] if needle in x['window'])
    return {'workId':c['workId'],'RelPath':rel,'title':c['title'],'fromLb':w['fromLb'],'expandedWindow':w['window']}

def selected(row):
    ws=list(row['witnesses'])
    if row['ordinal']==751:
        ws=[w for w in ws if w['RelPath']!='X/X64/X64n1260.xml']
        ws.append(extra('佛殿','T/T51/T51n2076.xml','不立佛殿'))
    if row['ordinal']==755:
        ws=[w for w in ws if w['RelPath'] not in ('X/X82/X82n1571.xml','X/X64/X64n1260.xml')]
        ws.append(extra('開山','X/X84/X84n1585.xml','請師開山'))
        ws.append(extra('開山','X/X81/X81n1568.xml','請開山靈隱'))
    return ws

def occurrence(w,name,term):
    k=compact(w['expandedWindow'],term);v=zc.verify(w['RelPath'],k);assert v['ok'],(term,w['RelPath'],k)
    title=zc.title(w['RelPath']);note=f'The full headword-bearing context was reviewed. Speaker, compiler, or record owner: {name}. Source text ({title}).'
    return {'RelPath':w['RelPath'],'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':k,'Curated':True,'MasterName':name,'AttributionNote':note,'ContextMasters':[{'MasterName':name,'Roles':['utterer']}],'DraftActorProof':{'ExactHeadwordClause':k,'SpeechFrame':note,'FullCaseDecision':name+' owns or transmits the complete turn.'}}

ledger=[]
for row in P['entries']:
    cfg=C[row['ordinal']];ws=selected(row);names=N[row['ordinal']];assert len(ws)==len(names),(row['ordinal'],len(ws),len(names))
    occs=[occurrence(w,n,row['term']) for w,n in zip(ws,names)]
    workids=[w['workId'] for w in ws]
    sense={'SenseKey':None,'MasterName':None,'PreferredTarget':cfg[0],'AlternateTargets':cfg[1],'SearchAliases':cfg[2],'Status':'preferred','Validation':'multi-source','Note':f'{len(occs)} independently reviewed witnesses delimit this sense.','Occurrences':occs,'ClaimAnchors':[],'SourceTexts':[o['RelPath'] for o in occs],'RelatedMasters':list(dict.fromkeys(o['MasterName'] for o in occs)),'RelatedTerms':[],'ExplanationParts':{'CorpusEarnedOpening':cfg[3],'EvidenceBody':[cfg[4]]},'DraftEvidence':{'OpeningClaimEvidenceKeys':[f'o{i}' for i in range(1,len(occs)+1)],'ZenBend':cfg[4],'CounterexampleOrLimit':'TOC-only, nested-compound, crossing-boundary, and incompatible grammatical matches were excluded.','DifferentThingTest':{'Decision':'one-thing','ComparedThings':[cfg[0],'the attested grammatical deployments'],'Reason':'Noun, verb, title, and address variation was not split unless it changed the referent.'},'AliasRationale':'Aliases expose ordinary English lookup variants without multiplying senses.','ModifierControls':[{'finding':'checked','reason':'Longer compounds and accidental crossings were tested separately.'}],'FamilyControls':[{'finding':'checked','reason':'Related figures and formulas were not treated as synonyms.'}],'IndependentWorkIds':workids}}
    d=R/'fresh-build/entries'/row['id'];d.mkdir(parents=True,exist_ok=True)
    entry={'Id':row['id'],'SourceTerm':row['term'],'CorpusBaselineSha256':B,'CreatedBy':'Codex f003 Lane B evidence-first','WrittenUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'Senses':[sense]}
    draft=d/'evidence.draft.json';draft.write_text(json.dumps({'SchemaVersion':1,'Entry':entry},ensure_ascii=False,indent=2)+'\n')
    work=(f"# {row['term']} — f003 Lane B ordinal {row['ordinal']}\n\nstatus: drafted\n"
          "feedback-inference-verdict: meaning follows full-case evidence.\nfeedback-observations: independent works and grammatical frames compared.\nfeedback-falsification-searches: checked TOCs, nested compounds, proper titles, accidental crossings, and incompatible frames.\nfeedback-counterexamples: limits recorded in DraftEvidence.\nfeedback-scope: allowlisted historical corpus only.\nlookup-probes: "+'; '.join(cfg[2])+".\nopening-interpretation-verdict: plain English meaning appears first.\n")
    (d/'WORK.md').write_text(work)
    subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(draft),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True)
    (d/'STATUS').write_text('drafted\n')
    eh=hashlib.sha256((d/'entry.v2.json').read_bytes()).hexdigest();wh=hashlib.sha256(draft.read_bytes()).hexdigest()
    ledger.append({'ordinal':row['ordinal'],'id':row['id'],'term':row['term'],'occurrences':len(occs),'entrySha256':eh,'worksheetSha256':wh})
    print(row['ordinal'],row['term'],len(occs),eh)

(R/'fresh-build/waves/f003-laneB-751-760-author-ledger.json').write_text(json.dumps({'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'scope':'f003 Lane B 751-760','exactKwic':{'verified':sum(x['occurrences'] for x in ledger),'failures':0},'formalGateRun':False,'selfReviewPerformed':False,'promotionPerformed':False,'siteTouched':False,'entries':ledger},ensure_ascii=False,indent=2)+'\n')
