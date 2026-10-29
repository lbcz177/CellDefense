local f=assert(io.open(app.params.plan,'r'));local p=json.decode(f:read('*a'));f:close()
local pc=app.pixelColor;local W,H=1600,900;local dark=pc.rgba(57,21,54,255);local cream=pc.rgba(255,246,216,255)
local function fit(im,w,h)
 local cp=Image(im);local ratio=math.min(w/im.width,h/im.height);cp:resize(math.floor(im.width*ratio+.5),math.floor(im.height*ratio+.5))
 local out=Image(w,h,ColorMode.RGB);out:drawImage(cp,Point(math.floor((w-cp.width)/2),math.floor((h-cp.height)/2)));return out
end
local function slice(im,w,h,b)
 local out=Image(w,h,ColorMode.RGB);local sx={0,b,im.width-b};local sy={0,b,im.height-b};local sw={b,im.width-2*b,b};local sh={b,im.height-2*b,b}
 local dx={0,b,w-b};local dy={0,b,h-b};local dw={b,w-2*b,b};local dh={b,h-2*b,b}
 for y=1,3 do for x=1,3 do local t=Image(im,Rectangle(sx[x],sy[y],sw[x],sh[y]));t:resize(dw[x],dh[y]);out:drawImage(t,Point(dx[x],dy[y])) end end
 return out
end
local function center(im,dst,cx,y) dst:drawImage(im,Point(math.floor(cx-im.width/2),y)) end
local function load(path)return Image{fromFile=path} end
local ui={};for _,name in ipairs({'卡片_常态','卡片_高亮','标题底板','返回箭头','下一页箭头','星级_亮','星级_暗'}) do ui[name]=load(p.previous..'/组件/'..name..'.png') end
local function export(im,name) im:saveAs(p.art..'/组件/'..name..'.png') end
export(slice(ui['卡片_常态'],170,170,24),'序号按钮_常态');export(slice(ui['卡片_高亮'],170,170,24),'序号按钮_高亮')
export(ui['卡片_常态'],'详情底板');export(ui['卡片_常态'],'操作按钮_常态');export(ui['卡片_高亮'],'操作按钮_高亮')
local text={};for _,t in ipairs(p.oldTexts) do text[t.key]=load(t.output) end
for _,t in ipairs(p.texts) do
 local nw,nh=t.nativeSize[1],t.nativeSize[2];local im=Image(nw+4,nh+5,ColorMode.RGB)
 if t.style=='footer' then for _,v in ipairs(t.pixels) do local x,y=v%nw+2,math.floor(v/nw)+2;for dy=-1,1 do for dx=-1,1 do im:drawPixel(x+dx,y+dy,dark) end end end end
 for _,v in ipairs(t.pixels) do im:drawPixel(v%nw+2,math.floor(v/nw)+2,t.style=='footer' and cream or dark) end
 im:resize(im.width*t.scale,im.height*t.scale);im:saveAs(t.output);text[t.key]=im
end
local previews={};local exported={};local selectors={}
local function save(layers,names,file)
 local s=Sprite(W,H,ColorMode.RGB);s:assignColorSpace(ColorSpace{sRGB=true});local flat=Image(W,H,ColorMode.RGB)
 for i,im in ipairs(layers) do local layer=i==1 and s.layers[1] or s:newLayer();layer.name=names[i];s:newCel(layer,1,im,Point(0,0));flat:drawImage(im,Point(0,0)) end
 flat:saveAs(p.art..'/完整界面/'..file..'.png');s:saveAs(p.art..'/源文件/'..file..'.aseprite');s:close()
 exported[#exported+1]={file=file,layers=#layers};return flat
end
local function image()return Image(W,H,ColorMode.RGB) end
local function title(frame,labels,c)
 frame:drawImage(slice(ui['标题底板'],650,100,24),Point(475,56));center(text[string.format('chapter%d_title',c.id)],labels,800,78);center(text[string.format('chapter%d_label',c.id)],labels,800,24)
 center(text.level_select,labels,800,172);frame:drawImage(ui['返回箭头'],Point(64,64));center(text.back_chapters,labels,190,83)
end
for _,c in ipairs(p.chapters) do
 local bg=load(p.backgrounds[c.background]);local frame,labels,stars=image(),image(),image();title(frame,labels,c)
 local n=#c.levels;local pitch=n==5 and 260 or 300;local left=(W-(n-1)*pitch)/2
 for i=1,n do
  local cx=left+(i-1)*pitch;frame:drawImage(slice(ui['卡片_常态'],170,170,24),Point(cx-85,360))
  local num=text['number'..string.format('%d',i)];center(num,labels,cx,360+math.floor((170-num.height)/2))
  for k=0,2 do stars:drawImage(fit(ui['星级_暗'],32,32),Point(cx-56+k*40,552)) end
 end
 center(text.hint,labels,800,664)
 local name=string.format('0%d_第%d章_序号选关',c.id,c.id)
 local flat=save({bg,frame,labels,stars},{'组织背景_不带关卡内容','序号按钮_可增加与分页','序号与文字_独立','星级_读取实际记录'},name)
 selectors[c.id]=flat;previews[#previews+1]=flat
end
for _,c in ipairs(p.chapters) do
 local bg=Image(selectors[c.id]);local shade=image();for it in shade:pixels() do it(pc.rgba(42,13,40,160)) end
 local frame,visual,labels,states=image(),image(),image(),image()
 frame:drawImage(slice(ui['卡片_常态'],1140,590,24),Point(230,155))
 center(text[c.levels[1].id..'_detailtitle'],labels,800,184)
 local map=fit(load(c.levels[1].map),560,365);visual:drawImage(map,Point(280,264))
 center(text.history,labels,1080,294);center(text.sample_score,states,1080,356)
 center(text.best_stars,labels,1080,474)
 for k=0,2 do states:drawImage(fit(ui['星级_亮'],40,40),Point(1008+k*52,524)) end
 frame:drawImage(slice(ui['卡片_常态'],260,70,24),Point(450,650));frame:drawImage(slice(ui['卡片_高亮'],260,70,24),Point(890,650))
 center(text['return'],labels,580,663);center(text.start,labels,1020,663)
 local flat=save({bg,shade,frame,visual,labels,states},{'序号页背景','模态遮罩_阻挡后层','详情底板与操作按钮','地图_仅详情显示','固定文字与关卡名','历史最高分与最佳星级_动态示例'},string.format('0%d_第%d章_点击后详情示意',c.id+3,c.id))
 if c.id==1 then
  previews[#previews+1]=flat
  local empty=image();center(text.unplayed,empty,1080,372)
  for k=0,2 do empty:drawImage(fit(ui['星级_暗'],40,40),Point(1008+k*52,524)) end
  save({bg,shade,frame,visual,labels,empty},{'序号页背景','模态遮罩','详情底板','地图','固定文字','未通关_不伪造零分记录'},'07_详情_未通关示意')
 end
end
local sheet=Image(1648,948,ColorMode.RGB);for it in sheet:pixels() do it(dark) end
for i,im in ipairs(previews) do local cp=Image(im);cp:resize(800,450);sheet:drawImage(cp,Point(16+((i-1)%2)*816,16+math.floor((i-1)/2)*466)) end
sheet:saveAs(p.art..'/序号选关与详情总览.png')
local out=assert(io.open(p.art..'/制作记录~/Aseprite导出清单.json','w'));out:write(json.encode(exported));out:close()
print('PASS: 3 number-and-star overview screens, 3 map/history detail examples and 1 unplayed state, with editable sources')
