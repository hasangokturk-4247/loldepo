package com.decod.grm;

import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;
import android.util.Base64;
import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;
import java.security.KeyStore;
import java.security.SecureRandom;
import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.Mac;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;
import javax.crypto.spec.SecretKeySpec;

final class CryptoBox {
    static final String LOCAL_MAGIC="GRMLOCAL-ANDROID22\n";
    static final String CLOUD_MAGIC="GRMSYNC22\n";
    private static final String KEY_ALIAS="DECOD_GRM_ANDROID_LOCAL_V22";
    private CryptoBox(){}
    static String b64u(byte[] b){return Base64.encodeToString(b,Base64.URL_SAFE|Base64.NO_WRAP|Base64.NO_PADDING);}
    static byte[] b64uDec(String s){return Base64.decode(s,Base64.URL_SAFE|Base64.NO_WRAP|Base64.NO_PADDING);}
    static String b64(byte[] b){return Base64.encodeToString(b,Base64.NO_WRAP|Base64.NO_PADDING);}
    static byte[] b64Dec(String s){return Base64.decode(s,Base64.NO_WRAP|Base64.NO_PADDING);}
    static byte[] sha256(byte[] b)throws Exception{return java.security.MessageDigest.getInstance("SHA-256").digest(b);}
    static byte[] hmacSha256(byte[] key,byte[] msg)throws Exception{Mac m=Mac.getInstance("HmacSHA256");m.init(new SecretKeySpec(key,"HmacSHA256"));return m.doFinal(msg);}
    private static SecretKey localKey()throws Exception{KeyStore ks=KeyStore.getInstance("AndroidKeyStore");ks.load(null);if(ks.containsAlias(KEY_ALIAS))return ((KeyStore.SecretKeyEntry)ks.getEntry(KEY_ALIAS,null)).getSecretKey();KeyGenerator kg=KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES,"AndroidKeyStore");kg.init(new KeyGenParameterSpec.Builder(KEY_ALIAS,KeyProperties.PURPOSE_ENCRYPT|KeyProperties.PURPOSE_DECRYPT).setBlockModes(KeyProperties.BLOCK_MODE_GCM).setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE).setKeySize(256).build());return kg.generateKey();}
    static byte[] encryptLocal(byte[] plain)throws Exception{Cipher c=Cipher.getInstance("AES/GCM/NoPadding");c.init(Cipher.ENCRYPT_MODE,localKey());byte[] iv=c.getIV(),sealed=c.doFinal(plain),magic=LOCAL_MAGIC.getBytes(StandardCharsets.UTF_8);ByteBuffer out=ByteBuffer.allocate(magic.length+iv.length+sealed.length);out.put(magic).put(iv).put(sealed);return out.array();}
    static byte[] decryptLocal(byte[] data)throws Exception{byte[] magic=LOCAL_MAGIC.getBytes(StandardCharsets.UTF_8);if(data.length<=magic.length+12)throw new IllegalArgumentException("Yerel veri paketi eksik");for(int i=0;i<magic.length;i++)if(data[i]!=magic[i])throw new IllegalArgumentException("Yerel veri biçimi tanınmadı");byte[] iv=java.util.Arrays.copyOfRange(data,magic.length,magic.length+12),sealed=java.util.Arrays.copyOfRange(data,magic.length+12,data.length);Cipher c=Cipher.getInstance("AES/GCM/NoPadding");c.init(Cipher.DECRYPT_MODE,localKey(),new GCMParameterSpec(128,iv));return c.doFinal(sealed);}
    static byte[] tenantKey(String workspaceKey)throws Exception{byte[] k=b64uDec(workspaceKey.trim());if(k.length!=32)throw new IllegalArgumentException("G-Cloud şirket anahtarı geçersiz");return k;}
    static byte[] encryptTenant(String workspaceKey,byte[] plain,String magicText)throws Exception{byte[] key=tenantKey(workspaceKey),iv=new byte[12];new SecureRandom().nextBytes(iv);Cipher c=Cipher.getInstance("AES/GCM/NoPadding");c.init(Cipher.ENCRYPT_MODE,new SecretKeySpec(key,"AES"),new GCMParameterSpec(128,iv));byte[] sealed=c.doFinal(plain),magic=magicText.getBytes(StandardCharsets.UTF_8);ByteBuffer out=ByteBuffer.allocate(magic.length+iv.length+sealed.length);out.put(magic).put(iv).put(sealed);return out.array();}
    static byte[] decryptTenant(String workspaceKey,byte[] data,String magicText)throws Exception{byte[] magic=magicText.getBytes(StandardCharsets.UTF_8);if(data.length<=magic.length+12)throw new IllegalArgumentException("G-Rm veri paketi eksik");for(int i=0;i<magic.length;i++)if(data[i]!=magic[i])throw new IllegalArgumentException("G-Rm veri biçimi tanınmadı");byte[] iv=java.util.Arrays.copyOfRange(data,magic.length,magic.length+12),sealed=java.util.Arrays.copyOfRange(data,magic.length+12,data.length);Cipher c=Cipher.getInstance("AES/GCM/NoPadding");c.init(Cipher.DECRYPT_MODE,new SecretKeySpec(tenantKey(workspaceKey),"AES"),new GCMParameterSpec(128,iv));return c.doFinal(sealed);}
}