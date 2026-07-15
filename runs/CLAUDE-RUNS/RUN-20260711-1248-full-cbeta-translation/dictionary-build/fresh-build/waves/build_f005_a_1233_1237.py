from pathlib import Path
import json,subprocess,sys,hashlib
from f005_author_lib import R,BASE,NOW,occurrence,sense,work_text

def occs(rel,term,master,n,decision):
 return [occurrence(rel,term,master,decision,i) for i in range(n)]

S=[]
S.append(('t_600aa5eb8aee','白雲萬里',sense('white clouds for ten thousand miles',['ten thousand miles of white cloud'],['white clouds far away','lost beyond white clouds'],
'White clouds stretching ten thousand miles make distance visible. Chan masters repeatedly place the line after hesitation, conceptual pursuit, or a response that has missed the encounter: the person is already vastly far away.',
['Muting Pufu answers an enacted response and several inherited formulas with the distance line.','Yongjue Yuanxian says that turning the eyes and searching again puts the assembly ten thousand miles away.'],
occs('J/J40/J40nB493.xml','白雲萬里','Muting Pufu',4,'The complete discourse in Muting Pufu’s own record assigns the exact line to him.')+occs('X/X72/X72n1437.xml','白雲萬里','Yongjue Yuanxian',2,'The complete address in Yongjue Yuanxian’s own record assigns the exact line to him.'),
'Four hundred fifty exact hits occur across the frozen corpus; six selected turns in two independent works preserve answer, warning, and distance verdict.','The landscape becomes an interview verdict: renewed calculation or a failed move is not merely mistaken but already ten thousand miles away.','The line can also occur as scenery; this entry claims the verdict only where the stored speech explicitly follows a failed or delayed move.','Compared distance predicates, 擬議, and white-cloud scenery.',[])))
S.append(('t_e0adf3bcdebd','一棒一條痕',sense('one blow, one welt',['each blow leaves one mark'],['one strike one mark','a blow leaves a trace'],
'One blow leaving one welt describes force that lands exactly enough to remain visible. Chan masters use the paired formula for consequential action, then sometimes say that even this clean impact still drags through mud and water.',
['Panzilong Zisu closes a live exchange by striking and declaring one blow, one welt.','Yuelin Shiguan answers a question about the true saying with the paired blow-and-blood formula.'],
occs('J/J37/J37nB396.xml','一棒一條痕','Panzilong Zisu',4,'The complete exchange in Panzilong Zisu’s record assigns the exact line to him.')+occs('X/X69/X69n1354.xml','一棒一條痕','Yuelin Shiguan',2,'The complete address in Yuelin Shiguan’s own record assigns the exact formula to him.'),
'One hundred seventy-one exact hits occur in the frozen corpus. Six turns in two independent records preserve live striking and explicit appraisal.','The Chan bend is accountable impact: a blow is judged by the trace it actually leaves, not by a claim of ferocity.','Some records criticize even perfectly marked striking as still entangled; the formula is not universal praise.','Compared 一摑一掌血, 棒, and striking predicates.',['一摑一掌血'])))
S.append(('t_200c608c05d7','一摑一掌血',sense('one slap, one palmful of blood',['each slap draws blood'],['one slap one hand blood','a slap leaves blood'],
'One slap producing a palmful of blood intensifies the demand that an action show an immediate result. Chan records pair it with one blow leaving one welt, use the pair as an answer, and also test whether such force is still incomplete.',
['Yuelin Shiguan gives the paired formula as his answer about a true saying.','Qingzhong Tiebi Ji places it in a hall verse whose impossible images widen what counts as effective contact.'],
occs('X/X69/X69n1354.xml','一摑一掌血','Yuelin Shiguan',3,'The complete record assigns the exact paired formula to Yuelin Shiguan.')+occs('J/J29/J29nB240.xml','一摑一掌血','Qingzhong Tiebi Ji',2,'The complete hall verses in Qingzhong Tiebi Ji’s own record assign the exact line to him.'),
'Seventeen exact hits occur in fourteen frozen files. Five turns in two independent records retain answer and verse deployments.','The bloody palm is not ornament: the public formula demands an observable consequence from the slap.','The physical image does not license a historical claim that every cited exchange involved literal injury.','Compared 一棒一條痕 and striking-result formulas.',['一棒一條痕'])))
S.append(('t_8be6a86878e8','開眼尿床',sense('wetting the bed with eyes open',['wide awake yet wetting the bed'],['awake bedwetting','eyes open still asleep'],
'Wetting the bed with one’s eyes open makes failure worse by removing the excuse of sleep. Chan masters use it for someone visibly awake and verbally active who nevertheless misses what is directly happening.',
['Juelang Daosheng answers a sequence about the Buddha’s career by calling the teaching performance open-eyed bedwetting.','He also uses the phrase elsewhere for knowing talk that remains caught in its own display.'],
occs('J/J34/J34nB311.xml','開眼尿床','Juelang Daosheng',5,'The complete exchange or letter in Juelang Daosheng’s own full record assigns the exact phrase to him.')+occs('J/J25/J25nB174.xml','開眼尿床','Juelang Daosheng',3,'The complete discourse in Juelang Daosheng’s shorter record assigns the exact phrase to him.'),
'Fifty-three exact hits occur in thirty-two frozen files. Eight deployments across two independent records preserve answer, insult, and warning.','The Chan insult targets failure in full view: open eyes do not guarantee that the speaker has met the case.','The phrase describes the cited failure; it is not a medical diagnosis or evidence about literal sleep.','Compared 夢中說夢, 眼, and waking/sleep predicates.',['夢中說夢'])))
S.append(('t_e5de33930e3b','夢中說夢',sense('telling a dream inside a dream',['dreaming within a dream'],['speaking dreams in a dream','a dream commenting on dreams'],
'Telling a dream inside a dream is speech that comments without leaving the condition it describes. Chan masters apply it to doctrinal distinctions, inherited explanations, and their own words, so the phrase can expose recursive talk without placing the present voice outside it.',
['Chushi Fanqi applies the phrase while sorting inherited explanations.','Yongjue Yuanxian extends it to buddhas, patriarchs, old masters, himself, and the assembly in one uninterrupted address.'],
occs('X/X71/X71n1420.xml','夢中說夢','Chushi Fanqi',3,'The complete commentary in Chushi Fanqi’s own record assigns the exact phrase to him.')+occs('X/X72/X72n1435.xml','夢中說夢','Wuyi Yuanlai',4,'The complete discourse in Wuyi Yuanlai’s own record assigns the exact phrase to him.'),
'One hundred ten exact hits occur in sixty-four frozen files. Seven selected deployments in two independent works retain recursive critique and self-inclusion.','Zen bends the idiom back onto the teaching seat: the master can call even his own present speech another dream told within the dream.','The line does not prove that every statement is false; it marks the recursive status assigned in the cited passages.','Compared 開眼尿床 and explicit waking/dream predicates.',['開眼尿床'])))

pending=R/'fresh-build/pending-roster.json';pd=json.loads(pending.read_text());have={x['canonicalName'] for x in pd['candidates']}
for n in ['Panzilong Zisu','Qingzhong Tiebi Ji']:
 if n not in have: pd['candidates'].append({'canonicalName':n,'aliases':[n],'evidence':[],'reviewedBy':'Codex f005 lane A author','reviewReport':'fresh-build/waves/f005-laneA-1233-1237-full-composite.json','status':'awaiting-roster-integration'})
pending.write_text(json.dumps(pd,ensure_ascii=False,indent=2)+'\n')
ids=[]
for eid,term,s in S:
 b=R/'fresh-build/entries'/eid;b.mkdir(parents=True,exist_ok=True);w=b/'evidence.draft.json';w.write_text(json.dumps({'SchemaVersion':1,'Entry':{'Id':eid,'SourceTerm':term,'CorpusBaselineSha256':BASE,'CreatedBy':'Codex f005 lane A author','WrittenUtc':NOW,'Senses':[s]}},ensure_ascii=False,indent=2)+'\n');(b/'WORK.md').write_text(work_text(term,s));q=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(w),'--output',str(b/'entry.v2.json'),'--report',str(b/'evidence-compile-report.json')],capture_output=True,text=True);assert q.returncode==0,q.stdout+q.stderr;ids.append(eid)
print(' '.join(ids))
