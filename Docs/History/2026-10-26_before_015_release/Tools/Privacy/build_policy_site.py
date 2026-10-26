"""Build the static independent policy page from the exact Android policy text."""
from pathlib import Path
import argparse
import hashlib
import html
import json
import re

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Assets/Plugins/Android/CellPrivacy.androidlib/res/raw/cell_privacy_policy.txt"
OUTPUT = ROOT / "Sites/cell-defense-privacy/dist"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--require-complete", action="store_true")
    args = parser.parse_args()
    text = SOURCE.read_text(encoding="utf-8")
    draft = any(marker in text for marker in ("本地整改验证稿", "尚待补充", "联系渠道待补"))
    if args.require_complete and draft:
        raise SystemExit("Public policy not ready: complete the actual operator/contact details first.")
    registration = json.loads((ROOT / "Docs/Privacy/site-registration.json").read_text(encoding="utf-8"))
    origin = registration["expected_url"].rstrip("/")
    sections = re.split(r"\n(?=[一二三四五六七八九十]+、)", text.strip())
    intro, sections = sections[0], sections[1:]
    assert len(sections) == 8
    navigation, articles = [], []
    for i, section in enumerate(sections, 1):
        title, body = section.split("\n", 1)
        navigation.append(f'<a href="#section-{i}">{html.escape(title)}</a>')
        paragraphs = []
        for line in body.splitlines():
            if not line.strip():
                continue
            copy = html.escape(line)
            copy = re.sub(r"https://[^\s<]+", lambda match: '<a href="' + match.group(0) + '">' + match.group(0) + '</a>', copy)
            paragraphs.append("<p>" + copy + "</p>")
        articles.append(f'<section id="section-{i}"><h2>{html.escape(title)}</h2>{"".join(paragraphs)}</section>')
    notice = '<div class="draft" role="note">预览稿 · 开发者联系渠道尚待补齐，本页暂不作为公开政策使用。</div>' if draft else ""
    document = '''<!doctype html>
<html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>胞卫战｜隐私政策</title><meta name="description" content="《胞卫战》Android 离线试玩版隐私政策：Unity 引擎信息处理、授权选择、本地记录与撤回方式。">
<link rel="canonical" href="__ORIGIN__/"><meta name="color-scheme" content="light">
<style>
:root{--ink:#163e36;--body:#304c45;--paper:#f7faf6;--line:#dce6de;--accent:#236a58}*{box-sizing:border-box}html{scroll-behavior:smooth;scroll-padding-top:28px}body{margin:0;color:var(--body);background:var(--paper);font:16px/1.95 system-ui,-apple-system,"Segoe UI","Microsoft YaHei",sans-serif}a{color:var(--accent);text-underline-offset:4px;overflow-wrap:anywhere}a:focus-visible{outline:3px solid #7fbda9;outline-offset:4px}header{border-bottom:1px solid var(--line);background:white}header .wrap{max-width:1160px;padding:44px 28px 36px;margin:auto}.brand{font-size:14px;font-weight:650;letter-spacing:.08em;color:var(--accent);display:flex;align-items:center;gap:10px}.mark{width:22px;height:22px;border:2px solid #35866c;border-radius:50%;display:inline-block;box-shadow:inset 0 0 0 4px #e0f0e4}h1{color:var(--ink);font-size:clamp(32px,5vw,44px);line-height:1.35;margin:20px 0 10px;letter-spacing:-.03em}.sub{font-size:14px;margin:0;color:#5d746b}.facts{display:flex;flex-wrap:wrap;gap:10px;margin-top:24px}.facts span{padding:5px 13px;border:1px solid var(--line);border-radius:30px;font-size:13px;background:#f8fbf8}.layout{max-width:1160px;margin:auto;display:grid;grid-template-columns:225px minmax(0,1fr);gap:60px;padding:36px 28px 70px}aside{position:sticky;top:28px;align-self:start;font-size:13px}.nav-label{text-transform:uppercase;color:#6c8277;font-size:12px;letter-spacing:.1em;margin:0 0 12px}nav a{display:block;text-decoration:none;padding:8px 0;color:#526e63;line-height:1.6}nav a:hover{color:#155d46;text-decoration:underline}.download{display:inline-block;margin-top:18px;padding:9px 14px;border:1px solid #bdd4c5;border-radius:8px;text-decoration:none}article{min-width:0}article section{padding:0 0 24px;margin:0 0 26px;border-bottom:1px solid var(--line)}h2{font-size:21px;line-height:1.6;color:var(--ink);margin:0 0 15px;font-weight:650}p{margin:0 0 14px;overflow-wrap:anywhere}.draft{padding:15px 18px;border:1px solid #ead59a;border-radius:10px;background:#fff8df;font-size:14px;margin-bottom:28px;color:#715917}footer{border-top:1px solid var(--line);padding:25px 28px;color:#657a6f;font-size:12px;text-align:center;background:white}.skip{position:absolute;left:12px;top:-80px;background:white;padding:10px}.skip:focus{top:10px}@media(max-width:820px){.layout{grid-template-columns:1fr;gap:25px;padding:25px 20px 45px}aside{position:static}nav{display:grid;grid-template-columns:1fr 1fr;gap:0 18px}.download{margin-top:10px}header .wrap{padding:30px 20px 28px}}@media(max-width:440px){nav{grid-template-columns:1fr}body{font-size:15px}h2{font-size:19px}}@media(prefers-reduced-motion:reduce){html{scroll-behavior:auto}}@media print{aside,.facts,.download,.skip{display:none}.layout{display:block;max-width:100%;padding:20px}header .wrap{padding:0}body,header,footer{background:white}a{color:inherit}section{break-inside:auto}h2{break-after:avoid}}
</style></head><body><a class="skip" href="#policy">跳到政策正文</a><header><div class="wrap"><div class="brand"><span class="mark" aria-hidden="true"></span>胞卫战 · 玩家信息与隐私</div><h1>隐私政策</h1><p class="sub">更新日期：2026 年 10 月 22 日 · 适用于 Android 离线试玩版</p><div class="facts"><span>同意后启动引擎</span><span>离线运行</span><span>支持查看与撤回</span></div></div></header><main class="layout"><aside aria-label="政策目录"><p class="nav-label">政策目录</p><nav>__NAV__</nav><a class="download" href="privacy-policy.txt" download>下载政策文本 ↓</a></aside><article id="policy">__NOTICE____ARTICLES__</article></main><footer>《胞卫战》隐私政策 · 本页不加载统计脚本、广告或第三方字体。</footer></body></html>'''
    document = (document.replace("__ORIGIN__", html.escape(origin, quote=True))
                .replace("__NAV__", "".join(navigation)).replace("__ARTICLES__", "".join(articles)).replace("__NOTICE__", notice))
    OUTPUT.mkdir(parents=True, exist_ok=True)
    (OUTPUT / "index.html").write_text(document, encoding="utf-8")
    (OUTPUT / "privacy-policy.txt").write_bytes(SOURCE.read_bytes())
    (OUTPUT / "robots.txt").write_text("User-agent: *\nAllow: /\n", encoding="utf-8")
    result = dict(project_id=registration["project_id"], directory=str(OUTPUT), section_count=8,
                  policy_sha256=hashlib.sha256(SOURCE.read_bytes()).hexdigest(), contact_complete=not draft,
                  draft=draft, no_scripts=True, expected_url=origin + "/", published=False)
    (ROOT / "Docs/Privacy/site-build.json").write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(result, ensure_ascii=False))


if __name__ == "__main__":
    main()
