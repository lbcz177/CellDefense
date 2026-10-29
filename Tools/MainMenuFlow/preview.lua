local folder=app.params.frames
local output=app.params.output
local sprite=Sprite(640,360,ColorMode.RGB)
sprite.layers[1].name='Unity实际渲染_细胞流动预览'
local order={}
for i=0,179 do order[#order+1]=i end
for index,number in ipairs(order) do
  local image=Image{fromFile=folder..string.format('/menu-flow-smooth-%03d.png',number)}
  if index>1 then sprite:newEmptyFrame(index) end
  sprite:newCel(sprite.layers[1],index,image,Point(0,0))
  sprite.frames[index].duration=.03
end
sprite:saveAs(output)
sprite:close()
print('Saved 33-fps Unity preview; one complete forward cycle, no reversal')
