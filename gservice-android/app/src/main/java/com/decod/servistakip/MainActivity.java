package com.decod.servistakip;

import android.Manifest;
import android.app.Activity;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.Paint;
import android.graphics.Path;
import android.graphics.Rect;
import android.graphics.drawable.ColorDrawable;
import android.graphics.drawable.Drawable;
import android.graphics.drawable.GradientDrawable;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.view.WindowInsets;
import android.webkit.JavascriptInterface;
import android.webkit.PermissionRequest;
import android.webkit.WebChromeClient;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.FrameLayout;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.TextView;

import androidx.work.ExistingPeriodicWorkPolicy;
import androidx.work.PeriodicWorkRequest;
import androidx.work.WorkManager;

import com.google.zxing.integration.android.IntentIntegrator;
import com.google.zxing.integration.android.IntentResult;

import org.json.JSONArray;
import org.json.JSONObject;

import java.util.ArrayList;
import java.util.concurrent.TimeUnit;

public class MainActivity extends Activity {
    private static final String APP_URL = "https://decod-servis-api-production.up.railway.app/app";
    private static final String CHANNEL_ID = "gservice_alerts";
    private WebView web;
    private LinearLayout drawer;
    private View scrim;
    private FrameLayout root;
    private FrameLayout.LayoutParams webLp;
    private FrameLayout.LayoutParams hamburgerLp;
    private FrameLayout.LayoutParams drawerLp;
    private final Handler handler = new Handler(Looper.getMainLooper());

    private static final int HOME=1, QR=2, BOX=3, WRENCH=4, CAL=5, JOB=6,
            HISTORY=7, BELL=8, USER=9, SETTINGS=10, LOGOUT=11, MENU=12, CUSTOMERS=13;

    @Override public void onCreate(Bundle b) {
        super.onCreate(b);
        getWindow().setStatusBarColor(Color.rgb(17,24,39));
        getWindow().setNavigationBarColor(Color.rgb(17,24,39));
        requestAppPermissions();
        createNotificationChannel();
        scheduleBackgroundAlerts();
        buildUi();
        configureWebView();
        web.loadUrl(APP_URL + "?android=1");
        handler.postDelayed(alertPoller, 8000);
    }

    @Override protected void onDestroy() {
        handler.removeCallbacks(alertPoller);
        super.onDestroy();
    }

    private void requestAppPermissions() {
        if (Build.VERSION.SDK_INT < 23) return;
        ArrayList<String> p = new ArrayList<>();
        if (checkSelfPermission(Manifest.permission.CAMERA) != PackageManager.PERMISSION_GRANTED) {
            p.add(Manifest.permission.CAMERA);
        }
        if (Build.VERSION.SDK_INT >= 33 &&
                checkSelfPermission(Manifest.permission.POST_NOTIFICATIONS) != PackageManager.PERMISSION_GRANTED) {
            p.add(Manifest.permission.POST_NOTIFICATIONS);
        }
        if (!p.isEmpty()) requestPermissions(p.toArray(new String[0]), 20);
    }

    private void createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= 26) {
            NotificationManager nm=(NotificationManager)getSystemService(Context.NOTIFICATION_SERVICE);
            NotificationChannel c=new NotificationChannel(CHANNEL_ID,"G-Service Bildirimleri",NotificationManager.IMPORTANCE_DEFAULT);
            c.setDescription("Parça bekleyen ve servis uyarıları");
            nm.createNotificationChannel(c);
        }
    }

    private void scheduleBackgroundAlerts() {
        PeriodicWorkRequest req = new PeriodicWorkRequest.Builder(AlertWorker.class, 15, TimeUnit.MINUTES).build();
        WorkManager.getInstance(this).enqueueUniquePeriodicWork(
                "gservice-alerts",
                ExistingPeriodicWorkPolicy.KEEP,
                req
        );
    }

    private int dp(int v) { return (int)(v * getResources().getDisplayMetrics().density + 0.5f); }

    private TextView label(String text, int sp) {
        TextView t = new TextView(this);
        t.setText(text);
        t.setTextSize(sp);
        t.setTextColor(Color.WHITE);
        t.setGravity(Gravity.CENTER_VERTICAL);
        return t;
    }

    private ImageView icon(int type, int size) {
        ImageView v = new ImageView(this);
        v.setImageDrawable(new LineIcon(type));
        v.setPadding(dp(2), dp(2), dp(2), dp(2));
        v.setScaleType(ImageView.ScaleType.CENTER_INSIDE);
        v.setLayoutParams(new LinearLayout.LayoutParams(dp(size), dp(size)));
        return v;
    }

    private GradientDrawable roundedBg(int color, int radiusDp) {
        GradientDrawable d=new GradientDrawable();
        d.setColor(color);
        d.setCornerRadius(dp(radiusDp));
        return d;
    }

    private void buildUi() {
        root = new FrameLayout(this);
        root.setBackgroundColor(Color.rgb(17,24,39));

        web = new WebView(this);
        webLp = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT);
        root.addView(web, webLp);

        ImageView hamburger = icon(MENU, 28);
        hamburger.setPadding(dp(12), dp(12), dp(12), dp(12));
        hamburger.setBackground(roundedBg(Color.rgb(17,24,39), 12));
        hamburgerLp = new FrameLayout.LayoutParams(dp(52), dp(52), Gravity.TOP | Gravity.START);
        hamburgerLp.setMargins(dp(10), dp(8), 0, 0);
        root.addView(hamburger, hamburgerLp);

        scrim = new View(this);
        scrim.setBackgroundColor(0x88000000);
        scrim.setVisibility(View.GONE);
        root.addView(scrim, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT));

        drawer = new LinearLayout(this);
        drawer.setOrientation(LinearLayout.VERTICAL);
        drawer.setPadding(0, dp(12), 0, dp(12));
        drawer.setBackgroundColor(Color.rgb(27,27,27));
        drawer.setVisibility(View.GONE);
        drawerLp = new FrameLayout.LayoutParams(dp(304), ViewGroup.LayoutParams.MATCH_PARENT, Gravity.START);
        root.addView(drawer, drawerLp);

        LinearLayout header = new LinearLayout(this);
        header.setOrientation(LinearLayout.HORIZONTAL);
        header.setGravity(Gravity.CENTER_VERTICAL);
        header.setPadding(dp(20), 0, dp(16), 0);
        ImageView appMark = icon(WRENCH, 30);
        header.addView(appMark);
        TextView title = label("G-Service", 24);
        title.setTypeface(null, android.graphics.Typeface.BOLD);
        LinearLayout.LayoutParams tp = new LinearLayout.LayoutParams(0, dp(60), 1f);
        tp.setMargins(dp(12),0,0,0);
        header.addView(title,tp);
        drawer.addView(header, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dp(64)));

        addMenu("Ana Sayfa", HOME, () -> showWebPage("dashboard"));
        addMenu("QR Tara", QR, this::startQrScan);
        addMenu("Müşteriler", CUSTOMERS, () -> showWebPage("customers"));
        addMenu("Makineler", BOX, () -> showWebPage("products"));
        addMenu("Servis Kayıtları", WRENCH, () -> showWebPage("records"));
        addMenu("Periyodik Bakımlar", CAL, () -> showWebPage("maintenance"));
        addMenu("Bana Atanan İşler", JOB, this::showAssignedJobs);
        addMenu("Bildirimler", BELL, this::showNotifications);
        addMenu("Ayarlar", SETTINGS, () -> showWebPage("settings"));

        View spacer = new View(this);
        drawer.addView(spacer, new LinearLayout.LayoutParams(1, 0, 1f));

        LinearLayout logout = menuRow("Çıkış Yap", LOGOUT);
        logout.setBackgroundColor(Color.rgb(45,45,45));
        logout.setOnClickListener(v -> {
            evalJs("if(typeof logout==='function'){logout();}");
            closeDrawer();
        });
        drawer.addView(logout, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dp(56)));

        hamburger.setOnClickListener(v -> openDrawer());
        scrim.setOnClickListener(v -> closeDrawer());

        root.setOnApplyWindowInsetsListener((v, insets) -> {
            int top, bottom;
            if (Build.VERSION.SDK_INT >= 30) {
                android.graphics.Insets bars = insets.getInsets(WindowInsets.Type.systemBars());
                top = bars.top;
                bottom = bars.bottom;
            } else {
                top = insets.getSystemWindowInsetTop();
                bottom = insets.getSystemWindowInsetBottom();
            }
            webLp.topMargin = top;
            webLp.bottomMargin = bottom;
            web.setLayoutParams(webLp);

            hamburgerLp.topMargin = top + dp(8);
            hamburgerLp.leftMargin = dp(10);
            hamburger.setLayoutParams(hamburgerLp);

            drawerLp.topMargin = top;
            drawerLp.bottomMargin = bottom;
            drawer.setLayoutParams(drawerLp);
            return insets;
        });

        setContentView(root);
    }

    private LinearLayout menuRow(String name, int iconType) {
        LinearLayout row = new LinearLayout(this);
        row.setOrientation(LinearLayout.HORIZONTAL);
        row.setGravity(Gravity.CENTER_VERTICAL);
        row.setPadding(dp(20), 0, dp(14), 0);

        ImageView iv = icon(iconType, 24);
        row.addView(iv);

        TextView tv = label(name, 16);
        LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MATCH_PARENT, 1f);
        lp.setMargins(dp(16), 0, 0, 0);
        row.addView(tv, lp);
        return row;
    }

    private void addMenu(String name, int iconType, Runnable action) {
        LinearLayout item = menuRow(name, iconType);
        item.setOnClickListener(v -> {
            closeDrawer();
            action.run();
        });
        drawer.addView(item, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dp(52)));
    }

    private void openDrawer() {
        scrim.setVisibility(View.VISIBLE);
        drawer.setVisibility(View.VISIBLE);
    }

    private void closeDrawer() {
        drawer.setVisibility(View.GONE);
        scrim.setVisibility(View.GONE);
    }

    private void showWebPage(String id) {
        evalJs("if(window.nativeShowPage){nativeShowPage('" + id + "');}else if(typeof showPage==='function'){showPage('" + id + "');}");
    }

    private void showAssignedJobs() {
        evalJs("if(window.nativeShowAssigned){nativeShowAssigned();}");
    }

    private void showNotifications() {
        evalJs("if(window.nativeShowNotifications){nativeShowNotifications();}");
    }

    private void startQrScan() {
        IntentIntegrator integrator = new IntentIntegrator(this);
        integrator.setDesiredBarcodeFormats(IntentIntegrator.QR_CODE_TYPES);
        integrator.setPrompt("Makinenin QR kodunu okutun");
        integrator.setBeepEnabled(false);
        integrator.setOrientationLocked(false);
        integrator.setBarcodeImageEnabled(false);
        integrator.initiateScan();
    }

    @Override protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        IntentResult result = IntentIntegrator.parseActivityResult(requestCode, resultCode, data);
        if (result != null) {
            if (result.getContents() != null && !result.getContents().trim().isEmpty()) {
                String url = APP_URL + "?android=1&scan=" + Uri.encode(result.getContents().trim());
                web.loadUrl(url);
            }
            return;
        }
        super.onActivityResult(requestCode, resultCode, data);
    }

    private void configureWebView() {
        WebSettings s = web.getSettings();
        s.setJavaScriptEnabled(true);
        s.setDomStorageEnabled(true);
        s.setDatabaseEnabled(true);
        s.setMediaPlaybackRequiresUserGesture(false);
        web.addJavascriptInterface(new JsBridge(), "AndroidBridge");

        web.setWebViewClient(new WebViewClient() {
            @Override public void onPageFinished(WebView view, String url) {
                super.onPageFinished(view, url);
                injectMobileShell();
                handler.postDelayed(() -> injectMobileShell(), 800);
                handler.postDelayed(() -> injectMobileShell(), 2200);
            }
        });

        web.setWebChromeClient(new WebChromeClient() {
            @Override public void onPermissionRequest(PermissionRequest request) {
                runOnUiThread(() -> request.grant(request.getResources()));
            }
        });
    }

    private void injectMobileShell() {
        String js =
                "(function(){" +
                "try{" +
                "if(!document.getElementById('gservice-native-style')){" +
                "var st=document.createElement('style');st.id='gservice-native-style';" +
                "st.textContent='.sidebar{display:none!important}.shell{display:block!important;min-height:100vh!important}.main{width:100%!important;min-width:0!important;margin:0!important}.topbar{padding-left:78px!important;min-height:64px!important;position:sticky!important;top:0!important;z-index:25!important;background:#fff!important}.content{width:100%!important}.mobileNativeSection{display:block}.mobileNativeSection.hidden{display:none!important}@media(max-width:800px){.content{padding-left:20px!important;padding-right:20px!important}}';" +
                "document.head.appendChild(st);" +
                "}" +

                "window.nativeShowPage=function(id){" +
                "var n=document.getElementById('mobileAssigned');if(n)n.classList.add('hidden');" +
                "n=document.getElementById('mobileNotifications');if(n)n.classList.add('hidden');" +
                "if(typeof showPage==='function')showPage(id);" +
                "};" +

                "window.nativeShowAssigned=function(){" +
                "if(!window.me||!window.cache)return;" +
                "['dashboard','customers','products','records','maintenance','users','settings','mobileNotifications'].forEach(function(x){var e=document.getElementById(x);if(e)e.classList.add('hidden')});" +
                "var s=document.getElementById('mobileAssigned');if(!s){s=document.createElement('section');s.id='mobileAssigned';s.className='mobileNativeSection';document.querySelector('.content').appendChild(s);}s.classList.remove('hidden');" +
                "document.querySelectorAll('.nav button[data-page]').forEach(function(b){b.classList.remove('active')});" +
                "if(window.pageTitle)pageTitle.textContent='Bana Atanan İşler';" +
                "var list=(cache.records||[]).filter(function(r){return r.assigned_to===me.id||(!r.assigned_to&&r.created_by===me.id)});" +
                "var h='<div class=\"card\"><h2 style=\"margin-top:0\">Bana Atanan İşler</h2>';" +
                "if(!list.length){h+='<div class=\"muted\">Şu anda sana atanmış açık veya geçmiş servis kaydı yok.</div>';}else{" +
                "h+='<div style=\"overflow:auto\"><table><tr><th>Tarih</th><th>Müşteri / Makine</th><th>Tür</th><th>Durum</th><th></th></tr>';" +
                "list.forEach(function(r){h+='<tr><td>'+esc(formatDateTime(r.created_at))+'</td><td><b>'+esc(r.product_name||'')+'</b><div class=\"muted\">'+esc(r.customer_display||'')+(r.product_code?' · '+esc(r.product_code):'')+'</div></td><td>'+(r.type==='FAULT'?'Arıza':'Periyodik Bakım')+'</td><td>'+esc(statusText(r.status))+'</td><td><button class=\"btn blue small\" onclick=\"showRecordDetail(&#39;'+r.id+'&#39;)\">Aç</button></td></tr>';});" +
                "h+='</table></div>';}" +
                "h+='</div>';s.innerHTML=h;" +
                "};" +

                "window.nativeShowNotifications=function(){" +
                "['dashboard','customers','products','records','maintenance','users','settings','mobileAssigned'].forEach(function(x){var e=document.getElementById(x);if(e)e.classList.add('hidden')});" +
                "var s=document.getElementById('mobileNotifications');if(!s){s=document.createElement('section');s.id='mobileNotifications';s.className='mobileNativeSection';document.querySelector('.content').appendChild(s);}s.classList.remove('hidden');" +
                "document.querySelectorAll('.nav button[data-page]').forEach(function(b){b.classList.remove('active')});" +
                "if(window.pageTitle)pageTitle.textContent='Bildirimler';" +
                "var a=(cache&&cache.alerts)||[];var h='<div class=\"card\"><h2 style=\"margin-top:0\">Bildirimler</h2>';" +
                "if(!a.length){h+='<div class=\"muted\">Yeni bildirim yok.</div>';}else{" +
                "a.forEach(function(x){h+='<div class=\"card\" style=\"margin:10px 0;padding:14px\"><b>'+esc(x.product_name||'Makine')+'</b><div class=\"muted\">'+esc(x.waiting_part_name||'Parça bekliyor')+' · '+esc(x.expected_part_date||'')+'</div></div>';});}" +
                "h+='</div>';s.innerHTML=h;" +
                "};" +

                "if(window.boot&&!window.__nativeBootWrapped){var ob=window.boot;window.boot=async function(){var r=await ob.apply(this,arguments);try{AndroidBridge.setToken(localStorage.getItem('gservice_token')||'')}catch(e){}return r};window.__nativeBootWrapped=true;}" +
                "try{AndroidBridge.setToken(localStorage.getItem('gservice_token')||'')}catch(e){}" +
                "}catch(e){}" +
                "})();";
        evalJs(js);
    }

    private void evalJs(String js) {
        if (web == null) return;
        web.post(() -> web.evaluateJavascript(js, null));
    }

    private final Runnable alertPoller = new Runnable() {
        @Override public void run() {
            evalJs("(function(){try{var t=localStorage.getItem('gservice_token')||'';if(t&&window.AndroidBridge){AndroidBridge.setToken(t);}if(window.me&&typeof api==='function'){api('/alerts').then(function(a){cache.alerts=a;AndroidBridge.receiveAlerts(JSON.stringify(a));}).catch(function(){});}}catch(e){}})();");
            handler.postDelayed(this, 60000);
        }
    };

    private class JsBridge {
        @JavascriptInterface public void setToken(String token) {
            getSharedPreferences("gservice", MODE_PRIVATE).edit().putString("token", token == null ? "" : token).apply();
        }

        @JavascriptInterface public void receiveAlerts(String json) {
            try {
                JSONArray a=new JSONArray(json);
                if(a.length()==0)return;
                JSONObject first=a.getJSONObject(0);
                StringBuilder sig=new StringBuilder();
                for(int i=0;i<a.length();i++){
                    JSONObject x=a.getJSONObject(i);
                    sig.append(x.optString("id")).append('|').append(x.optString("expected_part_date")).append(';');
                }
                SharedPreferences sp=getSharedPreferences("gservice",MODE_PRIVATE);
                String old=sp.getString("last_alert_signature","");
                if(sig.toString().equals(old))return;
                sp.edit().putString("last_alert_signature",sig.toString()).apply();
                String product=first.optString("product_name","Makine");
                String part=first.optString("waiting_part_name","Parça bekliyor");
                String date=first.optString("expected_part_date","");
                AlertWorker.showNotification(MainActivity.this, product, part + (date.isEmpty()?"":" · "+date));
            } catch(Exception ignored){}
        }
    }

    @Override public void onBackPressed() {
        if (drawer != null && drawer.getVisibility() == View.VISIBLE) {
            closeDrawer();
            return;
        }
        if (web != null && web.canGoBack()) web.goBack();
        else super.onBackPressed();
    }

    private static class LineIcon extends Drawable {
        private final int type;
        private final Paint p = new Paint(Paint.ANTI_ALIAS_FLAG);
        private final Path path = new Path();

        LineIcon(int type) {
            this.type = type;
            p.setColor(Color.WHITE);
            p.setStyle(Paint.Style.STROKE);
            p.setStrokeWidth(2.4f);
            p.setStrokeCap(Paint.Cap.ROUND);
            p.setStrokeJoin(Paint.Join.ROUND);
        }

        @Override public void draw(Canvas c) {
            Rect b=getBounds();
            float w=b.width(), h=b.height(), x=b.left, y=b.top;
            float cx=x+w/2f, cy=y+h/2f, s=Math.min(w,h)/24f;
            c.save(); c.translate(cx,cy); c.scale(s,s); path.reset();

            switch(type) {
                case MENU:
                    for(int i=-1;i<=1;i++) c.drawLine(-8,i*6,8,i*6,p); break;
                case HOME:
                    path.moveTo(-9,0); path.lineTo(0,-8); path.lineTo(9,0);
                    path.moveTo(-6,-2); path.lineTo(-6,9); path.lineTo(6,9); path.lineTo(6,-2); c.drawPath(path,p); break;
                case QR:
                    drawQr(c); break;
                case CUSTOMERS:
                    c.drawCircle(-4,-4,3,p); c.drawCircle(5,-3,3,p);
                    c.drawArc(-10,1,2,10,180,180,false,p); c.drawArc(-1,2,11,11,180,180,false,p); break;
                case BOX:
                    path.moveTo(-8,-5); path.lineTo(0,-9); path.lineTo(8,-5); path.lineTo(0,-1); path.close();
                    path.moveTo(-8,-5); path.lineTo(-8,5); path.lineTo(0,9); path.lineTo(8,5); path.lineTo(8,-5);
                    path.moveTo(0,-1); path.lineTo(0,9); c.drawPath(path,p); break;
                case WRENCH:
                    c.drawCircle(-4,-4,4,p); c.drawLine(-1,-1,8,8,p); c.drawCircle(8,8,2,p); break;
                case CAL:
                    c.drawRect(-8,-7,8,8,p); c.drawLine(-8,-2,8,-2,p); c.drawLine(-4,-10,-4,-5,p); c.drawLine(4,-10,4,-5,p); break;
                case JOB:
                    c.drawRect(-7,-8,7,9,p); c.drawLine(-3,-8,-3,-10,p); c.drawLine(3,-8,3,-10,p);
                    c.drawLine(-3,-2,4,-2,p); c.drawLine(-3,3,4,3,p); break;
                case HISTORY:
                    c.drawArc(-8,-8,8,8,-45,285,false,p);
                    path.moveTo(-8,-3); path.lineTo(-8,-8); path.lineTo(-3,-8); c.drawPath(path,p);
                    c.drawLine(0,0,0,-5,p); c.drawLine(0,0,4,3,p); break;
                case BELL:
                    path.moveTo(-7,5); path.quadTo(-4,2,-4,-3); path.quadTo(-4,-8,0,-8);
                    path.quadTo(4,-8,4,-3); path.quadTo(4,2,7,5); path.close();
                    c.drawPath(path,p); c.drawLine(-2,8,2,8,p); break;
                case USER:
                    c.drawCircle(0,-5,4,p); c.drawArc(-8,1,8,13,180,180,false,p); break;
                case SETTINGS:
                    c.drawCircle(0,0,3,p);
                    for(int i=0;i<8;i++){ double a=i*Math.PI/4; float x1=(float)Math.cos(a)*6, y1=(float)Math.sin(a)*6;
                        float x2=(float)Math.cos(a)*9, y2=(float)Math.sin(a)*9; c.drawLine(x1,y1,x2,y2,p);}
                    c.drawCircle(0,0,7,p); break;
                case LOGOUT:
                    c.drawRect(-9,-8,1,8,p); c.drawLine(-1,0,9,0,p);
                    path.moveTo(5,-4); path.lineTo(9,0); path.lineTo(5,4); c.drawPath(path,p); break;
            }
            c.restore();
        }

        private void drawQr(Canvas c) {
            c.drawRect(-9,-9,-2,-2,p); c.drawRect(2,-9,9,-2,p); c.drawRect(-9,2,-2,9,p);
            c.drawRect(-7,-7,-4,-4,p); c.drawRect(4,-7,7,-4,p); c.drawRect(-7,4,-4,7,p);
            c.drawLine(2,2,6,2,p); c.drawLine(6,2,6,6,p); c.drawLine(2,6,6,6,p);
            c.drawLine(2,6,2,9,p); c.drawLine(6,6,9,6,p); c.drawLine(9,2,9,4,p);
        }

        @Override public void setAlpha(int alpha){ p.setAlpha(alpha); }
        @Override public void setColorFilter(android.graphics.ColorFilter cf){ p.setColorFilter(cf); }
        @Override public int getOpacity(){ return android.graphics.PixelFormat.TRANSLUCENT; }
    }
}
