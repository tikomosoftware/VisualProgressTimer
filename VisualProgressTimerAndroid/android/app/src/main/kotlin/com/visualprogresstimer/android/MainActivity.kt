package com.visualprogresstimer.android

import android.Manifest
import android.app.AlarmManager
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.net.Uri
import android.os.Build
import android.provider.Settings
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel

class MainActivity : FlutterActivity() {
    private var notificationPermissionResult: MethodChannel.Result? = null

    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)

        MethodChannel(
            flutterEngine.dartExecutor.binaryMessenger,
            "visual_progress_timer/alarm"
        ).setMethodCallHandler { call, result ->
            when (call.method) {
                "loadState" -> result.success(TimerStore.load(this))
                "saveSettings" -> {
                    TimerStore.saveSettings(
                        this,
                        call.argument<Int>("durationSeconds") ?: 2700,
                        call.argument<String>("gaugeColor") ?: "#202A44",
                        call.argument<Boolean>("darkMode") ?: false
                    )
                    result.success(null)
                }
                "scheduleTimer" -> {
                    val endAtWallMillis = call.argument<Long>("endAtWallMillis") ?: 0L
                    val durationSeconds = call.argument<Int>("durationSeconds") ?: 2700
                    val gaugeColor = call.argument<String>("gaugeColor") ?: "#202A44"
                    val darkMode = call.argument<Boolean>("darkMode") ?: false
                    TimerStore.saveRunning(
                        this,
                        durationSeconds,
                        endAtWallMillis,
                        gaugeColor,
                        darkMode
                    )
                    TimerAlarmScheduler.schedule(this, endAtWallMillis)
                    result.success(null)
                }
                "cancelTimer" -> {
                    TimerAlarmScheduler.cancel(this)
                    TimerStore.saveIdle(this)
                    result.success(null)
                }
                "canPostNotifications" -> result.success(canPostNotifications())
                "requestPostNotifications" -> requestPostNotifications(result)
                "canScheduleExactAlarms" -> result.success(canScheduleExactAlarms())
                "openExactAlarmSettings" -> {
                    openExactAlarmSettings()
                    result.success(null)
                }
                else -> result.notImplemented()
            }
        }
    }

    override fun onRequestPermissionsResult(
        requestCode: Int,
        permissions: Array<out String>,
        grantResults: IntArray
    ) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults)
        if (requestCode == REQUEST_POST_NOTIFICATIONS) {
            val granted = grantResults.firstOrNull() == PackageManager.PERMISSION_GRANTED
            notificationPermissionResult?.success(granted)
            notificationPermissionResult = null
        }
    }

    private fun canPostNotifications(): Boolean {
        return Build.VERSION.SDK_INT < Build.VERSION_CODES.TIRAMISU ||
            checkSelfPermission(Manifest.permission.POST_NOTIFICATIONS) ==
            PackageManager.PERMISSION_GRANTED
    }

    private fun requestPostNotifications(result: MethodChannel.Result) {
        if (canPostNotifications()) {
            result.success(true)
            return
        }

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            notificationPermissionResult = result
            requestPermissions(
                arrayOf(Manifest.permission.POST_NOTIFICATIONS),
                REQUEST_POST_NOTIFICATIONS
            )
            return
        }

        result.success(true)
    }

    private fun canScheduleExactAlarms(): Boolean {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.S) {
            return true
        }
        val alarmManager = getSystemService(Context.ALARM_SERVICE) as AlarmManager
        return alarmManager.canScheduleExactAlarms()
    }

    private fun openExactAlarmSettings() {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.S) {
            return
        }
        val intent = Intent(Settings.ACTION_REQUEST_SCHEDULE_EXACT_ALARM).apply {
            data = Uri.parse("package:$packageName")
        }
        startActivity(intent)
    }

    companion object {
        private const val REQUEST_POST_NOTIFICATIONS = 8401
    }
}
