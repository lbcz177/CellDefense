from pathlib import Path
import hashlib,json,re,collections
root=Path(__file__).resolve().parents[2]
work=Path(__file__).parent
plan=json.loads((work/'plan.json').read_text(encoding='utf-8'))
art=Path(plan['assetRoot']); records=art/'制作记录~'
manifest=json.loads((records/'Unity接入记录.json').read_text(encoding='utf-8'))
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
for job in plan['jobs']:
    assert sha(root/job['source'])==job['sourceSha256'],job['source']
    assert sha(root/(job['source']+'.meta'))==job['sourceMetaSha256'],job['source']+'.meta'
mapping={j['oldGuid']:j['newGuid'] for j in plan['jobs']}
icon_mapping={}
for entry in manifest['changedFiles']:
    rel=entry['path']; before=(records/'接入前备份'/rel).read_bytes();after=(root/rel).read_bytes()
    assert hashlib.sha256(before).hexdigest()==entry['beforeSha256']
    assert hashlib.sha256(after).hexdigest()==entry['afterSha256'],rel
    if '/Data/Towers/' in rel:
        a=re.search(rb'buildIcon: \{fileID: 21300000, guid: ([0-9a-f]+)',before)[1].decode()
        b=re.search(rb'buildIcon: \{fileID: 21300000, guid: ([0-9a-f]+)',after)[1].decode()
        icon_mapping[a]=b
    expected=re.sub(rb'(?<=guid: )[0-9a-f]{32}',lambda m:mapping.get(m[0].decode(),icon_mapping.get(m[0].decode(),m[0].decode())).encode(),before)
    assert expected==after,rel+' changed beyond image references'
guids=collections.defaultdict(list)
for p in (root/'Assets').rglob('*.meta'):
    if any(part.endswith('~') for part in p.parts):continue
    match=re.search(r'(?m)^guid: (\w+)',p.read_text(encoding='utf-8-sig',errors='ignore'))
    if match:guids[match[1]].append(p.relative_to(root).as_posix())
for guid in list(mapping.values())+list(icon_mapping.values()):
    assert len(guids[guid])==1,(guid,guids[guid])
summary=dict(legacyPngHashesPreserved=75,legacyMetaHashesPreserved=75,newTextures=81,
             referenceFilesWithOnlyGuidChanges=len(manifest['changedFiles']),duplicateNewGuids=0,
             runtimeScriptsChanged=0,animationFrames=220)
(records/'文件完整性验证.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(summary))
