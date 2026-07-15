from pathlib import Path
import datetime,hashlib,json,subprocess,sys
R=Path(__file__).resolve().parents[2];H=R/'fresh-build/waves';sys.path.insert(0,str(R));import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
ROWS={1186:'t_851951c2b336',1187:'t_d3631f4abf25',1188:'t_cb09819b2297',1189:'t_9c0e7c40344c',1190:'t_428d6c64e37c',1191:'t_7f17cf4947f8',1192:'t_9f3c195d3834',1193:'t_adef287521cf',1194:'t_90cca5eb04c7',1195:'t_85ee3a3007c6'}
N=lambda x:('named',x);U=lambda label,role='compiler',ctx=():('other',label,role,list(ctx))
ACT={
1186:[N('Baima Ai'),N('Tianyin Yuanxiu'),N('Xueguan Zhiyin'),N('Yingning Jing')],
1187:[U('the unnamed verse author preserved by the Chan verse anthology','verse-author'),U('the unnamed verse author preserved by Zongjian Falin','verse-author'),U('the Wudeng Quanshu biographer','compiler'),N('Sanyi Mingyu'),U('Xilin Xuansu, the preface author','compiler')],
1188:[N('Dongshan Liangjie'),N('Yongjue Yuanxian'),U('the unnamed verse author preserved by the Chan verse anthology','verse-author'),N('Baiyun Zhizuo'),U('Zisai Yeren Xuezi, the exposition author','compiler'),N('Shiqi Tongyun')],
1189:[N('Feiyin Tongrong'),N('Ruibai Mingxue'),U('the signed preface author of the Recorded Sayings of Yinyuan','compiler',['Yinyuan Longqi']),N('Hanxiu Ruqian'),N('Mingjue Cong')],
1190:[N('Lingyun Zhiqin'),N('Shending Yikui'),N('Yunmen Wenyan'),N('Yunmen Wenyan')],
1191:[N('Miaozhan Wenzhao'),N('Huitong Qingdan'),N('Dahui Zonggao'),N('Fushan Fayuan'),N('Baoning Yuanji'),N('Baichi Xingyuan')],
1192:[N('Tiansheng Tai'),N('Zhuyu'),N('Qinghua Yin'),N('Baofu Congzhan'),N('Wuwai Wunian Yuanxin'),N('Juelang Daosheng')],
1193:[N('Eryin Mi'),N('Fushan Fayuan'),N('Fushan Fayuan'),N('Baizhang Yiqi')],
1194:[N('Caoshan Benji'),N('Xutang Zhiyu'),N('Baofu Qinghuo'),N("Zhe'an Jingfan")],
1195:[U('the unnamed Lengyan verse author preserved by Zongjian Falin','verse-author'),N('Liangshan Yuanguan'),N('Nanyan Sheng'),N("Zhe'an Jingfan"),N('Dabei Miaoyun')]
}
EXPL={
1186:'The frog at the bottom of a well performs impossible actions: Baima Ai makes it swallow the moon, while Xueguan Zhiyin makes it fly into the sky. Later masters raise and verse the image as a deliberately cramped scene whose action exceeds its setting.',
1187:'A snowflake entering a red-hot furnace vanishes without residue. Collected verses use that instantaneous disappearance to cap a case; Sanyi Mingyu makes it a condition maintained throughout the day, while Xilin Xuansu applies it to the rapid give-and-take of Dongshan-style exchanges.',
1188:'A lotus flourishing in fire names effective action under conditions that should destroy it. Dongshan Liangjie places the image in the five ranks, Baiyun Zhizuo gives “a lotus is born in fire” as a direct answer, and Yongjue Yuanxian turns it into advice for a lay practitioner.',
1189:'A pestle-tip sprouting flowers is an impossible event used as public teaching. Feiyin Tongrong includes it in a Buddha-birthday sequence, Ruibai Mingxue verses it as renewed color, Hanxiu Ruqian lists it among mind-produced impossibilities, and Mingjue Cong gives it as a direct answer.',
1190:'The river-loss saying sends the search back to the place of loss: if money was lost in the river, search that river. The corpus preserves the answer under both Lingyun Zhiqin and Yunmen Wenyan; Shending Yikui later uses the same line in an address, so the conflicting inherited attribution remains visible.',
1191:'To cut a wound into sound flesh is to manufacture injury where none existed. Named speakers apply the verdict to added explanations, talk of buddhas and ancestors, and even their own continued exposition; later commentary applies it to overworking an inherited case.',
1192:'To fall on level ground is to fail without an external obstacle. Named commentators use it as a verdict on figures in inherited cases, while encounter and hall-address speakers throw it directly at interlocutors or inherited formulations; the utterer remains distinct from the person judged.',
1193:'To raise a bone heap on level ground is to manufacture obstruction where the ground was clear. Fushan Fayuan gives it as an answer about the ancestor’s coming, while Eryin Mi and Baizhang Yiqi apply the image to unnecessary contrivance in disciplined conduct and teaching.',
1194:'The thief-is-family answer explains why robbery cannot empty the impoverished house: the danger belongs within. Caoshan Benji gives the inherited answer, Xutang Zhiyu and Baofu Qinghuo reuse it directly, and Zhe’an Jingfan turns it into a verse requiring further examination.',
1195:'A household thief is hard to guard against because the threat already occupies the defended house. Liangshan Yuanguan answers that recognition ends the grievance; Nanyan Sheng reuses the line directly, while Zhe’an Jingfan and Dabei Miaoyun criticize or replace Liangshan’s handling.'}

RECUT={
(1186,1):'白馬靄禪師。僧問：如何是清淨法身？師云：井底蝦蟆吞却月。',
(1190,1):'靈雲因僧問：如何是端坐念實相？師曰：河裏失錢河裏摝。',
(1190,4):'僧問雲門：「如何是端坐念實相？」門云：「河裏失錢河裏摝。」',
(1191,5):'浮山□云：國師好肉剜瘡，雲門灸瘢上著艾，雪竇大似隨邪逐惡，殊不知鼻孔總在侍者手裏。',
(1193,2):'舒州浮山法遠圓鑑禪師僧問如何是祖師西來意師曰平地起骨堆。',
(1193,4):'浮山遠禪師。僧問：如何是祖師西來意？師云：平地起骨堆。',
(1194,1):'曹山因僧問家貧遭刼時如何師曰不能盡底去曰為什麼不能盡底去師曰賊是家親。',
(1195,2):'梁山，上堂。真園頭出問：家賊難防時如何？山曰：識得不為冤。'
}
# Delete duplicate recensions that add no distinct deployment: Baiyun's same
# answer, Miaozhan's same address, Fushan's same comment, and a third copy of
# Fushan's bone-heap case.
DROP={1188:{6},1191:{4,6},1193:{3}}
ORIGINAL_LEN={1188:6,1191:7,1193:5}
NEW={
1188:('J/J26/J26nB183.xml','無佛法。若離卻世法，別求佛法，何異斬頭覓活、離水求冰耶？只於二六時中，動靜施為、應緣接物、料理家務，無時不到、無事不然，自不覺故耳，正所謂日用而不知也。果能直下覺得了、信得及去，一覺永覺、一信承信，則火裏蓮花時時顯艷，自然貼貼地，信手拈、信口道、信腳下，要來便來、要去便去，那見有佛法、世法，了與不了，荊林、拄人哉？雖然，山僧亦是寐語。此復。','Shiqi Tongyun'),
1191:('J/J28/J28nB202.xml','云：「腦後也須粉碎。」僧拂袖，云：「男兒自有衝天志，不向他人行處行。」師云：「少賣弄。」乃以拄杖卓一卓，喝一喝，云：「臨濟大師來也，是汝諸人且作麼生與他相見？有般漢聞與麼道，便云：『幸自太平無象，何用好肉剜瘡？』似這等見解，三十年後有人索飯錢在。','Baichi Xingyuan')}

def recut(o,text):
 v=zc.verify(o['RelPath'],text);assert v['ok'],(o['RelPath'],text);o['Kwic']=text;o['FromLb']=v['fromLb'];o['ToLb']=v['toLb']
def actor(o,spec):
 if spec[0]=='named':
  name=spec[1];o['MasterName']=name;o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':name,'Roles':['utterer']}];proof=f'The complete source unit assigns the exact headword-bearing turn to {name}.'
 else:
  label,role,ctx=spec[1:];o['MasterName']=None;o['ContextMasters']=[{'MasterName':x,'Roles':['section-subject']} for x in ctx];status='reviewed-unnamed' if label.startswith('the ') else 'identified-non-master';proof=f'The complete unit assigns the wording to {label}; no named master owns this verse, narration, or paratext.';o['ActorAttribution']={'Status':status,'Kind':label,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':proof,'ReviewedBy':'Codex f004 C1186-1195 independent-review repair author','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True}
 who=o.get('MasterName') or o['ActorAttribution']['ActorLabel'];o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {who}. {proof}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':who,'SpeechFrame':proof,'FullCaseDecision':proof}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()

# Register source-attested names absent from the public roster.
pp=R/'fresh-build/pending-roster.json';pd=json.loads(pp.read_text());have={x['canonicalName'] for x in pd['candidates']}
for n,eid in ROWS.items():
 e=json.loads((R/'fresh-build/entries'/eid/'evidence.draft.json').read_text())['Entry'];os=e['Senses'][0]['Occurrences'];drops=DROP.get(n,set()) if len(os)==ORIGINAL_LEN.get(n,-1) else set();kept=[o for i,o in enumerate(os,1) if i not in drops]
 for o,spec in zip(kept,ACT[n]):
  if spec[0]=='named' and spec[1] not in have:
   pd['candidates'].append({'canonicalName':spec[1],'aliases':[spec[1]],'evidence':[{k:o[k] for k in ('RelPath','FromLb','ToLb','Kwic')}],'reviewedBy':'Codex f004 C1186-1195 repair author','reviewReport':'fresh-build/waves/f004-laneC-1186-1195-independent-semantic-review.json','status':'awaiting-roster-integration'});have.add(spec[1])
pp.write_text(json.dumps(pd,ensure_ascii=False,indent=2)+'\n')

out=[]
for n,eid in ROWS.items():
 b=R/'fresh-build/entries'/eid;wp=b/'evidence.draft.json';w=json.loads(wp.read_text());e=w['Entry'];old=e['Senses'][0]['Occurrences']
 for i,text in [(i,t) for (nn,i),t in RECUT.items() if nn==n]:
  if not any(o.get('Kwic')==text for o in old): recut(old[i-1],text)
 drops=DROP.get(n,set()) if len(old)==ORIGINAL_LEN.get(n,-1) else set();os=[o for i,o in enumerate(old,1) if i not in drops]
 if n in NEW and not any(o['RelPath']==NEW[n][0] for o in os):
  rel,text,name=NEW[n];v=zc.verify(rel,text);assert v['ok'];neo=dict(os[-1]);neo.update({'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':text,'MasterName':name,'ContextMasters':[]});neo.pop('ActorAttribution',None);os.append(neo)
 assert len(os)==len(ACT[n]);e['Senses'][0]['Occurrences']=os
 for o,spec in zip(os,ACT[n]):actor(o,spec)
 s=e['Senses'][0];s['Explanation']=EXPL[n];s['ExplanationParts']={'CorpusEarnedOpening':EXPL[n],'EvidenceBody':['Complete-unit reading distinguishes original utterance, later raising, verse/commentary, biography, and paratext, and controls duplicate recensions at the deployment-family level.']};s['DraftEvidence']['ZenBend']=EXPL[n];s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(os)+1)];s['DraftEvidence']['IndependentWorkIds']=list(dict.fromkeys(zc.work_id(o['RelPath']) for o in os))
 w['Entry']=e;wp.write_text(json.dumps(w,ensure_ascii=False,indent=2)+'\n');ep=b/'entry.v2.json';rp=b/'c1186-1195-independent-review-repair-compile.json';q=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(wp),'--output',str(ep),'--report',str(rp)],text=True,capture_output=True)
 if q.returncode:raise SystemExit(q.stdout+q.stderr)
 ce=json.loads(ep.read_text());total=exact=0
 for ss in ce['Senses']:
  for o in ss['Occurrences']:
   total+=1;v=zc.verify(o['RelPath'],o['Kwic']);exact+=int(v['ok'] and v['fromLb']==o['FromLb'] and v['toLb']==o['ToLb'] and ce['SourceTerm'] in o['Kwic'])
 assert total==exact;row={'ordinal':n,'id':eid,'term':ce['SourceTerm'],'occurrences':total,'exactKwicsAndSpans':exact,'entrySha256':sha(ep),'worksheetSha256':sha(wp),'compileHardPass':True,'sourceReview':'f004-laneC-1186-1195-independent-semantic-review.json','selfReview':False,'promoted':False};(H/f'f004-laneC-{n}-independent-review-author-repair-checkpoint.json').write_text(json.dumps(row,ensure_ascii=False,indent=2)+'\n');out.append(row)
(H/'f004-laneC-1186-1195-independent-review-author-repair-ledger.json').write_text(json.dumps({'schemaVersion':1,'generatedUtc':NOW,'entries':out,'occurrences':sum(x['occurrences'] for x in out),'exactKwicsAndSpans':sum(x['exactKwicsAndSpans'] for x in out),'selfReview':False,'promoted':False},ensure_ascii=False,indent=2)+'\n');print(json.dumps({'entries':10,'occurrences':sum(x['occurrences'] for x in out)}))
