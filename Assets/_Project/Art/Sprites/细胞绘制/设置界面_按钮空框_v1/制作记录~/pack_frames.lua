local root=assert(app.params.root)
local textRoot=assert(app.params.text)
local pc=app.pixelColor
local function bounds(im)
  local x0,y0,x1,y1=im.width,im.height,-1,-1
  for it in im:pixels() do if pc.rgbaA(it())>=128 then
    x0=math.min(x0,it.x);y0=math.min(y0,it.y);x1=math.max(x1,it.x);y1=math.max(y1,it.y)
  end end
  assert(x1>=x0,'Empty frame');return Rectangle(x0,y0,x1-x0+1,y1-y0+1)
end
local function alpha(im)
  for it in im:pixels() do local c=it();if pc.rgbaA(c)<128 then it(0)
    else it(pc.rgba(pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c),255)) end end
end
local function edges(im)
  for x=0,im.width-1 do assert(pc.rgbaA(im:getPixel(x,0))==0 and pc.rgbaA(im:getPixel(x,im.height-1))==0) end
  for y=0,im.height-1 do assert(pc.rgbaA(im:getPixel(0,y))==0 and pc.rgbaA(im:getPixel(im.width-1,y))==0) end
end
local images={}
for _,id in ipairs({'normal','selected'}) do
  local source=Image{fromFile=root..'/制作记录~/'..id..'_source.png'};local crop=Image(source,bounds(source))
  -- Resize uniformly, then extend the empty center so the stepped endcaps stay proportional.
  crop:resize(math.floor(crop.width*64/crop.height+0.5),64);alpha(crop)
  assert(crop.width>96,'Insufficient cap width')
  local expanded=Image(432,64,ColorMode.RGB)
  expanded:drawImage(Image(crop,Rectangle(0,0,48,64)),Point(0,0))
  local middle=Image(crop,Rectangle(48,0,crop.width-96,64));middle:resize(336,64)
  expanded:drawImage(middle,Point(48,0))
  expanded:drawImage(Image(crop,Rectangle(crop.width-48,0,48,64)),Point(384,0))
  local canvas=Image(448,80,ColorMode.RGB);canvas:drawImage(expanded,Point(8,8));edges(canvas)
  images[id]=canvas
end
local files={
  {file='01_恢复默认_空框_未选中.png',state='normal',role='恢复默认'},
  {file='02_恢复默认_空框_蓝色选中.png',state='selected',role='恢复默认'},
  {file='03_返回_空框_未选中.png',state='normal',role='返回'},
  {file='04_返回_空框_蓝色选中.png',state='selected',role='返回'}
}
for _,item in ipairs(files) do images[item.state]:saveAs(root..'/'..item.file) end
local source=Sprite(448,160,ColorMode.RGB);source:assignColorSpace(ColorSpace{sRGB=true})
source.layers[1].name='Normal blank frame';source:newCel(source.layers[1],1,images.normal,Point(0,0))
local selected=source:newLayer();selected.name='Selected blank frame';source:newCel(selected,1,images.selected,Point(0,80))
source:saveAs(root..'/恢复默认与返回_按钮框.aseprite');source:close()
local defaults=Image{fromFile=textRoot..'/05_恢复默认.png'};defaults:resize(144,40)
local back=Image{fromFile=textRoot..'/06_返回.png'};back:resize(80,40)
local combined=Sprite(960,224,ColorMode.RGB);combined:assignColorSpace(ColorSpace{sRGB=true})
local background=Image(960,224,ColorMode.RGB);background:clear(pc.rgba(30,26,40,255))
combined.layers[1].name='Preview background';combined:newCel(combined.layers[1],1,background,Point(0,0))
local labels={defaults,back}
for row,id in ipairs({'normal','selected'}) do for col=1,2 do
  local x=16+(col-1)*480;local y=16+(row-1)*112
  local frame=combined:newLayer();frame.name=id..' frame '..col;combined:newCel(frame,1,images[id],Point(x,y))
  local caption=combined:newLayer();caption.name=(col==1 and '恢复默认' or '返回')..' '..id
  combined:newCel(caption,1,labels[col],Point(x+math.floor((448-labels[col].width)/2),y+20))
end end
combined:saveAs(root..'/恢复默认与返回_组合参考.aseprite')
local preview=Image(960,224,ColorMode.RGB);preview:drawSprite(combined,1);preview:saveAs(root..'/恢复默认与返回_组合预览.png');combined:close()
local empty=Image(960,224,ColorMode.RGB);empty:clear(pc.rgba(30,26,40,255))
for row,id in ipairs({'normal','selected'}) do for col=1,2 do empty:drawImage(images[id],Point(16+(col-1)*480,16+(row-1)*112)) end end
empty:saveAs(root..'/按钮空框_预览.png')
local reopened=app.open(root..'/恢复默认与返回_按钮框.aseprite')
assert(reopened.layers[1]:cel(1).image:isEqual(images.normal),'Native normal mismatch')
assert(reopened.layers[2]:cel(1).image:isEqual(images.selected),'Native selected mismatch');reopened:close()
for _,item in ipairs(files) do assert((Image{fromFile=root..'/'..item.file}):isEqual(images[item.state]),'Export mismatch') end
local f=assert(io.open(root..'/制作记录~/validation.json','w'));f:write(json.encode{
  passed=true,files=files,width=448,height=80,inkBounds={x=8,y=8,width=432,height=64},
  sharedFrameDesign=true,textBakedIntoFrame=false,transparentEdges=true,nativeSourceReopened=true,
  suggestedBorder={left=56,bottom=24,right=56,top=24},previewColumns={'恢复默认','返回'},
  previewRows={'未选中','蓝色选中'},labelScale=0.5});f:close()
print('Settings button frames PASS: four independent blank PNGs, matching state canvases, proportional endcaps, native source verified.')
