local root=app.params.output
local source=Image{fromFile=app.params.source}
local plate=Image{fromFile=app.params.plate}
plate:resize(source.width,source.height)
local pc=app.pixelColor
local bounds={{0,1440,1580},{75,1430,1590},{150,1330,1540},{230,1210,1435},{310,1120,1350},{400,1000,1250},{480,895,1150},{540,875,1080},{610,885,1090},{680,865,1080},{750,725,980},{830,610,840},{940,520,720}}
local result=Image(source)
local changed=0
for y=0,source.height-1 do
  local left,right=0,0
  for i=2,#bounds do if y<=bounds[i][1] then
    local a,b=bounds[i-1],bounds[i];local f=(y-a[1])/(b[1]-a[1])
    left=a[2]+(b[2]-a[2])*f;right=a[3]+(b[3]-a[3])*f;break
  end end
  for x=math.max(0,math.floor(left)),math.min(source.width-1,math.ceil(right)) do
    local c=source:getPixel(x,y);local r,g,b=pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c)
    local clean=plate:getPixel(x,y)
    local cr,cg,cb=pc.rgbaR(clean),pc.rgbaG(clean),pc.rgbaB(clean)
    local originalPurple=r>15 and b>35 and g<r*.7 and b>r*.55 and b>g*1.25
    local cleanPurple=cr>15 and cb>35 and cg<cr*.8 and cb>cr*.55 and cb>cg*1.2
    local foreground=(g>r*.95 and b>r*1.2) or (r>180 and g>160 and b>150)
      or (r>180 and g>120 and b<120) or (g>r*.8 and b<100)
    if (originalPurple or cleanPurple) and not foreground then
      result:drawPixel(x,y,plate:getPixel(x,y));changed=changed+1
    end
  end
end
result:saveAs(root..'/胞卫战_主界面_v1.png')
local specs={{'紫色细菌_球形',1087,290,133,122,66,60},
             {'紫色细菌_杆状',906,452,106,127,49,65},
             {'紫色细菌_小球',1018,389,97,104,47,49}}
local function key(x,y,w) return y*w+x end
for _,s in ipairs(specs) do
  local name,ox,oy,w,h,cx,cy=table.unpack(s)
  local outside={};local queue={};local head=1
  local function barrier(x,y)
    local c=source:getPixel(ox+x,oy+y)
    local r,g,b=pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c)
    return g<45 and b>35 and b>r*.5 and b>g*1.5
  end
  local function visit(x,y)
    if x<0 or y<0 or x>=w or y>=h then return end
    local k=key(x,y,w)
    if not outside[k] and not barrier(x,y) then outside[k]=true;queue[#queue+1]={x,y} end
  end
  for x=0,w-1 do visit(x,0);visit(x,h-1) end
  for y=0,h-1 do visit(0,y);visit(w-1,y) end
  while head<=#queue do
    local p=queue[head];head=head+1
    visit(p[1]+1,p[2]);visit(p[1]-1,p[2]);visit(p[1],p[2]+1);visit(p[1],p[2]-1)
  end
  local connected={};queue={{cx,cy}};head=1;connected[key(cx,cy,w)]=true
  while head<=#queue do
    local p=queue[head];head=head+1
    for _,d in ipairs({{1,0},{-1,0},{0,1},{0,-1}}) do
      local x,y=p[1]+d[1],p[2]+d[2];local k=key(x,y,w)
      if x>=0 and y>=0 and x<w and y<h and not outside[k] and not connected[k] then
        connected[k]=true;queue[#queue+1]={x,y}
      end
    end
  end
  local im=Image(w,h,ColorMode.RGB);local count=0
  for y=0,h-1 do for x=0,w-1 do if connected[key(x,y,w)] then
    im:drawPixel(x,y,source:getPixel(ox+x,oy+y));count=count+1
  end end end
  assert(count>800,'Empty bacteria cutout '..name)
  im:saveAs(root..'/'..name..'.png')
  local sprite=Sprite(w,h,ColorMode.RGB);sprite:newCel(sprite.layers[1],1,im,Point(0,0))
  sprite:saveAs(root..'/源文件/'..name..'.aseprite');sprite:close()
  print(name..' pixels='..count)
end
local sprite=Sprite(source.width,source.height,ColorMode.RGB)
sprite.layers[1].name='保留原图_仅通道紫色像素替换'
sprite:newCel(sprite.layers[1],1,result,Point(0,0))
sprite:saveAs(root..'/源文件/主界面_分离细菌.aseprite');sprite:close()
print('Channel pixels replaced: '..changed)
