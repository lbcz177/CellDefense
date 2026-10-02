-- Technical packing of image-generated poses; run with Aseprite --batch.
local input = assert(app.params.input, "Missing --script-param input=...")
local output = assert(app.params.output, "Missing --script-param output=...")
local source = Image{fromFile=input}
local pc = app.pixelColor
local size, count, columns = 128, 8, 4
local duration = 0.1
local poses, measurements = {}, {}
local maximum = 0

local function visibleBounds(image, threshold)
  local left, top, right, bottom = image.width, image.height, -1, -1
  for it in image:pixels() do
    if pc.rgbaA(it()) >= threshold then
      left = math.min(left, it.x)
      top = math.min(top, it.y)
      right = math.max(right, it.x)
      bottom = math.max(bottom, it.y)
    end
  end
  assert(right >= left and bottom >= top, "Empty frame")
  return {x=left, y=top, w=right-left+1, h=bottom-top+1}
end

for index=0,count-1 do
  local column, row = index % columns, math.floor(index/columns)
  local x0, x1 = math.floor(column*source.width/columns), math.floor((column+1)*source.width/columns)
  local y0, y1 = math.floor(row*source.height/2), math.floor((row+1)*source.height/2)
  local tile = Image(source, Rectangle(x0,y0,x1-x0,y1-y0))
  local b = visibleBounds(tile, 128)
  assert(b.x > 2 and b.y > 2 and b.x+b.w < tile.width-2 and b.y+b.h < tile.height-2,
    "Pose touches a grid boundary")
  -- Retain one source-pixel margin and the generated alpha channel.
  local crop = Image(tile, Rectangle(b.x-1,b.y-1,b.w+2,b.h+2))
  maximum = math.max(maximum, crop.width, crop.height)
  poses[index+1] = crop
  measurements[index+1] = {sourceCell={x=x0,y=y0,w=x1-x0,h=y1-y0},sourceBounds=b}
end

-- Use one common scale: preserve the size changes that make squash/stretch work.
local scale = 96 / maximum
local sprite = Sprite(size,size,ColorMode.RGB)
sprite:assignColorSpace(ColorSpace{sRGB=true})
sprite.layers[1].name = "Move"
sprite.gridBounds = Rectangle(0,0,size,size)
local layer = sprite.layers[1]
local sheet = Image(size*columns,size*2,ColorMode.RGB)
local images, frames = {}, {}

for index=1,count do
  local pose = poses[index]
  pose:resize(math.floor(pose.width*scale+0.5),math.floor(pose.height*scale+0.5))
  local frameImage = Image(size,size,ColorMode.RGB)
  local x = math.floor((size-pose.width)/2)
  local y = 112-pose.height
  frameImage:drawImage(pose,Point(x,y))
  local b = visibleBounds(frameImage,128)
  assert(b.x >= 8 and b.y >= 8 and b.x+b.w <= size-8 and b.y+b.h <= size-8,
    "Insufficient safe margin")
  if index > 1 then sprite:newEmptyFrame(index) end
  if layer:cel(index) then sprite:deleteCel(layer,index) end
  sprite:newCel(layer,index,frameImage,Point(0,0))
  sprite.frames[index].duration = duration
  local column,row = (index-1)%columns,math.floor((index-1)/columns)
  sheet:drawImage(frameImage,Point(column*size,row*size))
  frameImage:saveAs(output.."/frames/staph_move_"..string.format("%02d",index)..".png")
  images[index] = frameImage
  frames[index] = {
    filename="staph_move_"..string.format("%02d",index),
    frame={x=column*size,y=row*size,w=size,h=size},
    unityRect={x=column*size,y=(1-row)*size,w=size,h=size},
    duration=100,visibleBounds=b,
    pivot={x=0.5,y=0.125},source=measurements[index]
  }
end

local transitions = {}
for index=1,count do
  local nextIndex = index%count+1
  local silhouetteChange, union, difference = 0,0,0
  for y=0,size-1 do
    for x=0,size-1 do
      local a,b = images[index]:getPixel(x,y),images[nextIndex]:getPixel(x,y)
      local visibleA,visibleB = pc.rgbaA(a)>=128,pc.rgbaA(b)>=128
      if visibleA or visibleB then
        union=union+1
        if visibleA ~= visibleB then silhouetteChange=silhouetteChange+1 end
        difference=difference+math.abs(pc.rgbaR(a)-pc.rgbaR(b))+
          math.abs(pc.rgbaG(a)-pc.rgbaG(b))+math.abs(pc.rgbaB(a)-pc.rgbaB(b))+
          math.abs(pc.rgbaA(a)-pc.rgbaA(b))
      end
    end
  end
  assert(difference > 0, "Consecutive identical frames")
  transitions[index]={from=index,to=nextIndex,silhouetteChange=silhouetteChange/union,
    rgbaDifference=difference/(union*4*255)}
end

local tag = sprite:newTag(1,count)
tag.name = "Move"
tag.aniDir = AniDir.FORWARD
tag.repeats = 0
sprite:saveAs(output.."/staph_move.aseprite")
sheet:saveAs(output.."/staph_move_sheet.png")
local manifest = {
  name="Staphylococcus Move",frameCount=count,fps=10,durationSeconds=0.8,loop=true,
  sheet={width=size*columns,height=size*2,columns=columns,rows=2,cellWidth=size,cellHeight=size,padding=0},
  order="left-to-right then top-to-bottom",unityPivot={x=0.5,y=0.125},
  commonScale=scale,sourceWidth=source.width,sourceHeight=source.height,
  frames=frames,transitions=transitions,
  method="Built-in image_gen poses; Aseprite nearest-neighbor normalization, bottom alignment and sprite-sheet assembly"
}
local file=assert(io.open(output.."/animation.json","w"))
file:write(json.encode(manifest))
file:close()
print(json.encode({frames=count,width=sheet.width,height=sheet.height,transitions=transitions}))
