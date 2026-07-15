from pathlib import Path
import hashlib,json,subprocess,sys
from f005_author_lib import R,BASE,NOW,occurrence,sense,work_text

specs=[]
specs.append(('t_114ad0f001c1','腰纏十萬貫',sense(
 'ten thousand strings of cash around the waist',['wearing ten thousand strings of cash'],
 ['ten thousand cash at the waist','wealth and a crane to Yangzhou','ride a crane to Yangzhou'],
 'Ten thousand strings of cash around the waist supplies the wealth in the comic wish to be rich, immortal, and immediately in Yangzhou: the next line rides there on a crane. Chan masters preserve the complete impossible combination as a verse ending, an answer, and a criticism of wanting every incompatible advantage at once.',
 ['Tangzhong Zhongren caps his imperial verse with the money-and-crane couplet.','Dahui Zonggao adds the couplet to his comment on Juzhi’s finger.','Yun’e Xi answers a question about the Buddha’s star-sighting with the same Yangzhou wish.','Juelang Daosheng expands the line into a conditional promise that everyone may ride to Yangzhou.','A later letter explicitly invokes the saying against refusing to relinquish anything while hoping to gain everything.'],
 [occurrence('X/X82/X82n1571.xml','腰纏十萬貫','Tangzhong Zhongren','The named Tangzhong Zhongren biography introduces the exact headword-bearing verse as his composition.'),
  occurrence('T/T47/T47n1998A.xml','腰纏十萬貫','Dahui Zonggao','Dahui Zonggao announces his own added verse before reciting the money-and-crane line.'),
  occurrence('J/J28/J28nB203.xml','腰纏十萬貫','Yun’e Xi','The reply frame in Yun’e Xi’s own exchange assigns the exact answer to him.'),
  occurrence('J/J34/J34nB311.xml','腰纏十萬貫','Juelang Daosheng','Juelang Daosheng quotes and extends the Yangzhou couplet inside his own hall address.'),
  occurrence('J/J38/J38nB406.xml','腰纏十萬貫','Baochi Jizong','Baochi Jizong names the saying in his signed letter while describing the incompatible wish.')],
 'Seventy-seven exact hits occur in fifty-one frozen works; five distinct deployments retain the full Yangzhou setting rather than isolating the money clause.','The Chan record uses the inherited impossible wish as a portable capping line and as an explicit comparison for trying to retain and gain everything simultaneously.','The cash remains wealth in the saying; the record does not establish that money itself has a special Zen symbolism, and the phrase is incomplete without testing its crane-and-Yangzhou continuation.','Compared 騎鶴上揚州, 俱胝一指, and the impossible-wish family; the continuation is family evidence rather than a second sense.', ['騎鶴上揚州'])))

specs.append(('t_38586eed0d08','針劄不入',sense(
 'a needle cannot pierce it',['needle-impervious'],['impenetrable to a needle','needle cannot enter','too solid to prick'],
 'A needle cannot pierce it describes a surface or condition without even the smallest point of entry. Chan masters use the phrase as a direct answer, contrast it with being open in seven directions, and apply it to an iron-solid eye, a diamond, empty space, or the unresolved place where one must still find an entrance.',
 ['Guangjiao Shoune answers “what is the patch-robed person’s true eye?” with the phrase.','Puan Yinsu calls the diamond-like item impervious to a needle.','Mingjue Chongxian answers a question about actual study with the same line, then says that water reaches its channel.','Shitou Xiqian says his place admits no needle; Yaoshan Weiyan answers that his place is like flowers planted on stone.','Zuliang Qi sets the needle-tight condition against a bag open in seven directions.','Lüyan He quotes Chushi Fan’s use of the phrase for the ten directions of empty space.'],
 [occurrence('X/X82/X82n1571.xml','針劄不入','Guangjiao Shoune','The named Guangjiao Shoune exchange assigns the exact reply to him.'),
  occurrence('X/X69/X69n1356.xml','針劄不入','Puan Yinsu','Puan Yinsu utters the phrase in his own letter while describing the diamond-like item.'),
  occurrence('X/X78/X78n1556.xml','針劄不入','Mingjue Chongxian','The question-and-answer frame assigns this exact reply to Mingjue Chongxian.'),
  occurrence('X/X80/X80n1565.xml','針劄不入','Shitou Xiqian','The complete Shitou–Yaoshan exchange explicitly gives this exact line to Shitou Xiqian.'),
  occurrence('J/J39/J39nB449.xml','針劄不入','Zuliang Qi','Zuliang Qi utters both sides of the needle-tight/open-bag contrast in his own hall address.'),
  occurrence('J/J39/J39nB452.xml','針劄不入','Lüyan He','Lüyan He recites Chushi Fan’s attributed line before giving his own response.'),
  occurrence('X/X71/X71n1420.xml','針劄不入','Chushi Fanqi','Chushi Fanqi states his own empty-space line in his named hall record.')],
 'One hundred fifty exact hits occur in ninety-three frozen works. Seven witnesses cover answer, object predicate, spatial contrast, quoted precedent, and later independent reuse.','The Chan bend lies in moving the smallest imaginable physical penetration across eyes, space, speech, and public answers while preserving the same no-entry constraint.','Imperviousness can be commended or criticized; the phrase alone does not establish whether the condition is desirable, nor does it name a substance.','Compared 水洩不通, 七穿八穴, 風吹不入, and 水灑不著; incompatible predicates were checked but the stored uses retain one penetration image.', ['風吹不入','水灑不著'])))

specs.append(('t_6efa9006e436','水灑不著',sense(
 'water cannot wet it',['water will not stick'],['water cannot touch it','unwetted by water','water does not adhere'],
 'Water cannot wet it is an imperviousness formula: poured or splashed water fails to adhere. Chan masters use it as an answer about what rain cannot moisten, pair it with wind failing to enter, and then test the supposedly sealed condition rather than treating it as an automatic success.',
 ['Zhaoqing Daoxian answers that the place universal rain does not moisten is “water cannot wet it.”','Beiyuan Tong answers a question about this condition with “dry and bare.”','Yuanwu Keqin pairs it with wind not entering and a diamond sword.','Yunyan Tansheng gives it as his answer about Yaoshan after extinction.','Huqiu Shaolong says that even wind-proof and water-proof composure cannot save itself when inspected.'],
 [occurrence('X/X80/X80n1565.xml','水灑不著','Zhaoqing Daoxian','The complete exchange assigns this exact reply to Zhaoqing Daoxian.'),
  occurrence('X/X81/X81n1568.xml','水灑不著','Beiyuan Tong','The Beiyuan Tong section assigns the answer “dry and bare” after the exact headword question.'),
  occurrence('T/T48/T48n2003.xml','水灑不著','Yuanwu Keqin','Yuanwu Keqin utters the paired formula in his own case commentary.'),
  occurrence('T/T51/T51n2076.xml','水灑不著','Yunyan Tansheng','The named exchange gives this exact answer about Yaoshan to Yunyan Tansheng.'),
  occurrence('X/X84/X84n1583.xml','水灑不著','Huqiu Shaolong','Huqiu Shaolong utters the paired formula and its explicit limitation in one hall address.')],
 'Seventy-five exact hits occur in forty-four frozen works. Five independent witnesses include answers, comparison, paired formula, and an explicit negative check.','The record bends the ordinary no-wetting constraint into a recurrent appraisal, but Huqiu explicitly refuses to let impermeability stand as sufficient by itself.','The paired wind formula and the opposite water-channel language limit the entry to impermeability; they do not license a doctrine of purity.','Compared 風吹不入, 水到渠成, 乾剝剝地, and the no-hole iron hammer; the water and wind clauses remain linked but separately searchable.', ['風吹不入'])))

specs.append(('t_a14bd52beff8','風吹不入',sense(
 'wind cannot enter it',['wind-impervious'],['no wind can enter','sealed against wind','wind cannot get in'],
 'Wind cannot enter it describes something sealed so completely that even moving air finds no opening. Chan records pair the line with water not wetting, needles not piercing, fire not burning, and blades not cutting, then use those impossible protections in questions and appraisals whose adequacy remains open to challenge.',
 ['Fachang Yiyu compares the paired wind-and-water condition to a hammer without a hole.','Guangzhao asks the assembly what cannot be gathered, scattered, penetrated by wind, wetted, burned, or cut.','Gulin Qingmao applies the line to the patch-robed person’s true eye during a lantern-festival address.','Huqiu Shaolong explicitly couples “wind cannot enter” with “mixing mud and water,” refusing a simple opposition.','Yuanwu Keqin includes the wind-, water-, and needle-impervious person among the requirements of a free actor.','A later adjudication joins the raw-iron image to wind-imperviousness.'],
 [occurrence('X/X82/X82n1571.xml','風吹不入','Fachang Yiyu','Fachang Yiyu utters the paired formula and no-hole-hammer comparison in his named hall section.'),
  occurrence('X/X78/X78n1556.xml','風吹不入','Guangzhao','Guangzhao governs the uninterrupted hall question listing the impossible protections.'),
  occurrence('X/X71/X71n1412.xml','風吹不入','Gulin Qingmao','Gulin Qingmao utters the phrase in his own lantern-festival hall address.'),
  occurrence('X/X84/X84n1583.xml','風吹不入','Huqiu Shaolong','Huqiu Shaolong states both directions of the wind-and-mud relation in one address.'),
  occurrence('T/T47/T47n1997.xml','風吹不入','Yuanwu Keqin','Yuanwu Keqin utters the phrase in his own informal address among two further imperviousness predicates.'),
  occurrence('X/X66/X66n1297.xml','風吹不入','Weishan Lingyou','The Weishan section’s later adjudication applies the raw-iron and wind-impervious predicates to the case actor.'),
  occurrence('X/X69/X69n1359.xml','風吹不入','Dahui Zonggao','Dahui Zonggao uses the paired wind-and-water phrase in his signed instruction to a fundraiser.')],
 'One hundred forty-two exact hits occur in eighty-three frozen works. Seven independent witnesses cover object question, eye appraisal, address, relational formula, and linked impermeability tests.','The Chan use accumulates impossible physical protections around an object or person, then exposes those protections to interview and counterformula rather than defining them as final attainment.','The record itself pairs wind-imperviousness with mixing mud and water, so the entry cannot equate sealedness with aloofness or purity.','Compared 水灑不著, 針劄不入, 火燒不得, 刀斫不斷, and 無孔鐵鎚; all preserve one no-entry predicate.', ['水灑不著','針劄不入'])))

specs.append(('t_b0df4ae7015d','荊棘林',sense(
 'a thorn thicket',['thorn forest'],['bramble thicket','forest of thorns','tangled thornwood'],
 'A thorn thicket is dense, catching growth through which ordinary passage is painful and difficult. Chan masters make entering, leaving, or crossing it a measure in public address: Yunmen says the level ground kills countless people but one who gets through the thicket is skilled, while later speakers can point to a whisk or the encounter itself as the thicket still to cross.',
 ['Cijue Zongze describes people boring their heads into the thicket while believing only living beings suffer.','Yunmen Wenyan’s formula contrasts deaths on level ground with the skilled person who gets through the thorns.','A later hall speaker raises Yunmen, then declares that the whisk itself is the thorn thicket and asks how anyone crosses it.','Yunju Daoqi answers a request to clear a road through the thicket by asking where the questioner intends to go.','Yaoshan Weiyan places the questioner’s red and ulcerated parents lying in the thicket.','Xiatang Huiyuan describes pulling one’s feet free of the thicket while wearing the inherited robe.','Liao’an Qingyu says the chief seat strides within the thicket and pulls out the stakes rooted by others.'],
 [occurrence('X/X82/X82n1571.xml','荊棘林','Cijue Zongze','Cijue Zongze utters the head-boring thorn-thicket line in his named hall section.'),
  occurrence('X/X64/X64n1260.xml','荊棘林','Yunmen Wenyan','The source explicitly attributes the level-ground/thorn-thicket formula to Yunmen Wenyan.'),
  occurrence('X/X68/X68n1318.xml','荊棘林','Foyan Qingyuan','Foyan Qingyuan raises Yunmen and then identifies his own whisk as the thorn thicket in the continuing address.'),
  occurrence('X/X81/X81n1568.xml','荊棘林','Yunju Daoqi','The complete exchange assigns the road-clearing counterquestion to Yunju Daoqi.'),
  occurrence('X/X80/X80n1565.xml','荊棘林','Yaoshan Weiyan','The complete return-home exchange assigns the red-parent thorn-thicket description to Yaoshan Weiyan.'),
  occurrence('X/X69/X69n1360.xml','荊棘林','Xiatang Huiyuan','Xiatang Huiyuan utters the foot-pulling line in his own uninterrupted address.'),
  occurrence('X/X71/X71n1414.xml','荊棘林','Liao’an Qingyu','Liao’an Qingyu utters the chief-seat line in his own guest address.'),
  occurrence('J/J37/J37nB388.xml','荊棘林','Shending Yikui','Shending Yikui raises Yunmen’s exact formula while addressing the completed retreat.')],
 'Six hundred one exact hits occur in 189 frozen works. Eight curated witnesses preserve inherited formula, answer, bodily danger, implement identification, and later institutional address.','The thicket keeps its snagging physical constraint while Chan speakers relocate it into the interview itself: the obstacle may be the road asked for, the raised whisk, or the inherited formula now being tested.','Passing the thicket is sometimes praised, yet later comments say even a crossing can remain caught; the image therefore does not name a single stage or guaranteed result.','Compared 蒺藜園, 平地上死人無數, 過得荊棘林, and thorn-thicket action verbs; the forest and related caltrop garden were not merged.', ['蒺藜園'])))

pending=R/'fresh-build/pending-roster.json';pd=json.loads(pending.read_text());known={m['names'][0] for m in json.loads((R.parents[3]/'Assets/Data/master-dates.json').read_text())['masters']};have={x['canonicalName'] for x in pd['candidates']}
for eid,term,s in specs:
 for o in s['Occurrences']:
  n=o['MasterName']
  if n not in known and n not in have:
   pd['candidates'].append({'canonicalName':n,'aliases':[n],'evidence':[{k:o[k] for k in ('RelPath','FromLb','ToLb','Kwic')}],'reviewedBy':'Codex f005 lane A author','reviewReport':'fresh-build/waves/f005-laneA-1213-1222-full-composite.json','status':'awaiting-roster-integration'});have.add(n)
pending.write_text(json.dumps(pd,ensure_ascii=False,indent=2)+'\n')

rows=[]
for eid,term,s in specs:
 b=R/'fresh-build/entries'/eid;b.mkdir(parents=True,exist_ok=True)
 draft={'SchemaVersion':1,'Entry':{'Id':eid,'SourceTerm':term,'CorpusBaselineSha256':BASE,'CreatedBy':'Codex f005 lane A author','WrittenUtc':NOW,'Senses':[s]}}
 wp=b/'evidence.draft.json';wp.write_text(json.dumps(draft,ensure_ascii=False,indent=2)+'\n');(b/'WORK.md').write_text(work_text(term,s))
 ep=b/'entry.v2.json';rp=b/'evidence-compile-report.json';q=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(wp),'--output',str(ep),'--report',str(rp)],text=True,capture_output=True)
 if q.returncode:raise SystemExit(q.stdout+q.stderr)
 rows.append((eid,hashlib.sha256(ep.read_bytes()).hexdigest()))
out=R/'fresh-build/waves/f005-laneA-1213-1217-author-ledger.json';payload={'schemaVersion':1,'wave':'f005','lane':'A','ordinals':[1213,1217],'entries':[{'id':i,'sha256':h} for i,h in rows],'writtenUtc':NOW};tmp=out.with_suffix('.tmp');tmp.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n');tmp.replace(out);print(json.dumps(payload,ensure_ascii=False,indent=2))
