const fs=require('fs');const p=__dirname+'/fresh-build/semantic-regressions.json';const x=JSON.parse(fs.readFileSync(p));
Object.assign(x,{
 't_51a4f3a03bd8':{term:'端的',occurrenceAssertions:[{RelPath:'X/X84/X84n1583.xml',FromLb:'0409a06',mustActorStatus:'reviewed-unnamed',forbiddenMasterNames:['Dahui Zonggao']}]},
 't_32a92c635f49':{term:'宗乘',occurrenceAssertions:[{RelPath:'T/T51/T51n2076.xml',FromLb:'0252a06',mustActorStatus:'reviewed-unnamed',forbiddenMasterNames:['Letan Changxing']}]},
 't_5306489d35c6':{term:'化主',occurrenceAssertions:[{RelPath:'C/C077/C077n1710.xml',FromLb:'0669a04',mustActorStatus:'narrated'}]},
 't_0229ebe0b9e7':{term:'十二時',occurrenceAssertions:[{RelPath:'C/C077/C077n1710.xml',FromLb:'0635b06',mustMasterName:'Huangbo Xiyun',forbiddenMasterNames:['Nanquan Puyuan']},{RelPath:'X/X81/X81n1571.xml',FromLb:'0424a16',mustActorStatus:'narrated',forbiddenMasterNames:['Baozhi']},{RelPath:'X/X80/X80n1565.xml',FromLb:'0051a05',mustMasterName:'Xitang Zhizang'},{RelPath:'T/T51/T51n2076.xml',FromLb:'0230a19',mustMasterName:'Xitang Zhizang'},{RelPath:'X/X68/X68n1318.xml',FromLb:'0349b14',mustMasterName:'Fenyang Shanzhao'}]}
});fs.writeFileSync(p,JSON.stringify(x,null,2)+'\n');
