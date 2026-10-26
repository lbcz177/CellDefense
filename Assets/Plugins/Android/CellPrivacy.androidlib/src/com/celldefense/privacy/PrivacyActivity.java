package com.celldefense.privacy;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.Intent;
import android.graphics.Color;
import android.os.Bundle;
import android.view.KeyEvent;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.TextView;
import android.widget.Toast;

/** Launcher. Deliberately extends Android Activity and never references a Unity class. */
public final class PrivacyActivity extends Activity {
    public static final String REVIEW = "review_policy";
    private String policy;
    private boolean reviewing;
    private boolean startingGame;

    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        getWindow().getDecorView().setBackgroundColor(Color.rgb(239, 247, 243));
        reviewing = getIntent().getBooleanExtra(REVIEW, false) && ConsentStore.isAccepted(this);
        try { policy = ConsentStore.policy(this); }
        catch (Exception error) {
            new AlertDialog.Builder(this).setTitle("无法读取隐私政策")
                    .setMessage("请重新安装完整安装包。为保护你的选择，本次不会启动游戏。")
                    .setPositiveButton("关闭", (dialog, which) -> reject()).setCancelable(false).show();
            return;
        }
        if (reviewing) showFullPolicy(true);
        else if (ConsentStore.isAccepted(this)) startGame();
        else showConsent();
    }

    private TextView text(String value, int size) {
        TextView view = new TextView(this);
        view.setText(value);
        view.setTextSize(size);
        view.setTextColor(Color.rgb(25, 52, 46));
        view.setLineSpacing(dp(4), 1f);
        view.setPadding(dp(20), dp(12), dp(20), dp(12));
        return view;
    }

    private int dp(int value) { return Math.round(value * getResources().getDisplayMetrics().density); }

    private void constrain(AlertDialog dialog) {
        // Keep system dialog actions outside the scroll area on short landscape windows.
        dialog.getWindow().setLayout(Math.min(dp(640), getResources().getDisplayMetrics().widthPixels - dp(32)),
                ViewGroup.LayoutParams.WRAP_CONTENT);
    }

    private ScrollView scroll(String value) {
        ScrollView view = new ScrollView(this);
        view.setFillViewport(false);
        TextView copy = text(value, 15);
        copy.setTextIsSelectable(true);
        view.addView(copy);
        return view;
    }

    private int bodyHeight() {
        return Math.max(dp(72), Math.min(dp(270), getResources().getDisplayMetrics().heightPixels - dp(190)));
    }

    private void showConsent() {
        LinearLayout content = new LinearLayout(this);
        content.setOrientation(LinearLayout.VERTICAL);
        String summary = "欢迎使用《胞卫战》。请阅读隐私政策后自主选择。\n\n"
                + "本游戏使用 Unity 引擎。你同意后，引擎会获取设备传感器信息（如加速度计、陀螺仪的类型及支持情况），用于识别设备能力和适配运行；"
                + "获取 Android ID，用于引擎识别设备及运行环境校验。\n\n"
                + "本版本为离线游戏，未启用广告或自动统计，不申请联网权限。"
                + "同意前不启动 Unity 引擎；选择拒绝将退出，你可下次打开后重新选择。";
        content.addView(scroll(summary), new LinearLayout.LayoutParams(-1, bodyHeight()));
        Button details = new Button(this);
        details.setText("查看完整隐私政策");
        details.setOnClickListener(view -> showFullPolicy(false));
        content.addView(details, new LinearLayout.LayoutParams(-1, dp(48)));
        AlertDialog dialog = new AlertDialog.Builder(this).setTitle("隐私政策与授权")
                .setView(content).setNegativeButton("拒绝", (d, which) -> reject())
                .setPositiveButton("同意", null).setCancelable(false).create();
        dialog.setOnKeyListener((d, key, event) -> {
            if (key != KeyEvent.KEYCODE_BACK) return false;
            if (event.getAction() == KeyEvent.ACTION_UP) reject();
            return true;
        });
        dialog.setOnShowListener(d -> {
            constrain(dialog);
            dialog.getButton(AlertDialog.BUTTON_POSITIVE).setOnClickListener(view -> {
                if (!ConsentStore.accept(this)) {
                    Toast.makeText(this, "未能保存选择，请检查存储空间后重试。游戏尚未启动。", Toast.LENGTH_LONG).show();
                    return;
                }
                dialog.dismiss();
                startGame();
            });
        });
        dialog.show();
    }

    private void showFullPolicy(boolean canRevoke) {
        LinearLayout content = new LinearLayout(this);
        content.setOrientation(LinearLayout.VERTICAL);
        content.addView(scroll(policy), new LinearLayout.LayoutParams(-1, bodyHeight()));
        AlertDialog.Builder builder = new AlertDialog.Builder(this).setTitle("《胞卫战》隐私政策")
                .setView(content).setPositiveButton(canRevoke ? "关闭" : "返回", (d, which) -> {
                    if (canRevoke) finish();
                });
        if (canRevoke) builder.setNegativeButton("撤回同意并退出", null);
        AlertDialog dialog = builder.create();
        dialog.setCanceledOnTouchOutside(false);
        dialog.setOnCancelListener(d -> { if (canRevoke) finish(); });
        dialog.setOnShowListener(d -> {
            constrain(dialog);
            if (canRevoke) dialog.getButton(AlertDialog.BUTTON_NEGATIVE).setOnClickListener(view -> {
                if (!ConsentStore.revoke(this)) {
                    Toast.makeText(this, "未能保存撤回选择。请在系统设置中清除本游戏数据后退出。", Toast.LENGTH_LONG).show();
                    return;
                }
                finishAffinity();
                android.os.Process.killProcess(android.os.Process.myPid());
            });
        });
        dialog.show();
    }

    private void startGame() {
        if (startingGame || !ConsentStore.isAccepted(this)) return;
        startingGame = true;
        // A string component name avoids resolving/loading any engine class in this launcher.
        Intent intent = new Intent().setClassName(getPackageName(), "com.celldefense.privacy.ConsentUnityActivity");
        intent.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP | Intent.FLAG_ACTIVITY_SINGLE_TOP);
        startActivity(intent);
        finish();
    }

    private void reject() {
        ConsentStore.revoke(this);
        finishAndRemoveTask();
    }

    @Override public void onBackPressed() {
        if (reviewing) finish(); else reject();
    }
}
