package com.visualprogresstimer.android

import android.content.Context

object TimerStore {
    private const val PREFS = "visual_progress_timer"
    private const val KEY_STATUS = "status"
    private const val KEY_DURATION_SECONDS = "durationSeconds"
    private const val KEY_END_AT_WALL_MILLIS = "endAtWallMillis"
    private const val KEY_GAUGE_COLOR = "gaugeColor"
    private const val KEY_DARK_MODE = "darkMode"

    fun load(context: Context): Map<String, Any> {
        val prefs = context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
        return mapOf(
            "status" to prefs.getString(KEY_STATUS, "idle").orEmpty(),
            "durationSeconds" to prefs.getInt(KEY_DURATION_SECONDS, 2700),
            "endAtWallMillis" to prefs.getLong(KEY_END_AT_WALL_MILLIS, 0L),
            "gaugeColor" to prefs.getString(KEY_GAUGE_COLOR, "#202A44").orEmpty(),
            "darkMode" to prefs.getBoolean(KEY_DARK_MODE, false)
        )
    }

    fun saveSettings(
        context: Context,
        durationSeconds: Int,
        gaugeColor: String,
        darkMode: Boolean
    ) {
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
            .edit()
            .putInt(KEY_DURATION_SECONDS, durationSeconds.coerceIn(60, 3600))
            .putString(KEY_GAUGE_COLOR, gaugeColor)
            .putBoolean(KEY_DARK_MODE, darkMode)
            .apply()
    }

    fun saveRunning(
        context: Context,
        durationSeconds: Int,
        endAtWallMillis: Long,
        gaugeColor: String,
        darkMode: Boolean
    ) {
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
            .edit()
            .putString(KEY_STATUS, "running")
            .putInt(KEY_DURATION_SECONDS, durationSeconds.coerceIn(60, 3600))
            .putLong(KEY_END_AT_WALL_MILLIS, endAtWallMillis)
            .putString(KEY_GAUGE_COLOR, gaugeColor)
            .putBoolean(KEY_DARK_MODE, darkMode)
            .apply()
    }

    fun saveIdle(context: Context) {
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
            .edit()
            .putString(KEY_STATUS, "idle")
            .putLong(KEY_END_AT_WALL_MILLIS, 0L)
            .apply()
    }

    fun saveCompleted(context: Context) {
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
            .edit()
            .putString(KEY_STATUS, "completed")
            .putLong(KEY_END_AT_WALL_MILLIS, 0L)
            .apply()
    }
}
