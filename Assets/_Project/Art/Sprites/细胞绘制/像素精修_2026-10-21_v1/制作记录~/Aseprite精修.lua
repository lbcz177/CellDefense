-- All pixel edits and editable-file exports run inside Aseprite.
local configFile=assert(io.open(assert(app.params.plan),'r'))
local plan=json.decode(configFile:read('*a'));configFile:close()
local root,asset=plan.root,plan.assetRoot
local pc=app.pixelColor
local function load(job) return Image{fromFile=root..'/'..job.source} end
local function bin(p) return (pc.rgbaR(p)>>3)*1024+(pc.rgbaG(p)>>3)*32+(pc.rgbaB(p)>>3) end
local function makePalette(images,count)
 local hist={}
 for _,im in ipairs(images) do for it in im:pixels() do
  local p=it()
  if pc.rgbaA(p)>=128 then
   local k=bin(p);local c=hist[k]
   if not c then c={n=0,r=0,g=0,b=0};hist[k]=c end
   c.n=c.n+1;c.r=c.r+pc.rgbaR(p);c.g=c.g+pc.rgbaG(p);c.b=c.b+pc.rgbaB(p)
  end
 end end
 local entries={}
 for k,c in pairs(hist) do c.key=k;c.r=c.r/c.n;c.g=c.g/c.n;c.b=c.b/c.n;table.insert(entries,c) end
 local function box(values)
  local b={values=values,n=0,r0=255,r1=0,g0=255,g1=0,b0=255,b1=0}
  for _,c in ipairs(values) do
   b.n=b.n+c.n;b.r0=math.min(b.r0,c.r);b.r1=math.max(b.r1,c.r)
   b.g0=math.min(b.g0,c.g);b.g1=math.max(b.g1,c.g);b.b0=math.min(b.b0,c.b);b.b1=math.max(b.b1,c.b)
  end
  b.range=math.max(b.r1-b.r0,b.g1-b.g0,b.b1-b.b0)
  b.score=#values>1 and b.range*math.sqrt(b.n) or -1
  return b
 end
 local boxes={box(entries)}
 while #boxes<count do
  local best=1
  for i=2,#boxes do if boxes[i].score>boxes[best].score then best=i end end
  local b=boxes[best];if b.score<=0 then break end
  local axis='r'
  if b.g1-b.g0>b.r1-b.r0 then axis='g' end
  if b.b1-b.b0>math.max(b.r1-b.r0,b.g1-b.g0) then axis='b' end
  table.sort(b.values,function(a,c) if a[axis]==c[axis] then return a.key<c.key end return a[axis]<c[axis] end)
  local sum,split=0,1
  for i=1,#b.values-1 do sum=sum+b.values[i].n;split=i;if sum>=b.n/2 then break end end
  local a,c={},{}
  for i,v in ipairs(b.values) do table.insert(i<=split and a or c,v) end
  boxes[best]=box(a);table.insert(boxes,box(c))
 end
 local colors={}
 for _,b in ipairs(boxes) do
  local r,g,blue=0,0,0
  for _,c in ipairs(b.values) do r=r+c.r*c.n;g=g+c.g*c.n;blue=blue+c.b*c.n end
  table.insert(colors,{r=math.floor(r/b.n+.5),g=math.floor(g/b.n+.5),b=math.floor(blue/b.n+.5)})
 end
 local mapping={}
 for k,c in pairs(hist) do
  local best,distance=nil,math.huge
  for _,v in ipairs(colors) do
   local d=(c.r-v.r)^2+(c.g-v.g)^2+(c.b-v.b)^2
   if d<distance then best=v;distance=d end
  end
  mapping[k]=best
 end
 return colors,mapping
end
local function refine(im,kind,colors,mapping)
 local result=Image(im)
 for it in result:pixels() do
  local p=it();local a=pc.rgbaA(p)
  if kind=='ui' then a=a>=128 and 255 or 0
  elseif kind=='effect' then if a<=16 then a=0 elseif a>=240 then a=255 end end
  if a==0 then it(0)
  else
   local c=mapping[bin(p)]
   if not c then
    local r,g,b=pc.rgbaR(p),pc.rgbaG(p),pc.rgbaB(p);local distance=math.huge
    for _,v in ipairs(colors) do
     local d=(r-v.r)^2+(g-v.g)^2+(b-v.b)^2
     if d<distance then c=v;distance=d end
    end
   end
   it(pc.rgba(c.r,c.g,c.b,a))
  end
 end
 return result
end
local function aseprite(job,image)
 if job.kind=='animation' then
  local s=Sprite(128,128,ColorMode.RGB);s.layers[1].name='细胞动作'
  local times={.05,.04,.04,.03,.04,.04,.05,.05,.06,.08}
  local move=job.source:find('_move/',1,true)~=nil
  for i=1,10 do
   if i>1 then s:newEmptyFrame(i) end
   local frame=Image(image,Rectangle((i-1)%5*128,math.floor((i-1)/5)*128,128,128))
   s:newCel(s.layers[1],i,frame)
   s.frames[i].duration=move and .1 or times[i]
  end
  local tag=s:newTag(1,10);tag.name=move and 'Move' or 'Attack'
  s:saveAs(asset..'/制作记录~/Aseprite源文件/'..job.group..(move and '_move' or '_attack')..'.aseprite');s:close()
 else
  local s=Sprite(image.width,image.height,ColorMode.RGB);s.layers[1].name='像素精修'
  s:newCel(s.layers[1],1,image)
  s:saveAs(asset..'/制作记录~/Aseprite源文件/'..string.format('%02d',job.index)..'.aseprite');s:close()
 end
end
local groups,order={},{}
for i,job in ipairs(plan.jobs) do
 job.index=i
 if not groups[job.group] then groups[job.group]={};table.insert(order,job.group) end
 table.insert(groups[job.group],job)
end
local summaries={}
local comparison=Image(768,768,ColorMode.RGB);comparison:clear(pc.rgba(35,30,48,255))
local comparisonIndex=0
local iconNames={'近卫','纤维蛋白壁垒','酸化炮','神经脉冲','巨噬细胞','抗体细胞'}
local icons={}
for _,group in ipairs(order) do
 local jobs=groups[group];local originals={}
 for _,job in ipairs(jobs) do table.insert(originals,load(job)) end
 local colors,mapping
 if jobs[1].kind~='import' then colors,mapping=makePalette(originals,jobs[1].colors) end
 for j,job in ipairs(jobs) do
  local original=originals[j]
  local output=job.kind=='import' and Image(original) or refine(original,job.kind,colors,mapping)
  output:saveAs(root..'/'..job.destination)
  if job.kind~='import' then aseprite(job,output) end
  local pixels,changed,totalError,alphaChanged=0,0,0,0
  for it in original:pixels() do
   local p,q=it(),output:getPixel(it.x,it.y)
   if pc.rgbaA(p)~=pc.rgbaA(q) then alphaChanged=alphaChanged+1 end
   if pc.rgbaA(p)>=128 then
    pixels=pixels+1;if p~=q then changed=changed+1 end
    totalError=totalError+math.abs(pc.rgbaR(p)-pc.rgbaR(q))+math.abs(pc.rgbaG(p)-pc.rgbaG(q))+math.abs(pc.rgbaB(p)-pc.rgbaB(q))
   end
  end
  table.insert(summaries,{source=job.source,destination=job.destination,visiblePixels=pixels,changedPixels=changed,meanRGBError=totalError/math.max(1,pixels*3),alphaChanged=alphaChanged,paletteColors=colors and #colors or 0})
  if job.kind=='animation' then
   local unit=tonumber(group:sub(1,2))
   if unit and unit<=6 then
    local frame=Image(output,Rectangle(0,0,128,128));local x0,y0,x1,y1=128,128,-1,-1
    for it in frame:pixels() do if pc.rgbaA(it())>=128 then
     x0=math.min(x0,it.x);x1=math.max(x1,it.x);y0=math.min(y0,it.y);y1=math.max(y1,it.y)
    end end
    local crop=Image(frame,Rectangle(x0,y0,x1-x0+1,y1-y0+1))
    local scale=104/math.max(crop.width,crop.height)
    crop:resize(math.floor(crop.width*scale+.5),math.floor(crop.height*scale+.5))
    local icon=Image(128,128,ColorMode.RGB);icon:drawImage(crop,Point(math.floor((128-crop.width)/2),math.floor((128-crop.height)/2)))
    local filename=string.format('%02d_',unit)..iconNames[unit]..'.png'
    icon:saveAs(asset..'/图标/'..filename)
    table.insert(icons,{unit=unit,path=asset..'/图标/'..filename,group=group})
   end
   if job.source:find('_attack/',1,true) and comparisonIndex<6 then
    local before=Image(original,Rectangle(0,0,128,128));local after=Image(output,Rectangle(0,0,128,128))
    before:resize(256,256);after:resize(256,256)
    local x=(comparisonIndex%3)*256;local y=math.floor(comparisonIndex/3)*384
    before:resize(128,128);after:resize(128,128)
    comparison:drawImage(before,Point(x,y+12));comparison:drawImage(after,Point(x+128,y+12))
    local zoomBefore=Image(original,Rectangle(32,24,64,64));zoomBefore:resize(128,128)
    local zoomAfter=Image(output,Rectangle(32,24,64,64));zoomAfter:resize(128,128)
    comparison:drawImage(zoomBefore,Point(x,y+152));comparison:drawImage(zoomAfter,Point(x+128,y+152))
    comparisonIndex=comparisonIndex+1
   end
  end
 end
 print('REFINED '..group)
end
comparison:saveAs(asset..'/制作记录~/角色像素前后对比.png')
local iconSheet=Image(768,128,ColorMode.RGB);iconSheet:clear(pc.rgba(35,30,48,255))
for _,v in ipairs(icons) do iconSheet:drawImage(Image{fromFile=v.path},Point((v.unit-1)*128,0)) end
iconSheet:resize(1536,256);iconSheet:saveAs(asset..'/六塔图标预览.png')
local f=assert(io.open(asset..'/制作记录~/Aseprite处理结果.json','w'));f:write(json.encode({jobs=summaries,icons=icons}));f:close()
print('COMPLETE '..#summaries..' images and '..#icons..' icons')
