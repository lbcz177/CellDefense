local f=assert(io.open(app.params['plan'],'r'));local plan=json.decode(f:read('*a'));f:close()
local art=plan.art;local pc=app.pixelColor
local dark=pc.rgba(49,27,52,255);local cream=pc.rgba(255,246,215,255);local gold=pc.rgba(255,218,155,255)
local glyphs={};local lookup={}
for i,g in ipairs(plan.glyphs) do
 local tile=Image(18,18,ColorMode.RGB)
 for _,v in ipairs(g.pixels) do
  local x=v%18;local y=math.floor(v/18)
  for _,d in ipairs({{-1,0},{1,0},{0,-1},{0,1},{1,1}}) do
   local a,b=x+d[1],y+d[2];if a>=0 and a<18 and b>=0 and b<18 then tile:drawPixel(a,b,dark) end
  end
 end
 for _,v in ipairs(g.pixels) do local x=v%18;local y=math.floor(v/18);tile:drawPixel(x,y,y>=11 and gold or cream) end
 glyphs[i]=tile;lookup[g.code]=i
end
local atlas=Image(plan.glyphWidth,plan.glyphHeight,ColorMode.RGB)
for i,tile in ipairs(glyphs) do atlas:drawImage(tile,Point(((i-1)%plan.columns)*18,math.floor((i-1)/plan.columns)*18)) end
atlas:saveAs(art..'/字图/提示像素字图.png')
local phrases=Image(plan.phraseWidth,plan.phraseHeight,ColorMode.RGB)
for _,entry in ipairs(plan.phrases) do
 local phrase=Image(entry.width,entry.height,ColorMode.RGB)
 for lineIndex,line in ipairs(entry.lines) do
  local x=0
  for _,code in utf8.codes(line) do
   local index=assert(lookup[code],tostring(code));phrase:drawImage(glyphs[index],Point(x,(lineIndex-1)*16))
   x=x+plan.glyphs[index].advance
  end
 end
 phrases:drawImage(phrase,Point(entry.x,entry.y))
 phrase:saveAs(art..'/制作记录~/原始生成/'..entry.name..'.png')
end
phrases:saveAs(art..'/固定文案/固定提示文案图集.png')
local original=Image{fromFile=art..'/制作记录~/原始生成/提示面板_imagegen.png'}
local x0,y0,x1,y1=original.width,original.height,-1,-1
for it in original:pixels() do if pc.rgbaA(it())>=128 then x0=math.min(x0,it.x);x1=math.max(x1,it.x);y0=math.min(y0,it.y);y1=math.max(y1,it.y) end end
local cropped=Image(original,Rectangle(x0,y0,x1-x0+1,y1-y0+1));cropped:resize(256,96)
local colors={{48,36,56},{56,46,69},{250,126,134},{217,93,113},{255,232,189},{98,212,230},{123,57,82}}
local panel=Image(256,96,ColorMode.RGB)
for it in cropped:pixels() do
 local p=it();if pc.rgbaA(p)>=128 then
  local best,score=1,math.huge
  for i,c in ipairs(colors) do local s=(pc.rgbaR(p)-c[1])^2+(pc.rgbaG(p)-c[2])^2+(pc.rgbaB(p)-c[3])^2;if s<score then best=i;score=s end end
  local c=colors[best]
  panel:drawPixel(it.x,it.y,pc.rgba(c[1],c[2],c[3],255))
 end
end
for y=14,81 do for x=14,241 do panel:drawPixel(x,y,pc.rgba(48,36,56,255)) end end
panel:saveAs(art..'/面板/01_引导与提示面板.png')
local enemy=Image(panel)
for it in enemy:pixels() do local p=it();if pc.rgbaR(p)==250 and pc.rgbaG(p)==126 then it(pc.rgba(184,105,166,255))
 elseif pc.rgbaR(p)==98 and pc.rgbaG(p)==212 then it(pc.rgba(255,209,102,255)) end end
enemy:saveAs(art..'/面板/02_怪物出现面板.png')
local function button(selected)
 local im=Image(96,24,ColorMode.RGB)
 for y=0,23 do for x=0,95 do
  local corner=(x<3 or x>92) and (y<3 or y>20)
  if not corner then
   local edge=x<=1 or x>=94 or y<=1 or y>=22
   local shade=y>=20
   local c=edge and dark or shade and pc.rgba(217,93,113,255) or selected and pc.rgba(44,164,207,255) or pc.rgba(83,52,77,255)
   im:drawPixel(x,y,c)
  end
 end end
 for x=5,90 do im:drawPixel(x,2,selected and pc.rgba(151,240,244,255) or pc.rgba(250,126,134,255)) end
 return im
end
button(false):saveAs(art..'/面板/03_提示按钮_常态.png')
button(true):saveAs(art..'/面板/04_提示按钮_高亮.png')
local source=Sprite(plan.phraseWidth,plan.phraseHeight,ColorMode.RGB)
source.cels[1].image=phrases;source.layers[1].name='固定文案：逐条图像，切片见处理计划'
source:saveAs(art..'/制作记录~/固定提示文案.aseprite');source:close()
source=Sprite(256,96,ColorMode.RGB);source.cels[1].image=panel;source.layers[1].name='提示面板'
source:saveAs(art..'/制作记录~/提示面板.aseprite');source:close()
print('COMPLETE '..#glyphs..' glyph images, '..#plan.phrases..' phrase sprites and 4 panel/button images')
