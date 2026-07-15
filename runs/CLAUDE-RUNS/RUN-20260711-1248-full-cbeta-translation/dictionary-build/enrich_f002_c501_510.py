#!/usr/bin/env python3
"""Hand-curated unique-deployment enrichment for f002 C501-510."""
import json, subprocess
from pathlib import Path
import zc

ROOT=Path(__file__).parent

ADDITIONS={
  't_b90a5f36ec86': [{
    'RelPath':'X/X66/X66n1297.xml','Kwic':'天堂覺舉誌公語云：弄精魂漢有什麼限？舉玄沙語云：奇怪！八十翁翁入場屋，真誠不是小兒戲。','MasterName':'Hongzhi Zhengjue',
    'AttributionNote':'Forest of Models of the Chan School (宗鑑法林) explicitly marks Tiantong Jue, Hongzhi Zhengjue, as raising Baozhi and Xuansha in turn; Hongzhi calls the first an unlimited fellow fooling with the vital spirit and sharply distinguishes the second.'}],
  't_d4df8bc75ad7': [{
    'RelPath':'J/J39/J39nB450.xml','Kwic':'設或未然，不兔三月為期，煆出銅頭鐵額；九旬作限，轉成鳳子龍兒。','MasterName':'Minshu Xiang',
    'AttributionNote':'Recorded Sayings of Master Minshu (敏樹禪師語錄), Baizhang Chan Cloister hall address. Minshu Xiang sets a three-month term for forging copper heads and iron foreheads and a ninety-day limit for turning them into phoenix chicks and dragon children.'}],
  't_48bc24c64738': [{
    'RelPath':'X/X82/X82n1571.xml','Kwic':'紹興諸暨鍾山報恩譚禪師紹興諸暨鍾山報恩譚禪師上堂：法身無像，應物現形。諸禪德作麼生說箇應物現形底道理？拈拄杖示眾曰：世尊身長丈六，這箇拄杖子亦長丈六。','MasterName':'Zhongshan Baoen Tan',
    'AttributionNote':'Complete Collection of the Five Lamps (五燈全書), Zhongshan Baoen Tan section. The duplicated section heading names Baoen Tan; he ascends the hall, states the headword, asks its principle, and answers demonstratively with his staff.'},
    {'RelPath':'X/X81/X81n1568.xml','Kwic':'示眾：參學之人，大須子細。如賓主相見，便有言論往來。或應物現形，或全體作用，或把機權喜怒，或現半身，或乘師子，或乘象王。','MasterName':'Linji Yixuan',
    'AttributionNote':'Strict Five Lamps Lineage (五燈嚴統), Linji Yixuan section. Linji addresses students and places responding to things and manifesting form among the shifting actions in a guest-host encounter.'}],
  't_8253f56255ce': [{
    'RelPath':'J/J28/J28nB219.xml','Kwic':'無一眾生不攝受，塵塵剎剎圓融互入，則禪人之心量與虛空等、與法界等，其廣大圓融豈可思議哉？','MasterName':'Zhuanyu Guanheng',
    'AttributionNote':'Recorded Sayings of Zhuanyu Guanheng (紫竹林顓愚衡和尚語錄). In his continuous address Zhuanyu says that every dust and every land mutually enters without obstruction, expanding the Chan person’s measure of mind to equal empty space and the dharma realm.'}],
  't_96473172e857': [{
    'RelPath':'T/T48/T48n2016.xml','Kwic':'以唯一真法界故。則心外無法。不可以法界更證法界。','MasterName':'Yongming Yanshou',
    'AttributionNote':'Record of the Source-Mirror (宗鏡錄). Yongming Yanshou states that because there is only the one true dharma realm, there is no dharma outside mind and the dharma realm cannot be used to certify the dharma realm.'},
    {'RelPath':'J/J38/J38nB406.xml','Kwic':'為伊方寸裏，先靠著箇心外無法，一切不可得底道理，任意舉止，隨順妄情，以為行于非道方合佛道。','MasterName':'Tianran Hanshi',
    'AttributionNote':'Recorded Sayings of Tianran of Lushan (廬山天然禪師語錄). Tianran Hanshi criticizes people who lean first on the proposition that there is no dharma outside mind and nothing can be obtained, then use it to license arbitrary conduct.'}],
  't_cb2205148690': [{
    'RelPath':'J/J34/J34nB299.xml','Kwic':'虛空碎，得箇前後際斷','MasterName':'Sanfeng Hanyue Fazang',
    'AttributionNote':'Recorded Sayings of Sanfeng Hancang (三峰藏和尚語錄). Sanfeng Hanyue Fazang describes the before-and-after boundary breaking like Mount Tai collapsing and empty space shattering, then warns that without penetrating ancestral speech one sits in a dead place.'},
    {'RelPath':'J/J34/J34nB300.xml','Kwic':'無減無增，前後際斷，則當人全體現前','MasterName':'Chaozong Tongren',
    'AttributionNote':'Recorded Sayings of Chaozong Tongren (朝宗禪師語錄), New Year hall address. Chaozong says that with neither decrease nor increase and the before-and-after boundary cut off, the person’s whole body appears throughout the worlds.'}],
  't_e95ea628d5dd': [{
    'RelPath':'X/X64/X64n1260.xml','Kwic':'師曰：入泥入水即不無先師','MasterName':'Jingci Daochong',
    'AttributionNote':'Collected Guidelines of the Patriarchs (列祖提綱錄) explicitly introduces Jingci Daochong commenting on his former teacher: Daochong grants the teacher’s entering mud and water, then counters with the cold cicada clutching dead wood.'},
    {'RelPath':'X/X66/X66n1297.xml','Kwic':'芭蕉徹云：更進一步。又云：雖是入泥入水，幾人搆得？','MasterName':'Bajiao Huiche',
    'AttributionNote':'Forest of Models of the Chan School (宗鑑法林) explicitly marks Bajiao Huiche’s two comments: “advance another step,” then “although this is entering mud and water, how many people can reach it?”'}],
  't_b33fddd5d4f1': [{
    'RelPath':'X/X66/X66n1297.xml','Kwic':'盟石息云：婆子高高峰頂立，就下應難；菴主深深海底行，搆上不易。','MasterName':'Mengshi Xi',
    'AttributionNote':'Forest of Models of the Chan School (宗鑑法林) explicitly marks Mengshi Xi commenting on the burned-hut case: the old woman stands on the highest peak, while the hut master walks on the deepest seabed.'}],
  't_6214dc704b24': [{
    'RelPath':'X/X85/X85n1593.xml',
    'Kwic':'莫是真實相為麼。莫是正恁麼時無一法可證麼。莫是認伊來處麼。莫是全體顯露麼。莫錯會好。如此見解。喚作依草附木。與佛法天地懸隔。',
    'MasterName':'Tiantai Deshao',
    'AttributionNote':"Correct Lineage of Chan (禪宗正脉), Tiantai Deshao section. After asking how the assembly construes his answers, Tiantai Deshao himself lists four proposed construals, warns 'do not understand wrongly,' and calls those views dependence on grass and attachment to trees.",
  },{
    'RelPath':'X/X78/X78n1553.xml',
    'Kwic':'師上堂，示眾云：十方薄伽梵，一路涅槃門。諸禪德！且作麼是涅槃門？莫是山僧者裏聚會少時便為涅槃門麼？莫是僧堂裏衣鉢下坐、寂默觀空便為涅槃門麼？莫錯會好。',
    'MasterName':'Lushan Huacheng Jian',
    'AttributionNote':"Tiansheng Extensive Lamp Record (天聖廣燈錄), Lushan Huacheng Jian section. The section explicitly introduces Huacheng Jian ascending the hall; in one continuous address he rejects both a brief gathering around him and silent contemplation beneath the robe and bowl as the nirvana gate, then warns the assembly not to understand wrongly.",
  }],
}

for ident, rows in ADDITIONS.items():
    path=ROOT/'fresh-build'/'entries'/ident/'evidence.draft.json'
    data=json.loads(path.read_text(encoding='utf8'))
    sense=data['Entry']['Senses'][0]
    existing={(o['RelPath'],o['Kwic']) for o in sense['Occurrences']}
    for row in rows:
        if (row['RelPath'],row['Kwic']) in existing: continue
        verdict=zc.verify(row['RelPath'],row['Kwic'])
        if not verdict['ok']: raise SystemExit(f"verification failed: {ident}: {verdict}")
        note=row['AttributionNote']
        occurrence={
          'RelPath':row['RelPath'],'FromLb':verdict['fromLb'],'ToLb':verdict['toLb'],
          'Kwic':row['Kwic'],'Curated':True,'AttributionNote':note,
          'MasterName':row['MasterName'],
          'DraftActorProof':{'ExactHeadwordClause':row['Kwic'],'SpeechFrame':note,'FullCaseDecision':note},
        }
        sense['Occurrences'].append(occurrence)
        if row['RelPath'] not in sense['SourceTexts']: sense['SourceTexts'].append(row['RelPath'])
        wid=zc.work_id(row['RelPath'])
        if wid not in sense['DraftEvidence']['IndependentWorkIds']:
            sense['DraftEvidence']['IndependentWorkIds'].append(wid)
    path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    subprocess.run(['python3',str(ROOT/'compile_evidence_draft.py'),str(path)],check=True)
