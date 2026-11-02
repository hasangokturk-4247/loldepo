package com.decod.gservice;

import android.Manifest;
import android.animation.ObjectAnimator;
import android.app.Activity;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.graphics.Color;
import android.graphics.drawable.GradientDrawable;
import android.os.Bundle;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.webkit.CookieManager;
import android.webkit.WebChromeClient;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.FrameLayout;
import android.widget.TextView;
import android.widget.Toast;

import androidx.core.app.ActivityCompat;
import androidx.core.content.ContextCompat;

import com.google.zxing.integration.android.IntentIntegrator;
import com.google.zxing.integration.android.IntentResult;

import java.net.URLEncoder;
import java.nio.charset.StandardCharsets;

public class MainActivity extends Activity {
    private static final String BASE_URL = "https://decod-servis-api-production.up.railway.app/app?android=1";
    private WebView webView;
    private FrameLayout root;
    private TextView scanButton;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        showSplash();
    }

    private void showSplash() {
        root = new FrameLayout(this);
        root.setBackgroundColor(Color.WHITE);

        TextView logo = new TextView(this);
        logo.setText("decod");
        logo.setTextColor(Color.rgb(227, 6, 19));
        logo.setTextSize(36f);
        logo.setTypeface(android.graphics.Typeface.create("sans", android.graphics.Typeface.BOLD));
        logo.setGravity(Gravity.CENTER);

        FrameLayout.LayoutParams lp = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.WRAP_CONTENT,
                ViewGroup.LayoutParams.WRAP_CONTENT,
                Gravity.CENTER
        );
        root.addView(logo, lp);
        setContentView(root);

        ObjectAnimator rot = ObjectAnimator.ofFloat(logo, "rotationY", 0f, 360f);
        rot.setDuration(1050);
        rot.start();

        logo.postDelayed(this::showMain, 1100);
    }

    private void showMain() {
        root.removeAllViews();

        webView = new WebView(this);
        WebSettings ws = webView.getSettings();
        ws.setJavaScriptEnabled(true);
        ws.setDomStorageEnabled(true);
        ws.setDatabaseEnabled(true);
        ws.setCacheMode(WebSettings.LOAD_DEFAULT);
        ws.setSupportZoom(false);
        ws.setBuiltInZoomControls(false);
        ws.setDisplayZoomControls(false);
        ws.setUserAgentString(ws.getUserAgentString() + " G-Service-Android/1.0.2");

        CookieManager.getInstance().setAcceptCookie(true);
        CookieManager.getInstance().setAcceptThirdPartyCookies(webView, true);

        webView.setWebChromeClient(new WebChromeClient());
        webView.setWebViewClient(new WebViewClient());

        root.addView(webView, new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.MATCH_PARENT
        ));

        scanButton = new TextView(this);
        scanButton.setText("QR KODU TARA");
        scanButton.setTextColor(Color.WHITE);
        scanButton.setTextSize(16f);
        scanButton.setGravity(Gravity.CENTER);
        scanButton.setTypeface(android.graphics.Typeface.DEFAULT_BOLD);
        scanButton.setClickable(true);
        scanButton.setFocusable(true);
        scanButton.setElevation(dp(12));

        GradientDrawable qrBg = new GradientDrawable();
        qrBg.setColor(Color.rgb(227, 6, 19));
        qrBg.setCornerRadius(dp(14));
        scanButton.setBackground(qrBg);
        scanButton.setOnClickListener(v -> startQr());

        FrameLayout.LayoutParams bp = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                dp(58),
                Gravity.BOTTOM
        );
        bp.setMargins(dp(16), 0, dp(16), dp(24));
        root.addView(scanButton, bp);
        scanButton.bringToFront();

        webView.loadUrl(BASE_URL);
    }

    private int dp(int value) {
        return Math.round(value * getResources().getDisplayMetrics().density);
    }

    private void startQr() {
        if (ContextCompat.checkSelfPermission(this, Manifest.permission.CAMERA) != PackageManager.PERMISSION_GRANTED) {
            ActivityCompat.requestPermissions(this, new String[]{Manifest.permission.CAMERA}, 201);
            return;
        }
        launchScanner();
    }

    private void launchScanner() {
        IntentIntegrator integrator = new IntentIntegrator(this);
        integrator.setDesiredBarcodeFormats(IntentIntegrator.QR_CODE);
        integrator.setPrompt("G-Service ürün QR kodunu okutun");
        integrator.setBeepEnabled(true);
        integrator.setOrientationLocked(false);
        integrator.setBarcodeImageEnabled(false);
        integrator.initiateScan();
    }

    @Override
    public void onRequestPermissionsResult(int requestCode, String[] permissions, int[] grantResults) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults);
        if (requestCode == 201 && grantResults.length > 0 && grantResults[0] == PackageManager.PERMISSION_GRANTED) {
            launchScanner();
        } else if (requestCode == 201) {
            Toast.makeText(this, "QR tarama için kamera izni gerekli.", Toast.LENGTH_LONG).show();
        }
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        IntentResult result = IntentIntegrator.parseActivityResult(requestCode, resultCode, data);
        if (result != null) {
            if (result.getContents() != null && webView != null) {
                String q = URLEncoder.encode(result.getContents(), StandardCharsets.UTF_8);
                webView.loadUrl(BASE_URL + "&scan=" + q);
            }
            return;
        }
        super.onActivityResult(requestCode, resultCode, data);
    }

    @Override
    public void onBackPressed() {
        if (webView != null && webView.canGoBack()) webView.goBack();
        else super.onBackPressed();
    }

    @Override
    protected void onPause() {
        super.onPause();
        if (webView != null) webView.onPause();
    }

    @Override
    protected void onResume() {
        super.onResume();
        if (webView != null) webView.onResume();
    }
}
