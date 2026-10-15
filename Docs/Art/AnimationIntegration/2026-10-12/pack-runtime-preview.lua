local docs=assert(app.params.docs)
local output=assert(app.params.output)
local pc=app.pixelColor
local sprite=Sprite(640,480,ColorMode.RGB)
sprite:assignColorSpace(ColorSpace{sRGB=true})
sprite.layers[1].name='Unity runtime capture'
for i=1,33 do
  local image=Image{fromFile=docs..'/runtime-frames/frame_'..string.format('%03d',i-1)..'.png'}
  assert(image.width==640 and image.height==480)
  if i>1 then sprite:newEmptyFrame(i) end
  if sprite.layers[1]:cel(i) then sprite:deleteCel(sprite.layers[1],i) end
  sprite:newCel(sprite.layers[1],i,image,Point(0,0))
  sprite.frames[i].duration=0.05
  if i==4 then image:saveAs(output..'/Unity运行预览.png') end
end
sprite:saveAs(output..'/Unity运行预览.gif')
print(json.encode({width=640,height=480,frames=33,durationMs=1650,source='Actual Unity player-loop camera captures'}))
