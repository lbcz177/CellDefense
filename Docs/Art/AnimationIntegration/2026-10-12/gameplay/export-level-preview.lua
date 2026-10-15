local input = app.params["input"]
local output = app.params["output"]
local sprite = Sprite(1280, 720, ColorMode.RGB)
local layer = sprite.layers[1]
for index = 0, 89 do
  local filename = input .. "/frame_" .. string.format("%03d", index) .. ".png"
  if index > 0 then sprite:newEmptyFrame() end
  local frame = sprite.frames[index + 1]
  sprite:newCel(layer, frame, Image{fromFile=filename}, Point(0, 0))
  frame.duration = 0.1
end
-- Crop camera margins only; the footage is from the existing second level.
sprite:crop(Rectangle(340, 140, 600, 440))
sprite:saveAs(output)
sprite:close()
