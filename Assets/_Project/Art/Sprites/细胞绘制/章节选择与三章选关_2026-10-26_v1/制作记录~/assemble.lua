local f=assert(io.open(app.params.plan,'r'));local p=json.decode(f:read('*a'));f:close()
local pc=app.pixelColor;local W,H=1600,900
local dark=pc.rgba(57,21,54,255);local ivory=pc.rgba(255,246,216,255);local cyan=pc.rgba(33,196,217,255)
local function native(im,path,name)
 local s=Sprite(im.width,im.height,ColorMode.RGB);s:assignColorSpace(ColorSpace{sRGB=true})
 s.layers[1].name=name or '美术基底';s:newCel(s.layers[1],1,im,Point(0,0));s:saveAs(path);s:close()
end
local function fit(im,w,h)
 local scale=math.min(w/im.width,h/im.height);local cp=Image(im)
 cp:resize(math.max(1,math.floor(im.width*scale+.5)),math.max(1,math.floor(im.height*scale+.5)))
 local out=Image(w,h,ColorMode.RGB);out:drawImage(cp,Point(math.floor((w-cp.width)/2),math.floor((h-cp.height)/2)))
 return out
end
local function slice(im,w,h,b)
 local result=Image(w,h,ColorMode.RGB)
 local sx={0,b,im.width-b};local sw={b,im.width-2*b,b};local sy={0,b,im.height-b};local sh={b,im.height-2*b,b}
 local dx={0,b,w-b};local dw={b,w-2*b,b};local dy={0,b,h-b};local dh={b,h-2*b,b}
 assert(w>2*b and h>2*b)
 for y=1,3 do for x=1,3 do
  local tile=Image(im,Rectangle(sx[x],sy[y],sw[x],sh[y]));tile:resize(dw[x],dh[y]);result:drawImage(tile,Point(dx[x],dy[y]))
 end end
 return result
end
local images={};local exports={};local ui={};local text={};local bgs={}
local function export(im,name,folder)
 local path=p.art..'/'..(folder or '组件')..'/'..name..'.png';im:saveAs(path)
 exports[#exports+1]={file=path,width=im.width,height=im.height};return path
end
local raw
for _,j in ipairs(p.jobs) do
 if j.id=='kit' then raw=Image{fromFile=j.raw}
 else
  local im=Image{fromFile=j.raw};im:resize(W,H);im:saveAs(j.target);bgs[j.id]=im
 end
end
assert(raw)
local componentSheet=Image(900,500,ColorMode.RGB)
for i,c in ipairs(p.kitRegions) do
 local r=c.region;local cut=Image(raw,Rectangle(r[1],r[2],r[3],r[4]))
 local x0,y0,x1,y1=cut.width,cut.height,-1,-1
 for it in cut:pixels() do
  local v=it()
  if pc.rgbaA(v)>=180 then
   it(pc.rgba(pc.rgbaR(v),pc.rgbaG(v),pc.rgbaB(v),255));x0=math.min(x0,it.x);x1=math.max(x1,it.x);y0=math.min(y0,it.y);y1=math.max(y1,it.y)
  else it(0) end
 end
 assert(x1>x0 and y1>y0,'Missing component '..c.name)
 local clean=Image(cut,Rectangle(x0,y0,x1-x0+1,y1-y0+1))
 if c.size then clean:resize(c.size[1],c.size[2]) else clean=fit(clean,c.fit[1],c.fit[2]) end
 export(clean,c.name);ui[c.name]=clean
 componentSheet:drawImage(fit(clean,260,210),Point(20+((i-1)%3)*300,20+math.floor((i-1)/3)*250))
end
native(componentSheet,p.art..'/源文件/通用界面组件.aseprite','六个独立组件_对应单张PNG')
local star=Image{fromFile=p.stars};local half=math.floor(star.width/2)
ui.starOn=Image(star,Rectangle(0,0,half,star.height));ui.starOff=Image(star,Rectangle(half,0,half,star.height))
export(ui.starOn,'星级_亮');export(ui.starOff,'星级_暗')
local textAtlas=Image(1500,800,ColorMode.RGB)
local textIndex={}
for i,t in ipairs(p.texts) do
 local nw,nh=t.nativeSize[1],t.nativeSize[2];local pad=2
 local im=Image(nw+pad*2,nh+pad*2+1,ColorMode.RGB)
 if t.style=='heading' or t.style=='footer' then
  if t.style=='heading' then for _,v in ipairs(t.pixels) do local x=v%nw+pad;local y=math.floor(v/nw)+pad;im:drawPixel(x,y+2,cyan) end end
  for _,v in ipairs(t.pixels) do
   local x=v%nw+pad;local y=math.floor(v/nw)+pad
   for dy=-1,1 do for dx=-1,1 do im:drawPixel(x+dx,y+dy,dark) end end
  end
 end
 local ink=(t.style=='heading' or t.style=='ivory' or t.style=='footer') and ivory or dark
 for _,v in ipairs(t.pixels) do im:drawPixel(v%nw+pad,math.floor(v/nw)+pad,ink) end
 im:resize(im.width*t.scale,im.height*t.scale);im:saveAs(t.output);text[t.key]=im
 assert(im.width<=300 and im.height<=80,'Text atlas overflow '..t.key)
 local x=((i-1)%5)*300;local y=math.floor((i-1)/5)*80;textAtlas:drawImage(im,Point(x,y))
 textIndex[#textIndex+1]={key=t.key,text=t.text,file=t.output,rect={x,y,im.width,im.height}}
end
native(textAtlas,p.art..'/源文件/独立文字图集.aseprite','可独立替换的像素字图')
local thumbs={};local covers={}
for _,c in ipairs(p.chapters) do
 for _,l in ipairs(c.levels) do
  local map=Image{fromFile=l.map};local im=fit(map,300,174);im:saveAs(l.thumbnail);thumbs[l.id]=im
 end
 local source=c.levels[c.id==1 and 1 or #c.levels].map
 local im=fit(Image{fromFile=source},344,252);im:saveAs(c.cover);covers[c.id]=im
end
local function center(im,target,cx,y) target:drawImage(im,Point(math.floor(cx-im.width/2),y)) end
local function placeText(key,layer,cx,y) center(assert(text[key],key),layer,cx,y) end
local outputs={};local layerChecks={}
local function newScene(background)
 return {background=Image(assert(bgs[background])),frames=Image(W,H,ColorMode.RGB),thumbs=Image(W,H,ColorMode.RGB),text=Image(W,H,ColorMode.RGB),stars=Image(W,H,ColorMode.RGB),navigation=Image(W,H,ColorMode.RGB),paging=Image(W,H,ColorMode.RGB)}
end
local function topbar(scene,title,label,backLabel)
 scene.frames:drawImage(slice(ui['标题底板'],650,100,24),Point(475,56))
 placeText(title,scene.text,800,78)
 if label then placeText(label,scene.text,800,24) end
 scene.navigation:drawImage(ui['返回箭头'],Point(64,64));placeText(backLabel,scene.text,190,83)
end
local function paging(scene)
 scene.paging:drawImage(ui['返回箭头'],Point(630,816));scene.paging:drawImage(ui['下一页箭头'],Point(882,816));placeText('page',scene.paging,800,835)
end
local function saveScene(scene,filename,pageVisible)
 local s=Sprite(W,H,ColorMode.RGB);s:assignColorSpace(ColorSpace{sRGB=true})
 local flat=Image(W,H,ColorMode.RGB)
 local keys={'background','frames','thumbs','text','stars','navigation','paging'}
 local names={'组织背景_不含固定槽位','卡片与标题底板_可复用','地图缩略图_可替换','独立文字_可修改','星级_按记录替换','返回导航与编号徽标_独立','分页_扩展时启用'}
 for i,key in ipairs(keys) do
  local layer=i==1 and s.layers[1] or s:newLayer();layer.name=names[i]
  s:newCel(layer,1,scene[key],Point(0,0));layer.isVisible=key~='paging' or pageVisible==true
  if key~='paging' or pageVisible then flat:drawImage(scene[key],Point(0,0)) end
 end
 local imagePath=p.art..'/完整界面/'..filename..'.png';local sourcePath=p.art..'/源文件/'..filename..'.aseprite'
 flat:saveAs(imagePath);s:saveAs(sourcePath);s:close()
 outputs[#outputs+1]={name=filename,image=imagePath,source=sourcePath,layers=7,paginationVisible=pageVisible==true}
 images[#images+1]=flat
 local reopened=assert(app.open(sourcePath));assert(#reopened.layers==7);assert(reopened.layers[1]:cel(1).image:isEqual(scene.background));reopened:close()
 return flat
end
local cardRects={}
for _,c in ipairs(p.chapters) do
 local ck=string.format('chapter%d',c.id)
 local scene=newScene(c.background);topbar(scene,ck..'_title',ck..'_label','back_chapters')
 placeText('level_select',scene.text,800,170)
 local count=#c.levels;local columns=c.id==1 and 3 or 2;local w,h=380,270
 for i,l in ipairs(c.levels) do
  local row=math.floor((i-1)/columns);local col=(i-1)%columns
  local n=math.min(columns,count-row*columns);local gap=50
  local x=math.floor((W-(n*w+(n-1)*gap))/2)+col*(w+gap);local y=p.layout.levelRows[row+1]
  scene.frames:drawImage(slice(ui['卡片_常态'],w,h,24),Point(x,y));scene.thumbs:drawImage(thumbs[l.id],Point(x+40,y+12))
  scene.navigation:drawImage(slice(ui['编号底板'],82,32,12),Point(x+16,y+18))
  placeText(l.id..'_id',scene.navigation,x+57,y+19);placeText(l.id..'_name',scene.text,x+w/2,y+191)
  local off=fit(ui.starOff,24,24)
  for k=0,2 do scene.stars:drawImage(off,Point(x+145+k*33,y+234)) end
  cardRects[#cardRects+1]={id=l.id,rect={x,y,w,h},thumbnail=l.thumbnail,titleKey=l.id..'_name',idKey=l.id..'_id'}
  local card=Image(w,h,ColorMode.RGB);card:drawImage(slice(ui['卡片_常态'],w,h,24),Point(0,0));card:drawImage(thumbs[l.id],Point(40,12))
  card:drawImage(slice(ui['编号底板'],82,32,12),Point(16,18));center(text[l.id..'_id'],card,57,19);center(text[l.id..'_name'],card,w/2,191)
  for k=0,2 do card:drawImage(off,Point(145+k*33,234)) end
  card:saveAs(l.card)
 end
 placeText('level_hint',scene.text,800,835);paging(scene)
 saveScene(scene,string.format('0%d_第%d章_%s_选关',c.id,c.id,c.name),false)
end
local scene=newScene('chapters');topbar(scene,'chapter_select',nil,'back_home')
for i,c in ipairs(p.chapters) do
 local ck=string.format('chapter%d',c.id)
 local x=p.layout.chapterPositions[i];local y=220;local w,h=400,520
 scene.frames:drawImage(slice(ui['卡片_常态'],w,h,24),Point(x,y))
 scene.thumbs:drawImage(covers[c.id],Point(x+28,y+28))
 placeText(ck..'_label',scene.text,x+w/2,y+294)
 placeText(ck..'_cardtitle',scene.text,x+w/2,y+340)
 placeText(ck..'_tagline',scene.text,x+w/2,y+411)
 scene.frames:drawImage(slice(ui['编号底板'],120,38,12),Point(x+140,y+458))
 local count=text[ck..'_count'];local bright=Image(count)
 for it in bright:pixels() do local v=it();if pc.rgbaA(v)>0 then it(ivory) end end
 center(bright,scene.text,x+w/2,y+462)
end
placeText('chapter_hint',scene.text,800,816);paging(scene)
saveScene(scene,'04_章节选择',false)
local overview=Image(1648,948,ColorMode.RGB)
for it in overview:pixels() do it(dark) end
for i,im in ipairs(images) do local cp=Image(im);cp:resize(800,450);overview:drawImage(cp,Point(16+((i-1)%2)*816,16+math.floor((i-1)/2)*466)) end
overview:saveAs(p.art..'/四个界面总览.png');native(overview,p.art..'/预览~/四界面总览.aseprite','三张选关与一张选章')
-- A separate art-only next-page sample; no promised fourth chapter or unlock rule.
local extension=newScene('chapters');topbar(extension,'chapter_select',nil,'back_home')
extension.frames:drawImage(slice(ui['卡片_常态'],400,520,24),Point(600,220));placeText('more_title',extension.text,800,412);placeText('more_body',extension.text,800,490);paging(extension)
local ext=Image(extension.background)
for _,key in ipairs({'frames','text','navigation','paging'}) do ext:drawImage(extension[key],Point(0,0)) end
ext:saveAs(p.art..'/预览~/未来章节分页示意.png')
local log=assert(io.open(p.art..'/制作记录~/Aseprite导出清单.json','w'));log:write(json.encode({screens=outputs,components=exports,textSlices=textIndex,cards=cardRects}));log:close()
print('PASS: 4 full menus, 13 true-map thumbnails, separate frame/text/star/nav assets, 7-layer editable screens, and optional pagination art exported')
