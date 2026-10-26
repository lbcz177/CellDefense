local root=assert(app.params.root)
local pc=app.pixelColor
local names={'评分星星图集','本局得分底板','评分补全字图_12px','评分补全字图_8px','结算净字图_12px','结算净字图_8px'}
local results={}
for _,name in ipairs(names) do
 local png=Image{fromFile=root..'/'..name..'.png'}
 local source=assert(app.open(root..'/'..name..'.aseprite'))
 local im=Image(source.width,source.height,ColorMode.RGB);im:drawSprite(source,1)
 assert(im:isEqual(png),'Source/export mismatch: '..name)
 local transparent,ink=0,0
 for it in png:pixels() do
  local alpha=pc.rgbaA(it());assert(alpha==0 or alpha==255,'Soft alpha in '..name)
  if alpha==0 then transparent=transparent+1 else ink=ink+1 end
 end
 assert(transparent>0 and ink>0,'Empty or opaque sheet: '..name)
 results[#results+1]={name=name,width=png.width,height=png.height,sourceMatchesPng=true,hardAlpha=true}
 source:close()
end
local f=assert(io.open(root..'/制作记录~/Aseprite素材校验.json','w'));f:write(json.encode(results));f:close()
print('PASS: six editable sources match exported PNGs; binary alpha verified')
