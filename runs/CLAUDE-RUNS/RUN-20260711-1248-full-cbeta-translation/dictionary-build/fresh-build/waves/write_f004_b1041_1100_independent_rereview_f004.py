from pathlib import Path
import datetime,hashlib,json
R=Path(__file__).resolve().parents[2]; H=R/'fresh-build/waves'
src=H/'f004-b1041-1100-semantic-prose-author-final-ledger.json'; d=json.loads(src.read_text())
assert hashlib.sha256(src.read_bytes()).hexdigest()=='c733b1c8576857bf6f6ae4d053d8c32b08cb7bc8c164ed619e1f6f23a5985dd5'
reasons={
1041:'o2 is an unnamed monk’s direct answer (“or gathering the community, or communal tea”), not compiler narration.',
1042:'o2 is Yuanwu’s Blue Cliff commentary, while o3/o4 preserve source-named Langting Ting and Lianyue Daozheng as anonymous identities; named exact actors were not resolved.',
1043:'o5 is direct discourse in the named source record, not an anonymous compiler turn; the actor collapse survives.',
1044:'o5 assigns Su Shi as actor although the headword-bearing quoted verdict “Layman Dongpo is far too talkative” belongs to This’an’s later verse.',
1045:'The prose recasts a recurring confession question as a violent command and claims refusal of dependence without anchors; o2/o3/o6 are questioner turns, not compiler narration.',
1046:'o1 is a contents-page string rather than a lexical use; the evidence set also merges garment transmission, imperial bestowal, and event headings without clean actor control.',
1047:'o4 is a named master’s own hall action but remains “unnamed textual utterer”; the exact actor is recoverable from the source container.',
1048:'o2 is an unnamed monk’s direct question, o3/o6 quote Wuzu Jie, and o5 is an embedded dialogue turn; all are collapsed to compiler narration.',
1049:'o3/o4 are named masters’ gate/address wording, not compiler prose; the exact-actor repair is incomplete.',
1050:'o1/o2 are authored exposition in named masters’ records, not anonymous compiler prose; named actor resolution remains incomplete.',
1052:'o2 assigns a narrated introduction to a monastic questioner; o3 is modern literary-history prose and o4 a contents list, neither a defining Chan deployment.',
1053:'o2 is direct discourse in Baizhang’s record and o4 is an embedded attributed teaching passage, not generic compiler narration.',
1057:'o1/o2/o4 are direct named hall actions, and o6 explicitly says the master struck the Chan seat but assigns the questioning monk as actor.',
1058:'o2/o3/o4 are contents/catalogue rows, so three of five stored occurrences are documentary strings rather than lexical deployments.',
1059:'o1/o2/o3 are named hall speakers; o4 falsely assigns ancient Dongshan Liangjie to a seventeenth-century inaugural address; o5 leaves source-named Yushan Shangsi anonymous.',
1060:'o2 leaves source-named Lushan Huacheng Jian anonymous, while o3/o4/o5 are named authored/hall discourse collapsed to compiler.',
1062:'The exact actor logic is inconsistent: o2/o7 assign nearby named figures even though 別云 is the compiler’s attribution marker; other equivalent rows use narrator.',
1066:'o1 contains an unnamed monk’s question about Linji’s four shouts but assigns Zhongfeng Mingben; the remaining authored verse/commentary rows are largely collapsed to compiler.',
1067:'o2/o5/o6 are named hall/commentarial discourse, not anonymous compiler turns; “responsive capacity” also exceeds what the stored formula alone establishes.',
1072:'The board-blocking-one-side explanation is unanchored by every stored occurrence, and named masters’ hall/commentarial uses remain assigned to compiler.',
1073:'o2/o4/o5/o6 are named hall/commentarial speech, and o3 assigns Huangbo Xiyun to a later record owner’s “our house’s seed-stock” utterance.',
1077:'KEEP',
1079:'Every row records a master physically throwing down the whisk in a hall address, yet all six exact actions are assigned to an unnamed compiler.',
1080:'o5 is a later case-title/verse occurrence, not an utterance by Linji Yixuan; the named case is otherwise documentary narration.',
1083:'o1 is a contents-page row; o5 assigns the monk’s question as actor although the headword occurs in the master’s answer “the earth-god hall.”',
1087:'o1/o4/o5 are named hall or authored wording; o2 is Yunfeng Yue’s quoted appraisal but is assigned to Xuedou Chongxian.',
1088:'o1/o3 are masters explicitly raising the Devadatta case in hall discourse, not anonymous compiler narration.',
1091:'The evidence conflates at least three different lexical objects: ordaining preceptor (得戒和尚), Master Jie (戒和尚), and a precept-keeping monk inside 持戒和尚; the single office gloss is invalid.',
1095:'The entry explicitly combines abstract great compassion, the Great-Compassion figure, and Great Compassion Monastery in one sense; different referents require separation and o3 is a title/heading row.',
1097:'o4 assigns Nanyang Huizhong to a much later source owner’s address; o5 is a monk’s direct “Zhaozhou bridge” question but is assigned to compiler.',
1099:'All six rows are visible staff-leaning actions performed by named hall speakers, yet every exact actor is stored as an unnamed compiler.',
1100:'o3 contains the master’s answer “Chaofu waters the ox” but assigns the questioning monk; direct answer and question were reversed.',
}
out=[]
for row in d['entries']:
 p=R/'fresh-build/entries'/row['id']/'entry.v2.json'; sha=hashlib.sha256(p.read_bytes()).hexdigest(); assert sha==row['entrySha256']
 verdict='KEEP' if reasons[row['ordinal']]=='KEEP' else 'REVISE'
 out.append({'ordinal':row['ordinal'],'id':row['id'],'term':row['term'],'entrySha256':sha,'occurrences':row['occurrences'],'verdict':verdict,
  'fullCasesRead':True,'semanticProseRead':True,'reason':'All stored cases, exact turns, source heads, work identities, sense boundaries, and reader prose agree.' if verdict=='KEEP' else reasons[row['ordinal']]})
 for cut in (10,20,30):
  if len(out)==cut:
   q=H/f'f004-b1041-1100-independent-rereview-f004-checkpoint-{cut}.json'; assert not q.exists();q.write_text(json.dumps({'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'reviewer':'f004 independent rereviewer','sourceLedgerSha256':hashlib.sha256(src.read_bytes()).hexdigest(),'entries':out.copy(),'selfReview':False,'edited':False,'promoted':False},ensure_ascii=False,indent=2)+'\n')
final=H/'f004-b1041-1100-independent-rereview-f004.json';assert not final.exists()
payload={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'reviewer':'f004 independent rereviewer','sourceLedger':src.name,'sourceLedgerSha256':hashlib.sha256(src.read_bytes()).hexdigest(),'allCurrentHashesMatched':True,'allFullCasesRead':True,'entries':out,'counts':{'entries':len(out),'occurrences':sum(x['occurrences'] for x in out),'KEEP':sum(x['verdict']=='KEEP' for x in out),'REVISE':sum(x['verdict']=='REVISE' for x in out)},'selfReview':False,'edited':False,'promoted':False}
final.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n');print(final,hashlib.sha256(final.read_bytes()).hexdigest(),payload['counts'])
