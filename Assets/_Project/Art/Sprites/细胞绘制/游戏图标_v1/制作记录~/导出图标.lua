local root=assert(app.params.root)
local input=Image{fromFile=root..'/制作记录~/原始生成图.png'}
local sourceW,sourceH=input.width,input.height
assert(input.width==input.height,'Expected square icon')
-- Normalize the generated artwork to one editable 128px pixel grid.
input:resize(128,128)
local s=Sprite(128,128,ColorMode.RGB)
s:assignColorSpace(ColorSpace{sRGB=true})
s.layers[1].name='游戏图标'
s:newCel(s.layers[1],1,input)
s:saveAs(root..'/胞卫战_游戏图标.aseprite')
for _,n in ipairs({16,32,48,64,128,256,512,1024}) do
 local output=Image(input)
 output:resize(n,n)
 output:saveAs(root..'/导出尺寸/icon_'..n..'.png')
 if n==1024 then output:saveAs(root..'/胞卫战_游戏图标_1024.png') end
end
-- Native pixel-size contact sheet, shown at nearest-neighbor 3x for inspection.
local overview=Image(384,176,ColorMode.RGB)
overview:clear(app.pixelColor.rgba(41,32,52,255))
local x=8
for _,n in ipairs({16,32,48,64,128}) do
 local im=Image(input);im:resize(n,n)
 overview:drawImage(im,Point(x,math.floor((176-n)/2)))
 x=x+n+12
end
overview:resize(1152,528)
overview:saveAs(root..'/制作记录~/小尺寸检查.png')
local f=assert(io.open(root..'/制作记录~/Aseprite制作记录.json','w'))
f:write(json.encode({tool='built-in image_gen + Aseprite 1.3.18.6',date='2026-10-19',sourceWidth=sourceW,sourceHeight=sourceH,nativeSize=128,exports={16,32,48,64,128,256,512,1024},scaling='nearest neighbor',background='opaque square',source='胞卫战_游戏图标.aseprite'}))
f:close()
s:close()
print('OK: square game icon, Aseprite source and eight sizes saved.')