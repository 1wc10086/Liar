/* kernelx-manifest
[
  {
    "id": "pax.decode",
    "implementation": "implementation",
    "params": [
      { "name": "InputFile", "type": "path", "required": true, "extensions": [".pax", ".PAX"] },
      { "name": "OutputFile", "type": "path", "required": true, "extensions": [".json"] }
    ]
  },
  {
    "id": "pax.encode",
    "implementation": "implementation",
    "params": [
      { "name": "InputFile", "type": "path", "required": true, "extensions": [".json"] },
      { "name": "OutputFile", "type": "path", "required": true, "extensions": [".pax", ".PAX"] }
    ]
  }
]
*/

var PAX_MAX_RECORDS = 100000;
var PAX_COMPOSITION_HEADER = 1;
var PAX_LAYER_HEADER = 2;
var PAX_ANCHOR_POINT = 3;
var PAX_POSITION = 4;
var PAX_SCALE = 5;
var PAX_ROTATION = 6;
var PAX_OPACITY = 7;
var PAX_KEYFRAME_MARKER = 8;
var PAX_END_OF_LAYER = 9;
var PAX_END_OF_POPFX = 9999999;
var PAX_LOOP_REPEAT = 10;
var PAX_LOOP_PINGPONG = 11;

function paxText(bytes) {
  var text = "";
  for (var i = 0; i < bytes.length;) {
    var first = bytes[i++];
    if (first < 0x80) {
      text += String.fromCharCode(first);
      continue;
    }
    var count = first < 0xe0 ? 2 : first < 0xf0 ? 3 : 4;
    if (i + count - 1 > bytes.length) throw new Error("Invalid UTF-8 PAX string");
    var codePoint = first & (count === 2 ? 0x1f : count === 3 ? 0x0f : 0x07);
    for (var j = 1; j < count; ++j) {
      var next = bytes[i++];
      if ((next & 0xc0) !== 0x80) throw new Error("Invalid UTF-8 PAX string");
      codePoint = (codePoint << 6) | (next & 0x3f);
    }
    if (codePoint <= 0xffff) text += String.fromCharCode(codePoint);
    else {
      codePoint -= 0x10000;
      text += String.fromCharCode(0xd800 | (codePoint >> 10), 0xdc00 | (codePoint & 0x3ff));
    }
  }
  return text;
}

function paxDisplayText(text) {
  var nul = text.indexOf("\0");
  return nul < 0 ? text : text.substring(0, nul);
}

function paxEncodedText(displayText, encodedText) {
  if (typeof encodedText === "string" && paxDisplayText(encodedText) === displayText) return encodedText;
  return displayText;
}

function paxUtf8(text) {
  var bytes = [];
  text = String(text);
  for (var i = 0; i < text.length; ++i) {
    var codePoint = text.charCodeAt(i);
    if (codePoint >= 0xd800 && codePoint <= 0xdbff && i + 1 < text.length) {
      var low = text.charCodeAt(i + 1);
      if (low >= 0xdc00 && low <= 0xdfff) {
        codePoint = 0x10000 + ((codePoint - 0xd800) << 10) + low - 0xdc00;
        ++i;
      }
    }
    if (codePoint < 0x80) bytes.push(codePoint);
    else if (codePoint < 0x800) bytes.push(0xc0 | (codePoint >> 6), 0x80 | (codePoint & 0x3f));
    else if (codePoint < 0x10000) bytes.push(0xe0 | (codePoint >> 12), 0x80 | ((codePoint >> 6) & 0x3f), 0x80 | (codePoint & 0x3f));
    else bytes.push(0xf0 | (codePoint >> 18), 0x80 | ((codePoint >> 12) & 0x3f), 0x80 | ((codePoint >> 6) & 0x3f), 0x80 | (codePoint & 0x3f));
  }
  return new Uint8Array(bytes);
}

function paxReader(bytes) {
  var position = 0;

  function requireBytes(count, field) {
    if (count < 0 || count > bytes.length - position) throw new Error("Truncated PAX " + field + " at 0x" + position.toString(16));
  }

  function u32At(offset) {
    return (bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24)) >>> 0;
  }

  return {
    position: function () { return position; },
    remaining: function () { return bytes.length - position; },
    i32: function (field) {
      requireBytes(4, field || "i32");
      var value = u32At(position);
      position += 4;
      return value > 0x7fffffff ? value - 0x100000000 : value;
    },
    u8: function (field) {
      requireBytes(1, field || "u8");
      return bytes[position++];
    },
    boolean: function (field) { return this.u8(field || "boolean") !== 0; },
    f64: function (field) {
      requireBytes(8, field || "f64");
      var low = u32At(position);
      var high = u32At(position + 4);
      position += 8;
      var sign = high >>> 31;
      var exponent = (high >>> 20) & 0x7ff;
      var fraction = (high & 0xfffff) * 4294967296 + low;
      if (exponent === 0x7ff) return fraction ? NaN : (sign ? -Infinity : Infinity);
      if (exponent === 0) return (sign ? -1 : 1) * fraction * Math.pow(2, -1074);
      return (sign ? -1 : 1) * (1 + fraction / 4503599627370496) * Math.pow(2, exponent - 1023);
    },
    string: function (length, field) {
      if (!Number.isInteger(length) || length < 0) throw new Error("Invalid PAX " + (field || "string") + " length");
      requireBytes(length, field || "string");
    var value = paxText(bytes.subarray(position, position + length));
      position += length;
      return value;
    }
  };
}

function paxWriter() {
  var chunks = [];
  var length = 0;

  function push(bytes) {
    chunks.push(bytes);
    length += bytes.length;
  }

  function number(value, field) {
    if (typeof value !== "number" || !Number.isFinite(value)) throw new Error("Invalid PAX " + field);
    return value;
  }

  return {
    i32: function (value, field) {
      value = number(value, field || "i32");
      if (!Number.isInteger(value) || value < -2147483648 || value > 2147483647) throw new Error("Invalid PAX " + (field || "i32"));
      var bytes = new Uint8Array(4);
      var unsigned = value < 0 ? value + 0x100000000 : value;
      bytes[0] = unsigned & 0xff;
      bytes[1] = (unsigned >>> 8) & 0xff;
      bytes[2] = (unsigned >>> 16) & 0xff;
      bytes[3] = (unsigned >>> 24) & 0xff;
      push(bytes);
    },
    boolean: function (value) { push(new Uint8Array([value ? 1 : 0])); },
    f64: function (value, field) {
      value = number(value, field || "f64");
      var buffer = new ArrayBuffer(8);
      new DataView(buffer).setFloat64(0, value, true);
      push(new Uint8Array(buffer));
    },
    string: function (value, field) {
      if (typeof value !== "string") throw new Error("Invalid PAX " + (field || "string"));
      var bytes = paxUtf8(value);
      this.i32(bytes.length, (field || "string") + " length");
      push(bytes);
    },
    stringSized: function (value, length, field) {
      if (typeof value !== "string" || !Number.isInteger(length) || length < 0) throw new Error("Invalid PAX " + (field || "string"));
      var bytes = paxUtf8(value);
      if (bytes.length > length) throw new Error("PAX " + (field || "string") + " exceeds its encoded length");
      this.i32(length, (field || "string") + " length");
      var padded = new Uint8Array(length);
      padded.set(bytes);
      push(padded);
    },
    finish: function () {
      var output = new Uint8Array(length);
      var offset = 0;
      for (var i = 0; i < chunks.length; ++i) {
        output.set(chunks[i], offset);
        offset += chunks[i].length;
      }
      return output;
    }
  };
}

function paxRequireCount(value, field) {
  if (!Number.isInteger(value) || value < 0 || value > PAX_MAX_RECORDS) throw new Error("Invalid PAX " + field);
  return value;
}

function paxTransformName(tag) {
  if (tag === PAX_ANCHOR_POINT) return "anchorPoint";
  if (tag === PAX_POSITION) return "position";
  if (tag === PAX_SCALE) return "scale";
  if (tag === PAX_ROTATION) return "rotation";
  if (tag === PAX_OPACITY) return "opacity";
  return null;
}

function paxTransformValue(tag, value1, value2) {
  if (tag === PAX_SCALE) return { x: value1 / 100, y: value2 / 100 };
  if (tag === PAX_ROTATION) return { degrees: value1, radians: value1 * Math.PI / 180 };
  if (tag === PAX_OPACITY) return value1 / 100;
  return { x: value1, y: value2 };
}

function paxRawTransformValue(tag, value) {
  if (tag === PAX_SCALE) return { value1: value.x * 100, value2: value.y * 100 };
  if (tag === PAX_ROTATION) return { value1: value.degrees !== undefined ? value.degrees : value.radians * 180 / Math.PI, value2: 0 };
  if (tag === PAX_OPACITY) return { value1: value * 100, value2: 0 };
  return { value1: value.x, value2: value.y };
}

function paxReadTransform(reader, tag, sourceDuration, compositionDuration) {
  var name = paxTransformName(tag);
  if (!name) throw new Error("Unknown PAX transform tag " + tag + " at 0x" + reader.position().toString(16));
  var initialValue1 = reader.f64(name + " initial value 1");
  var initialValue2 = reader.f64(name + " initial value 2");
  var staticFrameCount = paxRequireCount(reader.i32(name + " static frame count"), name + " static frame count");
  var encodedKeyedFrameCount = paxRequireCount(reader.i32(name + " keyed frame count"), name + " keyed frame count");
  var keyedFrameCount = encodedKeyedFrameCount;
  var hasLoop = reader.boolean(name + " has loop");
  var loopType = reader.i32(name + " loop type");
  var loopFrame = reader.i32(name + " loop frame");
  if (hasLoop && loopType !== PAX_LOOP_REPEAT && loopType !== PAX_LOOP_PINGPONG) throw new Error("Invalid PAX " + name + " loop type " + loopType);

  var frames = [];
  var firstStaticFrame = null;
  if (staticFrameCount > 0) {
    firstStaticFrame = reader.i32(name + " first static frame");
    keyedFrameCount = 0;
    for (var staticIndex = 0; staticIndex < staticFrameCount; ++staticIndex) {
      var staticValue1 = reader.f64(name + " static value 1");
      var staticValue2 = reader.f64(name + " static value 2");
      frames.push({ frame: firstStaticFrame + staticIndex, value: paxTransformValue(tag, staticValue1, staticValue2) });
    }
  } else {
    frames.push({ frame: 0, value: paxTransformValue(tag, initialValue1, initialValue2) });
    for (var keyIndex = 0; keyIndex < keyedFrameCount; ++keyIndex) {
      var marker = reader.i32(name + " keyframe marker");
      var sourceFrame = reader.i32(name + " source frame");
      var value1 = reader.f64(name + " keyframe value 1");
      var value2 = reader.f64(name + " keyframe value 2");
      frames.push({
        marker: marker,
        sourceFrame: sourceFrame,
        frame: sourceFrame * Math.floor(compositionDuration / sourceDuration),
        value: paxTransformValue(tag, value1, value2)
      });
    }
  }

  return {
    tag: tag,
    type: name,
    initialValue: paxTransformValue(tag, initialValue1, initialValue2),
    staticFrameCount: staticFrameCount,
    firstStaticFrame: firstStaticFrame,
    encodedKeyedFrameCount: encodedKeyedFrameCount,
    keyedFrameCount: keyedFrameCount,
    encodedLoopType: loopType,
    encodedLoopFrame: loopFrame,
    loop: hasLoop ? { type: loopType === PAX_LOOP_REPEAT ? "repeat" : "pingPong", frame: loopFrame } : null,
    frames: frames
  };
}

function paxReadLayer(reader, version, footageById, compositionDuration, index) {
  var tag = reader.i32("layer header");
  if (tag !== PAX_LAYER_HEADER) throw new Error("Expected PAX layer header at 0x" + (reader.position() - 4).toString(16));
  var enabled = reader.boolean("layer enabled");
  if (!enabled) return { index: index, enabled: false };

  var isFootage = reader.boolean("layer type");
  var footageId = reader.i32("layer footage id");
  var folderLength = paxRequireCount(reader.i32("layer folder length"), "layer folder length");
  var folderEncodedText = reader.string(folderLength, "layer folder");
  var folder = paxDisplayText(folderEncodedText);
  var layerNameLength = paxRequireCount(reader.i32("layer name length"), "layer name length");
  var layerNameEncodedText = reader.string(layerNameLength, "layer name");
  var layerName = paxDisplayText(layerNameEncodedText);
  var startFrame = reader.i32("layer start frame");
  var sourceDuration = reader.i32("layer source duration");
  var duration = reader.i32("layer duration");
  var offset = reader.i32("layer offset");
  if (sourceDuration <= 0) throw new Error("Invalid PAX layer source duration");
  var additive = version >= 5 ? reader.boolean("layer additive") : false;

  var layer = {
    index: index,
    enabled: true,
    type: isFootage ? "footage" : "composition",
    footageId: footageId,
    folder: folder,
    folderEncodedText: folderEncodedText,
    folderEncodedLength: folderLength,
    name: layerName,
    nameEncodedText: layerNameEncodedText,
    nameEncodedLength: layerNameLength,
    startFrame: startFrame,
    sourceDuration: sourceDuration,
    duration: duration,
    offset: offset,
    additive: additive,
    transforms: {}
  };
  if (isFootage) layer.footage = footageById[footageId] || null;

  for (;;) {
    var transformTag = reader.i32("layer transform tag");
    if (transformTag === PAX_END_OF_LAYER) break;
    var transform = paxReadTransform(reader, transformTag, sourceDuration, duration);
    layer.transforms[transform.type] = transform;
  }
  return layer;
}

function paxAddDefaults(layer, width, height) {
  if (!layer.enabled) return;
  var transforms = layer.transforms;
  if (!transforms.opacity) transforms.opacity = { type: "opacity", defaulted: true, frames: [{ frame: 0, value: 1 }] };
  if (!transforms.rotation) transforms.rotation = { type: "rotation", defaulted: true, frames: [{ frame: 0, value: { degrees: 0, radians: 0 } }] };
  if (!transforms.scale) transforms.scale = { type: "scale", defaulted: true, frames: [{ frame: 0, value: { x: 1, y: 1 } }] };
  if (!transforms.position) transforms.position = { type: "position", defaulted: true, frames: [{ frame: 0, value: { x: width / 2, y: height / 2 } }] };
  if (layer.type === "footage" && !transforms.anchorPoint && layer.footage) {
    transforms.anchorPoint = { type: "anchorPoint", defaulted: true, frames: [{ frame: 0, value: { x: layer.footage.width / 2, y: layer.footage.height / 2 } }] };
  }
}

function paxTransformTag(transform, name) {
  if (transform && Number.isInteger(transform.tag)) return transform.tag;
  if (name === "anchorPoint") return PAX_ANCHOR_POINT;
  if (name === "position") return PAX_POSITION;
  if (name === "scale") return PAX_SCALE;
  if (name === "rotation") return PAX_ROTATION;
  if (name === "opacity") return PAX_OPACITY;
  throw new Error("Invalid PAX transform name " + name);
}

function paxEncodeTransform(writer, transform, name, sourceDuration, compositionDuration) {
  var tag = paxTransformTag(transform, name);
  if (!paxTransformName(tag) || !transform || !Array.isArray(transform.frames) || !transform.frames.length) throw new Error("Invalid PAX " + name + " transform");
  var initial = paxRawTransformValue(tag, transform.initialValue === undefined ? transform.frames[0].value : transform.initialValue);
  var staticCount = transform.staticFrameCount || 0;
  if (!Number.isInteger(staticCount) || staticCount < 0 || staticCount > PAX_MAX_RECORDS) throw new Error("Invalid PAX " + name + " static frame count");
  var frames = transform.frames;
  var keyedFrames = staticCount ? [] : frames.slice(1);
  if (staticCount && frames.length !== staticCount) throw new Error("PAX " + name + " static frame count does not match frames");
  if (!staticCount && transform.encodedKeyedFrameCount !== undefined && transform.encodedKeyedFrameCount !== keyedFrames.length) {
    transform.encodedKeyedFrameCount = keyedFrames.length;
  }
  var loopType = transform.encodedLoopType === undefined ? 0 : transform.encodedLoopType;
  var loopFrame = transform.encodedLoopFrame === undefined ? 0 : transform.encodedLoopFrame;
  if (transform.loop) {
    loopType = transform.loop.type === "repeat" ? PAX_LOOP_REPEAT : transform.loop.type === "pingPong" ? PAX_LOOP_PINGPONG : 0;
    if (!loopType || !Number.isInteger(transform.loop.frame)) throw new Error("Invalid PAX " + name + " loop");
    loopFrame = transform.loop.frame;
  }
  writer.i32(tag, name + " tag");
  writer.f64(initial.value1, name + " initial value 1");
  writer.f64(initial.value2, name + " initial value 2");
  writer.i32(staticCount, name + " static frame count");
  writer.i32(staticCount ? transform.encodedKeyedFrameCount : keyedFrames.length, name + " keyed frame count");
  writer.boolean(!!transform.loop);
  writer.i32(loopType, name + " loop type");
  writer.i32(loopFrame, name + " loop frame");
  if (staticCount) {
    if (!Number.isInteger(transform.firstStaticFrame)) throw new Error("Invalid PAX " + name + " first static frame");
    writer.i32(transform.firstStaticFrame, name + " first static frame");
    for (var staticIndex = 0; staticIndex < frames.length; ++staticIndex) {
      var staticValue = paxRawTransformValue(tag, frames[staticIndex].value);
      writer.f64(staticValue.value1, name + " static value 1");
      writer.f64(staticValue.value2, name + " static value 2");
    }
  } else {
    for (var keyIndex = 0; keyIndex < keyedFrames.length; ++keyIndex) {
      var keyframe = keyedFrames[keyIndex];
      var rawValue = paxRawTransformValue(tag, keyframe.value);
      var sourceFrame = keyframe.sourceFrame;
      if (!Number.isInteger(sourceFrame)) sourceFrame = Math.round(keyframe.frame * sourceDuration / compositionDuration);
      writer.i32(keyframe.marker === undefined ? PAX_KEYFRAME_MARKER : keyframe.marker, name + " keyframe marker");
      writer.i32(sourceFrame, name + " source frame");
      writer.f64(rawValue.value1, name + " keyframe value 1");
      writer.f64(rawValue.value2, name + " keyframe value 2");
    }
  }
}

function paxEncodeLayer(writer, layer, version, compositionDuration, index) {
  if (!layer || typeof layer !== "object") throw new Error("Invalid PAX layer " + index);
  writer.i32(PAX_LAYER_HEADER, "layer header");
  writer.boolean(layer.enabled === true);
  if (layer.enabled !== true) return;
  var isFootage = layer.type === "footage";
  if (!isFootage && layer.type !== "composition") throw new Error("Invalid PAX layer type");
  if (!Number.isInteger(layer.footageId)) throw new Error("Invalid PAX layer footage ID");
  if (!Number.isInteger(layer.startFrame) || !Number.isInteger(layer.sourceDuration) || layer.sourceDuration <= 0 || !Number.isInteger(layer.duration) || !Number.isInteger(layer.offset)) {
    throw new Error("Invalid PAX layer timing");
  }
  writer.boolean(isFootage);
  writer.i32(layer.footageId, "layer footage ID");
  var folderText = paxEncodedText(layer.folder || "", layer.folderEncodedText);
  var nameText = paxEncodedText(layer.name, layer.nameEncodedText);
  writer.stringSized(folderText, layer.folderEncodedLength === undefined ? paxUtf8(folderText).length : layer.folderEncodedLength, "layer folder");
  writer.stringSized(nameText, layer.nameEncodedLength === undefined ? paxUtf8(nameText).length : layer.nameEncodedLength, "layer name");
  writer.i32(layer.startFrame, "layer start frame");
  writer.i32(layer.sourceDuration, "layer source duration");
  writer.i32(layer.duration, "layer duration");
  writer.i32(layer.offset, "layer offset");
  if (version >= 5) writer.boolean(layer.additive === true);

  var transforms = layer.transforms || {};
  var order = ["anchorPoint", "position", "scale", "rotation", "opacity"];
  for (var transformIndex = 0; transformIndex < order.length; ++transformIndex) {
    var name = order[transformIndex];
    if (transforms[name] && !transforms[name].defaulted) paxEncodeTransform(writer, transforms[name], name, layer.sourceDuration, layer.duration);
  }
  writer.i32(PAX_END_OF_LAYER, "layer terminator");
}

function paxEncode(decoded) {
  if (!decoded || typeof decoded !== "object") throw new Error("Invalid PAX JSON");
  if (!Number.isInteger(decoded.version) || decoded.version < 1 || decoded.version > 5) throw new Error("Unsupported PAX version");
  if (!Array.isArray(decoded.footages) || !Array.isArray(decoded.compositions)) throw new Error("Invalid PAX JSON collections");
  paxRequireCount(decoded.footages.length, "footage count");
  paxRequireCount(decoded.compositions.length, "composition count");
  var writer = paxWriter();
  writer.i32(decoded.version, "version");
  writer.i32(decoded.footages.length, "footage count");
  for (var footageIndex = 0; footageIndex < decoded.footages.length; ++footageIndex) {
    var footage = decoded.footages[footageIndex];
    if (!footage || !Number.isInteger(footage.id) || !Number.isInteger(footage.width) || !Number.isInteger(footage.height)) throw new Error("Invalid PAX footage " + footageIndex);
    writer.string(footage.shortName, "footage short name");
    writer.i32(footage.id, "footage ID");
    writer.string(footage.fullName, "footage full name");
    writer.i32(footage.width, "footage width");
    writer.i32(footage.height, "footage height");
  }
  for (var compositionIndex = 0; compositionIndex < decoded.compositions.length; ++compositionIndex) {
    var composition = decoded.compositions[compositionIndex];
    if (!composition || !Number.isInteger(composition.width) || !Number.isInteger(composition.height) || !Number.isInteger(composition.duration) || !Array.isArray(composition.layers)) {
      throw new Error("Invalid PAX composition " + compositionIndex);
    }
    if (composition.layerCount !== undefined && composition.layerCount !== composition.layers.length) throw new Error("PAX composition layer count does not match layers");
    paxRequireCount(composition.layers.length, "composition layer count");
    writer.i32(PAX_COMPOSITION_HEADER, "composition header");
    var compositionText = paxEncodedText(composition.name, composition.nameEncodedText);
    writer.stringSized(compositionText, composition.nameEncodedLength === undefined ? paxUtf8(compositionText).length : composition.nameEncodedLength, "composition name");
    writer.i32(composition.width, "composition width");
    writer.i32(composition.height, "composition height");
    writer.i32(composition.layers.length, "composition layer count");
    writer.i32(composition.duration, "composition duration");
    for (var layerIndex = 0; layerIndex < composition.layers.length; ++layerIndex) paxEncodeLayer(writer, composition.layers[layerIndex], decoded.version, composition.duration, layerIndex);
  }
  writer.i32(PAX_END_OF_POPFX, "PAX terminator");
  return writer.finish();
}

function paxDecode(bytes) {
  var reader = paxReader(bytes);
  var version = reader.i32("version");
  var footageCount = paxRequireCount(reader.i32("footage count"), "footage count");
  var footages = [];
  var footageById = {};
  for (var i = 0; i < footageCount; ++i) {
    var offset = reader.position();
    var shortNameLength = paxRequireCount(reader.i32("footage short name length"), "footage short name length");
    var shortName = reader.string(shortNameLength, "footage short name");
    var id = reader.i32("footage id");
    var fullNameLength = paxRequireCount(reader.i32("footage full name length"), "footage full name length");
    var fullName = reader.string(fullNameLength, "footage full name");
    var width = reader.i32("footage width");
    var height = reader.i32("footage height");
    var footage = { offset: offset, shortName: shortName, id: id, fullName: fullName, width: width, height: height };
    footages.push(footage);
    footageById[id] = footage;
  }

  var compositions = [];
  var terminator = null;
  while (reader.remaining() > 0) {
    var compositionOffset = reader.position();
    var header = reader.i32("composition header");
    if (header !== PAX_COMPOSITION_HEADER) {
      terminator = { offset: compositionOffset, tag: header, name: header === PAX_END_OF_POPFX ? "endOfPopFx" : "unknown" };
      break;
    }
    var nameLength = paxRequireCount(reader.i32("composition name length"), "composition name length");
    var nameEncodedText = reader.string(nameLength, "composition name");
    var name = paxDisplayText(nameEncodedText);
    var width = reader.i32("composition width");
    var height = reader.i32("composition height");
    var layerCount = paxRequireCount(reader.i32("composition layer count"), "composition layer count");
    var duration = reader.i32("composition duration");
    var composition = { offset: compositionOffset, name: name, nameEncodedText: nameEncodedText, nameEncodedLength: nameLength, width: width, height: height, layerCount: layerCount, duration: duration, layers: [] };
    for (var layerIndex = 0; layerIndex < layerCount; ++layerIndex) {
      var layer = paxReadLayer(reader, version, footageById, duration, layerIndex);
      paxAddDefaults(layer, width, height);
      composition.layers.push(layer);
    }
    compositions.push(composition);
  }
  if (reader.remaining() !== 0) throw new Error("Unexpected trailing PAX data at 0x" + reader.position().toString(16));

  var compositionByName = {};
  for (var compositionIndex = 0; compositionIndex < compositions.length; ++compositionIndex) {
    compositionByName[compositions[compositionIndex].name.toLowerCase()] = compositionIndex;
  }
  for (var parentIndex = 0; parentIndex < compositions.length; ++parentIndex) {
    var parent = compositions[parentIndex];
    for (var parentLayerIndex = 0; parentLayerIndex < parent.layers.length; ++parentLayerIndex) {
      var nestedLayer = parent.layers[parentLayerIndex];
      if (nestedLayer.enabled && nestedLayer.type === "composition") nestedLayer.compositionIndex = compositionByName[nestedLayer.name.toLowerCase()];
    }
  }
  return { format: "PopCap PAX", endianness: "little", version: version, footages: footages, compositions: compositions, terminator: terminator };
}

function paxDecodeFile(input, output) {
  if (!input || !kernelx.io.isFile(input)) return { success: false, error: "InputFile is required and must exist" };
  if (!output) return { success: false, error: "OutputFile is required" };
  try {
    var data = kernelx.io.mmapRead(input);
    if (!data || data.byteLength < 8) return { success: false, error: "Invalid or truncated PAX file" };
    var decoded = paxDecode(new Uint8Array(data));
    if (!kernelx.io.writeText(output, JSON.stringify(decoded, null, 2))) return { success: false, error: "Unable to write " + output };
    return { success: true, output: output, compositions: decoded.compositions.length, footages: decoded.footages.length };
  } catch (error) {
    return { success: false, error: error && error.message ? error.message : String(error) };
  }
}

function paxEncodeFile(input, output) {
  if (!input || !kernelx.io.isFile(input)) return { success: false, error: "InputFile is required and must exist" };
  if (!output) return { success: false, error: "OutputFile is required" };
  try {
    var text = kernelx.io.readText(input);
    var decoded = JSON.parse(text);
    var bytes = paxEncode(decoded);
    if (!kernelx.io.writeBytes(output, bytes)) return { success: false, error: "Unable to write " + output };
    return { success: true, output: output, compositions: decoded.compositions.length, footages: decoded.footages.length };
  } catch (error) {
    return { success: false, error: error && error.message ? error.message : String(error) };
  }
}

function paxIsJsonPath(path) {
  return typeof path === "string" && /\.json$/i.test(path);
}

function execute(params, id) {
  return id === "pax.encode" || (!id && paxIsJsonPath(params.InputFile))
    ? paxEncodeFile(params.InputFile, params.OutputFile)
    : paxDecodeFile(params.InputFile, params.OutputFile);
}
