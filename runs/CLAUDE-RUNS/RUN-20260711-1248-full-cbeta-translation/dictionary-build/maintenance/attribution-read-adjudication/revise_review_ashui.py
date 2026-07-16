import hashlib, json
from datetime import datetime, timezone
from pathlib import Path

B=Path(__file__).resolve().parents[2]
ENTRY=B/'fresh-build'/'entries'/'t_43ecdacadde0'/'entry.v2.json'
OUT=Path(__file__).with_name('ashui-t_43ecdacadde0-independent-review-ledger.json')

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

old=sha(ENTRY)
data=json.loads(ENTRY.read_text())
sense=data['Senses'][0]
sense['Note']=(
    "Family comparison: 'who?' (誰) and 'which one?' (阿那箇) are interrogatives; "
    "'that one' (那箇) and 'this one' (這箇) are demonstratives; 'who?' (阿誰) does not "
    "acquire a separate 'true self' sense merely because identity is sometimes its subject. "
    "Agent, author, beneficiary, and lineage predecessor are different answers to the same "
    "pronoun, not different referents encoded by the word. Frozen-corpus concordance: 3312 "
    "exact hits in 374 storage files representing 369 independent works."
)
occ=sense['Occurrences'][2]
occ['MasterName']='Dazhu Huihai'
occ['ContextMasters']=[{'MasterName':'Dazhu Huihai','Roles':['utterer']}]
occ['AttributionNote']=(
    'Source text (景德傳燈錄). Exact actor: Dazhu Huihai. '
    'The complete section headed 越州大珠慧海禪師 identifies Dazhu—not Mazu—as the master '
    'who asks the scripture lecturer who spoke the scripture.'
)
sense['Explanation']=(
    'Who? is the direct interrogative used to demand a person rather than a doctrine or object. '
    'Baizhang Huaihai asks “Who are you?” and who can carry a message; Dazhu Huihai asks who '
    'spoke a scripture. Yunyan Tansheng asks for whom Baizhang works daily, and Xueyan Zuqin '
    'repeatedly asks Gaofeng Yuanmiao who drags the dead corpse. Unnamed monks use the same '
    'word to demand lineage succession or the recipient of an unsayable phrase. The force is '
    'the public demand to identify the responsible person in the exchange.'
)
ENTRY.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n')
payload={
    'generatedUtc':datetime.now(timezone.utc).isoformat(),
    'entryId':'t_43ecdacadde0','SourceTerm':'阿誰','disposition':'REVISE',
    'selfApproved':False,'requiresIndependentReview':True,
    'oldSha256':old,'newSha256':sha(ENTRY),
    'findings':[
        'Corrected the scripture-lecturer occurrence from Mazu Daoyi to Dazhu Huihai after full-section reread.',
        'Removed three duplicated copies of the frozen-corpus concordance sentence.',
        'Updated the definition prose to name Dazhu Huihai.'
    ]
}
OUT.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(payload,ensure_ascii=False))
