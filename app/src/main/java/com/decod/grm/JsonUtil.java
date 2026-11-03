package com.decod.grm;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.Iterator;

final class JsonUtil {
    private JsonUtil() {}
    static String s(JSONObject o, String k) { return o == null ? "" : o.optString(k, "").trim(); }
    static long l(JSONObject o, String k) { return o == null ? 0L : o.optLong(k, 0L); }
    static int i(JSONObject o, String k) { return o == null ? 0 : o.optInt(k, 0); }
    static boolean b(JSONObject o, String k) { return o != null && o.optBoolean(k, false); }
    static JSONArray a(JSONObject o, String k) { JSONArray x = o == null ? null : o.optJSONArray(k); return x == null ? new JSONArray() : x; }
    static JSONObject o(JSONObject x, String k) { JSONObject v = x == null ? null : x.optJSONObject(k); return v == null ? new JSONObject() : v; }
    static JSONObject deepCopy(JSONObject src) { if (src == null) return new JSONObject(); try { return new JSONObject(src.toString()); } catch (JSONException e) { return new JSONObject(); } }
    static String norm(String x) { return x == null ? "" : x.trim().toUpperCase(java.util.Locale.ROOT); }
    static String sha256Hex(byte[] b) { try { byte[] h=MessageDigest.getInstance("SHA-256").digest(b); StringBuilder sb=new StringBuilder(h.length*2); for(byte v:h) sb.append(String.format(java.util.Locale.ROOT,"%02X",v)); return sb.toString(); } catch(Exception e){ throw new IllegalStateException(e); } }
    static boolean jsonEqual(Object a,Object b){ if(a==b)return true; if(a==null||b==null)return false; return a.toString().equals(b.toString()); }
    static JSONObject mergeObject(JSONObject base,JSONObject patch)throws JSONException{ JSONObject out=deepCopy(base); Iterator<String> it=patch.keys(); while(it.hasNext()){String k=it.next();out.put(k,patch.get(k));} return out; }
    static byte[] utf8(String s){return s.getBytes(StandardCharsets.UTF_8);}
}