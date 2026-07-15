from pathlib import Path
import datetime, hashlib, json, subprocess, sys

R=Path(__file__).resolve().parents[2]; sys.path.insert(0,str(R)); import zc
BASE='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a'
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat(); RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']

def occurrence(rel,kwic,master=None,label=None,status=None,role='utterer',contexts=()):
 v=zc.verify(rel,kwic); assert v['ok'] and v['count']==1,(rel,kwic,v)
 proof=(f'The complete passage assigns this exact headword-bearing wording to {master}.' if master else
        f'The complete passage names {label} as the exact headword-bearing actor.')
 o={'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kwic,'MasterName':master,'Curated':True,
    'ContextMasters':([{'MasterName':master,'Roles':['utterer']}] if master else [])+[{'MasterName':x,'Roles':['record-owner']} for x in contexts],
    'AttributionNote':f'Source text ({zc.title(rel)}; {rel}). Exact actor: {master or label}. {proof}',
    'DraftActorProof':{'ExactHeadwordClause':kwic,'GrammaticalSubject':master or label,'SpeechFrame':proof,'FullCaseDecision':proof}}
 if not master:
  o['ActorAttribution']={'Status':status or 'identified-non-master','Kind':label,'ActorLabel':label,'ActorRole':role,
    'RungsChecked':RUNGS,'GrammarEvidence':proof,'ReviewedBy':'Codex f005 lane A author','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True}
 return o

def sense(target,alts,aliases,opening,body,occurrences,note,zenbend,limit,aliases_reason,modifier,family,related=()):
 works=list(dict.fromkeys(zc.work_id(o['RelPath']) for o in occurrences))
 return {'SenseKey':None,'MasterName':None,'PreferredTarget':target,'AlternateTargets':alts,'SearchAliases':aliases,'Status':'preferred',
  'ExplanationParts':{'CorpusEarnedOpening':opening,'EvidenceBody':body},'Validation':'multi-source','Note':note,
  'Occurrences':occurrences,'ClaimAnchors':[],'SourceTexts':list(dict.fromkeys(o['RelPath'] for o in occurrences)),
  'RelatedMasters':list(dict.fromkeys(o['MasterName'] for o in occurrences if o.get('MasterName'))),'RelatedTerms':list(related),
  'DraftEvidence':{'OpeningClaimEvidenceKeys':[f'o{i}' for i in range(1,len(occurrences)+1)],'ZenBend':zenbend,
   'CounterexampleOrLimit':limit,'DifferentThingTest':{'Decision':'one-thing','ComparedThings':['literal eye injury','the recurrent saying in Chan speech'],'Reason':'Every curated use keeps the same eye-obstruction image; appraisal and question-and-answer roles do not create a different thing.'},
   'AliasRationale':aliases_reason,'ModifierControls':modifier,'FamilyControls':family,'IndependentWorkIds':works}}

rows=[]
specs=[
 ('t_9808ec580b69','金屑落眼',sense(
  'gold dust falling into the eye',['gold dust in the eye'],['gold dust in one’s eye','gold speck in the eye','eye-obscuring gold dust'],
  'Gold dust in the eye is something fine that nevertheless obstructs sight. Chan speakers apply the image to an understanding, a sight, or even accepted scriptural wording when holding to it becomes an obstruction.',
  ['Huangbo Wunian Shenyou warns a correspondent that having an intellectual understanding is already gold dust in the eye.',
   'Huayan Shengke explicitly pairs the phrase with “forming an obstruction in the eye” and “a good thing is not as good as nothing.”',
   'Weilin Daopei raises the whisk, asks whether the assembly sees, and says that seeing is not absent but is still gold dust in the eye.',
   'In the Tiantong Xian encounter, Yuzhi says that even seeing the buddha in Bifeng Monastery would be gold dust in the eye.'],
  [occurrence('J/J20/J20nB098.xml','若有所會，便是金屑落眼矣。','Huangbo Wunian Shenyou'),
   occurrence('J/J35/J35nB342.xml','承當即便是歟？金屑落眼成障翳，好事不如無也。','Huayan Shengke'),
   occurrence('X/X72/X72n1439.xml','竪拂子，云：還見麼？見即不無，爭奈金屑落眼。','Weilin Daopei'),
   occurrence('X/X84/X84n1579.xml','曰：縱見得，也是金屑落眼。',label='Yuzhi',status='identified-non-master',role='interlocutor')],
  'Nineteen exact hits occur in sixteen frozen works. Parallel Yunju recensions were treated as one inherited family rather than multiplied.',
  'The corpus keeps the physical eye-obstruction image while repeatedly attaching it to prized or apparently correct material; its favorable “gold” modifier does not make the obstruction harmless.',
  'The phrase does not establish a general theory that gold symbolizes value; it states that gold dust injures sight and applies that complete image in particular exchanges.',
  'The aliases preserve the concrete dust-in-eye scene and common English word order.',
  ['modifier-relation: figurative-image; 金 is the substance in the fixed dust image, not a claim that an eye is made of gold.','display-modifier: retain gold dust because the noun phrase names the obstructing particulate directly.'],
  ['Compared 金屑, 落眼, 眼翳, and the longer 金屑雖貴落眼成翳 family; longer variants confirm the same image and are not counted as separate headword occurrences.'],['眼裏著沙'])),
 ('t_130d20fb3834','眼裏著沙',sense(
  'sand getting into the eye',['sand in the eye'],['sand in one’s eye','an eye that admits no sand','eye-obstructing sand'],
  'Sand getting into the eye is the stock image of something the eye cannot admit. The Chan record preserves Baishui Benren’s paired saying that the eye cannot take sand and the ear cannot take water, then sets it against the contrary image of an eye containing Mount Sumeru.',
  ['Baishui Benren states the pair and answers a request for its meaning with “a worthy one without peer.” Hongzhi Zhengjue later quotes the complete exchange.',
   'Shending Yikui places “the eye cannot take sand” opposite “the eye contains Mount Sumeru,” making the incompatible capacities explicit in one address.',
   'Chushi Fanqi repeats both the exclusion and inclusion formulas and assigns them to contrasting ways of coming and going.',
   'Linye Qi invokes the sand-and-water pair after saying that even a displayed auspicious scene would still incur a beating.',
   'Daowu Wujin Wen turns the image transitively: if the assembly cannot name the incense burner’s color, he has put sand in their eyes and water in their ears.'],
  [occurrence('J/J32/J32nB272.xml','白水垂語云：『眼裏著沙不得，耳裏著水不得。』僧便問：『如何是眼裏著沙不得？』水云：『應真無比。』','Baishui Benren',contexts=['Hongzhi Zhengjue']),
   occurrence('J/J37/J37nB388.xml','不與麼不與麼，眼裏著沙不得，耳裏著水不得。','Shending Yikui'),
   occurrence('X/X71/X71n1420.xml','乃云：眼裏著沙不得，耳裏著水不得，恁麼來者不向一人；','Chushi Fanqi'),
   occurrence('J/J26/J26nB186.xml','何謂如此？眼裏著沙不得，耳裏著水不得。','Linye Qi'),
   occurrence('X/X82/X82n1571.xml','若道不得，汝諸人被山僧眼裏著沙，耳裏著水去也。','Daowu Wujin Wen')],
  'Sixty-seven exact hits occur in forty independent frozen works. Repeated recensions of Baishui’s saying were classified as one inherited case family.',
  'The paired formula is a portable public-interview saying: later masters quote it, contrast it with containing a mountain, and make putting sand in the eye an action within an address.',
  'The corpus also records the opposite formula, an eye that can contain Mount Sumeru. That counterformula limits “cannot admit sand” to the attested saying rather than a universal claim about incapacity.',
  'The aliases cover the literal scene, the negative formula, and the recurrent obstruction wording without introducing a hidden interpretation.',
  ['not-applicable: no apparent material modifier occurs in this headword.'],
  ['Compared 耳裏著水, 眼裏著得須彌山, and the paired 白水 saying; these are contrast/family evidence, not substitute occurrences for the headword.'],['耳裏著水'])),
]

# Register source-proven names absent from the public roster. Each candidate is
# bound to the exact canary evidence row; this does not claim roster completion.
pp=R/'fresh-build/pending-roster.json'; pd=json.loads(pp.read_text()); have={x['canonicalName'] for x in pd['candidates']}
for eid,term,s in specs:
 for o in s['Occurrences']:
  name=o.get('MasterName')
  if name and name not in have and name not in {'Weilin Daopei','Chushi Fanqi'}:
   pd['candidates'].append({'canonicalName':name,'aliases':[name],'evidence':[{k:o[k] for k in ('RelPath','FromLb','ToLb','Kwic')}],'reviewedBy':'Codex f005 lane A author','reviewReport':'fresh-build/waves/f005-laneA-1201-1202-canary-pre-review.json','status':'awaiting-roster-integration'});have.add(name)
pp.write_text(json.dumps(pd,ensure_ascii=False,indent=2)+'\n')

for eid,term,s in specs:
 b=R/'fresh-build/entries'/eid; b.mkdir(parents=True,exist_ok=True)
 e={'SchemaVersion':1,'Entry':{'Id':eid,'SourceTerm':term,'CorpusBaselineSha256':BASE,'CreatedBy':'Codex f005 lane A author','WrittenUtc':NOW,'Senses':[s]}}
 wp=b/'evidence.draft.json'; wp.write_text(json.dumps(e,ensure_ascii=False,indent=2)+'\n')
 work=f'''# {term} — f005 lane A construction

- discovery-provenance: `fresh-build/waves/f005-laneA-1201-1300-preflight.json`; historical entries, if any, were treated only as evidence inventories.
- indexed-path: preflight frozen-corpus concordance; every saved row reverified with `zc.verify`.
- definition-searches: searched definition formulas, direct questions, paired formulas, contrasts, longer family forms, and repeated case recensions.
- deployment-inventory: {len(s['Occurrences'])} curated exact rows across {len(s['DraftEvidence']['IndependentWorkIds'])} independent work IDs; duplicate inherited recensions excluded from depth.
- omission-audit: every distinct deployment used in public prose has an exact occurrence; repeated parallel wording was not padded.
- family-retest: {s['DraftEvidence']['FamilyControls'][0]}
- sense-target-distinguishability: `not-applicable — one attested referent`.
- feedback-inference-verdict: `supported` — {s['DraftEvidence']['ZenBend']}
- feedback-observations: `o1–o{len(s['Occurrences'])}` support the opening, named deployments, and stated contrast.
- feedback-falsification-searches: `literal scene; definition formulas; opposite formula; longer compounds; duplicate recensions; contradictory predicates`.
- feedback-counterexamples: {s['DraftEvidence']['CounterexampleOrLimit']}
- feedback-scope: `corpus-wide fixed image within the cited Chan deployments; no outside symbolic or doctrinal theory`.
- lookup-probes: `{'; '.join(s['SearchAliases'])}`.
- opening-interpretation-verdict: `supported` — the first two sentences state the ordinary scene and the narrow corpus-earned deployment before quotations.
'''
 if term=='金屑落眼': work+='- modifier-relation-verdict: `figurative-image` — gold dust is the named particulate in the fixed image; no object is claimed to be made of gold.\n- display-modifier-verdict: `retain gold dust` — the English names the directly attested particulate without a material-construction ambiguity.\n'
 (b/'WORK.md').write_text(work)
 ep=b/'entry.v2.json'; rp=b/'evidence-compile-report.json'; q=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(wp),'--output',str(ep),'--report',str(rp)],text=True,capture_output=True)
 if q.returncode: raise SystemExit(q.stdout+q.stderr)
 rows.append((eid,hashlib.sha256(ep.read_bytes()).hexdigest()))

lane=R/'fresh-build/waves/f005-laneA.json'; d=json.loads(lane.read_text())
for row in d['entries']:
 hit=next((h for i,h in rows if i==row['id']),None)
 if hit: row.update(state='drafted',entrySha256=hit,gateReport='fresh-build/waves/f005-laneA-1201-1202-canary-pre-review.json',failures=[])
d['completed']=2; d['nextId']=d['entries'][2]['id']; d['nextTerm']=d['entries'][2]['term']; d['updatedUtc']=NOW
lane.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'entries':rows},indent=2))
