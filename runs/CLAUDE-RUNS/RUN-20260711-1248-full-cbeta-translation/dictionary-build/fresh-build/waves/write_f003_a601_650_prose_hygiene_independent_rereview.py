#!/usr/bin/env python3
import datetime, hashlib, json
from pathlib import Path

R=Path(__file__).resolve().parents[2]
formal=R/'fresh-build/waves/f003-laneA-601-650-prose-hygiene-formal-gate.json'
ledger=R/'fresh-build/waves/f003-laneA-601-650-prose-hygiene-repair-ledger.json'
prior=R/'fresh-build/waves/f003-laneA-601-650-postrepair-independent-exact-rereview.json'
F=json.loads(formal.read_text()); L=json.loads(ledger.read_text()); P=json.loads(prior.read_text())
assert F['hardPass'] and len(F['entries'])==len(L['entries'])==len(P['rows'])==50
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()

specific={
601:'The new prose names Sanyi Yu but no stored occurrence has that actor; it also attributes a moving-boat/fixed-notch proof to J37nB386 without making that quote reader-visible.',
602:'The new prose names Yongjue Yuanxian, but only one stored turn supports the Xi Shi comparison and the entry still does not distinguish literal imitation from ridicule of borrowed performance across the other cases.',
603:'The new prose assigns the operative claim to Feiyin Tongrong while the stored named actor is 費隱禪師 and the support-versus-crossing inference is not anchored to an exact reader-visible clause.',
604:'The two Tiantong Jue quotations are still reviewed-unnamed despite naming 天童覺 in the KWIC; the explanation instead selects Xutang Zhiyu and adds “conceptual measurement” without an exact anchored predicate.',
605:'The explanation says Yuansou Xingduan “appears as what ties up a donkey,” a grammatical and semantic actor/object collapse; it does not distinguish the stake, a phrase called a stake, and the listener’s tethering by exact source.',
606:'The diseased-eye definition is plausible, but the six stored occurrences do not include the promised diseased-eye self-definition under Yongming Yanshou; the prose cites an unshown J28nB208 control.',
607:'A temple-title occurrence is falsely assigned to Yuezhou Junshan Xiansheng, and the huge 古尊宿語錄 KWIC is an accidental 水月 substring inside 水月寺 rather than reflection evidence; actor and sense hygiene therefore fail.',
608:'The explanation names Baizhang Huaihai and X66n1297, but the selected occurrences and source-specific predicates do not reader-visibly establish that attribution or the claimed “responses that trail behind.”',
609:'The explanation turns Furong Zixian into the grammatical subject of a snowflake and claims no residue, while the stored Furong line contrasts gratitude and ingratitude; the asserted inference is not anchored.',
610:'“Huangdi’s demanded memorial” is a factual error: the case is Nanyang Huizhong’s request to the emperor/patron. Recensional duplicates and later free deployments remain unsynthesized.',
611:'The prose makes Changqing Yingyuan the subject of “is played or heard”; the selected lines distribute playing/hearing across several actors and narrated verses, so the exact source claim is malformed and under-anchored.',
612:'The thirst-and-pursuit definition is not supported by the selected Yongming KWIC (“如陽焰如幻”); the actual thirst/pursuit clause is a different narrated X64 occurrence and must be named and anchored.',
613:'The explanation says Baichi Xingyuan is sought under a dragon’s jaw, but Baichi’s stored line has the pearl rolling up waves. The dragon-jaw and direct-attainment claims are not attached to their actual clauses.',
614:'Dahui’s stored clause supports rarity, but the explanation generalizes meetings/arrivals without identifying which of the other occurrences supplies those uses or separating a personal/title-like use.',
615:'Konggu Daocheng supports failed plucking, but visible-form and obtaining claims are spread across other actors; the article repeats Konggu’s name and does not anchor the broader synthesis.',
616:'The entry still names only Chenghui Xianxu for the positive bloom while anonymously invoking “others” for warnings against deadness; the opposed Zen deployments require explicit actor/source anchors.',
617:'The prose makes Liao’an Qingyu both responder and object “appearing in questions”; possession, request, responsiveness, and decorative-jewel counteruses are not assigned to exact actors.',
618:'The prose makes Meixi Du the stringless lute that “is nevertheless played or heard.” Actor, instrument, and predicate are conflated, and the hearing-versus-mechanism inference lacks exact anchors.',
619:'The prose makes Guizong Zhichang the thing quoted to show inclusion and does not distinguish one work split across volumes/recensions from independent evidence; work-level support remains unclear.',
620:'The balance image is useful, but the article assigns all weighing, pointer, and judgment predicates to Zishou Yuancheng despite named and unnamed questioners supplying different turns.',
621:'The travel-expense meaning is clear, but Huangbo Xiyun is made the grammatical subject of “occur together”; each journey/payment inference and narrated versus spoken case remains unanchored.',
622:'The insult is defined, but all eating, answering, and functioning predicates are assigned generically to Cishou Huaishen without showing which exact clause supplies each or testing ordinary literal rice-bag uses.',
623:'The residue control is useful, but Tianzhen Weize is made to “throw” all corpus insults; the entry does not name the other utterers or anchor the stale/exhausted-performance inference.',
624:'The article assigns praise, blame, open taking, and disappeared footing wholesale to Quan’an Qiji. The selected cases use the thief differently and need source-by-source synthesis and a literal boundary.',
625:'Yibian is made to name and test every daily-work use. The article does not distinguish quoted Pang family material, ordinary labor, and later encounter deployment by exact actor/source.',
626:'Dongming Huiqian is made to place and measure every balance weight, while questions and appraisals have different utterers. “Real heft” remains an inference without a named exact anchor.',
627:'Yuanwu Keqin is made into the burr (“is given as something to bite”); object and speaker are conflated, and bite/chew/penetrate predicates are not mapped to exact stored cases.',
628:'The mat’s ordinary scene is responsible, but Zhihai Benyi is assigned all bodily, wear, carrying, and hall predicates. The article still lacks source-specific deployment and catalogue/title filtering.',
629:'Shangu Chonghui is made to cite, address, and judge all Sudhana uses; figure, utterer, pilgrimage narration, and direct address are not separated or individually anchored.',
630:'The public-presentation claim is assigned to Zihu Lizong alone without showing the assembly clause, and the awakening/qualification inference risks importing the broader story rather than defining the figure by stored Zen deployment.',
631:'Kuang’an Shiyuan is made the source of all fare and house-custom contrasts. The explanation’s “named sources just cited” points to no reader-visible citations and does not separate ordinary household from school-specific use.',
632:'Mandatory karma gate fails: the six stored karma occurrences omit the claimed past-cause→present-effect, 少室六門 “佛是無業人，無因果,” 遮詮 self-glosses, fixed-karma/hell, and fox-funeral controls. The explanation cites them without reader-visible occurrence anchors, and three thin non-karma senses have only 1/2/1 witnesses.',
633:'The 255/257 claim is prose-only: neither the counted-work ledger nor scope is reader-visible in the entry. The decisive fox-control occurrence is present, but its speaker is 朝宗禪師, not Mian Xianjie, and the article does not synthesize all seven ordinary self-binding scenes.',
634:'The six occurrences support condemnation, but the prose names Gu Xue Zhe, Gu Ting, and Juelang Daosheng while their stored clauses are all attributed merely to compilers. The mandatory apophatic 遮詮 controls are cited but absent as reader-visible occurrences.',
635:'Literal bodily burning is retained, but Xueguan Zhiyin is assigned all biographical and ceremonial clauses. Finger versus crown burning, actor, vow object, and documentary narration remain unsplit and unanchored.',
636:'The article leaves the only evidentiary actor as “the documentary narrator in Fu Dashi’s record” and provides no named master deployment or exact vow/offer recipient; its broad enacted-vow conclusion exceeds the selected clauses.',
637:'Yunfeng Wenyue is made the bodily mark that “occurs”; actor and object are conflated, and the vow/community setting is asserted without an exact reader-visible clause naming the undertaking.',
638:'Shishuang Lin is made to “receive the incense burn,” but the stored uses need separation between self-burning, ceremony, and named recipient; the bodily-vow conclusion is not individually anchored.',
639:'Konggu Daocheng is assigned all lifting, burning, recipient, and lineage-declaration predicates. Distinct ceremony speakers and the explicit recipient after each 拈香 must be named per case.',
640:'Yunmen Lingkan is assigned both ordinary and formal incense uses wholesale. Burning, dedication, honor, gratitude, and commitment are distinct claims that need exact actor/source anchors and counterexamples.',
641:'Dongming Huiqian is made to name every act, setting, and recipient. The entry does not distinguish documentary ceremony wording from a master’s utterance or map beneficiary claims to exact quotes.',
642:'Linji Yixuan is made the incense stick that “matters” in every offering/time use; actor/object grammar fails, and counting, offering, and time-marking require separate exact anchors.',
643:'The opening improperly narrows one stick of incense to Linji Yixuan, although the term is corpus-wide. Recipient, allegiance, gratitude, blessing, and transmission claims are not tied to each exact dedication.',
644:'Yongming Yanshou is assigned all vow makers and objects. The entry does not identify who vows what in each stored clause or distinguish a stated vow from documentary narration and generic formulae.',
645:'Huineng is used as the sole source for rescue, service, transmission, and future-action vows. Those distinct objects need exact occurrences and speakers, and the explanation’s “headword-bearing scene” remains unshown.',
646:'Xuefeng Genxin is made to “mark” every swearing act; the article does not identify the oath content, addressee, documentary frames, or literal truth/performance boundary case by case.',
647:'The entry retains an unnamed documentary narrator as its only prose attribution and says “named sources just cited” when none are reader-visible; standing obligation versus wish is not anchored.',
648:'The case-label scope is sensible, but all citation, compression, and criticism are assigned to Gulin. The exact Huike/Bodhidharma elements, later speakers, and historical-versus-deployment boundary remain unanchored.',
649:'The wall/unmoving inference is assigned to Wuyi Yuanlai while the article refers vaguely to “named sources just cited.” The going-is-unmoving control and wall simile need exact actor/source anchors, not an unshown synthesis.',
650:'Muzhou Daoming is made to ask all questions and invoke the figure, but questioners and respondents differ. The imported-figure boundary, exact “unmoving” predicates, and direct-address/title contamination remain unaudited.'}

rows=[]
for ordinal,(fr,lr,pr) in enumerate(zip(F['entries'],L['entries'],P['rows']),601):
    ep=Path(fr['path']); d=ep.parent
    assert sha(ep)==lr['entrySha256']
    e=json.loads(ep.read_text()); occ=sum(len(s.get('Occurrences',[])) for s in e['Senses'])
    rows.append({'ordinal':ordinal,'id':fr['id'],'term':e['SourceTerm'],'entrySha256':sha(ep),
                 'worksheetSha256':sha(d/'evidence.draft.json'),'repairLedgerEntrySha256':lr['entrySha256'],
                 'verdict':'REVISE','occurrencesRead':occ,'priorFinding':pr['finding'],
                 'finding':specific[ordinal]})
assert sum(r['occurrencesRead'] for r in rows)==277
base={'schemaVersion':1,'reviewType':'independent exact-hash post-prose-hygiene rereview',
      'wave':'f003','lane':'A','ordinals':'601-650','generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),
      'reviewer':'Codex independent reviewer (not A601-650 author/repairer)','readOnly':True,
      'entriesEdited':False,'promotion':False,'merge':False,'siteTouched':False,
      'formalGate':str(formal.relative_to(R)),'formalGateSha256':sha(formal),'formalGateHardPass':True,
      'repairLedger':str(ledger.relative_to(R)),'repairLedgerSha256':sha(ledger),
      'priorReview':str(prior.relative_to(R)),'priorReviewSha256':sha(prior),
      'currentHashesVerified':True,'occurrencesRead':277,
      'reviewMethod':'Read all 50 compiled entries, all 277 stored occurrences and full-case actor/source records; retested ordinary scene, Zen bend, title/catalogue/substring contamination, exact utterer prose, and the mandatory karma brief controls including reader-visible evidence and the 255/257 scope.',
      'summary':{'KEEP':0,'REVISE':50}}
for start in (601,611,621,631,641):
    p=dict(base); sub=[r for r in rows if start<=r['ordinal']<=start+9]
    p.update({'checkpointRange':f'{start}-{start+9}','rows':sub,'occurrencesRead':sum(x['occurrencesRead'] for x in sub)})
    out=R/f'fresh-build/waves/f003-laneA-{start}-{start+9}-prose-hygiene-independent-rereview.json'
    out.write_text(json.dumps(p,ensure_ascii=False,indent=2)+'\n')
out=R/'fresh-build/waves/f003-laneA-601-650-prose-hygiene-independent-rereview.json'
p=dict(base);p['rows']=rows;out.write_text(json.dumps(p,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'report':str(out.relative_to(R)),'sha256':sha(out),'formalGateSha256':sha(formal),'repairLedgerSha256':sha(ledger),'summary':p['summary'],'occurrencesRead':277},indent=2))
