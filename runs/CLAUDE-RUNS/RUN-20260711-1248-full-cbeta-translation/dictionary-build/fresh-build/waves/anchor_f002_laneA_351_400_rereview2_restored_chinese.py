import json,os,subprocess,sys
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'));sys.path.insert(0,ROOT);import zc
def anchor(rel,kw,claim,name):
 v=zc.verify(rel,kw);assert v['ok'],(rel,kw,v)
 return {'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'ClaimText':claim,'MasterName':name,'Curated':True,'AttributionNote':f'Source text ({zc.title(rel)}). {name} owns the exact restored source phrase after complete-case review.','ContextMasters':[{'MasterName':name,'Roles':['utterer']}],'DraftActorProof':{'ExactHeadwordClause':kw,'GrammaticalSubject':name,'SpeechFrame':f'Complete-case review assigns this exact source phrase to {name}.','FullCaseDecision':f'{name} owns the exact restored source phrase.'}}
for ident in ['t_18ec645f99f7','t_f4c65b25832f','t_78bd967fdcd6']:
 d=os.path.join(ROOT,'fresh-build','entries',ident);wp=os.path.join(d,'evidence.draft.json');z=json.load(open(wp));s=z['Entry']['Senses'][0]
 if ident=='t_18ec645f99f7':s['ClaimAnchors']=[anchor('T/T48/T48n2006.xml','四賓主者。師家有鼻孔。名主中主。學人有鼻孔。名賓中主。','學人有鼻孔','Huiyan Zhizhao')]
 if ident=='t_f4c65b25832f':s['ClaimAnchors']=[anchor('X/X82/X82n1571.xml','南泉斬猫話','斬猫','Hu Anguo')]
 if ident=='t_78bd967fdcd6':
  s['ExplanationParts']['EvidenceBody']=[x.replace('疑情無大小','疑。情無大小') for x in s['ExplanationParts']['EvidenceBody']]
  o=s['Occurrences'][0];kw='疑。情無大小，但疑之重，是謂大疑；疑之輕，是謂小疑';v=zc.verify(o['RelPath'],kw);assert v['ok'];o.update(Kwic=kw,FromLb=v['fromLb'],ToLb=v['toLb'])
  s['ClaimAnchors']=[anchor('X/X72/X72n1435.xml','滾作一箇疑團','疑團','Wuyi Yuanlai')]
 open(wp,'w').write(json.dumps(z,ensure_ascii=False,indent=2)+'\n')
 r=subprocess.run([sys.executable,os.path.join(ROOT,'compile_evidence_draft.py'),wp,'--output',os.path.join(d,'entry.v2.json'),'--report',os.path.join(d,'compile-report.json')],capture_output=True,text=True);assert r.returncode==0,r.stdout+r.stderr
print('anchored restored Chinese')
