local root=assert(app.params.root)
local pc=app.pixelColor
local function bounds(im)
  local x0,y0,x1,y1=im.width,im.height,-1,-1
  for it in im:pixels() do if pc.rgbaA(it())>=128 then
    x0=math.min(x0,it.x);y0=math.min(y0,it.y);x1=math.max(x1,it.x);y1=math.max(y1,it.y)
  end end
  assert(x1>=x0,'Empty glyph')
  return Rectangle(x0,y0,x1-x0+1,y1-y0+1)
end
local function hardAlpha(im)
  for it in im:pixels() do local c=it(); if pc.rgbaA(c)<128 then it(0)
    else it(pc.rgba(pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c),255)) end end
end
local function fit(source,w,h,inkW,inkH)
  local im=Image(source,bounds(source))
  local scale=math.min(inkW/im.width,inkH/im.height)
  im:resize(math.max(1,math.floor(im.width*scale+0.5)),math.max(1,math.floor(im.height*scale+0.5)))
  hardAlpha(im)
  local result=Image(w,h,ColorMode.RGB)
  result:drawImage(im,Point(math.floor((w-im.width)/2),h-8-im.height))
  return result
end
local function checkEdges(im)
  for x=0,im.width-1 do assert(pc.rgbaA(im:getPixel(x,0))==0 and pc.rgbaA(im:getPixel(x,im.height-1))==0) end
  for y=0,im.height-1 do assert(pc.rgbaA(im:getPixel(0,y))==0 and pc.rgbaA(im:getPixel(im.width-1,y))==0) end
end
local sheet=Image{fromFile=root..'/制作记录~/digits_source.png'}
local atlas=Sprite(320,160,ColorMode.RGB);atlas:assignColorSpace(ColorSpace{sRGB=true});atlas.gridBounds=Rectangle(0,0,64,80)
local digits,report={},{}
for n=0,9 do
  local col,row=n%5,math.floor(n/5)
  local x0=math.floor(col*sheet.width/5);local x1=math.floor((col+1)*sheet.width/5)
  local y0=math.floor(row*sheet.height/2);local y1=math.floor((row+1)*sheet.height/2)
  local tile=Image(sheet,Rectangle(x0,y0,x1-x0,y1-y0))
  local im=fit(tile,64,80,56,64);checkEdges(im)
  local b=bounds(im);assert(b.height==64,'Unequal glyph height');assert(b.y+b.height==72,'Unequal baseline')
  digits[n+1]=im;im:saveAs(root..'/数字/'..n..'.png')
  local layer=n==0 and atlas.layers[1] or atlas:newLayer();layer.name=tostring(n)
  if layer:cel(1) then atlas:deleteCel(layer,1) end
  atlas:newCel(layer,1,im,Point(col*64,row*80))
  report[#report+1]={name=tostring(n),unicode=48+n,file='数字/'..n..'.png',inkHeight=64,baselineFromTop=72,
    rect={x=col*64,y=(1-row)*80,width=64,height=80}}
end
atlas:saveAs(root..'/ATP像素数字.aseprite')
local atlasImage=Image(320,160,ColorMode.RGB);atlasImage:drawSprite(atlas,1);atlasImage:saveAs(root..'/数字图集_5x2.png')
local label=fit(Image{fromFile=root..'/制作记录~/label_source.png'},192,80,176,64)
local icon=fit(Image{fromFile=root..'/制作记录~/icon_source.png'},128,128,112,112)
checkEdges(label);checkEdges(icon)
label:saveAs(root..'/ATP字样.png');icon:saveAs(root..'/ATP标识.png')
local assets=Sprite(384,128,ColorMode.RGB);assets:assignColorSpace(ColorSpace{sRGB=true})
assets.layers[1].name='ATP icon';assets:newCel(assets.layers[1],1,icon,Point(0,0))
local labelLayer=assets:newLayer();labelLayer.name='ATP label';assets:newCel(labelLayer,1,label,Point(160,24))
assets:saveAs(root..'/ATP字样与标识.aseprite');assets:close()
-- Reopen native source and compare every slice with its individual PNG.
atlas:close();local reopened=app.open(root..'/ATP像素数字.aseprite')
local reopenedImage=Image(320,160,ColorMode.RGB);reopenedImage:drawSprite(reopened,1)
assert(reopenedImage:isEqual(atlasImage),'Native source mismatch')
for n=0,9 do
  local tile=Image(reopenedImage,Rectangle((n%5)*64,math.floor(n/5)*80,64,80))
  assert(tile:isEqual(Image{fromFile=root..'/数字/'..n..'.png'}),'Individual glyph mismatch')
  for k=0,n-1 do assert(not digits[n+1]:isEqual(digits[k+1]),'Duplicate glyph') end
end
reopened:close()
local preview=Image(704,336,ColorMode.RGB);preview:clear(pc.rgba(35,27,43,255))
local iconSmall=Image(icon);iconSmall:resize(96,96)
preview:drawImage(iconSmall,Point(24,12));preview:drawImage(label,Point(128,20))
for i,n in ipairs({1,2,5,0}) do preview:drawImage(digits[n+1],Point(360+(i-1)*64,20)) end
for n=0,9 do preview:drawImage(digits[n+1],Point(192+(n%5)*64,136+math.floor(n/5)*80)) end
preview:saveAs(root..'/展示预览.png')
local f=assert(io.open(root..'/制作记录~/aseprite-validation.json','w'))
f:write(json.encode{passed=true,tool='Aseprite',glyphs=report,distinctDigits=10,transparentEdges=true,
  atlasWidth=320,atlasHeight=160,cellWidth=64,cellHeight=80,nativeSourceReopened=true})
f:close();print('ATP art PASS: ten distinct equal-height glyphs, transparent edges, native source and PNG slices match.')
