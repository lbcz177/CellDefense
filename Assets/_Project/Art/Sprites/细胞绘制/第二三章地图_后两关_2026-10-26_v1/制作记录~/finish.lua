local f=assert(io.open(app.params.plan,'r'));local p=json.decode(f:read('*a'));f:close()
local pc=app.pixelColor;local previews={};local results={}
local function saveSource(sprite,file) sprite:saveAs(file);sprite:close() end
for _,m in ipairs(p.maps) do
 local base=Image{fromFile=m.image};local width,height=base.width,base.height
 local sprite=Sprite(width,height,ColorMode.RGB);sprite:assignColorSpace(ColorSpace{sRGB=true})
 sprite.layers[1].name='地图基底_原始生成';sprite:newCel(sprite.layers[1],1,base,Point(0,0))
 local composite=Image(base)
 for _,z in ipairs(m.zones) do
  local allowed={};for _,c in ipairs(z.cells) do allowed[string.format('%d,%d',c[1],c[2])]=true end
  local step=4;local gw=math.ceil(width/step);local gh=math.ceil(height/step)
  local mask={};local cx,cy,rx,ry=z.center[1],z.center[2],z.radius[1],z.radius[2]
  local maskPixels=0
  for y=0,gh-1 do mask[y]={};for x=0,gw-1 do
   local wx=(x*step+step*.5)/width*15;local wy=11-(y*step+step*.5)/height*11
   local smooth=(math.abs((wx-cx)/rx)^4+math.abs((wy-cy)/ry)^4)<=1
   mask[y][x]=smooth and allowed[string.format('%d,%d',math.floor(wx),math.floor(wy))]==true
   if mask[y][x] then maskPixels=maskPixels+1 end
  end end
  assert(maskPixels>100,'Empty region mask: '..m.id..z.key)
  local function inside(x,y) return y>=0 and y<gh and x>=0 and x<gw and mask[y][x] end
  local overlay=Image(width,height,ColorMode.RGB)
  for y=0,gh-1 do for x=0,gw-1 do if inside(x,y) then
   local edge=not inside(x-1,y) or not inside(x+1,y) or not inside(x,y-1) or not inside(x,y+1)
   local bright=((x+y)%11)<7;local color=pc.rgba(255,138,61,32)
   if edge and bright then color=pc.rgba(255,211,112,225)
   elseif edge then color=pc.rgba(255,155,66,110) end
   for dy=0,step-1 do for dx=0,step-1 do
    local px,py=x*step+dx,y*step+dy
    if px<width and py<height then overlay:drawPixel(px,py,color) end
   end end
  end end end
  overlay:saveAs(z.overlay)
  local layer=sprite:newLayer();layer.name='炎症范围_'..z.key..'_独立可关闭';sprite:newCel(layer,1,overlay,Point(0,0))
  composite:drawImage(overlay,Point(0,0))
  results[#results+1]={id=m.id,key=z.key,overlay=z.overlay,logicalCells=#z.cells,maskPixels=maskPixels,state='low-opacity independent region boundary; gameplay not implemented'}
 end
 if #m.zones>0 then composite:saveAs(p.art..'/预览~/'..m.id..'_炎症范围叠加预览.png') end
 saveSource(sprite,m.image:gsub('%.png$','.aseprite'))
 previews[#previews+1]=composite
end
local sheet=Image(1500,1122,ColorMode.RGB)
for it in sheet:pixels() do it(pc.rgba(56,38,54,255)) end
for i,im in ipairs(previews) do
 local copy=Image(im);copy:resize(720,528)
 sheet:drawImage(copy,Point(20+((i-1)%2)*740,20+math.floor((i-1)/2)*554))
end
sheet:saveAs(p.art..'/四张地图总览.png')
local sheetSource=Sprite(sheet.width,sheet.height,ColorMode.RGB);sheetSource:newCel(sheetSource.layers[1],1,sheet,Point(0,0))
saveSource(sheetSource,p.art..'/预览~/四图总览.aseprite')
local out=assert(io.open(p.art..'/制作记录~/叠层导出记录.json','w'));out:write(json.encode(results));out:close()
for _,m in ipairs(p.maps) do
 local source=assert(app.open(m.image:gsub('%.png$','.aseprite')))
 assert(source.layers[1]:cel(1).image:isEqual(Image{fromFile=m.image}),'Changed original base: '..m.id)
 assert(#source.layers==#m.zones+1,'Unexpected layer count: '..m.id)
 source:close()
end
print('PASS: four native sources preserve exact base PNGs; three independent region overlays and overview exported')
