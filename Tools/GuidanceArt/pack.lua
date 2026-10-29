local f=assert(io.open(app.params.plan,'r'));local p=json.decode(f:read('*a'));f:close();local pc=app.pixelColor
for _,j in ipairs(p.jobs) do
 local im=Image{fromFile=j.raw};im:resize(256,256)
 for it in im:pixels() do local c=it();local r,g,b=pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c)
  if pc.rgbaA(c)<210 or r>240 and g<60 then it(0) else it(pc.rgba(r,g,b,255)) end
 end
 im:saveAs(p.art..'/'..j.id..'.png');local doc=Sprite(256,256,ColorMode.RGB);doc:newCel(doc.layers[1],1,im,Point(0,0));doc:saveAs(p.art..'/'..j.id..'.aseprite');doc:close()
end
for _,kind in ipairs({'particle','warning'}) do
 local im=Image(16,16,ColorMode.RGB)
 if kind=='particle' then
  for y=3,12 do for x=3,12 do if (x-7.5)^2+(y-7.5)^2<22 then im:drawPixel(x,y,pc.rgba(226,246,220,255)) end end end
  for y=4,6 do for x=5,7 do im:drawPixel(x,y,pc.rgba(255,255,239,255)) end end
 else
  for y=1,14 do for x=2,13 do if x+y>=7 and x-y<=6 and y-x<=7 and x+y<=23 then im:drawPixel(x,y,pc.rgba(112,58,64,255)) end end end
  for y=3,9 do for x=7,9 do im:drawPixel(x,y,pc.rgba(255,241,190,255)) end end
  for y=11,12 do for x=7,9 do im:drawPixel(x,y,pc.rgba(255,241,190,255)) end end
 end
 im:saveAs(p.art..'/'..kind..'.png')
end
print('Saved membrane frames, tissue tile, vesicle and warning sprite.')
