local root=assert(app.params.root)
local digits=assert(app.params.digits)
local pc=app.pixelColor
local f=assert(io.open(root..'/制作记录~/config.json'));local config=json.decode(f:read('*a'));f:close()
local function bounds(im)
  local x0,y0,x1,y1=im.width,im.height,-1,-1
  for it in im:pixels() do if pc.rgbaA(it())>=128 then
    x0=math.min(x0,it.x);y0=math.min(y0,it.y);x1=math.max(x1,it.x);y1=math.max(y1,it.y)
  end end
  assert(x1>=x0,'Empty word');return Rectangle(x0,y0,x1-x0+1,y1-y0+1)
end
local function hardAlpha(im)
  for it in im:pixels() do local c=it();if pc.rgbaA(c)<128 then it(0)
    else it(pc.rgba(pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c),255)) end end
end
local function check(im)
  for x=0,im.width-1 do assert(pc.rgbaA(im:getPixel(x,0))==0 and pc.rgbaA(im:getPixel(x,im.height-1))==0) end
  for y=0,im.height-1 do assert(pc.rgbaA(im:getPixel(0,y))==0 and pc.rgbaA(im:getPixel(im.width-1,y))==0) end
end
local images,reports={},{}
for _,c in ipairs(config) do
  local source=Image{fromFile=root..'/制作记录~/'..c.id..'_source.png'};local im=Image(source,bounds(source))
  local scale=math.min((c.width-16)/im.width,c.inkHeight/im.height)
  im:resize(math.max(1,math.floor(im.width*scale+0.5)),math.max(1,math.floor(im.height*scale+0.5)));hardAlpha(im)
  local canvas=Image(c.width,c.height,ColorMode.RGB);canvas:drawImage(im,Point(math.floor((c.width-im.width)/2),c.height-8-im.height))
  check(canvas);local b=bounds(canvas)
  assert(b.height==c.inkHeight,'Inconsistent text height: '..c.id);assert(b.y+b.height==c.height-8,'Inconsistent baseline')
  canvas:saveAs(root..'/'..c.file);images[c.id]=canvas
  reports[#reports+1]={id=c.id,text=c.text,file=c.file,width=c.width,height=c.height,inkHeight=b.height,
    baselineFromTop=c.height-8,transparentEdges=true}
end
local spr=Sprite(960,584,ColorMode.RGB);spr:assignColorSpace(ColorSpace{sRGB=true})
local positions={
  {'settings',64,32},{'volume',512,32},{'mute',64,144},{'fullscreen',512,144},
  {'defaults',64,256},{'back',512,256},{'mute_hint',64,368},{'window_hint',64,432},
  {'fullscreen_hint',64,496},{'percent',800,368}
}
for i,p in ipairs(positions) do local layer=i==1 and spr.layers[1] or spr:newLayer();layer.name=p[1]
  spr:newCel(layer,1,images[p[1]],Point(p[2],p[3])) end
spr:saveAs(root..'/设置文字_分层源文件.aseprite')
local composite=Image(960,584,ColorMode.RGB);composite:drawSprite(spr,1);composite:saveAs(root..'/设置文字_透明总览.png');spr:close()
local editable=app.open(root..'/设置文字_分层源文件.aseprite');local reload=Image(960,584,ColorMode.RGB);reload:drawSprite(editable,1)
assert(reload:isEqual(composite),'Source roundtrip mismatch')
for i,p in ipairs(positions) do assert(editable.layers[i]:cel(1).image:isEqual(images[p[1]]),'Layer roundtrip mismatch') end
editable:close()
for _,c in ipairs(config) do assert(images[c.id]:isEqual(Image{fromFile=root..'/'..c.file}),'PNG roundtrip mismatch') end
local preview=Image(960,584,ColorMode.RGB);preview:clear(pc.rgba(30,26,40,255));preview:drawImage(composite,Point(0,0))
preview:drawImage(Image{fromFile=digits..'/6.png'},Point(672,368));preview:drawImage(Image{fromFile=digits..'/5.png'},Point(736,368))
preview:saveAs(root..'/设置文字_预览.png')
local f=assert(io.open(root..'/制作记录~/validation.json','w'));f:write(json.encode{
  passed=true,separateTextSprites=10,texts=reports,nativeSourceReopened=true,transparentEdges=true,
  mainInkHeight=64,mainBaselineFromTop=72,hintInkHeight=32,hintBaselineFromTop=40,
  percentCompatibleWithExistingDigits=true,previewValue='65%',previewPositions=positions});f:close()
print('Settings text PASS: ten separate transparent text sprites, normalized baselines, reopened native source and PNG roundtrip.')
