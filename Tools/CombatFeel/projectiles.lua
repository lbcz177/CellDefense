local root=assert(app.params.root);local out=root..'/Assets/_Project/Art/Sprites/细胞绘制/全塔打击反馈_2026-10-29_v1/投射物'
local pc=app.pixelColor
local paths={root..'/Assets/_Project/Art/Sprites/细胞绘制/帧动画/运行样例_v1/单体颗粒.png',root..'/Assets/_Project/Art/Sprites/细胞绘制/像素精修_2026-10-21_v1/图标/06_抗体细胞.png',root..'/Assets/_Project/Art/Sprites/细胞绘制/2026-09-28_完整补图/08_子弹与特效/077_慢速酸化囊泡弹.png'}
local names={'近卫颗粒','抗体Y蛋白','酸化囊泡'}
for i,path in ipairs(paths) do
 local raw=Image{fromFile=path}
 if i==2 then local crop=Image(42,38,ColorMode.RGB);crop:drawImage(raw,Point(-43,-12));raw=crop end
 local x0,y0,x1,y1=raw.width,raw.height,0,0
 for it in raw:pixels() do local c=it();if pc.rgbaA(c)<210 then it(0) else
  it(pc.rgba(pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c),255));x0=math.min(x0,it.x);y0=math.min(y0,it.y);x1=math.max(x1,it.x);y1=math.max(y1,it.y)
 end end
 local crop=Image(x1-x0+1,y1-y0+1,ColorMode.RGB);crop:drawImage(raw,Point(-x0,-y0))
 local scale=48/math.max(crop.width,crop.height);crop:resize(math.floor(crop.width*scale+.5),math.floor(crop.height*scale+.5))
 local final=Image(64,64,ColorMode.RGB);final:drawImage(crop,Point(math.floor((64-crop.width)/2),math.floor((64-crop.height)/2)))
 final:saveAs(out..'/'..names[i]..'.png');local doc=Sprite(64,64,ColorMode.RGB);doc.layers[1].name=names[i]
 doc:newCel(doc.layers[1],1,final,Point(0,0));doc:saveAs(out..'/'..names[i]..'.aseprite');doc:close()
end
print('Prepared three separate crisp travel heads from established projectile/cell art.')
