local folder=app.params.folder
local f=assert(io.open(folder..'/制作记录~/30x22_逻辑布局与连通检查.json','r'))
local p=json.decode(f:read('*a'));f:close()
local cell=24
local im=Image(p.width*cell,p.height*cell,ColorMode.RGB)
local pc=app.pixelColor
local function rect(x,y,w,h,c)
  for yy=y,y+h-1 do for xx=x,x+w-1 do im:drawPixel(xx,yy,c) end end
end
rect(0,0,im.width,im.height,pc.rgba(240,220,204,255))
for _,c in ipairs(p.blockedCells) do
  rect(c[1]*cell,(p.height-1-c[2])*cell,cell,cell,pc.rgba(130,96,71,255))
end
rect(p.entry[1]*cell,(p.height-1-p.entry[2])*cell,cell,cell,pc.rgba(204,91,125,255))
rect(p.core[1]*cell,(p.height-1-p.core[2])*cell,cell,cell,pc.rgba(29,160,134,255))
im:saveAs(folder..'/制作记录~/30x22_空间参考.png')
local source=app.open(folder..'/制作记录~/确认画风_生成原图_1466x1073.png')
source.layers[1].name='确认画风_棕边淡周边_原生尺寸'
source:saveAs(folder..'/源文件/2-1_确认画风_原生尺寸.aseprite');source:close()
print('Saved native baseline and 30x22 geometry guide')
