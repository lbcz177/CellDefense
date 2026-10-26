package com.celldefense.privacy;

import android.app.Activity;
import android.content.Intent;
import android.content.res.Configuration;
import android.os.Bundle;
import android.view.Gravity;
import android.view.KeyEvent;
import android.view.MotionEvent;
import android.view.View;
import android.view.Window;
import android.widget.Button;
import android.widget.FrameLayout;
import com.unity3d.player.IUnityPlayerLifecycleEvents;
import com.unity3d.player.UnityPlayer;

/** Unity 2022.3 Activity forwarding, with consent checked before player construction.
 * Extends Activity directly: calling UnityPlayerActivity.super.onCreate would load Unity.
 * Every callback handles the uninitialized case, including Android restoring an old task.
 */
public final class ConsentUnityActivity extends Activity implements IUnityPlayerLifecycleEvents {
    protected UnityPlayer mUnityPlayer; // Unity's native code relies on this field name.
    private Button privacyButton;

    @Override protected void onCreate(Bundle state) {
        requestWindowFeature(Window.FEATURE_NO_TITLE);
        super.onCreate(state);
        if (!ConsentStore.isAccepted(this)) {
            startActivity(new Intent(this, PrivacyActivity.class));
            finish();
            return;
        }
        mUnityPlayer = new UnityPlayer(this, this);
        setContentView(mUnityPlayer);
        mUnityPlayer.requestFocus();
    }

    public void setPrivacyButtonVisible(final boolean visible) {
        runOnUiThread(() -> {
            if (mUnityPlayer == null || isFinishing()) return;
            if (privacyButton == null && visible) {
                privacyButton = new Button(this);
                privacyButton.setText("隐私政策");
                privacyButton.setTextSize(14);
                privacyButton.setContentDescription("查看隐私政策或撤回同意");
                privacyButton.setOnClickListener(view -> {
                    Intent intent = new Intent(this, PrivacyActivity.class);
                    intent.putExtra(PrivacyActivity.REVIEW, true);
                    startActivity(intent);
                });
                int margin = Math.round(12 * getResources().getDisplayMetrics().density);
                FrameLayout.LayoutParams layout = new FrameLayout.LayoutParams(-2, -2, Gravity.BOTTOM | Gravity.LEFT);
                layout.setMargins(margin, margin, margin, margin);
                addContentView(privacyButton, layout);
            }
            if (privacyButton != null) privacyButton.setVisibility(visible ? View.VISIBLE : View.GONE);
        });
    }

    @Override public void onUnityPlayerUnloaded() { moveTaskToBack(true); }
    @Override public void onUnityPlayerQuitted() { }
    @Override protected void onNewIntent(Intent intent) {
        super.onNewIntent(intent);
        setIntent(intent);
        if (mUnityPlayer != null) mUnityPlayer.newIntent(intent);
    }
    @Override protected void onStart() { super.onStart(); if (mUnityPlayer != null) mUnityPlayer.onStart(); }
    @Override protected void onStop() { super.onStop(); if (mUnityPlayer != null) mUnityPlayer.onStop(); }
    @Override protected void onPause() { super.onPause(); if (mUnityPlayer != null) mUnityPlayer.onPause(); }
    @Override protected void onResume() { super.onResume(); if (mUnityPlayer != null) mUnityPlayer.onResume(); }
    @Override protected void onDestroy() {
        if (mUnityPlayer != null) mUnityPlayer.destroy();
        super.onDestroy();
    }
    @Override public void onLowMemory() { super.onLowMemory(); if (mUnityPlayer != null) mUnityPlayer.lowMemory(); }
    @Override public void onTrimMemory(int level) {
        super.onTrimMemory(level);
        if (level == TRIM_MEMORY_RUNNING_CRITICAL && mUnityPlayer != null) mUnityPlayer.lowMemory();
    }
    @Override public void onConfigurationChanged(Configuration config) {
        super.onConfigurationChanged(config);
        if (mUnityPlayer != null) mUnityPlayer.configurationChanged(config);
    }
    @Override public void onWindowFocusChanged(boolean focused) {
        super.onWindowFocusChanged(focused);
        if (mUnityPlayer != null) mUnityPlayer.windowFocusChanged(focused);
    }
    @Override public boolean dispatchKeyEvent(KeyEvent event) {
        if (mUnityPlayer != null && event.getAction() == KeyEvent.ACTION_MULTIPLE) return mUnityPlayer.injectEvent(event);
        return super.dispatchKeyEvent(event);
    }
    @Override public boolean onKeyUp(int code, KeyEvent event) {
        return mUnityPlayer != null ? mUnityPlayer.onKeyUp(code, event) : super.onKeyUp(code, event);
    }
    @Override public boolean onKeyDown(int code, KeyEvent event) {
        return mUnityPlayer != null ? mUnityPlayer.onKeyDown(code, event) : super.onKeyDown(code, event);
    }
    @Override public boolean onTouchEvent(MotionEvent event) {
        return mUnityPlayer != null ? mUnityPlayer.onTouchEvent(event) : super.onTouchEvent(event);
    }
    @Override public boolean onGenericMotionEvent(MotionEvent event) {
        return mUnityPlayer != null ? mUnityPlayer.onGenericMotionEvent(event) : super.onGenericMotionEvent(event);
    }
}
