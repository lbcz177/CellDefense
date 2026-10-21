from pathlib import Path
from PIL import Image, ImageChops
import hashlib, json, re, shutil, uuid

root=Path(__file__).resolve().parents[2]
work=Path(__file__).parent
plan=json.loads((work/'plan.json').read_text(encoding='utf-8'))
art=Path(plan['assetRoot'])
records=art/'制作记录~'
manifest=records/'Unity接入记录.json'
assert not manifest.exists(), 'Already integrated; inspect the manifest instead of applying twice.'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
changes={}
metrics=[]
for j in plan['jobs']:
    source=root/j['source']; dest=root/j['destination']
    assert sha(source)==j['sourceSha256'], source
    assert sha(Path(str(source)+'.meta'))==j['sourceMetaSha256'], source
    a=Image.open(source).convert('RGBA'); b=Image.open(dest).convert('RGBA')
    assert a.size==b.size==tuple(j['sourceDimensions']), dest
    if j['kind']=='animation':
        assert not ImageChops.difference(a.getchannel('A'),b.getchannel('A')).getbbox(), dest
    elif j['kind']=='import':
        assert a.tobytes()==b.tobytes(), dest
    elif j['kind']=='ui':
        assert set(b.getchannel('A').getdata()) <= {0,255}, dest
    metrics.append(dict(path=j['destination'],colorsBefore=len(set(a.getdata())),colorsAfter=len(set(b.getdata())),sha256=sha(dest)))
    meta=Path(str(source)+'.meta').read_text(encoding='utf-8')
    meta=re.sub(r'(?m)^guid: [0-9a-f]+$', 'guid: '+j['newGuid'], meta)
    for field,value in [('filterMode',0),('textureCompression',0),('enableMipMap',0),('crunchedCompression',0)]:
        meta=re.sub(r'(?m)^(\s*'+field+r':) \d+',r'\g<1> '+str(value),meta)
    Path(str(dest)+'.meta').write_text(meta,encoding='utf-8',newline='\n')
    for parent in j['parents']:
        if parent not in changes:
            changes[parent]=(root/parent).read_bytes()

icons=json.loads((records/'Aseprite处理结果.json').read_text(encoding='utf-8'))['icons']
old_icons={4:1,8:2,12:3,16:4,20:5,24:6}
audit=json.loads((work/'active-assets.json').read_text(encoding='utf-8'))
old_guid_to_unit={r['guid']:old_icons[int(Path(r['path']).name[:3])] for r in audit
                  if '/02_' in r['path'] and Path(r['path']).name[:3].isdigit()
                  and int(Path(r['path']).name[:3]) in old_icons}
assert len(old_guid_to_unit)==6, old_guid_to_unit
template=(root/'Assets/_Project/Art/Sprites/细胞绘制/设置界面_声音与拉条_v1/01_声音图标.png.meta').read_text(encoding='utf-8')
for icon in icons:
    icon['newGuid']=uuid.uuid4().hex
    p=Path(icon['path']); assert Image.open(p).size==(128,128)
    meta=re.sub(r'(?m)^guid: [0-9a-f]+$', 'guid: '+icon['newGuid'],template)
    meta=re.sub(r'(?m)^(    spriteID:) [0-9a-f]+$',r'\g<1> '+uuid.uuid4().hex,meta)
    Path(str(p)+'.meta').write_text(meta,encoding='utf-8',newline='\n')
icon_guids={v['unit']:v['newGuid'] for v in icons}
icon_edits=[]
for p in (root/'Assets/_Project/Data/Towers').glob('*.asset'):
    content=p.read_text(encoding='utf-8')
    match=re.search(r'(?m)^  buildIcon: \{fileID: 21300000, guid: ([0-9a-f]+), type: 3\}',content)
    if match and match[1] in old_guid_to_unit:
        rel=p.relative_to(root).as_posix();changes.setdefault(rel,p.read_bytes())
        icon_edits.append((rel,match[1],icon_guids[old_guid_to_unit[match[1]]]))
assert len(icon_edits)==6, icon_edits

mapping={j['oldGuid']:j['newGuid'] for j in plan['jobs']}
backups=[]
for rel,original in changes.items():
    # Preserve the exact current worktree bytes, including other changes already present.
    p=root/rel; assert p.read_bytes()==original, rel
    dest=records/'接入前备份'/rel;dest.parent.mkdir(parents=True,exist_ok=True)
    dest.write_bytes(original)
    text=original.decode('utf-8')
    text=re.sub(r'(?<=guid: )[0-9a-f]{32}',lambda m:mapping.get(m[0],m[0]),text)
    for target,old,new in icon_edits:
        if target==rel:
            text=text.replace('buildIcon: {fileID: 21300000, guid: '+old, 'buildIcon: {fileID: 21300000, guid: '+new)
    updated=text.encode('utf-8')
    assert updated!=original, rel
    p.write_bytes(updated)
    backups.append(dict(path=rel,beforeSha256=hashlib.sha256(original).hexdigest(),afterSha256=sha(p)))

# All legacy images and importer identities remain intact.
for j in plan['jobs']:
    assert sha(root/j['source'])==j['sourceSha256']
    assert sha(root/(j['source']+'.meta'))==j['sourceMetaSha256']
result=dict(textures=plan['jobs'],icons=icons,changedFiles=backups,metrics=metrics)
manifest.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(dict(newTextures=len(plan['jobs']),newIcons=len(icons),changedReferences=len(backups),
                     animationColorCounts=[(m['colorsBefore'],m['colorsAfter']) for m,j in zip(metrics,plan['jobs']) if j['kind']=='animation']),ensure_ascii=False))
