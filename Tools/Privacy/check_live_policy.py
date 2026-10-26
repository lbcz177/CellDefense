"""Check the policy without a login; retain failures without claiming a pass."""
import json
import hashlib
import urllib.request
import urllib.error
from pathlib import Path

root = Path(__file__).resolve().parents[2]
url = "https://cell-defense-privacy.jazzy-flint-2968.chatgpt.site/"
result = dict(url=url, anonymous=True, passed=False)
try:
    request = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(request, timeout=30) as response:
        page = response.read().decode()
        result.update(final_url=response.url, status=response.status,
                      contact_present="2558057120" in page, operator_present="ichen" in page,
                      section_count=page.count('<section id='))
    text_request = urllib.request.Request(url + "privacy-policy.txt", headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(text_request, timeout=30) as response:
        policy = response.read()
        source = (root / "Assets/Plugins/Android/CellPrivacy.androidlib/res/raw/cell_privacy_policy.txt").read_bytes()
        result.update(text_matches_packaged_policy=policy == source, policy_sha256=hashlib.sha256(policy).hexdigest())
    result["passed"] = (result["status"] == 200 and result["final_url"].rstrip('/') == url.rstrip('/')
                        and result["contact_present"] and result["operator_present"]
                        and result["section_count"] == 8 and result["text_matches_packaged_policy"])
except urllib.error.HTTPError as error:
    result.update(status=error.code, response_headers=dict(error.headers), response_excerpt=error.read(1500).decode(errors="replace"))
except Exception as error:
    result["error"] = str(error)
(root / "Logs/PrivacyFix20261022/site-live-validation.json").write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
print(json.dumps(result, ensure_ascii=False))
raise SystemExit(0 if result["passed"] else 1)
