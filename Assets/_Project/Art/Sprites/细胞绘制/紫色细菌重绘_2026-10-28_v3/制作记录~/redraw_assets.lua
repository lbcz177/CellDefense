local root=app.params.output
local source=Image{fromFile=app.params.source}
local plate=Image{fromFile=app.params.plate}
plate:resize(source.width,source.height)
local pc=app.pixelColor
local bounds={{0,1440,1580},{75,1430,1590},{150,1330,1540},{230,1210,1435},{310,1120,1350},{400,1000,1250},{480,895,1150},{540,875,1080},{610,885,1090},{680,865,1080},{750,725,980},{830,610,840},{940,520,720}}
local result=Image(source)
for y=0,source.height-1 do
  local left,right=0,0
  for i=2,#bounds do if y<=bounds[i][1] then
    local a,b=bounds[i-1],bounds[i];local f=(y-a[1])/(b[1]-a[1])
    left=a[2]+(b[2]-a[2])*f;right=a[3]+(b[3]-a[3])*f;break
  end end
  for x=math.max(0,math.floor(left)),math.min(source.width-1,math.ceil(right)) do
    local c=source:getPixel(x,y);local r,g,b=pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c)
    local clean=plate:getPixel(x,y);local cr,cg,cb=pc.rgbaR(clean),pc.rgbaG(clean),pc.rgbaB(clean)
    local purple=r>5 and b>15 and g<r*.8 and b>r*.55 and b>g*1.15
    local cleanPurple=cr>5 and cb>15 and cg<cr*.8 and cb>cr*.55 and cb>cg*1.2
    local foreground=(g>r*.95 and b>r*1.2) or (r>180 and g>160 and b>150)
      or (r>180 and g>120 and b<120) or (g>80 and g>r*.8 and b<100)
    if (purple or cleanPurple) and not foreground then result:drawPixel(x,y,clean) end
  end
end
result:saveAs(root..'/胞卫战_主界面_v1.png')
local atlas=Image{fromFile=app.params.atlas}
assert(atlas.width==atlas.height*3,'Expected a 3-cell horizontal atlas')
local names={'紫色细菌_球形_重绘','紫色细菌_杆状_重绘','紫色细菌_小球_重绘'}
for i,name in ipairs(names) do
  local im=Image(atlas.height,atlas.height,ColorMode.RGB)
  im:drawImage(atlas,Point(-(i-1)*atlas.height,0))
  im:resize(128,128)
  local visible=0
  for y=0,127 do for x=0,127 do
    local c=im:getPixel(x,y)
    if pc.rgbaA(c)<128 then im:drawPixel(x,y,0)
    else
      im:drawPixel(x,y,pc.rgba(pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c),255));visible=visible+1
      assert(x>4 and x<123 and y>4 and y<123,'Sprite touches canvas edge')
    end
  end end
  assert(visible>1000,'Empty sprite')
  im:saveAs(root..'/'..name..'.png')
  local sprite=Sprite(128,128,ColorMode.RGB)
  sprite.layers[1].name='独立重绘_完整透明细菌'
  sprite:newCel(sprite.layers[1],1,im,Point(0,0))
  sprite:saveAs(root..'/源文件/'..name..'.aseprite');sprite:close()
  print(name..' visible='..visible)
end
local sprite=Sprite(source.width,source.height,ColorMode.RGB)
sprite:newCel(sprite.layers[1],1,result,Point(0,0))
sprite:saveAs(root..'/源文件/主界面_通道清理.aseprite');sprite:close()
