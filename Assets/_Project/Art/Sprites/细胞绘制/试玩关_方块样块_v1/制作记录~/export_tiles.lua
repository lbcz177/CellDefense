-- Technical export only: all tissue artwork comes from the generated source PNGs.
local output=assert(app.params.output)
local f=assert(io.open(output.."/制作记录~/prompts.json","r"))
local config=json.decode(f:read("*a")); f:close()
local pc=app.pixelColor
local palette={
 {247,156,128},{251,168,139},{253,180,150},{242,146,120},
 {250,132,109},{248,119,103},{241,104,99},{225,85,92},
 {204,74,99},{231,136,110},{240,169,140},{242,188,157},
 {245,204,175},{245,221,196},{246,231,216},{253,239,222}
}
local baseColor=pc.rgba(palette[1][1],palette[1][2],palette[1][3],255)
local tiles,reports={},{}
for index,job in ipairs(config.jobs) do
 local source=Image{fromFile=job.sourcePath}
 local originalWidth,originalHeight=source.width,source.height
 source:resize(128,128)
 local tile=Image(128,128,ColorMode.RGB)
 local colors={}
 for y=0,127 do
  for x=0,127 do
   local p=source:getPixel(x,y)
   local r,g,b=pc.rgbaR(p),pc.rgbaG(p),pc.rgbaB(p)
   local best,bestDistance=1,math.huge
   for n,c in ipairs(palette) do
    local distance=(r-c[1])^2+(g-c[2])^2+(b-c[3])^2
    if distance<bestDistance then best,bestDistance=n,distance end
   end
   local c=palette[best]
   local q=pc.rgba(c[1],c[2],c[3],255)
   -- All three tiles use the same connector colour; no transparent seams.
   if x<3 or y<3 or x>124 or y>124 then q=baseColor end
   tile:drawPixel(x,y,q)
   colors[tostring(q)]=true
  end
 end
 tile:saveAs(output.."/"..job.filename)
 local editable=Sprite(128,128,ColorMode.RGB)
 editable:assignColorSpace(ColorSpace{sRGB=true})
 editable.layers[1].name=job.id
 editable:deleteCel(editable.layers[1],1)
 editable:newCel(editable.layers[1],1,tile,Point(0,0))
 editable.gridBounds=Rectangle(0,0,128,128)
 editable:saveAs(output.."/制作记录~/"..job.id..".aseprite")
 editable:close()
 local n=0; for _ in pairs(colors) do n=n+1 end
 local opaque=true; local edgeMismatch=0
 for y=0,127 do for x=0,127 do
  local p=tile:getPixel(x,y)
  if pc.rgbaA(p)~=255 then opaque=false end
  if (x==0 or y==0 or x==127 or y==127) and p~=baseColor then edgeMismatch=edgeMismatch+1 end
 end end
 assert(opaque and edgeMismatch==0,"Tile export validation failed")
 reports[index]={file=job.filename,width=128,height=128,colors=n,opaque=opaque,
   edgeMismatch=edgeMismatch,sourceWidth=originalWidth,sourceHeight=originalHeight}
 tiles[index]=tile
end
local layout={{1,2,1},{3,1,2},{1,3,1}}
local preview=Image(384,384,ColorMode.RGB)
for row=1,3 do for col=1,3 do
 preview:drawImage(tiles[layout[row][col]],Point((col-1)*128,(row-1)*128))
end end
preview:resize(1152,1152)
preview:saveAs(output.."/制作记录~/preview_3x3.png")
local strip=Image(384,128,ColorMode.RGB)
for index,tile in ipairs(tiles) do strip:drawImage(tile,Point((index-1)*128,0)) end
strip:resize(1152,384)
strip:saveAs(output.."/制作记录~/preview_samples.png")
local report={method="Built-in image_gen artwork; Aseprite nearest-neighbor resize, shared 16-color palette, common 3-pixel edge margin, opaque PNG export",tiles=reports,previewLayout=layout,pixelsPerUnit=128}
local reportFile=assert(io.open(output.."/制作记录~/validation.json","w"));reportFile:write(json.encode(report));reportFile:close()
print(json.encode(report))

