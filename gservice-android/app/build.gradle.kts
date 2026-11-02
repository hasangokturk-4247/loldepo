plugins { id("com.android.application") }

android {
    namespace = "com.decod.servistakip"
    compileSdk = 35
    defaultConfig {
        applicationId = "com.decod.servistakip"
        minSdk = 26
        targetSdk = 35
        versionCode = 107
        versionName = "1.0.7"
    }
}

dependencies {
    implementation("com.journeyapps:zxing-android-embedded:4.3.0")
    implementation("androidx.work:work-runtime:2.10.0")
}
