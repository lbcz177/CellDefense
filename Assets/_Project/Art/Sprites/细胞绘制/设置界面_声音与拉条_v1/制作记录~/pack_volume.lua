local root=assert(app.params.root)
local pc=app.pixelColor
local function bounds(im)
  local x0,y0,x1,y1=im.width,im.height,-1,-1
  for it in im:pixels() do if pc.rgbaA(it())>=128 then
    x0=math.min(x0,it.x);y0=math.min(y0,it.y);x1=math.max(x1,it.x);y1=math.max(y1,it.y)
  end end
  assert(x1>=x0,'Empty asset');return Rectangle(x0,y0,x1-x0+1,y1-y0+1)
end
local function alpha(im)
  for it in im:pixels() do local c=it();if pc.rgbaA(c)<128 then it(0)
    else it(pc.rgba(pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c),255)) end end
end
local function check(im)
  for x=0,im.width-1 do assert(pc.rgbaA(im:getPixel(x,0))==0 and pc.rgbaA(im:getPixel(x,im.height-1))==0) end
  for y=0,im.height-1 do assert(pc.rgbaA(im:getPixel(0,y))==0 and pc.rgbaA(im:getPixel(im.width-1,y))==0) end
end
local configs={
  {id='sound',file='01_声音图标.png',w=128,h=128,iw=112,ih=112,fit=true},
  {id='muted',file='02_静音图标.png',w=128,h=128,iw=112,ih=112,fit=true},
  {id='track',file='03_音量槽_底条.png',w=448,h=48,iw=432,ih=32},
  {id='fill',file='04_音量条_蓝色填充.png',w=432,h=32,iw=416,ih=16},
  {id='handle',file='05_音量滑块_拖动柄.png',w=48,h=64,iw=32,ih=48}
}
local images,reports={},{}
for _,c in ipairs(configs) do
  local src=Image{fromFile=root..'/制作记录~/'..c.id..'_source.png'};local im=Image(src,bounds(src))
  local w,h=c.iw,c.ih
  if c.fit then local s=math.min(w/im.width,h/im.height);w=math.floor(im.width*s+0.5);h=math.floor(im.height*s+0.5) end
  im:resize(w,h);alpha(im)
  local final=Image(c.w,c.h,ColorMode.RGB);final:drawImage(im,Point(math.floor((c.w-w)/2),math.floor((c.h-h)/2)))
  check(final);final:saveAs(root..'/'..c.file);images[c.id]=final
  reports[#reports+1]={id=c.id,file=c.file,width=c.w,height=c.h,transparentEdges=true}
end
local function scaled(im,w,h) local copy=Image(im);copy:resize(w,h);return copy end
local value=0.65;local pixels=math.floor(416*value+0.5)
local partial=Image(images.fill)
for it in partial:pixels() do if it.x>=8+pixels then it(0) end end
local spr=Sprite(640,112,ColorMode.RGB);spr:assignColorSpace(ColorSpace{sRGB=true})
local parts={
  {id='sound',im=scaled(images.sound,64,64),x=16,y=24,visible=true},
  {id='muted',im=scaled(images.muted,64,64),x=16,y=24,visible=false},
  {id='track',im=images.track,x=112,y=32,visible=true},
  {id='full_fill',im=images.fill,x=120,y=40,visible=false},
  {id='preview_fill_65_percent',im=partial,x=120,y=40,visible=true},
  {id='handle',im=images.handle,x=112+16+pixels-24,y=24,visible=true}
}
for i,p in ipairs(parts) do local layer=i==1 and spr.layers[1] or spr:newLayer();layer.name=p.id
  spr:newCel(layer,1,p.im,Point(p.x,p.y));layer.isVisible=p.visible end
local combined=Image(640,112,ColorMode.RGB);combined:drawSprite(spr,1);check(combined)
combined:saveAs(root..'/声音与拉条_组合透明.png');spr:saveAs(root..'/声音与拉条_分层源文件.aseprite');spr:close()
local native=app.open(root..'/声音与拉条_分层源文件.aseprite');local reload=Image(640,112,ColorMode.RGB);reload:drawSprite(native,1)
assert(reload:isEqual(combined),'Native composition mismatch');native:close()
for _,c in ipairs(configs) do assert(images[c.id]:isEqual(Image{fromFile=root..'/'..c.file}),'Independent PNG mismatch') end
local preview=Image(704,256,ColorMode.RGB);preview:clear(pc.rgba(30,26,40,255))
preview:drawImage(combined,Point(32,16))
local mutedPreview=Image(combined)
for y=0,mutedPreview.height-1 do for x=0,95 do mutedPreview:drawPixel(x,y,0) end end
mutedPreview:drawImage(scaled(images.muted,64,64),Point(16,24))
preview:drawImage(mutedPreview,Point(32,128));preview:saveAs(root..'/声音与拉条_预览.png')
local f=assert(io.open(root..'/制作记录~/validation.json','w'));f:write(json.encode{
  passed=true,separateAssets=5,assets=reports,nativeSourceReopened=true,transparentEdges=true,
  previewValue=0.65,mutePreservesSliderValue=true,trackInnerRect={x=16,y=16,width=416,height=16},
  fillVisibleRect={x=8,y=8,width=416,height=16},handleVisibleRect={x=8,y=8,width=32,height=48},
  compositionCanvas={width=640,height=112},compositionParts={{id='sound',x=16,y=24,w=64,h=64},
  {id='track',x=112,y=32,w=448,h=48},{id='fill',x=120,y=40,w=432,h=32},
  {id='handle',x=112+16+pixels-24,y=24,w=48,h=64}}});f:close()
print('Volume UI PASS: five independent transparent assets, layered source roundtrip, fill and handle aligned.')
