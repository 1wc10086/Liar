import 'package:flutter/material.dart';
import 'dart:ui';

enum SwitchStyle { material, pixel }

class EnhancedSwitch extends StatelessWidget {
  final bool value;
  final ValueChanged<bool>? onChanged;
  final SwitchStyle style;
  final double? scale;

  const EnhancedSwitch({
    super.key,
    required this.value,
    this.onChanged,
    this.style = SwitchStyle.material,
    this.scale,
  });

  @override
  Widget build(BuildContext context) {
    if (style == SwitchStyle.material) {
      return Transform.scale(
        scale: scale ?? 1.0,
        child: Switch(value: value, onChanged: onChanged),
      );
    }
    return _PixelSwitch(value: value, onChanged: onChanged, scale: scale);
  }
}

class _PixelSwitch extends StatefulWidget {
  final bool value;
  final ValueChanged<bool>? onChanged;
  final double? scale;

  const _PixelSwitch({required this.value, this.onChanged, this.scale});

  @override
  State<_PixelSwitch> createState() => _PixelSwitchState();
}

class _PixelSwitchState extends State<_PixelSwitch> with SingleTickerProviderStateMixin {
  late AnimationController _ctrl;
  static const trackW = 56.0;
  static const trackH = 28.0;
  static const thumbS = 20.0;
  static const borderW = 1.8;
  static const thumbStart = (trackH - thumbS) / 2;
  static const thumbEnd = trackW - thumbS / 2 - trackH / 2;

  @override
  void initState() {
    super.initState();
    _ctrl = AnimationController(
      vsync: this, duration: const Duration(milliseconds: 240),
      value: widget.value ? 1.0 : 0.0,
    );
  }

  @override
  void didUpdateWidget(_PixelSwitch oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.value != widget.value) {
      if (widget.value) { _ctrl.forward(); } else { _ctrl.reverse(); }
    }
  }

  @override
  void dispose() { _ctrl.dispose(); super.dispose(); }

  Color _lerpC(Color a, Color b) => Color.lerp(a, b, Curves.easeOutCubic.transform(_ctrl.value))!;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final onSurface = cs.onSurface;
    final onPrimary = cs.onPrimary;

    return Transform.scale(
      scale: widget.scale ?? 1.0,
      child: GestureDetector(
        onTap: widget.onChanged != null ? () => widget.onChanged!(!widget.value) : null,
        child: AnimatedBuilder(
          animation: _ctrl,
          builder: (context, _) {
            final t = Curves.easeOutCubic.transform(_ctrl.value);
            final offset = lerpDouble(thumbStart, thumbEnd, t)!;

            return Stack(clipBehavior: Clip.none, children: [
              Container(
                width: trackW, height: trackH,
                decoration: BoxDecoration(
                  color: Color.lerp(cs.surfaceContainerHighest, cs.primary, t),
                  borderRadius: BorderRadius.circular(trackH / 2),
                  border: Border.all(
                    color: Color.lerp(cs.outline, cs.primary, t)!,
                    width: borderW,
                  ),
                ),
              ),
              Positioned(
                left: offset,
                top: thumbStart,
                child: Container(
                  width: thumbS, height: thumbS,
                  decoration: BoxDecoration(
                    color: Color.lerp(cs.outline, onPrimary, t),
                    shape: BoxShape.circle,
                  ),
                ),
              ),
            ]);
          },
        ),
      ),
    );
  }
}

class EnhancedSwitchListTile extends StatelessWidget {
  final Widget? leading;
  final Widget title;
  final Widget? subtitle;
  final bool value;
  final ValueChanged<bool>? onChanged;
  final SwitchStyle style;
  final double? scale;

  const EnhancedSwitchListTile({
    super.key,
    this.leading,
    required this.title,
    this.subtitle,
    required this.value,
    this.onChanged,
    this.style = SwitchStyle.material,
    this.scale,
  });

  @override
  Widget build(BuildContext context) {
    return ListTile(
      leading: leading,
      title: title,
      subtitle: subtitle,
      onTap: onChanged != null ? () => onChanged!(!value) : null,
      trailing: EnhancedSwitch(
        value: value,
        onChanged: onChanged,
        style: style,
        scale: scale,
      ),
    );
  }
}
