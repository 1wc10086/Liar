/* kernelx-manifest
[
  {
    "id": "curve_dat.decode",
    "implementation": "implementation",
    "params": [
      { "name": "InputFile", "type": "path", "required": true, "extensions": [".dat", ".DAT"] },
      { "name": "OutputFile", "type": "path", "required": true, "extensions": [".json"] }
    ]
  },
  {
    "id": "curve_dat.encode",
    "implementation": "implementation",
    "params": [
      { "name": "InputFile", "type": "path", "required": true, "extensions": [".json"] },
      { "name": "OutputFile", "type": "path", "required": true, "extensions": [".dat", ".DAT"] }
    ]
  }
]
*/

// PopCap Zuma's Revenge CURV v1-v15.
var CURV_MAGIC = "CURV";
var CURV_MAX_VERSION = 15;
var CURV_MAX_EDITOR_BYTES = 1000000;
var CURV_MAX_POINTS = 10000000;
var CURV_POWER_UPS = [
  "proximityBomb", "slowDown", "accuracy", "moveBackwards", "lob", "bombBullet", "ballEater",
  "cannon", "colorNuke", "laser", "fireball", "shieldFrog", "freezeBoss", "gauntletMultBall"
];
var CURV_DEPRECATED_POWER_UPS = { 4: true, 5: true, 6: true, 10: true, 11: true, 12: true };

function curvHex(bytes) {
  var hex = "";
  for (var i = 0; i < bytes.length; ++i) {
    var value = bytes[i].toString(16);
    hex += value.length === 1 ? "0" + value : value;
  }
  return hex;
}

function curvReader(bytes) {
  var view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
  var position = 0;

  function requireBytes(size, field) {
    if (size < 0 || size > bytes.length - position) {
      throw new Error("Truncated CURV " + field + " at 0x" + position.toString(16));
    }
  }

  return {
    position: function () { return position; },
    remaining: function () { return bytes.length - position; },
    bytes: function (size, field) {
      requireBytes(size, field || "bytes");
      var value = bytes.subarray(position, position + size);
      position += size;
      return value;
    },
    u8: function (field) {
      requireBytes(1, field || "u8");
      return bytes[position++];
    },
    i8: function (field) {
      var value = this.u8(field || "i8");
      return value > 127 ? value - 256 : value;
    },
    boolean: function (field) { return this.u8(field || "boolean") !== 0; },
    u32: function (field) {
      requireBytes(4, field || "u32");
      var value = view.getUint32(position, true);
      position += 4;
      return value;
    },
    i32: function (field) {
      requireBytes(4, field || "i32");
      var value = view.getInt32(position, true);
      position += 4;
      return value;
    },
    f32: function (field) {
      requireBytes(4, field || "f32");
      var value = view.getFloat32(position, true);
      position += 4;
      return value;
    }
  };
}

function curvWriter() {
  var chunks = [];
  var length = 0;

  function push(bytes) {
    chunks.push(bytes);
    length += bytes.length;
  }

  function integer(value, minimum, maximum, field) {
    if (!Number.isInteger(value) || value < minimum || value > maximum) throw new Error("Invalid CURV " + field);
    return value;
  }

  function finite(value, field) {
    if (typeof value !== "number" || !Number.isFinite(value)) throw new Error("Invalid CURV " + field);
    return value;
  }

  return {
    bytes: function (value, field) {
      if (!(value instanceof Uint8Array)) throw new Error("Invalid CURV " + (field || "bytes"));
      push(value);
    },
    u8: function (value, field) { push(new Uint8Array([integer(value, 0, 255, field || "u8")])); },
    i8: function (value, field) { this.u8(integer(value, -128, 127, field || "i8") & 255, field || "i8"); },
    boolean: function (value) { this.u8(value ? 1 : 0, "boolean"); },
    u32: function (value, field) {
      integer(value, 0, 4294967295, field || "u32");
      var bytes = new Uint8Array(4);
      new DataView(bytes.buffer).setUint32(0, value, true);
      push(bytes);
    },
    i32: function (value, field) {
      integer(value, -2147483648, 2147483647, field || "i32");
      var bytes = new Uint8Array(4);
      new DataView(bytes.buffer).setInt32(0, value, true);
      push(bytes);
    },
    f32: function (value, field) {
      finite(value, field || "f32");
      var bytes = new Uint8Array(4);
      new DataView(bytes.buffer).setFloat32(0, value, true);
      push(bytes);
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

function curvHexBytes(value, field) {
  if (typeof value !== "string" || value.length % 2 !== 0 || !/^[0-9a-fA-F]*$/.test(value)) throw new Error("Invalid CURV " + field);
  var bytes = new Uint8Array(value.length / 2);
  for (var i = 0; i < bytes.length; ++i) bytes[i] = parseInt(value.substr(i * 2, 2), 16);
  return bytes;
}

function curvValue(value, fallback) {
  return value === undefined ? fallback : value;
}

function curvReadPowerUps(reader, version) {
  var count = reader.u32("power-up slot count");
  if (count > CURV_POWER_UPS.length) throw new Error("Unsupported CURV power-up slot count " + count);
  var powerUps = [];
  for (var i = 0; i < count; ++i) {
    var deprecated = CURV_DEPRECATED_POWER_UPS[i] === true;
    var entry = { index: i, type: CURV_POWER_UPS[i], deprecated: deprecated };
    if (deprecated) {
      entry.discardedFrequency = reader.i32("deprecated power-up frequency");
      if (version >= 12) entry.discardedMaximum = reader.i32("deprecated power-up maximum");
    } else {
      entry.frequency = reader.i32("power-up frequency");
      if (version >= 12) entry.maximum = reader.i32("power-up maximum");
    }
    powerUps.push(entry);
  }
  return powerUps;
}

function curvReadPoints(reader, version, attributesPresent) {
  var count = reader.u32("point count");
  if (count > CURV_MAX_POINTS) throw new Error("Invalid CURV point count " + count);
  var points = [];
  var x = 0;
  var y = 0;
  for (var i = 0; i < count; ++i) {
    var point = { index: i };
    if (version === 1) {
      point.encoding = "absolute";
      point.x = reader.f32("point x");
      point.y = reader.f32("point y");
      point.inTunnel = reader.boolean("point in-tunnel");
      point.priority = reader.u8("point priority");
    } else if (version < 4) {
      if (i === 0) {
        point.encoding = "absolute";
        point.x = reader.f32("first point x");
        point.y = reader.f32("first point y");
      } else {
        point.encoding = "delta";
        point.deltaX = reader.i8("point delta x");
        point.deltaY = reader.i8("point delta y");
        point.x = x + point.deltaX / 100;
        point.y = y + point.deltaY / 100;
      }
      if (attributesPresent) {
        point.inTunnel = reader.boolean("point in-tunnel");
        point.priority = reader.u8("point priority");
      }
    } else {
      var flags = reader.u8("point flags");
      point.flags = flags;
      point.inTunnel = (flags & 1) !== 0;
      var absolute = (flags & 2) !== 0;
      point.encoding = absolute ? "absolute" : "delta";
      if (attributesPresent || version >= 15) point.priority = reader.u8("point priority");
      if (absolute) {
        point.x = reader.f32("point x");
        point.y = reader.f32("point y");
      } else {
        point.deltaX = reader.i8("point delta x");
        point.deltaY = reader.i8("point delta y");
        point.x = x + point.deltaX / 100;
        point.y = y + point.deltaY / 100;
      }
    }
    x = point.x;
    y = point.y;
    points.push(point);
  }
  return points;
}

function curvDecode(bytes) {
  if (!(bytes instanceof Uint8Array) || bytes.length < 8) throw new Error("Invalid or truncated CURV file");
  var reader = curvReader(bytes);
  var headerOffset = reader.position();
  var magic = "";
  var magicBytes = reader.bytes(4, "magic");
  for (var i = 0; i < magicBytes.length; ++i) magic += String.fromCharCode(magicBytes[i]);
  if (magic !== CURV_MAGIC) throw new Error("Invalid CURV signature at 0x" + headerOffset.toString(16) + ": " + curvHex(magicBytes));
  var version = reader.i32("version");
  if (version < 1 || version > CURV_MAX_VERSION) throw new Error("Unsupported CURV version " + version);

  var result = { format: "PopCap CURV", endianness: "little", version: version, settings: {}, editor: null, points: [] };
  if (version >= 8) result.settings.linear = reader.boolean("linear");
  if (version >= 7) {
    var settings = result.settings;
    settings.startDistance = reader.i32("start distance");
    settings.numBalls = reader.i32("number of balls");
    settings.ballRepeat = reader.i32("ball repeat");
    settings.maxSingle = reader.i32("max single");
    settings.numColors = reader.i32("number of colors");
    if (version <= 10) {
      settings.obsoleteUnknownInteger = reader.i32("obsolete integer");
      settings.obsoleteUnknownFloat = reader.f32("obsolete float");
    }
    settings.speed = reader.f32("speed");
    settings.slowDistance = reader.i32("slow distance");
    settings.accelerationRate = reader.f32("acceleration rate");
    settings.maxSpeed = reader.f32("max speed");
    settings.scoreTarget = reader.i32("score target");
    settings.skullRotation = reader.i32("skull rotation");
    settings.zumaBack = reader.i32("zuma back");
    settings.zumaSlow = reader.i32("zuma slow");
    settings.slowFactor = version >= 13 ? reader.f32("slow factor") : 4;
    settings.maxClumpSize = version >= 14 ? reader.i32("max clump size") : 10;
    settings.powerUps = curvReadPowerUps(reader, version);
    settings.powerUpChance = version >= 12 ? reader.i32("power-up chance") : 0;
    settings.drawCurve = reader.boolean("draw curve");
    settings.drawTunnels = reader.boolean("draw tunnels");
    settings.destroyAll = reader.boolean("destroy all");
    settings.drawPit = version > 8 ? reader.boolean("draw pit") : true;
    settings.dieAtEnd = version > 9 ? reader.boolean("die at end") : true;
  }

  var noEditorData = false;
  var pointAttributesPresent = true;
  if (version >= 3) {
    noEditorData = reader.boolean("no editor data");
    pointAttributesPresent = reader.boolean("point attributes present");
  }
  if (!noEditorData) {
    var editType = reader.u32("edit type");
    var editorDataLength = reader.u32("editor data length");
    if (editorDataLength > CURV_MAX_EDITOR_BYTES) throw new Error("Invalid CURV editor data length " + editorDataLength);
    result.editor = { editType: editType, dataLength: editorDataLength, dataHex: curvHex(reader.bytes(editorDataLength, "editor data")) };
  }
  result.pointAttributesPresent = pointAttributesPresent;
  result.points = curvReadPoints(reader, version, pointAttributesPresent);
  if (reader.remaining() !== 0) throw new Error("Unexpected trailing CURV data at 0x" + reader.position().toString(16));
  return result;
}

function curvRequireObject(value, field) {
  if (!value || typeof value !== "object" || Array.isArray(value)) throw new Error("Invalid CURV " + field);
  return value;
}

function curvWritePowerUps(writer, settings, version) {
  var powerUps = settings.powerUps;
  if (!Array.isArray(powerUps) || powerUps.length > CURV_POWER_UPS.length) throw new Error("Invalid CURV powerUps");
  writer.u32(powerUps.length, "power-up slot count");
  for (var i = 0; i < powerUps.length; ++i) {
    var entry = curvRequireObject(powerUps[i], "power-up " + i);
    if (entry.index !== undefined && entry.index !== i) throw new Error("CURV power-up index does not match its slot");
    if (entry.type !== undefined && entry.type !== CURV_POWER_UPS[i]) throw new Error("CURV power-up type does not match its slot");
    var deprecated = CURV_DEPRECATED_POWER_UPS[i] === true;
    if (deprecated) {
      writer.i32(curvValue(entry.discardedFrequency, 0), "deprecated power-up frequency");
      if (version >= 12) writer.i32(curvValue(entry.discardedMaximum, 0), "deprecated power-up maximum");
    } else {
      writer.i32(curvValue(entry.frequency, 0), "power-up frequency");
      if (version >= 12) writer.i32(curvValue(entry.maximum, 100000000), "power-up maximum");
    }
  }
}

function curvCanReuseDelta(point, previousX, previousY) {
  if (!point || point.encoding !== "delta" || !Number.isInteger(point.deltaX) || !Number.isInteger(point.deltaY)) return false;
  return Math.abs(point.x - (previousX + point.deltaX / 100)) < 0.0000001 && Math.abs(point.y - (previousY + point.deltaY / 100)) < 0.0000001;
}

function curvWritePointAttributes(writer, point, required) {
  if (!required) return;
  writer.boolean(point.inTunnel === true);
  writer.u8(curvValue(point.priority, 0), "point priority");
}

function curvWritePoints(writer, decoded, version, attributesPresent) {
  var points = decoded.points;
  if (!Array.isArray(points) || points.length > CURV_MAX_POINTS) throw new Error("Invalid CURV points");
  writer.u32(points.length, "point count");
  var previousX = 0;
  var previousY = 0;
  for (var i = 0; i < points.length; ++i) {
    var point = curvRequireObject(points[i], "point " + i);
    if (point.index !== undefined && point.index !== i) throw new Error("CURV point index does not match its array position");
    if (typeof point.x !== "number" || !Number.isFinite(point.x) || typeof point.y !== "number" || !Number.isFinite(point.y)) throw new Error("Invalid CURV point coordinates " + i);
    if (version === 1) {
      writer.f32(point.x, "point x");
      writer.f32(point.y, "point y");
      curvWritePointAttributes(writer, point, true);
    } else if (version < 4) {
      if (i === 0) {
        writer.f32(point.x, "first point x");
        writer.f32(point.y, "first point y");
      } else {
        var canReuse = curvCanReuseDelta(point, previousX, previousY);
        var deltaX = canReuse ? point.deltaX : Math.round((point.x - previousX) * 100);
        var deltaY = canReuse ? point.deltaY : Math.round((point.y - previousY) * 100);
        if (deltaX < -128 || deltaX > 127 || deltaY < -128 || deltaY > 127) throw new Error("CURV v2-v3 point " + i + " cannot be encoded as a signed-byte delta");
        if (!canReuse && (Math.abs(point.x - (previousX + deltaX / 100)) >= 0.0000001 || Math.abs(point.y - (previousY + deltaY / 100)) >= 0.0000001)) {
          throw new Error("CURV v2-v3 point " + i + " is not aligned to the 0.01-unit delta grid");
        }
        writer.i8(deltaX, "point delta x");
        writer.i8(deltaY, "point delta y");
      }
      curvWritePointAttributes(writer, point, attributesPresent);
    } else {
      var useDelta = curvCanReuseDelta(point, previousX, previousY);
      if (!useDelta) {
        var candidateX = Math.round((point.x - previousX) * 100);
        var candidateY = Math.round((point.y - previousY) * 100);
        useDelta = candidateX >= -128 && candidateX <= 127 && candidateY >= -128 && candidateY <= 127 &&
          Math.abs(point.x - (previousX + candidateX / 100)) < 0.0000001 && Math.abs(point.y - (previousY + candidateY / 100)) < 0.0000001;
        if (useDelta) {
          point = { x: point.x, y: point.y, inTunnel: point.inTunnel, priority: point.priority, flags: point.flags, deltaX: candidateX, deltaY: candidateY };
        }
      }
      var preservedFlags = Number.isInteger(point.flags) ? point.flags : 0;
      if (preservedFlags < 0 || preservedFlags > 255) throw new Error("Invalid CURV point flags " + i);
      var flags = (preservedFlags & 252) | (point.inTunnel === true ? 1 : 0) | (useDelta ? 0 : 2);
      writer.u8(flags, "point flags");
      if (attributesPresent || version >= 15) writer.u8(curvValue(point.priority, 0), "point priority");
      if (useDelta) {
        writer.i8(point.deltaX, "point delta x");
        writer.i8(point.deltaY, "point delta y");
      } else {
        writer.f32(point.x, "point x");
        writer.f32(point.y, "point y");
      }
    }
    previousX = point.x;
    previousY = point.y;
  }
}

function curvEncode(decoded) {
  curvRequireObject(decoded, "JSON");
  var version = decoded.version;
  if (!Number.isInteger(version) || version < 1 || version > CURV_MAX_VERSION) throw new Error("Unsupported CURV version");
  var writer = curvWriter();
  writer.bytes(new Uint8Array([67, 85, 82, 86]), "magic");
  writer.i32(version, "version");
  var settings = curvRequireObject(decoded.settings, "settings");
  if (version >= 8) writer.boolean(settings.linear === true);
  if (version >= 7) {
    writer.i32(settings.startDistance, "start distance");
    writer.i32(settings.numBalls, "number of balls");
    writer.i32(settings.ballRepeat, "ball repeat");
    writer.i32(settings.maxSingle, "max single");
    writer.i32(settings.numColors, "number of colors");
    if (version <= 10) {
      writer.i32(settings.obsoleteUnknownInteger, "obsolete integer");
      writer.f32(settings.obsoleteUnknownFloat, "obsolete float");
    }
    writer.f32(settings.speed, "speed");
    writer.i32(settings.slowDistance, "slow distance");
    writer.f32(settings.accelerationRate, "acceleration rate");
    writer.f32(settings.maxSpeed, "max speed");
    writer.i32(settings.scoreTarget, "score target");
    writer.i32(settings.skullRotation, "skull rotation");
    writer.i32(settings.zumaBack, "zuma back");
    writer.i32(settings.zumaSlow, "zuma slow");
    if (version >= 13) writer.f32(settings.slowFactor, "slow factor");
    if (version >= 14) writer.i32(settings.maxClumpSize, "max clump size");
    curvWritePowerUps(writer, settings, version);
    if (version >= 12) writer.i32(settings.powerUpChance, "power-up chance");
    writer.boolean(settings.drawCurve === true);
    writer.boolean(settings.drawTunnels === true);
    writer.boolean(settings.destroyAll === true);
    if (version > 8) writer.boolean(settings.drawPit === true);
    if (version > 9) writer.boolean(settings.dieAtEnd === true);
  }

  var editor = decoded.editor;
  var attributesPresent = decoded.pointAttributesPresent !== false;
  if (version >= 3) {
    writer.boolean(editor === null || editor === undefined);
    writer.boolean(attributesPresent);
  }
  if (version < 3 || (editor !== null && editor !== undefined)) {
    editor = curvRequireObject(editor, "editor");
    var editorBytes = curvHexBytes(editor.dataHex, "editor dataHex");
    if (editorBytes.length > CURV_MAX_EDITOR_BYTES || (editor.dataLength !== undefined && editor.dataLength !== editorBytes.length)) throw new Error("Invalid CURV editor data length");
    writer.u32(editor.editType, "edit type");
    writer.u32(editorBytes.length, "editor data length");
    writer.bytes(editorBytes, "editor data");
  }
  curvWritePoints(writer, decoded, version, attributesPresent);
  return writer.finish();
}

function curvDecodeFile(input, output) {
  if (!input || !kernelx.io.isFile(input)) return { success: false, error: "InputFile is required and must exist" };
  if (!output) return { success: false, error: "OutputFile is required" };
  try {
    var data = kernelx.io.mmapRead(input);
    var decoded = curvDecode(new Uint8Array(data));
    if (!kernelx.io.writeText(output, JSON.stringify(decoded, null, 2))) return { success: false, error: "Unable to write " + output };
    return { success: true, output: output, version: decoded.version, points: decoded.points.length };
  } catch (error) {
    return { success: false, error: error && error.message ? error.message : String(error) };
  }
}

function curvEncodeFile(input, output) {
  if (!input || !kernelx.io.isFile(input)) return { success: false, error: "InputFile is required and must exist" };
  if (!output) return { success: false, error: "OutputFile is required" };
  try {
    var decoded = JSON.parse(kernelx.io.readText(input));
    var bytes = curvEncode(decoded);
    if (!kernelx.io.writeBytes(output, bytes)) return { success: false, error: "Unable to write " + output };
    return { success: true, output: output, version: decoded.version, points: decoded.points.length };
  } catch (error) {
    return { success: false, error: error && error.message ? error.message : String(error) };
  }
}

function curvIsJsonPath(path) {
  return typeof path === "string" && /\.json$/i.test(path);
}

function execute(params, id) {
  return id === "zuma_curve_dat.encode" || (!id && curvIsJsonPath(params.InputFile))
    ? curvEncodeFile(params.InputFile, params.OutputFile)
    : curvDecodeFile(params.InputFile, params.OutputFile);
}
