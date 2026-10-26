local f=assert(io.open(app.params.plan,'r'));local entries=json.decode(f:read('*a'));f:close()
local pc=app.pixelColor
for _,entry in ipairs(entries) do
 local atlas=Image{fromFile=entry.source}
 for _,g in ipairs(entry.glyphs) do
  local tile=Image(entry.cell,entry.cell,ColorMode.RGB)
  for _,v in ipairs(g.pixels) do
   local x,y=v%entry.cell,math.floor(v/entry.cell)
   for _,d in ipairs({{-1,0},{1,0},{0,-1},{0,1},{1,1}}) do
    local a,b=x+d[1],y+d[2]
    if a>=0 and a<entry.cell and b>=0 and b<entry.cell then tile:drawPixel(a,b,pc.rgba(49,27,52,255)) end
   end
  end
  for _,v in ipairs(g.pixels) do
   local x,y=v%entry.cell,math.floor(v/entry.cell)
   tile:drawPixel(x,y,entry.native==12 and y>=11 and pc.rgba(255,218,155,255) or pc.rgba(255,246,215,255))
  end
  atlas:drawImage(tile,Point((g.index%56)*entry.cell,math.floor(g.index/56)*entry.cell))
 end
 atlas:saveAs(entry.target)
 local s=Sprite(atlas.width,atlas.height,ColorMode.RGB)
 s:assignColorSpace(ColorSpace{sRGB=true});s:newCel(s.layers[1],1,atlas,Point(0,0))
 s:saveAs(entry.target:gsub('%.png$','.aseprite'));s:close()
 -- Score details use the same glyph pixels without the outline, for clean dark ink on cream.
 local flat=Image(atlas)
 for it in flat:pixels() do
  local c=it()
  if pc.rgbaA(c)>0 then
   if pc.rgbaR(c)==49 and pc.rgbaG(c)==27 and pc.rgbaB(c)==52 then it(0)
   else it(pc.rgba(255,255,255,255)) end
  end
 end
 local target=entry.target:gsub('评分补全字图','结算净字图')
 flat:saveAs(target)
 local source=Sprite(flat.width,flat.height,ColorMode.RGB);source:assignColorSpace(ColorSpace{sRGB=true})
 source:newCel(source.layers[1],1,flat,Point(0,0));source:saveAs(target:gsub('%.png$','.aseprite'));source:close()
end
print('Extended both PNG glyph atlases without changing existing glyph pixels')
