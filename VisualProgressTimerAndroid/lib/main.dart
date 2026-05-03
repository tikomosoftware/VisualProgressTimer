import 'dart:async';
import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

void main() {
  runApp(const VisualProgressTimerApp());
}

class VisualProgressTimerApp extends StatelessWidget {
  const VisualProgressTimerApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Visual Progress Timer',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xff202a44)),
        useMaterial3: true,
      ),
      home: const TimerScreen(),
    );
  }
}

class TimerScreen extends StatefulWidget {
  const TimerScreen({super.key});

  @override
  State<TimerScreen> createState() => _TimerScreenState();
}

class _TimerScreenState extends State<TimerScreen> {
  static const _channel = MethodChannel('visual_progress_timer/alarm');
  static const _maxMinutes = 60;

  Timer? _ticker;
  Duration _duration = const Duration(minutes: 45);
  DateTime? _endAt;
  Color _gaugeColor = const Color(0xff202a44);
  bool _darkMode = false;
  bool _canNotify = true;
  bool _canScheduleExact = true;

  bool get _isRunning => _endAt != null && remaining > Duration.zero;

  Duration get remaining {
    final endAt = _endAt;
    if (endAt == null) {
      return _duration;
    }
    final value = endAt.difference(DateTime.now());
    return value.isNegative ? Duration.zero : value;
  }

  @override
  void initState() {
    super.initState();
    _loadState();
    _ticker = Timer.periodic(const Duration(milliseconds: 200), (_) {
      if (mounted && _isRunning) {
        setState(() {});
      }
    });
  }

  @override
  void dispose() {
    _ticker?.cancel();
    super.dispose();
  }

  Future<void> _loadState() async {
    final state = await _invokeMap('loadState');
    final durationSeconds = state['durationSeconds'] as int? ?? 2700;
    final endAtMillis = state['endAtWallMillis'] as int? ?? 0;
    final status = state['status'] as String? ?? 'idle';
    final gaugeColor = state['gaugeColor'] as String? ?? '#202A44';
    final darkMode = state['darkMode'] as bool? ?? false;

    setState(() {
      _duration = Duration(seconds: durationSeconds).clampMinutes();
      _endAt = status == 'running' && endAtMillis > 0
          ? DateTime.fromMillisecondsSinceEpoch(endAtMillis)
          : null;
      _gaugeColor = _parseColor(gaugeColor);
      _darkMode = darkMode;
    });
    await _refreshCapabilities();
  }

  Future<Map<dynamic, dynamic>> _invokeMap(
    String method, [
    Object? args,
  ]) async {
    final value = await _channel.invokeMethod<Object?>(method, args);
    return value is Map ? value : <dynamic, dynamic>{};
  }

  Future<void> _refreshCapabilities() async {
    final canNotify =
        await _channel.invokeMethod<bool>('canPostNotifications') ?? true;
    final canScheduleExact =
        await _channel.invokeMethod<bool>('canScheduleExactAlarms') ?? true;
    if (!mounted) {
      return;
    }
    setState(() {
      _canNotify = canNotify;
      _canScheduleExact = canScheduleExact;
    });
  }

  Future<void> _startStop() async {
    if (_isRunning) {
      await _channel.invokeMethod<void>('cancelTimer');
      setState(() => _endAt = null);
      return;
    }

    await _ensureNotificationPermission();
    await _refreshCapabilities();

    final endAt = DateTime.now().add(_duration);
    await _channel.invokeMethod<void>('scheduleTimer', {
      'durationSeconds': _duration.inSeconds,
      'endAtWallMillis': endAt.millisecondsSinceEpoch,
      'gaugeColor': _formatColor(_gaugeColor),
      'darkMode': _darkMode,
    });
    setState(() => _endAt = endAt);
  }

  Future<void> _ensureNotificationPermission() async {
    if (_canNotify) {
      return;
    }
    await _channel.invokeMethod<bool>('requestPostNotifications');
  }

  Future<void> _openExactAlarmSettings() async {
    await _channel.invokeMethod<void>('openExactAlarmSettings');
    await Future<void>.delayed(const Duration(milliseconds: 500));
    await _refreshCapabilities();
  }

  Future<void> _setDurationFromLocalPosition(Offset position, Size size) async {
    final center = Offset(size.width / 2, size.height / 2);
    var degrees =
        math.atan2(position.dy - center.dy, position.dx - center.dx) *
            180 /
            math.pi +
        90;
    if (degrees < 0) {
      degrees += 360;
    }
    final minutes = (degrees / 360 * _maxMinutes).round().clamp(1, _maxMinutes);
    setState(() {
      _endAt = null;
      _duration = Duration(minutes: minutes);
    });
    await _channel.invokeMethod<void>('saveSettings', {
      'durationSeconds': _duration.inSeconds,
      'gaugeColor': _formatColor(_gaugeColor),
      'darkMode': _darkMode,
    });
  }

  Future<void> _setGaugeColor(Color color) async {
    setState(() => _gaugeColor = color);
    await _channel.invokeMethod<void>('saveSettings', {
      'durationSeconds': _duration.inSeconds,
      'gaugeColor': _formatColor(_gaugeColor),
      'darkMode': _darkMode,
    });
  }

  Future<void> _setDarkMode(bool value) async {
    setState(() => _darkMode = value);
    await _channel.invokeMethod<void>('saveSettings', {
      'durationSeconds': _duration.inSeconds,
      'gaugeColor': _formatColor(_gaugeColor),
      'darkMode': _darkMode,
    });
  }

  @override
  Widget build(BuildContext context) {
    final background = _darkMode
        ? const Color(0xff181b1f)
        : const Color(0xfff6f8fb);
    final foreground = _darkMode
        ? const Color(0xfff2f5f8)
        : const Color(0xff1c222a);
    final panel = _darkMode ? const Color(0xff2c3138) : Colors.white;
    final ratio =
        remaining.inMilliseconds /
        Duration(minutes: _maxMinutes).inMilliseconds;

    return Scaffold(
      backgroundColor: background,
      appBar: AppBar(
        backgroundColor: background,
        foregroundColor: foreground,
        title: const Text('Visual Progress Timer'),
        actions: [
          IconButton(
            tooltip: _darkMode ? 'Light mode' : 'Dark mode',
            onPressed: () => _setDarkMode(!_darkMode),
            icon: Icon(_darkMode ? Icons.light_mode : Icons.dark_mode),
          ),
        ],
      ),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(20, 8, 20, 20),
          child: Column(
            children: [
              _CapabilityBanner(
                canNotify: _canNotify,
                canScheduleExact: _canScheduleExact,
                onNotificationTap: _ensureNotificationPermission,
                onExactAlarmTap: _openExactAlarmSettings,
              ),
              Expanded(
                child: LayoutBuilder(
                  builder: (context, constraints) {
                    final size = math.min(
                      constraints.maxWidth,
                      constraints.maxHeight,
                    );
                    return Center(
                      child: GestureDetector(
                        onPanStart: (details) => _setDurationFromLocalPosition(
                          details.localPosition,
                          Size.square(size),
                        ),
                        onPanUpdate: (details) => _setDurationFromLocalPosition(
                          details.localPosition,
                          Size.square(size),
                        ),
                        child: SizedBox.square(
                          dimension: size,
                          child: CustomPaint(
                            painter: TimerPainter(
                              gaugeColor: _gaugeColor,
                              darkMode: _darkMode,
                              ratio: ratio.clamp(0, 1).toDouble(),
                            ),
                          ),
                        ),
                      ),
                    );
                  },
                ),
              ),
              Container(
                height: 72,
                padding: const EdgeInsets.symmetric(horizontal: 14),
                decoration: BoxDecoration(
                  color: panel,
                  borderRadius: BorderRadius.circular(24),
                ),
                child: Row(
                  children: [
                    FilledButton.icon(
                      onPressed: _startStop,
                      icon: Icon(_isRunning ? Icons.stop : Icons.play_arrow),
                      label: Text(_isRunning ? 'Stop' : 'Start'),
                    ),
                    Expanded(
                      child: Center(
                        child: Text(
                          _formatRemaining(remaining),
                          style: TextStyle(
                            color: foreground,
                            fontSize: 30,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      ),
                    ),
                    _ColorMenu(
                      selected: _gaugeColor,
                      onSelected: _setGaugeColor,
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  static Color _parseColor(String value) {
    final hex = value.replaceFirst('#', '');
    return Color(int.parse('ff$hex', radix: 16));
  }

  static String _formatColor(Color color) {
    final value = color.toARGB32() & 0x00ffffff;
    return '#${value.toRadixString(16).padLeft(6, '0').toUpperCase()}';
  }

  static String _formatRemaining(Duration duration) {
    final totalSeconds = duration.inSeconds;
    final minutes = totalSeconds ~/ 60;
    final seconds = totalSeconds % 60;
    return '${minutes.toString().padLeft(2, '0')}:${seconds.toString().padLeft(2, '0')}';
  }
}

class _CapabilityBanner extends StatelessWidget {
  const _CapabilityBanner({
    required this.canNotify,
    required this.canScheduleExact,
    required this.onNotificationTap,
    required this.onExactAlarmTap,
  });

  final bool canNotify;
  final bool canScheduleExact;
  final VoidCallback onNotificationTap;
  final VoidCallback onExactAlarmTap;

  @override
  Widget build(BuildContext context) {
    if (canNotify && canScheduleExact) {
      return const SizedBox(height: 0);
    }

    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Row(
        children: [
          if (!canNotify)
            Expanded(
              child: OutlinedButton.icon(
                onPressed: onNotificationTap,
                icon: const Icon(Icons.notifications_active),
                label: const Text('Allow notifications'),
              ),
            ),
          if (!canNotify && !canScheduleExact) const SizedBox(width: 8),
          if (!canScheduleExact)
            Expanded(
              child: OutlinedButton.icon(
                onPressed: onExactAlarmTap,
                icon: const Icon(Icons.alarm_on),
                label: const Text('Allow exact alarms'),
              ),
            ),
        ],
      ),
    );
  }
}

class _ColorMenu extends StatelessWidget {
  const _ColorMenu({required this.selected, required this.onSelected});

  final Color selected;
  final ValueChanged<Color> onSelected;

  static const colors = [
    Color(0xff202a44),
    Color(0xffd94a3a),
    Color(0xff2e7d6b),
  ];

  @override
  Widget build(BuildContext context) {
    return PopupMenuButton<Color>(
      tooltip: 'Color',
      onSelected: onSelected,
      icon: Icon(Icons.palette, color: selected),
      itemBuilder: (context) {
        return [
          for (final color in colors)
            PopupMenuItem(
              value: color,
              child: Row(
                children: [
                  Container(
                    width: 20,
                    height: 20,
                    decoration: BoxDecoration(
                      color: color,
                      shape: BoxShape.circle,
                    ),
                  ),
                  const SizedBox(width: 12),
                  Text(color == selected ? 'Selected' : 'Use color'),
                ],
              ),
            ),
        ];
      },
    );
  }
}

class TimerPainter extends CustomPainter {
  TimerPainter({
    required this.gaugeColor,
    required this.darkMode,
    required this.ratio,
  });

  final Color gaugeColor;
  final bool darkMode;
  final double ratio;

  @override
  void paint(Canvas canvas, Size size) {
    final side = math.min(size.width, size.height);
    final center = Offset(side / 2, side / 2);
    final frameFill = _blend(
      darkMode ? const Color(0xff2c3138) : const Color(0xffe8eef5),
      gaugeColor,
      darkMode ? 0.5 : 0.38,
    );
    final frameStroke = _blend(
      darkMode ? const Color(0xff84909e) : const Color(0xff9cabbc),
      gaugeColor,
      0.62,
    );
    final framePaint = Paint()..color = frameFill;
    final facePaint = Paint()
      ..color = darkMode ? const Color(0xffedf0f4) : Colors.white;
    final strokePaint = Paint()
      ..color = frameStroke
      ..style = PaintingStyle.stroke
      ..strokeWidth = side * 0.012;

    canvas.drawRRect(
      RRect.fromRectAndRadius(
        Rect.fromLTWH(0, 0, side, side),
        Radius.circular(side * 0.13),
      ),
      framePaint,
    );

    final inset = side * 0.085;
    canvas.drawRRect(
      RRect.fromRectAndRadius(
        Rect.fromLTWH(inset, inset, side - inset * 2, side - inset * 2),
        Radius.circular(side * 0.09),
      ),
      facePaint,
    );
    canvas.drawRRect(
      RRect.fromRectAndRadius(
        Rect.fromLTWH(inset, inset, side - inset * 2, side - inset * 2),
        Radius.circular(side * 0.09),
      ),
      strokePaint,
    );

    final radius = side * 0.29;
    if (ratio > 0.001) {
      canvas.drawArc(
        Rect.fromCircle(center: center, radius: radius),
        -math.pi / 2,
        math.pi * 2 * ratio,
        true,
        Paint()..color = gaugeColor,
      );
    }

    final tickPaint = Paint()
      ..color = Colors.black
      ..strokeCap = StrokeCap.round;
    for (var minute = 0; minute < 60; minute++) {
      final major = minute % 5 == 0;
      tickPaint.strokeWidth = major ? 2.2 : 1.1;
      final angle = minute * 6 * math.pi / 180 - math.pi / 2;
      final outer = side * 0.33;
      final inner = outer - (major ? side * 0.032 : side * 0.018);
      canvas.drawLine(
        _point(center, inner, angle),
        _point(center, outer, angle),
        tickPaint,
      );
    }

    _drawPointer(canvas, center, radius, side);
    canvas.drawCircle(
      center.translate(side * 0.012, side * 0.018),
      side * 0.066,
      Paint()..color = const Color(0x45202838),
    );
    canvas.drawCircle(
      center,
      side * 0.062,
      Paint()..color = const Color(0xffdce5ee),
    );
  }

  void _drawPointer(Canvas canvas, Offset center, double radius, double side) {
    final angle = math.pi * 2 * ratio - math.pi / 2;
    final end = _point(center, radius * 1.08, angle);
    final pointerPaint = Paint()
      ..color = const Color(0x88a2b0bf)
      ..strokeWidth = side * 0.009
      ..strokeCap = StrokeCap.round;
    canvas.drawLine(center, end, pointerPaint);
  }

  static Offset _point(Offset center, double radius, double angle) {
    return Offset(
      center.dx + math.cos(angle) * radius,
      center.dy + math.sin(angle) * radius,
    );
  }

  static Color _blend(Color base, Color tint, double amount) {
    final clamped = amount.clamp(0, 1).toDouble();
    final baseValue = base.toARGB32();
    final tintValue = tint.toARGB32();
    final baseRed = (baseValue >> 16) & 0xff;
    final baseGreen = (baseValue >> 8) & 0xff;
    final baseBlue = baseValue & 0xff;
    final tintRed = (tintValue >> 16) & 0xff;
    final tintGreen = (tintValue >> 8) & 0xff;
    final tintBlue = tintValue & 0xff;

    return Color.fromARGB(
      255,
      (baseRed + (tintRed - baseRed) * clamped).round(),
      (baseGreen + (tintGreen - baseGreen) * clamped).round(),
      (baseBlue + (tintBlue - baseBlue) * clamped).round(),
    );
  }

  @override
  bool shouldRepaint(TimerPainter oldDelegate) {
    return oldDelegate.ratio != ratio ||
        oldDelegate.gaugeColor != gaugeColor ||
        oldDelegate.darkMode != darkMode;
  }
}

extension on Duration {
  Duration clampMinutes() {
    final minutes = inMinutes.clamp(1, 60);
    return Duration(minutes: minutes);
  }
}
