local root=assert(app.params.root)
local pc=app.pixelColor
local function rgb(r,g,b) return pc.rgba(r,g,b,255) end
local plum=rgb(66,29,53)
local gold=rgb(255,205,89)
local cream=rgb(255,246,207)
local function save(im,name)
 im:saveAs(root..'/'..name..'.png')
 local s=Sprite(im.width,im.height,ColorMode.RGB)
 s:assignColorSpace(ColorSpace{sRGB=true})
 s.layers[1].name=name
 s:newCel(s.layers[1],1,im,Point(0,0))
 s:saveAs(root..'/'..name..'.aseprite');s:close()
end
local shape={
 '0000000110000000', '0000001111000000', '0000001111000000', '0000011111100000',
 '0111111111111110', '1111111111111111', '0111111111111110', '0011111111111100',
 '0001111111111000', '0000111111110000', '0001111111111000', '0001111111111000',
 '0011111001111100', '0011110000111100', '0111100000011110', '0110000000000110'}
local function inside(x,y)
 return x>=0 and x<16 and y>=0 and y<16 and shape[y+1]:sub(x+1,x+1)=='1'
end
local atlas=Image(36,18,ColorMode.RGB)
for state=0,1 do
 for y=0,15 do for x=0,15 do if inside(x,y) then
  local edge=not inside(x-1,y) or not inside(x+1,y) or not inside(x,y-1) or not inside(x,y+1)
  local c
  if state==0 then
   c=edge and plum or (y<7 and rgb(255,224,116) or (y<10 and gold or rgb(242,160,62)))
   if not edge and x>=7 and x<=8 and y>=2 and y<=6 then c=cream end
   if not edge and x>=3 and x<=5 and y==5 then c=cream end
  else
   c=edge and rgb(145,102,108) or (y<8 and rgb(215,182,161) or rgb(193,155,143))
  end
  atlas:drawPixel(state*18+x+1,y+1,c)
 end end end
end
save(atlas,'评分星星图集')
local ribbon=Image(128,24,ColorMode.RGB)
local function rect(x0,y0,x1,y1,c)
 for y=y0,y1 do for x=x0,x1 do ribbon:drawPixel(x,y,c) end end
end
rect(3,1,124,22,plum);rect(1,3,126,20,plum)
rect(4,2,123,20,gold);rect(2,4,125,19,gold)
rect(5,3,122,19,rgb(90,43,67));rect(3,5,124,18,rgb(90,43,67))
rect(6,4,121,18,plum);rect(4,6,123,17,plum)
rect(6,3,121,3,cream);rect(3,6,3,15,cream)
rect(6,20,122,20,rgb(185,103,72))
-- Small bevels keep the middle clear for live score digits.
rect(5,5,7,5,rgb(134,72,89));rect(120,17,122,17,rgb(134,72,89))
save(ribbon,'本局得分底板')
print('Exported two hard-alpha pixel assets and editable Aseprite sources')
