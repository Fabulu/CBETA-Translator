import json
from pathlib import Path
R=Path(__file__).resolve().parents[2]
RUNG=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
def load(i):
 p=R/'fresh-build/entries'/i/'evidence.draft.json';return p,json.loads(p.read_text())
def unnamed(o,respondent,source):
 o['MasterName']=None
 o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'monastic questioner','ActorLabel':'unnamed monastic questioner','ActorRole':'questioner','RungsChecked':RUNG,'GrammarEvidence':'The marked monastic question contains the exact headword; the named teacher answers afterward without repeating it.','ReviewedBy':'Codex fresh f001 lane A independent-review repair','ReviewedUtc':'2026-07-15T00:00:00Z'}
 o['ContextMasters']=[{'MasterName':respondent,'Roles':['respondent','record-owner']}]
 o['AttributionNote']=f'An unnamed monastic questioner in {source} utters the exact headword; {respondent} responds afterward without repeating it.'
 o['DraftActorProof']={'GrammaticalSubject':'the unnamed monastic questioner','FullCaseDecision':f'The unnamed monastic owns the exact headword question; {respondent} is only the respondent.'}
def save(p,d):p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')

p,d=load('t_e6eb14b6c1ca');s=d['Entry']['Senses'][0];unnamed(s['Occurrences'][0],'Miyun Yuanwu','the Recorded Sayings of Chan Master Miyun (密雲禪師語錄)');s['Note']='Seven exact evidence rows across seven independent works support the paired life-giving-sword expression.';save(p,d)

p,d=load('t_d03aa9267f79');s=d['Entry']['Senses'][0];unnamed(s['Occurrences'][0],'Tianyin Yuanxiu','the Recorded Sayings of Master Tianyin (天隱和尚語錄)');s['SearchAliases']=['great capacity and great function','great capacity, great function','great pivot and great function'];s['ExplanationParts']['EvidenceBody']=['Huangbo Xiyun states that he saw Mazu’s great capacity and great function. Tianyin Yuanxiu separately asks about the compound and its two coordinated members, then elsewhere says the pair is not located on a sitting mat or Chan board.','Yulin Tongxiu warns against mistaking coarse abandon for the pair; Guting Shanjian and Poshan Haiming describe its inexhaustibility and whole-body operation. These comparisons concern one coordinated device.'];s['Note']='Six exact evidence rows across five independent works support the coordinated pair.';s['RelatedMasters']=[x for x in s.get('RelatedMasters',[]) if x not in ['Yangshan Huiji','Yuanwu Keqin']];save(p,d)

p,d=load('t_1da939bf1267');s=d['Entry']['Senses'][0];unnamed(s['Occurrences'][4],'Sanshan Denglai','the Recorded Sayings of Sanshan Lai (三山來禪師語錄)');s['Note']='Eleven stored evidence rows remain after governed graphic variants are retained; C077n1710 and D48n8939 are treated as editions of one work rather than independent works.';save(p,d)

p,d=load('t_8650004bb9d7');s=d['Entry']['Senses'][0]
for idx,name,title in [(4,'Fenyang Shanzhao','the Old Recorded Sayings of Venerable Masters (古尊宿語錄)'),(6,'Feiyin Tongrong','the Recorded Sayings of Chan Master Feiyin (費隱禪師語錄)'),(7,'Sanyi Yu','the Recorded Sayings of Chan Master Sanyi (三宜盂禪師語錄)')]:unnamed(s['Occurrences'][idx],name,title)
s['PreferredTarget']='reaching within integration';s['AlternateTargets']=['reaching amid both','the disputed fourth of Dongshan’s Five Ranks'];s['SearchAliases']=['fourth Five Rank','reaching within integration','reaching amid both','partial within reaching'];s['ExplanationParts']['CorpusEarnedOpening']='Within the Caodong Five Ranks, reaching within integration names a disputed fourth-slot label; the English “reaching” keeps 至 distinct from the fifth rank’s 到, while the exact nuance remains uncertain.';s['Note']='Eight stored evidence rows preserve the exact label and its governed alternate; parallel Caoshan witnesses are canonicalized by independent work rather than raw file count.';save(p,d)
