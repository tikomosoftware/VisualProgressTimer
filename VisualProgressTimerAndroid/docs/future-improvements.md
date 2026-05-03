# Future Improvements

## Notification stop action

Current behavior:

- When the timer finishes, Android shows a high-priority timer notification.
- The alarm sound is provided by the notification channel.
- Tapping or dismissing the notification stops the notification sound on the tested device.

This is acceptable for the initial Android version because it follows a familiar
Android notification pattern and keeps the implementation simple.

Possible future improvement:

- Add a `STOP` action button directly to the time-up notification.
- Let users stop the alarm without opening the app.
- Optionally add the same stop behavior to the in-app time-up state.

Recommended implementation if this is added:

- Move alarm sound playback from notification-channel sound to an app-controlled
  Android component such as a foreground service or dedicated alarm sound helper.
- Use `MediaPlayer` or `Ringtone` for playback.
- Add a notification action that sends a broadcast such as
  `com.visualprogresstimer.android.STOP_ALARM`.
- Handle that action in a receiver, stop playback, cancel the notification, and
  update timer state.

Tradeoffs:

- Pros: clearer stop affordance, more alarm-app-like behavior, easier to support
  repeated or looping alarm sound.
- Cons: more native Android code, more lifecycle handling, and more edge cases
  around app process death and OEM battery restrictions.

Decision for now:

Keep the current notification-channel sound behavior. Revisit this after the
core Android timer experience is stable on real devices.
