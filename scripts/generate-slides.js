"use strict";
const pptxgen = require("pptxgenjs");
const path = require("path");

const pres = new pptxgen();
pres.layout = "LAYOUT_16x9";
pres.author = "Adrien Clerbois";
pres.title = "Comment j'ai hacke ma machine a cafe pour en faire un MCP";

const C = {
  bg: "180C05", bgLight: "281508", card: "341A0A", cardHi: "432010",
  gold: "D49A14", amber: "F5B828", cream: "FBF0DD", muted: "9A7050",
  blue: "1A72B8", teal: "178A52", purple: "7B3A7A", rust: "C64018",
  green: "2A8A40", dim: "4A2A14",
};
const W = 10, H = 5.625, TOTAL = 16;

function fillBg(s, c=C.bg){ s.addShape(pres.shapes.RECTANGLE,{x:0,y:0,w:W,h:H,fill:{color:c},line:{color:c}}); }
function leftBar(s){ s.addShape(pres.shapes.RECTANGLE,{x:0,y:0,w:0.4,h:H,fill:{color:C.rust},line:{color:C.rust}}); }
function contentSlide(title, section){
  const s=pres.addSlide(); fillBg(s); leftBar(s);
  s.addText(title,{x:0.65,y:0.22,w:8.8,h:0.64,fontSize:21,fontFace:"Trebuchet MS",bold:true,color:C.cream,align:"left",margin:0});
  s.addShape(pres.shapes.LINE,{x:0.65,y:0.88,w:8.7,h:0,line:{color:C.gold,width:0.75}});
  if(section) s.addText(section.toUpperCase(),{x:0.65,y:0.22,w:8.7,h:0.3,fontSize:7.5,color:C.muted,bold:true,charSpacing:2,align:"right",margin:0});
  return s;
}
function node(sl,x,y,w,h,{label,sub,fill=C.card,border=C.gold,tc=C.cream}={}){
  sl.addShape(pres.shapes.RECTANGLE,{x,y,w,h,fill:{color:fill},line:{color:border,width:0.75}});
  if(label||sub){
    const items=[];
    if(label) items.push({text:label,options:{bold:true,fontSize:10,color:tc,breakLine:!!sub}});
    if(sub)   items.push({text:sub,options:{fontSize:8,color:C.muted}});
    sl.addText(items,{x,y,w,h,align:"center",valign:"middle",margin:4});
  }
}
function hArrow(sl,x1,yMid,x2,label="",dashed=false){
  sl.addShape(pres.shapes.LINE,{x:x1,y:yMid,w:x2-x1,h:0,line:{color:C.gold,width:1.3,endArrowType:"arrow",dashType:dashed?"dash":"solid"}});
  if(label){const cx=(x1+x2)/2;sl.addText(label,{x:cx-0.65,y:yMid-0.27,w:1.3,h:0.26,fontSize:7.5,color:C.muted,align:"center",margin:0});}
}
function vArrow(sl,xMid,y1,y2,label="",dashed=false){
  sl.addShape(pres.shapes.LINE,{x:xMid,y:y1,w:0,h:y2-y1,line:{color:C.gold,width:1.3,endArrowType:"arrow",dashType:dashed?"dash":"solid"}});
  if(label) sl.addText(label,{x:xMid+0.1,y:(y1+y2)/2-0.13,w:1.6,h:0.26,fontSize:7.5,color:C.muted,margin:0});
}
function badge(sl,x,y,num,size=0.32){
  sl.addShape(pres.shapes.OVAL,{x,y,w:size,h:size,fill:{color:C.gold},line:{color:C.gold}});
  sl.addText(String(num),{x,y,w:size,h:size,fontSize:9,bold:true,color:C.bg,align:"center",valign:"middle",margin:0});
}
function pageNum(sl,num,total){
  sl.addText(`${num} / ${total}`,{x:8.8,y:5.3,w:0.9,h:0.22,fontSize:7.5,color:C.dim,align:"right",margin:0});
}

// SLIDE 1 - Title
{
  const s = pres.addSlide(); fillBg(s); leftBar(s);
  s.addShape(pres.shapes.OVAL,{x:7.5,y:-0.9,w:3.8,h:3.8,fill:{color:C.card},line:{color:C.gold,width:1.5}});
  s.addShape(pres.shapes.OVAL,{x:7.85,y:-0.55,w:3.1,h:3.1,fill:{color:C.bgLight},line:{color:C.rust,width:0.7}});
  s.addText("Comment j'ai hacke\nma machine a cafe",{x:0.7,y:0.55,w:7.2,h:2.1,fontSize:36,fontFace:"Georgia",bold:true,color:C.cream,align:"left"});
  s.addText("pour en faire un MCP",{x:0.7,y:2.72,w:7.2,h:0.72,fontSize:25,fontFace:"Georgia",italic:true,color:C.gold,align:"left"});
  s.addShape(pres.shapes.LINE,{x:0.7,y:3.54,w:6.2,h:0,line:{color:C.dim,width:0.75}});
  s.addText("Adrien Clerbois  -  2026",{x:0.7,y:3.7,w:6.2,h:0.35,fontSize:12,color:C.muted,align:"left",margin:0});
  const tags=["APK Reverse Engineering","Network Tracing","ECAM Binary Protocol","MCP + .NET 10"];
  tags.forEach((t,i)=>{
    const tx=0.7+i*2.25;
    s.addShape(pres.shapes.RECTANGLE,{x:tx,y:4.22,w:2.1,h:0.32,fill:{color:C.card},line:{color:C.gold,width:0.4}});
    s.addText(t,{x:tx,y:4.22,w:2.1,h:0.32,fontSize:8,color:C.amber,align:"center",valign:"middle",margin:0});
  });
  s.addNotes("Bonjour a tous ! Je m'appelle Adrien Clerbois et aujourd'hui je vais vous raconter comment j'ai transforme ma machine a cafe De'Longhi en un outil pilote par une intelligence artificielle.\n\nL'histoire commence avec un APK Android, quelques outils de decompilation, un proxy reseau pour le tracing, et beaucoup de curiosite.\n\nSpoiler : a la fin, GitHub Copilot m'a fait un expresso.");
}

// SLIDE 2 - Qu'est-ce que le MCP ?
{
  const s=contentSlide("Qu est-ce que le MCP ?","MODEL CONTEXT PROTOCOL");
  pageNum(s,2,TOTAL);
  // Definition banner
  s.addShape(pres.shapes.RECTANGLE,{x:0.65,y:1.05,w:9.1,h:0.7,fill:{color:C.card},line:{color:C.rust,width:0.75}});
  s.addShape(pres.shapes.RECTANGLE,{x:0.65,y:1.05,w:0.07,h:0.7,fill:{color:C.rust},line:{color:C.rust}});
  s.addText("Model Context Protocol - standard ouvert cree par Anthropic (nov. 2024), adopte par OpenAI, Google, Microsoft...\nComme un USB, mais pour l'IA : n'importe quel AI client peut parler a n'importe quel serveur MCP.",{x:0.85,y:1.05,w:8.8,h:0.7,fontSize:11,color:C.cream,align:"left",valign:"middle",margin:6});
  // Flow diagram
  const MNODES=[
    {x:0.65,label:"AI Client",sub:"Copilot / Claude\nCursor / Windsurf",color:C.blue},
    {x:3.7, label:"MCP Server",sub:"McpDelonghi\n.NET 10  stdio",color:C.teal},
    {x:6.75,label:"Outils et Donnees",sub:"14 tools\nAPI IoT De'Longhi",color:C.gold},
  ];
  MNODES.forEach(n=>{
    s.addShape(pres.shapes.RECTANGLE,{x:n.x,y:2.0,w:2.65,h:1.12,fill:{color:C.card},line:{color:n.color,width:0.9}});
    s.addShape(pres.shapes.RECTANGLE,{x:n.x,y:2.0,w:2.65,h:0.07,fill:{color:n.color},line:{color:n.color}});
    s.addText(n.label,{x:n.x,y:2.1,w:2.65,h:0.4,fontSize:13,bold:true,color:C.cream,align:"center",margin:0});
    s.addText(n.sub,{x:n.x,y:2.54,w:2.65,h:0.5,fontSize:9.5,color:C.muted,align:"center",margin:0});
  });
  hArrow(s,0.65+2.65,2.56,3.7,"JSON-RPC 2.0\nstdio / HTTP-SSE");
  hArrow(s,3.7+2.65,2.56,6.75,"tool_call\nresult");
  // 3 primitives
  const MPRIMS=[
    {icon:"TOOLS",color:C.rust, title:"Tools",   desc:"Actions appelables par l'IA\nbrew_beverage, power_on..."},
    {icon:"RES",  color:C.blue, title:"Resources",desc:"Donnees lisibles par l'IA\nfichiers, URLs, BDD"},
    {icon:"PRO",  color:C.purple,title:"Prompts", desc:"Templates reutilisables\npour guider le modele"},
  ];
  MPRIMS.forEach((p,i)=>{
    const x=0.65+i*3.15;
    s.addShape(pres.shapes.RECTANGLE,{x,y:3.3,w:2.98,h:1.95,fill:{color:C.card},line:{color:p.color,width:0.6}});
    s.addShape(pres.shapes.RECTANGLE,{x,y:3.3,w:0.07,h:1.95,fill:{color:p.color},line:{color:p.color}});
    s.addText(p.icon,{x,y:3.37,w:2.98,h:0.5,fontSize:20,bold:true,color:p.color,align:"center",margin:0});
    s.addText(p.title,{x:x+0.15,y:3.91,w:2.72,h:0.34,fontSize:12,bold:true,color:C.cream,margin:0});
    s.addShape(pres.shapes.LINE,{x:x+0.15,y:4.29,w:2.68,h:0,line:{color:C.dim,width:0.4}});
    s.addText(p.desc,{x:x+0.15,y:4.37,w:2.72,h:0.75,fontSize:9.5,color:C.muted,margin:0});
  });
  s.addNotes("Avant de plonger dans le hack, quelques mots sur ce qu'est le MCP.\n\nModel Context Protocol est un standard ouvert cree par Anthropic en novembre 2024. L'idee : definir un protocole universel pour connecter les AI agents aux outils et donnees du monde reel.\n\nL'analogie parfaite : USB. Avant l'USB, chaque peripherique avait son propre connecteur. Avec l'USB, un standard unique. MCP fait pareil pour l'IA : n'importe quel client MCP - GitHub Copilot, Claude Desktop, Cursor, Windsurf - peut utiliser n'importe quel serveur MCP.\n\nLe protocole utilise JSON-RPC 2.0. La communication se fait via stdio pour les serveurs locaux - comme le notre - ou HTTP-SSE pour les serveurs distants.\n\nTrois primitives : les Tools que l'IA peut appeler - brew_beverage, get_machine_status, etc. Les Resources que l'IA peut lire. Et les Prompts - des templates reutilisables.\n\nAdopte tres rapidement par OpenAI, Google, Microsoft, Mistral - c'est aujourd'hui le standard de facto pour connecter les AI agents au monde reel.");
}

// SLIDE 3 - La machine
{
  const s=contentSlide("La machine : De'Longhi Eletta Explore","INTRODUCTION");
  pageNum(s,3,TOTAL);
  const feats=[
    ["Expresso full automatique","Machine full auto haut de gamme"],
    ["Application Coffee Link","Android / iOS, connexion WiFi"],
    ["WiFi integre","Plateforme Ayla Networks IoT"],
    ["30+ boissons","Recettes par profil, mouture ajustable",false],
    ["API cloud non documentee","...mais presente dans l'APK",true],
  ];
  feats.forEach(([icon,text,highlight],i)=>{
    const y=1.05+i*0.8;
    s.addShape(pres.shapes.RECTANGLE,{x:0.65,y,w:5.7,h:0.65,fill:{color:C.card},line:{color:C.dim,width:0.4}});
    s.addShape(pres.shapes.RECTANGLE,{x:0.65,y,w:0.07,h:0.65,fill:{color:highlight?C.gold:C.teal},line:{color:highlight?C.gold:C.teal}});
    s.addText(text,{x:0.88,y,w:5.4,h:0.65,fontSize:11.5,color:highlight?C.amber:C.cream,bold:!!highlight,align:"left",valign:"middle",margin:0});
  });
  s.addShape(pres.shapes.RECTANGLE,{x:6.6,y:1.05,w:3.05,h:4.2,fill:{color:C.card},line:{color:C.gold,width:0.75}});
  [{val:"30+",label:"boissons disponibles"},{val:"8 313",label:"fichiers Java\ndecompiles"},{val:"3",label:"couches reseau\nretro-ingeniees"}].forEach(({val,label},i)=>{
    const y=1.2+i*1.35;
    s.addText(val,{x:6.6,y,w:3.05,h:0.75,fontSize:46,bold:true,fontFace:"Georgia",color:C.gold,align:"center",valign:"middle",margin:0});
    s.addText(label,{x:6.6,y:y+0.75,w:3.05,h:0.5,fontSize:10,color:C.muted,align:"center",margin:0});
    if(i<2) s.addShape(pres.shapes.LINE,{x:6.85,y:y+1.28,w:2.55,h:0,line:{color:C.dim,width:0.4}});
  });
  s.addNotes("La machine en question, c'est une De'Longhi Eletta Explore - un expresso connecte haut de gamme. Elle a une app Android officielle, Coffee Link, et un module WiFi integre.\n\nSur le papier, c'est une machine pour faire du cafe. Dans la pratique, c'est un systeme IoT complet avec une API REST privee, un protocole binaire custom, et deux fournisseurs de services cloud completement meconnus : Ayla Networks et Gigya.\n\n8 313 fichiers Java decompiles depuis l'APK, et 3 couches reseau a retro-ingenier.");
}

// SLIDE 3 - Point de depart: l'APK
{
  const s=contentSlide("Le point de depart : decompiler l'APK","REVERSE ENGINEERING");
  pageNum(s,4,TOTAL);
  const steps=[
    {num:"01",color:C.blue,title:"Telecharger l'APK",desc:"De'Longhi Coffee Link\nsur le Play Store\nExtraction via ADB"},
    {num:"02",color:C.teal,title:"Decompiler avec jadx",desc:"jadx 1.5.1\n8 313 fichiers Java\nit.delonghi.* lisible"},
    {num:"03",color:C.gold,title:"Analyser le code source",desc:"DeLonghi.java\nApp ID, App Secret\nURLs, protocole ECAM"},
  ];
  steps.forEach((step,i)=>{
    const x=0.65+i*3.1;
    s.addShape(pres.shapes.RECTANGLE,{x,y:1.05,w:2.92,h:4.18,fill:{color:C.card},line:{color:step.color,width:1}});
    s.addShape(pres.shapes.RECTANGLE,{x,y:1.05,w:2.92,h:0.08,fill:{color:step.color},line:{color:step.color}});
    s.addText(step.num,{x,y:1.18,w:2.92,h:0.8,fontSize:44,bold:true,fontFace:"Georgia",color:step.color,align:"center",valign:"middle",margin:0});
    s.addText(step.title,{x:x+0.15,y:2.1,w:2.62,h:0.48,fontSize:13,bold:true,color:C.cream,align:"center",margin:0});
    s.addShape(pres.shapes.LINE,{x:x+0.3,y:2.63,w:2.32,h:0,line:{color:C.dim,width:0.4}});
    s.addText(step.desc,{x:x+0.1,y:2.75,w:2.72,h:1.5,fontSize:11,color:C.muted,align:"center",margin:0});
    if(i<2) hArrow(s,x+2.92,3.14,x+3.1);
  });
  s.addShape(pres.shapes.RECTANGLE,{x:0.65,y:4.92,w:9.1,h:0.35,fill:{color:C.bgLight},line:{color:C.dim,width:0.4}});
  s.addText("jadx-1.5.1  -  Java OpenJDK 21  -  docs/apk/decompiled/  -  winget: No package found -> manual GitHub download",{x:0.75,y:4.92,w:9.0,h:0.35,fontSize:8.5,color:C.muted,italic:true,align:"left",valign:"middle",margin:0});
  s.addNotes("Mon point de depart : l'APK de l'application Android. Pas la documentation officielle, pas une API publique - l'APK.\n\nJ'ai extrait le fichier depuis le Play Store, et j'ai utilise jadx, un decompilateur Java open source. Petite anecdote : winget n'avait pas le package jadx, donc j'ai du le telecharger manuellement depuis les releases GitHub.\n\nLe resultat : 8 313 fichiers Java. Et la bonne surprise : le package it.delonghi.* n'etait pas obfusque. Les noms de classes, methodes et variables sont lisibles - DeLonghiWifiConnectService, EcamServiceV2, BeanSystem...");
}

// SLIDE 4 - Ce qu'on trouve dans l'APK
{
  const s=contentSlide("Ce qu'on trouve dans l'APK","REVERSE ENGINEERING");
  pageNum(s,5,TOTAL);
  const findings=[
    {file:"DeLonghi.java:63",label:"Ayla App ID & Secret",code:'appId     = "DLonghiCoffeeIdKit-sQ-id"\nappSecret = "DLonghiCoffeeIdKit-HT6b0VNd4y6..."',color:C.rust},
    {file:"AndroidManifest.xml:186",label:"Gigya API Key",code:'gigya_api_key = "4_DRIMLu7jk9bkKwpRRoQOuw"',color:C.purple},
    {file:"DeLonghiWifiConnectService.java:293",label:"Propriete ECAM monitor",code:'d302_monitor_machine  (Eletta)\nd302_monitor           (PrimaDonna)',color:C.teal},
    {file:"DeLonghiWifiConnectService.java:670",label:"Ping de connexion WiFi",code:'app_device_connected\n-> force push des donnees machine',color:C.blue},
  ];
  findings.forEach((f,i)=>{
    const col=i%2,row=Math.floor(i/2);
    const x=0.65+col*4.7,y=1.05+row*2.2;
    s.addShape(pres.shapes.RECTANGLE,{x,y,w:4.45,h:2.02,fill:{color:C.card},line:{color:f.color,width:0.75}});
    s.addShape(pres.shapes.RECTANGLE,{x,y,w:0.07,h:2.02,fill:{color:f.color},line:{color:f.color}});
    s.addText(f.file,{x:x+0.17,y:y+0.1,w:4.15,h:0.26,fontSize:8,color:C.muted,fontFace:"Consolas",margin:0});
    s.addText(f.label,{x:x+0.17,y:y+0.38,w:4.15,h:0.32,fontSize:12,bold:true,color:C.cream,margin:0});
    s.addShape(pres.shapes.RECTANGLE,{x:x+0.17,y:y+0.74,w:4.12,h:1.08,fill:{color:"0D0A07"},line:{color:C.dim,width:0.4}});
    s.addText(f.code,{x:x+0.27,y:y+0.74,w:4.0,h:1.08,fontSize:9.5,color:C.amber,fontFace:"Consolas",align:"left",valign:"middle",margin:5});
  });
  s.addNotes("Dans ces 8 000 fichiers, j'ai trouve exactement ce que j'esperais.\n\nDes le fichier DeLonghi.java, ligne 63 : l'App ID et l'App Secret Ayla Networks - en clair, hardcodes dans le source.\n\nDans le AndroidManifest.xml : la cle API Gigya.\n\nDans DeLonghiWifiConnectService.java : les noms exacts des proprietes IoT que l'application lit et ecrit - d302_monitor_machine pour lire l'etat de la machine, app_device_connected pour declencher un ping.\n\nCe sont ces noms de proprietes qui vont me permettre de communiquer avec la machine via l'API Ayla.");
}

// SLIDE 5 - Le tracing reseau (NOUVEAU)
{
  const s=contentSlide("Le tracing reseau : intercepter le trafic en live","REVERSE ENGINEERING");
  pageNum(s,6,TOTAL);
  const nds=[
    {x:0.52,w:2.3,label:"Telephone\nAndroid",sub:"Coffee Link App\ncert. racine installe",border:C.blue},
    {x:3.7,w:2.5,label:"mitmproxy\n(proxy HTTPS)",sub:"dechiffrement TLS\ncertificat custom",border:C.gold},
    {x:7.1,w:2.55,label:"APIs Cloud\nDe'Longhi",sub:"accounts.eu1.gigya.com\nads-eu.aylanetworks.com",border:C.purple},
  ];
  nds.forEach(n=>node(s,n.x,1.08,n.w,1.28,{label:n.label,sub:n.sub,border:n.border}));
  hArrow(s,0.52+2.3,1.72,3.7,"HTTPS intercepte");
  hArrow(s,3.7+2.5,1.72,7.1,"HTTPS re-chiffre");
  s.addText("Setup : proxy WiFi 192.168.x.x:8080  +  installation certificat mitmproxy sur Android",{x:0.52,y:2.55,w:9.1,h:0.35,fontSize:9,color:C.muted,fontFace:"Consolas",italic:true,align:"left",margin:0});
  const REQS=[
    {method:"POST",color:C.purple,url:"accounts.eu1.gigya.com\n/accounts.login",body:"loginID=user@mail.com\npassword=***\napiKey=4_DRIMLu7...",resp:"id_token: eyJhbG..."},
    {method:"POST",color:C.teal,url:"user-field-eu.aylanetworks.com\n/api/v1/token_sign_in",body:'uid_signature: ...\napp_id: "DLonghiCoffee..."',resp:"access_token: ...\nexpires_in: 86400"},
    {method:"POST",color:C.rust,url:"ads-eu.aylanetworks.com\n/apiv1/dsns/{DSN}/properties\n/app_data_request/datapoints.json",body:'value: "0D0983F00201002802..."',resp:"201 Created"},
  ];
  REQS.forEach((r,i)=>{
    const x=0.52+i*3.12;
    s.addShape(pres.shapes.RECTANGLE,{x,y:3.02,w:2.95,h:2.28,fill:{color:C.card},line:{color:r.color,width:0.75}});
    s.addShape(pres.shapes.RECTANGLE,{x,y:3.02,w:0.07,h:2.28,fill:{color:r.color},line:{color:r.color}});
    s.addText(r.method,{x:x+0.15,y:3.07,w:2.7,h:0.26,fontSize:9,bold:true,color:r.color,margin:0});
    s.addText(r.url,{x:x+0.15,y:3.33,w:2.7,h:0.52,fontSize:8,fontFace:"Consolas",color:C.cream,margin:0});
    s.addShape(pres.shapes.LINE,{x:x+0.15,y:3.88,w:2.65,h:0,line:{color:C.dim,width:0.4}});
    s.addText(r.body,{x:x+0.15,y:3.93,w:2.7,h:0.65,fontSize:7.5,fontFace:"Consolas",color:C.muted,margin:0});
    s.addShape(pres.shapes.RECTANGLE,{x:x+0.15,y:4.62,w:2.7,h:0.42,fill:{color:C.bgLight},line:{color:r.color,width:0.35}});
    s.addText("<- "+r.resp,{x:x+0.22,y:4.62,w:2.6,h:0.42,fontSize:7.5,fontFace:"Consolas",color:r.color,valign:"middle",margin:2});
  });
  s.addNotes("L'APK m'a donne la structure du code, mais pour confirmer le protocole reel et voir les donnees qui transitent en live, j'ai mis en place du tracing reseau.\n\nLa technique : configurer mitmproxy comme proxy HTTPS sur le WiFi, installer son certificat racine sur Android, et lancer l'application. Toutes les requetes HTTPS passent alors en clair.\n\nCe que j'ai observe en live :\n1. L'app appelle d'abord Gigya pour s'authentifier - j'ai vu le loginID, la cle API, et l'id_token JWT en reponse.\n2. Ensuite un appel Ayla pour echanger ce token et obtenir un access_token valable 24h.\n3. Enfin les vraies commandes vers la machine : un POST vers /apiv1/dsns/{DSN}/properties/app_data_request/datapoints.json avec en body une valeur Base64 - le paquet ECAM binaire.\n\nLe tracing reseau a ete la cle pour comprendre le format exact des requetes.");
}

// SLIDE 6 - Architecture: Authentification
{
  const s=contentSlide("Architecture : flux d'authentification en 3 etapes","ARCHITECTURE");
  pageNum(s,7,TOTAL);
  const NW=1.52,NH=1.35,NY=1.45;
  const NX=[0.45,2.41,4.37,6.33,8.1];
  const nDefs=[
    {label:"MCP Client\n(AI Agent)",sub:"GitHub Copilot\nVS Code",border:C.blue},
    {label:"Gigya EU1\nIdentity",sub:"accounts.eu1\n.gigya.com",border:C.purple},
    {label:"JWT\nExchange",sub:"id_token\n-> Ayla token",border:C.gold},
    {label:"Ayla Networks\nIoT Cloud EU",sub:"user-field-eu\n.aylanetworks.com",border:C.teal},
    {label:"Machine\nDe'Longhi",sub:"DSN / ECAM",border:C.rust},
  ];
  nDefs.forEach((n,i)=>node(s,NX[i],NY,NW,NH,{label:n.label,sub:n.sub,border:n.border}));
  const aLbls=["email + password","id_token JWT","token_sign_in","REST props"];
  NX.forEach((x,i)=>{if(i<4) hArrow(s,x+NW,NY+NH/2,NX[i+1],aLbls[i]);});
  [1,2,3].forEach((n,i)=>badge(s,NX[i]+NW-0.15,NY-0.16,n));
  const details=[
    {x:0.45,text:"API Key Gigya\n4_DRIMLu7jk9bkKwpRRoQOuw",color:C.purple},
    {x:3.2,text:"App ID + Secret\nDLonghiCoffeeIdKit-*",color:C.teal},
    {x:5.95,text:"Token cache\nSemaphoreSlim",color:C.blue},
    {x:8.0,text:"auth_token header\nx-ayla-source: Mobile",color:C.gold},
  ];
  details.forEach(d=>{
    s.addShape(pres.shapes.RECTANGLE,{x:d.x,y:3.1,w:2.5,h:0.88,fill:{color:C.bgLight},line:{color:d.color,width:0.5}});
    s.addText(d.text,{x:d.x+0.1,y:3.1,w:2.3,h:0.88,fontSize:8.5,color:C.cream,margin:4});
  });
  s.addShape(pres.shapes.RECTANGLE,{x:0.45,y:4.18,w:9.2,h:0.65,fill:{color:"0D0A07"},line:{color:C.dim,width:0.4}});
  s.addText("DelonghiAuthService.cs  ->  GigyaLogin() -> ExchangeToken() -> AylaSignIn()  ->  token cached in-memory, auto-refresh",{x:0.6,y:4.18,w:9.0,h:0.65,fontSize:9,color:C.amber,fontFace:"Consolas",align:"left",valign:"middle",margin:4});
  s.addNotes("Le flux d'authentification se fait en 3 etapes, toutes visibles dans le tracing reseau que j'ai fait au prealable.\n\nEtape 1 : on s'authentifie sur Gigya avec l'email et le mot de passe De'Longhi. Gigya est le fournisseur d'identite - c'est SAP Gigya, utilise par des milliers de marques. On recoit un id_token JWT.\n\nEtape 2 : on echange ce token JWT contre un access_token Ayla via token_sign_in. C'est la qu'on passe aussi l'App ID et l'App Secret trouves dans l'APK.\n\nEtape 3 : tous les appels Ayla suivants utilisent cet access_token dans le header auth_token, avec x-ayla-source: Mobile pour se faire passer pour l'app.\n\nEn C#, tout ca est encapsule dans DelonghiAuthService avec un cache thread-safe et auto-refresh.");
}

// SLIDE 7 - Protocole ECAM
{
  const s=contentSlide("Le protocole ECAM : structure du paquet binaire","PROTOCOLE");
  pageNum(s,8,TOTAL);
  const BYTES=[
    {hex:"D0",label:"header",color:C.blue},
    {hex:"len",label:"longueur-1",color:C.blue},
    {hex:"83",label:"cmd BREW",color:C.rust},
    {hex:"F0",label:"flags",color:C.purple},
    {hex:"02",label:"profil",color:C.teal},
    {hex:"01",label:"pid:COFFEE",color:C.gold,wide:true},
    {hex:"00 28",label:"40 mL (16b)",color:C.gold,wide:true},
    {hex:"02",label:"pid:GRIND",color:C.amber},
    {hex:"05",label:"niveau 5",color:C.amber},
    {hex:"03",label:"pid:TEMP",color:C.green},
    {hex:"01",label:"Medium",color:C.green},
    {hex:"CRC_H CRC_L",label:"CRC-16",color:C.muted,wide:true},
  ];
  const BW=0.6,BH=0.72,BY=1.08,GAP=0.04;
  let cx=0.52;
  BYTES.forEach(b=>{
    const bw=b.wide?BW*1.5:BW;
    s.addShape(pres.shapes.RECTANGLE,{x:cx,y:BY,w:bw,h:BH,fill:{color:C.card},line:{color:b.color,width:0.9}});
    s.addText(b.hex,{x:cx,y:BY,w:bw,h:BH,fontSize:8.5,bold:true,fontFace:"Consolas",color:b.color,align:"center",valign:"middle",margin:0});
    s.addText(b.label,{x:cx-0.05,y:BY+BH+0.07,w:bw+0.1,h:0.38,fontSize:7,color:C.muted,align:"center",margin:0});
    cx+=bw+GAP;
  });
  s.addShape(pres.shapes.RECTANGLE,{x:0.52,y:2.45,w:9.2,h:0.58,fill:{color:C.bgLight},line:{color:C.gold,width:0.5}});
  s.addText("CRC-16 / SPI-FUJITSU  -  Polynome : 0x1021  Init : 0x1D0F  Calcule sur tous les octets SAUF les 2 derniers",{x:0.65,y:2.45,w:9.0,h:0.58,fontSize:10,color:C.amber,fontFace:"Consolas",align:"left",valign:"middle",margin:4});
  s.addText("Commandes ECAM connues :",{x:0.52,y:3.2,w:2.5,h:0.3,fontSize:9.5,color:C.muted,bold:true,margin:0});
  const CMDS=[{cmd:"0x75",name:"POWER_ON"},{cmd:"0x80",name:"POWER_OFF"},{cmd:"0x83",name:"BREW"},{cmd:"0x84",name:"CANCEL"},{cmd:"0x8F",name:"PING"},{cmd:"0x95",name:"SETTINGS"},{cmd:"0xA1",name:"PROFILE"},{cmd:"0xA4",name:"RENAME"},{cmd:"0xA6",name:"RECIPE"}];
  CMDS.forEach((c,i)=>{
    const bx=3.1+i*0.86;
    s.addShape(pres.shapes.RECTANGLE,{x:bx,y:3.12,w:0.79,h:0.42,fill:{color:C.card},line:{color:C.teal,width:0.4}});
    s.addText([{text:c.cmd,options:{bold:true,fontSize:7.5,color:C.teal,breakLine:true}},{text:c.name,options:{fontSize:7,color:C.muted}}],{x:bx,y:3.12,w:0.79,h:0.42,align:"center",valign:"middle",margin:2});
  });
  s.addShape(pres.shapes.RECTANGLE,{x:0.52,y:3.72,w:9.2,h:0.42,fill:{color:C.bgLight},line:{color:C.dim,width:0.4}});
  s.addText("Format donnees : TLV (Tag-Length-Value) - big params COFFEE/MILK/HOT_WATER sur 2 octets (16-bit), autres sur 1 octet",{x:0.65,y:3.72,w:9.0,h:0.42,fontSize:9,color:C.muted,align:"left",valign:"middle",margin:0});
  s.addText("Transport : paquets ECAM encodes Base64 -> valeurs de proprietes Ayla (app_data_request / data_request)",{x:0.52,y:4.28,w:9.2,h:0.35,fontSize:9,color:C.muted,italic:true,align:"left",margin:0});
  s.addNotes("Le protocole ECAM est le coeur du systeme. C'est un format binaire custom qui circule entre le module WiFi et le CPU de la machine sur un bus interne.\n\nChaque paquet : 0xD0 en premier octet, la longueur-1, le code commande - ici 0x83 pour BREW -, les flags, le numero de profil, puis les donnees en TLV.\n\nTLV : Tag-Length-Value. Chaque parametre commence par un ID - 01 pour le cafe, 02 pour la mouture, 03 pour la temperature. La valeur suit sur 1 ou 2 octets.\n\nLe CRC-16 a ete le defi : des dizaines de variantes existent. J'ai trouve dans le code Java la confirmation de SPI-FUJITSU - polynome 0x1021, init 0x1D0F.\n\nLes paquets sont ensuite encodes en Base64 avant d'etre envoyes a Ayla comme valeur de propriete.");
}

// SLIDE 8 - Architecture: Transport IoT
{
  const s=contentSlide("Architecture : transport IoT - Ayla Networks","ARCHITECTURE");
  pageNum(s,9,TOTAL);
  const COL=[
    {x:0.5,w:2.6,label:"McpDelonghi\n.NET 10",color:C.blue,items:["AylaApiClient","DelonghiAuthService","EcamPacket","Crc16"]},
    {x:3.65,w:2.85,label:"Ayla Networks\nIoT Cloud (EU)",color:C.purple,items:["user-field-eu (auth)","ads-eu (device)","app_data_request","d302_monitor_machine","Base64 ECAM payload"]},
    {x:7.05,w:2.65,label:"De'Longhi\nEletta Explore",color:C.rust,items:["WiFi + Ayla Agent","ECAM CPU","CRC-16 validation","Binary UART bus"]},
  ];
  const PY=1.05,PH=4.15,IY0=2.0,IH=0.58,IG=0.06;
  COL.forEach(col=>{
    s.addShape(pres.shapes.RECTANGLE,{x:col.x,y:PY,w:col.w,h:PH,fill:{color:C.card},line:{color:col.color,width:1}});
    s.addShape(pres.shapes.RECTANGLE,{x:col.x,y:PY,w:col.w,h:0.08,fill:{color:col.color},line:{color:col.color}});
    s.addText(col.label,{x:col.x,y:PY+0.12,w:col.w,h:0.78,fontSize:12,bold:true,color:C.cream,align:"center",margin:0});
    col.items.forEach((item,i)=>{
      const iy=IY0+i*(IH+IG);
      s.addShape(pres.shapes.RECTANGLE,{x:col.x+0.14,y:iy,w:col.w-0.28,h:IH,fill:{color:C.bgLight},line:{color:col.color,width:0.35}});
      s.addText(item,{x:col.x+0.14,y:iy,w:col.w-0.28,h:IH,fontSize:9,fontFace:"Consolas",color:C.cream,align:"center",valign:"middle",margin:2});
    });
  });
  const midY=PY+PH/2;
  hArrow(s,COL[0].x+COL[0].w,midY,COL[1].x,"HTTPS\nREST API");
  hArrow(s,COL[1].x+COL[1].w,midY,COL[2].x,"MQTT /\nWebSocket");
  s.addNotes("Voici le schema de transport. Notre serveur MCP .NET 10 contient quatre classes cles : AylaApiClient pour les appels REST, DelonghiAuthService pour le token, EcamPacket pour construire les paquets binaires, et Crc16 pour le checksum.\n\nAu centre, Ayla Networks fait office de cloud IoT - il maintient la connexion persistante avec la machine. On envoie nos commandes HTTPS vers ads-eu.aylanetworks.com, et Ayla les pousse a la machine via MQTT ou WebSocket.\n\nA droite, la machine recoit les commandes via son module WiFi, qui les passe en interne au CPU ECAM. Ce CPU valide le CRC-16 avant d'executer la commande.\n\nC'est la plateforme que le tracing reseau m'a permis de cartographier completement.");
}

// SLIDE 9 - Construire le serveur MCP
{
  const s=contentSlide("Construire le serveur MCP en C# (.NET 10)","IMPLEMENTATION");
  pageNum(s,10,TOTAL);
  const cards=[
    {icon:"SDK",color:C.blue,title:"ModelContextProtocol SDK",code:"AddMcpServer()\n .WithStdioServerTransport()\n .WithToolsFromAssembly()",note:"Enregistrement automatique\ndes outils par reflexion .NET"},
    {icon:"ATT",color:C.teal,title:"Pattern Tool Attribute",code:"[McpServerToolType]\n[McpServerTool]\n[Description(\"...\")]",note:"Chaque methode C# devient\nun outil MCP expose au modele"},
    {icon:"AUTH",color:C.gold,title:"Auth + Client IoT",code:"DelonghiAuthService\nAylaApiClient\nEcamPacket / Crc16",note:"Token cache thread-safe\nFallback app/data_request"},
  ];
  cards.forEach((c,i)=>{
    const x=0.6+i*3.1;
    s.addShape(pres.shapes.RECTANGLE,{x,y:1.05,w:2.92,h:4.2,fill:{color:C.card},line:{color:c.color,width:0.75}});
    s.addShape(pres.shapes.RECTANGLE,{x,y:1.05,w:2.92,h:0.08,fill:{color:c.color},line:{color:c.color}});
    s.addText(c.icon,{x,y:1.2,w:2.92,h:0.65,fontSize:26,bold:true,color:c.color,align:"center",margin:0});
    s.addText(c.title,{x:x+0.12,y:1.92,w:2.68,h:0.48,fontSize:12,bold:true,color:C.cream,align:"center",margin:0});
    s.addShape(pres.shapes.RECTANGLE,{x:x+0.12,y:2.47,w:2.68,h:1.08,fill:{color:"0D0A07"},line:{color:C.dim,width:0.4}});
    s.addText(c.code,{x:x+0.2,y:2.47,w:2.55,h:1.08,fontSize:9,fontFace:"Consolas",color:c.color,align:"left",valign:"middle",margin:5});
    s.addShape(pres.shapes.LINE,{x:x+0.2,y:3.62,w:2.52,h:0,line:{color:C.dim,width:0.4}});
    s.addText(c.note,{x:x+0.12,y:3.7,w:2.68,h:0.72,fontSize:9.5,color:C.muted,align:"center",margin:0});
  });
  s.addNotes("Cote implementation MCP, le SDK .NET est remarquablement simple a prendre en main.\n\nTrois lignes dans Program.cs : AddMcpServer(), WithStdioServerTransport() - le serveur communique en JSON-RPC sur stdin/stdout, c'est le standard MCP - et WithToolsFromAssembly() pour l'auto-decouverte.\n\nLe framework scanne l'assembly a la recherche des classes marquees [McpServerToolType]. Les methodes avec [McpServerTool] deviennent des outils. Le [Description(...)] sur la methode et sur chaque parametre devient la documentation que le modele de langage lit pour savoir quoi appeler.\n\nResultat : le modele voit directement 'brew_beverage - brews a beverage on the De'Longhi machine', avec les parametres documentes. Zero configuration supplementaire.");
}

// SLIDE 10 - Architecture: Vue d'ensemble MCP
{
  const s=contentSlide("Architecture : vue d'ensemble - de l'IA a la cafetiere","ARCHITECTURE");
  pageNum(s,11,TOTAL);
  const TIERS=[
    {y:1.08,h:0.76,color:C.blue,label:"AI Agent",sub:"GitHub Copilot  VS Code  Claude  Cursor  etc."},
    {y:2.1,h:0.76,color:C.teal,label:"McpDelonghi Server (.NET 10)",sub:"BrewTools  StatusTools  MachineryTools  (stdio)"},
    {y:3.12,h:0.76,color:C.purple,label:"Ayla Networks IoT Cloud EU",sub:"user-field-eu.aylanetworks.com  ads-eu.aylanetworks.com"},
    {y:4.14,h:0.78,color:C.rust,label:"De'Longhi Eletta Explore",sub:"WiFi Module  ECAM CPU  CRC-16 SPI-FUJITSU  Binary Protocol"},
  ];
  TIERS.forEach(t=>{
    s.addShape(pres.shapes.RECTANGLE,{x:0.5,y:t.y,w:5.8,h:t.h,fill:{color:C.card},line:{color:t.color,width:0.9}});
    s.addShape(pres.shapes.RECTANGLE,{x:0.5,y:t.y,w:0.08,h:t.h,fill:{color:t.color},line:{color:t.color}});
    s.addText(t.label,{x:0.75,y:t.y+0.05,w:5.5,h:0.38,fontSize:12,bold:true,color:C.cream,margin:0});
    s.addText(t.sub,{x:0.75,y:t.y+0.4,w:5.5,h:0.28,fontSize:8.5,color:C.muted,margin:0});
  });
  const AX=3.4;
  [{label:"MCP Protocol (stdio)"},{label:"HTTPS REST API"},{label:"Base64 ECAM packets"}].forEach((a,i)=>{
    vArrow(s,AX,TIERS[i].y+TIERS[i].h,TIERS[i+1].y,a.label);
  });
  const ANNOTS=[
    {y:TIERS[0].y,color:C.blue,text:"JSON-RPC 2.0\ntool calls"},
    {y:TIERS[1].y,color:C.teal,text:"C# DI\nHttpClient"},
    {y:TIERS[2].y,color:C.purple,text:"Gigya EU1\n-> JWT token"},
    {y:TIERS[3].y,color:C.rust,text:"cmd 0x83\n+ CRC-16"},
  ];
  ANNOTS.forEach(a=>{
    s.addShape(pres.shapes.RECTANGLE,{x:6.65,y:a.y+0.08,w:3.05,h:a.y===TIERS[3].y?0.62:0.58,fill:{color:C.bgLight},line:{color:a.color,width:0.5}});
    s.addText(a.text,{x:6.65,y:a.y+0.08,w:3.05,h:0.6,fontSize:10,color:a.color,align:"center",valign:"middle",margin:0});
  });
  s.addNotes("Voici la vue d'ensemble complete, de l'IA a la cafetiere.\n\nQuand je demande a Copilot de me faire un expresso, voici ce qui se passe :\n\nL'agent AI envoie un tool call JSON-RPC 2.0 au serveur MCP via stdio - c'est la communication standard du protocole MCP.\n\nLe serveur MCP traduit ca en appels REST HTTPS vers Ayla Networks, avec l'authentification JWT.\n\nAyla pousse la commande ECAM encodee en Base64 a la machine via sa connexion persistante.\n\nLa machine decode le paquet, valide le CRC-16, et execute - cafe en preparation.\n\nTout ce systeme tient dans environ 800 lignes de C#, sans dependances exotiques.");
}

// SLIDE 11 - Les outils MCP
{
  const s=contentSlide("Les outils MCP exposes","IMPLEMENTATION");
  pageNum(s,12,TOTAL);
  const TOOLS=[
    {name:"get_machine_status",cat:"Status",color:C.teal},
    {name:"get_maintenance_info",cat:"Status",color:C.teal},
    {name:"get_beverage_counters",cat:"Status",color:C.teal},
    {name:"get_profiles",cat:"Status",color:C.teal},
    {name:"get_device_info",cat:"Status NEW",color:C.gold},
    {name:"get_machine_settings",cat:"Status NEW",color:C.gold},
    {name:"get_raw_properties",cat:"Debug",color:C.muted},
    {name:"get_beverages",cat:"Brew",color:C.blue},
    {name:"brew_beverage",cat:"Brew",color:C.blue},
    {name:"stop_beverage",cat:"Brew",color:C.blue},
    {name:"get_beverage_recipe",cat:"Brew NEW",color:C.gold},
    {name:"power_on",cat:"Machine",color:C.rust},
    {name:"power_off",cat:"Machine",color:C.rust},
    {name:"set_active_profile",cat:"Machine NEW",color:C.gold},
  ];
  const COLS=7,TW=1.31,TH=0.75,SX=0.42,SY=1.05,HGAP=0.03,VGAP=0.18;
  TOOLS.forEach((t,i)=>{
    const col=i%COLS,row=Math.floor(i/COLS);
    const x=SX+col*(TW+HGAP),y=SY+row*(TH+VGAP);
    const isNew=t.cat.includes("NEW");
    s.addShape(pres.shapes.RECTANGLE,{x,y,w:TW,h:TH,fill:{color:C.card},line:{color:t.color,width:isNew?1.1:0.4}});
    if(isNew) s.addShape(pres.shapes.RECTANGLE,{x,y,w:TW,h:0.07,fill:{color:C.gold},line:{color:C.gold}});
    s.addText([{text:t.name,options:{bold:true,fontSize:7.5,color:C.cream,breakLine:true}},{text:t.cat,options:{fontSize:7.5,color:t.color}}],{x,y,w:TW,h:TH,align:"center",valign:"middle",margin:3});
  });
  s.addShape(pres.shapes.LINE,{x:0.42,y:4.82,w:9.15,h:0,line:{color:C.dim,width:0.4}});
  s.addText("NEW : Nouvel outil ajoute apres analyse APK (get_device_info, get_machine_settings, get_beverage_recipe, set_active_profile)",{x:0.42,y:4.9,w:9.15,h:0.3,fontSize:9,color:C.gold,align:"left",margin:0});
  s.addNotes("Le serveur expose 14 outils au total, en trois categories.\n\nStatus : tout ce qui concerne l'etat de la machine - son etat courant, les alertes, les compteurs de boissons, les profils, et grace au RE de l'APK, les parametres machine comme l'unite de temperature et la durete de l'eau.\n\nBrew : get_beverages pour lister les boissons disponibles, brew_beverage pour en preparer une, stop_beverage pour annuler, et get_beverage_recipe pour voir les parametres detailles d'une recette.\n\nMachine : power_on, power_off, et set_active_profile pour changer le profil utilisateur actif.\n\nLes 4 outils NEW ont ete ajoutes directement a partir de l'analyse APK et du code DeLonghiWifiConnectService.java.");
}

// SLIDE 12 - Nouvelles fonctionnalites
{
  const s=contentSlide("Nouvelles fonctionnalites decouvertes dans l'APK","NOUVELLES FEATURES");
  pageNum(s,13,TOTAL);
  const NT=[
    {name:"get_device_info",desc:"Modele, OEM code, firmware\nDSN, statut de connexion",source:"GetDevicesAsync() -> AylaDevice\nasw_version  oem_model  dsn"},
    {name:"get_machine_settings",desc:"Unite temperature (C/F)\nMinuteur auto-off, durete eau 1-5",source:"d281_mach_sett_temperature\nd282_mach_sett_auto_off"},
    {name:"get_beverage_recipe",desc:"Recette complete : volume cafe/lait\nmouture, temperature, pre-ground",source:"TLV parse -> _rec_{profile}_{bev}\n{ coffee_ml, grind_level, temperature }"},
    {name:"set_active_profile",desc:"Active le profil 1-4 sur la machine\nCommande ECAM 0x95 setting 0xEE",source:"C2407d.getPacketForSetProfile\n-> DeLonghiWifiConnectService.java"},
  ];
  NT.forEach((t,i)=>{
    const col=i%2,row=Math.floor(i/2);
    const x=0.5+col*4.8,y=1.05+row*2.25;
    s.addShape(pres.shapes.RECTANGLE,{x,y,w:4.55,h:2.05,fill:{color:C.card},line:{color:C.gold,width:1}});
    s.addShape(pres.shapes.RECTANGLE,{x,y,w:4.55,h:0.08,fill:{color:C.gold},line:{color:C.gold}});
    s.addText(t.name,{x:x+0.15,y:y+0.12,w:4.25,h:0.38,fontSize:12,bold:true,fontFace:"Consolas",color:C.amber,margin:0});
    s.addText(t.desc,{x:x+0.15,y:y+0.54,w:4.25,h:0.5,fontSize:10,color:C.cream,margin:0});
    s.addShape(pres.shapes.RECTANGLE,{x:x+0.15,y:y+1.1,w:4.25,h:0.78,fill:{color:"0D0A07"},line:{color:C.dim,width:0.4}});
    s.addText(t.source,{x:x+0.22,y:y+1.1,w:4.1,h:0.78,fontSize:8.5,fontFace:"Consolas",color:C.amber,align:"left",valign:"middle",margin:4});
  });
  s.addNotes("Ces 4 nouveaux outils illustrent le processus de decouverte de fonctionnalites.\n\nPour get_beverage_recipe : le tracing reseau m'avait montre que les recettes sont stockees comme proprietes Ayla - des blobs Base64. En cherchant getPacketForRecipe dans le code Java, j'ai compris le format TLV. Maintenant le modele peut demander 'quelle est ma recette d'expresso ?' et obtenir le volume exact, la mouture, la temperature.\n\nPour set_active_profile : dans DeLonghiWifiConnectService.java, j'ai trouve getPacketForSetProfile qui appelle C2407d avec le code commande 0x95, setting 0xEE, et le numero de profil. Implemente en C# en 10 lignes.\n\nC'est la force de l'approche APK + tracing : on ne devine pas, on sait exactement ce que la machine accepte.");
}

// SLIDE 13 - DEMO
{
  const s=pres.addSlide(); fillBg(s);
  s.addShape(pres.shapes.RECTANGLE,{x:5.6,y:0,w:4.4,h:H,fill:{color:C.card},line:{color:C.card}});
  s.addShape(pres.shapes.RECTANGLE,{x:5.6,y:0,w:0.07,h:H,fill:{color:C.gold},line:{color:C.gold}});
  s.addText("LIVE",{x:0.4,y:0.25,w:5.0,h:1.15,fontSize:80,fontFace:"Georgia",bold:true,color:C.gold,align:"left"});
  s.addText("DEMO",{x:0.4,y:1.4,w:5.0,h:0.95,fontSize:68,fontFace:"Georgia",bold:true,color:C.cream,align:"left"});
  s.addText('"Fais-moi un expresso"\n-> l\'agent appelle brew_beverage\n-> la machine prepare votre cafe',{x:0.4,y:2.55,w:5.0,h:1.55,fontSize:14,color:C.muted,fontFace:"Georgia",italic:true,align:"left"});
  const STEPS=[
    {n:"1",t:"User prompt",d:'"Make me an espresso"'},
    {n:"2",t:"Tool call",d:"brew_beverage(espresso, 2)"},
    {n:"3",t:"ECAM packet",d:"0xD0 ... 0x83 ... CRC-16"},
    {n:"4",t:"Ayla REST",d:"POST app_data_request"},
    {n:"5",t:"Machine",d:"Brewing starts"},
  ];
  STEPS.forEach((st,i)=>{
    const y=0.95+i*0.85;
    badge(s,5.92,y+0.12,st.n,0.36);
    s.addText(st.t,{x:6.42,y:y+0.05,w:3.2,h:0.3,fontSize:11.5,bold:true,color:C.cream,margin:0});
    s.addText(st.d,{x:6.42,y:y+0.36,w:3.2,h:0.26,fontSize:8.5,color:C.muted,fontFace:"Consolas",margin:0});
    if(i<4) s.addShape(pres.shapes.LINE,{x:6.1,y:y+0.62,w:0,h:0.22,line:{color:C.gold,width:0.9,dashType:"dash"}});
  });
  pageNum(s,14,TOTAL);
  s.addNotes("Place a la demonstration !\n\n[Ouvrir VS Code avec GitHub Copilot, onglet Chat]\n\nJe vais simplement demander : 'Fais-moi un expresso'.\n\nCopilot analyse la demande, selectionne l'outil brew_beverage parmi les 14 disponibles, remplit les parametres beverage_key='espresso', profile=2.\n\nLe serveur MCP construit le paquet ECAM binaire, calcule le CRC-16, encode en Base64, et POST vers Ayla.\n\nLa machine recoit la commande, valide le CRC, et commence la preparation.\n\n[Attendre le son de la machine...]\n\nCe que vous venez de voir : un chemin complet de l'intention en langage naturel a un acte physique reel, grace a APK reverse engineering + MCP.");
}

// SLIDE 14 - Ce qu'on a appris
{
  const s=contentSlide("Ce qu'on a appris","LEARNINGS");
  pageNum(s,15,TOTAL);
  const LEARN=[
    {icon:"APK",color:C.gold,title:"Les APKs sont une mine d'or",desc:"Credentials, URLs, protocoles binaires - tout est la,\nsouvent lisible dans les packages non obfusques."},
    {icon:"NET",color:C.purple,title:"Le tracing reseau confirme tout",desc:"mitmproxy sur le WiFi + certificat Android =\nvoir chaque requete en clair, sans effort."},
    {icon:"MCP",color:C.teal,title:"MCP = pont parfait entre AI et IoT",desc:"Un serveur MCP transforme une API privee\nen outil natif pour tout AI agent."},
    {icon:"RE",color:C.rust,title:"Le reverse engineering debloque tout",desc:".NET 10 + MCP SDK + quelques jours de RE\n= expresso commande par une IA."},
  ];
  LEARN.forEach((l,i)=>{
    const col=i%2,row=Math.floor(i/2);
    const x=0.5+col*4.8,y=1.05+row*2.25;
    s.addShape(pres.shapes.RECTANGLE,{x,y,w:4.55,h:2.06,fill:{color:C.card},line:{color:l.color,width:0.75}});
    s.addShape(pres.shapes.RECTANGLE,{x,y,w:0.07,h:2.06,fill:{color:l.color},line:{color:l.color}});
    s.addText(l.icon+" - "+l.title,{x:x+0.2,y:y+0.14,w:4.2,h:0.46,fontSize:12,bold:true,color:C.cream,margin:0});
    s.addShape(pres.shapes.LINE,{x:x+0.2,y:y+0.65,w:4.1,h:0,line:{color:C.dim,width:0.4}});
    s.addText(l.desc,{x:x+0.2,y:y+0.76,w:4.2,h:1.1,fontSize:10.5,color:C.muted,margin:0});
  });
  s.addNotes("Quatre lecons de cette aventure.\n\nUn : les APKs Android sont incroyablement riches en information. Des que l'obfuscation est partielle ou absente, vous avez acces a toute l'architecture interne.\n\nDeux : le tracing reseau avec mitmproxy est rapide a mettre en place et extremement puissant. Configuration du proxy WiFi, certificat racine sur Android, et on voit tout en clair. C'est complementaire a l'analyse statique du code.\n\nTrois : MCP est exactement le protocole qu'il manquait pour connecter les AI agents au monde reel. Une fois le serveur ecrit, n'importe quel agent compatible MCP - Copilot, Claude, Cursor - peut piloter la machine.\n\nQuatre : le reverse engineering n'est pas reserve aux hackers. C'est une competence d'ingenieur qui permet de comprendre des systemes fermes et de les integrer dans des outils nouveaux.");
}

// SLIDE 15 - Conclusion
{
  const s=pres.addSlide(); fillBg(s); leftBar(s);
  s.addText("From APK to Autonomous Barista",{x:0.7,y:0.48,w:8.8,h:0.75,fontSize:28,fontFace:"Georgia",bold:true,color:C.cream,align:"left"});
  const PIPELINE=[
    {icon:"APK",label:"APK",sub:"jadx"},
    {icon:"NET",label:"Tracing",sub:"mitmproxy"},
    {icon:"KEY",label:"Auth",sub:"Gigya+Ayla"},
    {icon:"PRO",label:"Protocol",sub:"ECAM CRC"},
    {icon:"MCP",label:"MCP",sub:".NET 10"},
    {icon:"CUP",label:"Coffee!",sub:"AI agent"},
  ];
  const PW=1.32,PH=1.12,PY=1.52;
  PIPELINE.forEach((p,i)=>{
    const px=0.6+i*(PW+0.18);
    s.addShape(pres.shapes.RECTANGLE,{x:px,y:PY,w:PW,h:PH,fill:{color:C.card},line:{color:C.gold,width:0.75}});
    s.addText(p.icon+"\n"+p.label,{x:px,y:PY+0.05,w:PW,h:0.75,fontSize:13,bold:true,color:C.cream,align:"center",margin:0});
    s.addText(p.sub,{x:px,y:PY+0.82,w:PW,h:0.27,fontSize:8.5,color:C.muted,align:"center",margin:0});
    if(i<5) hArrow(s,px+PW,PY+PH/2,px+PW+0.18);
  });
  s.addShape(pres.shapes.RECTANGLE,{x:0.7,y:3.05,w:8.8,h:0.68,fill:{color:C.card},line:{color:C.gold,width:0.75}});
  s.addText("github.com/AClerbois/mcp-delonghi",{x:0.7,y:3.05,w:8.8,h:0.68,fontSize:17,bold:true,fontFace:"Consolas",color:C.amber,align:"center",valign:"middle",margin:0});
  s.addText('"The best way to understand a system is to take it apart\n- then put it back together as an AI tool."',{x:0.7,y:3.95,w:8.8,h:0.82,fontSize:12,italic:true,color:C.muted,fontFace:"Georgia",align:"center",margin:0});
  s.addShape(pres.shapes.LINE,{x:0.7,y:4.88,w:8.8,h:0,line:{color:C.dim,width:0.5}});
  s.addText("Made with reverse engineering  -  2026",{x:0.7,y:4.97,w:8.8,h:0.28,fontSize:9,color:C.dim,align:"center",margin:0});
  pageNum(s,16,TOTAL);
  s.addNotes("En resume, le pipeline complet : APK -> tracing reseau -> credentials -> auth Gigya + Ayla -> protocole ECAM -> serveur MCP .NET 10 -> expresso par IA.\n\nLe code source est open source sur GitHub : github.com/AClerbois/mcp-delonghi. N'hesitez pas a forker, adapter pour votre propre machine connectee, ou contribuer de nouveaux outils.\n\nPour aller plus loin : il reste beaucoup a explorer dans l'APK - la gestion du Bean System, la configuration du WiFi, le renommage des recettes custom. Le protocole ECAM est riche.\n\nMerci pour votre attention. Des questions ?");
}

// -- Write file --
const OUT=path.join(__dirname,"..","docs","mcp-delonghi-talk.pptx");
pres.writeFile({fileName:OUT})
  .then(()=>console.log(`Presentation saved -> ${OUT}  (${TOTAL} slides)`))
  .catch(err=>{console.error("Error:",err);process.exit(1);});
