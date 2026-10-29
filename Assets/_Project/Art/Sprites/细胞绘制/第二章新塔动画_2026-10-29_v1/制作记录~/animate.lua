local f=assert(io.open(app.params.plan,'r'));local p=json.decode(f:read('*a'));f:close()
local pc=app.pixelColor
local function bounds(im)
 local b={x=im.width,y=im.height,r=-1,b=-1}
 for it in im:pixels() do if pc.rgbaA(it())>=128 then b.x=math.min(b.x,it.x);b.y=math.min(b.y,it.y);b.r=math.max(b.r,it.x);b.b=math.max(b.b,it.y) end end
 assert(b.r>b.x,'Empty frame');return b
end
-- Generated grids have uneven gutters. Split at the largest transparent gaps, as in the original ten-frame packer.
local function cuts(counts,n)
 if n==1 then return {0,#counts} end
 local bands,start={},nil
 for i=1,#counts+1 do
  local on=i<=#counts and counts[i]>2
  if on and not start then start=i-1 end
  if not on and start then
   if #bands>0 and start-bands[#bands].b<=8 then bands[#bands].b=i-2 else bands[#bands+1]={a=start,b=i-2} end
   start=nil
  end
 end
 assert(#bands>=n,'Insufficient distinct frame bands')
 local gaps={};for i=1,#bands-1 do gaps[#gaps+1]={size=bands[i+1].a-bands[i].b,at=math.floor((bands[i+1].a+bands[i].b)/2)} end
 table.sort(gaps,function(a,b)return a.size>b.size end)
 local result={0};for i=1,n-1 do result[#result+1]=gaps[i].at end;result[#result+1]=#counts;table.sort(result);return result
end
local actionMs={50,40,40,50,40,65,40,55,70,100}
local contactMs={30,40,45,55,65,75}
local all={};local reports={}
for _,j in ipairs(p.jobs) do
 local raw=Image{fromFile=j.raw};local ysCount={}
 for y=0,raw.height-1 do local n=0;for x=0,raw.width-1 do if pc.rgbaA(raw:getPixel(x,y))>=128 then n=n+1 end end;ysCount[y+1]=n end
 local ys=cuts(ysCount,j.rows);local poses={};local maxExtent=0
 for row=1,j.rows do
  local xsCount={};for x=0,raw.width-1 do local n=0;for y=ys[row],ys[row+1]-1 do if pc.rgbaA(raw:getPixel(x,y))>=128 then n=n+1 end end;xsCount[x+1]=n end
  local xs=cuts(xsCount,j.columns)
  for col=1,j.columns do
   local im=Image(xs[col+1]-xs[col],ys[row+1]-ys[row],ColorMode.RGB);im:drawImage(raw,Point(-xs[col],-ys[row]))
   local b=bounds(im);local cx,cy=(b.x+b.r+1)/2,(b.y+b.b+1)/2
   if j.count==10 then
    local sx,sy,n=0,0,0
    for it in im:pixels() do
     local c=it();local r,g,blue=pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c)
     local hit=j.id=='platelet_action' and r>g*1.10 and r>blue*1.3 or j.id=='complement_action' and g>r*1.3 and blue>r*1.3
     if pc.rgbaA(c)>=128 and hit then sx=sx+it.x+.5;sy=sy+it.y+.5;n=n+1 end
    end
    if n>20 then cx=sx/n;cy=sy/n end
   end
   maxExtent=math.max(maxExtent,cx-b.x,b.r+1-cx,cy-b.y,b.b+1-cy)
   poses[#poses+1]={im=im,cx=cx,cy=cy,rawBounds=b}
  end
 end
 assert(#poses==j.count);local scale=55/maxExtent
 local doc=Sprite(128,128,ColorMode.RGB);doc.layers[1].name=j.name;doc:assignColorSpace(ColorSpace{sRGB=true})
 local atlas=Image(j.columns*128,j.rows*128,ColorMode.RGB);local images,frames={},{}
 for i,pose in ipairs(poses) do
  local im=Image(pose.im);im:resize(math.max(1,math.floor(im.width*scale+.5)),math.max(1,math.floor(im.height*scale+.5)))
  local frame=Image(128,128,ColorMode.RGB);frame:drawImage(im,Point(math.floor(64-pose.cx*scale+.5),math.floor(64-pose.cy*scale+.5)))
  for it in frame:pixels() do local c=it();if pc.rgbaA(c)<128 then it(0) else it(pc.rgba(pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c),255)) end end
  local b=bounds(frame);assert(b.x>=7 and b.y>=7 and b.r<121 and b.b<121,'Unsafe frame margin')
  for _,previous in ipairs(images) do assert(not frame:isEqual(previous),'Duplicate animation pose') end
  images[#images+1]=frame
  if i>1 then doc:newEmptyFrame() end
  if doc.layers[1]:cel(i) then doc:deleteCel(doc.layers[1],i) end
  doc:newCel(doc.layers[1],i,frame,Point(0,0))
  local ms=j.count==10 and actionMs[i] or contactMs[i];doc.frames[i].duration=ms/1000
  frame:saveAs(j.directory..'/frames/frame_'..string.format('%02d',i)..'.png')
  atlas:drawImage(frame,Point(((i-1)%j.columns)*128,math.floor((i-1)/j.columns)*128))
  frames[#frames+1]={index=i,durationMs=ms,bounds=b}
 end
 if j.count==10 then doc:newTag(1,4).name='Charge';doc:newTag(5,10).name='Release' else doc:newTag(1,6).name='Contact' end
 doc:saveAs(j.directory..'/animation.aseprite');atlas:saveAs(j.directory..'/animation_sheet.png')
 doc:saveAs(j.directory..'/preview.gif');doc:close()
 local first=frames[1].bounds
 local report={id=j.id,name=j.name,distinct=j.count,sourceScale=scale,frames=frames,ppu=j.count==10 and math.max(first.r-first.x+1,first.b-first.y+1)*128/104 or 128}
 local out=assert(io.open(j.directory..'/animation.json','w'));out:write(json.encode(report));out:close()
 reports[#reports+1]=report;all[j.id]=images
end
-- Large art preview: body and target contact are shown separately; runtime uses real action clocks.
local preview=Sprite(1024,640,ColorMode.RGB);preview.layers[1].name='动作与目标特效预览'
local releaseEnds={.04,.105,.145,.20,.27,.37};local impactEnds={.03,.07,.115,.17,.235,.31}
local function phase(t,ends) for i,v in ipairs(ends) do if t<v then return i end end;return nil end
for frame=1,90 do
 local t=(frame-1)/30;local sheet=Image(1024,640,ColorMode.RGB)
 for it in sheet:pixels() do it(pc.rgba(36,37,56,255)) end
 for unit=1,2 do
  local id=unit==1 and 'platelet_action' or 'complement_action';local fx=unit==1 and 'repair_impact' or 'lysis_impact'
  local localT=t%1.5;local index=1
  if localT>=.25 and localT<.55 then index=1+math.min(3,math.floor((localT-.25)/.30*4)) end
  local releaseFrame=localT>=.55 and phase(localT-.55,releaseEnds) or nil
  if releaseFrame then index=4+releaseFrame end
  local body=Image(all[id][index]);body:resize(384,384);sheet:drawImage(body,Point(64+(unit-1)*512,0))
  local effectFrame=localT>=.55 and phase(localT-.55,impactEnds) or nil
  if effectFrame then local effect=Image(all[fx][effectFrame]);effect:resize(224,224);sheet:drawImage(effect,Point(144+(unit-1)*512,376)) end
 end
 if frame>1 then preview:newEmptyFrame() end
 if preview.layers[1]:cel(frame) then preview:deleteCel(preview.layers[1],frame) end
 preview:newCel(preview.layers[1],frame,sheet,Point(0,0));preview.frames[frame].duration=1/30
end
preview:saveAs(p.art..'/两座新塔_动作与命中预览.gif');preview:close()
local f=assert(io.open(p.art..'/制作记录~/逐帧检查.json','w'));f:write(json.encode(reports));f:close()
print('Exported 20 body poses, 12 contact poses, four native animations, atlases, frames and previews.')
