local f=assert(io.open(app.params.plan,'r'));local p=json.decode(f:read('*a'));f:close()
local pc=app.pixelColor
local colors={body=pc.rgba(83,47,62,255),heading=pc.rgba(73,38,55,255),accent=pc.rgba(36,118,126,255),muted=pc.rgba(140,100,108,255)}
local textMap={};for _,t in ipairs(p.texts) do textMap[t.key]=t end
local function drawText(image,key,x,y,color)
 local t=assert(textMap[key],key);local scale=t.scale
 for _,idx in ipairs(t.pixels) do
  local px=x+(idx%t.width)*scale;local py=y+math.floor(idx/t.width)*scale
  for dy=0,scale-1 do for dx=0,scale-1 do image:drawPixel(px+dx,py+dy,color) end end
 end
end
local raw={};for _,j in ipairs(p.jobs) do raw[j.id]=j.raw end
local frame=Image{fromFile=raw.template};frame:resize(1280,720)
frame:saveAs(p.art..'/组件/图鉴底板.png')
for name,key in pairs(p.navigation) do
 local t=textMap[key];local im=Image(t.width*t.scale+8,t.height*t.scale+8,ColorMode.RGB)
 drawText(im,key,4,4,colors.heading);im:saveAs(p.art..'/文字/'..name..'.png')
end
local portraits={};local pageImages={}
for _,e in ipairs(p.entries) do
 local source=Image{fromFile=raw[e.id]};local minx,miny,maxx,maxy=source.width,source.height,0,0
 for y=0,source.height-1 do for x=0,source.width-1 do if pc.rgbaA(source:getPixel(x,y))>=128 then
  minx=math.min(minx,x);miny=math.min(miny,y);maxx=math.max(maxx,x);maxy=math.max(maxy,y)
 end end end
 assert(maxx>minx and maxy>miny,'Empty portrait '..e.id)
 local crop=Image(maxx-minx+1,maxy-miny+1,ColorMode.RGB);crop:drawImage(source,Point(-minx,-miny))
 local scale=328/math.max(crop.width,crop.height)
 crop:resize(math.floor(crop.width*scale+.5),math.floor(crop.height*scale+.5))
 for it in crop:pixels() do local c=it();if pc.rgbaA(c)<128 then it(0) else it(pc.rgba(pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c),255)) end end
 local portrait=Image(384,384,ColorMode.RGB)
 portrait:drawImage(crop,Point(math.floor((384-crop.width)/2),math.floor((384-crop.height)/2)))
 portrait:saveAs(e.portrait);portraits[#portraits+1]=portrait
 local t=textMap[e.caption];local caption=Image(t.width*t.scale+8,t.height*t.scale+8,ColorMode.RGB)
 drawText(caption,e.caption,4,4,colors.heading);caption:saveAs(e.captionPath)
 local unit=Image(1280,720,ColorMode.RGB)
 unit:drawImage(portrait,Point(44,180))
 local writing=Image(1280,720,ColorMode.RGB)
 for _,item in ipairs(e.drawText) do
  local tint=colors[item.color]
  if item.color=='accent' and e.category==1 then tint=pc.rgba(139,53,107,255) end
  drawText(writing,item.key,item.x,item.y,tint)
 end
 local page=Image(frame);page:drawImage(unit);page:drawImage(writing);page:saveAs(e.page);pageImages[#pageImages+1]=page
 local sprite=Sprite(1280,720,ColorMode.RGB)
 sprite.layers[1].name='图鉴底板';sprite:newCel(sprite.layers[1],1,frame,Point(0,0))
 local layer=sprite:newLayer();layer.name='独立单位立绘';sprite:newCel(layer,1,unit,Point(0,0))
 layer=sprite:newLayer();layer.name='像素文字_已核对配置';sprite:newCel(layer,1,writing,Point(0,0))
 sprite:saveAs(p.art..'/源文件/'..e.id..'_图鉴.aseprite');sprite:close()
end
local sheet=Image(1240,1400,ColorMode.RGB)
for it in sheet:pixels() do it(pc.rgba(248,226,206,255)) end
for i,e in ipairs(p.entries) do
 local col=(i-1)%4;local row=math.floor((i-1)/4)
 if i>12 then col=col+1 end
 local image=Image(portraits[i]);image:resize(250,250)
 sheet:drawImage(image,Point(30+col*300,18+row*345))
 local t=textMap[e.caption];local x=30+col*300+math.floor((250-t.width*t.scale)/2)
 drawText(sheet,e.caption,x,278+row*345,colors.heading)
end
sheet:saveAs(p.art..'/全角色立绘总览.png')
local pages=Image(1280,760,ColorMode.RGB)
for it in pages:pixels() do it(pc.rgba(70,40,56,255)) end
for i,page in ipairs(pageImages) do local copy=Image(page);copy:resize(304,171);pages:drawImage(copy,Point(8+((i-1)%4)*320,8+math.floor((i-1)/4)*188)) end
pages:saveAs(p.art..'/图鉴页总览.png')
print('Exported 14 portraits, 14 full pages, image captions, native layered sources and overviews.')
