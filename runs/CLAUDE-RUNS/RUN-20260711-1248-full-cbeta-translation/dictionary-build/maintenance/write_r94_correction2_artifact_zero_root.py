#!/usr/bin/env python3
import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
root=Path(__file__).resolve().parent.parent;m=root/"maintenance"
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
bind={}
for k,n in {
"timegate":"non-iriya-v7-depth-regeneration-r94-timegate-root.json",
"authority":"non-iriya-v7-depth-regeneration-r94-final30-authority-review-manifest-root.json",
"correction1Closure":"non-iriya-v7-depth-regeneration-r94-correction1-closure-root.json",
"failureReview":"non-iriya-v7-depth-regeneration-r94-correction1-whole-batch-independent-review-a.json"}.items():
 p=m/n;bind[k]={"path":str(p.relative_to(root)),"sha256":sha(p)}
d={"schemaVersion":"r94-correction-artifact-zero.v1","cohort":"R94-correction2",
"scope":{"entryCount":30,"repair":"mechanical-full-product-regeneration-from-final-authority"},
"clockPolicy":"inherits-original-r94-clock-no-reset","correctionDeadlineSeconds":5380,
"bindings":bind,"semanticResearchAuthorized":False,"publicMutationAuthorized":False,
"createdUtc":datetime.now(timezone.utc).isoformat()}
p=m/"non-iriya-v7-depth-regeneration-r94-correction2-artifact-zero-root.json"
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n");print(str(p),sha(p))
