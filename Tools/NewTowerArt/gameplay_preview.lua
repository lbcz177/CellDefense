local root=assert(app.params.root)
local source=root..'/验证~/实际战斗帧'
local detail=app.params.detail=='true'
local sprite=Sprite(detail and 510 or 640,detail and 465 or 360,ColorMode.RGB);sprite.layers[1].name='Unity实际战斗帧'
for i=0,155 do
 local image=Image{fromFile=source..'/frame_'..string.format('%03d',i)..'.png'}
 if detail then local crop=Image(170,155,ColorMode.RGB);crop:drawImage(image,Point(-95,-105));crop:resize(510,465);image=crop end
 if i>0 then sprite:newEmptyFrame() end
 if sprite.layers[1]:cel(i+1) then sprite:deleteCel(sprite.layers[1],i+1) end
 sprite:newCel(sprite.layers[1],i+1,image,Point(0,0))
 -- GIF has 10ms time steps: 30/30/40ms preserves the 30fps capture's total duration.
 sprite.frames[i+1].duration=i%3==2 and .04 or .03
end
sprite:saveAs(root..(detail and '/两座新塔_实际战斗特写_30fps.gif' or '/两座新塔_实际战斗_30fps.gif'));sprite:close()
print('Saved 156 actual Unity frames, 5.2 seconds, continuous forward playback.')
