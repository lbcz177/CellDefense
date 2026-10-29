local f=assert(io.open(app.params.plan,'r'));local p=json.decode(f:read('*a'));f:close()
local pc=app.pixelColor;local raw=Image{fromFile=p.source};local icons={};local selected={};local manifests={}
local dark=pc.rgba(57,21,54,255);local cyan=pc.rgba(31,218,225,255)
local function fit(im,w,h,inset)
 inset=inset or 0;local ratio=math.min((w-2*inset)/im.width,(h-2*inset)/im.height);local cp=Image(im);cp:resize(math.floor(im.width*ratio+.5),math.floor(im.height*ratio+.5))
 local out=Image(w,h,ColorMode.RGB);out:drawImage(cp,Point(math.floor((w-cp.width)/2),math.floor((h-cp.height)/2)));return out
end
local function outline(im)
 local w,h=im.width,im.height;local dist={};local queue={};local head=1
 local function key(x,y)return y*w+x end
 for y=0,h-1 do for x=0,w-1 do if pc.rgbaA(im:getPixel(x,y))>0 then dist[key(x,y)]=0;queue[#queue+1]={x,y} end end end
 while head<=#queue do
  local cell=queue[head];head=head+1;local x,y=cell[1],cell[2];local d=dist[key(x,y)]
  if d<7 then for _,off in ipairs({{-1,0},{1,0},{0,-1},{0,1}}) do
   local nx,ny=x+off[1],y+off[2]
   if nx>=0 and ny>=0 and nx<w and ny<h and dist[key(nx,ny)]==nil then dist[key(nx,ny)]=d+1;queue[#queue+1]={nx,ny} end
  end end
 end
 local ring=Image(w,h,ColorMode.RGB)
 for y=0,h-1 do for x=0,w-1 do local d=dist[key(x,y)];if d and d>0 then ring:drawPixel(x,y,d<=5 and cyan or dark) end end end
 local active=Image(ring);active:drawImage(im,Point(0,0));return ring,active
end
for i=1,6 do
 local cw,ch=raw.width/3,raw.height/2;local x=math.floor(((i-1)%3)*cw);local y=math.floor(math.floor((i-1)/3)*ch)
 local cell=Image(raw,Rectangle(x,y,math.floor(cw),math.floor(ch)));local x0,y0,x1,y1=cell.width,cell.height,-1,-1
 for it in cell:pixels() do local v=it();if pc.rgbaA(v)>=160 then
  it(pc.rgba(pc.rgbaR(v),pc.rgbaG(v),pc.rgbaB(v),255));x0=math.min(x0,it.x);y0=math.min(y0,it.y);x1=math.max(x1,it.x);y1=math.max(y1,it.y)
 else it(0) end end
 assert(x1>x0 and y1>y0,'Empty badge cell')
 local im=fit(Image(cell,Rectangle(x0,y0,x1-x0+1,y1-y0+1)),256,256,12)
 local ring,active=outline(im);local name=i<=5 and string.format('%02d_关卡%d',i,i) or '06_空白扩展徽章'
 local normalPath=p.art..'/徽章/'..name..'_常态.png';local selectedPath=p.art..'/徽章/'..name..'_选中.png'
 im:saveAs(normalPath);active:saveAs(selectedPath);icons[i]=im;selected[i]=active
 local source=Sprite(256,256,ColorMode.RGB);source:assignColorSpace(ColorSpace{sRGB=true});source.layers[1].name='细胞徽章与数字_常态';source:newCel(source.layers[1],1,im,Point(0,0))
 local halo=source:newLayer();halo.name='选中描边_可开关';source:newCel(halo,1,ring,Point(0,0));halo.isVisible=false
 local native=p.art..'/源文件/'..name..'.aseprite';source:saveAs(native);source:close()
 manifests[#manifests+1]={index=i,normal=normalPath,selected=selectedPath,source=native,size={256,256}}
end
local old=p.previous;local texts={};for _,t in ipairs(p.oldTexts) do texts[t.key]=Image{fromFile=t.output} end
local v2=p.previousV2;local frame=Image{fromFile=old..'/组件/标题底板.png'};local back=Image{fromFile=old..'/组件/返回箭头.png'};local star=Image{fromFile=old..'/组件/星级_暗.png'};local hint=Image{fromFile=v2..'/文字/hint.png'}
local function slice(im,w,h,b)
 local out=Image(w,h,ColorMode.RGB);local sx={0,b,im.width-b};local sy={0,b,im.height-b};local sw={b,im.width-2*b,b};local sh={b,im.height-2*b,b};local dx={0,b,w-b};local dy={0,b,h-b};local dw={b,w-2*b,b};local dh={b,h-2*b,b}
 for y=1,3 do for x=1,3 do local t=Image(im,Rectangle(sx[x],sy[y],sw[x],sh[y]));t:resize(dw[x],dh[y]);out:drawImage(t,Point(dx[x],dy[y])) end end;return out
end
local function center(im,dst,cx,y)dst:drawImage(im,Point(math.floor(cx-im.width/2),y)) end
local pages={}
for _,c in ipairs(p.chapters) do
 local bg=Image{fromFile=p.backgrounds[c.background]};local controls=Image(1600,900,ColorMode.RGB);local badges=Image(1600,900,ColorMode.RGB);local ratings=Image(1600,900,ColorMode.RGB)
 controls:drawImage(slice(frame,650,100,24),Point(475,56));center(texts[string.format('chapter%d_title',c.id)],controls,800,78);center(texts[string.format('chapter%d_label',c.id)],controls,800,24)
 controls:drawImage(back,Point(64,64));center(texts.back_chapters,controls,190,83);center(texts.level_select,controls,800,172);center(hint,controls,800,664)
 local n=#c.levels;local pitch=n==5 and 260 or 300;local left=(1600-(n-1)*pitch)/2
 for i=1,n do
  local cx=left+(i-1)*pitch;center(fit(icons[i],200,200),badges,cx,340)
  for j=0,2 do ratings:drawImage(fit(star,32,32),Point(cx-56+j*40,552)) end
 end
 local flat=Image(bg);for _,im in ipairs({controls,badges,ratings}) do flat:drawImage(im,Point(0,0)) end
 local name=string.format('0%d_第%d章_细胞徽章选关',c.id,c.id);flat:saveAs(p.art..'/完整界面/'..name..'.png')
 local s=Sprite(1600,900,ColorMode.RGB);for i,im in ipairs({bg,controls,badges,ratings}) do local layer=i==1 and s.layers[1] or s:newLayer();layer.name=({'组织背景','标题与导航','独立数字徽章_可扩展','动态星级_示意'})[i];s:newCel(layer,1,im,Point(0,0)) end
 s:saveAs(p.art..'/源文件/'..name..'.aseprite');s:close();pages[#pages+1]=flat
end
local sheet=Image(1600,620,ColorMode.RGB);for it in sheet:pixels() do it(pc.rgba(255,245,223,255)) end
for i=1,5 do center(icons[i],sheet,280+(i-1)*260,20);center(selected[i],sheet,280+(i-1)*260,326) end
sheet:saveAs(p.art..'/五枚徽章_常态与选中.png')
local overview=Image(1648,948,ColorMode.RGB);for it in overview:pixels() do it(dark) end
for i,im in ipairs(pages) do local cp=Image(im);cp:resize(800,450);overview:drawImage(cp,Point(16+((i-1)%2)*816,16+math.floor((i-1)/2)*466)) end
local fourth=Image(800,450,ColorMode.RGB);for it in fourth:pixels() do it(pc.rgba(255,245,223,255)) end
for i=1,5 do fourth:drawImage(fit(icons[i],148,148),Point(12+(i-1)*156,40));fourth:drawImage(fit(selected[i],148,148),Point(12+(i-1)*156,250)) end
overview:drawImage(fourth,Point(832,482));overview:saveAs(p.art..'/细胞徽章选关总览.png')
local out=assert(io.open(p.art..'/制作记录~/徽章导出清单.json','w'));out:write(json.encode(manifests));out:close()
print('PASS: 5 illustrated number badges plus extensible blank, 6 selected states and native sources, 3 revised chapter menus exported')
