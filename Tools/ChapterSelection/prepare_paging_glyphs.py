"""Extend clean menu glyph canvases through Aseprite, retaining every existing glyph pixel."""
from pathlib import Path
import json
import math
import re
import subprocess
import uuid
from PIL import Image

root = Path(__file__).resolve().parents[2]
folder = root / 'Assets/_Project/Art/Sprites/细胞绘制/章节翻页_2026-10-29_v1/字图'
archive = folder / '制作记录~'
archive.mkdir(parents=True, exist_ok=True)
prompt = (root / 'Assets/_Project/Resources/PromptImageResources.asset').read_text(encoding='utf-8-sig')
resource = root / 'Assets/_Project/Resources/ScoreArtworkResources.asset'
score = resource.read_text(encoding='utf-8-sig')
if not (archive / resource.name).exists():
    (archive / resource.name).write_text(score, encoding='utf-8')
characters = json.loads(re.search(r'^  characters: (.*)', prompt, re.M)[1])
meta_by_guid = {}
for meta in (root / 'Assets/_Project/Art').rglob('*.png.meta'):
    if any(part.endswith('~') for part in meta.parts):
        continue
    match = re.search(r'^guid: (\w+)', meta.read_text(encoding='utf-8-sig'), re.M)
    if match:
        meta_by_guid[match[1]] = meta

lua = []
checks = []
for native, cell, field, source_field in [(12, 18, 'scoreGlyphAtlas', 'glyphAtlas'), (8, 14, 'smallScoreGlyphAtlas', 'smallGlyphAtlas')]:
    old_guid = re.search(r'^  ' + field + r': .*guid: (\w+)', score, re.M)[1]
    source_guid = re.search(r'^  ' + source_field + r': .*guid: (\w+)', prompt, re.M)[1]
    old_meta, source_meta = meta_by_guid[old_guid], meta_by_guid[source_guid]
    old_path, source_path = Path(str(old_meta)[:-5]), Path(str(source_meta)[:-5])
    old, source = Image.open(old_path).convert('RGBA'), Image.open(source_path).convert('RGBA')
    target = folder / f'章节净字图_{native}px.png'
    if target.exists():
        raise RuntimeError('Already generated: ' + str(target))
    height = max(old.height, math.ceil(len(characters) / 56) * cell)
    lua += ['do', f'local previous=app.open({json.dumps(old_path.as_posix(), ensure_ascii=False)})',
            f'local sprite=Sprite(previous.width,{height},ColorMode.RGB)',
            'local image=sprite.cels[1].image', 'image:drawImage(previous.cels[1].image,Point(0,0))', 'previous:close()']
    appended = []
    for index, char in enumerate(characters):
        if char.isspace():
            continue
        x, y = index % 56 * cell, index // 56 * cell
        if old.crop((x, y, x + cell, y + cell)).getchannel('A').getbbox():
            continue
        pixels = [(px, py) for py in range(cell) for px in range(cell)
                  if source.getpixel((x + px, y + py)) == (255, 255, 255, 255)]
        if not pixels:
            raise RuntimeError(f'No clean glyph: {char!r}, index {index}')
        for px, py in pixels:
            lua.append(f'image:drawPixel({x+px},{y+py},app.pixelColor.rgba(255,255,255,255))')
        appended.append({'index': index, 'character': char})
    lua += [f'sprite:saveAs({json.dumps(target.as_posix(), ensure_ascii=False)})',
            f'sprite:saveAs({json.dumps(target.with_suffix(".aseprite").as_posix(), ensure_ascii=False)})',
            'sprite:close()', 'end']
    new_guid = uuid.uuid4().hex
    Path(str(target) + '.meta').write_text(
        re.sub(r'^guid: \w+', 'guid: ' + new_guid, old_meta.read_text(encoding='utf-8-sig'), flags=re.M), encoding='utf-8')
    score = score.replace('guid: ' + old_guid, 'guid: ' + new_guid)
    checks.append((old_path, target, cell, appended))

script = archive / 'extend_clean_glyphs.lua'
script.write_text('\n'.join(lua), encoding='utf-8')
aseprite = next(Path('D:/aseprite').rglob('Aseprite.exe'))
subprocess.run([str(aseprite), '-b', '--script', str(script)], check=True)
report = []
for old_path, target, cell, appended in checks:
    old, result = Image.open(old_path).convert('RGBA'), Image.open(target).convert('RGBA')
    for index, char in enumerate(characters):
        if char.isspace():
            continue
        x, y = index % 56 * cell, index // 56 * cell
        box = x, y, x + cell, y + cell
        if old.crop(box).getchannel('A').getbbox():
            assert old.crop(box).tobytes() == result.crop(box).tobytes(), index
        assert result.crop(box).getchannel('A').getbbox(), index
    report.append({'source': str(old_path.relative_to(root)), 'target': str(target.relative_to(root)),
                   'size': result.size, 'added': appended, 'all_existing_glyphs_preserved': True})
resource.write_text(score, encoding='utf-8')
(archive / 'glyph-verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({'supported_glyphs': len(characters), 'added_per_size': [len(item[3]) for item in checks]}))
