import 'dart:convert';

dynamic decodeJsonc(String source) {
  final withoutComments = StringBuffer();
  var inString = false;
  var escaped = false;
  for (var i = 0; i < source.length; i++) {
    final char = source[i];
    if (inString) {
      withoutComments.write(char);
      if (escaped) {
        escaped = false;
      } else if (char == r'\') {
        escaped = true;
      } else if (char == '"') {
        inString = false;
      }
      continue;
    }
    if (char == '"') {
      inString = true;
      withoutComments.write(char);
    } else if (char == '/' && i + 1 < source.length && source[i + 1] == '/') {
      withoutComments.write(' ');
      i += 2;
      while (i < source.length && source[i] != '\n' && source[i] != '\r') {
        i++;
      }
      if (i < source.length) withoutComments.write(source[i]);
    } else if (char == '/' && i + 1 < source.length && source[i + 1] == '*') {
      withoutComments.write(' ');
      i += 2;
      while (i + 1 < source.length && !(source[i] == '*' && source[i + 1] == '/')) {
        if (source[i] == '\n' || source[i] == '\r') withoutComments.write(source[i]);
        i++;
      }
      if (i + 1 >= source.length) throw const FormatException('Unterminated JSONC comment');
      i++;
    } else {
      withoutComments.write(char);
    }
  }

  final withoutTrailingCommas = StringBuffer();
  final json = withoutComments.toString();
  inString = false;
  escaped = false;
  for (var i = 0; i < json.length; i++) {
    final char = json[i];
    if (inString) {
      withoutTrailingCommas.write(char);
      if (escaped) {
        escaped = false;
      } else if (char == r'\') {
        escaped = true;
      } else if (char == '"') {
        inString = false;
      }
      continue;
    }
    if (char == '"') {
      inString = true;
      withoutTrailingCommas.write(char);
    } else if (char == ',') {
      var next = i + 1;
      while (next < json.length && json[next].trim().isEmpty) {
        next++;
      }
      if (next >= json.length || (json[next] != ']' && json[next] != '}')) {
        withoutTrailingCommas.write(char);
      }
    } else {
      withoutTrailingCommas.write(char);
    }
  }
  try {
    return jsonDecode(withoutTrailingCommas.toString());
  } on FormatException {
    rethrow;
  } catch (error) {
    throw FormatException('Invalid JSONC: $error');
  }
}

bool _isWs(String ch) => ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r';

int _skipString(String s, int i) {
  i++;
  while (i < s.length) {
    final ch = s[i];
    if (ch == r'\') {
      i += 2;
      continue;
    }
    if (ch == '"') return i + 1;
    i++;
  }
  return i;
}

int _skipValue(String s, int i) {
  if (i >= s.length) return i;
  final ch = s[i];
  if (ch == '"') return _skipString(s, i);
  if (ch == '{' || ch == '[') {
    final open = ch;
    final close = open == '{' ? '}' : ']';
    var depth = 1;
    i++;
    while (i < s.length && depth > 0) {
      final c = s[i];
      if (c == '"') {
        i = _skipString(s, i);
        continue;
      }
      if (c == '/' && i + 1 < s.length && s[i + 1] == '/') {
        i += 2;
        while (i < s.length && s[i] != '\n' && s[i] != '\r') {
          i++;
        }
        continue;
      }
      if (c == '/' && i + 1 < s.length && s[i + 1] == '*') {
        i += 2;
        while (i + 1 < s.length && !(s[i] == '*' && s[i + 1] == '/')) {
          i++;
        }
        if (i + 1 < s.length) i += 2;
        continue;
      }
      if (c == open) {
        depth++;
      } else if (c == close) {
        depth--;
      }
      i++;
    }
    return i;
  }
  while (i < s.length &&
      !_isWs(s[i]) &&
      s[i] != ',' &&
      s[i] != '}' &&
      s[i] != ']') {
    i++;
  }
  return i;
}

String patchJsoncString(String source, Map<String, String> updates) {
  final remaining = Map<String, String>.from(updates);
  final spans = <List<dynamic>>[]; 

  var i = 0;
  while (i < source.length) {
    final ch = source[i];
    if (ch == '"') {
      final start = i;
      i = _skipString(source, i);
      final token = source.substring(start + 1, i - 1);
      if (!remaining.containsKey(token)) continue;
      var k = i;
      while (k < source.length && _isWs(source[k])) {
        k++;
      }
      if (k >= source.length || source[k] != ':') continue;
      k++;
      while (k < source.length && _isWs(source[k])) {
        k++;
      }
      final valEnd = _skipValue(source, k);
      spans.add([k, valEnd, jsonEncode(remaining[token]!)]);
      remaining.remove(token);
      continue;
    }
    if (ch == '/' && i + 1 < source.length && source[i + 1] == '/') {
      i += 2;
      while (i < source.length && source[i] != '\n' && source[i] != '\r') {
        i++;
      }
      continue;
    }
    if (ch == '/' && i + 1 < source.length && source[i + 1] == '*') {
      i += 2;
      while (i + 1 < source.length && !(source[i] == '*' && source[i + 1] == '/')) {
        i++;
      }
      if (i + 1 < source.length) i += 2;
      continue;
    }
    i++;
  }

  if (remaining.isNotEmpty) {
    final insertAt = _findOuterObjectClose(source);
    if (insertAt != null) {
      final head = source.substring(0, insertAt);
      final tail = source.substring(insertAt);
      var lastNonWs = head.length - 1;
      while (lastNonWs >= 0 && _isWs(head[lastNonWs])) {
        lastNonWs--;
      }
      final lastCh = lastNonWs >= 0 ? head[lastNonWs] : '';
      final needsComma = lastCh != ',' && lastCh != '{' && lastCh != '[';
      final indent = _indentForInsert(head);
      final pieces = <String>[];
      for (final entry in remaining.entries) {
        pieces.add('$indent"${entry.key}": ${jsonEncode(entry.value)}');
      }
      final joined = pieces.join(',\n');
      final comma = needsComma ? ',' : '';
      source = '${head.substring(0, lastNonWs + 1)}$comma\n$joined\n$tail';
    } else {
      final fallback = <String, dynamic>{};
      for (final e in remaining.entries) {
        fallback[e.key] = e.value;
      }
      source = const JsonEncoder.withIndent('    ').convert(fallback);
    }
    remaining.clear();
  }

  spans.sort((a, b) => (b[0] as int).compareTo(a[0] as int));
  for (final s in spans) {
    source =
        '${source.substring(0, s[0] as int)}${s[2]}${source.substring(s[1] as int)}';
  }
  return source;
}

int? _findOuterObjectClose(String s) {
  var i = 0;
  final stack = <String>[];
  int? lastClose;
  while (i < s.length) {
    final ch = s[i];
    if (ch == '"') {
      i = _skipString(s, i);
      continue;
    }
    if (ch == '/' && i + 1 < s.length && s[i + 1] == '/') {
      i += 2;
      while (i < s.length && s[i] != '\n' && s[i] != '\r') {
        i++;
      }
      continue;
    }
    if (ch == '/' && i + 1 < s.length && s[i + 1] == '*') {
      i += 2;
      while (i + 1 < s.length && !(s[i] == '*' && s[i + 1] == '/')) {
        i++;
      }
      if (i + 1 < s.length) i += 2;
      continue;
    }
    if (ch == '{' || ch == '[') {
      stack.add(ch);
    } else if (ch == '}' || ch == ']') {
      stack.removeLast();
      if (stack.isEmpty) lastClose = i;
    }
    i++;
  }
  return lastClose;
}

String _indentForInsert(String head) {
  final lineStart = head.lastIndexOf('\n');
  final slice = head.substring(lineStart + 1);
  final match = RegExp(r'^(\s*)').firstMatch(slice);
  return match?.group(1) ?? '    ';
}
