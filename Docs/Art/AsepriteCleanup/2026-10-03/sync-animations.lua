local work=assert(app.params.work)
local f=assert(io.open(work.."/plan.json","r"));local plan=json.decode(f:read("*a"));f:close()
local pc=app.pixelColor
local function writeJSON(path,v) local file=assert(io.open(path,"w"));file:write(json.encode(v));file:close() end
local function sameImage(a,b)
 if a.width~=b.width or a.height~=b.height then return false end
 for y=0,a.height-1 do for x=0,a.width-1 do
  local p,q=a:getPixel(x,y),b:getPixel(x,y)
  if pc.rgbaA(p)~=pc.rgbaA(q) or (pc.rgbaA(p)>0 and p~=q) then return false end
 end end
 return true
end
local reports={}
for _,job in ipairs(plan.jobs) do if job.kind=="animation" then
 local sheet=Image{fromFile=job.output}
 assert(sheet.width==512 and sheet.height==256)
 local sprite=assert(app.open(plan.root.."/"..job.aseRelative))
 assert(sprite.width==128 and sprite.height==128 and #sprite.frames==8 and #sprite.layers==1)
 local times,frames={},{}
 for i=1,8 do
  times[i]=sprite.frames[i].duration
  local col,row=(i-1)%4,math.floor((i-1)/4)
  local frame=Image(sheet,Rectangle(col*128,row*128,128,128))
  local path=work.."/staged/"..job.framesRelative[i]
  frame:saveAs(path)
  if sprite.layers[1]:cel(i) then sprite:deleteCel(sprite.layers[1],i) end
  sprite:newCel(sprite.layers[1],i,frame,Point(0,0))
  frames[i]=frame
  assert(sameImage(frame,Image{fromFile=path}),"Saved frame does not match sheet")
 end
 local sourceTagCount=#sprite.tags
 local asePath=work.."/staged/"..job.aseRelative
 sprite:saveAs(asePath)
 sprite:close()
 local check=assert(app.open(asePath))
 assert(#check.frames==8 and #check.tags==sourceTagCount)
 local transitions={}
 for i=1,8 do
  assert(math.abs(check.frames[i].duration-times[i])<0.0001)
  local cel=check.layers[1]:cel(i)
  local actual=Image(128,128,ColorMode.RGB)
  actual:drawImage(cel.image,cel.position)
  assert(sameImage(actual,frames[i]),"Aseprite file and PNG frame differ")
  local nextIndex=i%8+1
  assert(not sameImage(frames[i],frames[nextIndex]),"Animation contains duplicate neighbouring poses")
  transitions[i]={from=i,to=nextIndex,different=true}
 end
 check:close()
 local report={id=job.id,clip=job.aseRelative,frames=8,frameWidth=128,frameHeight=128,
  sheetWidth=512,sheetHeight=256,times=times,asepriteMatchesPng=true,framesMatchSheet=true,
  neighboringFramesDistinct=true,tagCount=sourceTagCount}
 reports[#reports+1]=report
 writeJSON(work.."/reports/animation-"..job.id..".json",report)
 print(json.encode(report))
end end
writeJSON(work.."/animation-validation.json",reports)

