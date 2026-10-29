local root=assert(app.params.root)
local prefix=(app.params.mode=='live' and '正常资源实战' or '八塔受控实战')..(app.params.version and '_v'..app.params.version or '')
local folder=root..'/'..(app.params.capture or '验证~')..'/'..(app.params.mode=='live' and '正常资源实战帧' or '八塔实际渲染帧')
local sprite=Sprite(960,540,ColorMode.RGB);sprite.layers[1].name=prefix..'_Unity原始渲染帧'
for i=0,179 do
 local image=Image{fromFile=folder..'/frame_'..string.format('%03d',i)..'.png'};image:resize(960,540)
 if i>0 then sprite:newEmptyFrame() end
 if sprite.layers[1]:cel(i+1) then sprite:deleteCel(sprite.layers[1],i+1) end
 sprite:newCel(sprite.layers[1],i+1,image,Point(0,0));sprite.frames[i+1].duration=i%3==2 and .04 or .03
end
sprite:saveAs(root..'/'..prefix..'_30fps.gif');sprite:close()
print('Exported '..prefix..': 180 frames, 6 seconds, 30fps capture timing.')
