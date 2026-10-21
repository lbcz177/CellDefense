local root=assert(app.params.root)
local art=assert(app.params.art)
local f=assert(io.open(root..'/units.json'));local units=json.decode(f:read('*a'));f:close()
local pc=app.pixelColor
local function load(unit,action)
  local path=art..'/'..unit.Id..'_'..string.lower(action)
  local spr=app.open(path..'/animation.aseprite');local images,durations={},{}
  for i=1,10 do local im=Image(128,128,ColorMode.RGB);im:drawSprite(spr,i);im:resize(256,256)
    images[i]=im;durations[i]=math.floor(spr.frames[i].duration*1000+0.5) end
  spr:close();return {images=images,durations=durations,action=action}
end
local function draw(name,clips,cols,rows)
  local spr=Sprite(cols*256,rows*256,ColorMode.RGB);spr:assignColorSpace(ColorSpace{sRGB=true})
  for time=0,1950,50 do
    local frame=time/50+1;if frame>1 then spr:newEmptyFrame() end
    if spr.layers[1]:cel(frame) then spr:deleteCel(spr.layers[1],frame) end
    local im=Image(spr.width,spr.height,ColorMode.RGB);im:clear(pc.rgba(30,36,52,255))
    for j,clip in ipairs(clips) do
      local t=time%1000;local i=1
      if clip.action=='Attack' and t>=480 then i=1
      else while i<10 and t>=clip.durations[i] do t=t-clip.durations[i];i=i+1 end end
      im:drawImage(clip.images[i],Point(((j-1)%cols)*256,math.floor((j-1)/cols)*256))
    end
    spr:newCel(spr.layers[1],frame,im,Point(0,0));spr.frames[frame].duration=0.05
    if frame==1 or frame==5 then im:saveAs(root..'/'..name..'_'..frame..'.png') end
  end
  spr:saveAs(root..'/'..name..'.gif');spr:close()
end
local towers,enemies={},{}
for _,u in ipairs(units) do if u.Kind=='Tower' then towers[#towers+1]=load(u,'Attack')
  else enemies[#enemies+1]=load(u,'Move');enemies[#enemies+1]=load(u,'Attack') end end
draw('tower-overview',towers,3,2)
draw('enemy-overview',enemies,4,4)
print('Created six-tower and eight-enemy previews; attacks include a short observation hold only in overview.')
