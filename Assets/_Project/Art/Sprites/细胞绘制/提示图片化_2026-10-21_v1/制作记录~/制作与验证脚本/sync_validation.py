from pathlib import Path
import hashlib, shutil, json
root=Path(__file__).resolve().parents[2]
target=root/'Logs/CodexTradingTests'
assert (target/'Library').is_dir()
copied=[]
for folder in ['Assets','Packages','ProjectSettings']:
    for p in (root/folder).rglob('*'):
        if not p.is_file() or any(part.endswith('~') for part in p.parts): continue
        rel=p.relative_to(root); dest=target/rel
        if dest.exists() and p.stat().st_size==dest.stat().st_size and hashlib.sha256(p.read_bytes()).digest()==hashlib.sha256(dest.read_bytes()).digest(): continue
        dest.parent.mkdir(parents=True,exist_ok=True)
        shutil.copy2(p,dest); copied.append(rel.as_posix())
(root/'Logs/PromptArtwork20261021/synced.json').write_text(json.dumps(copied,ensure_ascii=False,indent=2),encoding='utf-8')
print('Synchronized',len(copied),'changed files into the existing isolated Unity validation project.')

