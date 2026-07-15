#!/usr/bin/env python3
"""Normalize A601-650 structural person links without touching source titles."""
import json, pathlib

B = pathlib.Path(__file__).resolve().parents[2]
REV = B/'fresh-build/waves/f003-laneA-601-650-corrective-fresh-independent-exact-review.json'
IDS = [x['id'] for x in json.loads(REV.read_text(encoding='utf-8'))['rows']]

M = {
'百癡禪師':'Baichi Yuanshuo','入就瑞白禪師':'Ruibai Mingxue','了菴清欲禪師':"Lia'an Qingyu",'費隱禪師':'Feiyin Tongrong',
'天目中峰':'Zhongfeng Mingben','蔗菴範禪師':'Zhean Jingfan','雪關禪師':'Xueguan Zhiyin','大慧普覺禪師':'Dahui Zonggao',
'破山禪師':'Poshan Haiming','三宜盂禪師':'Sanyi Mingyu','虛堂和尚':'Xutang Zhiyu','神鼎一揆禪師':'Shending Yikui',
'古雪哲禪師':'Guxue Zhe','廬山天然禪師':'Tianran Hanshi','佛冤禪師':'Foyuan','南陽慧忠國師':'Nanyang Huizhong',
'石雨禪師':'Shiyu Mingfang','東山梅溪度禪師':'Meixi Fudu','明覺聰禪師':'Mingjue Cong','空谷道澄禪師':'Konggu Daocheng',
'天隱和尚':'Tianyin Yuanxiu','幻有傳禪師':'Huanyou Zhengchuan','長沙東明慧遷禪師':'Dongming Huiqian','朝宗禪師':'Chaozong Tongren',
'普濟玉琳國師':'Yulin Tongxiu','雲溪俍亭挺禪師':'Langting Jingting','永覺元賢禪師':'Yongjue Yuanxian','普菴印肅禪師':'Puan Yinsu',
'天台山德韶國師':'Tiantai Deshao','吹萬禪師':'Chuiwan Guangzhen','山暉禪師':'Shanhui','Pin Jixiang':'Pin Jixiang',
'遠菴僼禪師':"Yuan'an Feng",'湛然圓澄禪師':'Zhanran Yuancheng','安吉州上方日益禪師':'Shangfang Riyi',
'江州東林興龍寺常總照覺禪師':'Changzong','百愚禪師':'Baiyu Si','杭州徑山元叟行端禪師':'Yuansou Xingduan',
'秀州華亭船子德誠禪師':'Chuanzi Decheng','南康軍雲居真如院元祐禪師':'Yunju Yuanyou','宏智禪師':'Hongzhi Zhengjue',
'皷山智嚴了覺禪師':'Gushan Zhiyan Liaojue','洪州百丈懷海大智禪師馬祖一嗣':'Baizhang Huaihai','洪州百丈懷海禪師凡十六':'Baizhang Huaihai',
'宜興芙蓉自閒覺禪師':'Furong Zijian','隱元禪師':'Yinyuan Longqi','岳州平江長慶應圓禪師':'Changqing Yingyuan',
'岳州君山顯昇禪師':'Junshan Xiansheng','廬州澄慧咸詡禪師':'Chenghui Xianxu','攖寧靜禪師':'Yingning Jing',
'子湖山第一代神力禪師':'Zihu Lizong','壽州資壽院圓澄巖禪師':'Zishou Yuancheng','東京慧林常悟禪師':'Huilin Changwu',
'東京慧林懷深慈受禪師':'Cishou Huaishen','雲門匡真禪師':'Yunmen Wenyan','鎮江府焦山或菴師體禪師':'Huoan Shiti',
'襄州洞山守初宗慧禪師':'Dongshan Shouchu','杭州海門天真惟則禪師':'Tianzhen Weize','寧波府育王月江正印禪師':'Yuejiang Zhengyin',
'慶元府東山全菴齊己禪師':'Quanan Qiji','吉州躭源山應真禪師六二南陽忠嗣':'Danyuan Yingzhen','青州普照寺一辨禪師':'Puzhao Yibian',
'蓮峰禪師':'Lianfeng','天岸昇禪師':"Tian'an Sheng",'襄州谷隱山蘊聰慈照禪師':'Guyin Yuncong','寧波府東山全菴齊己禪師':'Quanan Qiji',
'安吉州道場正堂明辯禪師':'Zhengfang Mingbian','圓悟佛果禪師':'Yuanwu Keqin','瑞州黃檗一菴月禪師':"Huangbo Yi'an Yue",
'東京智海本逸正覺禪師':'Zhihai Benyi','天隱修禪師':'Tianyin Yuanxiu','舒州山谷三祖冲會圓智禪師':'Sanzu Chonghui',
'洪州百丈道恒禪師':'Baizhang Daoheng','常德府梁山廓庵師遠禪師':'Kuoan Shiyuan','不會禪師':'Buhui','石溪心月禪師':'Shixi Xinyue',
'鎮州臨濟義玄禪師':'Linji Yixuan','無異元來禪師':'Wuyi Yuanlai','慶元府天童密菴咸傑禪師':"Mi'an Xianjie",'靈隱禮':'Lingyin Li',
'古庭禪師':'Guting Shanjian','善慧大士':'Fu Dashi','Jifei Ruyi':'Jifei Ruyi','Xueguan Zhiyin':'Xueguan Zhiyin',
'Huayan Shengke':'Huayan Shengke','伏獅祇園禪師':'Fushi Qiyuan','石門山慈照禪師蘊聦':'Shimen Yuncong',
'越州雲門靈侃禪師':'Yunmen Lingkan','首山禪師大鑑下八世風穴':'Shoushan Shengnian','頻吉祥禪師':'Pin Jixiang',
'明州奉化縣布袋和尚':'Budai','Chijue Daochong':'Chijue Daochong','盛京奉天般若古林禪師':'Bore Gulin','博山無異大師':'Wuyi Yuanlai',
'河北智隍禪師六祖能嗣':'Zhihuang','即非禪師':'Jifei Ruyi','鎮州臨濟義玄禪師凡四十八':'Linji Yixuan',
'天台豐干禪師':'Fenggan','紫竹林顓愚衡和尚':'Zhuanyu Guanheng','衡州南嶽懷讓禪師大鑒能嗣':'Nanyue Huairang',
'潭州龍山隱山禪師馬祖一嗣':'Yinshan','天界覺浪盛禪師':'Juelang Daosheng','江西馬祖道一禪師南嶽讓嗣':'Mazu Daoyi',
'越州天衣義懷禪師':'Tianyi Yihuai','常州宜興龍池一源永寧禪師':'Yiyuan Yongning','Daxiu Zhu':'Daxiu Zhu',
'𭔃香城順禪師':'Xiangcheng Shun','昭覺竹峰續禪師':'Zhufeng Zhenxu','雲居佛印元禪師':'Foyin Liaoyuan',
'雪竇石奇禪師':'Shiqi Tongyun','法璽印禪師':'Faxi Yin','隆興黃龍如曉禪師':'Huanglong Ruxiao',
'高峰龍泉院因師集賢':'Longquan Congyue','白雲端和尚語嗣楊岐':'Baiyun Shouduan','福州雪峰亘信彌禪師':'Xuefeng Hengxin',
'嵩山野竹禪師':'Yezhu Fusheng','廣福山勝覺寺密印禪師':'Miyin',
'寶誌禪師':'Baozhi','臨濟慧照禪師餘錄':'Linji Yixuan',
}

# These strings are bibliographic/group headings, not linkable people.
DROP={'懷讓禪師第二世','頌上玉泉和尚','青原山行思禪師第六世之三','二十七祖般若多羅尊者不如蜜多嗣',
      '雲菴真淨文禪師語嗣黃龍','二十三祖鶴勒那尊者勒那梵語，鶴即華言，以常感羣鶴戀慕故名耳',
      '二十三祖鶴勒那尊者勒那梵語。鶴即華言。以常感羣鶴戀慕故名耳','三祖鑑智禪師信心銘'}

def rewrite(x):
    if isinstance(x,dict):
        for k in list(x):
            v=x[k]
            if k=='MasterName' and isinstance(v,str) and v in M: x[k]=M[v]
            else: rewrite(v)
        if isinstance(x.get('ContextMasters'),list):
            x['ContextMasters']=[cm for cm in x['ContextMasters'] if cm.get('MasterName') not in DROP]
    elif isinstance(x,list):
        for y in x: rewrite(y)

for i in IDS:
    for fn in ('evidence.draft.json','entry.v2.json'):
        p=B/'fresh-build/entries'/i/fn
        d=json.loads(p.read_text(encoding='utf-8')); rewrite(d)
        p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

# Passage-level corrections for four headings previously mistaken for utterers.
def actor(o,label,role='questioner'):
    o.pop('MasterName',None)
    o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':'monk','ActorLabel':label,'ActorRole':role,
      'RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],
      'ReviewedBy':'Codex f003 A601-650 round3 repair author','ReviewedUtc':'2026-07-15T09:05:00Z',
      'GrammarEvidence':'The complete case assigns the exact headword clause to this unnamed turn, not to the surrounding lineage or section heading.'}

for i,idx,kind in [('t_c86b1e91c7b5',1,'lishan'),('t_502eeb8c9b1e',5,'questioner'),
                   ('t_e1eda88159c6',1,'questioner'),('t_179e443ac255',4,'lingyin')]:
    for fn in ('evidence.draft.json','entry.v2.json'):
        p=B/'fresh-build/entries'/i/fn; d=json.loads(p.read_text(encoding='utf-8'))
        o=d['Entry']['Senses'][0]['Occurrences'][idx] if fn.startswith('evidence') else d['Senses'][0]['Occurrences'][idx]
        if kind=='lishan':
            o['MasterName']='Lishan'; o['ContextMasters']=[{'MasterName':'Lishan','Roles':['utterer','section-subject']}]
            o['AttributionNote']='Jingde Record of the Transmission of the Lamp (景德傳燈錄), Lishan section: Lishan answers the monk with “empty blossoms, a shimmering mirage.”'
        elif kind=='lingyin':
            o['MasterName']='Lingyin Li'; o['ContextMasters']=[{'MasterName':'Lingyin Li','Roles':['utterer']},{'MasterName':'Bodhidharma','Roles':['person-discussed']}]
            o['AttributionNote']='Zongmen niangu huiji (宗門拈古彙集): Lingyin Li utters the headword-bearing appraisal of Bodhidharma.'
        else:
            actor(o,'unnamed monk questioning '+('Gule Yuanyan' if i=='t_e1eda88159c6' else 'the record master'))
            o['ContextMasters']=([{'MasterName':'Gule Yuanyan','Roles':['respondent','section-subject']}] if i=='t_e1eda88159c6' else [])
            o['AttributionNote']=('Jingde Record of the Transmission of the Lamp (景德傳燈錄), Gule Yuanyan section: an unnamed monk utters the headword in his follow-up.' if i=='t_e1eda88159c6' else 'Ancient Worthies’ Recorded Sayings (古尊宿語錄): an unnamed monk utters the headword in a question; the preceding lineage heading is not a speaker.')
        senses=d['Entry']['Senses'] if fn.startswith('evidence') else d['Senses']
        for s in senses:
            if isinstance(s.get('RelatedMasters'),list): s['RelatedMasters']=[M.get(x,x) for x in s['RelatedMasters'] if x not in DROP]
        p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

def named_nonmaster(o,label,role,grammar):
    o.pop('MasterName',None)
    o['ActorAttribution']={'Status':'identified-non-master','Kind':role,'ActorLabel':label,'ActorRole':role,
      'RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],
      'ReviewedBy':'Codex f003 A601-650 round3 repair author','ReviewedUtc':'2026-07-15T09:05:00Z','GrammarEvidence':grammar}

def semantic_repairs(d):
    root=d.get('Entry',d); i=root['Id']; s=root['Senses'][0]; os=s.get('Occurrences',[])
    if i=='t_60251334dc81':
        for j in (0,1,3):
            named_nonmaster(os[j],'Jiangzhou prefect Li Bo','questioner','The complete case explicitly introduces 江州刺史李渤/李㴾 as the questioner and carries his turn through the headword clause.')
            os[j]['ContextMasters']=[{'MasterName':'Guizong Zhichang','Roles':['respondent','section-subject']}]
            os[j]['AttributionNote']='The recorded Guizong case explicitly names Jiangzhou prefect Li Bo as utterer of the mustard-seed question; Guizong Zhichang answers him.'
        os[2]['ContextMasters']=[]
        s['Explanation']="‘Mount Sumeru enters a mustard seed’ places an enormous mountain inside a tiny seed. Jiangzhou prefect Li Bo explicitly utters the question in three recensions of his exchange with Guizong Zhichang; those witnesses are one case, not three independent deployments. A separate monastic question in the Patriarchs’ Hall Collection asks the same formula in an independently recorded exchange whose respondent is not identified by this stored cut."
    elif i=='t_3efd163c8697':
        os[0]['ContextMasters']=[{'MasterName':'Zihu Lizong','Roles':['respondent','section-subject']}]
        os[1].pop('ActorAttribution',None);os[1]['MasterName']='Juelang Daosheng';os[1]['ContextMasters']=[{'MasterName':'Juelang Daosheng','Roles':['verse-author','utterer']}]
        os[1]['AttributionNote']='Juelang Daosheng’s authored verse sequence: Juelang utters the line about the dragon girl presenting the jewel and transforming her whole body.'
        os[3].pop('ActorAttribution',None);os[3]['MasterName']='Dahui Zonggao';os[3]['ContextMasters']=[{'MasterName':'Dahui Zonggao','Roles':['utterer']}]
        os[3]['AttributionNote']='Dahui Pujue’s formal discourse (大慧普覺禪師普說): Dahui Zonggao invokes the dragon girl among scriptural figures awakened in one lifetime.'
        os[5].pop('ActorAttribution',None);os[5]['MasterName']='Langting Jingting';os[5]['ContextMasters']=[{'MasterName':'Langting Jingting','Roles':['utterer','record-owner']}]
    elif i=='t_32289452a85b':
        named_nonmaster(os[0],'the named collective petitioners speaking as “we”','utterer','The first-person plural 某等…今謹 marks the petitioners’ direct collective declaration, not compiler narration.')
        os[0]['ContextMasters']=[{'MasterName':'Fu Dashi','Roles':['addressee','person-discussed']}]
        os[2]['ContextMasters']=[]
        os[2]['ActorAttribution']['ActorLabel']='the disciplinary compiler listing a legal category';os[2]['ActorAttribution']['GrammarEvidence']='The compact legal list has no speech turn; the former title-plus-person ContextMasters value was metadata, not an actor.'
    elif i=='t_08c0c321eb2a':
        os[0].pop('MasterName',None); os[0]['ActorAttribution']={'Status':'narrated','Kind':'monastic regulation','ActorLabel':'the monastic-code compiler prescribing the attendant’s action','ActorRole':'compiler','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'ReviewedBy':'Codex f003 A601-650 round3 repair author','ReviewedUtc':'2026-07-15T09:05:00Z','GrammarEvidence':'The code narrates/prescribes the attendant lighting incense before reporting; 炷香 is the action, not words uttered by the attendant.'};os[0]['ContextMasters']=[]
    elif i=='t_d801848213ab':
        os[0]={'RelPath':'X/X68/X68n1319.xml','FromLb':'0535c14','ToLb':'0535c15','Kwic':'蔡居士作禮云：弟子每對神佛前發願，護持蔡鐸正念，禁止蔡鐸邪思。','Curated':True,'ActorAttribution':{'Status':'identified-non-master','Kind':'layman','ActorLabel':'layman Cai making the stated vow','ActorRole':'utterer','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'ReviewedBy':'Codex f003 A601-650 round3 repair author','ReviewedUtc':'2026-07-15T09:05:00Z','GrammarEvidence':'蔡居士作禮云 explicitly introduces Cai’s direct first-person vow.'},'ContextMasters':[{'MasterName':'Yulin Tongxiu','Roles':['respondent']}],'AttributionNote':'Yulin Tongxiu’s record: layman Cai directly says that he makes a vow before spirits and buddhas; Yulin answers him.'}
        os[5].pop('ActorAttribution',None);os[5]['MasterName']='Mingjue Cong';os[5]['ContextMasters']=[{'MasterName':'Mingjue Cong','Roles':['utterer','record-owner']}];os[5]['AttributionNote']='Recorded Sayings of Mingjue Cong: after answering the monk, Mingjue begins his 乃云 sermon and utters the concluding vow line.'
        s['Explanation']='To make a vow is to form a stated undertaking; each passage supplies its own speaker and object. Layman Cai states his vow and receives Yulin Tongxiu’s answer; Wuyi Yuanlai instructs hearers to vow toward the stated destination; documentary sources narrate other vows; and Mingjue Cong closes a hall sermon with a vow line. A former table-of-contents hit has been replaced by direct passage evidence.'
    elif i=='t_e89833bb5e63':
        os[2]['Kwic']='問：發菩提心最勝功德，利他自利為菩薩行，如諸比丘對佛菩薩發大誓願。為是義故，其如宰官當權住世一切易辦，至或卑微分力歉薄，作何願力而為功德？';os[2]['FromLb']='0321b10';os[2]['ToLb']='0321b13'
        named_nonmaster(os[4],'the assembled good friends reciting after Huineng','utterer','一時逐惠能道 explicitly directs the assembly to repeat the four vows after Huineng.')
        os[4]['ContextMasters']=[{'MasterName':'Huineng','Roles':['teacher','section-subject']}]
        os[5].pop('ActorAttribution',None);os[5]['MasterName']='Huineng';os[5]['ContextMasters']=[{'MasterName':'Huineng','Roles':['utterer','teacher']}];os[5]['AttributionNote']='Platform Scripture: Huineng directly instructs the assembly to make the four great vows and listen closely.'
        s['Explanation']='A solemn vow is the content of a declared undertaking; the dominant fixed form here is the four great vows. Huineng leads one assembly in reciting them and directly instructs another to make them, while other masters and compilers quote, enumerate, or discuss the formula. Different speakers retain their exact turns; the repeated four-part referent remains one sense.'
    elif i=='t_11c14d7191f1':
        os[1].pop('ActorAttribution',None);os[1]['MasterName']='Shakyamuni Buddha';os[1]['ContextMasters']=[{'MasterName':'Shakyamuni Buddha','Roles':['utterer']}];os[1]['AttributionNote']='Source Mirror Record quotes the Lotus Scripture and explicitly introduces this first-person vow with 法華經云; Shakyamuni Buddha is the quoted utterer.'
    elif i=='t_5517bf8c66c2':
        respondents=['Sanzu Chonghui','Muzhou Daoming','Yaoshan Yisu','Muzhou Daoming','Baoen Xuanze','Yaoshan Yisu']
        for o,n in zip(os,respondents): o['ContextMasters']=[{'MasterName':n,'Roles':['respondent','section-subject']}]
        os[0]['Kwic']='問：如何是不動尊？師曰：寸步千里。';os[0]['ToLb']='0011c20'
        os[3]['Kwic']='問：如何是不動尊？師云：邂逅到崖州。';os[3]['ToLb']='0486b23'
        os[4]['Kwic']='問：如何是不動尊？師曰：飛飛颺颺。';os[4]['ToLb']='0012b15'
        os[5]['Kwic']='問如何是不動尊。師曰。四王擡不起。';os[5]['ToLb']='0479a15'
        s['Explanation']='The Immovable Honored One is the figure named in a recurring public question, “What is the Immovable Honored One?” The records preserve distinct cases rather than one fivefold recension: Sanzu Chonghui answers “a thousand miles in an inch-step,” Muzhou Daoming answers “if by chance, you reach Yazhou” in parallel witnesses, Baoen Xuanze answers “fluttering and billowing,” and Yaoshan Yisu answers “the Four Kings cannot lift it.” The different answers are retained as different masters’ deployments; none is substituted for a universal definition.'
        s['Note']='A recurring question across distinct masters and works. Two Muzhou witnesses are parallel recensions; the other respondent cases are independent. The entry does not infer a biography from the title.'
    if i in {'t_60251334dc81','t_d801848213ab','t_e89833bb5e63','t_5517bf8c66c2'} and s.get('Explanation'):
        opening,sep,body=s['Explanation'].partition('. ')
        if not sep and ' The records ' in s['Explanation']:
            opening,body=s['Explanation'].split(' The records ',1); body='The records '+body; sep=' '
        opener=opening if opening.endswith(('.', '?”', '!”')) else opening+'.'
        s['ExplanationParts']={'CorpusEarnedOpening':opener,'EvidenceBody':[body] if body else []}

for i in IDS:
    for fn in ('evidence.draft.json','entry.v2.json'):
        p=B/'fresh-build/entries'/i/fn; d=json.loads(p.read_text(encoding='utf-8')); semantic_repairs(d)
        if fn=='evidence.draft.json':
            for s in d['Entry']['Senses']:
                for o in s.get('Occurrences',[]):
                    if not o.get('MasterName') and o.get('ActorAttribution') and not o.get('DraftActorProof'):
                        a=o['ActorAttribution']; o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':a.get('ActorLabel'),'SpeechFrame':a.get('GrammarEvidence'),'FullCaseDecision':a.get('GrammarEvidence')}
        p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'entries':len(IDS),'map':len(M),'droppedHeadings':sorted(DROP)},ensure_ascii=False))
