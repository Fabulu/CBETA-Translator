from pathlib import Path
import datetime, hashlib, json, subprocess, sys

R=Path(__file__).resolve().parents[2]; H=R/'fresh-build/waves'; sys.path.insert(0,str(R)); import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat()
ROWS={926:'t_d0a4a5271135',927:'t_4e2840f46db3',928:'t_454f6f606450',929:'t_72fd192e30c4',930:'t_6ce996d17a55',931:'t_aa45c307e9f1',932:'t_601e936dc0a3',933:'t_b4c37e2f25c3',934:'t_7ca97d96fc84',935:'t_32076ca13bb7'}
RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']

N=lambda name:("named",name)
U=lambda label,role='compiler',contexts=():("other",label,role,list(contexts))
ACT={
926:[N('Caotang Shanqing'),U('the unnamed monastic questioner','questioner',['Tiantai Deshao']),N('Dadian Baotong'),N('Butui Yong'),N('Shoushan Xingnian'),U('the author of the Nanquan verse in the Chan verse anthology','verse-author',['Nanquan Puyuan']),N('Wansong Xingxiu')],
927:[U('the Chixiu Baizhang Qinggui contents editor'),U('the Chixiu Baizhang Qinggui rule compiler'),N('Foyan Qingyuan'),U('the Wudeng Quanshu biographer','compiler'),N('Miyin'),U('the author of the Peng-family sleeping-hall donation address','compiler'),U('the Miyun Yuanwu annalist','compiler',['Miyun Yuanwu'])],
928:[N('Xinwen Tanben'),N('Yunxi Ting'),N('Yuanmi'),N('Tianyin Yuanxiu'),N('Tianran Hanshi')],
929:[N('Xuefeng Zongyan'),N("Lia'an Qingyu"),U('the signed preface author of the Recorded Sayings of Feiyin','compiler',['Feiyin Tongrong']),N('Yuanwu Keqin'),U('Yan Dacan, the preface author','compiler'),N('Guishan Zhe'),N('Mingjue Cong')],
930:[N('Huailian'),U('the unnamed monastic questioner','questioner',['Guxue Zhe']),N('Feiyin Tongrong'),U('the author of the letter to the Jinli Chan community','compiler'),U('the unnamed monastic questioner','questioner'),U('the authors of the Huayan memorial','compiler'),U('the signed preface author of the Recorded Sayings of Hongjue','compiler')],
931:[U('the Chixiu Baizhang Qinggui institutional editor'),U('the Chixiu Baizhang Qinggui rule compiler'),U('the Chanyuan Qinggui rule compiler'),U('the Liezu Tigang Lu institutional editor'),N("Lia'an Qingyu"),N('Mian Xianjie'),N('Fojian Huiqin')],
932:[N('Juelang Daosheng'),N('Zhuanyu Guanheng'),U("Jingqi, the preface author",'compiler',["Zhe'an Jingfan"]),N('Jifei Ruyi'),N('Juelang Daosheng'),U('the preface author of Chanyu Neiji','compiler',['Yongjue Yuanxian']),N('Kangxi Emperor')],
933:[N("Meng'an Yuancong"),N('Zhaozhou Congshen'),N("Zhe'an Jingfan"),U('Zongze, compiler of Chanyuan Qinggui','compiler'),N('Zhaozhou Congshen'),U('Daoqian, compiler of Dahui Pujue Chanshi Zongmen Wuku','compiler'),N('Zhaozhou Congshen')],
934:[N('Hanyue Fazang'),N('Dongshan Jue'),N('Juelang Daosheng'),N('Dahui Zonggao'),U('the unnamed monastic questioner','questioner',['Yongzheng Emperor']),N('Yunju Yuan'),N('Juelang Daosheng')],
935:[U("Zhuo'an Fanfu, the preface author",'compiler',['Juelang Daosheng']),N('Muyun Tongmen'),N('Juelang Daosheng'),U('Xiong Shaozai, the petition author','compiler',['Guxue Zhe']),N('Poshan Haiming'),N('Feiyin Tongrong'),N('Zhufeng Min')]
}

def recut_at(o,ctx=110):
    hits=zc.find(o['RelPath'],o.get('Kwic') or '',ctx=ctx)
    # Bare-token witnesses can recur; preserve the original line identity.
    hit=next((h for h in hits if h.get('fromLb')==o.get('FromLb')),None)
    if hit is None:
        term=o.get('_term')
        hits=zc.find(o['RelPath'],term,ctx=ctx)
        hit=next((h for h in hits if h.get('fromLb')==o.get('FromLb')),None)
    if hit is None: raise RuntimeError((o['RelPath'],o.get('FromLb'),o.get('Kwic')))
    o['Kwic']=hit['window']; v=zc.verify(o['RelPath'],o['Kwic']); assert v['ok']
    o['FromLb'],o['ToLb']=v['fromLb'],v['toLb']

def set_actor(o,spec):
    if spec[0]=='named':
        name=spec[1]; o['MasterName']=name; o.pop('ActorAttribution',None)
        o['ContextMasters']=[{'MasterName':name,'Roles':['utterer']}]
        proof=f'The complete source unit places the exact headword-bearing wording in {name}’s own speech or authored comment.'
    else:
        label,role,contexts=spec[1:]; o['MasterName']=None
        o['ContextMasters']=[{'MasterName':x,'Roles':['section-subject']} for x in contexts]
        proof=f'The complete source unit assigns the exact wording to {label}; it is not direct speech by the contextual master.'
        # A role-only label is not an identified human.  Keep it explicitly
        # reviewed-unnamed after the six-rung search; only source-supplied names
        # (Zongze, Daoqian, Yan Dacan, etc.) qualify as identified-non-master.
        status='reviewed-unnamed' if label.startswith('the ') else 'identified-non-master'
        o['ActorAttribution']={'Status':status,'Kind':label,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':proof,'ReviewedBy':'Codex f004 A926-935 reviewer11 author repair','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True}
    who=o.get('MasterName') or o['ActorAttribution']['ActorLabel']
    o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {who}. {proof}'
    o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':who,'SpeechFrame':proof,'FullCaseDecision':proof}

OPEN={
926:('stone person','An impossible stone person listens, answers, burns his feet, claps, or carries news; these impossible actions test whether a response is alive rather than merely conceptual.'),
927:("abbot’s private quarters",'The sleeping hall is the abbot’s private room: rules stage departures from it, biographies make it a death chamber, builders construct it, and masters turn its moonlit privacy into teaching imagery.'),
928:("Yunmen’s three phrases",'The three attested phrases are “covering heaven and earth,” “cutting off the manifold streams,” and “following the waves”; later masters quote, collect, and criticize this triad.'),
929:('hammer and tongs','The smith’s hammer and tongs become an image for forceful Chan training: blows, pressure, and tempering figure the testing that transforms a student.'),
930:('raised teaching seat','The lion seat is the raised platform for formal assembly teaching; questions mark the ascent, while letters and petitions praise or request that public role.'),
931:('senior monastic officers','The head officers form the west-rank senior monastic administration: rules govern their appointment and movement, and public addresses distinguish their responsibility from that of stewards and the assembly.'),
932:('continuity of the teaching lineage','The compound names the continued life of awakened teaching: masters vow, admonish, and give thanks so that the Buddha-lineage is not cut off, while prefaces and imperial prose fear precisely that rupture.'),
933:('latrine','The east privy is both a regulated monastic facility and the deliberately indecorous setting of Zhaozhou’s exchange with Wenyuan: “One cannot speak the awakened teaching to you on the latrine.”'),
934:('become Buddha on the spot','The phrase promises immediate buddhahood, but the records test it against killing, the butcher dropping his knife, a questioner seeking permission, and the demand for a decisive present response.'),
935:('lineage nourishment','The metaphor is nourishment personally received from one’s lineage source: named masters acknowledge it from the teaching seat or incense burner, while prefaces and petitions describe its transmission as gratitude and continuity.')}

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()

# These two names are explicit in the cited clauses but absent from the public
# roster.  Register source-bound candidates so strict compilation does not
# replace an attested speaker with an anonymous record owner.
pp=R/'fresh-build/pending-roster.json'; pending=json.loads(pp.read_text()); have={x['canonicalName'] for x in pending['candidates']}
for name,eid,idx,aliases in [('Butui Yong',ROWS[926],4,['不退勇']),('Yunju Yuan',ROWS[934],6,['雲居元禪師','雲居元'])]:
    if name not in have:
        o=json.loads((R/'fresh-build/entries'/eid/'evidence.draft.json').read_text())['Entry']['Senses'][0]['Occurrences'][idx-1]
        pending['candidates'].append({'canonicalName':name,'aliases':aliases,'evidence':[{k:o[k] for k in ('RelPath','FromLb','ToLb','Kwic')}],'reviewedBy':'Codex f004 A926-935 reviewer11 author repair','reviewReport':'fresh-build/waves/f004-laneA-926-935-reviewer11-independent.json','status':'awaiting-roster-integration'})
        have.add(name)
pp.write_text(json.dumps(pending,ensure_ascii=False,indent=2)+'\n')

# First widen every mechanically bare witness and the specific clipped witnesses named by reviewer11.
WIDEN={926:[2],927:[1,3,6],928:[3,4],931:[1,2,3,4],933:[1,2,3,4,5,6,7],934:[4]}
out=[]
for n,eid in ROWS.items():
    b=R/'fresh-build/entries'/eid; wp=b/'evidence.draft.json'; data=json.loads(wp.read_text()); e=data['Entry']; os=e['Senses'][0]['Occurrences']
    for o in os: o['_term']=e['SourceTerm']
    for idx in WIDEN.get(n,[]):
        if len(os[idx-1]['Kwic']) < 250: recut_at(os[idx-1],150 if n in {928,933} else 110)
    for o in os: o.pop('_term',None)
    for o,spec in zip(os,ACT[n]): set_actor(o,spec)
    target,opening=OPEN[n]
    for s in e['Senses']:
        s['PreferredTarget']=target; s['Explanation']=opening
        s['ExplanationParts']={'CorpusEarnedOpening':opening,'EvidenceBody':['The stored complete witnesses distinguish direct master speech, questions, institutional rules, narration, quotation, and signed paratext rather than flattening them into record ownership.']}
        s['DraftEvidence']['ZenBend']=opening
        s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(os)+1)]
    data['Entry']=e; wp.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n')
    ep=b/'entry.v2.json'; report=b/'a926-935-reviewer11-repair-compile.json'
    q=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(wp),'--output',str(ep),'--report',str(report)],text=True,capture_output=True)
    if q.returncode: raise SystemExit(q.stdout+q.stderr)
    ce=json.loads(ep.read_text()); total=exact=0
    for s in ce['Senses']:
        for o in s['Occurrences']:
            total+=1; v=zc.verify(o['RelPath'],o['Kwic']); exact+=int(v['ok'] and v['fromLb']==o['FromLb'] and v['toLb']==o['ToLb'] and ce['SourceTerm'] in o['Kwic'])
    assert exact==total
    row={'ordinal':n,'id':eid,'term':ce['SourceTerm'],'occurrences':total,'exactKwicsAndSpans':exact,'entrySha256':sha(ep),'worksheetSha256':sha(wp),'compileHardPass':True,'reviewer11FindingsRepaired':True,'selfReview':False,'promoted':False}
    (H/f'f004-laneA-{n}-reviewer11-author-repair-checkpoint.json').write_text(json.dumps(row,ensure_ascii=False,indent=2)+'\n'); out.append(row)

ledger={'schemaVersion':1,'generatedUtc':NOW,'role':'author-repair-only','sourceReview':'f004-laneA-926-935-reviewer11-independent.json','entries':out,'occurrences':sum(x['occurrences'] for x in out),'exactKwicsAndSpans':sum(x['exactKwicsAndSpans'] for x in out),'selfReview':False,'promoted':False}
(H/'f004-laneA-926-935-reviewer11-author-repair-ledger.json').write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'entries':len(out),'occurrences':ledger['occurrences'],'exact':ledger['exactKwicsAndSpans']}))
