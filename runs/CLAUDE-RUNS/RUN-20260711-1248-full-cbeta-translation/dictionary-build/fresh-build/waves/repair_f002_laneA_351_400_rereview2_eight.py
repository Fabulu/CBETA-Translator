import json,os,subprocess,sys
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'))
IDS=['t_84e490b1773f','t_eedf4100b3d7','t_18ec645f99f7','t_1e3e02536ca2','t_f4c65b25832f','t_f7c3da035832','t_fac9b9afebf6','t_78bd967fdcd6']
for ident in IDS:
 d=os.path.join(ROOT,'fresh-build','entries',ident);wp=os.path.join(d,'evidence.draft.json');z=json.load(open(wp));e=z['Entry'];s=e['Senses'][0];body=s['ExplanationParts']['EvidenceBody'][0]
 if ident=='t_84e490b1773f':
  body=body.replace('One cited Chan figure claps and laughs','Yangqi Fanghui claps and laughs')
 if ident=='t_eedf4100b3d7':
  body=body.replace("masters order monks to ‘try making a lion's roar,’ monks claim it", "Baozi Guangyun tells his interlocutor to ‘try making a lion's roar,’ and monks claim it")
  body=body.replace("the cited Chan figure answers ‘let the jackal cry as it likes,’", "Shoushan Xingnian answers ‘let the jackal cry as it likes,’")
 if ident=='t_18ec645f99f7':
  body=body.replace("'the student has nostrils' (the student having a nose-hole, 'the student has nostrils')", "‘the student has a nose-hole’ (學人有鼻孔)")
  s['Note']='Primary Linji sense within the four guest-host configurations. The exact nose-hole definition and the stated contrast with Caodong are preserved. Split is required under §5 #0f.8 because the corpus says the identically written category belongs to two different named systems.'
 if ident=='t_1e3e02536ca2':
  body=body.replace('滾作一箇the ball of doubt','滾作一箇疑團').replace('打破the ball of doubt','打破疑團').replace('the ball of doubt不破','疑團不破').replace("the cited voice's mass of doubt more acute (the ball of doubt愈切)","Gaofeng Yuanmiao's mass of doubt more acute (疑團愈切)")
 if ident=='t_f4c65b25832f':
  body=body.replace('“cut the cat” (the variant-graph form of the headword)','“cut the cat” (斬猫)')
  s['Note']='The two cat graphs (貓/猫) are orthographic variants and receive separate searchable articles with one shared case sense. The entry preserves the explicit precept challenges and refusals without deciding why Nanquan acted.'
 if ident=='t_f7c3da035832':
  body=body.replace('Later masters quote all four, ask publicly which kind a particular shout is, and answer questions about the sword-shout', 'Linji Yixuan transmits the fourfold classification; later, an unnamed monk asks about the sword-shout, Dagui Zhe extends the classification to a staff, and Yuanwu Keqin applies its cutting action in commentary. Recorded answers describe the sword-shout')
  s['Note']='271 raw hits in 111 allowlist texts. The headword is corpus-wide as a quoted and extended Linji classification. The complete four are the Diamond King’s jeweled sword, the crouching golden-haired lion, probing pole and shadow grass, and a shout not functioning as a shout.'
 if ident=='t_fac9b9afebf6':
  s['ExplanationParts']['CorpusEarnedOpening']='Probing pole and shadow grass names diagnostic devices for testing what lies before Linji Yixuan’s words or a participant’s conduct.'
  body=body.replace("Later records preserve it as a public category: masters are asked", "Later records preserve it as a public category: unnamed monks ask Shoushan Xingnian and Feiyin Tongrong")
  body=body.replace("A later comment calls a particular conversational move", "Tianyin Yuanxiu calls a particular conversational move")
  s['Note']='251 raw hits in 102 allowlist texts. The traditional probing-pole form is the headword and dominant form; the simplified-graph variant is not adopted. One corpus-wide sense is sufficient.'
 if ident=='t_78bd967fdcd6':
  body=body.replace('the claim that doubt has no fixed magnitude，但疑之重，是謂大疑；疑之輕，是謂小疑','疑情無大小，但疑之重，是謂大疑；疑之輕，是謂小疑')
  s['Note']='Zen bends an ordinary degree phrase by making it a named category explicitly defined against small doubt and repeatedly paired with great awakening. The family comparison with doubt-mass (疑情) and ball of doubt (疑團) supports one sense, not a noun/verb split: both constructions name the same doubt.'
 s['ExplanationParts']['EvidenceBody'][0]=body
 open(wp,'w').write(json.dumps(z,ensure_ascii=False,indent=2)+'\n')
 r=subprocess.run([sys.executable,os.path.join(ROOT,'compile_evidence_draft.py'),wp,'--output',os.path.join(d,'entry.v2.json'),'--report',os.path.join(d,'compile-report.json')],capture_output=True,text=True)
 assert r.returncode==0,r.stdout+r.stderr
print(json.dumps({'recompiled':IDS}))
