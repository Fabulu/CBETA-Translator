import json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
B='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a'
rows=[
('J/J36/J36nB359.xml','結制，上堂。「十五日已前，卒風暴雨；十五日已後，朗月晴空。','Baiyu Jingsi'),
('J/J27/J27nB190.xml','結制，上堂。拈疏，曰：「釋迦拈的、迦葉笑的、達磨坐的、神光立的','Shiyu Mingfang'),
('J/J36/J36nB369.xml','結制，小參。「聖制咸尊十月半，龍蛇好向紅爐鍛','Zhean Fan'),
('J/J37/J37nB383.xml','結制，上堂。僧問：「實際理地不受一塵，佛事門中缺一不可','Hanxiu Qian'),
('J/J27/J27nB192.xml','結制上堂。拈香云：「這些子，從來匝地普天','Daxiu Zhu'),
('J/J39/J39nB454.xml','結制，上堂。豎拂子曰：「祇者些子，名不得，狀不得。','Pin Jixiang'),
('J/J26/J26nB186.xml','結制上堂。「每歲到冬方結制，知浴化主甚辛勤。','Linye Qi'),
('L/L158/L158n1652.xml','十月初一日入內奉旨於十五日啟期結制是日御駕親臨命欽差官李昌祚同僧錄司執香迎請','Mingjue Cong'),
]
oc=[]
for rel,kwic,owner in rows:
 v=zc.verify(rel,kwic);assert v['ok'],(rel,kwic,v)
 title=zc.title(rel)
 oc.append({'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kwic,'MasterName':None,'ActorAttribution':{'Status':'impersonal','Kind':'editorial occasion heading','ActorLabel':'an impersonal occasion label supplied by the recorder','ActorRole':'compiler','GrammarEvidence':'The headword labels the occasion or date of the following address; it is recorder-governed metadata, not words uttered by the record owner in the quoted address.','ReviewedBy':'Codex fresh lane-C full-case review','ReviewedUtc':'2026-07-14T17:40:00Z'},'ContextMasters':[{'MasterName':owner,'Roles':['record-owner']}],'Curated':True,'AttributionNote':f'{title}: the recorder uses the headword as an occasion/date label for an address by {owner}; it is editorial metadata rather than the teacher’s utterance.'})
entry={'Id':'t_b0f2ccf6d140','SourceTerm':'結制','CreatedBy':'Codex fresh-build lane C','WrittenUtc':None,'CorpusBaselineSha256':B,'Senses':[{'SenseKey':None,'MasterName':None,'PreferredTarget':'begin the monastic restriction period','AlternateTargets':['open the restriction period','begin the seasonal retreat period'],'SearchAliases':['begin restriction period','open retreat period','restriction ceremony','seasonal retreat opening'],'Status':'preferred','Explanation':'To begin the monastic restriction period is to formally open a fixed seasonal interval in which the resident community follows its established schedule and limits. Recorded-sayings collections commonly use the term as an occasion heading before an address or short evening instruction. Paired references to releasing the restriction mark the opposite boundary. The stored headings therefore attest an institutional calendar action, not an utterance by the teacher whose address follows.','Validation':'multi-source','Note':'The frozen corpus has 2,188 exact hits in 273 files representing 270 independent works. Eight anchors from eight works show winter and summer openings, hall addresses, evening instruction, an imperial opening date, and explicit pairing with release of the restriction. Table-of-contents-only hits were excluded.','Occurrences':oc,'SourceTexts':[r[0] for r in rows],'RelatedMasters':[r[2] for r in rows],'RelatedTerms':['解制','結夏','安居','小參']}]}
out=ROOT/'fresh-build/entries/t_b0f2ccf6d140';out.mkdir(parents=True,exist_ok=True);(out/'entry.v2.json').write_text(json.dumps(entry,ensure_ascii=False,indent=2)+'\n');(out/'STATUS').write_text('drafted\n')
