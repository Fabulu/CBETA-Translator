from pathlib import Path
import hashlib,json,subprocess,sys
H=Path(__file__).resolve()
R=H.parents[2]/"runs/CLAUDE-RUNS/RUN-20260711-1248-full-cbeta-translation/dictionary-build"
helper=R/"fresh-build/waves/build_f005_b_1303_1312.py"
__file__=str(helper)
exec(compile(helper.read_text(encoding="utf-8").split("\nS = {}\n",1)[0],"helpers","exec"),globals())

def mk(o,idx,actor=None,mode="named",role="utterer",contexts=(),decision=""):
 return lead(o,idx,actor,mode,role=role,contexts=contexts,decision=decision)
def se(target,aliases,opening,body,occs,bend,limit,related,key=None):
 return sense(target,[],aliases,opening,[body],occs,bend,limit,related,key)
S={}
S[1313]=[se("not destroyed",["not destroyed","unbroken","intact","indestructible"],"Not destroyed says that a body, object, capacity, or stated relation remains intact rather than being destroyed.","Direct teachings predicate it of embodied activity or responsive use; biographies also use it literally of tongues, teeth, eyes, and relics surviving cremation.",[mk(1313,1,"Yongming Yanshou"),mk(1313,2,mode="narrated",contexts=(("Baoning Yuanji",["action-performer","person-described"]),)),mk(1313,3,mode="narrated",contexts=(("Fozhi Duanyu",["person-described"]),)),mk(1313,4,"Dazhi"),mk(1313,5,mode="narrated"),mk(1313,7,"Gulin Qingmao"),mk(1313,9,"Yichu Yuan"),mk(1313,10,"Hengchuan Xinggong")],"Zen records place doctrinal non-destruction beside physical post-cremation reports.","The predicate does not make every surviving thing metaphysically indestructible.",["金剛不壞","不滅"])]
S[1314]=[se("after a long pause, he said",["after a long pause he said","after a long silence","paused for a long time"],"After a long pause, he said is the recorder's formula for silence followed by speech in a public exchange.","Eight independent records use the same narrative hinge before the next quoted words.",[mk(1314,i,mode="narrated") for i in (1,2,3,4,5,7,8,9)],"The silence occupies a recorded turn before the answer arrives.","The formula is written by the recorder, not spoken by the person who speaks afterward.",["良久","默然"])]
S[1314][0]["Occurrences"][0]=mk(1314,1,mode="narrated",contexts=(("Muzhou Daoming",["action-performer","respondent"]),))
o=S[1314][0]["Occurrences"][0];kw="作麼生是本色衲僧良久云有輸有贏有防禦使";v=zc.verify(o["RelPath"],kw);assert v["ok"] and v["count"]==1;o["Kwic"]=kw;o["FromLb"]=v["fromLb"];o["ToLb"]=v["toLb"];o["DraftActorProof"]["ExactHeadwordClause"]=kw
S[1316]=[se("manifold differences",["manifold differences","thousand distinctions","myriad differences"],"Manifold differences names the many distinctions or divergent forms encountered at once.","Speakers place these differences under one phrase, one thread, one substance, or the action of being cut off.",[mk(1316,1,"Fengqi Zhongqing"),mk(1316,2,"Yuanwu Keqin"),mk(1316,3,"Baichi Yuan"),mk(1316,4,"Feiyin Tongrong"),mk(1316,6,"Shoushan Xingnian"),mk(1316,7,"Lingrui Ni Zugui"),mk(1316,8,"Shanhui Zhiyi"),mk(1316,9,"Jiqi Hongchu")],"Masters use the abundance of distinctions as something threaded through, unmoved, or knocked down.","Claims that the differences are one are local predicates, not the bare gloss.",["千差萬別","差別"])]
S[1318]=[se("to go on like that",["go on like that","proceed that way","continue like this"],"To go on like that means to proceed in the manner just stated or displayed.","Unnamed monks ask what happens if one goes on so; named masters use the phrase in comment, rebuke, and funeral speech.",[mk(1318,1,mode="questioner",contexts=(("Fayuan Foquan",["respondent"]),)),mk(1318,2,"Hongzhi Zhengjue"),mk(1318,4,mode="questioner",contexts=(("Baoci Wensui",["respondent"]),)),mk(1318,5,"Shending Hongyin"),mk(1318,6,mode="questioner",contexts=(("Puti Guifang",["respondent"]),)),mk(1318,8,"Kaifu Daoning"),mk(1318,9,"Yichu Yuan"),mk(1318,10,mode="questioner",contexts=(("Nantang Qingyu",["respondent"]),))],"The phrase forces the preceding display into a public question: if one continues exactly so, what then?","It points backward to its local case and gives no context-free method.",["恁麼行","與麼去"])]
S[1319]=[se("without dependence",["without dependence","unsupported","relying on nothing"],"Without dependence describes what has no support, attachment, fixed residence, or thing on which it relies.","Speakers pair it with no fixed abode and apply it to a body, person, place, or knowing that lacks support.",[mk(1319,1,"Yongming Yanshou"),mk(1319,2,"Fozhi Zhikai"),mk(1319,5,"Hongzhi Zhengjue"),mk(1319,6,"Baozhi"),mk(1319,7,"Zhuanyu Guanheng"),mk(1319,8,"Hengchuan Xinggong"),mk(1319,10,"Huihong")],"The records turn ordinary reliance into a testable absence of foothold, residence, or object.","The phrase does not by itself instantiate the title Man of No Dependence.",["無住","無所依"])]
S[1320]=[se("to reside on a mountain",["reside on a mountain","live on a mountain","mountain residence"],"To reside on a mountain is to live at, or take responsibility for, a mountain monastery or hermitage.","Speakers ask whether words accord with mountain residence, raise residence cases, and ask how long someone has lived there.",[mk(1320,1,"Fachang Yiyu"),mk(1320,3,"Poshan Haiming"),mk(1320,4,"Tianyin Yuanxiu"),mk(1320,5,"Hanyue Fazang"),mk(1320,6,"Wumen Huikai"),mk(1320,8,mode="narrated",contexts=(("Damei Fachang",["person-described"]),)),mk(1320,9,"Xuansha Shibei"),mk(1320,10,"Ouyang Xiu",mode="nonmaster",role="questioner")],"Residence becomes a public test of the responsibility and duration of occupying the mountain seat.","The embedded characters in hold fast mountains and rivers are not this phrase.",["住持","住庵"])]
S[1320][0]["Occurrences"][2]=mk(1320,4,mode="questioner",contexts=(("Tianyin Yuanxiu",["respondent","record-owner"]),))
S[1322]=[se("to raise",["raise","lift up","raise the staff","raise the whisk"],"To raise is a narrated physical action in which an implement is lifted before or within an encounter.","Recorders narrate Dahui, Ziling, Mi'an, Miyun, Baizhang, Shiqi, Yongji, and Yantou raising implements.",[mk(1322,1,mode="narrated",contexts=(("Dahui Zonggao",["action-performer"]),)),mk(1322,2,mode="narrated",contexts=(("Jiashan Lingquan Ziling",["action-performer"]),)),mk(1322,3,mode="narrated",contexts=(("Mi'an Xianjie",["action-performer"]),)),mk(1322,4,mode="narrated",contexts=(("Miyun Yuanwu",["action-performer"]),)),mk(1322,5,mode="narrated",contexts=(("Baizhang Huaihai",["action-performer"]),)),mk(1322,7,mode="narrated",contexts=(("Shiqi Tongyun",["action-performer"]),)),mk(1322,8,mode="narrated",contexts=(("Yongji Rong",["action-performer"]),)),mk(1322,10,mode="narrated",contexts=(("Yantou Quanhuo",["action-performer"]),))],"The lifted implement becomes a visible turn, but the recorder utters the reporting verb.","The verb reports the lift and gives no fixed meaning to every object.",["豎起","拈起"])]

S[1315]=[se("mountains and waters",["mountains and waters","landscape","mountain and water scenery"],"Mountains and waters names the visible landscape, or its mountains and waters considered together.","Yang Jie speaks of roaming landscapes; Huangbo coordinates mountain as mountain and water as water; Zhuanyu denies that represented mountains and waters simply are the things; a parallel record preserves Yang Jie's exchange.",[mk(1315,1,"Yang Jie",mode="nonmaster",role="questioner",contexts=(("Furong Daokai",["respondent"]),)),mk(1315,3,"Huangbo Xiyun"),mk(1315,5,"Zhuanyu Guanheng"),mk(1315,10,"Yang Jie",mode="nonmaster",role="questioner",contexts=(("Furong Daokai",["respondent"]),))],"The ordinary landscape pair becomes a public test when speakers insist on or deny the identity of mountain and water.","Strings such as Dongshan walking on water and Guishan's water buffalo are not this lexical unit.",["山河","江山"])]
for rel,kwic,actor,mode,contexts,title in [
 ("X/X81/X81n1568.xml","曰：虗涉他如許多山水。眼曰：如許多山水也不惡。","the unnamed monastic questioner","questioner",(("Fayan Wenyi",["respondent"]),),"五燈嚴統(第10卷-第25卷)"),
 ("T/T47/T47n1997.xml","山是山水是水。互換投機去。","Yuanwu Keqin","named",(),"圓悟佛果禪師語錄"),
 ("X/X66/X66n1297.xml","大士頌。須彌芥子父，芥子須彌爺。山水坦然平，敲冰來煑茶。","Fu Dashi","named",(),"宗鑑法林")]:
 v=zc.verify(rel,kwic);assert v["ok"] and v["count"]==1
 note=f"Source text ({title}; {rel}). Full-case reading identifies {actor} as the exact headword utterer."
 base={"RelPath":rel,"FromLb":v["fromLb"],"ToLb":v["toLb"],"Kwic":kwic,"Curated":True,"MasterName":actor if mode=="named" else None,"AttributionNote":note,"ContextMasters":([{"MasterName":actor,"Roles":["utterer"]}] if mode=="named" else [])+[{"MasterName":n,"Roles":rs} for n,rs in contexts],"DraftActorProof":{"ExactHeadwordClause":kwic,"GrammaticalSubject":actor,"SpeechFrame":note,"FullCaseDecision":note}}
 if mode=="questioner":base["ActorAttribution"]={"Status":"reviewed-unnamed","Kind":"unnamed monastic participant","ActorLabel":actor,"ActorRole":"questioner","RungsChecked":RUNGS,"GrammarEvidence":note,"ReviewedBy":"Codex f005 lane B 1313-1322 author","ReviewedUtc":NOW,"AuthoredVoiceRiskReviewed":True}
 S[1315][0]["Occurrences"].append(base)

huike=[mk(1317,1,mode="questioner",contexts=(("Fayuan Foquan",["respondent"]),("Huike",["case-figure"]))),mk(1317,2,"Dahui Zonggao",contexts=(("Huike",["case-figure"]),)),mk(1317,4,"Huangbo Xiyun",contexts=(("Huike",["case-figure"]),)),mk(1317,7,"Baichi Yuan",contexts=(("Huike",["case-figure"]),)),mk(1317,9,mode="questioner",contexts=(("Huike",["case-figure"]),))]
ananda=[mk(1317,6,mode="narrated",contexts=(("Ananda",["person-described"]),))]
for rel,kwic,actor in [
 ("J/J38/J38nB425.xml","第二祖阿難陀尊者第二祖阿難陀尊者如來成道祖生日","Jifei Ruyi"),
 ("J/J27/J27nB189.xml","二祖阿難尊者二祖阿難尊者如相擬如來","Sanyi Mingyu"),
 ("J/J33/J33nB286.xml","二祖阿難尊者二祖阿難尊者師問迦葉尊者云","Yingning Jing")]:
 v=zc.verify(rel,kwic);assert v["ok"] and v["count"]==1
 title={"J/J38/J38nB425.xml":"即非禪師全錄","J/J27/J27nB189.xml":"三宜盂禪師語錄","J/J33/J33nB286.xml":"攖寧靜禪師語錄"}[rel]
 note=f"Source text ({title}; {rel}). {actor} presents the headword-bearing lineage case; Ananda is its named figure."
 ananda.append({"RelPath":rel,"FromLb":v["fromLb"],"ToLb":v["toLb"],"Kwic":kwic,"Curated":True,"MasterName":actor,"AttributionNote":note,"ContextMasters":[{"MasterName":actor,"Roles":["utterer"]},{"MasterName":"Ananda","Roles":["case-figure"]}],"DraftActorProof":{"ExactHeadwordClause":kwic,"GrammaticalSubject":actor,"SpeechFrame":note,"FullCaseDecision":f"{actor} presents the case."}})
a=se("the Second Patriarch, Huike",["Second Patriarch Huike","Huike","Chinese second patriarch"],"The Second Patriarch is Huike when records recount Bodhidharma's Chinese succession, standing in snow, severed arm, or request to pacify the mind.","Dahui, Huangbo, Baichi, and questioners invoke Huike's succession and defining encounters.",huike,"The ordinal is a compact lineage name used to raise Huike's cases.","It does not name Ananda when the surrounding lineage is Indian.",["慧可","神光"],"huike")
b=se("the Second Patriarch, Ananda",["Second Patriarch Ananda","Ananda","Indian second patriarch"],"The Second Patriarch is Ananda in enumerations and cases of the Indian lineage after Mahakasyapa.","A biography and three independent records explicitly present Ananda under the ordinal title.",ananda,"Later masters use the ordinal title to raise Ananda as a lineage figure.","This Indian title is distinct from Huike's Chinese title.",["阿難","迦葉"],"ananda")
sp={"Decision":"different-thing","ComparedThings":["Huike in the Chinese lineage","Ananda in the Indian lineage"],"Reason":"The same ordinal names two different people in two lineage enumerations."}
a["DraftEvidence"]["DifferentThingTest"]=sp;b["DraftEvidence"]["DifferentThingTest"]=sp;S[1317]=[a,b]

light=[mk(1321,1,"Fayings Zujing"),mk(1321,3,"Zhongfeng Mingben"),mk(1321,6,"Baozhi"),mk(1321,9,"Qingyuan Yuzhe"),mk(1321,10,"Xuansha Shibei",contexts=(("Huihong",["later-quoter","commentator"]),))]
person=[mk(1321,4,mode="narrated",contexts=(("Huike",["person-described"]),)),mk(1321,5,mode="narrated",contexts=(("Huike",["person-described"]),)),mk(1321,7,"Gulin Qingmao",contexts=(("Huike",["case-figure"]),)),mk(1321,8,"Hongzhi Zhengjue",contexts=(("Huike",["case-figure"]),))]
a=se("spiritual radiance",["spiritual radiance","numinous light","radiant light"],"Spiritual radiance is a brilliant or far-reaching light said to shine, fill space, or appear around a person.","Speakers release it, call it solitary and enduring, or place its illumination across vast distance.",light,"Masters make the radiance's reach and location answerable in verse and address.","This light is not the person formerly named Shenguang.",["靈光","光明"],"light")
b=se("Shenguang, Huike's former name",["Shenguang","Huike's former name","Master Shenguang"],"Shenguang is the name borne by Huike before Bodhidharma renamed him.","Biographies identify Shenguang and then Huike; later masters invoke Shenguang as that familiar case figure.",person,"The former name invokes Huike's encounter without saying Huike.","This person-name is not an occurrence of radiant light.",["慧可","二祖"],"person")
sp={"Decision":"different-thing","ComparedThings":["radiant light","Shenguang, the person later named Huike"],"Reason":"One is illumination; the other is a person's proper name."}
a["DraftEvidence"]["DifferentThingTest"]=sp;b["DraftEvidence"]["DifferentThingTest"]=sp;S[1321]=[a,b]

# Narrow two named-master rows to their actual turns so an earlier anonymous
# question in the same case cannot trigger the authored-voice safeguard.
for ordinal,index,kwic in [(1316,1,"師云。千差俱不動。"),(1318,1,"覺上座今日也恁麼去也。還相委悉麼。")]:
 o=S[ordinal][0]["Occurrences"][index];v=zc.verify(o["RelPath"],kwic);assert v["ok"] and v["count"]==1
 o["Kwic"]=kwic;o["FromLb"]=v["fromLb"];o["ToLb"]=v["toLb"];o["DraftActorProof"]["ExactHeadwordClause"]=kwic
# In Linjian Lu the headword is inside the quoted monk's question to Yun'an,
# not in Huihong's surrounding narration.
S[1319][0]["Occurrences"][-1]=mk(1319,10,mode="questioner",contexts=(("Yun'an Kewen",["respondent","case-figure"]),))

for senses in S.values():
 for s in senses:
  occset=s["Occurrences"]; ws=list(dict.fromkeys(zc.work_id(o["RelPath"]) for o in occset))
  s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in occset));s["RelatedMasters"]=list(dict.fromkeys(c["MasterName"] for o in occset for c in o.get("ContextMasters",[]) if c.get("MasterName")))
  s["Note"]=f"{len(occset)} exact witnesses from {len(ws)} independent works delimit this sense.";s["DraftEvidence"]["IndependentWorkIds"]=ws;s["DraftEvidence"]["OpeningClaimEvidenceKeys"]=[f"o{i}" for i in range(1,len(occset)+1)]

rows=[]
for ordinal in range(1313,1323):
 e=PE[ordinal];d=R/"fresh-build/entries"/e["id"];d.mkdir(parents=True,exist_ok=True)
 draft={"SchemaVersion":1,"Entry":{"Id":e["id"],"SourceTerm":e["term"],"CorpusBaselineSha256":BASE,"CreatedBy":"Codex f005 lane B 1313-1322 author","WrittenUtc":NOW,"Senses":S[ordinal]}}
 wp=d/"evidence.draft.json";atomic_json(wp,draft);oc=sum(len(s["Occurrences"]) for s in S[ordinal]);works=len({zc.work_id(o["RelPath"]) for s in S[ordinal] for o in s["Occurrences"]})
 (d/"WORK.md").write_text(f"# {e['term']} — f005 lane B\n\n- frozen-corpus: {BASE}; 494 files / 487 works.\n- indexed-path: packet leads followed by full-case reading; every stored row reverified with zc.verify.\n- definition-searches: exact form, questions, predicates, narration, titles, compounds, and counterexamples.\n- deployment-inventory: {oc} exact rows / {works} independent works.\n- omission-audit: false segmentation, title-only strings, duplicate works, and ambiguous voices excluded.\n- family-retest: longer formulas and adjacent actions read without treating them as synonyms.\n- sense-target-distinguishability: different people or referents split; grammar and paraphrase remain one.\n- feedback-inference-verdict: supported by the stored corpus deployments.\n- feedback-observations: stored occurrences anchor the opening, deployment, actor decision, and limit.\n- feedback-falsification-searches: alternate referents, narrative versus direct speech, title/person uses, and quoted voices.\n- feedback-counterexamples: recorded in each sense's CounterexampleOrLimit.\n- feedback-scope: corpus-specific observable deployment only.\n- lookup-probes: controlled English aliases stored per sense.\n- opening-interpretation-verdict: supported by the selected exact witnesses.\n",encoding="utf-8")
 ep=d/"entry.v2.json";rp=d/"f005-b-1313-1322-compile-report.json";q=subprocess.run([sys.executable,str(R/"compile_evidence_draft.py"),str(wp),"--output",str(ep),"--report",str(rp)],capture_output=True,text=True)
 if q.returncode:raise SystemExit(q.stdout+q.stderr)
 (d/"STATUS").write_text("drafted\n",encoding="utf-8");rows.append({"ordinal":ordinal,"id":e["id"],"term":e["term"],"occurrences":oc,"entrySha256":hashlib.sha256(ep.read_bytes()).hexdigest(),"worksheetSha256":hashlib.sha256(wp.read_bytes()).hexdigest(),"state":"drafted-awaiting-independent-review"})
atomic_json(R/"fresh-build/waves/f005-laneB-1313-1322-author-rows.json",{"schemaVersion":1,"generatedUtc":NOW,"rows":rows})
roster=json.loads((R.parents[3]/"Assets/Data/master-dates.json").read_text(encoding="utf-8"));known={m["names"][0] for m in roster["masters"]};cand={}
for senses in S.values():
 for s in senses:
  for o in s["Occurrences"]:
   for n in [o.get("MasterName"),*[c.get("MasterName") for c in o.get("ContextMasters",[])]]:
    if n and n not in known:cand.setdefault(n,[]).append({k:o[k] for k in ("RelPath","FromLb","ToLb","Kwic")})
pending={"schemaVersion":1,"generatedUtc":NOW,"candidates":[{"canonicalName":n,"aliases":[n],"evidence":list({json.dumps(x,sort_keys=True,ensure_ascii=False):x for x in es}.values()),"reviewedBy":"Codex f005 lane B 1313-1322 author","reviewReport":"fresh-build/waves/f005-laneB-1313-1322-author-checkpoint.json","status":"awaiting-roster-integration"} for n,es in sorted(cand.items())]}
atomic_json(R/"fresh-build/waves/f005-laneB-1313-1322-pending-roster.json",pending)
print(json.dumps({"entries":len(rows),"occurrences":sum(x["occurrences"] for x in rows)},indent=2))
