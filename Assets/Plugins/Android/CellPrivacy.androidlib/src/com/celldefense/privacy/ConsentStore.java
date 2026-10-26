package com.celldefense.privacy;

import android.content.Context;
import android.content.SharedPreferences;
import java.io.ByteArrayOutputStream;
import java.io.InputStream;
import java.security.MessageDigest;

/** Only local consent storage and a packaged text resource; no engine or SDK references. */
public final class ConsentStore {
    private static final String FILE = "cell_privacy_consent";
    private static final String REVISION = "accepted_policy_sha256";
    private ConsentStore() { }

    public static String policy(Context context) throws Exception {
        int id = context.getResources().getIdentifier("cell_privacy_policy", "raw", context.getPackageName());
        if (id == 0) throw new IllegalStateException("Packaged privacy policy is missing");
        try (InputStream input = context.getResources().openRawResource(id);
             ByteArrayOutputStream output = new ByteArrayOutputStream()) {
            byte[] buffer = new byte[4096];
            int count;
            while ((count = input.read(buffer)) != -1) output.write(buffer, 0, count);
            return output.toString("UTF-8");
        }
    }

    private static String revision(Context context) throws Exception {
        byte[] digest = MessageDigest.getInstance("SHA-256").digest(policy(context).getBytes("UTF-8"));
        StringBuilder hex = new StringBuilder();
        for (byte value : digest) hex.append(String.format(java.util.Locale.ROOT, "%02x", value & 0xff));
        return hex.toString();
    }

    public static boolean isAccepted(Context context) {
        try {
            return revision(context).equals(context.getSharedPreferences(FILE, Context.MODE_PRIVATE)
                    .getString(REVISION, ""));
        } catch (Exception ignored) {
            return false; // Missing/changed/unreadable policy always requires fresh consent.
        }
    }

    public static boolean accept(Context context) {
        try {
            SharedPreferences preferences = context.getSharedPreferences(FILE, Context.MODE_PRIVATE);
            boolean saved = preferences.edit()
                    .putString(REVISION, revision(context))
                    .putLong("accepted_at_ms", System.currentTimeMillis()).commit();
            // Android may update memory even when a disk commit fails. Roll that back too.
            if (!saved) preferences.edit().clear().commit();
            return saved;
        } catch (Exception ignored) { return false; }
    }

    public static boolean revoke(Context context) {
        return context.getSharedPreferences(FILE, Context.MODE_PRIVATE).edit().clear().commit();
    }
}
