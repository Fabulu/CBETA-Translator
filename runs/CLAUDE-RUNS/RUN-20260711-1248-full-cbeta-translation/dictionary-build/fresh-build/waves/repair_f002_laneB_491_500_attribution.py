#!/usr/bin/env python3
import json
from pathlib import Path
R=Path(__file__).resolve().parents[2]
for id_ in ('t_5636cffb8ad1','t_5f6e8c98ffe7','t_d065698c14a8','t_8bced2c0bc2f','t_26c9a5cb0fe3'):
 p=R/'fresh-build/entries'/id_/'evidence.draft.json';d=json.loads(p.read_text())
 def clean(v):
  if isinstance(v,str):return v.replace('the teacher says that lingering thought','the identified master says that lingering thought').replace("the speaker's prior expectation","the recorded respondent’s prior expectation").replace('or "I knew it.','or “I knew it.”').replace('“chasing the clod.”','“chasing the clod.”').replace('does not yet make a good dog.','does not yet make a good dog.”').replace('water (水): "mixing mud and blending water.','water (水): “mixing mud and blending water.”').replace('objects. the recorded questioner','objects. The recorded questioner')
  if isinstance(v,list):return [clean(x) for x in v]
  if isinstance(v,dict):return {k:clean(x) for k,x in v.items()}
  return v
 p.write_text(json.dumps(clean(d),ensure_ascii=False,indent=2)+'\n')
