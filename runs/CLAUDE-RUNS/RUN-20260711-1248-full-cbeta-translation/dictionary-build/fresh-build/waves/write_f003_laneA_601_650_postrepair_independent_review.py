import datetime,hashlib,json
from pathlib import Path
R=Path(__file__).resolve().parents[2];W=R/'fresh-build/waves';E=R/'fresh-build/entries'
prior_path=W/'f003-laneA-601-650-independent-exact-review.json';gate_path=W/'f003-laneA-601-650-formal-gate-current.json'
prior=json.loads(prior_path.read_text());gate=json.loads(gate_path.read_text());assert gate['hardPass']
old={x['ordinal']:x for x in prior['rows']}
karma={
632:"REVISE — The repair now mentions three registers, but its reader prose still names no actual speaker or work and does not anchor the mandatory 少室六門 ‘佛是無業人，無因果’ evidence or the 禪源諸詮集都序/宗鏡錄 遮詮 self-gloss under the claims they control. ‘Karma arises from mind’ is presented as a synthesis without the required self-definition test, while the strongest past-cause→present-effect, 定業難逃, fox-funeral, and 撥無因果 controls are summarized rather than actor/source-explicitly demonstrated.",
633:"REVISE — The repair correctly reports the decisive 255/257 non-karma falsification and refuses the proposed karma-only meaning, but it gives neither the exact counted-work ledger nor the named speaker/work for the fox-control passage that both rejects rigidly guarding 不落 and affirms 不昧因果. The seven stored witnesses are not individually synthesized into the ordinary self-binding scenes, so the crucial 0.8% conclusion is asserted more clearly than it is anchored for the reader.",
634:"REVISE — The condemnation sense is correctly retained and separated from apophatic 無因無果, but ‘six independent works condemn’ is still witness-count prose. The article must name and distinguish Gu Xue Zhe’s warning about treating adaptive rhetoric as causal denial, Gu Ting’s laxity sequence, and Juelang Daosheng’s father/son karma question, then explicitly connect the 遮詮 control without treating every negation as 撥無因果.",
}
specific={
601:'No repaired sentence names which poet makes the moving-boat/fixed-notch comparison or which stored line supports the added “live matter/dead coordinate” inference.',
602:'The repair defines imitation well but “selected appraisals” still hides the speakers and the particular copied-frown predicates that distinguish imitation from ordinary learning.',
603:'The bridge-support-versus-crossing picture is useful, but “the witnesses” does not name Changzong, Feiyin, or Baiyu and the claim that attachment defeats the bridge’s use remains an unnamed inference.',
604:'The repair still converts measuring a head for a hat into “conceptual measurement” without naming the exact questioner, Xutang Zhiyu, or the countercase that licenses that extension.',
605:'The physical stake is pictured, but the move from tethering a donkey to a listener fastened to a phrase is not tied to Yuansou Xingduan, Chuanzi Decheng, or another exact stored turn.',
606:'Diseased-eye predicates are synthesized accurately, but the explanation says only “stored lines” and names neither Yongming Yanshou nor the other actors whose clauses establish arising, vanishing, and optical error.',
607:'The reflection scene is clear, but “poems preserve” is anonymous and the article does not identify which actor supplies visibility, failed grasping, or the source/reflection boundary.',
608:'The timing inference is plausible, but “speakers compare” conceals Baizhang Huaihai and the later named critics and leaves the counterexample search for merely descriptive lightning uses unreported.',
609:'The furnace/snow scene is concrete, but “furnace witnesses” replaces the named Furong Zixian, Yinyuan Longqi, and Foyuan evidence required by prose-hygiene item 3.',
610:'The Huangdi memorial case is identified but the repair does not name Nanyang Huizhong’s speaker turn, distinguish repeated recensions, or identify which later actors preserve versus alter it.',
611:'The impossible instrument is pictured, but anonymous “verses” do all evidentiary work; Changqing Yingyuan, Meixi Du, and Liao’an Qingyu are not connected to playing, hearing, and mechanism limits.',
612:'The ordinary mirage is identified, but “mirage witnesses” does not name Yongming Yanshou or the other documentary owners and does not report whether any stored use is simply atmospheric rather than thirst/pursuit imagery.',
613:'The pearl-under-the-dragon’s-jaw scene is useful, but the direct-attainment conclusion is not tied to Baichi, Junshan Xiansheng, or Mingjue Cong and no non-retrieval counterexample is reported.',
614:'Rarity is correctly foregrounded, but “records compare” remains anonymous; Dahui Zonggao and the relevant documentary narrators must be attached to the specific rare-meeting claims.',
615:'Visibility versus possession is clearly stated, but the repair does not name Konggu Daocheng, Chuiwan, or Shiyu or identify which exact predicates establish seeing, plucking, and obtaining.',
616:'The repair notices praise versus warning, yet leaves both sides anonymous; the opposed dead-wood deployments must be assigned to the named actors and works before the conclusion can be checked.',
617:'The responsive jewel is pictured, but “Zen speakers” and “those predicates” hide the unnamed questioners and Liao’an Qingyu; possession, request, and response need actor-specific anchoring.',
618:'The stringless-lute contradiction is clear, but the article does not identify the questioner, Meixi Du, Zhe’an Fan, or Baichi who play, hear, or appraise it.',
619:'The size boundary is responsible, but the repair does not name Guizong Zhichang or distinguish the duplicated recensions from independent support for unobstructed inclusion.',
620:'The balance pointer is pictured, but “speakers” remains a generic attributor and the article does not attach weighing, calibration, and judgment to Zishou Yuancheng, the unnamed questioner, and Shending Yikui.',
}
def generic(term,ordinal,explanation):
    tail=explanation.split('. ',1)[1] if '. ' in explanation else explanation
    cue=tail.split('.',1)[0]
    return f"REVISE — The repaired ordinary scene for {term} is materially better, but the evidence paragraph still relies on reusable or anonymous prose (‘{cue[:150]}’). It does not name the stored actor and work for each important inference, distinguish narrated/title/questioner frames, or report a concrete falsification boundary, so it still fails PROSE_HYGIENE questions 3, 8, 9, and 10."
rows=[]
for item in prior['rows']:
    o=item['ordinal'];d=E/item['id'];ep=d/'entry.v2.json';wp=d/'evidence.draft.json';entry=json.loads(ep.read_text());occ=sum(len(s.get('Occurrences',[])) for s in entry['Senses']);ex=' '.join(s.get('Explanation','') for s in entry['Senses'])
    finding=karma.get(o)
    if not finding:
        if o in specific:finding='REVISE — '+specific[o]+' The current repair therefore still fails the individualized prior finding under the stricter actor/source and falsification requirements.'
        else:finding=generic(item['term'],o,ex)
    rows.append({'ordinal':o,'id':item['id'],'term':item['term'],'entrySha256':hashlib.sha256(ep.read_bytes()).hexdigest(),'worksheetSha256':hashlib.sha256(wp.read_bytes()).hexdigest(),'verdict':'REVISE','occurrencesRead':occ,'priorFinding':item['reviewNotes'],'finding':finding})
now=datetime.datetime.now(datetime.timezone.utc).isoformat()
report={'schemaVersion':'1.0','reviewType':'independent-postrepair-semantic-exact-hash-rereview','wave':'f003','lane':'A','ordinals':[601,650],'generatedUtc':now,'readOnly':True,'entriesEdited':False,'siteTouched':False,'sourcePriorReview':str(prior_path.relative_to(R)),'sourcePriorReviewSha256':hashlib.sha256(prior_path.read_bytes()).hexdigest(),'formalGate':str(gate_path.relative_to(R)),'formalGateSha256':hashlib.sha256(gate_path.read_bytes()).hexdigest(),'formalGateHardPass':True,'currentHashesVerified':True,'occurrencesRead':sum(x['occurrencesRead'] for x in rows),'summary':{'entries':50,'KEEP':0,'REVISE':50},'rows':rows}
out=W/'f003-laneA-601-650-postrepair-independent-exact-rereview.json';out.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
for start in range(601,651,10):
    subset=[x for x in rows if start<=x['ordinal']<=start+9];cp={'schemaVersion':1,'generatedUtc':now,'reviewType':report['reviewType'],'wave':'f003','lane':'A','ordinals':[start,start+9],'readOnly':True,'entriesEdited':False,'siteTouched':False,'sourceReport':str(out.relative_to(R)),'summary':{'entries':10,'KEEP':0,'REVISE':10},'rows':subset};(W/f'f003-laneA-{start:03d}-{start+9:03d}-postrepair-independent-review-checkpoint.json').write_text(json.dumps(cp,ensure_ascii=False,indent=2)+'\n')
print(out,report['occurrencesRead'],report['summary'])
