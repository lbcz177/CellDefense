local f=assert(io.open(app.params.plan,'r'));local p=json.decode(f:read('*a'));f:close()
local pc=app.pixelColor;local cell=64
for _,m in ipairs(p.maps) do
 local im=Image(15*cell,11*cell,ColorMode.RGB)
 local function rectangle(x,y,w,h,c) for yy=y,y+h-1 do for xx=x,x+w-1 do im:drawPixel(xx,yy,c) end end end
 rectangle(0,0,im.width,im.height,pc.rgba(245,219,201,255))
 for _,c in ipairs(m.blocked) do rectangle(c[1]*cell,(10-c[2])*cell,cell,cell,pc.rgba(77,68,76,255)) end
 for _,c in ipairs(m.zone) do rectangle(c[1]*cell,(10-c[2])*cell,cell,cell,pc.rgba(244,177,66,255)) end
 for _,path in ipairs(m.paths) do
  for _,c in ipairs(path) do rectangle(c[1]*cell+29,(10-c[2])*cell+29,6,6,pc.rgba(49,119,209,255)) end
 end
 for _,c in ipairs(m.entries) do rectangle(c[1]*cell,(10-c[2])*cell,cell,cell,pc.rgba(189,42,105,255)) end
 rectangle(m.core[1]*cell+6,(10-m.core[2])*cell+6,cell-12,cell-12,pc.rgba(44,181,158,255))
 im:saveAs(m.guide)
end
print('Four checked layout guides exported')
