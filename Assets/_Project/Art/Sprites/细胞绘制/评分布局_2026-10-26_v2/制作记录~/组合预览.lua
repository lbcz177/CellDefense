-- Artwork composition reference; this is not a Unity runtime screenshot.
local f=assert(io.open(app.params.plan,'r'));local p=json.decode(f:read('*a'));f:close()
local pc=app.pixelColor
local glyphAtlas=Image{fromFile=p.art..'/结算净字图_12px.png'};local indices={}
local n=0;for _,c in utf8.codes(p.characters) do indices[c]=n;n=n+1 end
local function picture(out,file,cx,cy,w,h)
 local im=Image{fromFile=file};local scale=math.min(w/im.width,h/im.height)
 im:resize(math.floor(im.width*scale+.5),math.floor(im.height*scale+.5))
 out:drawImage(im,Point(math.floor(cx-im.width/2+.5),math.floor(cy-im.height/2+.5)))
end
local function label(out,text,cx,cy,size,tint)
 tint=tint or pc.rgba(66,28,54,255)
 local width=0;local scale=size/12
 for _,c in utf8.codes(text) do width=width+p.advances[assert(indices[c])+1]*scale end
 local x=cx-width/2;local y=cy-size*(4/3)/2-size/12
 for _,c in utf8.codes(text) do
  local index=indices[c];local tile=Image(glyphAtlas,Rectangle((index%56)*18,math.floor(index/56)*18,18,18))
  for it in tile:pixels() do if pc.rgbaA(it())>0 then it(tint) end end
  tile:resize(math.floor(18*scale+.5),math.floor(18*scale+.5))
  out:drawImage(tile,Point(math.floor(x-3*scale+.5),math.floor(y+.5)))
  x=x+p.advances[index+1]*scale
 end
end
local function ribbon(out,cx,cy)
 local im=Image{fromFile=p.art..'/本局得分底板.png'}
 local xs={0,8,120,128};local ys={0,8,16,24}
 local dx={cx-122,cx-114,cx+114,cx+122};local dy={cy-22,cy-14,cy+14,cy+22}
 for row=1,3 do for col=1,3 do
  local part=Image(im,Rectangle(xs[col],ys[row],xs[col+1]-xs[col],ys[row+1]-ys[row]))
  part:resize(dx[col+1]-dx[col],dy[row+1]-dy[row]);out:drawImage(part,Point(dx[col],dy[row]))
 end end
end
local function action(out,text,cx,cy)
 local im=Image{fromFile=p.buttonFrame}
 local xs={0,56,im.width-56,im.width};local ys={0,24,im.height-24,im.height}
 local dx={cx-130,cx-102,cx+102,cx+130};local dy={cy-26,cy-14,cy+14,cy+26}
 for row=1,3 do for col=1,3 do
  local part=Image(im,Rectangle(xs[col],ys[row],xs[col+1]-xs[col],ys[row+1]-ys[row]))
  part:resize(dx[col+1]-dx[col],dy[row+1]-dy[row]);out:drawImage(part,Point(dx[col],dy[row]))
 end end
 label(out,text,cx,cy,24)
end
for _,config in ipairs({{name='满分三星',win=true,score=10000,kills=4000,core=6000,best=10000,stars=3},
 {name='二星通关',win=true,score=7600,kills=4000,core=3600,best=9300,stars=2},
 {name='首次失败',win=false,score=0,kills=0,core=0,stars=0}}) do
 local out=Image{fromFile=p.background};local cy=360+(config.win and 0 or 28)
 picture(out,config.win and p.victory or p.defeat,640,360,600,600)
 ribbon(out,582,cy-12)
 label(out,'得分',500,cy-12,16,pc.rgba(255,245,209,255));label(out,'/ 10000',665,cy-10,14,pc.rgba(237,199,166,255))
 local str=tostring(config.score)
 for i=1,#str do picture(out,p.digits[tonumber(str:sub(i,i))+1],579+(i-1-(#str-1)/2)*18,cy-12,18,32) end
 local stars=Image{fromFile=p.art..'/评分星星图集.png'};local sz=102/3.5
 for i=0,2 do
  local tile=Image(stars,Rectangle(i<config.stars and 0 or 18,0,18,18));tile:resize(math.floor(sz+.5),math.floor(sz+.5))
  out:drawImage(tile,Point(math.floor(770-sz*1.75+i*sz*1.25+.5),math.floor(cy-12-sz/2+.5)))
 end
 picture(out,p.kill,472,cy+24,22,22);picture(out,p.life,658,cy+24,22,22)
 label(out,'击杀 '..config.kills..'/4000',560,cy+24,16);label(out,'核心 '..config.core..'/6000',746,cy+24,16)
 local buttons=config.win and {'下一关','重新开始','返回主菜单'} or {'重新开始','返回主菜单'}
 for i,text in ipairs(buttons) do action(out,text,640,360+(config.win and (82+(i-1)*62) or (116+(i-1)*70))) end
 local file=p.output..'/评分组合参考_'..config.name
 out:saveAs(file..'.png')
 local sprite=Sprite(out.width,out.height,ColorMode.RGB);sprite:newCel(sprite.layers[1],1,out,Point(0,0));sprite:saveAs(file..'.aseprite');sprite:close()
end
print('Three artwork composition references saved; not runtime screenshots')
