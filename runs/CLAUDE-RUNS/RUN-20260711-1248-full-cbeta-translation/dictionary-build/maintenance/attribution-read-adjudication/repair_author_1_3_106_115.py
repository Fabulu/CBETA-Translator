import hashlib, json
from copy import deepcopy
from datetime import datetime, timezone
from pathlib import Path

BUILD=Path(__file__).resolve().parents[2]; STAMP=datetime.now(timezone.utc).isoformat()
IDS=['t_ff3b9302050a','t_0051fc72360c','t_041f65670cd4','t_04efe13911ae','t_073dcbf657a3','t_09ed57d56bcf','t_0a686fa27769','t_0c53a5a2243b','t_11a4ff234f5a','t_135a001a5b0e']
RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']; LEDGER=[]
def path(i): return BUILD/'fresh-build'/'entries'/i/'entry.v2.json'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def load(i):
 p=path(i); return p,json.loads(p.read_text()),sha(p)
def cms(*xs): return [{'MasterName':n,'Roles':list(rs)} for n,rs in xs]
def named(o,n,note,*contexts):
 o['MasterName']=n; o.pop('ActorAttribution',None); o['ContextMasters']=cms((n,('utterer',)),*contexts); o['AttributionNote']=note
def anon(o,kind,label,role,e,note,*contexts,status='reviewed-unnamed'):
 o['MasterName']=None; o['ActorAttribution']={'Status':status,'Kind':kind,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':e,'ReviewedBy':'Codex cohorts 1-3 106-115 full-case READ-AND-FIX','ReviewedUtc':STAMP}; o['ContextMasters']=cms(*contexts); o['AttributionNote']=note
def save(p,d,old,findings):
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n'); LEDGER.append({'Id':d['Id'],'SourceTerm':d['SourceTerm'],'oldSha256':old,'newSha256':sha(p),'findings':findings})

# 逢場作戲 — six direct record-owner uses and one anonymous preface voice.
p,d,old=load(IDS[0]); o=d['Senses'][0]['Occurrences']
anon(o[5],'preface author','the unnamed preface author','compiler','The headword occurs in evaluative preface prose, not in Guting Shanjian\'s quoted speech; no personal signature is present in the complete supplied unit.','Recorded Sayings of Guting Shanjian (古庭禪師語錄), anonymous preface: the writer uses the phrase while discussing the record\'s transmission.',('Guting Shanjian',('person-described','record-owner')))
save(p,d,old,['read all 7 complete units','corrected preface prose falsely assigned to Guting Shanjian'])

# 臨濟四喝 — distinguish authors/commentators from anonymous monks inside raised cases and a heading.
p,d,old=load(IDS[1]); o=d['Senses'][0]['Occurrences']
anon(o[0],'monastic questioner','the unnamed monk','questioner','僧問 introduces the headword-bearing question inside Zhongfeng Mingben\'s raised case; Zhongfeng\'s own comment precedes it.','Extensive Record of Zhongfeng Mingben (天目中峰廣錄): an unnamed monk asks Xuefeng Yicun about Linji\'s four shouts; Zhongfeng Mingben raises the case.',('Zhongfeng Mingben',('later-raiser','record-owner')),('Xuefeng Yicun',('respondent','case-figure')))
named(o[1],'Qigang Zong','Mirror of the Lineage Dharma Grove (宗鑑法林): Qigang Zong explicitly comments on attempts to elucidate Linji\'s four shouts.',('Linji Yixuan',('person-discussed','case-figure')))
anon(o[2],'monastic questioner','the unnamed monk','questioner','有舉僧問 introduces a quoted anonymous monk\'s question; the surrounding long discourse is compiler-preserved prose.','Jiatai Universal Lamp Record (嘉泰普燈錄): an unnamed monk asks Xuefeng Yicun about Linji\'s four shouts.',('Xuefeng Yicun',('respondent','case-figure')),('Linji Yixuan',('person-discussed','case-figure')))
anon(o[3],'paratext heading','the verse heading','compiler','The exact string occurs in the title 臨濟四喝示月春禪人 and is not spoken in the verse body.','Recorded Sayings of Buhui Mingzong (不會禪師語錄): a compiler-preserved heading identifies Buhui Mingzong\'s verse on Linji\'s four shouts.',('Buhui Mingzong',('verse-author','record-owner')),('Linji Yixuan',('person-discussed','case-figure')),status='impersonal')
anon(o[4],'monastic questioner','the unnamed monk','questioner','僧問雪峰 marks the headword-bearing question inside Chaozong Tongren\'s later exposition.','Recorded Sayings of Chaozong Tongren (朝宗禪師語錄): an unnamed monk asks Xuefeng Yicun about Linji\'s four shouts; Chaozong Tongren later raises the case.',('Chaozong Tongren',('later-raiser','record-owner')),('Xuefeng Yicun',('respondent','case-figure')))
save(p,d,old,['read all 5 complete units','separated two direct commentators, three quoted questioners, and one paratext heading'])

# 無心 — direct masters, two questioners, two anonymous monks, and an imperial author.
p,d,old=load(IDS[2]); a=d['Senses'][0]['Occurrences']; b=d['Senses'][1]['Occurrences']
anon(a[5],'monastic questioner','the unnamed monk','questioner','因僧問 assigns 無心道人 to an unnamed monk; Luopu Yuan\'an answers only after 師曰.','Linked Collection of Chan Verses (禪宗頌古聯珠通集): an unnamed monk asks Luopu Yuan\'an about the no-mind wayfarer.',('Luopu Yuan\'an',('respondent','case-figure')))
anon(a[6],'monastic questioner','the unnamed monk','questioner','僧問 introduces the question; Falan Cheng\'s reply starts at 師曰.','Recorded Sayings of Falan Cheng (玉泉蓮月正禪師語錄): an unnamed monk asks Falan Cheng what a no-mind wayfarer is.',('Falan Cheng',('respondent','record-owner')))
anon(b[0],'monastic speakers','the two unnamed monks','interlocutor','二比丘共語曰 assigns the quoted 無心 clauses to two unnamed monks.','Recorded Sayings of Dongshan Meixi Du (洞山梅溪度禪師語錄): Dongshan raises a case in which two unnamed monks say they acted without intending to.',('Dongshan Meixi Du',('later-raiser','record-owner')))
anon(b[2],'imperial author','the Kangxi Emperor','author','朕之所言 is first-person imperial prose in the imperially selected record.','Imperially Selected Recorded Sayings (御選語錄): the Kangxi Emperor writes that earlier sayings coincided unintentionally with his own words.',status='identified-non-master')
save(p,d,old,['read all 11 complete units','corrected two questioners, two anonymous monks, and imperial authorship'])

# 不識 — named layman, Huangbo, Bao'en, and repeated anonymous pig-carriers.
p,d,old=load(IDS[3]); o=d['Senses'][0]['Occurrences']
anon(o[0],'lay interlocutor','Li Linzong','interlocutor','李曰 explicitly assigns 覿面不識 to Li Linzong, a named lay interlocutor.','Complete Compendium of the Five Lamps (五燈全書): Li Linzong tells Yunfeng Yuanyi that he does not recognize him face to face.',('Yunfeng Yuanyi',('respondent','section-subject')),status='identified-non-master')
named(o[1],'Huangbo Xiyun','Old Recorded Sayings of Venerable Masters (古尊宿語錄): Huangbo Xiyun says that at that point he still did not understand Mazu.',('Baizhang Huaihai',('interlocutor','case-figure')))
for i,title in [(2,'Mirror of the Lineage Dharma Grove'),(4,'Record Pointing at the Moon'),(5,'Compendium of the Five Lamps'),(6,'Linked Lamps Essential Record'),(7,'Linked Collection of Chan Verses')]:
 anon(o[i],'lay carrier','the unnamed pig-carrier','respondent','其人云 or its compressed equivalent assigns the headword-bearing answer to the unnamed person carrying the pig; Shakyamuni replies afterward.',f'{title}: an unnamed pig-carrier tells Shakyamuni that although he has all-knowledge, he does not recognize the pig.',('Shakyamuni',('questioner','case-figure')))
named(o[3],"Bao'en Huiming",'Strict Lineage of the Five Lamps (五燈嚴統): Bao’en Huiming answers that he does not recognize the incense platform.')
save(p,d,old,['read all 8 complete units','removed Chinese section headers from MasterName','recovered Li Linzong, Huangbo Xiyun, Bao’en Huiming, and repeated pig-carrier turns'])

# 如何是無寒暑處 — the phrase is the monk's second question in every witness.
p,d,old=load(IDS[4]); o=d['Senses'][0]['Occurrences']
for i,title in enumerate(['Mirror of the Lineage Dharma Grove','Essentials of the Chan Lineage','Blue Cliff Record','Compendium of Verse Comments']):
 anon(o[i],'monastic questioner','the unnamed monk','questioner','僧問/曰/云 introduces the question 如何是無寒暑處; Dongshan Liangjie\'s answer begins at 山曰 or 師云.',f'{title}: an unnamed monk asks Dongshan Liangjie where the place without heat or cold is.',('Dongshan Liangjie',('respondent','case-figure')))
save(p,d,old,['read all 4 complete cases','confirmed exact actor is the unnamed monk in every witness'])

# 普賢 — direct owners, authored prose, two compiled case narrations, and one unresolved anthology address.
p,d,old=load(IDS[5]); o=d['Senses'][0]['Occurrences']
named(o[0],'Jiangshan Faquan','Complete Compendium of the Five Lamps (五燈全書): Jiangshan Faquan directly contrasts Manjushri laughing and Samantabhadra frowning.')
named(o[1],'Yongming Yanshou','Record of the Mirror of the Teaching (宗鏡錄): Yongming Yanshou writes of entering Samantabhadra’s dharma-realm body.')
named(o[3],'Baizhang Daoheng','Strict Lineage of the Five Lamps (五燈嚴統): Baizhang Daoheng says that karmic delusion and dusty toil are Samantabhadra’s sphere.')
for i,title in [(4,'Compendium of the Five Lamps'),(7,'Record Pointing at the Moon')]:
 anon(o[i],'compiled case narration','the case narrator','compiler','世尊因普眼菩薩欲見普賢 is third-person case narration; no character utters the headword in this clause.',f'{title}: the compiler narrates Puyan Bodhisattva seeking Samantabhadra before Shakyamuni responds.',('Shakyamuni',('case-figure','respondent')))
anon(o[5],'recorded master','the unnamed imperial-birthday speaker','utterer','The complete unit is a direct imperial-birthday hall address, but the anthology excerpt and headings supplied here do not recover the speaker’s personal name.','Recorded Essentials of the Patriarchs’ Addresses (列祖提綱錄): an unidentified recorded master says that Manjushri and Samantabhadra both proclaim the true transformation.')
named(o[6],'Nanyang Huizhong','Patriarchal Hall Collection (祖堂集): Nanyang Huizhong says that this is the sphere of the great persons Samantabhadra and Manjushri.')
save(p,d,old,['read all 8 complete units','corrected Baizhang Daoheng and recovered Jiangshan Faquan, Yongming Yanshou, Nanyang Huizhong','separated compiled case narration'])

# 著語 — most tokens are narrator stage labels; only Xiangtian speaks the headword.
p,d,old=load(IDS[6]); o=d['Senses'][0]['Occurrences']
for i,title,who in [(0,'Blue Cliff Record','Xuedou Chongxian'),(1,'Dahui Zonggao Record','Yuanwu Keqin'),(2,'Book of Serenity','Yuanwu Keqin'),(3,'Tianyin Yuanxiu Record','Tianyin Yuanxiu'),(4,'Langting Jingting Record','Langting Jingting'),(6,'Hongzhi Zhengjue Record','Hongzhi Zhengjue')]:
 anon(o[i],'editorial narration','the record narrator','compiler','著語 appears in a narrator’s attribution or stage direction; the named master utters the following comment, not the word 著語 itself.',f'{title}: the narrator labels or requests an attached comment associated with {who}.',(who,('person-described','commentator')))
named(o[5],'Xiangtian Jinian','Recorded Sayings of Xiangtian Jinian (象田即念禪師語錄): Xiangtian Jinian says that he will place an attached comment on each case.')
save(p,d,old,['read all 7 complete units','corrected six editorial/stage-label tokens previously treated as master speech'])

# 知客 — office uses plus a distinct compositional “know the guest” saying.
p,d,old=load(IDS[7]); o=d['Senses'][0]['Occurrences']
for i,label,note in [(0,'the monastic-rule narrator','guest prefect presents the censer'),(1,'the unnamed preface author','the guest prefect’s rank is specified'),(2,'the monastic-rule direction','the guest prefect is directed to step out'),(4,'the record narrator','Ming is labeled guest prefect'),(5,'the record narrator','Benjing is labeled guest prefect'),(6,'the ceremony heading','the guest prefect is thanked')]:
 anon(o[i],'monastic or editorial narration',label,'compiler','The headword denotes the monastic office in narration, a rule direction, or an editorial speech label; it is not uttered by the nearby master.',f'Source record: {note}.',status='impersonal' if i in (0,2,6) else 'reviewed-unnamed')
named(o[3],'Miyun Yuanwu','Recorded Sayings of Miyun Yuanwu (密雲禪師語錄): Miyun Yuanwu contrasts the rare person who knows the guest before the matter is raised with people who chase a clod.')
# Make the lexical split explicit: office versus the verb-object phrase in Miyun's saying.
base=d['Senses'][0]; office=[x for j,x in enumerate(base['Occurrences']) if j!=3]; special=deepcopy(base); special['PreferredTarget']='to recognize the guest before the matter is raised'; special['Occurrences']=[o[3]]; special['Explanation']='Here 知客 is not the monastery office but the compositional phrase “know the guest.” Miyun Yuanwu says that one rarely meets a person who knows the guest before anything is raised, contrasting that person with the many hounds that chase a clod.'; special['ClaimAnchors']=[]
base['Occurrences']=office; base['Explanation']='The guest prefect is the monastery officer who receives and manages guests. Monastic rules direct the guest prefect to present the censer or step out; records also identify particular office-holders as “Ming, the guest prefect” and “Benjing, the guest prefect.” This office sense is distinct from Miyun Yuanwu’s phrase 未舉先知客, where 知 is the verb “know” and 客 its object.'
d['Senses']=[base,special]
save(p,d,old,['read all 7 complete units','split monastery office from Miyun Yuanwu’s compositional “know the guest” phrase'])

# 不會 — patriarch, masters, a king, an emperor, and exact quoted speakers.
p,d,old=load(IDS[8]); o=d['Senses'][0]['Occurrences']
for i in (0,3,5): named(o[i],'Gayashata',f'Source lineage record: the boy later named Gayashata says, “I do not understand principle; I am exactly one hundred years old.”',('Sanghanandi',('interlocutor','case-figure')))
named(o[1],'Baizhang Huaihai','Old Recorded Sayings of Venerable Masters (古尊宿語錄): Baizhang Huaihai says that upon reaching this point he does not understand.',('Mazu Daoyi',('later-quoter','case-figure')))
named(o[2],'Longya Judun','Strict Lineage of the Five Lamps (五燈嚴統): Longya Judun tells Tiantai Deshao, “Go—you do not understand my words.”',('Tiantai Deshao',('interlocutor','case-figure')))
for i in (4,6): named(o[i],'Cuiyan Kezhen','Source case commentary: Cuiyan Kezhen says that the outsider asks thus and Shakyamuni answers thus, yet does not understand that one penetration.',('Shakyamuni',('person-discussed','case-figure')))
anon(o[7],'royal interlocutor','King Ashoka','respondent','云不會 is King Ashoka’s answer in the Pindola exchange.','Linked Lamps Essential Record (聯燈會要): King Ashoka answers Pindola that he does not understand.',status='identified-non-master')
named(o[8],'Dahui Zonggao','Essentials of the Chan Lineage (禪宗正脉): Dahui Zonggao says he has told the listener plainly and the listener still does not understand.')
anon(o[9],'imperial verse author','the Kangxi Emperor','verse-author','The headword occurs in an imperial verse asking whether the puppet understands.','Imperially Selected Recorded Sayings (御選語錄): the Kangxi Emperor asks in verse whether the puppet understands.',status='identified-non-master')
save(p,d,old,['read all 10 complete units','recovered Gayashata, Baizhang, Longya, Cuiyan, King Ashoka, Dahui, and imperial verse authorship'])

# 青青翠竹 — two direct expositors, two question/answer turns, an anthology speaker, and a senior-seat quotation.
p,d,old=load(IDS[9]); o=d['Senses'][0]['Occurrences']
anon(o[2],'monastic questioner','the unnamed monk','questioner','僧問 introduces the headword-bearing proposition; Qinghai’s answer begins at 師曰.','Jingde Record of the Transmission of the Lamp (景德傳燈錄): an unnamed monk asks Qinghai about “verdant bamboo is entirely true thusness.”',('Qinghai',('respondent','section-subject')))
named(o[3],'Ciji Cong','Compendium of the Five Lamps (五燈會元): Ciji Cong answers the question about the color-following mani pearl with “verdant green bamboo, luxuriant yellow flowers.”')
anon(o[4],'recorded master','the unnamed formal-discourse speaker','utterer','有底道 occurs inside a direct formal discourse, but the anthology unit and available headings do not recover the speaker’s personal name.','Recorded Essentials of the Patriarchs’ Addresses (列祖提綱錄): an unidentified recorded master quotes the claim that green bamboo is true thusness and rejects treating such formulations as settled theory.')
anon(o[5],'named-by-office senior monastic','Long, the senior seat (隆首座)','interlocutor','隆首座謂師曰 explicitly assigns the headword-bearing statement to Long, identified by office but not by a rostered personal name.','Complete Compendium of the Five Lamps (五燈全書): Long, the senior seat, tells Tianqi Benrui that verdant bamboo is entirely true thusness.',('Tianqi Benrui',('interlocutor','section-subject')),status='identified-non-master')
save(p,d,old,['read all 6 complete units','corrected Fayan to Ciji Cong','separated questioner, anthology speaker, and Long senior-seat quotation'])

out=Path(__file__).with_name('cohorts-1-3-106-115-full-read-repair-ledger.json'); out.write_text(json.dumps({'generatedUtc':STAMP,'packetUnitsRead':73,'entries':LEDGER},ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'entries':len(LEDGER),'packetUnitsRead':73,'ledger':str(out),'hashes':[(x['SourceTerm'],x['oldSha256'],x['newSha256']) for x in LEDGER]},ensure_ascii=False,indent=2))
