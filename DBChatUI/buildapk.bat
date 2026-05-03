@rem 1. Build Vue
echo Building Vue App...
@call npm run build

@rem 2. Sync to Android
echo Syncing with Capacitor...
@call npx cap sync

echo setting up environment for Android build...
@rem 3. Compile the APK (The "Magic" Command)
@cd android
@set path=C:\Program Files\Eclipse Adoptium\jdk-21.0.10.7-hotspot\bin;%path%;
@set JAVA_HOME=C:\Program Files\Eclipse Adoptium\jdk-21.0.10.7-hotspot\

echo Building Android APK...
rem create apk
@call gradlew assembleDebug
echo Done! You can find the APK in android/app/build/outputs/apk/debug/app-debug.apk
