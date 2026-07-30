#!/usr/bin/env python3
import hashlib, json, os, time
from pathlib import Path
from maintenance.generic_bounded_constructor import verify_actor_closure, verify_whole_config_preclosure, canonical_compile_prewrite
from maintenance.governed_config_rollover import rebind

ROOT=Path(__file__).resolve().parents[1]; M=ROOT/"maintenance"
OLD=M/"non-iriya-v7-depth-regeneration-r69-constructor-config-b.json"
OLD_RESEARCH=M/"non-iriya-v7-depth-regeneration-r69-research-b.json"
AUTH=M/"non-iriya-v7-depth-regeneration-r69-final-release-authority-root.json"
TG=M/"non-iriya-v7-depth-regeneration-r70-timegate-b.json"
SEL=M/"non-iriya-v7-depth-regeneration-r70-selection-b.json"
RESEARCH=M/"non-iriya-v7-depth-regeneration-r70-research-b.json"
CFG=M/"non-iriya-v7-depth-regeneration-r70-constructor-config-b.json"
AUD=M/"non-iriya-v7-depth-regeneration-r70-constructor-command-audit-b.json"
ENGINE=M/"generic_bounded_constructor.py"; WRAP=M/"dictionary_python_env.py"
EXPECTED_OLD="9e901b98b441d7848aec67492937b1a451808785b5dcf2ed20c466544f2f3768"
EXPECTED_AUTH="643da19d38f6b97b12c87660f6b6c77af84859554d5e34e04afb6cd6ddd7961d"
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p): return json.loads(p.read_text(encoding="utf-8"))
def write(p,x):
    t=p.with_name(f".{p.name}.{os.getpid()}.tmp")
    t.write_text(json.dumps(x,ensure_ascii=False,indent=2)+"\n",encoding="utf-8"); os.replace(t,p)
if sha(OLD)!=EXPECTED_OLD or sha(AUTH)!=EXPECTED_AUTH: raise SystemExit("reviewed R69 authority drift")
gate=read(TG); research=read(OLD_RESEARCH); research["cohort"]="R70"
research["researchCheckpointSha256"]=sha(M/"non-iriya-v7-depth-regeneration-r70-research-checkpoint-b.json")
research["inheritedReviewedAuthoritySha256"]=EXPECTED_AUTH
write(RESEARCH,research)
config,audit=rebind(read(OLD),cohort="R70",started_epoch=gate["startedEpoch"],
    old_path_token="r69-",new_path_token="r70-",engine=ENGINE,wrapper=WRAP,
    config_path=CFG,allowed_root=ROOT,audit_epoch=time.time())
for entry in config["entries"]:
    dossier=entry["sourceDossier"]
    dossier["selectionBinding"]={"path":str(SEL.relative_to(ROOT)),"sha256":sha(SEL)}
    dossier["researchBinding"]={"path":str(RESEARCH.relative_to(ROOT)),"sha256":sha(RESEARCH)}
verify_actor_closure(config); verify_whole_config_preclosure(config); canonical_compile_prewrite(config)
write(CFG,config); write(AUD,audit)
print(json.dumps({"configSha256":sha(CFG),"auditSha256":sha(AUD),"researchSha256":sha(RESEARCH)}))
