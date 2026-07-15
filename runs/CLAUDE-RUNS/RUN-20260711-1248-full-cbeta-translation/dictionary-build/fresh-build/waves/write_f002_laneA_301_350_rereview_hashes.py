import hashlib,json,os
R=os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..'))
rows=[(305,'t_6c58ed7a7c6c'),(313,'t_c81bf91e508f'),(315,'t_5ac2c5d1fc1e'),(317,'t_c9ba42aa7e47'),(321,'t_2745ffff5972'),(330,'t_ab715aa474d5'),(332,'t_72e01bbb3474'),(333,'t_af92172da506'),(335,'t_f1eb87aa18ef'),(336,'t_1a86ee3d406f'),(339,'t_15eac1a3b037'),(340,'t_694f447dbd89'),(347,'t_aab4ca02ec21'),(349,'t_7653f61478aa'),(350,'t_a784d81e277b')]
def h(p):return hashlib.sha256(open(os.path.join(R,p),'rb').read()).hexdigest()
entries=[]
for ordinal,id in rows:
 d=f'fresh-build/entries/{id}';e=json.load(open(os.path.join(R,d,'entry.v2.json')));cr=json.load(open(os.path.join(R,d,'compile-report.json')));assert cr['hardPass']
 entries.append({'ordinal':ordinal,'id':id,'term':e['SourceTerm'],'worksheet':f'{d}/evidence.draft.json','worksheetSha256':h(f'{d}/evidence.draft.json'),'output':f'{d}/entry.v2.json','outputSha256':h(f'{d}/entry.v2.json'),'compilerReceipt':f'{d}/compile-report.json','compilerReceiptSha256':h(f'{d}/compile-report.json')})
p='fresh-build/waves/f002-laneA-301-350-rereview-updated-hashes.json';out={'schemaVersion':1,'wave':'f002','lane':'A','scope':'specified provisional rereview repairs only','formalGateRun':False,'siteTouched':False,'attributionDiagnostics':{'path':'fresh-build/waves/f002-laneA-301-350-rereview-attribution-diagnostics.json','sha256':h('fresh-build/waves/f002-laneA-301-350-rereview-attribution-diagnostics.json'),'hardFailures':0},'entries':entries};open(os.path.join(R,p),'w').write(json.dumps(out,ensure_ascii=False,indent=2)+'\n');print(h(p))
