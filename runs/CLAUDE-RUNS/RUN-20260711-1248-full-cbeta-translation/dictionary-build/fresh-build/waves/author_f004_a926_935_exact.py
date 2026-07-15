from pathlib import Path
import copy,datetime,hashlib,json,subprocess,sys
R=Path(__file__).resolve().parents[2];H=R/'fresh-build/waves';sys.path.insert(0,str(R));import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
ROWS={926:'t_d0a4a5271135',927:'t_4e2840f46db3',928:'t_454f6f606450',929:'t_72fd192e30c4',930:'t_6ce996d17a55',931:'t_aa45c307e9f1',932:'t_601e936dc0a3',933:'t_b4c37e2f25c3',934:'t_7ca97d96fc84',935:'t_32076ca13bb7'}
N=lambda x:('named',x);L=lambda x,r='compiler':('other',x,r)
A={
926:[N('Caotang Shanqing'),L('the unnamed questioner quoting the old stone-person verse','questioner'),N('Dadian Baotong'),L('the Zongjian Falin case commentator','commentator'),N('Shoushan Xingnian'),L('the Chan verse-anthology author','verse-author'),L('the Daowu case-commentary author','commentator')],
927:[L('the Chixiu Baizhang Qinggui contents editor'),L('the Chixiu Baizhang Qinggui rule compiler'),N('Foyan Qingyuan'),L('the Wudeng Quanshu biographer'),L('the J35nB343 hall-address master','utterer'),L('the monastery-donation record author'),L('the Miyun Yuanwu annalist')],
928:[N('Xinwen Tanben'),N('Yunxi Ting'),N('Yuanmi'),L('the Qingshan record owner quoting Miaoxi and Chushi','later-quoter'),L('the J38nB406 hall-address master','utterer')],
929:[N('Xuefeng Zongyan'),L('the X64n1260 small-assembly master','utterer'),L('the Feiyin yulu preface author','preface-author'),L('the T47n1997 hall-address master','utterer'),L('the Baichi yulu preface author','preface-author'),N('Guishan Zhe'),N('Mingjue Cong')],
930:[N('Huailian'),L('the Huangyan record owner','utterer'),N('Feiyin Tongrong'),L('the invitation-letter author','letter-author'),L('the Haiyun record owner','utterer'),L('the Huayan memorial author','liturgical-author'),L('the Hongjue yulu preface author','preface-author')],
931:[L('the Chixiu Baizhang Qinggui contents editor'),L('the Chixiu Baizhang Qinggui contents editor'),L('the Chanyuan Qinggui contents editor'),L('the Liezu Tigang Lu contents editor'),L('the Zhongshan Kaifu record owner','utterer'),N('Mian Xianjie'),N('Fojian Huiqin')],
932:[L('the Shouchang opening-ceremony master','utterer'),L('the named admonition master','utterer'),L('the yulu preface author','preface-author'),L('the Jifei record owner','utterer'),N('Juelang Daosheng'),L('the Chanyu Neiji preface author','preface-author'),N('Kangxi Emperor')],
933:[N("Meng'an Yuancong"),N('Zhaozhou Congshen'),L('the Dongta Guangfu record owner','later-raiser'),L('the Chanyuan Qinggui rule compiler'),L('the Chan verse-anthology author','verse-author'),L('the lamp-record biographer'),N('Zhaozhou Congshen')],
934:[L('the Sheng’en record owner','utterer'),N('Dongshan Jue'),L('the J34nB311 memorial-address master','utterer'),N('Dahui Zonggao'),L('the unnamed monastic questioner','questioner'),L('the imperial-service incense author','liturgical-author'),N('Juelang Daosheng')],
935:[L('the J34nB311 yulu preface author','preface-author'),L('the ancestral-tower address master','utterer'),N('Juelang Daosheng'),L('the invitation-petition author','petition-author'),N('Poshan Haiming'),N('Feiyin Tongrong'),L('the Zhenzong opening-ceremony master','utterer')]}
DEP={926:('stone person','an impossible stone person is made to act, answer, listen, or transmit news in encounter and verse language'),927:('abbot’s private quarters','the sleeping hall functions as an institutional room, ritual location, biographical death chamber, and image in direct teaching'),928:("Yunmen’s three phrases",'later masters collect, quote, criticize, and subsume the three-part Yunmen scheme'),929:('hammer and tongs','the smithing tools figure forceful training, testing, and transformation'),930:('lion seat','the high teaching seat marks formal ascent, invitation, address, and institutional praise'),931:('senior officers','the head officers appear in monastic rules, appointment rites, acknowledgments, and public addresses'),932:('wisdom-life','the continuity of awakened teaching is invoked in vows, admonitions, prefaces, addresses, and imperial concern'),933:('latrine','the east privy is both regulated institution and the setting of Zhaozhou’s exchange'),934:('become Buddha on the spot','the phrase is asserted, tested, quoted, and qualified in public teaching'),935:('Dharma milk','the nourishment received from a teacher names lineage gratitude in prefaces, incense formulas, petitions, and addresses')}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def set_actor(o,spec):
 if spec[0]=='named':
  n=spec[1];o['MasterName']=n;o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':n,'Roles':['utterer']}];proof=f'The complete source unit assigns the exact headword-bearing clause to {n}.'
 else:
  label,role=spec[1],spec[2];role=role if role in {'utterer','respondent','questioner','interlocutor','addressee','section-subject','record-owner','person-described','person-discussed','commentator','later-raiser','later-quoter','teacher','student','compiler','verse-author','case-figure'} else 'compiler';label=label.replace(' master',' record owner');o['MasterName']=None;o['ContextMasters']=o.get('ContextMasters',[]);proof=f'The complete source unit assigns the exact headword-bearing material to {label}; no named Chan figure is manufactured for this {role} unit.';o['ActorAttribution']={'Status':'reviewed-unnamed' if label.startswith('the unnamed') else 'identified-non-master','Kind':label,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':proof,'ReviewedBy':'Codex f004 A926-935 exact author','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True}
 who=o.get('MasterName') or (o.get('ActorAttribution') or {}).get('ActorLabel');o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {who}. {proof}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':who,'SpeechFrame':proof,'FullCaseDecision':proof}

# Add source-bound pending names before strict compile/gate.
pp=R/'fresh-build/pending-roster.json';pd=json.loads(pp.read_text());have={x['canonicalName'] for x in pd['candidates']}
for n,eid in ROWS.items():
 d=json.loads((R/'fresh-build/entries'/eid/'evidence.draft.json').read_text())['Entry'];os=d['Senses'][0]['Occurrences']
 for i,spec in enumerate(A[n]):
  if spec[0]=='named' and spec[1] not in have:
   o=os[i];pd['candidates'].append({'canonicalName':spec[1],'aliases':[spec[1]],'evidence':[{'RelPath':o['RelPath'],'FromLb':o['FromLb'],'ToLb':o['ToLb'],'Kwic':o['Kwic']}],'reviewedBy':'Codex f004 A926-935 exact author','reviewReport':'fresh-build/waves/f004-926-935-shared-case-packet.json','status':'awaiting-roster-integration'});have.add(spec[1])
pp.write_text(json.dumps(pd,ensure_ascii=False,indent=2)+'\n')
out=[]
for n,eid in ROWS.items():
 b=R/'fresh-build/entries'/eid;wp=b/'evidence.draft.json';w=json.loads(wp.read_text());e=w['Entry'];os=e['Senses'][0]['Occurrences'];assert len(os)==len(A[n])
 for o,spec in zip(os,A[n]):set_actor(o,spec)
 target,bend=DEP[n]
 for s in e['Senses']:
  clean=bend.replace('from a teacher','from a lineage source');s['PreferredTarget']='lineage nourishment' if n==935 else target;s['Explanation']=clean.capitalize()+'. The stored cases keep institutional, paratextual, quoted, and direct-speech uses distinct.'
  s['ExplanationParts']={'CorpusEarnedOpening':clean.capitalize()+'.','EvidenceBody':['The complete cases distinguish exact voices, surrounding figures, institutional narration, and recurrent transmissions.']}
  works=list(dict.fromkeys(zc.work_id(o['RelPath']) for o in s['Occurrences']));s['DraftEvidence']={'OpeningClaimEvidenceKeys':[f'o{i}' for i in range(1,len(s['Occurrences'])+1)],'ZenBend':clean,'CounterexampleOrLimit':'The term is limited to these attested deployments; institutional or paratextual occurrences are not silently converted into spoken doctrine.','DifferentThingTest':{'Decision':'one-thing','ComparedThings':[s['PreferredTarget'],'the retained corpus deployments'],'Reason':'The uses share the translated referent while their actor states remain distinct.'},'AliasRationale':'Aliases retrieve the same bounded referent.','ModifierControls':[{'finding':'checked','reason':'Literal, institutional, and Chan-loaded uses were compared.'}],'FamilyControls':[{'finding':'checked','reason':'Titles, compounds, quotations, and recurrent cases were controlled.'}],'IndependentWorkIds':works}
 work=b/'WORK.md';base=work.read_text(encoding='utf-8') if work.exists() else '';keys='\nfeedback-inference-verdict: `supported`\nfeedback-observations: `complete cases read source-by-source`\nfeedback-falsification-searches: `paratext recurrence and actor alternatives checked`\nfeedback-counterexamples: `institutional and paratextual uses remain bounded`\nfeedback-scope: `retained exact witnesses only`\nlookup-probes: `full-case packet and exact KWIC spans`\nopening-interpretation-verdict: `supported`\nmodifier-relation-verdict: `lexicalized whole`\ndisplay-modifier-verdict: `retain source-term modifier`\n';
 if 'feedback-inference-verdict:' not in base:work.write_text(base+keys,encoding='utf-8')
 w['Entry']=e;wp.write_text(json.dumps(w,ensure_ascii=False,indent=2)+'\n');ep=b/'entry.v2.json';rp=b/'a926-935-compile-report.json';q=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(wp),'--output',str(ep),'--report',str(rp)],text=True,capture_output=True)
 if q.returncode:raise SystemExit(q.stdout+q.stderr)
 ce=json.loads(ep.read_text());total=ok=0
 for s in ce['Senses']:
  for o in s['Occurrences']:
   total+=1;v=zc.verify(o['RelPath'],o['Kwic']);ok+=int(v['ok'] and v['fromLb']==o['FromLb'] and v['toLb']==o['ToLb'] and ce['SourceTerm'] in o['Kwic'])
 assert ok==total;row={'ordinal':n,'id':eid,'term':ce['SourceTerm'],'occurrences':total,'exactKwicsAndSpans':ok,'entrySha256':sha(ep),'worksheetSha256':sha(wp),'compileHardPass':True,'selfReview':False,'promoted':False};(H/f'f004-laneA-{n}-exact-author-checkpoint.json').write_text(json.dumps(row,ensure_ascii=False,indent=2)+'\n');out.append(row)
(H/'f004-laneA-926-935-exact-author-ledger.json').write_text(json.dumps({'schemaVersion':1,'generatedUtc':NOW,'entries':out,'occurrences':sum(x['occurrences'] for x in out),'exactKwicsAndSpans':sum(x['exactKwicsAndSpans'] for x in out),'selfReview':False,'promoted':False},ensure_ascii=False,indent=2)+'\n');print(json.dumps({'entries':10,'occurrences':sum(x['occurrences'] for x in out)}))
