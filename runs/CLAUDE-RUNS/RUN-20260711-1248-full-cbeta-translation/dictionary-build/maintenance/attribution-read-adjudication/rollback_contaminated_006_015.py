import json
from pathlib import Path
root=Path(__file__).resolve().parents[2]
merged=json.loads((root/"fresh-build/merged/termbase.v2.json").read_text())
entries=merged.get("Entries") or merged.get("entries")
byid={e["Id"]:e for e in entries}
for eid in ["t_7f696f177766","t_81147ad4e8bf","t_8879b278cd83","t_8a016f49e5b8","t_90e46d995978"]:
 p=root/f"fresh-build/entries/{eid}/entry.v2.json"
 p.write_text(json.dumps(byid[eid],ensure_ascii=False,indent=2)+"\n")
 print("restored",eid)
