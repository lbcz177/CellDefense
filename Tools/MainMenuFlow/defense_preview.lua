local sprite=Sprite(640,360,ColorMode.RGB)
sprite.layers[1].name='Unity实际渲染_蓝色防御细胞动效'
for frame=0,179 do
  local image=Image{fromFile=app.params.frames..string.format('/blue-defense-actions-%03d.png',frame)}
  if frame>0 then sprite:newEmptyFrame(frame+1) end
  sprite:newCel(sprite.layers[1],frame+1,image,Point(0,0))
  sprite.frames[frame+1].duration=.03
end
sprite:saveAs(app.params.output);sprite:close()
print('Saved actual Unity defender actions at 33.3 FPS')
