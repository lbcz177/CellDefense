"""Local source audit + real ConsentStore JVM tests (Android storage API test doubles).

This does not claim device instrumentation or certify SDK data access behavior.
"""
from pathlib import Path
import hashlib
import html
import json
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Logs/PrivacyFix20261022"
JAVA = Path("E:/UnityEditors/2022.3.53f1c1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin")
PLUGIN = ROOT / "Assets/Plugins/Android"
NATIVE = PLUGIN / "CellPrivacy.androidlib/src/com/celldefense/privacy"
A = "{http://schemas.android.com/apk/res/android}"
T = "{http://schemas.android.com/tools}"

STUBS = {
    "android/content/Context.java": '''package android.content;
import android.content.res.Resources;
public abstract class Context {
 public static final int MODE_PRIVATE=0;
 public abstract Resources getResources();
 public abstract String getPackageName();
 public abstract SharedPreferences getSharedPreferences(String name,int mode);
}''',
    "android/content/res/Resources.java": '''package android.content.res;
import java.io.*;
public class Resources {
 public String policy="policy version one"; public boolean available=true;
 public int getIdentifier(String name,String type,String pkg){return available?1:0;}
 public InputStream openRawResource(int id){return new ByteArrayInputStream(policy.getBytes(java.nio.charset.StandardCharsets.UTF_8));}
}''',
    "android/content/SharedPreferences.java": '''package android.content;
public interface SharedPreferences {
 String getString(String key,String fallback); Editor edit();
 interface Editor {Editor putString(String key,String value);Editor putLong(String key,long value);Editor clear();boolean commit();}
}''',
    "ConsentStoreJvmTest.java": '''import android.content.*;
import android.content.res.Resources;
import com.celldefense.privacy.ConsentStore;
import java.util.*;
public final class ConsentStoreJvmTest {
 static int checks=0;
 static void check(boolean condition,String label){checks++;if(!condition)throw new AssertionError(label);System.out.println("PASS "+label);}
 static final class Preferences implements SharedPreferences {
  Map<String,String> values=new HashMap<>(); boolean diskOk=true;
  public String getString(String key,String fallback){return values.getOrDefault(key,fallback);}
  public Editor edit(){return new Edit();}
  final class Edit implements Editor {
   Map<String,String> updates=new HashMap<>();boolean clear=false;
   public Editor putString(String key,String value){updates.put(key,value);return this;}
   public Editor putLong(String key,long value){updates.put(key,Long.toString(value));return this;}
   public Editor clear(){clear=true;return this;}
   public boolean commit(){if(clear)values.clear();values.putAll(updates);return diskOk;}
  }
 }
 static final class App extends Context {
  Resources resources=new Resources();Preferences preferences=new Preferences();
  public Resources getResources(){return resources;}
  public String getPackageName(){return "com.celldefense.game";}
  public SharedPreferences getSharedPreferences(String name,int mode){return preferences;}
 }
 public static void main(String[] args){
  App app=new App();
  check(!ConsentStore.isAccepted(app),"first launch has no implied consent");
  check(ConsentStore.accept(app),"explicit consent commits successfully");
  check(ConsentStore.isAccepted(app),"unchanged policy remembers explicit consent");
  check(app.preferences.values.get("accepted_policy_sha256").length()==64,"stores policy hash, not a device identifier");
  check(app.preferences.values.containsKey("accepted_at_ms"),"stores consent time");
  app.resources.policy="policy version two";
  check(!ConsentStore.isAccepted(app),"changed policy requires fresh consent");
  check(ConsentStore.accept(app)&&ConsentStore.isAccepted(app),"new policy can be accepted explicitly");
  check(ConsentStore.revoke(app)&&!ConsentStore.isAccepted(app),"withdrawal clears consent");
  app.preferences.diskOk=false;
  check(!ConsentStore.accept(app),"disk failure must not count as acceptance");
  check(!ConsentStore.isAccepted(app),"failed disk commit is rolled back in memory too");
  app.preferences.diskOk=true;app.resources.available=false;
  check(!ConsentStore.accept(app),"missing policy cannot be accepted");
  check(!ConsentStore.isAccepted(app),"missing policy fails closed");
  System.out.println("CHECKS="+checks);
 }
}''',
}


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    manifest = ET.parse(PLUGIN / "AndroidManifest.xml").getroot()
    app = manifest.find("application")
    launchers = []
    for activity in app.findall("activity"):
        for intent in activity.findall("intent-filter"):
            if any(category.get(A + "name") == "android.intent.category.LAUNCHER" for category in intent.findall("category")):
                launchers.append(activity.get(A + "name"))
    assert launchers == ["com.celldefense.privacy.PrivacyActivity"]
    game = next(activity for activity in app.findall("activity") if activity.get(A + "name").endswith("ConsentUnityActivity"))
    assert game.get(A + "exported") == "false"
    assert app.get(A + "allowBackup") == "false"
    removed = {permission.get(A + "name") for permission in manifest.findall("uses-permission") if permission.get(T + "node") == "remove"}
    assert {"android.permission.INTERNET", "android.permission.ACCESS_NETWORK_STATE"} <= removed
    launcher = (NATIVE / "PrivacyActivity.java").read_text(encoding="utf-8")
    store = (NATIVE / "ConsentStore.java").read_text(encoding="utf-8")
    for forbidden in ("import com.unity3d", "android.provider.Settings", "SensorManager", "System.loadLibrary", "UnityPlayer("):
        assert forbidden not in launcher + store, forbidden
    analytics = (ROOT / "ProjectSettings/UnityConnectSettings.asset").read_text(encoding="utf-8").split("UnityAnalyticsSettings:")[1].split("UnityAdsSettings:")[0]
    assert "m_Enabled: 0" in analytics and "m_InitializeOnStartup: 0" in analytics
    stub_root = OUT / "jvm-test-src"
    files = []
    for name, content in STUBS.items():
        path = stub_root / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content, encoding="utf-8")
        files.append(str(path))
    classes = OUT / "jvm-test-classes"
    classes.mkdir(exist_ok=True)
    compilation = subprocess.run([str(JAVA / "javac.exe"), "-encoding", "UTF-8", "-d", str(classes), *files,
                                  str(NATIVE / "ConsentStore.java")], capture_output=True, encoding="utf-8", errors="replace")
    (OUT / "jvm-compile.log").write_text(compilation.stdout + compilation.stderr, encoding="utf-8")
    compilation.check_returncode()
    run = subprocess.run([str(JAVA / "java.exe"), "-cp", str(classes), "ConsentStoreJvmTest"], capture_output=True, encoding="utf-8", errors="replace")
    (OUT / "jvm-tests.log").write_text(run.stdout + run.stderr, encoding="utf-8")
    run.check_returncode()
    assert "CHECKS=12" in run.stdout
    policy_path = PLUGIN / "CellPrivacy.androidlib/res/raw/cell_privacy_policy.txt"
    policy = policy_path.read_text(encoding="utf-8")
    assert all(term in policy for term in ("Android ID", "设备传感器信息", "用于", "同意", "拒绝"))
    inputs_path = ROOT / "Docs/Privacy/publication-inputs.json"
    inputs = json.loads(inputs_path.read_text(encoding="utf-8")) if inputs_path.exists() else {}
    contact_complete = (bool(inputs.get("contact_publication_authorized"))
                        and not inputs.get("operator_confirmation_pending", True)
                        and bool(inputs.get("operator_name"))
                        and inputs["operator_name"] in policy
                        and bool(inputs.get("contact_value")) and inputs["contact_value"] in policy
                        and not any(marker in policy for marker in ("本地整改验证稿", "尚待补充")))
    if inputs.get("operator_name"):
        assert contact_complete, "User-confirmed operator/contact must match the packaged policy"
    note = "联系资料已补齐；公开页面是否上线请以部署记录为准。" if contact_complete else "本地整改验证稿，联系渠道尚未补齐；本页面未公开发布。"
    page = '<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>胞卫战隐私政策 · 审阅副本</title><style>body{max-width:860px;margin:40px auto;padding:0 22px;font:16px/1.9 system-ui,"Microsoft YaHei",sans-serif;background:#f4f8f5;color:#18332d}pre{white-space:pre-wrap;font:inherit}.note{padding:16px;background:#fff0c6;border-radius:8px}</style><p class="note">' + note + '</p><pre>' + html.escape(policy) + '</pre></html>'
    review = ROOT / "Docs/Privacy"
    review.mkdir(parents=True, exist_ok=True)
    (review / "privacy-policy-draft.html").write_text(page, encoding="utf-8")
    summary = dict(result="Passed", consent_jvm_checks=12, source_manifest_audit=True,
                   policy_sha256=hashlib.sha256(policy_path.read_bytes()).hexdigest(),
                   policy_contact_complete=contact_complete, device_tested=False, runtime_sensor_or_identifier_instrumentation=False,
                   remote_writes=False)
    (OUT / "source-validation.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(run.stdout.strip())
    print(json.dumps(summary))


if __name__ == "__main__":
    main()
