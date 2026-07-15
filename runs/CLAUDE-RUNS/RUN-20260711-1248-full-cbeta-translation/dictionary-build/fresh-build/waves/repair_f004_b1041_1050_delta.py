from pathlib import Path
import copy, datetime, hashlib, json, subprocess, sys

R=Path(__file__).resolve().parents[2]; H=R/'fresh-build/waves'; sys.path.insert(0,str(R)); import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat(); REVIEW='fresh-build/waves/f004-b1041-1050-independent-rereview.json'
REVIEW_SHA='a46f79f6bd65d11170e384ca9d6465cdca61ee910e13d1c2ef2b4e92b855bf3f'
IDS={1041:'t_ea905d5d7453',1042:'t_b751a85ba963',1043:'t_dbbc09ad8c5d',1044:'t_efc6a42814ee',1045:'t_aced87de5b30',1046:'t_850d52f97185',1047:'t_1d04ccb80940',1048:'t_fb43354d2aae',1049:'t_93edb4403f03',1050:'t_c513dc22845c'}
RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
IMMUTABLE_1045='ed4b307c6e95fa26815f16ccfc4495915999e9deb0d891d05c4a43f981f3ce56'
IMMUTABLE_1077='6bc077fb30adb10a31b3b3e50d2f058ba8c7d6a0bb96fc2f82db3ce5e35283f3'

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def actor(o,label,role='editorial',status='narrated',context=None,proof=None):
    o['MasterName']=None; o['ContextMasters']=context or []
    p=proof or f'The exact headword is in {label}, rather than in a master’s direct speech.'
    o['ActorAttribution']={'Status':status,'Kind':label,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':p,'ReviewedBy':'Codex f004 checkpoint1 delta repair','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True}
    o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {label}. {p}'
    o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':p,'FullCaseDecision':p}
def named(o,name,role='utterer',proof=None):
    o['MasterName']=name; o.pop('ActorAttribution',None); o['ContextMasters']=[{'MasterName':name,'Roles':[role]}]
    p=proof or f'The complete source unit assigns the exact headword-bearing wording to {name}.'
    o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {name}. {p}'
    o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':name,'SpeechFrame':p,'FullCaseDecision':p}
def explain(s,opening,body):
    s['ExplanationParts']={'CorpusEarnedOpening':opening,'EvidenceBody':[body]}; s['DraftEvidence']['ZenBend']=body
def refresh(s,target,occs,validation='multi-source',status='preferred'):
    s['PreferredTarget']=target; s['AlternateTargets']=[]; s['SearchAliases']=[target]; s['Status']=status; s['Validation']=validation
    s['Occurrences']=occs; s['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in occs)); s['Note']=f'{len(set(s["SourceTexts"]))} distinct work IDs selected after complete-context review.'
    s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(occs)+1)]
    s['DraftEvidence']['IndependentWorkIds']=[f'work:{Path(x).stem}' for x in s['SourceTexts']]

review_doc=json.loads((R/REVIEW).read_text())
before={x['ordinal']:x['reviewedEntrySha256'] for x in review_doc['entries']}
assert before[1045]==IMMUTABLE_1045
assert sha(R/'fresh-build/entries/t_76ee526a2b16'/'entry.v2.json')==IMMUTABLE_1077
changed=[]; roster=[]
for n,eid in IDS.items():
    if n==1045: continue
    b=R/'fresh-build/entries'/eid; wp=b/'evidence.draft.json'; w=json.loads(wp.read_text()); e=w['Entry']; s=e['Senses'][0]; os=s['Occurrences']
    if n==1041:
        actor(os[0],'the occasion heading compiled by Jiyun','compiler',proof='Jiyun is explicitly named as compiler, and the headword occurs in the communal-tea occasion line before Ruibai Mingxue’s following speech.')
        named(os[2],'Chaozong Tongren',proof='普茶 occurs in the narrator’s occasion frame 一晚普茶次; the following case and comment are Chaozong Tongren’s speech.')
        actor(os[2],'the narrated occasion in Chaozong Tongren’s record','compiler',proof='The exact headword labels the narrated evening-tea occasion; Chaozong Tongren speaks only after that frame.')
        actor(os[3],'the occasion heading compiled by Longqi and collaborators','compiler',proof='Longqi and collaborators are explicitly named as compilers; the communal-tea wording is their occasion frame before the question and answer.')
        actor(os[4],'the compiler’s occasion heading before the address','compiler',proof='The exact headword is the communal-tea occasion label before an address; the direct speech begins only after that label.')
        explain(s,'Communal tea as an announced occasion for the assembled community.','The exact word usually belongs to a compiler or narrator’s occasion line—retreat tea, evening tea, or a tea-before-address heading—while the following speech belongs to the named master; one dialogue also names communal tea as a reason for sounding the drum.')
    elif n==1042:
        actor(os[0],'the unnamed verse author','verse-author',status='reviewed-unnamed',proof='The exact word occurs in a verse introduced by “verse says”; the surviving frame does not identify a safer personal author for that verse.')
        actor(os[5],'Yang Yi','compiler',status='identified-non-master',proof='The source explicitly signs this preface as written by Yang Yi; the headword occurs in his authored overview.')
        explain(s,'The chick pecking from within as the hen pecks from outside.','The records apply the pair to precisely matched response. Lianyue Daozheng defines it explicitly: the chick pecks, the mother pecks, and neither comes before or after the other; the other sayings and verse develop that simultaneity as encounter timing.')
    elif n==1043:
        named(os[1],'Xuefeng Dazhi',proof='The section is headed for Xuefeng Dazhi; this is the master who delivers the “wild-fox spirit” rebuke.')
        named(os[3],'Chongxian Qi',proof='The headword occurs in Chongxian Qi’s named comment on the Puyan–Puxian case, after the original case narration.')
        named(os[4],'Yuanwu Keqin',proof='Within the Mazu anthology section, the exact phrase occurs in the extended comment explicitly introduced as Yuanwu Keqin’s, not in Mazu’s speech.')
        named(os[5],'Chongxian Qi',proof='The parallel collection explicitly introduces this exact headword-bearing comment as Chongxian Qi’s; Baiyan Fu’s later comment begins afterward.')
        explain(s,'A “wild-fox spirit,” used as an accusation or cutting verdict.','Direct encounters use it as a rebuke aimed at an interlocutor; later named commentators also cite it as a stock verdict or reject a “wild-fox-spirit view.” The observable force is the rebuke and the accused interpretation, without assigning a hidden motive.')
    elif n==1044:
        actor(os[0],'the compilation narrator of the Dongpo–Foyin exchange','compiler',proof='The exact name occurs in narrated case setup; Dongpo and Foyin then speak as case participants.')
        actor(os[1],'the paratext title','compiler',proof='The name appears only in the title of a note on Layman Dongpo’s Great Compassion Pavilion record, before the authored prose; it is not a Yuanwu utterance.')
        actor(os[2],'the compilation narrator quoting Dongpo’s verse','compiler',proof='The narrator reports a master raising Dongpo’s verse and then records a responsive verse; the headword identifies Dongpo in that narrative.')
        actor(os[3],'the compilation narrator of the Dongpo–Foyin exchange','compiler',proof='The parallel source narrates the robe exchange and identifies Dongpo as a case participant.')
        explain(s,'Layman Dongpo, the poet-official Su Shi as represented in Chan sources.','The name appears across distinct genres: narrated Dongpo–Foyin exchanges, a paratext title, narration surrounding a quoted Dongpo verse, and This’an Shoujing’s direct comparison of Dongpo with another case figure.')
    elif n==1046:
        if len(e['Senses'])>1:
            bypath={o['RelPath']:o for ss in e['Senses'] for o in ss['Occurrences']}
            os=[bypath[p] for p in ['C/C077/C077n1710.xml','X/X82/X82n1571.xml','X/X84/X84n1583.xml','X/X84/X84n1585.xml','X/X84/X84n1579.xml','D/D48/D48n8939.xml']]
            s=e['Senses'][0]
        actor(os[2],'the biography narrator','compiler',proof='The robe is narrated as an imperial bestowal together with the title “Buddha Eye”; it is not direct speech by the biography’s subject.')
        actor(os[3],'the transmission-biography narrator','compiler',proof='The narrator reports a robe, bamboo slip, and portrait sent within a teacher–disciple transmission biography.')
        actor(os[4],'the transmission-biography narrator','compiler',proof='This parallel biography narrates the same robe, bamboo slip, and portrait transmission.')
        actor(os[5],'the compiler’s occasion heading','compiler',proof='The exact word occurs in the heading for a hall address after a patron presented a teaching robe; the direct speech begins in the following sermon.')
        lineage=copy.deepcopy(s); ceremonial=copy.deepcopy(s)
        refresh(lineage,'the lineage or teaching robe',[os[i] for i in (0,1,3,4)])
        explain(lineage,'A robe displayed or transmitted in a lineage setting.','Two masters lift the robe in formal acts, while two transmission biographies narrate a robe sent with a bamboo slip and portrait. These sources use the garment as an enacted or narrated lineage object, without claiming that possession uniformly equals realization.')
        refresh(ceremonial,'a robe bestowed or donated for a formal occasion',[os[i] for i in (2,5)])
        explain(ceremonial,'A ceremonial robe bestowed by the court or donated by a patron.','One biography narrates an imperial gift of a gold-patterned robe and title; another source places a patron’s donated robe in the heading for the ensuing hall address.')
        e['Senses']=[lineage,ceremonial]
    elif n==1047:
        named(os[0],'Juelang Daosheng',proof='The passage belongs to Juelang Daosheng’s authored Treatise Taking Fire as the Principle and invokes Fuxi in its fire-and-cosmology argument.')
        named(os[1],'Langting Ting',proof='The exact word occurs in Langting Ting’s direct informal address, where he calls Fuxi’s sixty-four hexagrams “the mystery within the mystery.”')
        actor(os[2],'Nanqian','compiler',status='identified-non-master',proof='The source explicitly signs the afterword as respectfully written by Nanqian; the headword occurs in Nanqian’s authored afterword.')
        explain(s,'Fuxi, the ancient culture hero invoked in Chan-era cosmology and accounts of cultural beginnings.','Juelang Daosheng links Fuxi’s dragon-horse diagram and the sixty-four hexagrams to fire cosmology; Langting Ting invokes those hexagrams; Nanqian discusses the institution of writing; other sayings place Fuxi amid primordial beginnings or revealed signs.')
    elif n==1048:
        named(os[0],'Baizhang Huaihai',proof='The continuous discourse belongs to Baizhang Huaihai’s old-sayings record; there is no embedded speaker takeover at the clause about seeing one’s own buddha-nature.')
        actor(os[3],'Ding Libiao','compiler',status='identified-non-master',proof='The source explicitly signs the Linggu Record preface as written by Ding Libiao; the headword occurs in his first-person preface narrative, not Juelang Daosheng’s speech.')
        named(os[6],'Tianzhu Chonghui',proof='The Tianzhu Chonghui section assigns the direct answer about one’s own share to Tianzhu Chonghui.')
        explain(s,'Oneself—one’s own person or what belongs to one rather than another.','The sources ask what is one’s own self, speak of seeing one’s own buddha-nature, contrast using another person’s material as one’s own, and oppose one’s own share or things to what belongs to someone else. The gloss stays with those observable contrasts.')
    elif n==1049:
        if len(e['Senses'])>1:
            bypath={o['RelPath']:o for ss in e['Senses'] for o in ss['Occurrences']}
            os=[bypath[p] for p in ['X/X82/X82n1571.xml','J/J34/J34nB311.xml','X/X70/X70n1390.xml','X/X64/X64n1260.xml','X/X84/X84n1583.xml','X/X70/X70n1376.xml']]
            s=e['Senses'][0]
        explain(s,'A living road: an opening or route through which movement remains possible.','Formal addresses declare or request an open road, an exit, or room to proceed; these uses do not require a preceding claim that every fixed route has closed.')
        special=copy.deepcopy(s); main=copy.deepcopy(s)
        refresh(main,'a living road or way through',[os[i] for i in (0,1,2,3,5)])
        explain(main,'A living road: an opening or route through which movement remains possible.','Formal addresses declare or request an open road, an exit, or room to proceed; these uses do not require a preceding claim that every fixed route has closed.')
        refresh(special,"Shandao’s direct route",[os[4]],validation='single-source',status='provisional')
        explain(special,"The route directly pointed out by Shandao.","Quanan Qiji calls this particular direct course a living road and says Shandao pointed it out directly, while challenging the assembly for passing it by face to face; this single-work referent remains provisional.")
        e['Senses']=[main,special]
    elif n==1050:
        actor(os[2],'Ruxi','person-described',status='identified-non-master',proof='The first-person autobiographical passage identifies its subject and author as Ruxi and narrates his receiving the novice’s ten precepts.')
        actor(os[4],'the procedural monastic-code voice','compiler',proof='The headword occurs in procedural admonitions for novices, prescribing the sequence from five precepts to ten precepts.')
        refresh(s,"the novice’s ten precepts",os)
        explain(s,"The novice’s ten precepts.",'The sources explicitly call them the novice’s ten precepts or discuss receiving them in novice ordination and discipline: teaching histories, autobiography, an ordination question, and procedural monastic-code prose all keep the target specific to novices.')
    wp.write_text(json.dumps(w,ensure_ascii=False,indent=2)+'\n')
    if len(e['Senses'])>1:
        work=b/'WORK.md'; text=work.read_text() if work.exists() else f'# {e["SourceTerm"]}\n'
        marker='sense-target-distinguishability:'
        if marker not in text:
            targets='; '.join(x['PreferredTarget'] for x in e['Senses'])
            text += f'\n- {marker} the retained targets denote different referents ({targets}); rhetorical variation alone is not split.\n'
            work.write_text(text)
    ep=b/'entry.v2.json'; rp=b/'b1041-1050-delta-repair-compile.json'
    q=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(wp),'--output',str(ep),'--report',str(rp)],text=True,capture_output=True)
    if q.returncode: raise SystemExit(q.stdout+q.stderr)
    changed.append({'ordinal':n,'id':eid,'term':e['SourceTerm'],'beforeEntrySha256':before[n],'afterEntrySha256':sha(ep),'worksheetSha256':sha(wp),'occurrences':sum(len(x['Occurrences']) for x in e['Senses']),'compileHardPass':True})
    for ss in e['Senses']:
        for o in ss['Occurrences']:
            for cm in o.get('ContextMasters',[]): roster.append((cm['MasterName'],o))

assert sha(R/'fresh-build/entries'/IDS[1045]/'entry.v2.json')==IMMUTABLE_1045
assert sha(R/'fresh-build/entries/t_76ee526a2b16'/'entry.v2.json')==IMMUTABLE_1077
pp=R/'fresh-build/pending-roster.json'; pd=json.loads(pp.read_text()); have={x['canonicalName'] for x in pd['candidates']}
for name,o in roster:
    if name not in have:
        pd['candidates'].append({'canonicalName':name,'aliases':[name],'evidence':[{k:o[k] for k in ('RelPath','FromLb','ToLb','Kwic')}],'reviewedBy':'Codex f004 checkpoint1 delta repair','reviewReport':REVIEW,'status':'awaiting-roster-integration'}); have.add(name)
pp.write_text(json.dumps(pd,ensure_ascii=False,indent=2)+'\n')
cp=H/'f004-b1041-1050-independent-rereview-delta-author-checkpoint.json'
payload={'schemaVersion':1,'generatedUtc':NOW,'sourceReview':REVIEW,'sourceReviewSha256':REVIEW_SHA,'scope':{'repairedOrdinals':[x['ordinal'] for x in changed],'immutableKeepOrdinal':1045,'untouchedLaterCheckpoints':True},'entries':changed,'immutableAssertions':{'1045EntrySha256':IMMUTABLE_1045,'1077EntrySha256':IMMUTABLE_1077},'selfReview':False,'promoted':False}
cp.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'checkpoint':str(cp),'sha256':sha(cp),'changed':len(changed),'occurrences':sum(x['occurrences'] for x in changed)}))
