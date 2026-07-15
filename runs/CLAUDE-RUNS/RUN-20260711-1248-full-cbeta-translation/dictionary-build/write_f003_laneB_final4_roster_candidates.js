const fs=require('fs');const base=__dirname+'/fresh-build/entries';
const specs=[
 ['Langshan Yu',['浪山嶼','浪山嶼禪師'],'t_51a4f3a03bd8',1],
 ['Lingyin Qingsong',['靈隱清聳','杭州靈隱清聳禪師'],'t_51a4f3a03bd8',3],
 ['Baishan Xinghai',['百癡','百癡禪師'],'t_51a4f3a03bd8',5],
 ['Baozhang Bai',['寶掌白','寶掌白禪師'],'t_51a4f3a03bd8',6],
 ['Tiantong Danjiao',['天童澹交','明州天童澹交禪師'],'t_32a92c635f49',0],
 ['Yongming Daoqian',['永明道潛','杭州永明寺道潛禪師'],'t_32a92c635f49',1],
 ['Letan Changxing',['泐潭常興','洪州泐潭常興禪師'],'t_32a92c635f49',2],
 ['Kaixian Zhao',['開先照','廬山開先照禪師'],'t_32a92c635f49',3],
 ['Chengtian Zong',['承天宗','承天宗禪師'],'t_32a92c635f49',7],
 ['Jingshan Daoqin',['徑山道欽','杭州徑山道欽禪師'],'t_0229ebe0b9e7',3],
 ['Maqiaoshan Benkong',['馬頰山本空','馬頰山本空禪師'],'t_0229ebe0b9e7',6]
];
const candidates=specs.map(([canonicalName,aliases,id,index])=>{const e=JSON.parse(fs.readFileSync(`${base}/${id}/entry.v2.json`)),o=e.Senses[0].Occurrences[index];return {canonicalName,aliases,evidence:[{RelPath:o.RelPath,FromLb:o.FromLb,ToLb:o.ToLb,Kwic:o.Kwic}],reviewedBy:'Codex f003 laneB final4 exact-turn repair author',reviewReport:'fresh-build/waves/f003-laneB-final24-fresh-independent-exact-review.json',status:'awaiting-roster-integration'};});
const out={schemaVersion:1,rule:'Separate candidate packet for root merge into the authoritative roster; every witness is already exact-KWIC verified by the full cohort gate.',candidates};fs.writeFileSync(__dirname+'/fresh-build/waves/f003-laneB-final4-roster-candidates.json',JSON.stringify(out,null,2)+'\n');
