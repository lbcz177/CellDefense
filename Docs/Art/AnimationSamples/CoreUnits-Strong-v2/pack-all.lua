-- Native Aseprite packing of image_gen poses, with common scaling and registered anchors.
local docRoot=assert(app.params.docs)
local assetRoot=assert(app.params.assets)
local configFile=assert(io.open(docRoot..'/progress.json','r'))
local jobs=json.decode(configFile:read('*a'));configFile:close()
local pc=app.pixelColor
local results,clips={},{}
local attackDurations={160,90,60,50,70,90,130,350}
local attackPhases={'Ready','Anticipation','Charge','Release','Recoil','Rebound','Settle','Ready'}
local function writeJson(path,value)
  local f=assert(io.open(path,'w'));f:write(json.encode(value));f:close()
end
local function bounds(image)
  local x0,y0,x1,y1=image.width,image.height,-1,-1
  for it in image:pixels() do
    if pc.rgbaA(it())>=128 then
      x0=math.min(x0,it.x);y0=math.min(y0,it.y)
      x1=math.max(x1,it.x);y1=math.max(y1,it.y)
    end
  end
  assert(x1>=x0 and y1>=y0,'Empty pose')
  return {x=x0,y=y0,w=x1-x0+1,h=y1-y0+1}
end
local function rowSplit(source)
  -- Generated artwork sometimes offsets the row gap. Find its actual transparent seam.
  local center=math.floor(source.height/2)
  for distance=0,math.floor(source.height*0.12) do
    for _,y in ipairs({center-distance,center+distance}) do
      local empty=true
      for yy=y-4,y+4 do
        for x=0,source.width-1 do
          if pc.rgbaA(source:getPixel(x,yy))>=128 then empty=false;break end
        end
        if not empty then break end
      end
      if empty then return y end
    end
  end
  error('No transparent seam between rows')
end
local function anchorX(image,job)
  if job.unit=='fibrin' or job.unit=='staph' or job.unit=='bacillus' then return image.width/2 end
  local sum,n=0,0
  for it in image:pixels() do
    local p=it();local r,g,b=pc.rgbaR(p),pc.rgbaG(p),pc.rgbaB(p)
    local matches=false
    if job.unit=='diplococcus' then
      matches=r>60 and b>r*1.1 and b>g*1.15
    else
      -- Keep the root/feet fixed; let the nucleus move during squash, stretch and recoil.
      matches=it.y>=math.floor(image.height*0.82)
    end
    if pc.rgbaA(p)>=128 and matches then sum=sum+it.x+0.5;n=n+1 end
  end
  if n<10 then return image.width/2 end
  return sum/n
end
local function pack(job)
  local output=assetRoot..'/'..job.id
  local source=Image{fromFile=docRoot..'/sources/'..job.id..'.png'}
  assert(pc.rgbaA(source:getPixel(0,0))==0,'Source must have real transparency')
  local split=rowSplit(source)
  local poses,measurements,images,frames={},{},{},{}
  local maxExtent=0
  local tower=job.unit~='staph' and job.unit~='diplococcus' and job.unit~='bacillus'
  local durations=job.action=='Move' and {100,100,100,100,100,100,100,100} or attackDurations
  local targets={64,64,64,64,64,64,64,64}
  local yOffsets={0,0,0,0,0,0,0,0}
  if not tower and job.action=='Attack' then
    if job.unit=='diplococcus' then targets={73,71,70,78,73,71,73,73}
    else targets={64,62,60,70,65,62,64,64} end
    yOffsets=job.unit=='bacillus' and {0,0,1,-3,-1,0,0,0} or {0,0,0,2,-1,0,0,0}
  elseif job.action=='Move' then
    targets=job.unit=='diplococcus' and {73,73,73,73,73,73,73,73} or targets
    yOffsets={-1,0,1,2,1,0,-1,-2}
  end
  local sourceCount=job.action=='Attack' and 7 or 8
  for index=1,sourceCount do
    local col,row=(index-1)%4,math.floor((index-1)/4)
    local x0,x1=math.floor(col*source.width/4),math.floor((col+1)*source.width/4)
    local y0,y1=row==0 and 0 or split,row==0 and split or source.height
    local tile=Image(source,Rectangle(x0,y0,x1-x0,y1-y0))
    local b=bounds(tile)
    assert(b.x>1 and b.y>1 and b.x+b.w<tile.width-1 and b.y+b.h<tile.height-1,
      'Pose touches cell boundary: '..index)
    local crop=Image(tile,Rectangle(b.x-1,b.y-1,b.w+2,b.h+2))
    poses[index]=crop
    measurements[index]={cell={x=x0,y=y0,w=x1-x0,h=y1-y0},bounds=b,anchorX=anchorX(crop,job)}
    maxExtent=math.max(maxExtent,crop.width,crop.height)
  end
  if job.action=='Attack' then
    poses[8]=Image(poses[1]);measurements[8]=measurements[1]
  end
  local scale=98/maxExtent
  for i=1,8 do
    local cx=measurements[i].anchorX
    scale=math.min(scale,(targets[i]-7)/cx,(121-targets[i])/(poses[i].width-cx))
  end
  local sprite=Sprite(128,128,ColorMode.RGB)
  sprite:assignColorSpace(ColorSpace{sRGB=true})
  sprite.gridBounds=Rectangle(0,0,128,128)
  sprite.layers[1].name=job.action
  local layer=sprite.layers[1]
  local sheet=Image(512,256,ColorMode.RGB)
  for i=1,8 do
    local pose=poses[i]
    pose:resize(math.floor(pose.width*scale+0.5),math.floor(pose.height*scale+0.5))
    local image=Image(128,128,ColorMode.RGB)
    local x=math.floor(targets[i]-measurements[i].anchorX*scale+0.5)
    local y=110-pose.height+yOffsets[i]
    image:drawImage(pose,Point(x,y))
    local b=bounds(image)
    assert(b.x>=4 and b.y>=4 and b.x+b.w<=124 and b.y+b.h<=124,'Insufficient safe margin')
    if i>1 then sprite:newEmptyFrame(i) end
    if layer:cel(i) then sprite:deleteCel(layer,i) end
    sprite:newCel(layer,i,image,Point(0,0));sprite.frames[i].duration=durations[i]/1000
    local col,row=(i-1)%4,math.floor((i-1)/4)
    sheet:drawImage(image,Point(col*128,row*128))
    local name=string.format('animation_%02d',i)
    image:saveAs(output..'/frames/'..name..'.png')
    images[i]=image
    frames[i]={name=name,phase=job.action=='Attack' and attackPhases[i] or ('MovePhase'..i),
      durationMs=durations[i],rect={x=col*128,y=(1-row)*128,width=128,height=128},visibleBounds=b}
  end
  local transitions={}
  for i=1,8 do
    local nextIndex=i%8+1
    local changed,union,silhouette=0,0,0
    for y=0,127 do for x=0,127 do
      local a,b=images[i]:getPixel(x,y),images[nextIndex]:getPixel(x,y)
      local va,vb=pc.rgbaA(a)>=128,pc.rgbaA(b)>=128
      if va or vb then
        union=union+1
        if a~=b then changed=changed+1 end
        if va~=vb then silhouette=silhouette+1 end
      end
    end end
    if not (job.action=='Attack' and i==8) then assert(changed>0,'Identical consecutive poses') end
    transitions[i]={from=i,to=nextIndex,changedFraction=changed/math.max(1,union),
      silhouetteChange=silhouette/math.max(1,union)}
  end
  if job.action=='Attack' then assert(images[1]:isEqual(images[8]),'Loop endpoints differ') end
  local tag=sprite:newTag(1,8);tag.name=job.action;tag.aniDir=AniDir.FORWARD
  sprite:saveAs(output..'/animation.aseprite');sheet:saveAs(output..'/animation_sheet.png')
  local preview=Sprite(384,384,ColorMode.RGB)
  preview:assignColorSpace(ColorSpace{sRGB=true});preview.layers[1].name='Preview'
  for i=1,8 do
    if i>1 then preview:newEmptyFrame(i) end
    if preview.layers[1]:cel(i) then preview:deleteCel(preview.layers[1],i) end
    local large=Image(images[i]);large:resize(384,384)
    local im=Image(384,384,ColorMode.RGB);im:clear(pc.rgba(34,40,56,255))
    im:drawImage(large,Point(0,0))
    preview:newCel(preview.layers[1],i,im,Point(0,0));preview.frames[i].duration=durations[i]/1000
  end
  preview:saveAs(output..'/preview.gif')
  local period=0;for _,v in ipairs(durations) do period=period+v end
  local document={name=job.label,unit=job.unit,action=job.action,frameCount=8,cycleMs=period,
    sheet={width=512,height=256,columns=4,rows=2,cellWidth=128,cellHeight=128},
    order='left-to-right then top-to-bottom',pivot={x=0.5,y=0.5},pixelsPerUnit=128,
    releaseFrame=job.action=='Attack' and 4 or nil,releaseTimeMs=job.action=='Attack' and 310 or nil,
    frames=frames,transitions=transitions,source={width=source.width,height=source.height,rowSeam=split,commonScale=scale},
    method='Built-in image_gen redraw; Aseprite nearest-neighbor registration, common-scale packing and timing.',
    notes='GIF background is for preview only. PNG frames have transparency. Attack previews include idle holds; fire once at release, not once per animation frame. Preserve variable frame times when creating a Unity clip.'}
  writeJson(output..'/animation.json',document)
  local reopened=app.open(output..'/animation.aseprite')
  assert(#reopened.frames==8 and reopened.width==128 and reopened.height==128)
  local reloadedSheet=Image{fromFile=output..'/animation_sheet.png'}
  for i=1,8 do
    local im=Image(128,128,ColorMode.RGB);im:drawSprite(reopened,i)
    local tile=Image(reloadedSheet,Rectangle(((i-1)%4)*128,math.floor((i-1)/4)*128,128,128))
    local single=Image{fromFile=output..'/frames/'..string.format('animation_%02d',i)..'.png'}
    assert(im:isEqual(images[i]) and im:isEqual(tile) and im:isEqual(single),'Frame export mismatch')
    assert(math.abs(reopened.frames[i].duration*1000-durations[i])<0.1,'Frame time mismatch')
    for p=0,127 do
      assert(pc.rgbaA(im:getPixel(p,0))==0 and pc.rgbaA(im:getPixel(p,127))==0)
      assert(pc.rgbaA(im:getPixel(0,p))==0 and pc.rgbaA(im:getPixel(127,p))==0)
    end
  end
  local validation={passed=true,id=job.id,frameCount=8,frameSize=128,sheetWidth=512,sheetHeight=256,
    transparentEdges=true,reopenedFramesEqual=true,loopEndpointsEqual=job.action=='Attack' and true or nil,
    sourceWidth=source.width,sourceHeight=source.height,rowSeam=split,commonScale=scale,cycleMs=period}
  writeJson(docRoot..'/reports/'..job.id..'.json',validation)
  clips[job.id]={images=images,durations=durations,period=period}
  return validation
end
for _,job in ipairs(jobs) do
  local ok,result=pcall(pack,job)
  if not ok then result={passed=false,id=job.id,error=tostring(result)} end
  results[#results+1]=result
  print(json.encode(result))
end
writeJson(docRoot..'/validation.json',results)
for _,r in ipairs(results) do assert(r.passed,'Packing failed: '..r.id..' '..tostring(r.error)) end
-- Add the previously completed neutrophil preview to the six-tower overview.
local old=app.open(assetRoot..'/../中性粒细胞_强化攻击_v2/neutrophil_attack.aseprite')
local existing={images={},durations={},period=1000}
for i=1,8 do
  local image=Image(128,128,ColorMode.RGB);image:drawSprite(old,i)
  existing.images[i]=image;existing.durations[i]=math.floor(old.frames[i].duration*1000+0.5)
end
clips['01_neutrophil_attack']=existing
local function overview(name,ids,columns,rows,period)
  local tile=256
  local sprite=Sprite(columns*tile,rows*tile,ColorMode.RGB)
  sprite:assignColorSpace(ColorSpace{sRGB=true});sprite.layers[1].name='Overview'
  for time=0,period-50,50 do
    local frame=time/50+1
    if frame>1 then sprite:newEmptyFrame(frame) end
    if sprite.layers[1]:cel(frame) then sprite:deleteCel(sprite.layers[1],frame) end
    local image=Image(sprite.width,sprite.height,ColorMode.RGB);image:clear(pc.rgba(34,40,56,255))
    for index,id in ipairs(ids) do
      local clip=assert(clips[id]);local t=time%clip.period
      local pose=1
      while pose<8 and t>=clip.durations[pose] do t=t-clip.durations[pose];pose=pose+1 end
      local enlarged=Image(clip.images[pose]);enlarged:resize(tile,tile)
      image:drawImage(enlarged,Point(((index-1)%columns)*tile,math.floor((index-1)/columns)*tile))
    end
    sprite:newCel(sprite.layers[1],frame,image,Point(0,0));sprite.frames[frame].duration=0.05
    if frame==1 then image:saveAs(assetRoot..'/'..name..'.png') end
  end
  sprite:saveAs(assetRoot..'/'..name..'.gif')
end
overview('塔攻击总览',{'01_neutrophil_attack','02_fibrin_attack','03_lysosome_attack',
  '04_neural_attack','05_phagocyte_attack','06_antibody_attack'},3,2,1000)
overview('敌人移动与攻击总览',{'07_staph_move','08_staph_attack','09_diplococcus_move',
  '10_diplococcus_attack','11_bacillus_move','12_bacillus_attack'},2,3,4000)
print('Completed 11 clips, 88 frames, and two overview GIFs.')
