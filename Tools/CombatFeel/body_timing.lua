local root=assert(app.params.root)
local art=root..'/Assets/_Project/Art/Sprites/细胞绘制/全塔打击反馈_2026-10-29_v1'
local names={'01_neutrophil','02_fibrin','03_lysosome','04_neural','05_phagocyte','06_antibody'}
local durations={60,45,35,20,25,60,35,45,55,100}
for _,name in ipairs(names) do
 local sheet=Image{fromFile=root..'/Assets/_Project/Art/Sprites/细胞绘制/像素精修_2026-10-21_v1/动画/'..name..'_attack/animation_sheet.png'}
 assert(sheet.width==640 and sheet.height==256)
 local sprite=Sprite(128,128,ColorMode.RGB);sprite.layers[1].name='原像素动作_强化节奏';sprite:assignColorSpace(ColorSpace{sRGB=true})
 for i=1,10 do
  local image=Image(128,128,ColorMode.RGB);image:drawImage(sheet,Point(-((i-1)%5)*128,-math.floor((i-1)/5)*128))
  if i>1 then sprite:newEmptyFrame() end
  if sprite.layers[1]:cel(i) then sprite:deleteCel(sprite.layers[1],i) end
  sprite:newCel(sprite.layers[1],i,image,Point(0,0));sprite.frames[i].duration=durations[i]/1000
 end
 sprite:newTag(1,4).name='Anticipation';sprite:newTag(5,6).name='Release_160ms';sprite:newTag(7,10).name='Recoil_and_settle'
 sprite:saveAs(art..'/塔身节奏/'..name..'_强化节奏.aseprite');sprite:close()
end
print('Saved six editable ten-pose timelines; the release pose still begins at 160ms.')
