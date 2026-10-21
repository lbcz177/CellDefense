local root=assert(app.params.root)
local atp=assert(app.params.atp)
local pc=app.pixelColor
local function bounds(im)
  local x0,y0,x1,y1=im.width,im.height,-1,-1
  for it in im:pixels() do if pc.rgbaA(it())>=128 then
    x0=math.min(x0,it.x);y0=math.min(y0,it.y);x1=math.max(x1,it.x);y1=math.max(y1,it.y)
  end end
  assert(x1>=x0,'Empty asset');return Rectangle(x0,y0,x1-x0+1,y1-y0+1)
end
local function hardAlpha(im)
  for it in im:pixels() do local c=it();if pc.rgbaA(c)<128 then it(0)
    else it(pc.rgba(pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c),255)) end end
end
local function fit(id,w,h,inkW,inkH,baseline)
  local source=Image{fromFile=root..'/制作记录~/'..id..'_source.png'}
  local im=Image(source,bounds(source));local scale=math.min(inkW/im.width,inkH/im.height)
  im:resize(math.max(1,math.floor(im.width*scale+0.5)),math.max(1,math.floor(im.height*scale+0.5)))
  hardAlpha(im)
  local result=Image(w,h,ColorMode.RGB)
  local y=baseline and baseline-im.height or math.floor((h-im.height)/2)
  result:drawImage(im,Point(math.floor((w-im.width)/2),y));return result
end
local function check(im)
  for x=0,im.width-1 do assert(pc.rgbaA(im:getPixel(x,0))==0 and pc.rgbaA(im:getPixel(x,im.height-1))==0) end
  for y=0,im.height-1 do assert(pc.rgbaA(im:getPixel(0,y))==0 and pc.rgbaA(im:getPixel(im.width-1,y))==0) end
end
local configs={
  {id='life_label',file='生命字样.png',w=160,h=80,iw=144,ih=64,baseline=72},
  {id='wave_label',file='波次字样.png',w=160,h=80,iw=144,ih=64,baseline=72},
  {id='life_icon',file='生命标识_爱心.png',w=128,h=128,iw=112,ih=112},
  {id='wave_icon',file='波次标识_敌群.png',w=128,h=128,iw=112,ih=112},
  {id='slash',file='数值分隔符_斜杠.png',w=64,h=80,iw=48,ih=64,baseline=72},
  {id='frame',file='HUD三栏空框.png',w=1280,h=208,iw=1264,ih=192}
}
local images,reports={},{}
for _,c in ipairs(configs) do
  local im=fit(c.id,c.w,c.h,c.iw,c.ih,c.baseline);check(im);images[c.id]=im
  im:saveAs(root..'/'..c.file)
  local b=bounds(im);if c.baseline then assert(b.height==64 and b.y+b.height==72,'Inconsistent glyph height or baseline') end
  reports[#reports+1]={file=c.file,width=c.w,height=c.h,transparentEdges=true,
    inkBounds={x=b.x,y=b.y,width=b.width,height=b.height}}
end
local frame=Sprite(1280,208,ColorMode.RGB);frame:assignColorSpace(ColorSpace{sRGB=true})
frame.layers[1].name='Empty three-column frame';frame:newCel(frame.layers[1],1,images.frame,Point(0,0))
frame:saveAs(root..'/HUD三栏空框.aseprite');frame:close()
local parts=Sprite(512,256,ColorMode.RGB);parts:assignColorSpace(ColorSpace{sRGB=true})
local positions={{'life_icon',0,0},{'life_label',144,24},{'wave_icon',0,128},{'wave_label',144,152},{'slash',384,24}}
for i,p in ipairs(positions) do local layer=i==1 and parts.layers[1] or parts:newLayer();layer.name=p[1]
  parts:newCel(layer,1,images[p[1]],Point(p[2],p[3])) end
parts:saveAs(root..'/生命波次素材.aseprite');parts:close()
local preview=Sprite(1280,208,ColorMode.RGB);preview:assignColorSpace(ColorSpace{sRGB=true})
preview.layers[1].name='Frame';preview:newCel(preview.layers[1],1,images.frame,Point(0,0))
local previewRects={}
local function put(name,im,x,y,w,h)
  local copy=Image(im);if w then copy:resize(w,h) end
  local layer=preview:newLayer();layer.name=name;preview:newCel(layer,1,copy,Point(x,y))
  previewRects[#previewRects+1]={name=name,x=x,y=y,width=copy.width,height=copy.height}
end
local function numbers(name,text,x,y)
  for i=1,#text do local c=text:sub(i,i);local im=c=='/' and images.slash or Image{fromFile=atp..'/数字/'..c..'.png'}
    put(name..' '..i,im,x+(i-1)*32,y,32,40) end
end
put('ATP icon',Image{fromFile=atp..'/ATP标识.png'},48,72,64,64)
put('ATP label',Image{fromFile=atp..'/ATP字样.png'},128,84,96,40);numbers('ATP value','1250',240,84)
put('Life icon',images.life_icon,516,72,64,64);put('Life label',images.life_label,596,84,80,40);numbers('Life value','20',692,84)
put('Wave icon',images.wave_icon,912,72,64,64);put('Wave label',images.wave_label,992,84,80,40);numbers('Wave value','1/3',1088,84)
preview:saveAs(root..'/HUD组合参考.aseprite')
local composite=Image(1280,208,ColorMode.RGB);composite:drawSprite(preview,1);composite:saveAs(root..'/HUD组合预览.png');preview:close()
-- Validate native sources after saving, without altering the delivery PNGs.
local native=app.open(root..'/HUD三栏空框.aseprite');local roundtrip=Image(1280,208,ColorMode.RGB);roundtrip:drawSprite(native,1)
assert(roundtrip:isEqual(images.frame),'Native frame mismatch');native:close()
local editable=app.open(root..'/生命波次素材.aseprite')
for i,p in ipairs(positions) do assert(editable.layers[i]:cel(1).image:isEqual(images[p[1]]),'Native part mismatch') end
editable:close()
for _,c in ipairs(configs) do assert(images[c.id]:isEqual(Image{fromFile=root..'/'..c.file}),'PNG roundtrip mismatch') end
local f=assert(io.open(root..'/制作记录~/validation.json','w'));f:write(json.encode{
  passed=true,tool='Aseprite',separateAssets=6,assets=reports,sharedDigits='../ATP像素数值_v1/数字',
  nativeSourcesReopened=true,previewOnlyValues={atp=1250,life=20,wave='1/3'},previewRects=previewRects});f:close()
print('HUD art PASS: six separate assets, equal glyph baseline, transparent edges and reopened Aseprite source verified.')
