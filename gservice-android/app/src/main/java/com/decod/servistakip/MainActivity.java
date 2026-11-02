package com.decod.servistakip;

import android.Manifest;
import android.app.Activity;
import android.graphics.Color;
import android.graphics.drawable.ColorDrawable;
import android.os.Bundle;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.webkit.PermissionRequest;
import android.webkit.WebChromeClient;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.FrameLayout;
import android.widget.LinearLayout;
import android.widget.TextView;

public class MainActivity extends Activity {
    private static final String URL = "https://decod-servis-api-production.up.railway.app/app";
    private WebView web;
    private LinearLayout drawer;
    private View scrim;

    @Override public void onCreate(Bundle b) {
        super.onCreate(b);
        if (android.os.Build.VERSION.SDK_INT >= 23) requestPermissions(new String[]{Manifest.permission.CAMERA}, 20);
        buildUi();
        configureWebView();
        web.loadUrl(URL);
    }

    private int dp(int v) { return (int)(v * getResources().getDisplayMetrics().density + 0.5f); }

    private TextView label(String text, int sp, int pad) {
        TextView t = new TextView(this);
        t.setText(text);
        t.setTextSize(sp);
        t.setTextColor(Color.WHITE);
        t.setGravity(Gravity.CENTER_VERTICAL);
        t.setPadding(dp(pad), 0, dp(pad), 0);
        return t;
    }

    private void buildUi() {
        FrameLayout root = new FrameLayout(this);
        web = new WebView(this);
        root.addView(web, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT));

        TextView hamburger = label("☰", 28, 0);
        hamburger.setGravity(Gravity.CENTER);
        hamburger.setBackground(new ColorDrawable(Color.rgb(20,20,20)));
        FrameLayout.LayoutParams hp = new FrameLayout.LayoutParams(dp(52), dp(52), Gravity.TOP | Gravity.START);
        hp.setMargins(dp(10), dp(12), 0, 0);
        root.addView(hamburger, hp);

        scrim = new View(this);
        scrim.setBackgroundColor(0x88000000);
        scrim.setVisibility(View.GONE);
        root.addView(scrim, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT));

        drawer = new LinearLayout(this);
        drawer.setOrientation(LinearLayout.VERTICAL);
        drawer.setPadding(0, dp(18), 0, dp(18));
        drawer.setBackgroundColor(Color.rgb(27,27,27));
        drawer.setVisibility(View.GONE);
        FrameLayout.LayoutParams dpv = new FrameLayout.LayoutParams(dp(292), ViewGroup.LayoutParams.MATCH_PARENT, Gravity.START);
        root.addView(drawer, dpv);

        TextView title = label("G-Service", 24, 20);
        title.setTypeface(null, android.graphics.Typeface.BOLD);
        drawer.addView(title, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dp(62)));

        addMenu("Ana Sayfa");
        addMenu("QR Tara");
        addMenu("Ürünler");
        addMenu("Arıza Kayıtları");
        addMenu("Periyodik Bakımlar");
        addMenu("Bana Atanan İşler");
        addMenu("Geçmiş Müdahaleler");
        addMenu("Bildirimler");
        addMenu("Profil / Kullanıcı Bilgileri");
        addMenu("Ayarlar");

        View spacer = new View(this);
        drawer.addView(spacer, new LinearLayout.LayoutParams(1, 0, 1f));

        TextView logout = label("Çıkış Yap", 16, 20);
        logout.setBackgroundColor(Color.rgb(45,45,45));
        logout.setOnClickListener(v -> {
            clickWebText("Çıkış Yap");
            closeDrawer();
        });
        drawer.addView(logout, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dp(54)));

        hamburger.setOnClickListener(v -> openDrawer());
        scrim.setOnClickListener(v -> closeDrawer());
        setContentView(root);
    }

    private void addMenu(String name) {
        TextView item = label(name, 16, 20);
        item.setOnClickListener(v -> {
            clickWebText(name);
            closeDrawer();
        });
        drawer.addView(item, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dp(50)));
    }

    private void openDrawer() {
        scrim.setVisibility(View.VISIBLE);
        drawer.setVisibility(View.VISIBLE);
    }

    private void closeDrawer() {
        drawer.setVisibility(View.GONE);
        scrim.setVisibility(View.GONE);
    }

    private void clickWebText(String text) {
        if ("Ana Sayfa".equals(text)) {
            web.loadUrl(URL);
            return;
        }

        String js = "javascript:(function(){var q=" + org.json.JSONObject.quote(text) + ";" +
                "var a=[...document.querySelectorAll('a,button,[role=button],li,div,span')];" +
                "var e=a.find(x=>(x.innerText||'').trim().toLowerCase()===q.toLowerCase())||" +
                "a.find(x=>(x.innerText||'').trim().toLowerCase().includes(q.toLowerCase()));" +
                "if(e){e.click();}else{window.dispatchEvent(new CustomEvent('gservice-menu',{detail:q}));}})()";

        web.evaluateJavascript(js, null);
    }

    private void configureWebView() {
        WebSettings s = web.getSettings();
        s.setJavaScriptEnabled(true);
        s.setDomStorageEnabled(true);
        s.setDatabaseEnabled(true);
        s.setMediaPlaybackRequiresUserGesture(false);

        web.setWebViewClient(new WebViewClient());
        web.setWebChromeClient(new WebChromeClient() {
            @Override public void onPermissionRequest(PermissionRequest request) {
                runOnUiThread(() -> request.grant(request.getResources()));
            }
        });
    }

    @Override public void onBackPressed() {
        if (drawer != null && drawer.getVisibility() == View.VISIBLE) {
            closeDrawer();
            return;
        }
        if (web != null && web.canGoBack()) web.goBack();
        else super.onBackPressed();
    }
}
