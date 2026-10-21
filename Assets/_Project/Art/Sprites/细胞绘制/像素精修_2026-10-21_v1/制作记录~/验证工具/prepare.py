from pathlib import Path
import json, hashlib, shutil, re, uuid

root=Path(__file__).resolve().parents[2]
work=Path(__file__).parent
asset=root/'Assets/_Project/Art/Sprites/细胞绘制/像素精修_2026-10-21_v1'
assert not asset.exists(), 'Use a new version directory instead of replacing an earlier revision.'
asset.mkdir(parents=True)
records=asset/'制作记录~'
(records/'Aseprite源文件').mkdir(parents=True)
rows=json.loads((work/'active-assets.json').read_text(encoding='utf-8'))
jobs=[]
for r in rows:
    p=r['path']
    kind=None
    if '/正俯视10帧_v3/' in p:
        kind='animation'
        key=p.split('/正俯视10帧_v3/')[1].split('/')[0]
        rel='动画/'+key+'/animation_sheet.png'
        group=key.rsplit('_',1)[0]
    elif ('/战斗UI/' in p and '/暂停与倍速按钮_v1/' not in p) or any('/'+x+'/' in p for x in ['暂停界面_v2_2026-10-18','设置界面_声音与拉条_v1','设置界面_按钮空框_v1','设置界面_文字素材_v1','胜负结算_v1']):
        kind='ui'
        rel='界面/'+p.split('/细胞绘制/')[1]
        group='ui_'+str(len(jobs))
    elif '/08_子弹与特效/089_' in p or '/08_子弹与特效/090_' in p:
        kind='effect';rel='特效/'+Path(p).name;group='effect_'+str(len(jobs))
    elif '/主界面/胞卫战_主界面_v1.png' in p:
        kind='import';rel='主界面/胞卫战_主界面_v1.png';group='menu'
    if not kind:continue
    dest=asset/rel;dest.parent.mkdir(parents=True,exist_ok=True)
    jobs.append(dict(source=p,destination=dest.relative_to(root).as_posix(),kind=kind,group=group,
                     colors=96 if kind=='animation' else 48 if kind=='ui' else 64,
                     oldGuid=r['guid'],newGuid=uuid.uuid4().hex,sourceSha256=r['sha256'],
                     sourceMetaSha256=hashlib.sha256((root/(p+'.meta')).read_bytes()).hexdigest(),
                     sourceDimensions=r['size'],parents=r['parents']))
config=dict(root=root.as_posix(),assetRoot=asset.as_posix(),jobs=jobs)
(work/'plan.json').write_text(json.dumps(config,ensure_ascii=False,indent=2),encoding='utf-8')
(records/'处理计划.json').write_text(json.dumps(config,ensure_ascii=False,indent=2),encoding='utf-8')
shutil.copy2(work/'active-assets.json',records/'在用资源审计.json')
print('PREPARED',len(jobs),'assets', {k:sum(j['kind']==k for j in jobs) for k in ['animation','ui','effect','import']})
print(asset)
