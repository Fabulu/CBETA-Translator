#!/usr/bin/env python3
from pathlib import Path
base=Path(__file__).with_name("launch_r92_retry1_constructor.py")
source=base.read_text().replace("r92-retry1","r93-correction1").replace('"R92"','"R93"')
source=source.replace('"t_219099a33daa","t_21a3463bc0db","t_21b44f051c7a"',
 '"t_2202e37854d4","t_2229af16905a","t_222d636a08a9"')
source=source.replace('"疾入於涅槃","隨處","財法二施"','"王老師","威音王","竪窮三際"')
source=source.replace('"requiredFloors":[4,7,4]','"requiredFloors":[7,7,4]')
source=source.replace('"admittedRequiredOccurrences":15,"adjudicatedCaseLoad":15',
 '"admittedRequiredOccurrences":18,"adjudicatedCaseLoad":18')
exec(compile(source,str(base),"exec"),{"__name__":"__main__","__file__":str(base)})
