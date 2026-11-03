package com.decod.grm;

import android.app.JobInfo;
import android.app.JobParameters;
import android.app.JobScheduler;
import android.app.JobService;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.app.Notification;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.os.Build;
import org.json.JSONArray;
import org.json.JSONObject;
import java.time.LocalDate;
import java.time.temporal.ChronoUnit;

public class SyncJobService extends JobService {
    static final int JOB_ID=22001; static final String CHANNEL="grm_reminders";
    static void schedule(Context c){JobScheduler js=(JobScheduler)c.getSystemService(Context.JOB_SCHEDULER_SERVICE);JobInfo job=new JobInfo.Builder(JOB_ID,new ComponentName(c,SyncJobService.class)).setRequiredNetworkType(JobInfo.NETWORK_TYPE_ANY).setPersisted(false).setPeriodic(15*60*1000L).build();js.schedule(job);}
    @Override public boolean onStartJob(JobParameters params){new Thread(()->{try{GrmStore s=new GrmStore(this);if(s.isLicensed()){new CloudClient(s).sync();notifyDue(s);}}catch(Exception ignored){}finally{jobFinished(params,false);}}).start();return true;}
    @Override public boolean onStopJob(JobParameters params){return true;}
    static void notifyDue(Context c,GrmStore s)throws Exception{JSONArray visits=s.visits(),rems=s.reminders();int count=0;LocalDate today=LocalDate.now();int days=s.settings().optInt("reminderDays",3);for(int i=0;i<visits.length();i++){JSONObject v=visits.optJSONObject(i);if(v==null||v.optBoolean("deleted",false)||v.optBoolean("reminderDone",false))continue;String d=JsonUtil.s(v,"nextVisitDate");if(d.isEmpty())continue;try{long diff=ChronoUnit.DAYS.between(today,LocalDate.parse(d));if(diff<=days)count++;}catch(Exception ignored){}}for(int i=0;i<rems.length();i++){JSONObject r=rems.optJSONObject(i);if(r==null||r.optBoolean("deleted",false)||"Tamamlandı".equalsIgnoreCase(JsonUtil.s(r,"status")))continue;String d=JsonUtil.s(r,"date");if(d.isEmpty())continue;try{long diff=ChronoUnit.DAYS.between(today,LocalDate.parse(d));if(diff<=days)count++;}catch(Exception ignored){}}if(count==0)return;NotificationManager nm=(NotificationManager)c.getSystemService(Context.NOTIFICATION_SERVICE);if(Build.VERSION.SDK_INT>=26)nm.createNotificationChannel(new NotificationChannel(CHANNEL,"G-Rm Hatırlatmaları",NotificationManager.IMPORTANCE_DEFAULT));Intent i=new Intent(c,MainActivity.class);PendingIntent pi=PendingIntent.getActivity(c,0,i,PendingIntent.FLAG_UPDATE_CURRENT|PendingIntent.FLAG_IMMUTABLE);Notification n=new Notification.Builder(c,CHANNEL).setSmallIcon(com.decod.grm.R.drawable.icon).setContentTitle("G-Rm").setContentText(count+" yaklaşan/geciken kayıt var").setAutoCancel(true).setContentIntent(pi).build();nm.notify(22002,n);}
    private void notifyDue(GrmStore s)throws Exception{notifyDue(this,s);}
}