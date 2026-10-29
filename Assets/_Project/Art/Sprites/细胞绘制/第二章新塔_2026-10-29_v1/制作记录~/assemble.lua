local f=assert(io.open(app.params.plan,'r'));local plan=json.decode(f:read('*a'));f:close()
local pc=app.pixelColor
local previews={}
for _,job in ipairs(plan.jobs) do
 local raw=Image{fromFile=job.raw};local x0,y0,x1,y1=raw.width,raw.height,-1,-1
 for y=0,raw.height-1 do for x=0,raw.width-1 do
  if pc.rgbaA(raw:getPixel(x,y))>=128 then x0=math.min(x0,x);y0=math.min(y0,y);x1=math.max(x1,x);y1=math.max(y1,y) end
 end end
 assert(x1>x0 and y1>y0,'Empty generated sprite')
 local crop=Image(x1-x0+1,y1-y0+1,ColorMode.RGB);crop:drawImage(raw,Point(-x0,-y0))
 local ratio=104/math.max(crop.width,crop.height)
 crop:resize(math.floor(crop.width*ratio+.5),math.floor(crop.height*ratio+.5))
 for it in crop:pixels() do local c=it();if pc.rgbaA(c)<128 then it(0) else it(pc.rgba(pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c),255)) end end
 local final=Image(128,128,ColorMode.RGB)
 final:drawImage(crop,Point(math.floor((128-crop.width)/2),math.floor((128-crop.height)/2)))
 final:saveAs(job.sprite)
 local portrait=Image(final);portrait:resize(384,384);portrait:saveAs(job.portrait)
 local doc=Sprite(128,128,ColorMode.RGB);doc.layers[1].name=job.name..'_128像素';doc:newCel(doc.layers[1],1,final,Point(0,0));doc:saveAs(job.source);doc:close()
 previews[#previews+1]=portrait
end
local overview=Image(900,480,ColorMode.RGB)
for it in overview:pixels() do it(pc.rgba(248,233,222,255)) end
for i,im in ipairs(previews) do overview:drawImage(im,Point(33+(i-1)*450,48)) end
overview:saveAs(plan.overview)
print('Exported two 128px gameplay sprites, 384px portraits, native sources and overview.')
