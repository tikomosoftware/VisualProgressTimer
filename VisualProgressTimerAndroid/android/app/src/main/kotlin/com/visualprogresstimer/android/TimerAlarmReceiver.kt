package com.visualprogresstimer.android

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent

class TimerAlarmReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent) {
        TimerStore.saveCompleted(context)
        NotificationHelper.showTimeUp(context)
    }
}
