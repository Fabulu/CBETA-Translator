#!/usr/bin/env python3
import hashlib, json
from datetime import datetime, timezone
from pathlib import Path

root=Path(__file__).resolve().parent.parent
m=root/"maintenance"
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
base=m/"non-iriya-v7-depth-regeneration-r94-timegate-root.json"
fail=m/"non-iriya-v7-depth-regeneration-r94-closure-root.json"
auth=m/"non-iriya-v7-depth-regeneration-r94-final30-authority-review-manifest-root.json"
d={
 "schemaVersion":"r94-correction-artifact-zero.v1","cohort":"R94-correction1",
 "scope":{"ids":["t_28fac5e98308"],"term":"問話","repair":"construct-missing-product"},
 "clockPolicy":"inherits-original-r94-clock-no-reset",
 "bindings":{"originalTimegate":{"path":str(base.relative_to(root)),"sha256":sha(base)},
             "failClosedConstruction":{"path":str(fail.relative_to(root)),"sha256":sha(fail)},
             "final30Authority":{"path":str(auth.relative_to(root)),"sha256":sha(auth)}},
 "correctionDeadlineSeconds":5380,"publicMutationAuthorized":False,
 "createdUtc":datetime.now(timezone.utc).isoformat()
}
p=m/"non-iriya-v7-depth-regeneration-r94-correction1-artifact-zero-root.json"
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print(str(p),sha(p))
