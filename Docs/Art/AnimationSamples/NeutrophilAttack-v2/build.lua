-- Pack image-generated attack poses with Aseprite; keep pixel art on a 128px grid.
local input = assert(app.params.input)
local output = assert(app.params.output)
local report = assert(app.params.report)
local source = Image{fromFile=input}
local pc = app.pixelColor
assert(pc.rgbaA(source:getPixel(0,0)) == 0, 'Source must have real transparency')
local size, columns, count = 128, 4, 8
local durations = {160,90,60,50,70,90,130,350}
local phases = {'Ready','Anticipation','Charge','Release','Recoil','Rebound','Settle','Ready'}
local anchorX = {67,69,71,64,74,66,67,67}
local function bounds(im)
  local x0,y0,x1,y1=im.width,im.height,-1,-1
  for it in im:pixels() do
    if pc.rgbaA(it())>=128 then
      x0=math.min(x0,it.x);y0=math.min(y0,it.y)
      x1=math.max(x1,it.x);y1=math.max(y1,it.y)
    end
  end
  assert(x1>=x0,'Empty pose')
  return {x=x0,y=y0,w=x1-x0+1,h=y1-y0+1}
end
local function nucleusX(im)
  local n,sum=0,0
  for it in im:pixels() do
    local p=it();local r,g,b=pc.rgbaR(p),pc.rgbaG(p),pc.rgbaB(p)
    if pc.rgbaA(p)>=128 and b>110 and b>r*1.6 and b>g*1.2 and g<170 then
      n=n+1;sum=sum+it.x+0.5
    end
  end
  assert(n>20,'Missing nucleus')
  return sum/n
end
local poses,measurements={},{}
local maxExtent=0
for i=1,7 do
  local col,row=(i-1)%columns,math.floor((i-1)/columns)
  local x0,x1=math.floor(col*source.width/4),math.floor((col+1)*source.width/4)
  local y0,y1=math.floor(row*source.height/2),math.floor((row+1)*source.height/2)
  local tile=Image(source,Rectangle(x0,y0,x1-x0,y1-y0))
  local b=bounds(tile)
  assert(b.x>1 and b.y>1 and b.x+b.w<tile.width-1 and b.y+b.h<tile.height-1,'Pose crosses cell boundary')
  local crop=Image(tile,Rectangle(b.x-1,b.y-1,b.w+2,b.h+2))
  poses[i]=crop
  measurements[i]={sourceCell={x=x0,y=y0,w=x1-x0,h=y1-y0},bounds=b,nucleusX=nucleusX(crop)}
  maxExtent=math.max(maxExtent,crop.width,crop.height)
end
poses[8]=Image(poses[1]);measurements[8]=measurements[1]
local scale=98/maxExtent
local sprite=Sprite(size,size,ColorMode.RGB)
sprite:assignColorSpace(ColorSpace{sRGB=true})
sprite.layers[1].name='Attack'
sprite.gridBounds=Rectangle(0,0,size,size)
local layer=sprite.layers[1]
local sheet=Image(size*4,size*2,ColorMode.RGB)
local rendered,frames={},{}
for i=1,count do
  local pose=poses[i]
  pose:resize(math.floor(pose.width*scale+0.5),math.floor(pose.height*scale+0.5))
  local im=Image(size,size,ColorMode.RGB)
  local x=math.floor(anchorX[i]-measurements[i].nucleusX*scale+0.5)
  local y=110-pose.height
  im:drawImage(pose,Point(x,y))
  local b=bounds(im)
  assert(b.x>=4 and b.y>=4 and b.x+b.w<=124 and b.y+b.h<=124,'Insufficient margin')
  if i>1 then sprite:newEmptyFrame(i) end
  if layer:cel(i) then sprite:deleteCel(layer,i) end
  sprite:newCel(layer,i,im,Point(0,0))
  sprite.frames[i].duration=durations[i]/1000
  local col,row=(i-1)%4,math.floor((i-1)/4)
  sheet:drawImage(im,Point(col*size,row*size))
  local name=string.format('neutrophil_attack_%02d',i)
  im:saveAs(output..'/frames/'..name..'.png')
  rendered[i]=im
  frames[i]={name=name,phase=phases[i],durationMs=durations[i],
    rect={x=col*size,y=(1-row)*size,width=size,height=size},visibleBounds=b}
end
assert(rendered[1]:isEqual(rendered[8]),'Loop endpoints differ')
local tag=sprite:newTag(1,8);tag.name='AttackPreview';tag.aniDir=AniDir.FORWARD
sprite:saveAs(output..'/neutrophil_attack.aseprite')
sheet:saveAs(output..'/neutrophil_attack_sheet.png')
local preview=Sprite(384,384,ColorMode.RGB)
preview:assignColorSpace(ColorSpace{sRGB=true})
preview.layers[1].name='Preview'
for i=1,8 do
  if i>1 then preview:newEmptyFrame(i) end
  if preview.layers[1]:cel(i) then preview:deleteCel(preview.layers[1],i) end
  local large=Image(rendered[i]);large:resize(384,384)
  local image=Image(384,384,ColorMode.RGB)
  image:clear(pc.rgba(34,40,56,255))
  image:drawImage(large,Point(0,0))
  preview:newCel(preview.layers[1],i,image,Point(0,0))
  preview.frames[i].duration=durations[i]/1000
end
preview:saveAs(output..'/preview.gif')
local document={name='中性粒细胞_强化攻击_v2',frameCount=8,previewCycleMs=1000,
  sheet={width=512,height=256,columns=4,rows=2,cellWidth=128,cellHeight=128},
  pivot={x=0.5,y=0.5},pixelsPerUnit=128,order='left-to-right then top-to-bottom',
  releaseFrame=4,releaseTimeMs=310,frames=frames,
  source={width=source.width,height=source.height,commonScale=scale},
  notes='Preview has idle holds; play a single attack per shot in-game, and fire at the release frame. GIF has a dark background; PNGs retain transparency. Final neutral frame reuses the first to close the loop.',
  method='Built-in image_gen redraw; Aseprite nearest-neighbor packing, registration, timing and GIF export.'}
local f=assert(io.open(output..'/animation.json','w'));f:write(json.encode(document));f:close()
local reopened=app.open(output..'/neutrophil_attack.aseprite')
assert(#reopened.frames==8 and reopened.width==128 and reopened.height==128)
for i=1,8 do
  local im=Image(128,128,ColorMode.RGB);im:drawSprite(reopened,i)
  assert(im:isEqual(rendered[i]),'Reopened frame differs')
  assert(math.abs(reopened.frames[i].duration*1000-durations[i])<0.1,'Duration changed')
  local tile=Image(sheet,Rectangle(((i-1)%4)*128,math.floor((i-1)/4)*128,128,128))
  assert(im:isEqual(tile),'Sheet differs')
  for p=0,127 do
    assert(pc.rgbaA(im:getPixel(p,0))==0 and pc.rgbaA(im:getPixel(p,127))==0)
    assert(pc.rgbaA(im:getPixel(0,p))==0 and pc.rgbaA(im:getPixel(127,p))==0)
  end
end
local result={passed=true,frames=8,sourceWidth=source.width,sourceHeight=source.height,
  frameSize=128,sheetWidth=512,sheetHeight=256,transparentEdges=true,identicalLoopEndpoints=true,
  durationsMs=durations,commonScale=scale}
local f=assert(io.open(report,'w'));f:write(json.encode(result));f:close()
print(json.encode(result))
