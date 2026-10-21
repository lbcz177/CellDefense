local root=assert(app.params.root)
local pc=app.pixelColor
local function bounds(im)
  local x0,y0,x1,y1=im.width,im.height,-1,-1
  for it in im:pixels() do if pc.rgbaA(it())>=128 then
    x0=math.min(x0,it.x);y0=math.min(y0,it.y);x1=math.max(x1,it.x);y1=math.max(y1,it.y)
  end end
  assert(x1>=x0,'Empty asset');return Rectangle(x0,y0,x1-x0+1,y1-y0+1)
end
local function alpha(im)
  for it in im:pixels() do local c=it();if pc.rgbaA(c)<128 then it(0)
    else it(pc.rgba(pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c),255)) end end
end
local function check(im)
  for x=0,im.width-1 do assert(pc.rgbaA(im:getPixel(x,0))==0 and pc.rgbaA(im:getPixel(x,im.height-1))==0) end
  for y=0,im.height-1 do assert(pc.rgbaA(im:getPixel(0,y))==0 and pc.rgbaA(im:getPixel(im.width-1,y))==0) end
end
local function prepare(id,w,h,iw,ih,fit)
  local source=Image{fromFile=root..'/制作记录~/'..id..'_source.png'}
  local im=Image(source,bounds(source));local rw,rh=iw,ih
  if fit then local s=math.min(iw/im.width,ih/im.height);rw=math.floor(im.width*s+0.5);rh=math.floor(im.height*s+0.5) end
  im:resize(rw,rh);alpha(im)
  local canvas=Image(w,h,ColorMode.RGB);canvas:drawImage(im,Point(math.floor((w-rw)/2),math.floor((h-rh)/2)))
  check(canvas);return canvas
end
local configs={
  {id='panel',file='01_空面板.png',w=640,h=800,iw=624,ih=784},
  {id='title',file='02_游戏暂停_标题.png',w=448,h=112,iw=416,ih=96,fit=true},
  {id='resume_normal',file='按钮/01_返回游戏_未选中.png'},
  {id='resume_selected',file='按钮/02_返回游戏_蓝色选中.png'},
  {id='settings_normal',file='按钮/03_设置_未选中.png'},
  {id='settings_selected',file='按钮/04_设置_蓝色选中.png'},
  {id='menu_normal',file='按钮/05_退出到主菜单_未选中.png'},
  {id='menu_selected',file='按钮/06_退出到主菜单_蓝色选中.png'}
}
local images,reports={},{}
for _,c in ipairs(configs) do
  c.w=c.w or 448;c.h=c.h or 112;c.iw=c.iw or 416;c.ih=c.ih or 96
  local im=prepare(c.id,c.w,c.h,c.iw,c.ih,c.fit);images[c.id]=im;im:saveAs(root..'/'..c.file)
  reports[#reports+1]={id=c.id,file=c.file,width=c.w,height=c.h,transparentEdges=true}
end
local spr=Sprite(640,800,ColorMode.RGB);spr:assignColorSpace(ColorSpace{sRGB=true})
local positions={
  {'panel',0,0},{'title',96,112},
  {'resume_normal',96,304},{'resume_selected',96,304},
  {'settings_normal',96,440},{'settings_selected',96,440},
  {'menu_normal',96,576},{'menu_selected',96,576}
}
local layers={}
for i,p in ipairs(positions) do local layer=i==1 and spr.layers[1] or spr:newLayer();layer.name=p[1];layers[p[1]]=layer
  spr:newCel(layer,1,images[p[1]],Point(p[2],p[3]));layer.isVisible=not p[1]:find('_selected',1,true) end
local normal=Image(640,800,ColorMode.RGB);normal:drawSprite(spr,1);normal:saveAs(root..'/暂停界面_全部未选中.png')
layers.resume_normal.isVisible=false;layers.resume_selected.isVisible=true
local composite=Image(640,800,ColorMode.RGB);composite:drawSprite(spr,1);check(composite)
composite:saveAs(root..'/暂停界面_组合透明.png');spr:saveAs(root..'/暂停界面_分层源文件.aseprite');spr:close()
local editable=app.open(root..'/暂停界面_分层源文件.aseprite');local roundtrip=Image(640,800,ColorMode.RGB);roundtrip:drawSprite(editable,1)
assert(roundtrip:isEqual(composite),'Native composition mismatch')
for i,p in ipairs(positions) do assert(editable.layers[i]:cel(1).image:isEqual(images[p[1]]),'Native part mismatch') end
editable:close()
for _,c in ipairs(configs) do assert(images[c.id]:isEqual(Image{fromFile=root..'/'..c.file}),'PNG reload mismatch') end
local preview=Image(960,960,ColorMode.RGB);preview:clear(pc.rgba(27,24,35,255));preview:drawImage(composite,Point(160,80))
preview:saveAs(root..'/暂停界面_预览.png')
local f=assert(io.open(root..'/制作记录~/validation.json','w'));f:write(json.encode{
  passed=true,separateAssets=8,assets=reports,buttonCanvas={width=448,height=112},
  buttonInkBounds={x=16,y=8,width=416,height=96},layerCount=8,nativeSourceReopened=true,
  buttonOrder={'返回游戏','设置','退出到主菜单'},previewSelected='返回游戏',canvas={width=640,height=800},
  panelPosition={x=0,y=0},titlePosition={x=96,y=112},buttonPositions={{x=96,y=304},{x=96,y=440},{x=96,y=576}}});f:close()
print('Pause UI PASS: eight transparent parts, three matched button state pairs, layered source and PNG roundtrip verified.')
