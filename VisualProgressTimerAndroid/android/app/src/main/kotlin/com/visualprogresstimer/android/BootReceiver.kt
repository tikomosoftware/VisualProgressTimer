package com.visualprogresstimer.android

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent

class BootReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent) {
        val state = TimerStore.load(context)
        if (state["status"] != "running") {
            return
        }

        val endAtWallMillis = state["endAtWallMillis"] as? Long ?: return
        if (endAtWallMillis > System.currentTimeMillis()) {
            TimerAlarmScheduler.schedule(context, endAtWallMillis)
        } else {
            TimerStore.saveCompleted(context)
            NotificationHelper.showTimeUp(context)
        }
    }
}
