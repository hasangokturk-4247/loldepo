package com.decod.servistakip;

import android.Manifest;
import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.os.Build;

import androidx.annotation.NonNull;
import androidx.work.Worker;
import androidx.work.WorkerParameters;

import org.json.JSONArray;
import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.InputStreamReader;
import java.net.HttpURLConnection;
import java.net.URL;

public class AlertWorker extends Worker {
    private static final String API = "https://decod-servis-api-production.up.railway.app";
    private static final String CHANNEL_ID = "gservice_alerts";

    public AlertWorker(@NonNull Context context, @NonNull WorkerParameters params) {
        super(context, params);
    }

    @NonNull @Override public Result doWork() {
        try {
            Context c=getApplicationContext();
            SharedPreferences sp=c.getSharedPreferences("gservice",Context.MODE_PRIVATE);
            String token=sp.getString("token","");
            if(token==null || token.isEmpty()) return Result.success();

            HttpURLConnection con=(HttpURLConnection)new URL(API+"/api/alerts").openConnection();
            con.setConnectTimeout(12000);
            con.setReadTimeout(12000);
            con.setRequestMethod("GET");
            con.setRequestProperty("Authorization","Bearer "+token);
            con.setRequestProperty("x-gservice-client","android");

            int code=con.getResponseCode();
            if(code==401 || code==403) return Result.success();
            if(code<200 || code>=300) return Result.retry();

            BufferedReader br=new BufferedReader(new InputStreamReader(con.getInputStream(),"UTF-8"));
            StringBuilder sb=new StringBuilder();
            String line;
            while((line=br.readLine())!=null) sb.append(line);
            br.close();

            JSONArray a=new JSONArray(sb.toString());
            if(a.length()==0){
                sp.edit().putString("last_alert_signature","").apply();
                return Result.success();
            }

            StringBuilder sig=new StringBuilder();
            for(int i=0;i<a.length();i++){
                JSONObject x=a.getJSONObject(i);
                sig.append(x.optString("id")).append('|').append(x.optString("expected_part_date")).append(';');
            }
            String old=sp.getString("last_alert_signature","");
            if(sig.toString().equals(old)) return Result.success();
            sp.edit().putString("last_alert_signature",sig.toString()).apply();

            JSONObject first=a.getJSONObject(0);
            String product=first.optString("product_name","Makine");
            String part=first.optString("waiting_part_name","Parça bekliyor");
            String date=first.optString("expected_part_date","");
            showNotification(c,product,part+(date.isEmpty()?"":" · "+date));
            return Result.success();
        } catch(Exception e) {
            return Result.retry();
        }
    }

    public static void showNotification(Context c, String title, String text) {
        if(Build.VERSION.SDK_INT>=33 && c.checkSelfPermission(Manifest.permission.POST_NOTIFICATIONS)!= PackageManager.PERMISSION_GRANTED) return;

        NotificationManager nm=(NotificationManager)c.getSystemService(Context.NOTIFICATION_SERVICE);
        if(Build.VERSION.SDK_INT>=26){
            NotificationChannel ch=new NotificationChannel(CHANNEL_ID,"G-Service Bildirimleri",NotificationManager.IMPORTANCE_DEFAULT);
            ch.setDescription("Parça bekleyen ve servis uyarıları");
            nm.createNotificationChannel(ch);
        }

        Intent i=new Intent(c,MainActivity.class);
        i.setFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP|Intent.FLAG_ACTIVITY_SINGLE_TOP);
        PendingIntent pi=PendingIntent.getActivity(c,100,i,PendingIntent.FLAG_UPDATE_CURRENT|PendingIntent.FLAG_IMMUTABLE);

        Notification.Builder b=Build.VERSION.SDK_INT>=26?new Notification.Builder(c,CHANNEL_ID):new Notification.Builder(c);
        b.setSmallIcon(android.R.drawable.stat_notify_more)
                .setContentTitle("G-Service · "+title)
                .setContentText(text)
                .setAutoCancel(true)
                .setContentIntent(pi)
                .setShowWhen(true);
        nm.notify(7001,b.build());
    }
}
