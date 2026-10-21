local root=assert(app.params.root)
local out=assert(app.params.out)
local only=app.params.unit
local function read(path) local f=assert(io.open(path,'r'));local v=json.decode(f:read('*a'));f:close();return v end
local function write(path,v) local f=assert(io.open(path,'w'));f:write(json.encode(v));f:close() end
local pc=app.pixelColor
local units=read(root..'/units.json');if units.Id then units={units} end
local function visible(p,enemy)
  if pc.rgbaA(p)<128 then return false end
  if not enemy then return true end
  return pc.rgbaB(p)>pc.rgbaG(p)*1.15 and pc.rgbaB(p)>pc.rgbaR(p)*0.45
end
local function bounds(im,enemy)
  local x0,y0,x1,y1=im.width,im.height,-1,-1
  for it in im:pixels() do if visible(it(),enemy) then
    x0=math.min(x0,it.x);y0=math.min(y0,it.y);x1=math.max(x1,it.x);y1=math.max(y1,it.y)
  end end
  assert(x1>=x0,'Empty pose')
  return {x=x0,y=y0,w=x1-x0+1,h=y1-y0+1}
end
local function cuts(counts,n)
  local bands={};local start=nil
  for i=1,#counts+1 do
    local on=i<=#counts and counts[i]>1
    if on and not start then start=i-1 end
    if not on and start then
      local finish=i-2
      if #bands>0 and start-bands[#bands].b<=7 then bands[#bands].b=finish
      else bands[#bands+1]={a=start,b=finish} end
      start=nil
    end
  end
  assert(#bands>=n,'Not enough separated sprite bands: '..#bands..'/'..n)
  local gaps={}
  for i=1,#bands-1 do gaps[#gaps+1]={size=bands[i+1].a-bands[i].b,at=math.floor((bands[i+1].a+bands[i].b)/2)} end
  table.sort(gaps,function(a,b)return a.size>b.size end)
  local result={0};for i=1,n-1 do result[#result+1]=gaps[i].at end
  result[#result+1]=#counts;table.sort(result);return result
end
local function anchor(im,id,enemy)
  local sx,sy,n=0,0,0
  for it in im:pixels() do
    local p=it();local r,g,b=pc.rgbaR(p),pc.rgbaG(p),pc.rgbaB(p)
    local hit=false
    if pc.rgbaA(p)>=128 then
      if enemy then hit=visible(p,true) and (id~='08_flagellate' or it.x>im.width*0.38)
      elseif id=='02_fibrin' or id=='03_lysosome' then hit=g>r*1.15 and g>b*1.08
      elseif id=='06_antibody' then hit=r>g*0.95 and g>b*1.4 and b<150
      else hit=b>r*1.4 and b>g*1.2 end
    end
    if hit then sx=sx+it.x+0.5;sy=sy+it.y+0.5;n=n+1 end
  end
  if n<10 then return im.width/2,im.height/2 end
  return sx/n,sy/n
end
local function hardAlpha(im)
  for it in im:pixels() do local p=it();if pc.rgbaA(p)<128 then it(0)
    else it(pc.rgba(pc.rgbaR(p),pc.rgbaG(p),pc.rgbaB(p),255)) end end
end
local attackMs={50,40,40,30,40,40,50,50,60,80}
local function export(unit,poses,coords,action,offset,scale)
  local dir=out..'/'..unit.Id..'_'..string.lower(action)
  local spr=Sprite(128,128,ColorMode.RGB);spr:assignColorSpace(ColorSpace{sRGB=true})
  spr.layers[1].name=action;spr.gridBounds=Rectangle(0,0,128,128)
  local sheet=Image(640,256,ColorMode.RGB);local images,frames={},{};local elapsed=0
  for i=1,10 do
    local source=poses[i+offset];local im=Image(source)
    im:resize(math.max(1,math.floor(im.width*scale+0.5)),math.max(1,math.floor(im.height*scale+0.5)))
    local frame=Image(128,128,ColorMode.RGB)
    frame:drawImage(im,Point(math.floor(64-coords[i+offset].x*scale+0.5),math.floor(64-coords[i+offset].y*scale+0.5)))
    hardAlpha(frame)
    local b=bounds(frame,false);assert(b.x>=5 and b.y>=5 and b.x+b.w<=123 and b.y+b.h<=123,'Unsafe output margin')
    if i>1 then spr:newEmptyFrame() end
    if spr.layers[1]:cel(i) then spr:deleteCel(spr.layers[1],i) end
    spr:newCel(spr.layers[1],i,frame,Point(0,0))
    local ms=action=='Move' and 100 or attackMs[i];spr.frames[i].duration=ms/1000
    frame:saveAs(dir..'/frames/frame_'..string.format('%02d',i)..'.png')
    sheet:drawImage(frame,Point(((i-1)%5)*128,math.floor((i-1)/5)*128))
    images[i]=frame;frames[i]={index=i,startMs=elapsed,durationMs=ms,bounds=b,
      rect={x=((i-1)%5)*128,y=(1-math.floor((i-1)/5))*128,width=128,height=128}}
    elapsed=elapsed+ms
  end
  local distinct=0
  for i=1,10 do local duplicate=false;for j=1,i-1 do if images[i]:isEqual(images[j]) then duplicate=true;break end end
    if not duplicate then distinct=distinct+1 end end
  assert(distinct==10,'Not ten distinct drawn poses: '..distinct)
  spr:newTag(1,10).name=action;spr:saveAs(dir..'/animation.aseprite');sheet:saveAs(dir..'/animation_sheet.png')
  local preview=Sprite(384,384,ColorMode.RGB);preview:assignColorSpace(ColorSpace{sRGB=true})
  for i=1,10 do
    if i>1 then preview:newEmptyFrame() end
    if preview.layers[1]:cel(i) then preview:deleteCel(preview.layers[1],i) end
    local large=Image(images[i]);large:resize(384,384)
    local frame=Image(384,384,ColorMode.RGB);frame:clear(pc.rgba(30,36,52,255));frame:drawImage(large,Point(0,0))
    preview:newCel(preview.layers[1],i,frame,Point(0,0));preview.frames[i].duration=frames[i].durationMs/1000
  end
  preview:saveAs(dir..'/preview.gif');preview:close()
  local check=app.open(dir..'/animation.aseprite');assert(#check.frames==10)
  local reloaded=Image{fromFile=dir..'/animation_sheet.png'}
  for i=1,10 do
    local im=Image(128,128,ColorMode.RGB);im:drawSprite(check,i)
    assert(im:isEqual(images[i]),'Aseprite export mismatch')
    local tile=Image(reloaded,Rectangle(((i-1)%5)*128,math.floor((i-1)/5)*128,128,128))
    assert(im:isEqual(tile),'Atlas export mismatch')
    for p=0,127 do assert(pc.rgbaA(im:getPixel(p,0))==0 and pc.rgbaA(im:getPixel(p,127))==0)
      assert(pc.rgbaA(im:getPixel(0,p))==0 and pc.rgbaA(im:getPixel(127,p))==0) end
  end
  check:close();spr:close()
  local report={id=unit.Id,action=action,passed=true,frames=10,distinctFrames=distinct,cycleMs=elapsed,
    releaseFrame=action=='Attack' and 5 or nil,releaseMs=action=='Attack' and 160 or nil,
    frameSize=128,columns=5,rows=2,width=640,height=256,pivot={x=0.5,y=0.5},
    facing='right',camera='orthographic-top-down',framesData=frames,transparentEdges=true,sourceScale=scale}
  write(dir..'/animation.json',report);print(json.encode({id=unit.Id,action=action,passed=true,distinctFrames=distinct}))
  return report
end
local reports={}
for _,unit in ipairs(units) do if not only or unit.Id==only then
  local sourcePath=root..'/sources/'..unit.Id..'.png';local f=io.open(sourcePath,'rb')
  if f then f:close()
    local ok,result=pcall(function()
      local enemy=unit.Kind=='Enemy';local rows=enemy and 4 or 2
      local source=Image{fromFile=sourcePath};hardAlpha(source)
      local yCounts={};for y=0,source.height-1 do local n=0;for x=0,source.width-1 do if visible(source:getPixel(x,y),enemy) then n=n+1 end end;yCounts[y+1]=n end
      local ys=cuts(yCounts,rows);local poses,coords={},{};local extent=0
      for row=1,rows do
        local xCounts={};for x=0,source.width-1 do local n=0;for y=ys[row],ys[row+1]-1 do if visible(source:getPixel(x,y),enemy) then n=n+1 end end;xCounts[x+1]=n end
        local xs=cuts(xCounts,5)
        for col=1,5 do
          local tile=Image(source,Rectangle(xs[col],ys[row],xs[col+1]-xs[col],ys[row+1]-ys[row]))
          local b=bounds(tile,enemy);local left=math.max(0,b.x-2);local top=math.max(0,b.y-2)
          local right=math.min(tile.width,b.x+b.w+2);local bottom=math.min(tile.height,b.y+b.h+2)
          local im=Image(tile,Rectangle(left,top,right-left,bottom-top));local ax,ay=anchor(im,unit.Id,enemy)
          poses[#poses+1]=im;coords[#coords+1]={x=ax,y=ay};extent=math.max(extent,im.width,im.height)
        end
      end
      local neutralExtent=0;for i=1,10 do neutralExtent=math.max(neutralExtent,poses[i].width,poses[i].height) end;local scale=81/neutralExtent
      for i,im in ipairs(poses) do local a=coords[i];scale=math.min(scale,56/math.max(1,a.x),56/math.max(1,im.width-a.x),56/math.max(1,a.y),56/math.max(1,im.height-a.y)) end
      if enemy then reports[#reports+1]=export(unit,poses,coords,'Move',0,scale) end
      reports[#reports+1]=export(unit,poses,coords,'Attack',enemy and 10 or 0,scale)
      return true
    end)
    if not ok then print('ERROR '..unit.Id..' '..tostring(result));reports[#reports+1]={id=unit.Id,passed=false,error=tostring(result)} end
  end
end end
write(root..'/native-validation'..(only and ('-'..only) or '')..'.json',reports)
for _,r in ipairs(reports) do assert(r.passed,'Packing failed: '..r.id) end

