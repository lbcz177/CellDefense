local f=assert(io.open(app.params['plan'],'r'));local plan=json.decode(f:read('*a'));f:close()
local pc=app.pixelColor;local atlas=Image(56*14,math.ceil(#plan.smallGlyphs/56)*14,ColorMode.RGB)
for i,g in ipairs(plan.smallGlyphs) do
 local tile=Image(14,14,ColorMode.RGB)
 for _,v in ipairs(g.pixels) do local x,y=v%14,math.floor(v/14)
  for _,d in ipairs({{-1,0},{1,0},{0,-1},{0,1},{1,1}}) do local a,b=x+d[1],y+d[2]
   if a>=0 and a<14 and b>=0 and b<14 then tile:drawPixel(a,b,pc.rgba(49,27,52,255)) end
  end
 end
 for _,v in ipairs(g.pixels) do local x,y=v%14,math.floor(v/14);tile:drawPixel(x,y,pc.rgba(255,246,215,255)) end
 atlas:drawImage(tile,Point(((i-1)%56)*14,math.floor((i-1)/56)*14))
end
atlas:saveAs(plan.art..'/字图/提示小字号字图_8px.png')
local sprite=Sprite(atlas.width,atlas.height,ColorMode.RGB);sprite.cels[1].image=atlas
sprite:saveAs(plan.art..'/制作记录~/提示小字号字图.aseprite');sprite:close()
print('COMPLETE native small glyph atlas')
