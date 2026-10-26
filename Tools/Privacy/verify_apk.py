"""Inspect the actual built APK, not just the source manifest. No device writes."""
from pathlib import Path
import argparse
import hashlib
import json
import os
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Logs/PrivacyFix20261022"
ANDROID = Path("E:/UnityEditors/2022.3.53f1c1/Editor/Data/PlaybackEngines/AndroidPlayer")
SDK = ANDROID / "SDK"
BUILD_TOOLS = SDK / "build-tools/34.0.0"
A = "{http://schemas.android.com/apk/res/android}"


def run(args):
    env = os.environ.copy()
    env["JAVA_HOME"] = str(ANDROID / "OpenJDK")
    result = subprocess.run([str(arg) for arg in args], env=env, capture_output=True, encoding="utf-8", errors="replace")
    if result.returncode:
        raise RuntimeError(result.stderr or result.stdout)
    return result.stdout


def main():
    global OUT
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("apk", type=Path)
    parser.add_argument("--expected-version", default="0.1.4")
    parser.add_argument("--expected-code", type=int, default=5)
    parser.add_argument("--output", type=Path, default=OUT)
    args = parser.parse_args()
    OUT = args.output.resolve()
    OUT.mkdir(parents=True, exist_ok=True)
    apk = args.apk.resolve()
    text = run([SDK / "cmdline-tools/6.0/bin/apkanalyzer.bat", "manifest", "print", apk])
    (OUT / "apk-manifest.xml").write_text(text, encoding="utf-8")
    manifest = ET.fromstring(text[text.index("<manifest"):])
    assert manifest.get("package") == "com.celldefense.game"
    assert manifest.get(A + "versionName") == args.expected_version
    assert manifest.get(A + "versionCode") == str(args.expected_code)
    app = manifest.find("application")
    assert app.get(A + "name", "android.app.Application") == "android.app.Application"
    assert app.get(A + "debuggable", "false") == "false"
    assert app.get(A + "allowBackup") == "false"
    assert app.get(A + "fullBackupContent") and app.get(A + "dataExtractionRules")
    assert len(app.findall("provider")) == 0, "Auto-start providers need their own consent audit"
    assert len(app.findall("service")) == 0
    assert len(app.findall("receiver")) == 0
    activities = app.findall("activity")
    assert {activity.get(A + "name") for activity in activities} == {
        "com.celldefense.privacy.PrivacyActivity", "com.celldefense.privacy.ConsentUnityActivity"}
    launchers = []
    for activity in activities:
        if activity.get(A + "name").endswith("ConsentUnityActivity"):
            assert activity.get(A + "exported") == "false"
        for intent in activity.findall("intent-filter"):
            if any(category.get(A + "name") == "android.intent.category.LAUNCHER" for category in intent.findall("category")):
                launchers.append(activity.get(A + "name"))
    assert launchers == ["com.celldefense.privacy.PrivacyActivity"]
    permissions = sorted(permission.get(A + "name") for permission in manifest.findall("uses-permission"))
    assert not {"android.permission.INTERNET", "android.permission.ACCESS_NETWORK_STATE",
                "android.permission.READ_PHONE_STATE", "android.permission.ACCESS_FINE_LOCATION",
                "android.permission.CAMERA", "android.permission.RECORD_AUDIO"}.intersection(permissions)
    source_policy = ROOT / "Assets/Plugins/Android/CellPrivacy.androidlib/res/raw/cell_privacy_policy.txt"
    resources = run([BUILD_TOOLS / "aapt.exe", "dump", "resources", apk])
    assert ":raw/cell_privacy_policy:" in resources
    with zipfile.ZipFile(apk) as archive:
        assert archive.testzip() is None
        # AAPT can shorten ZIP paths (e.g. res/Hh.txt), while keeping the raw resource name.
        expected_policy = source_policy.read_bytes()
        policy_entries = [entry.filename for entry in archive.infolist()
                          if entry.filename.startswith("res/") and entry.file_size == len(expected_policy)
                          and archive.read(entry) == expected_policy]
        assert len(policy_entries) == 1
        for abi in ("armeabi-v7a", "arm64-v8a"):
            for library in ("libunity.so", "libil2cpp.so"):
                assert archive.read(f"lib/{abi}/{library}")[:4] == b"\x7fELF"
        dex = OUT / "classes.dex"
        dex.write_bytes(archive.read("classes.dex"))
    disassembly = run([BUILD_TOOLS / "dexdump.exe", "-d", dex])
    parts = re.split(r"(?=^Class #\d+)", disassembly, flags=re.MULTILINE)
    relevant = [part for part in parts if "Class descriptor  : 'Lcom/celldefense/privacy/" in part]
    assert relevant
    (OUT / "privacy-dex.txt").write_text("\n".join(relevant), encoding="utf-8")
    gate = next(part for part in relevant if "Class descriptor  : 'Lcom/celldefense/privacy/PrivacyActivity;'" in part)
    # The launcher bytecode itself must not resolve any Unity library class.
    assert "Lcom/unity3d/player/" not in gate
    for name in ("isAccepted", "accept", "revoke"):
        assert re.search(r"ConsentStore;?\." + name + r"[:(]", disassembly), name
    signatures = {}
    for label, file in (("validation", apk), ("previous", ROOT / "Builds/Android/CellDefense-0.1.3.apk")):
        output = run([BUILD_TOOLS / "apksigner.bat", "verify", "--verbose", "--print-certs", file])
        (OUT / (label + "-signature.txt")).write_text(output, encoding="utf-8")
        signatures[label] = re.search(r"certificate SHA-256 digest: ([0-9a-f]+)", output).group(1)
    assert signatures["validation"] == signatures["previous"]
    summary = dict(result="Passed", apk=str(apk), bytes=apk.stat().st_size,
                   size_mib=round(apk.stat().st_size / 1024**2, 2),
                   sha256=hashlib.sha256(apk.read_bytes()).hexdigest(),
                   version_name=args.expected_version, version_code=args.expected_code, launcher=launchers[0],
                   unity_activity_exported=False, application_is_android_default=True,
                   auto_start_providers=0, permissions=permissions,
                   packaged_policy_matches=True, policy_zip_entry=policy_entries[0],
                   launcher_dex_has_no_unity_class_references=True,
                   signing_certificate_matches_previous=True, development=False,
                   contact_details_pending=any(marker in expected_policy.decode("utf-8") for marker in ("尚待补充", "本地整改验证稿")),
                   device_tested=False, remote_writes=False)
    (OUT / "apk-validation.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(summary, ensure_ascii=False))


if __name__ == "__main__":
    main()
