-- ImageGen artwork normalization and paired UI states. Executed inside Aseprite.
local root=assert(app.params.root)
local pc=app.pixelColor
local W,H=128,64
local C={outline=pc.rgba(50,22,50,255),face=pc.rgba(255,243,204,255),highlight=pc.rgba(255,253,233,255),bevel=pc.rgba(245,126,133,255),cyan=pc.rgba(18,182,236,255),cyanLight=pc.rgba(103,232,245,255),blue=pc.rgba(25,120,186,255)}
local function image() return Image(W,H,ColorMode.RGB) end
local function opaque(p) return pc.rgbaA(p)>0 end
local function bright(p)
 return pc.rgbaA(p)>180 and pc.rgbaR(p)>175 and pc.rgbaG(p)>90 and pc.rgbaB(p)>65
end
local src=Image{fromFile=root..'/制作记录~/原始生成图/pause.png'}
local left,top,right,bottom=src.width,src.height,-1,-1
local row={}
for y=0,src.height-1 do
 local lo,hi=src.width,-1
 for x=0,src.width-1 do
  if bright(src:getPixel(x,y)) then lo=math.min(lo,x);hi=math.max(hi,x) end
 end
 if hi-lo>src.width*.35 then
  row[y]={lo,hi};left=math.min(left,lo);right=math.max(right,hi);top=math.min(top,y);bottom=math.max(bottom,y)
 end
end
assert(right>left and bottom>top,'Empty button face')
local face=image()
for y=8,55 do
 local sy=math.min(bottom,top+math.floor((y-8+.5)*(bottom-top+1)/48))
 local rr=row[sy]
 if rr then
  for x=8,119 do
   local sx=left+math.floor((x-8+.5)*(right-left+1)/112)
   if sx>=rr[1] and sx<=rr[2] then
    local p=src:getPixel(sx,sy)
    local c=C.face
    if pc.rgbaR(p)>175 and pc.rgbaG(p)<175 then c=C.bevel
    elseif pc.rgbaG(p)>242 and pc.rgbaB(p)>210 then c=C.highlight end
    face:drawPixel(x,y,c)
   end
  end
 end
end
local plate=image()
-- Four logical pixels of consistent solid outline, no semitransparent halo.
for y=0,H-1 do for x=0,W-1 do
 if opaque(face:getPixel(x,y)) then
  for dy=-3,3 do for dx=-3,3 do
   if x+dx>=0 and x+dx<W and y+dy>=0 and y+dy<H then plate:drawPixel(x+dx,y+dy,C.outline) end
  end end
 end
end end
plate:drawImage(face)
local selectedPlate=image()
for it in selectedPlate:pixels() do
 local p=plate:getPixel(it.x,it.y)
 if p==C.face then p=C.cyan elseif p==C.highlight then p=C.cyanLight elseif p==C.bevel then p=C.blue end
 it(p)
end
local function glyph(name)
 local source=Image{fromFile=root..'/制作记录~/原始生成图/'..name..'.png'}
 local l,t,r,b=source.width,source.height,-1,-1
 local x0,x1=math.floor(source.width*.28),math.floor(source.width*.73)
 local y0,y1=math.floor(source.height*.31),math.floor(source.height*.68)
 local function dark(p) return pc.rgbaA(p)>200 and pc.rgbaR(p)<105 and pc.rgbaG(p)<75 and pc.rgbaB(p)<115 end
 for y=y0,y1 do for x=x0,x1 do
  if dark(source:getPixel(x,y)) then l=math.min(l,x);r=math.max(r,x);t=math.min(t,y);b=math.max(b,y) end
 end end
 assert(r>=l and b>=t,'Missing glyph '..name)
 local gh=28;local gw=math.floor((r-l+1)*gh/(b-t+1)+.5)
 local out=image();local ox=math.floor((W-gw)/2);local oy=math.floor((H-gh)/2)-1
 for y=0,gh-1 do for x=0,gw-1 do
  local sx=math.min(r,l+math.floor((x+.5)*(r-l+1)/gw))
  local sy=math.min(b,t+math.floor((y+.5)*(b-t+1)/gh))
  if dark(source:getPixel(sx,sy)) then out:drawPixel(ox+x,oy+y,C.outline) end
 end end
 return out,{x=ox,y=oy,w=gw,h=gh}
end
local function highlightedSymbol(symbol)
 local out=image()
 for y=1,H-2 do for x=1,W-2 do
  if opaque(symbol:getPixel(x,y)) then
   for dy=-1,1 do for dx=-1,1 do out:drawPixel(x+dx,y+dy,C.outline) end end
  end
 end end
 for it in symbol:pixels() do if opaque(it()) then out:drawPixel(it.x,it.y,C.face) end end
 return out
end
local labels={{id='pause',name='暂停'},{id='speed1',name='正常速度_1x'},{id='speed2',name='双倍速度_2x'}}
local overview=Sprite(432,164,ColorMode.RGB)
overview.layers[1].name='背景'
local bg=Image(432,164,ColorMode.RGB);bg:clear(pc.rgba(41,32,52,255));overview:newCel(overview.layers[1],1,bg)
local sheet=Image(384,128,ColorMode.RGB)
local report={width=W,height=H,alpha='binary 0/255',source='built-in image_gen',normalization='Aseprite Lua; shared frame geometry, nearest-neighbor glyphs, palette cleanup',assets={}}
for i,job in ipairs(labels) do
 local normalGlyph,bounds=glyph(job.id)
 local selectedGlyph=highlightedSymbol(normalGlyph)
 local s=Sprite(W,H,ColorMode.RGB);s:assignColorSpace(ColorSpace{sRGB=true})
 local frame=s.layers[1];frame.name='按钮底板'
 local icon=s:newLayer();icon.name='符号'
 s:newCel(frame,1,plate);s:newCel(icon,1,normalGlyph)
 s:newEmptyFrame(2);s:newCel(frame,2,selectedPlate);s:newCel(icon,2,selectedGlyph)
 s.frames[1].duration=1;s.frames[2].duration=1
 local a=s:newTag(1,1);a.name='Normal';local btag=s:newTag(2,2);btag.name='Selected'
 local files={}
 for state=1,2 do
  local composed=image();composed:drawSprite(s,state)
  local filename=string.format('%02d_',i)..job.name..(state==1 and '_常态.png' or '_高亮.png')
  composed:saveAs(root..'/'..filename);files[state]=filename
  sheet:drawImage(composed,Point((i-1)*W,(state-1)*H))
  local layer=overview:newLayer();layer.name=job.name..(state==1 and '_常态' or '_高亮')
  overview:newCel(layer,1,composed,Point(12+(i-1)*140,12+(state-1)*76))
 end
 s:saveAs(root..'/Aseprite源文件/'..job.name..'.aseprite')
 local verify=app.open(root..'/Aseprite源文件/'..job.name..'.aseprite')
 assert(#verify.frames==2 and #verify.layers==2,'Source layers missing')
 for state=1,2 do
  local v=image();v:drawSprite(verify,state)
  local original=Image{fromFile=root..'/'..files[state]}
  assert(v:isEqual(original),'Source/export mismatch')
 end
 verify:close();s:close()
 table.insert(report.assets,{id=job.id,files=files,glyphBounds=bounds,source='Aseprite源文件/'..job.name..'.aseprite'})
end
sheet:saveAs(root..'/制作记录~/按钮图集_3列2行.png')
overview:saveAs(root..'/Aseprite源文件/六态展示.aseprite')
local preview=Image(432,164,ColorMode.RGB);preview:drawSprite(overview,1)
preview:resize(1296,492);preview:saveAs(root..'/按钮展示预览.png')
local f=assert(io.open(root..'/制作记录~/Aseprite检查.json','w'));f:write(json.encode(report));f:close()
overview:close()
print('OK: six 128x64 PNGs, three two-layer/two-state sources, layered overview, export round-trip verified.')