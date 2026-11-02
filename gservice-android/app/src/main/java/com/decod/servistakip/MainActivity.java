package com.decod.servistakip;

import android.Manifest;
import android.app.Activity;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.Paint;
import android.graphics.Path;
import android.graphics.Rect;
import android.graphics.drawable.ColorDrawable;
import android.graphics.drawable.Drawable;
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
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.TextView;

public class MainActivity extends Activity {
    private static final String URL = "https://decod-servis-api-production.up.railway.app/app";
    private WebView web;
    private LinearLayout drawer;
    private View scrim;

    private static final int HOME=1, QR=2, BOX=3, WRENCH=4, CAL=5, JOB=6,
            HISTORY=7, BELL=8, USER=9, SETTINGS=10, LOGOUT=11, MENU=12;

    @Override public void onCreate(Bundle b) {
        super.onCreate(b);
        if (android.os.Build.VERSION.SDK_INT >= 23) requestPermissions(new String[]{Manifest.permission.CAMERA}, 20);
        buildUi();
        configureWebView();
        web.loadUrl(URL);
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

    private void buildUi() {
        FrameLayout root = new FrameLayout(this);
        web = new WebView(this);
        root.addView(web, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT));

        ImageView hamburger = icon(MENU, 28);
        hamburger.setPadding(dp(12), dp(12), dp(12), dp(12));
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
        drawer.setPadding(0, dp(14), 0, dp(14));
        drawer.setBackgroundColor(Color.rgb(27,27,27));
        drawer.setVisibility(View.GONE);
        FrameLayout.LayoutParams dpv = new FrameLayout.LayoutParams(dp(304), ViewGroup.LayoutParams.MATCH_PARENT, Gravity.START);
        root.addView(drawer, dpv);

        LinearLayout header = new LinearLayout(this);
        header.setOrientation(LinearLayout.HORIZONTAL);
        header.setGravity(Gravity.CENTER_VERTICAL);
        header.setPadding(dp(20), 0, dp(16), 0);
        ImageView appMark = icon(WRENCH, 30);
        header.addView(appMark);
        TextView title = label("G-Service", 24);
        title.setTypeface(null, android.graphics.Typeface.BOLD);
        LinearLayout.LayoutParams tp = new LinearLayout.LayoutParams(0, dp(62), 1f);
        tp.setMargins(dp(12),0,0,0);
        header.addView(title,tp);
        drawer.addView(header, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dp(66)));

        addMenu("Ana Sayfa", HOME);
        addMenu("QR Tara", QR);
        addMenu("Ürünler", BOX);
        addMenu("Arıza Kayıtları", WRENCH);
        addMenu("Periyodik Bakımlar", CAL);
        addMenu("Bana Atanan İşler", JOB);
        addMenu("Geçmiş Müdahaleler", HISTORY);
        addMenu("Bildirimler", BELL);
        addMenu("Profil / Kullanıcı Bilgileri", USER);
        addMenu("Ayarlar", SETTINGS);

        View spacer = new View(this);
        drawer.addView(spacer, new LinearLayout.LayoutParams(1, 0, 1f));

        LinearLayout logout = menuRow("Çıkış Yap", LOGOUT);
        logout.setBackgroundColor(Color.rgb(45,45,45));
        logout.setOnClickListener(v -> {
            clickWebText("Çıkış Yap");
            closeDrawer();
        });
        drawer.addView(logout, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dp(56)));

        hamburger.setOnClickListener(v -> openDrawer());
        scrim.setOnClickListener(v -> closeDrawer());
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

    private void addMenu(String name, int iconType) {
        LinearLayout item = menuRow(name, iconType);
        item.setOnClickListener(v -> {
            clickWebText(name);
            closeDrawer();
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
            c.save();
            c.translate(cx,cy);
            c.scale(s,s);
            path.reset();

            switch(type) {
                case MENU:
                    for(int i=-1;i<=1;i++) c.drawLine(-8,i*6,8,i*6,p);
                    break;
                case HOME:
                    path.moveTo(-9,0); path.lineTo(0,-8); path.lineTo(9,0);
                    path.moveTo(-6,-2); path.lineTo(-6,9); path.lineTo(6,9); path.lineTo(6,-2);
                    c.drawPath(path,p); break;
                case QR:
                    drawQr(c); break;
                case BOX:
                    path.moveTo(-8,-5); path.lineTo(0,-9); path.lineTo(8,-5); path.lineTo(0,-1); path.close();
                    path.moveTo(-8,-5); path.lineTo(-8,5); path.lineTo(0,9); path.lineTo(8,5); path.lineTo(8,-5);
                    path.moveTo(0,-1); path.lineTo(0,9); c.drawPath(path,p); break;
                case WRENCH:
                    c.drawCircle(-4,-4,4,p); c.drawLine(-1,-1,8,8,p); c.drawCircle(8,8,2,p); break;
                case CAL:
                    c.drawRect(-8,-7,8,8,p); c.drawLine(-8,-2,8,-2,p);
                    c.drawLine(-4,-10,-4,-5,p); c.drawLine(4,-10,4,-5,p); break;
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
