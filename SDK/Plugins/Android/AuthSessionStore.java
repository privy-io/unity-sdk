package io.privy.unity.storage;

import android.content.Context;
import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;
import android.util.AtomicFile;
import java.io.File;
import java.io.FileOutputStream;
import java.nio.charset.StandardCharsets;
import java.security.KeyStore;
import java.security.GeneralSecurityException;
import java.util.Arrays;
import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

/** Device-local encrypted auth storage. No credential material is written to preferences. */
public final class AuthSessionStore {
    private static final int VERSION = 1;
    private static final int IV_BYTES = 12;
    private static final int TAG_BITS = 128;
    private static final int MAX_RECORD_BYTES = 1024 * 1024;
    private final String alias;
    private final AtomicFile file;

    public AuthSessionStore(Context context, String account) {
        if (account == null || !account.matches("[a-f0-9]{64}")) {
            throw new IllegalArgumentException("Invalid session storage account.");
        }
        alias = "io.privy.unity.auth-session.v1." + account;
        file = new AtomicFile(new File(context.getApplicationContext().getNoBackupFilesDir(), alias));
    }

    public String read() throws Exception {
        // AtomicFile recovers interrupted writes when opening the record.
        if (!file.getBaseFile().exists() && !new File(file.getBaseFile().getPath() + ".bak").exists()) {
            return null;
        }
        byte[] record = file.readFully();
        byte[] plaintext = null;
        try {
            if (record.length < 1 + IV_BYTES + TAG_BITS / 8 || record.length > MAX_RECORD_BYTES || record[0] != VERSION) {
                throw new GeneralSecurityException("Invalid session storage record.");
            }
            KeyStore keyStore = keyStore();
            SecretKey key = (SecretKey) keyStore.getKey(alias, null);
            if (key == null) throw new GeneralSecurityException("Session storage key is unavailable.");
            byte[] iv = Arrays.copyOfRange(record, 1, 1 + IV_BYTES);
            Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
            cipher.init(Cipher.DECRYPT_MODE, key, new GCMParameterSpec(TAG_BITS, iv));
            cipher.updateAAD(alias.getBytes(StandardCharsets.UTF_8));
            plaintext = cipher.doFinal(record, 1 + IV_BYTES, record.length - 1 - IV_BYTES);
            return new String(plaintext, StandardCharsets.UTF_8);
        } catch (GeneralSecurityException invalidSession) {
            // Missing/invalidated keys or modified ciphertext cannot restore a session.
            // Deletion failures propagate instead of silently falling back to plaintext.
            delete();
            return null;
        } finally {
            Arrays.fill(record, (byte) 0);
            if (plaintext != null) Arrays.fill(plaintext, (byte) 0);
        }
    }

    public void write(String value) throws Exception {
        byte[] plaintext = value.getBytes(StandardCharsets.UTF_8);
        FileOutputStream output = null;
        try {
            if (plaintext.length > MAX_RECORD_BYTES - 1 - IV_BYTES - TAG_BITS / 8) {
                throw new IllegalArgumentException("Session storage record is too large.");
            }
            KeyStore keyStore = keyStore();
            SecretKey key = (SecretKey) keyStore.getKey(alias, null);
            if (key == null) {
                KeyGenerator generator = KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, "AndroidKeyStore");
                generator.init(new KeyGenParameterSpec.Builder(alias, KeyProperties.PURPOSE_ENCRYPT | KeyProperties.PURPOSE_DECRYPT)
                    .setKeySize(256)
                    .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
                    .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
                    .setRandomizedEncryptionRequired(true)
                    .build());
                key = generator.generateKey();
            }
            Cipher cipher = Cipher.getInstance("AES/GCM/NoPadding");
            cipher.init(Cipher.ENCRYPT_MODE, key);
            byte[] iv = cipher.getIV();
            if (iv.length != IV_BYTES) throw new GeneralSecurityException("Unexpected session storage IV.");
            cipher.updateAAD(alias.getBytes(StandardCharsets.UTF_8));
            byte[] ciphertext = cipher.doFinal(plaintext);
            output = file.startWrite();
            output.write(VERSION);
            output.write(iv);
            output.write(ciphertext);
            file.finishWrite(output);
            output = null;
        } finally {
            if (output != null) file.failWrite(output);
            Arrays.fill(plaintext, (byte) 0);
        }
    }

    public void delete() throws Exception {
        file.delete();
        if (file.getBaseFile().exists() || new File(file.getBaseFile().getPath() + ".bak").exists()
                || new File(file.getBaseFile().getPath() + ".new").exists()) {
            throw new java.io.IOException("Could not remove persisted session.");
        }
        KeyStore keyStore = keyStore();
        keyStore.deleteEntry(alias);
    }

    private static KeyStore keyStore() throws Exception {
        KeyStore keyStore = KeyStore.getInstance("AndroidKeyStore");
        keyStore.load(null);
        return keyStore;
    }
}
