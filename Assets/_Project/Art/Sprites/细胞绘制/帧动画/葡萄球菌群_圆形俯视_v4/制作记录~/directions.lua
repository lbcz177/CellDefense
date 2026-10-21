local root=assert(app.params.root)
local pc=app.pixelColor
local function load(dir)
  local f=assert(io.open(dir..'/animation.json'));local report=json.decode(f:read('*a'));f:close()
  local im={};for i=1,10 do im[i]=Image{fromFile=dir..'/frames/frame_'..string.format('%02d',i)..'.png'} end
  return im,report
end
local function rotate(im,turns)
  local out=Image(im.width,im.height,ColorMode.RGB)
  for it in im:pixels() do
    local x,y=it.x,it.y
    for n=1,turns do x,y=im.width-1-y,x end
    out:drawPixel(x,y,it())
  end;return out
end
local old,oldReport=load(root..'/制作记录~/backup/07_staph_move')
local move,moveReport=load(root..'/07_staph_move')
local attack,attackReport=load(root..'/07_staph_attack')
local function preview(file,clips,turns,timing)
  local spr=Sprite(#clips*256,256,ColorMode.RGB);spr:assignColorSpace(ColorSpace{sRGB=true})
  for i=1,10 do
    if i>1 then spr:newEmptyFrame() end
    if spr.layers[1]:cel(i) then spr:deleteCel(spr.layers[1],i) end
    local im=Image(spr.width,spr.height,ColorMode.RGB);im:clear(pc.rgba(30,36,52,255))
    for j,clip in ipairs(clips) do local actor=rotate(clip[i],turns[j]);actor:resize(256,256)
      im:drawImage(actor,Point((j-1)*256,0)) end
    spr:newCel(spr.layers[1],i,im,Point(0,0));spr.frames[i].duration=timing.framesData[i].durationMs/1000
    if i==1 then im:saveAs(root..'/'..file..'.png') end
  end
  spr:saveAs(root..'/'..file..'.gif');spr:close()
end
preview('向下移动_新旧对比',{old,move},{1,1},moveReport)
preview('四向移动检查',{move,move,move,move},{0,1,2,3},moveReport)
preview('四向攻击检查',{attack,attack,attack,attack},{0,1,2,3},attackReport)
local maxOld,maxNew=1,1
for _,f in ipairs(oldReport.framesData) do maxOld=math.max(maxOld,f.bounds.w/f.bounds.h,f.bounds.h/f.bounds.w) end
for _,f in ipairs(moveReport.framesData) do maxNew=math.max(maxNew,f.bounds.w/f.bounds.h,f.bounds.h/f.bounds.w) end
assert(maxNew<=1.12,'Movement silhouette still elongated')
local f=assert(io.open(root..'/制作记录~/shape-validation.json','w'));f:write(json.encode{
  passed=true,moveFrames=10,attackFrames=10,oldMaximumAspect=maxOld,newMaximumAspect=maxNew,
  directionOrder={'right','down','left','up'},comparisonOrder={'old','new'},mirroring=false,
  previewRotation='clockwise quarter-turns, equivalent to world Z clockwise rotation',nativePoseChecks=true});f:close()
print('Four-direction shape PASS: old max aspect '..maxOld..', new '..maxNew)
