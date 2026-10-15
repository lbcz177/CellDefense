local work=assert(app.params.work)
local f=assert(io.open(work.."/plan.json","r"));local plan=json.decode(f:read("*a"));f:close()
local pc=app.pixelColor
local ids={"001","002","003","004","005","006"}
local side=280
local function checker(width,height)
 local img=Image(width,height,ColorMode.RGB)
 for it in img:pixels() do local c=((math.floor(it.x/16)+math.floor(it.y/16))%2==0) and 228 or 241;it(pc.rgba(c,c,c,255)) end
 return img
end
local function bounds(img)
 local x0,y0,x1,y1=img.width,img.height,0,0
 for it in img:pixels() do if pc.rgbaA(it())>=32 then x0=math.min(x0,it.x);y0=math.min(y0,it.y);x1=math.max(x1,it.x);y1=math.max(y1,it.y) end end
 return Rectangle(x0,y0,x1-x0+1,y1-y0+1)
end
local comparison=checker(side*4,side*3)
for n,id in ipairs(ids) do
 local job=plan.jobs[tonumber(id)]
 local original=Image{fromFile=job.input}
 local repaired=Image{fromFile=job.output}
 local rect=bounds(original)
 original=Image(original,rect);repaired=Image(repaired,rect)
 local scale=240/math.max(rect.width,rect.height)
 local w,h=math.floor(rect.width*scale+0.5),math.floor(rect.height*scale+0.5)
 original:resize(w,h);repaired:resize(w,h)
 local pairCol=(n-1)%2;local row=math.floor((n-1)/2)
 local x,y=pairCol*side*2+math.floor((side-w)/2),row*side+math.floor((side-h)/2)
 comparison:drawImage(original,Point(x,y))
 comparison:drawImage(repaired,Point(x+side,y))
end
comparison:saveAs(work.."/comparison-six-towers.png")
-- After-only contact sheets cover every repaired source for quick visual review.
for start=1,#plan.jobs,30 do
 local sheet=checker(180*6,180*5)
 for idx=start,math.min(start+29,#plan.jobs) do
  local img=Image{fromFile=plan.jobs[idx].output}
  local rect=bounds(img);img=Image(img,rect)
  local scale=164/math.max(rect.width,rect.height)
  local w,h=math.max(1,math.floor(rect.width*scale)),math.max(1,math.floor(rect.height*scale))
  img:resize(w,h)
  local cell=idx-start
  sheet:drawImage(img,Point((cell%6)*180+math.floor((180-w)/2),math.floor(cell/6)*180+math.floor((180-h)/2)))
 end
 sheet:saveAs(work.."/contact-"..string.format("%03d",start)..".png")
end
print("Contact sheets and comparison exported.")

