from pathlib import Path
import datetime, hashlib, json, subprocess, sys

R = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(R))
NOW = '2026-07-15T19:35:00Z'
REVIEW = 'f005-laneA-1203-1217-independent-semantic-review.json'

def load(eid):
    p = R/'fresh-build/entries'/eid/'evidence.draft.json'
    d = json.loads(p.read_text())
    return p, d, d['Entry']['Senses'][0]

def unnamed(o, label, kind, grammar, contexts):
    o['MasterName'] = None
    o['ContextMasters'] = contexts
    o['ActorAttribution'] = {
        'Status': 'reviewed-unnamed', 'Kind': kind, 'ActorLabel': label,
        'ActorRole': 'questioner', 'GrammarEvidence': grammar,
        'RungsChecked': ['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],
        'ReviewedBy': 'Codex f005 lane A independent-review repair',
        'ReviewedUtc': NOW,
    }

def named_nonmaster(o, label, role, grammar, contexts):
    o['MasterName'] = None
    o['ContextMasters'] = contexts
    o['ActorAttribution'] = {
        'Status': 'identified-non-master', 'Kind': 'named story-figure speaker',
        'ActorLabel': label, 'ActorRole': role, 'GrammarEvidence': grammar,
        'RungsChecked': ['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],
        'ReviewedBy': 'Codex f005 lane A independent-review repair',
        'ReviewedUtc': NOW,
    }

def narrated(o, grammar, contexts):
    o['MasterName'] = None
    o['ContextMasters'] = contexts
    o['ActorAttribution'] = {
        'Status': 'narrated', 'Kind': 'compiler commentary',
        'ActorLabel': 'the unplaceable compiler-commentator', 'ActorRole': 'compiler',
        'GrammarEvidence': grammar,
        'RungsChecked': ['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],
        'ReviewedBy': 'Codex f005 lane A independent-review repair',
        'ReviewedUtc': NOW,
    }

changed=[]

# 1203 耳裏著水
p,d,s=load('t_321a547de25f'); o=s['Occurrences'][2]
o['MasterName']='Wanru Tongwei'; o['ContextMasters']=[{'MasterName':'Wanru Tongwei','Roles':['utterer','record-owner']}]
o['AttributionNote']='Complete hall discourse in the Full Compendium of the Five Lamps: Wanru Tongwei, who refers to himself by a doubled personal form in this case, utters the exact comparison.'
o['DraftActorProof'].update(GrammaticalSubject='Wanru Tongwei',SpeechFrame='Wanru Tongwei owns the uninterrupted hall discourse and utters the headword clause.',FullCaseDecision='The section and self-reference resolve the utterer as Wanru Tongwei, not Jueyin.')
s['ExplanationParts']['EvidenceBody'][2]='Wanru Tongwei applies the water image to Xiangyan’s hearing bamboo strike, alongside dust entering Lingyun’s eye.'
changed.append((p,d))

# 1204 鼻孔遼天
p,d,s=load('t_138ca8036367'); o=s['Occurrences'][4]
unnamed(o,'the unnamed monk questioning Feiyin Tongrong','monastic questioner','僧問 owns the headword-bearing alternative question; 師云 introduces Feiyin Tongrong’s answer afterward.',[{'MasterName':'Feiyin Tongrong','Roles':['respondent','record-owner']}])
o['AttributionNote']='Complete exchange in Feiyin Tongrong’s record: an unnamed monk utters the headword in the opening question; Feiyin responds afterward.'
o['DraftActorProof'].update(GrammaticalSubject='the unnamed monk questioner',SpeechFrame='僧問 owns the exact clause.',FullCaseDecision='Feiyin Tongrong is respondent, not headword utterer.')
s['ExplanationParts']['CorpusEarnedOpening']='Nostrils reaching the sky is an extravagant bodily picture used as a conspicuous appraisal. Chan records put it into an answer, a self-image, an unnamed monk’s alternative question, and a challenge whose appearance would put Tianyi Yihuai’s own life at stake; those varied jobs do not establish one hidden psychological state.'
changed.append((p,d))

# 1205 頂門具眼
p,d,s=load('t_640e09aef544')
s['ExplanationParts']['EvidenceBody'][2]='Juelang Daosheng places the crown eye beside a tally hanging behind the elbow in a public address about testing the assembly.'
o=s['Occurrences'][3]
o['AttributionNote']='The source record or case collection (天界覺浪盛禪師全錄; J/J34/J34nB311.xml). Exact actor: Juelang Daosheng, the MasterName-linked record owner, composed this line in his public address.'
changed.append((p,d))

# 1208 口似血盆
p,d,s=load('t_fd83eaebf6ad')
for idx,master,title in [(3,'Dahui Zonggao','Dahui Zonggao’s formal discourse'),(5,'Xutang Zhiyu','Xutang Zhiyu’s record')]:
    o=s['Occurrences'][idx]
    unnamed(o,f'the unnamed monk questioning {master}','monastic questioner','The question marker assigns the exact sword-teeth and blood-basin phrase to the unnamed monk; the named master answers afterward.',[{'MasterName':master,'Roles':['respondent','record-owner']}])
    o['AttributionNote']=f'Complete exchange in {title}: an unnamed monk utters the headword-bearing question; {master} is the respondent.'
    o['DraftActorProof'].update(GrammaticalSubject='the unnamed monk questioner',SpeechFrame='The question turn owns the exact headword.',FullCaseDecision=f'{master} answers afterward and is contextual respondent only.')
changed.append((p,d))

# 1210 雞寒上樹鴨寒下水
p,d,s=load('t_f4fc42267d33'); o=s['Occurrences'][1]
o['MasterName']='Baling Haojian'; o['ContextMasters']=[{'MasterName':'Baling Haojian','Roles':['utterer']},{'MasterName':'Zhenjing Kewen','Roles':['later-raiser','record-owner']}]
o['AttributionNote']='Zhenjing Kewen raises this with an inherited-saying marker in his record; the independent old case and parallel passage identify Baling Haojian as the source utterer.'
o['DraftActorProof'].update(GrammaticalSubject='Baling Haojian in the raised old saying',SpeechFrame='古人云 marks a quotation raised by Zhenjing Kewen.',FullCaseDecision='Parallel-case resolution assigns the saying to Baling; Zhenjing is later raiser.')
changed.append((p,d))

# 1213 腰纏十萬貫
p,d,s=load('t_114ad0f001c1'); o=s['Occurrences'][3]
named_nonmaster(o,'King Without Cover','utterer','The formal ruling marker introduces the king’s quoted ruling; the conditional promise is inside that speech.',[{'MasterName':'Juelang Daosheng','Roles':['later-quoter','record-owner']}])
o['AttributionNote']='The record owner frames and quotes King Without Cover’s story ruling; the named king is the exact non-master utterer of the conditional promise.'
o['DraftActorProof'].update(GrammaticalSubject='the allegorical King Without Cover',SpeechFrame='The headword lies inside 斷曰 quoted speech.',FullCaseDecision='Juelang is later quoter, not grammatical utterer.')
o=s['Occurrences'][4]; o['MasterName']='Tianran Hanshi'; o['ContextMasters']=[{'MasterName':'Tianran Hanshi','Roles':['utterer','record-owner']}]
o['AttributionNote']='The titled reply-letter in the Recorded Sayings of Chan Master Tianran Hanshi is authored by Tianran Hanshi, who utters the exact saying.'
o['DraftActorProof'].update(GrammaticalSubject='Tianran Hanshi',SpeechFrame='The titled letter belongs to Tianran Hanshi’s own record.',FullCaseDecision='Tianran Hanshi is the source author; Baochi Jizong has no alias evidence here.')
s['ExplanationParts']['EvidenceBody'][3]='Juelang Daosheng quotes the story king’s conditional promise that everyone may ride to Yangzhou.'
s['RelatedMasters']=[x for x in s.get('RelatedMasters',[]) if x!='Baochi Jizong']
if 'Tianran Hanshi' not in s['RelatedMasters']: s['RelatedMasters'].append('Tianran Hanshi')
s['ExplanationParts']['EvidenceBody'][4]='Tianran Hanshi invokes the saying in a letter against refusing to relinquish anything while hoping to gain everything.'
changed.append((p,d))

# 1214 針劄不入
p,d,s=load('t_38586eed0d08'); o=s['Occurrences'][5]
o['MasterName']='Chushi Fanqi'; o['ContextMasters']=[{'MasterName':'Chushi Fanqi','Roles':['utterer']},{'MasterName':'Lüyan He','Roles':['later-raiser','commentator','record-owner']}]
o['AttributionNote']='Lüyan He explicitly introduces a saying by Chushi Fanqi and quotes the headword-bearing line; Chushi is the source utterer and Lüyan the later raiser/commentator.'
o['DraftActorProof'].update(GrammaticalSubject='Chushi Fanqi in the explicit quotation',SpeechFrame='楚石和尚道 introduces the quotation.',FullCaseDecision='Chushi owns the headword; Lüyan comments only after the quote closes.')
s['ExplanationParts']['EvidenceBody'][5]='Lüyan He explicitly quotes Chushi Fanqi’s use of the phrase for the ten directions of empty space.'
changed.append((p,d))

# 1216 風吹不入
p,d,s=load('t_a14bd52beff8'); o=s['Occurrences'][5]
narrated(o,'After Weishan’s quoted remark closes, the headword-bearing sentence about Daci is unquoted compiler-commentarial adjudication.',[{'MasterName':'Weishan Lingyou','Roles':['person-discussed']}])
o['AttributionNote']='The Chan Compendium (宗鑑法林) records an unplaceable compiler-commentator’s adjudication about Daci after Weishan Lingyou’s quoted remark has closed; neither master utters the exact clause.'
o['DraftActorProof'].update(GrammaticalSubject='the unplaceable compiler-commentator',SpeechFrame='Unquoted adjudication follows the closed Weishan quotation.',FullCaseDecision='MasterName is null; Weishan and Daci are contextual figures.')
o=s['Occurrences'][6]; o['MasterName']="Ying'an Tanhua"; o['ContextMasters']=[{'MasterName':"Ying'an Tanhua",'Roles':['utterer','record-owner']}]
o['AttributionNote']="The Buddha-birthday hall address in the Recorded Sayings of Chan Master Ying'an Tanhua is uninterrupted speech by Ying'an Tanhua."
o['DraftActorProof'].update(GrammaticalSubject="Ying'an Tanhua",SpeechFrame="The section heading opens Ying'an’s hall address.",FullCaseDecision="Ying'an, not Dahui Zonggao, utters the exact clause.")
s['ExplanationParts']['EvidenceBody'][5]='An unplaceable compiler-commentator in the Chan Compendium joins the raw-iron image to wind-imperviousness, while Yingan Tanhua uses the line in his own Buddha-birthday address.'
changed.append((p,d))

# 1217 荊棘林
p,d,s=load('t_b0df4ae7015d')
o=s['Occurrences'][1]; o['MasterName']='Liao’an Qingyu'; o['ContextMasters']=[{'MasterName':'Liao’an Qingyu','Roles':['utterer','record-owner']}]
o['AttributionNote']='The section explicitly opens a founding-master memorial hall address for Liao’an Qingyu; he owns the following discourse and utters the exact formula.'
o['DraftActorProof'].update(GrammaticalSubject='Liao’an Qingyu',SpeechFrame='The explicit occasion heading governs the uninterrupted address.',FullCaseDecision='Liao’an, not Yunmen, is this occurrence’s utterer.')
o=s['Occurrences'][2]
import zc
text,_=zc._load(o['RelPath']); starts=[]; at=0
while True:
    at=text.find('荊棘林',at)
    if at<0: break
    starts.append(at); at+=3
pos=starts[1]; radius=20
while True:
    kw=text[max(0,pos-radius):pos+3+radius]; v=zc.verify(o['RelPath'],kw)
    if v['ok'] and v['count']==1: break
    radius+=8
o.update(FromLb=v['fromLb'],ToLb=v['toLb'],Kwic=kw,MasterName='Foyan Qingyuan',ContextMasters=[{'MasterName':'Yunmen Wenyan','Roles':['later-quoter','case-figure']},{'MasterName':'Foyan Qingyuan','Roles':['utterer','record-owner']}])
o['AttributionNote']='Foyan Qingyuan first raises Yunmen’s older formula, then independently says that this very whisk is the thorn thicket; this recut KWIC anchors Foyan’s exact turn only.'
o['DraftActorProof'].update(GrammaticalSubject='Foyan Qingyuan',SpeechFrame='師云 introduces the recut second headword token.',FullCaseDecision='The overlapping Yunmen token was excluded; this row stores only Foyan’s whisk declaration.')
o=s['Occurrences'][3]
unnamed(o,'the unnamed monk questioning Yunju Daoqi','monastic questioner','問 introduces the headword-bearing request; 師曰 introduces Yunju Daoqi’s answer afterward.',[{'MasterName':'Yunju Daoqi','Roles':['respondent','record-owner']}])
o['AttributionNote']='An unnamed monk asks the thorn-thicket question; Yunju Daoqi is the responding section master and does not repeat the headword.'
o['DraftActorProof'].update(GrammaticalSubject='the unnamed monk questioner',SpeechFrame='問 owns the exact clause.',FullCaseDecision='Yunju is respondent, not headword utterer.')
o=s['Occurrences'][7]; o['MasterName']='Yunmen Wenyan'; o['ContextMasters']=[{'MasterName':'Yunmen Wenyan','Roles':['utterer','case-figure']},{'MasterName':'Shending Yikui','Roles':['later-raiser','record-owner']}]
o['AttributionNote']='Shending Yikui raises the old formula with an inherited-saying marker; parallel cases resolve its source utterer as Yunmen Wenyan. Shending is the later raiser.'
o['DraftActorProof'].update(GrammaticalSubject='Yunmen Wenyan in the raised old saying',SpeechFrame='不見道 marks the inherited quotation.',FullCaseDecision='Parallel-passage resolution identifies Yunmen; Shending raises it later.')
s['ExplanationParts']['EvidenceBody'][1]='Liao’an Qingyu repeats the level-ground and thorn-thicket formula in a memorial hall address.'
s['ExplanationParts']['EvidenceBody'][2]='Foyan Qingyuan raises Yunmen’s formula, then declares that the whisk itself is the thorn thicket and asks how anyone crosses it.'
s['ExplanationParts']['EvidenceBody'][3]='An unnamed monk asks Yunju Daoqi to clear a road through the thicket; Yunju asks where he intends to go.'
s['ExplanationParts']['EvidenceBody'].append('Shending Yikui later raises Yunmen’s old thorn-thicket formula in a release-day address.')
changed.append((p,d))

# Remove the false pending-roster identity invented solely for the Tianran letter.
pending=R/'fresh-build/pending-roster.json'; pd=json.loads(pending.read_text())
pd['candidates']=[x for x in pd['candidates'] if x.get('canonicalName')!='Baochi Jizong']
pending.write_text(json.dumps(pd,ensure_ascii=False,indent=2)+'\n')

for p,d in changed:
    # Rule 10: every modified attribution names its source text and path.
    for sense in d['Entry']['Senses']:
        for o in sense['Occurrences']:
            title=zc.title(o['RelPath'])
            if title not in o['AttributionNote']:
                o['AttributionNote']=f'Source text ({title}; {o["RelPath"]}). '+o['AttributionNote']
    p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
    out=p.parent/'entry.v2.json'; report=p.parent/'evidence-compile-report.json'
    q=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(out),'--report',str(report)],text=True,capture_output=True)
    if q.returncode: raise SystemExit(q.stdout+q.stderr)
print(json.dumps({'repaired':[p.parent.name for p,_ in changed]},ensure_ascii=False))
