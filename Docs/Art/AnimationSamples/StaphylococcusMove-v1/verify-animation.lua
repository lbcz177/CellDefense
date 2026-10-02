local directory=assert(app.params.output)
local sprite=app.open(directory.."/staph_move.aseprite")
assert(sprite.width==128 and sprite.height==128 and #sprite.frames==8)
assert(#sprite.tags==1 and sprite.tags[1].name=="Move")
local sheet=Image{fromFile=directory.."/staph_move_sheet.png"}
assert(sheet.width==512 and sheet.height==256)
local pc=app.pixelColor
for index=1,8 do
  assert(math.abs(sprite.frames[index].duration-0.1)<0.0001)
  local rendered=Image(128,128,ColorMode.RGB)
  rendered:drawSprite(sprite,index)
  local column,row=(index-1)%4,math.floor((index-1)/4)
  local tile=Image(sheet,Rectangle(column*128,row*128,128,128))
  local single=Image{fromFile=directory.."/frames/staph_move_"..string.format("%02d",index)..".png"}
  assert(rendered:isEqual(tile),"Sheet differs from Aseprite frame "..index)
  assert(single:isEqual(tile),"Single frame differs from sheet "..index)
  for offset=0,127 do
    assert(pc.rgbaA(tile:getPixel(offset,0))==0)
    assert(pc.rgbaA(tile:getPixel(offset,127))==0)
    assert(pc.rgbaA(tile:getPixel(0,offset))==0)
    assert(pc.rgbaA(tile:getPixel(127,offset))==0)
  end
end
print(json.encode({passed=true,reopenedFrames=8,frameSize=128,sheetWidth=512,sheetHeight=256,
  equalSheetFrames=8,transparentFrameEdges=true,frameDurationMs=100}))
