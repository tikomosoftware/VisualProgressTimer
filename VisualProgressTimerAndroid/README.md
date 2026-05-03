# Visual Progress Timer Android

Flutter-based Android version of Visual Progress Timer.

This project keeps the Android app separate from the existing Windows/WPF app.
The timer state is based on an absolute end time, and Android `AlarmManager`
is used through a Kotlin platform channel so the finish notification can fire
even when the Flutter UI is not active.

## Run

```powershell
flutter pub get
flutter run
```

## Notes

- Android 13+ requires notification permission.
- Android 12+ may require exact alarm permission for precise finish alerts.
- If exact alarm permission is denied, the app can still run the visual timer,
  but finish notification timing is less reliable.
