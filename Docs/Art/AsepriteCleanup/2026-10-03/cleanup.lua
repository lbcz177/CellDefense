local work=assert(app.params.work)
local function readJSON(path)
 local f=assert(io.open(path,"r"));local v=json.decode(f:read("*a"));f:close();return v
end
local function writeJSON(path,v)
 local f=assert(io.open(path,"w"));f:write(json.encode(v));f:close()
end
local plan=readJSON(work.."/plan.json")
local only=app.params.ids or ""
local pc=app.pixelColor
local function fullImage(sprite,frame)
 local image=Image(sprite.width,sprite.height,ColorMode.RGB)
 image:drawSprite(sprite,frame or 1)
 return image
end
local function rgbDiff(a,b)
 return math.max(math.abs(pc.rgbaR(a)-pc.rgbaR(b)),math.abs(pc.rgbaG(a)-pc.rgbaG(b)),math.abs(pc.rgbaB(a)-pc.rgbaB(b)))
end
for _,job in ipairs(plan.jobs) do
 if only=="" or string.find(","..only..",",","..job.id..",",1,true) then
 local sprite=assert(app.open(job.input))
 if sprite.colorMode~=ColorMode.RGB then app.command.ChangePixelFormat{format="rgb"} end
 local original=fullImage(sprite)
 local filtered=Image(original)
 if job.medianLimit>0 then
  local kernel=(job.kind=="unit" or job.kind=="icon") and 5 or 3
  app.command.Despeckle{ui=false,width=kernel,height=kernel,channels=7}
  local proposed=fullImage(sprite)
  for it in filtered:pixels() do
   local p=it()
   if pc.rgbaA(p)>=240 then
    local q=proposed:getPixel(it.x,it.y)
    if rgbDiff(p,q)<=job.medianLimit then it(pc.rgba(pc.rgbaR(q),pc.rgbaG(q),pc.rgbaB(q),pc.rgbaA(p))) end
   end
  end
 end
 -- Keep the source palette and shading. Hard global quantization amplified
 -- near-threshold grain in the trial, so the final repair is spatial only.
 local reduced=filtered
 local result=Image(original)
 local visible,changed,totalError,maximumError=0,0,0,0
 local colorsBefore,colorsAfter,beforeCount,afterCount={},{},0,0
 for it in result:pixels() do
  local p=original:getPixel(it.x,it.y)
  local a=pc.rgbaA(p)
  if a>=240 then
   local q=reduced:getPixel(it.x,it.y)
   if pc.rgbaA(q)>0 then
    local dr,dg,db=pc.rgbaR(q)-pc.rgbaR(p),pc.rgbaG(q)-pc.rgbaG(p),pc.rgbaB(q)-pc.rgbaB(p)
    -- Keep unusual accents rather than forcing a bad palette match.
    if math.max(math.abs(dr),math.abs(dg),math.abs(db))<=12 then
     it(pc.rgba(pc.rgbaR(q),pc.rgbaG(q),pc.rgbaB(q),a))
    else
     it(filtered:getPixel(it.x,it.y))
    end
   end
  end
  local q=it()
  assert(pc.rgbaA(q)==a,"Alpha channel changed")
  if a>=240 then
   visible=visible+1
   local diff=rgbDiff(p,q)
   if diff>0 then changed=changed+1 end
   totalError=totalError+(math.abs(pc.rgbaR(p)-pc.rgbaR(q))+math.abs(pc.rgbaG(p)-pc.rgbaG(q))+math.abs(pc.rgbaB(p)-pc.rgbaB(q)))/3
   maximumError=math.max(maximumError,diff)
   local pkey=p & 0xffffff; local qkey=q & 0xffffff
   if beforeCount<50000 and not colorsBefore[pkey] then colorsBefore[pkey]=true;beforeCount=beforeCount+1 end
   if afterCount<50000 and not colorsAfter[qkey] then colorsAfter[qkey]=true;afterCount=afterCount+1 end
  end
 end
 assert(result.width==job.width and result.height==job.height)
 result:saveAs(job.output)
 sprite:close()
 local loaded=Image{fromFile=job.output}
 for y=0,original.height-1 do for x=0,original.width-1 do
  assert(pc.rgbaA(original:getPixel(x,y))==pc.rgbaA(loaded:getPixel(x,y)),"Saved PNG alpha mismatch")
 end end
 local report={id=job.id,relative=job.relative,kind=job.kind,width=result.width,height=result.height,
  method="Aseprite RGB median proposal with per-pixel edge guard; original alpha preserved",medianLimit=job.medianLimit,opaquePixels=visible,changedPixels=changed,
  meanChannelError=totalError/math.max(visible,1),maxChannelError=maximumError,
  opaqueColorsBefore=beforeCount,opaqueColorsAfter=afterCount,alphaUnchanged=true}
 writeJSON(work.."/reports/"..job.id..".json",report)
 print(json.encode(report))
 end
end

