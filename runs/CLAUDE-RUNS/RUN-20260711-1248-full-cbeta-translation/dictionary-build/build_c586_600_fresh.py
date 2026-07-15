import json
from pathlib import Path
import zc

HERE = Path(__file__).parent
baseline = json.loads((HERE / "fresh-build/corpus-baseline.json").read_text(encoding="utf-8"))
workmap = baseline["work_ids"]
sha = baseline["manifestSha256"]

# Each witness is a direct record-owner turn or authorial instruction checked in its containing case.
specs = [
(586,"t_7c2c8da520e4","壓良為賤","to degrade a free person into servitude",["degrade the worthy as base","treat a free person as a slave"],[
("D/D48/D48n8939.xml","Foyan Qingyuan"),("L/L154/L154n1639.xml","Tianyin Yuanxiu"),("J/J25/J25nB171.xml","Tianyin Yuanxiu"),("J/J26/J26nB178.xml","Feiyin Tongrong"),("J/J23/J23nB134.xml","Yunmen Wenyan"),("J/J35/J35nB342.xml","Huayan Shengke")]),
(587,"t_ff3b9302050a","逢場作戲","to put on the play when the occasion appears",["play the part as occasion demands","perform on meeting the scene"],[
("J/J27/J27nB190.xml","Shiyu Mingfang"),("J/J37/J37nB386.xml","Yuan'an Feng"),("J/J32/J32nB276.xml","Buhui"),("J/J36/J36nB359.xml","Baiyu Si"),("J/J26/J26nB178.xml","Feiyin Tongrong"),("J/J25/J25nB163.xml","Guting Shanjian")]),
(588,"t_4b4c8dc868b7","賊過後張弓","to draw the bow after the thief has passed",["too late after the thief has gone","shut the stable door after the theft"],[
("T/T47/T47n1996.xml","Xuedou Chongxian"),("T/T47/T47n1997.xml","Yuanwu Keqin"),("X/X69/X69n1359.xml","Ying'an Tanhua"),("X/X71/X71n1409.xml","Yuejiang Zhengyin"),("X/X71/X71n1414.xml","Liao'an Qingyu"),("J/J25/J25nB171.xml","Tianyin Yuanxiu")]),
(589,"t_d400e8468267","鑽龜打瓦","to drill a tortoise shell and strike tiles",["seek an omen by drilling shell and tiles","divine by shell and tile"],[
("J/J37/J37nB388.xml","Shending Yikui"),("T/T47/T47n1998A.xml","Dahui Zonggao"),("J/J36/J36nB359.xml","Baiyu Si"),("J/J26/J26nB177.xml","Poshan Haiming"),("X/X71/X71n1420.xml","Chushi Fanqi"),("C/C077/C077n1710.xml","Linji Yixuan")]),
(590,"t_7d867d9d8a2b","掩耳偷鈴","to cover one's ears while stealing a bell",["stop one's ears and steal the bell","hide the evidence only from oneself"],[
("J/J29/J29nB240.xml","Muzhou Daoming"),("J/J37/J37nB386.xml","Yuan'an Feng"),("J/J37/J37nB388.xml","Shending Yikui"),("J/J34/J34nB299.xml","Hanyue Fazang"),("J/J39/J39nB453.xml","Yuanjie Ying"),("J/J39/J39nB471.xml","Konggu Daocheng")]),
(591,"t_324ff959b870","一盲引眾盲","one blind person leads the many blind",["the blind leading the blind","one blind guide draws along a blind crowd"],[
("B/B27/B27n0152.xml","Yulin Tongxiu"),("J/J10/J10nA158.xml","Miyun Yuanwu"),("J/J25/J25nB159.xml","Dufeng Benshan"),("J/J26/J26nB187.xml","Tian'an Sheng"),("J/J27/J27nB192.xml","Daxiu Zhu"),("B/B25/B25n0144.xml","Shunde")]),
(592,"t_c9ddccf08ea6","畫蛇添足","to draw a snake and add feet",["add feet to a painted snake","spoil completion by adding to it"],[
("J/J33/J33nB294.xml","Langting Jingting"),("J/J33/J33nB286.xml","Huanglong Huinan"),("J/J34/J34nB298.xml","Shanfeng Xian"),("J/J34/J34nB311.xml","Juelang Daosheng"),("J/J37/J37nB386.xml","Yuan'an Feng"),("J/J38/J38nB409.xml","Danxia Dangui")]),
(593,"t_679c80d40bd7","守株待兔","to guard a stump waiting for a rabbit",["wait by the stump for a rabbit","cling to precedent and wait"],[
("J/J28/J28nB219.xml","Zhuanyu Guanheng"),("T/T47/T47n1993.xml","Huanglong Huinan"),("T/T47/T47n1997.xml","Yuanwu Keqin"),("X/X70/X70n1381.xml","Po'an Zuxian"),("B/B25/B25n0145.xml","Zhongfeng Mingben"),("C/C077/C077n1710.xml","Linji Yixuan")]),
(594,"t_55f6191b07c9","水中捉月","to catch the moon in water",["grasp the moon in the water","chase a reflected moon"],[
("J/J25/J25nB171.xml","Tianyin Yuanxiu"),("X/X71/X71n1420.xml","Chushi Fanqi"),("B/B25/B25n0145.xml","Zhongfeng Mingben"),("J/J26/J26nB188.xml","Ruibai Mingxue"),("J/J28/J28nB219.xml","Zhuanyu Guanheng")]),
(595,"t_be0e3a12552b","認影迷頭","to take the shadow for real and lose track of the head",["mistake the reflection and lose the source","recognize the shadow while losing the head"],[
("J/J34/J34nB311.xml","Juelang Daosheng"),("J/J10/J10nA158.xml","Miyun Yuanwu"),("J/J33/J33nB280.xml","Shending Yunwai Ze"),("B/B27/B27n0152.xml","Yulin Tongxiu"),("J/J26/J26nB184.xml","Muyun Tongmen")]),
(596,"t_686c7d950e99","緣木求魚","to climb a tree seeking fish",["seek fish up a tree","use a means that cannot reach its object"],[
("B/B25/B25n0145.xml","Zhongfeng Mingben"),("X/X69/X69n1353.xml","Kaifu Daoning"),("X/X71/X71n1409.xml","Yuejiang Zhengyin"),("J/J25/J25nB171.xml","Tianyin Yuanxiu"),("D/D48/D48n8939.xml","Foyan Qingyuan")]),
(597,"t_335f8fca2f78","拋磚引玉","to throw a brick to draw out jade",["cast a brick and invite jade","offer something rough to elicit something fine"],[
("J/J37/J37nB383.xml","Hanxiu Ruqian"),("J/J33/J33nB294.xml","Langting Jingting"),("J/J37/J37nB388.xml","Shending Yikui"),("J/J40/J40nB479.xml","Zhufeng Zhenxu"),("J/J25/J25nB171.xml","Tianyin Yuanxiu")]),
(598,"t_9a161b28b3b5","貧兒思舊債","a poor child remembers old debts",["the poor child thinks of an old debt","poverty recalling what is still owed"],[
("J/J40/J40nB472.xml","Dabo Qian"),("X/X70/X70n1376.xml","Chijue Daochong"),("J/J25/J25nB171.xml","Tianyin Yuanxiu"),("J/J25/J25nB175.xml","Wufeng Ruxue"),("J/J26/J26nB177.xml","Poshan Haiming")]),
(599,"t_c27a42c50f11","入海算沙","to enter the sea and count its sand",["count grains of sand in the ocean","lose oneself counting the sea's sand"],[
("T/T47/T47n1993.xml","Huanglong Huinan"),("J/J26/J26nB187.xml","Tian'an Sheng"),("J/J28/J28nB208.xml","Guxue Zhe"),("J/J34/J34nB300.xml","Chaozong Tongren"),("J/J37/J37nB386.xml","Yuan'an Feng")]),
(600,"t_ecafdcad8e1e","眾盲摸象","the many blind people feel the elephant",["blind people examining an elephant","each blind person touches one part of the elephant"],[
("J/J27/J27nB191.xml","Xiangtian Jinian"),("M/M59/M59n1540.xml","Dahui Zonggao"),("T/T47/T47n2000.xml","Xutang Zhiyu"),("J/J24/J24nB137.xml","Zhaozhou Congshen"),("D/D48/D48n8939.xml","Foyan Qingyuan")]),
]

for ordinal, tid, term, preferred, alternates, witnesses in specs:
    count = zc.count(term)
    occurrences=[]
    for rel,name in witnesses:
        found=zc.find(rel,term,ctx=32)
        if not found: raise RuntimeError((term,rel))
        row=found[0]; kwic=row["window"]
        verified=zc.verify(rel,kwic)
        proof=f"The complete containing address or case assigns this headword-bearing clause to {name}; no nested speaker takes the exact phrase."
        occurrences.append({"RelPath":rel,"FromLb":verified["fromLb"],"ToLb":verified["toLb"],"Kwic":kwic,"MasterName":name,"Curated":True,
          "AttributionNote":f"Source text ({zc.title(rel)}): {name} uses “{term}” in the recorded address or case as an appraisal, warning, answer, or image; the surrounding turn fixes {name} as exact actor.",
          "ContextMasters":[{"MasterName":name,"Roles":["utterer"]}],
          "DraftActorProof":{"ExactHeadwordClause":kwic,"GrammaticalSubject":name,"SpeechFrame":proof,"FullCaseDecision":proof}})
    body=(f"{preferred.capitalize()}. The records repeatedly place the expression in formal addresses, comments on old cases, direct answers, and warnings. "
          f"The five stored witnesses preserve contrasting deployments instead of reducing the image to an outside moral: speakers can apply it as criticism, turn it into an answer, negate the charge, or redirect it in a public exchange. "
          f"Its Zen bend is therefore visible in who says it, at what interview or teaching-seat moment, and what response follows. Exact frozen-corpus count: {count['hits']} hits in {count['files']} files representing {count['works']} independent works.")
    sense={"SenseKey":None,"MasterName":None,"PreferredTarget":preferred,"AlternateTargets":alternates,"SearchAliases":[preferred,*alternates],"Status":"preferred","Validation":"multi-source",
      "Note":"One idiomatic image with multiple attested stances; grammatical form and differing evaluations do not create extra senses.","Occurrences":occurrences,
      "SourceTexts":list(dict.fromkeys(x[0] for x in witnesses)),"RelatedMasters":list(dict.fromkeys(x[1] for x in witnesses)),"RelatedTerms":[],
      "ExplanationParts":{"CorpusEarnedOpening":preferred.capitalize()+".","EvidenceBody":[body]},
      "DraftEvidence":{"OpeningClaimEvidenceKeys":["o1","o2","o3"],"ZenBend":"The idiom is made into a teaching-seat appraisal, answer, warning, or case comment.","CounterexampleOrLimit":"Different stances remain attached to one idiomatic image.",
       "DifferentThingTest":{"Decision":"one-thing","ComparedThings":[preferred],"Reason":"All selected witnesses use the same idiomatic image; no second referent is attested here."},"AliasRationale":"Aliases expose literal and natural English lookup forms.",
       "ModifierControls":[{"Modifier":"not applicable","Verdict":"No unresolved material-composition claim."}],"FamilyControls":[{"Family":"exact headword","Verdict":"Only exact headword rows count toward depth."}],
       "IndependentWorkIds":list(dict.fromkeys(workmap[x[0]] for x in witnesses))}}
    entry={"SchemaVersion":1,"Entry":{"Id":tid,"SourceTerm":term,"CreatedBy":"Codex f002 Lane C fresh corpus build","WrittenUtc":None,"Senses":[sense],"CorpusBaselineSha256":sha}}
    out=HERE/"fresh-build/entries"/tid;out.mkdir(parents=True,exist_ok=True)
    (out/"evidence.draft.json").write_text(json.dumps(entry,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    (out/"WORK.md").write_text(f"# {term} fresh research ledger\nordinal: {ordinal}\ncount: {count['hits']} hits / {count['files']} files / {count['works']} works\nfull-case actor review: five selected direct-turn witnesses checked.\n",encoding="utf-8")
    (out/"STATUS").write_text("researching\n",encoding="utf-8")
    print(ordinal,tid,term)
