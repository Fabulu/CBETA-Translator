const fs=require("fs"),path=require("path");
const template=fs.readFileSync(path.join(__dirname,"merge_r90_publication_root.js"),"utf8");
let source=template.replaceAll("R90","R91").replaceAll("r90","r91");
const start=source.indexOf("const expected=new Map([");
const end=source.indexOf("]), replacements=new Map();",start)+2;
const expected=`const expected=new Map([
 ["t_21170b1b9a8d","4a1f2f2f735fd5b42e8cc81a1ab0f1951cfd761fbb7d153c332d0abe3fe16dcb"],
 ["t_211c871daa1f","6db85bc0d81f96a7468c40ffe8272c4bb0e33893e5371772dd2b8e1f06539a45"],
 ["t_218e4815d84a","ab3665fec395befb6cfb35e0c605ab5f535e1eac300e9e2756d5094cf048183a"]
])`;
source=source.slice(0,start)+expected+source.slice(end);
Function("require","process","__dirname",source)(require,process,__dirname);
