from pathlib import Path
from PIL import Image
import json,re,hashlib,shutil,datetime,xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[2];work=Path(__file__).parent
plan=json.loads((work/'plan-small.json').read_text(encoding='utf-8'));art=Path(plan['art'])
assert (work/'stage.txt').read_text(encoding='utf-8-sig').strip()=='Passed','Tests must finish successfully first.'
records=art/'制作记录~';evidence=art/'接入验证~';evidence.mkdir(exist_ok=True)
sources=records/'制作与验证脚本';sources.mkdir(exist_ok=True)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
summary={'fixedPhrases':len(plan['phrases']),'charactersPerSize':len(plan['glyphs']),'nativeGlyphSizes':[8,12]}
for platform in ['EditMode','PlayMode']:
    source=work/(platform+'.xml');xml=ET.parse(source).getroot();assert xml.attrib['result']=='Passed'
    summary[platform]={key:xml.attrib[key] for key in ['result','total','passed','failed','skipped','start-time','end-time']}
    shutil.copy2(source,sources/source.name)
play_start=datetime.datetime.fromisoformat(summary['PlayMode']['start-time'].replace('Z','+00:00')).timestamp()
screens=[]
names=[p.name for p in (root/'Logs/CodexTradingTests/Logs/PauseArtwork').glob('prompt-*.png')]
names += ['tutorial-select-cell.png','tutorial-build-choice.png','tutorial-barrier-info.png','tutorial-upgrade.png',
          'tutorial-mobile-1280x720.png','tutorial-mobile-1600x720.png','tutorial-mobile-1920x1080.png','upgrade-before.png','upgrade-after.png']
for name in sorted(set(names)):
    source=root/'Logs/CodexTradingTests/Logs/PauseArtwork'/name
    assert source.stat().st_mtime>=play_start-2,'Stale screenshot: '+name
    shutil.copy2(source,evidence/name)
    screens.append(dict(path=name,sha256=sha(source),size=list(Image.open(source).size)))
summary['screenshots']=screens
file_checks=[]
for entry in plan['before']:
    p=root/entry['path'];backup=records/'接入前备份'/entry['path']
    assert sha(backup)==entry['sha256'],'Source backup changed: '+entry['path']
    assert sha(p)==sha(root/'Logs/CodexTradingTests'/entry['path']),'Validation source differs: '+entry['path']
    file_checks.append(dict(path=entry['path'],beforeSha256=entry['sha256'],afterSha256=sha(p)))
for folder in ['Assets/_Project/Scripts','Assets/_Project/Tests']:
    for p in (root/folder).rglob('*.cs'):
        rel=p.relative_to(root);copy=root/'Logs/CodexTradingTests'/rel
        assert copy.exists() and sha(copy)==sha(p),'Validation C# differs: '+str(rel)
summary['changedSourceFiles']=file_checks
textures=[]
for p in art.rglob('*.png'):
    if any(part.endswith('~') for part in p.parts):continue
    image=Image.open(p).convert('RGBA');assert set(image.getchannel('A').getdata())<={0,255},p
    meta=Path(str(p)+'.meta').read_text(encoding='utf-8-sig')
    assert re.search(r'filterMode: 0',meta),p
    assert all(value=='0' for value in re.findall(r'textureCompression: (\d+)',meta)),p
    textures.append(dict(path=p.relative_to(root).as_posix(),sha256=sha(p),size=list(image.size)))
assert len(textures)==7,len(textures)
summary['runtimeTextures']=textures
for name in ['prepare.py','draw.lua','integrate.py','prepare_small.py','draw_small.lua','integrate_small.py','sync_validation.py','run-tests.ps1','archive.py']:
    shutil.copy2(work/name,sources/name)
for rel in ['Assets/_Project/Scripts/UI/PixelImageText.cs','Assets/_Project/Scripts/UI/PromptImageResources.cs',
            'Assets/_Project/Tests/EditMode/PromptArtworkImageTests.cs','Assets/_Project/Tests/PlayMode/PromptArtworkPlayModeTests.cs']:
    shutil.copy2(root/rel,sources/Path(rel).name)
sources.joinpath('使用说明.txt').write_text('这些是本轮制作与验证源代码的归档副本。实际工作入口在 Logs/PromptArtwork20261021，Python 脚本按该目录解析项目路径。不要在本目录直接运行 prepare/integrate；它们的首次接入防重检查会拒绝重复应用。修改代码或提示文案后应重新导出字图、同步隔离工程并运行验证。\n',encoding='utf-8')
(records/'最终接入与验证.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
(work/'summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({key:summary[key] for key in ['fixedPhrases','charactersPerSize','nativeGlyphSizes','EditMode','PlayMode']},ensure_ascii=False))
print('Archived',len(screens),'fresh Unity renders;',len(textures),'runtime PNGs; all C# sources match the test project.')
