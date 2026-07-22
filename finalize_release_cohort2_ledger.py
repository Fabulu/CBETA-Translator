#!/usr/bin/env python3
import hashlib,json
from pathlib import Path
H=Path('/mnt/c/programmieren/mergeworkcbeta/cbeta-translator/runs/CLAUDE-RUNS/RUN-20260711-1248-full-cbeta-translation/dictionary-build')
ids='t_49073773ac27 t_4fd4cdbc25b0 t_5ab492eefe6c t_5ba8bf9d83f2 t_6136d614a242 t_6b5825e8dc9a t_6b69c60b142d t_771649f50694 t_7c065becc98f t_7c22b3a70b70 t_7ee5a99b989c t_8873b46d7a4e t_88b6f3526f8e t_8cc2d1c484f9 t_8e59a2d1c6b2 t_9c1e9a072976 t_9d437dfc1719 t_9db05c37d46c t_a0471efec8ca t_a74430b8e7ec'.split()
gate=json.load(open(H/'maintenance/current-wave-release-repair-cohort2-strict-gate.json'))
rows=[]
for i in ids:
 p=H/'fresh-build/entries'/i/'entry.v2.json'; d=json.load(open(p)); rows.append({'id':i,'term':d['SourceTerm'],'entrySha256':hashlib.sha256(p.read_bytes()).hexdigest()})
ledger={'schemaVersion':'current-wave-release-repair-cohort-ledger.v1','cohort':2,'scopeIds':ids,'entries':rows,'pendingRosterPatch':'maintenance/current-wave-release-repair-cohort2-pending-roster-patch.json','sharedPendingRosterEdited':False,'focusedChecks':{'hardPass':gate['hardPass'],'exactFailures':gate['exactKwic']['failureCount'],'attributionExitCode':gate['attribution']['exitCode'],'depthSenseExitCode':gate['depthSense']['exitCode'],'publicFeedbackExitCode':gate['publicFeedback']['exitCode'],'publicFeedbackFlagged':gate['publicFeedback']['payload']['flagged'],'workSourceValidationExitCode':gate['workSourceValidation']['exitCode'],'corpusBaselineExitCode':gate['corpusBaseline']['exitCode']},'semanticReviewRequired':gate['semanticReviewRequired'],'releaseStatus':'focused current-byte gate hardPass=true','collisionControl':'Cohort-local pending roster and distinct ledger paths; no shared pending-roster write.'}
(H/'maintenance/current-wave-release-repair-cohort2-ledger.json').write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
