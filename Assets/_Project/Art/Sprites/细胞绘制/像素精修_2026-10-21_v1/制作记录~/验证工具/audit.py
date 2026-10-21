from pathlib import Path
from collections import deque, Counter
from PIL import Image
import re, json, hashlib

root = Path(__file__).resolve().parents[2]
out = Path(__file__).parent
guids = {}
for p in (root / 'Assets').rglob('*.meta'):
    if any(x.endswith('~') for x in p.parts):
        continue
    m = re.search(r'^guid: (\w+)', p.read_text(encoding='utf-8-sig', errors='ignore'), re.M)
    if m:
        guids[m[1]] = p.with_suffix('')
seeds = list((root / 'Assets/_Project/Scenes').glob('*.unity'))
seeds += list((root / 'Assets/_Project/Resources').glob('*.asset'))
seeds += [root / 'ProjectSettings/ProjectSettings.asset']
q, seen, parents, pngs = deque(seeds), set(), {}, []
while q:
    p = q.popleft()
    if p in seen:
        continue
    seen.add(p)
    if p.suffix.lower() == '.png':
        pngs.append(p)
        continue
    if not p.exists() or p.suffix not in ['.unity', '.prefab', '.asset', '.anim', '.controller', '.mat']:
        continue
    for g in set(re.findall(r'guid: ([0-9a-f]{32})', p.read_text(encoding='utf-8-sig', errors='ignore'))):
        target = guids.get(g)
        if target:
            parents.setdefault(target.relative_to(root).as_posix(), []).append(p.relative_to(root).as_posix())
            q.append(target)
report = []
for p in sorted(pngs):
    if '_Project/Art' not in p.as_posix():
        continue
    im = Image.open(p).convert('RGBA')
    a = im.getchannel('A')
    h = a.histogram()
    meta = Path(str(p) + '.meta').read_text(encoding='utf-8-sig')
    def prop(k):
        m = re.search(r'^\s*' + k + r':\s*([^\r\n]+)', meta, re.M)
        return m.group(1) if m else None
    r = dict(path=p.relative_to(root).as_posix(), size=list(im.size), partialAlpha=sum(h[1:255]),
             visible=im.width*im.height-h[0], bbox=a.getbbox(), colors=len(im.getcolors(9999999) or []),
             filter=prop('filterMode'), compression=prop('textureCompression'), maxSize=prop('maxTextureSize'),
             spriteMode=prop('spriteMode'), ppu=prop('spritePixelsToUnits'), guid=prop('guid'),
             sha256=hashlib.sha256(p.read_bytes()).hexdigest(), parents=parents.get(p.relative_to(root).as_posix(), []))
    report.append(r)
(out / 'active-assets.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print('ACTIVE', len(report), 'GRAPH', len(seen), 'WITH_PARTIAL_ALPHA', sum(r['partialAlpha'] > 0 for r in report))
print('GROUPS', dict(Counter('/'.join(r['path'].split('/')[5:7]) for r in report)))
for r in report:
    if '/frames/' not in r['path'] or r['partialAlpha'] or r['filter'] != '0' or r['compression'] != '0':
        print(json.dumps({k:v for k,v in r.items() if k not in ['sha256', 'parents']}, ensure_ascii=False))
