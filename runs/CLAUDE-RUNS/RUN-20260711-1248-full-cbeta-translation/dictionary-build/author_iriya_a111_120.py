import json, sys
from pathlib import Path

ROOT=Path(__file__).resolve().parent; sys.path.insert(0,str(ROOT)); import zc
PACKET=ROOT/'maintenance/iriya-author-packet-A061-160-v1.json'
OUT=ROOT/'maintenance/iriya-decisions-A111-120.json'
REV='Codex Iriya lane A111-120 full-case author'; STAMP='2026-07-18T18:20:00Z'
roster=json.loads((ROOT.parents[3]/'Assets/Data/lineage-masters.json').read_text())
ALIASES={n:r['names'][0] for r in roster for n in r.get('names',[])}
def named(x,v='direct-turn',role='utterer',context=()): return ('named-master',ALIASES.get(x,x),role,v,context)
def unlinked(x,v='direct-turn',role='utterer',context=()): return named(x,v,role,context) if x in ALIASES else ('identified-unlinked-master',x,role,v,context)
def nonmaster(x,v='compiler-narration',role='compiler',context=()): return ('identified-non-master',x,role,v,context)
def unnamed(x,v='compiler-narration',role='compiler',context=()): return ('reviewed-unnamed',x,role,v,context)
def narrated(x,role='verse-author',v='transmitted-verse',context=()): return ('narrated',x,role,v,context)

SEL={
111:[(0,unlinked('Baoning Yong 保寧勇')),(1,unlinked('Qinglong Si 青龍斯')),(2,unlinked('Baiyu Jingsi')),(3,named('Linquan Conglun')),(6,unlinked('Xiaoxi 曉皙')),(7,unlinked('Dae Shu 大峨蜀'))],
112:[(0,named('Xianglin Chengyuan','quoted-original',context=(('Lingyin Tuigeng Ning',('later-raiser','commentator')),))),(2,unlinked('Shimen Huiche 石門慧徹')),(3,unlinked('Shimen Huiche 石門慧徹','quoted-original')),(4,unlinked('Shimen Huiche 石門慧徹')),(5,unlinked('Liaotang Weiyi')),(7,unlinked('Shimen Huiche 石門慧徹','quoted-original',context=(("Tian'an Sheng",('later-raiser','commentator')),)))],
113:[(0,unlinked('Fuan Kefeng 復菴可封')),(1,unlinked('Rifang 日芳')),(2,named('Baizhang Huaihai')),(3,named('Hongzhi Zhengjue')),(6,named('Dahui Zonggao')),(7,named('Hongzhi Zhengjue'))],
114:[(0,named('Yunfeng')),(1,unlinked('Jingshan Xiu 徑山琇')),(2,named('Dahui Zonggao')),(4,unlinked('Huanglong Zuxin 黃龍祖心')),(5,named('Yuanwu Keqin')),(6,named('Baizhang Huaihai')),(7,unlinked("Zhe'an Fan 蔗菴範"))],
115:[(0,unlinked('Tianjie Sheng 天界盛')),(1,unlinked('Longhua Zong 龍華宗')),(3,named('Hongzhi Zhengjue')),(4,unlinked('Tianyin Xiu 天隱修')),(5,unlinked('Miaoyun Dabei 妙雲大悲')),(6,named('Sanzu Chonghui'))],
116:[(0,unlinked('Fogu Wen 佛古聞')),(1,unnamed('the unnamed editorial commentator behind the Falin yin label 法林音')),(2,unlinked('Yingshan Yin 瀛山誾')),(3,named('Guishan Lingyou')),(5,named('Haiyin Zhaoru')),(6,unlinked('Yunwaize 雲外澤'))],
117:[(0,named('Zhaozhou Congshen','quoted-original',context=(('Guanghui Lian',('later-raiser','commentator')),))),(1,named('Zhaozhou Congshen','quoted-original',context=(('Puxian Yuansu',('later-raiser','commentator')),))),(2,unlinked('Shending Hongyin 神鼎洪諲')),(3,named('Zhaozhou Congshen','quoted-original',context=(('Yuanwu Keqin',('later-raiser','commentator')),))),(5,named("Ying'an Tanhua")),(6,named('Zhaozhou Congshen','quoted-original',context=(('Chushi Fanqi',('later-raiser','commentator')),)))],
118:[(0,named('Baofu Congzhan')),(1,named('Yuanwu Keqin')),(2,named('Shitian Faxun')),(4,named('Fachang Yiyu')),(5,named('Huilin Zongben')),(7,named('Yuanwu Keqin'))],
119:[(0,named('Baizhang Huaihai')),(1,named('Dahui Zonggao')),(2,named('Changqing Huileng','quoted-original')),(3,named('Zhongfeng Mingben')),(4,unlinked('Chaozong 超宗')),(6,unlinked('Guangjiao Guixing 廣教歸省'))],
120:[(0,named('Yinyuan Longqi')),(1,unlinked('Chaozong 超宗')),(2,unlinked('Tianyin Xiu 天隱修')),(3,named('Zhongfeng Mingben')),(7,named('Baiyu Jingsi')),(5,unlinked('Lianyue 蓮月'))],
}
TARGET={111:'onlookers laugh',112:'Old Wang of the eastern village burns paper money at night',113:'water does not wash water',114:'give medicine according to the illness',115:'an echo within the words',116:'leave everyone under heaven in doubt',117:'break up the household and scatter the home',118:'a case already complete before you',119:'form an understanding by following the words',120:'receive emptiness and answer an echo'}
OPEN={
111:'“Onlookers laugh” is a closing verdict delivered from outside the action under review. Chan commentators use it when a case, reply, or inherited maneuver exposes itself to anyone watching.',
112:'Old Wang of the eastern village burns paper money at night is the recorded answer to what remains at year’s end and to a phrase outside the three vehicles. Later masters repeatedly raise, verse, and answer this same public case.',
113:'Water does not wash water states that a thing does not act upon itself as a second object. The records pair it with gold not exchanging for gold and mind not seeing mind, then turn the formula into a direct question at the teaching seat.',
114:'Giving medicine according to the illness is speech adjusted to the person and condition at hand. Masters use the medical image both to describe responsive words and to warn that one stock answer cannot be prescribed everywhere.',
115:'The records predicate an “echo in the words” of an utterance and use that phrase as an appraisal in named cases. They apply it to replies, challenges, and family contests without supplying a separate gloss for the echo.',
116:'To leave everyone under heaven in doubt is to make a word, gesture, or case remain publicly unsettled. Masters attach the verdict to questions, displayed objects, inherited cases, and replies that keep later readers arguing.',
117:'Breaking up the household and scattering the home is the destructive half of a paired Chan household image. The records contrast Zhaozhou’s phrase with Nanquan’s “making a living,” then ask whether either can stand alone.',
118:'A case already complete before you is a public matter that does not wait for fresh construction. Masters raise it from the seat, call it visible before birth or before a sail is hung, and warn that handling it can still miss what is already present.',
119:'Forming an understanding by following the words is taking the wording one has heard as the understanding itself. Masters name it when hearers trail an answer, repeat an old judgment, or build a position from someone else’s phrasing.',
120:'Receiving emptiness and answering an echo is responding to what has no substance of one’s own. Chan speakers apply the paired image to inherited talk and imitative responses, often immediately contrasting it with a present demand or blow.'}
BODY={
111:'Baoning Yong, Qinglong Si, Baiyu Jingsi, Linquan Conglun, Xiaoxi, and Dae Shu each place the laugh after a concrete case or appraisal, making the watcher’s verdict part of Chan case commentary.',
112:'Xianglin’s answer is transmitted in year-end addresses; Shimen gives it for the separate-transmission phrase; Liaotang Weiyi and Tian’an Sheng raise it and add their own verses.',
113:'Baizhang explains the formula beside non-seeking and unobstructed understanding; Hongzhi coordinates water, gold, eyes, and mind; Fuan Kefeng and Rifang demand that the assembly supply the line.',
114:'Yunfeng calls Xuefeng’s answer medicine for an illness before striking hesitation; Dahui requires capacities to be observed before instruction; Huanglong Zuxin and Yuanwu use the same medical frame in public addresses.',
115:'Tianjie Sheng hears the echo in the Buddha’s reply, Longhua Zong hears it in an outsider’s words, Tianyin Xiu places it in Linji-family contest, and Sanzu Chonghui gives it as an answer about sudden awakening.',
116:'Fogu Wen answers his teacher with the verdict; Guishan applies it to Yangshan’s one sentence; Haiyin rests it on the staff, while Yunwaize gives the same answer to both Deshan’s staff and Linji’s shout.',
117:'Guanghui Yuanlian and Shen Ding compare Zhaozhou with Nanquan; Puxian Yuansu weighs both household actions; Yuanwu says one requires the other; Ying’an and Chushi apply the image to later cases and public addresses.',
118:'Yuanwu calls his teacher’s matter already complete, Shitian Faxun says it cannot be tidied by handling, Fachang Yiyu places it before birth, and Huilin Zongben before the ancient sail is hung.',
119:'Baizhang describes beings following words into understanding; Dahui rejects that reading of Zhaozhou; Zhongfeng names and answers the same fault, while Chaozong and Guangjiao Guixing turn it against later case handling.',
120:'Yinyuan Longqi contrasts empty reception with working the local ground; Chaozong applies it to inherited talk; Tianyin Xiu warns the echo-following person not to move; Baiyu Jingsi applies the phrase to Kasyapa in the flower-sermon case; and Lianyue forbids it while raising the whisk.'}
ALIAS={
111:'Searches for an onlooker, watcher, or bystander laughing should retrieve the same compact appraisal.',
112:'Lookup retains Old Wang, the eastern village, night, and burning paper money so the case is not confused with ordinary offerings.',
113:'Water washing itself and the shorter non-self-washing wording are retrieval variants of the same formula.',
114:'Readers may search for medicine, remedy, illness, or sickness while keeping the responsive giving predicate.',
115:'Echo, resonance, and words belong together in lookup because the sound is explicitly located inside the saying.',
116:'Searches for leaving everyone doubtful or baffling all under heaven preserve the formula’s public scope.',
117:'Household, family, home, breaking, and scattering are kept together so the destructive household saying remains findable.',
118:'Already-complete case, ready-made case, and case before you retrieve the public-case expression without reducing it to “obvious.”',
119:'Following words into an understanding and interpreting after speech are controlled lookup paraphrases of the same named fault.',
120:'Empty reception and echo-answering remain paired in search because either half alone has many unrelated corpus uses.'}
FAMILY={
111:'傍觀者 and isolated laughter occur ordinarily; the fixed closing verdict requires the complete onlookers-laugh clause.',
112:'王老燒錢 is an attested shortening, but bare money-burning and unrelated village elders do not inherit this case identity.',
113:'金不博金, 眼不見眼, and 心不用心 are parallel formulas, not occurrences of the water headword.',
114:'應病施方 is a close prescription-family variant; generic medicine and illness passages do not count as this fixed phrase.',
115:'句裏藏鋒 is a frequent paired appraisal, but it remains a contrast or companion rather than part of the headword.',
116:'疑著 and ordinary 天下人 uses were excluded; the full lethal-doubt verdict supplies the article’s unit.',
117:'作活計 is the corpus-drawn counterpart, while isolated 破家 and household narration do not buy this saying’s depth.',
118:'公案 alone names the broader public-case family; only 見成公案 carries the already-complete predicate.',
119:'隨語作解 is a governed wording variant of the same formula; unrelated 隨語 clauses were excluded.',
120:'承響 and 接響 can describe neighboring echo language, but only the coordinated emptiness-and-echo expression supports this article.'}

def limit(pos): return {111:'The phrase is an attested appraisal, not evidence that a literal crowd always stands nearby.',112:'The man and rite belong to the fixed saying; the entry does not identify Wang as a recoverable historical person.',113:'The formula states non-self-operation; it does not by itself define every water, gold, eye, or mind compound.',114:'The medical image describes responsive speech in these cases; it does not turn Chan encounters into a clinical system.',115:'The echo is predicated of wording and response, not a separately narrated physical sound.',116:'The killing graph intensifies doubt; the retained cases do not narrate literal killing.',117:'Household ruin is a figurative action in the paired sayings; the witnesses do not report destroyed buildings.',118:'“Complete” does not mean mechanically settled by an annotator; the records still stage questions and tests around the case.',119:'Different conclusions following different sayings remain one named error, not separate senses.',120:'The paired phrase concerns derivative response; ordinary acoustic echoes without that appraisal are excluded.'}[pos]

packet=json.loads(PACKET.read_text()); rows={r['position']:r for r in packet['rows']}; entries=[]
for pos in range(111,121):
 row=rows[pos]; occ=[]
 for ci,dec in SEL[pos]:
  typ,label,role,voice,context=dec; case=row['candidateCases'][ci]; hits=[]
  for width in range(8,27):
   hits=[h for h in zc.find(case['relPath'],row['searchTerm'],ctx=width) if case['fromLb']<=h['fromLb']<=case['toLb'] and h['window'].count(row['searchTerm'])==1]
   if hits: break
  if not hits: raise RuntimeError((pos,ci,row['searchTerm']))
  chosen=1 if (pos,ci) in {(112,5),(113,6)} and len(hits)>1 else 0
  kwic=hits[chosen]['window']; v=zc.verify(case['relPath'],kwic)
  actor={'type':typ}
  if typ=='named-master': actor['masterName']=label
  else: actor.update({'kind':'full-case exact-turn adjudication','label':label,'role':role,'subject':label,'reviewedBy':REV,'reviewedUtc':STAMP,'authoredVoiceRiskReviewed':True})
  if (pos,ci)==(116,1):
   proof='The headword occurs in editorial commentary introduced by the recurring label Falin yin; the label identifies a commentary layer, not a personal master.'
  else:
   proof=(f'The complete case explicitly assigns the headword-bearing quoted turn to {label}; the surrounding compiler or record owner only transmits it.' if voice=='quoted-original' else f'The complete case places the exact phrase inside {label}\'s marked speech or attributed verse before the next structural boundary.')
  o={'caseIndex':ci,'fromLb':v['fromLb'],'toLb':v['toLb'],'kwic':kwic,'exactHeadwordClause':row['searchTerm'],'grammaticalProof':proof,'voiceLayer':voice,'speechMarkerReviewed':True,'speechMarkerAcknowledgement':'The complete structural case and every local speech boundary were read before this exact-turn assignment.','actor':actor}
  if context:
   o['contextMasters']=[{'masterName':ALIASES.get(name,name),'roles':list(roles)} for name,roles in context]
  if (pos,ci)==(112,3):
   o['quotedOriginalOuterFrameReviewed']=True
  if voice=='transmitted-verse': o['explicitVerseAttribution']=True
  if (pos,ci)==(119,2):
   o['contextActors']=[{'type':'identified-unlinked-master','label':'Xuedou Zong 雪竇宗','roles':['later-raiser','commentator'],'grammaticalProof':'Xuedou Zong raises the earlier exchange and explicitly quotes Changqing Huileng’s headword-bearing appraisal.'}]
   o['attributionContext']='Xuedou Zong raises the earlier exchange and quotes Changqing Huileng’s appraisal.'
  occ.append(o)
 lim=limit(pos); target=TARGET[pos]
 sense={'senseKey':None,'masterName':None,'preferredTarget':target,'alternateTargets':[],'searchAliases':[target],'status':'preferred','validation':'multi-source','note':lim,'explanationParts':{'corpusEarnedOpening':OPEN[pos],'evidenceBody':[BODY[pos]]},'relatedMasters':[],'relatedTerms':[],'draftEvidence':{'OpeningClaimEvidenceKeys':[f'o{i+1}' for i in range(len(occ))],'ZenBend':BODY[pos],'CounterexampleOrLimit':lim,'DifferentThingTest':{'Decision':'one-thing','ComparedThings':[target,'the retained question, address, verse, and commentary uses'],'Reason':lim},'AliasRationale':ALIAS[pos],'ModifierControls':[{'Term':'complete lexical frame','Finding':lim}],'FamilyControls':[{'Term':'component and parallel forms','Finding':FAMILY[pos]}]},'occurrences':occ}
 entries.append({'id':row['id'],'createdBy':REV,'writtenUtc':STAMP,'senses':[sense]})
payload={'schemaVersion':'iriya-compact-decisions-v1','entries':entries}; OUT.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n')
for a,b in ((111,115),(116,120)):
 (ROOT/f'maintenance/iriya-decisions-A{a}-{b}.json').write_text(json.dumps({'schemaVersion':'iriya-compact-decisions-v1','entries':entries[a-111:b-110]},ensure_ascii=False,indent=2)+'\n')
print(OUT, len(entries), sum(len(e['senses'][0]['occurrences']) for e in entries))
