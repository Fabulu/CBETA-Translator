import json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
B='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a'
rows=[
('T/T51/T51n2077.xml','上堂良久曰。夫行脚禪流直須著忖。','Shexian Guisheng'),
('T/T51/T51n2077.xml','良久曰。春雨一滴滑如油。','Guyin Yuncong'),
('T/T51/T51n2077.xml','良久曰。美食不中飽人喫。便下座。','Jiufeng Qin'),
('X/X82/X82n1571.xml','且作麼生即是？良久曰：還會麼？珍重！','Tianyi Yihuai'),
('X/X81/X81n1568.xml','上堂，良久曰：大眾看看。便下座。','Wuyun Zhifeng'),
('X/X84/X84n1583.xml','且道彌勒在甚麼處？良久曰：夜行莫踏白，不是水便是石。','Sengzhao'),
('J/J36/J36nB369.xml','眾無語。師良久曰：「金輪懶向當堂坐，何用丹墀擊靜鞭？」','Zhean Fan'),
('X/X80/X80n1565.xml','可良久曰。覔心了不可得。祖曰。我與汝安心竟。','Dazu Huike'),
]
oc=[]
for rel,k,name in rows:
 v=zc.verify(rel,k);assert v['ok'],(rel,k,v)
 oc.append({'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':k,'MasterName':name,'ContextMasters':[{'MasterName':name,'Roles':['utterer']}],'Curated':True,'AttributionNote':f'{zc.title(rel)}: complete-case reading identifies {name} as the person who speaks after the narrated pause.'})
entry={'Id':'t_d926adb80feb','SourceTerm':'良久曰','CreatedBy':'Codex fresh-build lane C','WrittenUtc':None,'CorpusBaselineSha256':B,'Senses':[{'SenseKey':None,'PreferredTarget':'after a while, he said','AlternateTargets':['after a pause, he said','after a long pause, he said'],'SearchAliases':['after a while he said','after a pause he said','long pause then said'],'Status':'preferred','Explanation':'After a while, he said is a narrative turn marker: the named or contextually established speaker pauses before giving the following words. It appears in hall addresses, exchanges, and retold cases. The pause is narrated by the recorder, but the verb of speaking belongs to the person who delivers the following reply; each occurrence therefore identifies that exact speaker rather than the book owner by default.','Validation':'multi-source','Note':'The frozen corpus has 2,184 exact hits in 98 files representing 95 works. Eight anchors cover hall addresses, replies after questions, a response after group silence, and transmitted early cases across six independent works. Repeated witnesses from one work do not inflate work spread.','Occurrences':oc,'SourceTexts':sorted({r[0] for r in rows}),'RelatedMasters':sorted({r[2] for r in rows}),'RelatedTerms':['良久','曰','默然']}]}
out=ROOT/'fresh-build/entries/t_d926adb80feb';out.mkdir(parents=True,exist_ok=True);(out/'entry.v2.json').write_text(json.dumps(entry,ensure_ascii=False,indent=2)+'\n');(out/'STATUS').write_text('drafted\n')
