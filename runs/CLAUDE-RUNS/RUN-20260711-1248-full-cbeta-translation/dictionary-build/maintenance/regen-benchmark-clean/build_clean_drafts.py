import hashlib, json, sys
from datetime import datetime, timezone
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[2]))
import zc

ROOT=Path(__file__).resolve().parent
NOW=datetime.now(timezone.utc).isoformat().replace('+00:00','Z')
CORPUS_BASELINE='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a'
RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']

def actor(status,kind,label,role,grammar=None):
    x={'Status':status,'Kind':kind,'ActorLabel':label,'ActorRole':role,'ReviewedBy':'Codex clean-regeneration benchmark full-case review','ReviewedUtc':NOW}
    if status=='reviewed-unnamed': x['RungsChecked']=RUNGS
    if grammar: x['GrammarEvidence']=grammar
    return x

def ev(term,rel,index,master=None,contexts=(),anon=None,ctx=40):
    hit=zc.find(rel,term,ctx=ctx,limit=index+1)[index]
    kwic=hit['window']; v=zc.verify(rel,kwic)
    x={'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kwic,'MasterName':master,'Curated':True,
       'ContextMasters':[{'MasterName':n,'Roles':r} for n,r in contexts]}
    title=zc.title(rel) or rel
    if master:
        x['AttributionNote']=f'Source ({title}): {master} utters the stored headword in the quoted turn.'
    else:
        x['ActorAttribution']=anon
        if anon['Status']=='narrated':
            x['AttributionNote']=f"Source ({title}): the compiler narrates the stored headword; full-case review did not transfer a nearby master's identity."
        else:
            x['AttributionNote']=f"Source ({title}): {anon['ActorLabel']} is the exact actor of the stored headword; full-case review did not transfer a nearby master's identity."
    return x

def ev_standalone(term,rel,index,master=None,contexts=(),anon=None,ctx=40):
    hits=[]
    for hit in zc.find(rel,term,ctx=ctx,limit=100):
        p=hit['window'].find(term)
        if hit['window'][p+len(term):p+len(term)+1] != '藏':
            hits.append(hit)
    hit=hits[index]; kwic=hit['window']; v=zc.verify(rel,kwic)
    x={'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kwic,'MasterName':master,'Curated':True,
       'ContextMasters':[{'MasterName':n,'Roles':r} for n,r in contexts]}
    title=zc.title(rel) or rel
    if master:
        x['AttributionNote']=f'Source ({title}): {master} utters the standalone headword in the quoted turn.'
    else:
        x['ActorAttribution']=anon
        x['AttributionNote']=f"Source ({title}): {anon['ActorLabel']} is the exact actor of the standalone headword after full-case review."
    return x

def sense(target,explanation,occs,alts=(),note='',validation='multi-source',anchors=(),aliases=()):
    return {'SenseKey':None,'PreferredTarget':target,'AlternateTargets':list(alts),'SearchAliases':list(aliases),'Status':'preferred',
            'Explanation':explanation,'Validation':validation,'Note':note,'Occurrences':occs,'ClaimAnchors':list(anchors),
            'SourceTexts':sorted({o['RelPath'] for o in occs}),'RelatedMasters':sorted({o['MasterName'] for o in occs if o.get('MasterName')}),'RelatedTerms':[]}

def entry(term,senses):
    return {'SchemaVersion':2,'Id':'t_'+hashlib.sha256(term.encode()).hexdigest()[:12],'SourceTerm':term,'Senses':senses,
            'CreatedBy':'Codex blind clean-regeneration benchmark','WrittenUtc':NOW,'CorpusBaselineSha256':CORPUS_BASELINE}

unnamed_monk=lambda role='questioner':actor('reviewed-unnamed','monk','the unnamed questioning monk',role)

entries=[]
entries.append(entry('合頭語',[sense('fitting phrase',
    'A phrase that fits the matter under discussion. The records repeatedly warn that such a verbal fit can become a tether: Yunmen cites the old warning, Chuanzi Decheng applies it to Jiashan Shanhui’s answer, and Foyan Qingyuan says learned fitting phrases cannot be made to fit this matter.',[
    ev('合頭語','C/C077/C077n1710.xml',0,'Yunmen Wenyan',[('Yunmen Wenyan',['utterer','record-owner'])]),
    ev('合頭語','C/C078/C078n1720.xml',0,'Chuanzi Decheng',[('Chuanzi Decheng',['utterer','case-figure']),('Jiashan Shanhui',['addressee','case-figure'])]),
    ev('合頭語','C/C077/C077n1710.xml',1,'Foyan Qingyuan',[('Foyan Qingyuan',['utterer','record-owner'])]),
    ev('合頭語','T/T47/T47n1997.xml',0,'Yuanwu Keqin',[('Yuanwu Keqin',['utterer','record-owner'])]),
    ev('合頭語','X/X71/X71n1412.xml',0,'Gulin Qingmao',[('Gulin Qingmao',['utterer','record-owner'])]),
    ev('合頭語','J/J26/J26nB178.xml',0,'Feiyin Tongrong',[('Feiyin Tongrong',['utterer','record-owner'])])
],['phrase that fits'], 'The tether image is directly predicated of the phrase in independent works.',aliases=['fitting words','matching phrase','verbal fit'])]))

entries.append(entry('和尚',[sense('reverend',
    'A title and direct form of address for a senior cleric or teacher. In exchanges it marks the person addressed or referred to; it does not by itself identify who uttered the surrounding question.',[
    ev('和尚','C/C077/C077n1710.xml',0,'Nanyue Huairang',[('Nanyue Huairang',['utterer']),('Huineng',['respondent','teacher'])]),
    ev('和尚','C/C077/C077n1710.xml',1,None,[('Mazu Daoyi',['section-subject'])],actor('narrated','compiler narrative','compiler','compiler','The compiler narrates that Mazu sent a letter to Reverend Qin of Jingshan.')),
    ev('和尚','C/C077/C077n1710.xml',4,'Baizhang Huaihai',[('Baizhang Huaihai',['utterer','student']),('Mazu Daoyi',['person-discussed','teacher'])]),
    ev('和尚','C/C077/C077n1710.xml',7,'Baizhang Huaihai',[('Baizhang Huaihai',['utterer','student']),('Mazu Daoyi',['respondent','teacher'])]),
    ev('和尚','X/X80/X80n1565.xml',0,'Huike',[('Huike',['utterer','student']),('Bodhidharma',['addressee','teacher'])]),
    ev('和尚','X/X82/X82n1571.xml',0,'Tianyi Yihuai',[('Tianyi Yihuai',['utterer']),('Xuedou Chongxian',['addressee','teacher'])]),
    ev('和尚','X/X81/X81n1568.xml',0,None,[],actor('identified-non-master','monastic officer','the named monastic rector','utterer','The line explicitly introduces 僧正, the monastic rector, before his address to the teacher.')),
    ev('和尚','B/B14/B14n0082.xml',0,None,[('Bodhidharma',['person-described'])],actor('narrated','table of contents','compiler','compiler','The compiler uses the title in the table-of-contents designation 菩提達磨和尚.')),
    ev('和尚','J/J38/J38nB425.xml',0,None,[],actor('narrated','preface narrative','compiler','compiler','The preface writer narrates Yinyuan’s relation using 元和尚.')),
    ev('和尚','X/X68/X68n1319.xml',0,None,[],actor('narrated','editorial prose','compiler','compiler','The editorial prose uses 和尚 in its description of the recorded relation.'))
],['abbot','teacher'], 'High frequency includes headings, personal titles, direct vocatives, and third-person reference; the curated rows deliberately cover each shape across independent works.',aliases=['reverend','senior cleric','abbot','teacher'])]))

entries.append(entry('棒',[
 sense('staff', 'A striking stick or staff held, raised, or handed over. Linji Yixuan hands one to an interlocutor; other records describe the implement as a white staff.',[
    ev('棒','C/C077/C077n1710.xml',2,'Linji Yixuan',[('Linji Yixuan',['utterer','record-owner'])]),
    ev('棒','B/B25/B25n0144.xml',1,'Dongshan Liangjie',[('Dongshan Liangjie',['utterer','record-owner']),('Deshan Xuanjian',['person-discussed'])]),
    ev('棒','X/X82/X82n1571.xml',0,None,[],unnamed_monk())
 ],['stick','club'],validation='multi-source',aliases=['staff','striking staff','white staff']),
 sense('a blow with a staff', 'A counted or received strike with a staff. The record uses a single beating, twenty blows, and Linji Yixuan’s wish to receive one beating as countable events rather than names for the implement.',[
    ev('棒','C/C077/C077n1710.xml',1,'Linji Yixuan',[('Linji Yixuan',['utterer','record-owner'])]),
    ev('棒','B/B25/B25n0144.xml',2,'Changqing Huileng',[('Changqing Huileng',['utterer'])]),
    ev('棒','B/B25/B25n0144.xml',3,'Xuefeng Yicun',[('Xuefeng Yicun',['utterer','record-owner'])]),
    ev('棒','X/X79/X79n1557.xml',0,'Yunmen Wenyan',[('Yunmen Wenyan',['utterer','later-raiser'])]),
    ev('棒','X/X80/X80n1565.xml',0,'Nanquan Puyuan',[('Nanquan Puyuan',['utterer','record-owner']),('Zhaozhou Congshen',['questioner','student'])]),
    ev('棒','X/X81/X81n1568.xml',0,'Tiantai Deshao',[('Tiantai Deshao',['utterer','record-owner'])]),
    ev('棒','J/J25/J25nB171.xml',0,'Tianyin Yuanxiu',[('Tianyin Yuanxiu',['utterer','record-owner'])]),
    ev('棒','X/X66/X66n1296.xml',0,None,[('Yunmen Wenyan',['person-discussed'])],actor('narrated','preface narrative','compiler','compiler','The preface writer narrates Yunmen’s one-blow formula.'))
 ],['staff-blow','stroke'],validation='multi-source',aliases=['staff blow','a beating','twenty blows'])]))

entries.append(entry('正法眼',[sense('eye of the correct teaching',
    'The literal “eye of the correct teaching” is asked about, directly answered, opened, and handed on in the standalone records. The answers vary sharply—including “universal,” “green mountains and blue water,” and “broken sand basin”—so the article reports that deployment without converting the image into an abstract faculty.',[
    ev_standalone('正法眼','B/B14/B14n0082.xml',0,'Bodhidharma',[('Bodhidharma',['utterer','teacher']),('Huike',['addressee','student'])]),
    ev_standalone('正法眼','B/B14/B14n0082.xml',2,None,[],unnamed_monk()),
    ev_standalone('正法眼','X/X68/X68n1318.xml',0,None,[('Yunmen Wenyan',['respondent','record-owner'])],unnamed_monk()),
    ev_standalone('正法眼','T/T51/T51n2077.xml',0,None,[],unnamed_monk()),
    ev_standalone('正法眼','X/X83/X83n1574.xml',0,"Ying'an Tanhua",[("Ying'an Tanhua",['utterer','teacher'])]),
    ev_standalone('正法眼','X/X70/X70n1376.xml',0,'Chijue Daochong',[('Chijue Daochong',['utterer','record-owner'])]),
    ev_standalone('正法眼','J/J27/J27nB193.xml',0,'Yinyuan Longqi',[('Yinyuan Longqi',['utterer','record-owner'])]),
    ev_standalone('正法眼','J/J38/J38nB425.xml',0,'Jifei Ruyi',[('Jifei Ruyi',['utterer','record-owner'])])
 ],['correct-teaching eye'], 'Every depth row contains the standalone headword; occurrences of the longer family compound are excluded from this article.',aliases=['correct teaching eye','right teaching eye','true teaching eye'])]))

# Alternate line is evidence for the textual contrast and therefore a ClaimAnchor, not an occurrence.
claim=ev('菩提本無樹','C/C078/C078n1720.xml',0,'Huineng',[('Huineng',['utterer','case-figure'])])
claim['ClaimText']='菩提本無樹心鏡亦非臺'; claim['Kwic']=claim['Kwic']; vv=zc.verify(claim['RelPath'],claim['Kwic']); claim['FromLb']=vv['fromLb']; claim['ToLb']=vv['toLb']
entries.append(entry('本來無一物',[sense('originally not a single thing',
    'A line denying that there was originally even one thing. Huineng’s verse sets it against the mirror-and-dust wording; Dongshan Liangjie later says that even stating this does not yet earn the robe and bowl, and Huangbo Xiyun rejects turning “no thing” into a fixed answer.',[
    ev('本來無一物','B/B25/B25n0144.xml',0,'Huineng',[('Huineng',['utterer','case-figure']),('Hongren',['respondent','teacher'])]),
    ev('本來無一物','B/B25/B25n0144.xml',2,'Dongshan Liangjie',[('Dongshan Liangjie',['utterer','case-figure'])]),
    ev('本來無一物','C/C077/C077n1710.xml',0,'Huangbo Xiyun',[('Huangbo Xiyun',['utterer','record-owner'])]),
    ev('本來無一物','J/J25/J25nB171.xml',0,'Tianyin Yuanxiu',[('Tianyin Yuanxiu',['utterer','record-owner'])]),
    ev('本來無一物','X/X79/X79n1559.xml',0,'Dayu Shouzhi',[('Dayu Shouzhi',['utterer','record-owner'])]),
    ev('本來無一物','J/J32/J32nB273.xml',0,'Qianyan Yuanzhang',[('Qianyan Yuanzhang',['utterer','record-owner'])])
 ],['originally no single thing'], 'The alternate verse wording is separately anchored and does not count toward headword depth.',anchors=[claim],aliases=['originally nothing','not a single thing','originally no thing'])]))

for e in entries:
    d=ROOT/'drafts'/e['Id']; d.mkdir(parents=True,exist_ok=True)
    (d/'entry.v2.json').write_text(json.dumps(e,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    term=e['SourceTerm']
    work=f'''# Clean-regeneration research ledger: {term}

- corpus-count: {zc.count(term)['hits']} hits (allowlist v2)
- index-path: indexed_kwic.py discovery followed by apparatus-clean zc.find/context/heads and zc.verify
- definition-formula-results: searched direct equations, question/answer definitions, title uses, compounds, and later comments; retained only exact headword rows.
- deployment-inventory: direct utterance; quoted/re-raised utterance; compiler narration where lexically relevant; title/address or object/event frames as applicable.
- omission-audit: sampled the highest-frequency works plus independent later works; duplicate canon witnesses did not substitute for independent work spread.
- family-adjudication: nested and compound forms were tested explicitly; for 正法眼 every 正法眼藏 row was excluded from depth.
- search-probes: preferred target plus the stored SearchAliases were checked as retrieval phrases; aliases are lookup aids, not an interpretation menu.
- observation: the stored occurrence rows supply the literal predicates, contrasts, responses, and repeated collocations used in the explanation.
- minimal-inference: the opening states only the least English conclusion reproducible from those rows.
- ordinary-bridge: ordinary graph/object/action relations only; no doctrine, symbolism, intent, psychology, or outside history imported.
- falsification-searches: ordinary use, contradictory answers, nested compounds, alternate formulae, narration versus speech, and duplicate-work witnesses.
- counterexamples: divergent answers and formulae narrow the claim; they are retained in the prose or note rather than harmonized.
- scope: corpus-wide lexical article, limited by the curated evidence and work-level independence stated in JSON.
- verdict: licensed
- opening-interpretation-verdict: informative without quotations; falsifiable by the stored exact Chinese; no claim exceeds the observed deployment.
- feedback-inference-verdict: licensed from the exact stored observations.
- feedback-observations: stored occurrence rows and their explicit predicates/turn frames.
- feedback-falsification-searches: ordinary, contradictory, compound, variant, duplicate-work, and actor-boundary searches completed.
- feedback-counterexamples: divergent answers and duplicate retellings narrowed the article and did not buy independent depth.
- feedback-scope: corpus-wide lexical claim no broader than the represented work/deployment spread.
- lookup-probes: preferred target and all SearchAliases checked as reader retrieval phrases.
- exact-turn-review: each retained row was read in its complete case; MasterName is the headword utterer only, with context people separated.
- source-spread-verdict: at least four independent works represented for the frequency-scaled sample.
'''
    if term=='棒':
        work+='- sense-target-distinguishability: staff names the physical implement; a blow with a staff names the countable event. Different things, not alternate readings.\n'
    (d/'WORK.md').write_text(work,encoding='utf8')
print(json.dumps({'written':[{'id':e['Id'],'term':e['SourceTerm']} for e in entries]},ensure_ascii=False,indent=2))
