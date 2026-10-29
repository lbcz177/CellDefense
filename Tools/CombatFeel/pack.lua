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
local contactMs={30,35,45,50,60,80}
local all={};local reports={}
for _,j in ipairs(p.jobs) do
 local raw=Image{fromFile=j.raw};for it in raw:pixels() do local c=it();if pc.rgbaA(c)<210 then it(0) else it(pc.rgba(pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c),255)) end end;local ysCount={}
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

local preview=Sprite(960,640,ColorMode.RGB);preview.layers[1].name='六塔专属接触特效'
local ends={.03,.065,.11,.16,.22,.30}
for n=0,89 do
 local time=(n/30)%.9;local canvas=Image(960,640,ColorMode.RGB)
 for it in canvas:pixels() do it(pc.rgba(36,37,56,255)) end
 for i,job in ipairs(p.jobs) do
  local frame=1;while frame<=6 and time>=ends[frame] do frame=frame+1 end
  if frame<=6 then local im=Image(all[job.id][frame]);im:resize(256,256);canvas:drawImage(im,Point(32+((i-1)%3)*320,32+math.floor((i-1)/3)*320)) end
 end
 if n>0 then preview:newEmptyFrame() end
 if preview.layers[1]:cel(n+1) then preview:deleteCel(preview.layers[1],n+1) end
 preview:newCel(preview.layers[1],n+1,canvas,Point(0,0));preview.frames[n+1].duration=n%3==2 and .04 or .03
end
preview:saveAs(p.art..'/六塔专属特效_30fps.gif');preview:close()
local f=assert(io.open(p.art..'/制作记录~/逐帧检查.json','w'));f:write(json.encode(reports));f:close()
print('Exported six distinct pixel contact animations: 36 frames, Aseprite sources, atlases and overview.')
