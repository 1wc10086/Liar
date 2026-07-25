/* kernelx-manifest
[
  {
    "id": "newton.encode",
    "implementation": "implementation",
    "buffer_size": "64m",
    "params": [
      { "name": "InputFile", "type": "path", "required": true, "extensions": [".json", ".JSON"] },
      { "name": "OutputFile", "type": "path", "required": true, "extensions": [".newton", ".NEWTON"] }
    ]
  },
  {
    "id": "newton.decode",
    "implementation": "implementation",
    "buffer_size": "64m",
    "params": [
      { "name": "InputFile", "type": "path", "required": true, "extensions": [".newton", ".NEWTON"] },
      { "name": "OutputFile", "type": "path", "required": true, "extensions": [".json", ".JSON"] }
    ]
  }
]
*/

var NEWTON_GROUP_TYPES = { composite: 1, simple: 2 };
var NEWTON_RESOURCE_TYPES = { Image: 1, PopAnim: 2, SoundBank: 3, File: 4, PrimeFont: 5, RenderEffect: 6, DecodedSoundBank: 7 };
var NEWTON_RESOURCE_TYPE_NAMES = [null, "Image", "PopAnim", "SoundBank", "File", "PrimeFont", "RenderEffect", "DecodedSoundBank"];

function newtonError(message) { throw new Error("NEWTON: " + message); }
function newtonIsObject(value) { return value !== null && typeof value === "object" && !Array.isArray(value); }
function newtonInteger(value, name) {
  if (typeof value !== "number" || !isFinite(value) || Math.floor(value) !== value || value < -2147483648 || value > 2147483647) newtonError(name + " must be a signed 32-bit integer");
  return value;
}
function newtonOptionalInteger(value, fallback, name) { return value === undefined ? fallback : newtonInteger(value, name); }
function newtonString(value, name) { if (typeof value !== "string") newtonError(name + " must be a string"); return value; }
function newtonPath(value) {
  if (typeof value === "string") return value;
  if (!Array.isArray(value)) newtonError("resource.path must be a string or string array");
  for (var index = 0; index < value.length; ++index) newtonString(value[index], "resource.path[" + index + "]");
  return value.join("\\");
}

function newtonUtf8(value) {
  var output = [], index = 0;
  while (index < value.length) {
    var code = value.charCodeAt(index++);
    if (code >= 0xd800 && code <= 0xdbff) {
      var low = value.charCodeAt(index);
      if (low >= 0xdc00 && low <= 0xdfff) { code = 0x10000 + ((code - 0xd800) << 10) + low - 0xdc00; ++index; }
      else code = 0xfffd;
    } else if (code >= 0xdc00 && code <= 0xdfff) code = 0xfffd;
    if (code < 0x80) output.push(code);
    else if (code < 0x800) output.push(0xc0 | (code >> 6), 0x80 | (code & 0x3f));
    else if (code < 0x10000) output.push(0xe0 | (code >> 12), 0x80 | ((code >> 6) & 0x3f), 0x80 | (code & 0x3f));
    else output.push(0xf0 | (code >> 18), 0x80 | ((code >> 12) & 0x3f), 0x80 | ((code >> 6) & 0x3f), 0x80 | (code & 0x3f));
  }
  return new Uint8Array(output);
}

function newtonDecodeUtf8(bytes) {
  var output = "", index = 0;
  while (index < bytes.length) {
    var first = bytes[index++], code, extra;
    if (first < 0x80) { output += String.fromCharCode(first); continue; }
    if (first >= 0xc2 && first <= 0xdf) { code = first & 0x1f; extra = 1; }
    else if (first >= 0xe0 && first <= 0xef) { code = first & 0x0f; extra = 2; }
    else if (first >= 0xf0 && first <= 0xf4) { code = first & 0x07; extra = 3; }
    else newtonError("invalid UTF-8 string");
    if (index + extra > bytes.length) newtonError("truncated UTF-8 string");
    for (var part = 0; part < extra; ++part) {
      var next = bytes[index++];
      if ((next & 0xc0) !== 0x80) newtonError("invalid UTF-8 string");
      code = (code << 6) | (next & 0x3f);
    }
    if (code < (extra === 1 ? 0x80 : extra === 2 ? 0x800 : 0x10000) || code > 0x10ffff || (code >= 0xd800 && code <= 0xdfff)) newtonError("invalid UTF-8 string");
    if (code < 0x10000) output += String.fromCharCode(code);
    else { code -= 0x10000; output += String.fromCharCode(0xd800 | (code >> 10), 0xdc00 | (code & 0x3ff)); }
  }
  return output;
}

function newtonWriter() { return { chunks: [], length: 0 }; }
function newtonWrite(writer, bytes) { writer.chunks.push(bytes); writer.length += bytes.length; }
function newtonWriteU8(writer, value) { newtonWrite(writer, new Uint8Array([value])); }
function newtonWriteI32(writer, value) { var bytes = new Uint8Array(4); new DataView(bytes.buffer).setInt32(0, value, false); newtonWrite(writer, bytes); }
function newtonWriteBoolean(writer, value) { newtonWriteU8(writer, value ? 1 : 0); }
function newtonWriteString(writer, value) {
  var bytes = newtonUtf8(value);
  newtonWriteI32(writer, newtonInteger(value.length, "string length"));
  newtonWrite(writer, bytes);
}
function newtonFinish(writer) {
  var output = new Uint8Array(writer.length), offset = 0;
  for (var index = 0; index < writer.chunks.length; ++index) { output.set(writer.chunks[index], offset); offset += writer.chunks[index].length; }
  return output;
}

function newtonReader(bytes) { return { bytes: bytes, offset: 0 }; }
function newtonRequire(reader, count) { if (count < 0 || count > reader.bytes.length - reader.offset) newtonError("unexpected end of file"); }
function newtonReadU8(reader) { newtonRequire(reader, 1); return reader.bytes[reader.offset++]; }
function newtonReadI32(reader) { newtonRequire(reader, 4); var value = new DataView(reader.bytes.buffer, reader.bytes.byteOffset + reader.offset, 4).getInt32(0, false); reader.offset += 4; return value; }
function newtonReadBoolean(reader) { var value = newtonReadU8(reader); if (value !== 0 && value !== 1) newtonError("invalid boolean integer"); return value === 1; }
function newtonReadString(reader) { var length = newtonReadI32(reader); if (length < 0) newtonError("negative string length"); newtonRequire(reader, length); var bytes = reader.bytes.subarray(reader.offset, reader.offset + length); reader.offset += length; return newtonDecodeUtf8(bytes); }
function newtonReadCount(reader, name) { var value = newtonReadI32(reader); if (value < 0) newtonError("negative " + name); return value; }

function newtonEncode(definition) {
  if (!newtonIsObject(definition) || !Array.isArray(definition.groups)) newtonError("definition.groups must be an array");
  var writer = newtonWriter();
  newtonWriteI32(writer, newtonInteger(definition.slot_count, "slot_count"));
  newtonWriteI32(writer, newtonInteger(definition.groups.length, "groups length"));
  for (var groupIndex = 0; groupIndex < definition.groups.length; ++groupIndex) {
    var group = definition.groups[groupIndex];
    if (!newtonIsObject(group)) newtonError("group[" + groupIndex + "] must be an object");
    var type = NEWTON_GROUP_TYPES[group.type];
    if (!type) newtonError("unknown group type " + group.type);
    var subgroups = group.subgroups === undefined ? [] : group.subgroups;
    var resources = group.resources === undefined ? [] : group.resources;
    if (!Array.isArray(subgroups) || !Array.isArray(resources)) newtonError("group subgroups and resources must be arrays");
    newtonWriteU8(writer, type);
    newtonWriteI32(writer, newtonOptionalInteger(group.res, 0, "group.res"));
    newtonWriteI32(writer, newtonInteger(subgroups.length, "subgroups length"));
    newtonWriteI32(writer, newtonInteger(resources.length, "resources length"));
    newtonWriteBoolean(writer, true);
    newtonWriteBoolean(writer, group.parent !== undefined);
    newtonWriteString(writer, newtonString(group.id, "group.id"));
    if (group.parent !== undefined) newtonWriteString(writer, newtonString(group.parent, "group.parent"));
    for (var subgroupIndex = 0; subgroupIndex < subgroups.length; ++subgroupIndex) {
      var subgroup = subgroups[subgroupIndex];
      if (!newtonIsObject(subgroup)) newtonError("subgroup must be an object");
      newtonWriteI32(writer, newtonOptionalInteger(subgroup.res, 0, "subgroup.res"));
      newtonWriteString(writer, newtonString(subgroup.id, "subgroup.id"));
    }
    for (var resourceIndex = 0; resourceIndex < resources.length; ++resourceIndex) {
      var resource = resources[resourceIndex];
      if (!newtonIsObject(resource)) newtonError("resource must be an object");
      var resourceType = NEWTON_RESOURCE_TYPES[resource.type];
      if (!resourceType) newtonError("unknown resource type " + resource.type);
      var hasParent = resource.parent !== undefined;
      newtonWriteU8(writer, resourceType);
      newtonWriteI32(writer, newtonInteger(resource.slot, "resource.slot"));
      newtonWriteI32(writer, newtonOptionalInteger(resource.width, 0, "resource.width"));
      newtonWriteI32(writer, newtonOptionalInteger(resource.height, 0, "resource.height"));
      newtonWriteI32(writer, newtonOptionalInteger(resource.x, 0, "resource.x"));
      newtonWriteI32(writer, resource.type === "Image" && hasParent ? newtonOptionalInteger(resource.y, 0, "resource.y") : 0x7fffffff);
      newtonWriteI32(writer, newtonOptionalInteger(resource.ax, 0, "resource.ax"));
      newtonWriteI32(writer, newtonOptionalInteger(resource.ay, 0, "resource.ay"));
      newtonWriteI32(writer, newtonOptionalInteger(resource.aw, 0, "resource.aw"));
      newtonWriteI32(writer, newtonOptionalInteger(resource.ah, 0, "resource.ah"));
      newtonWriteI32(writer, newtonOptionalInteger(resource.cols, 1, "resource.cols"));
      newtonWriteI32(writer, newtonOptionalInteger(resource.rows, 1, "resource.rows"));
      newtonWriteBoolean(writer, resource.atlas === undefined ? false : !!resource.atlas);
      newtonWriteBoolean(writer, true);
      newtonWriteBoolean(writer, true);
      newtonWriteBoolean(writer, hasParent);
      newtonWriteString(writer, newtonString(resource.id, "resource.id"));
      newtonWriteString(writer, newtonPath(resource.path));
      if (hasParent) newtonWriteString(writer, newtonString(resource.parent, "resource.parent"));
    }
  }
  return newtonFinish(writer);
}

function newtonDecode(bytes) {
  var reader = newtonReader(bytes), definition = { slot_count: newtonReadI32(reader), groups: [] };
  var groupCount = newtonReadCount(reader, "group count");
  for (var groupIndex = 0; groupIndex < groupCount; ++groupIndex) {
    var groupType = newtonReadU8(reader);
    if (groupType !== 1 && groupType !== 2) newtonError("unknown group type index " + groupType);
    var res = newtonReadI32(reader), subgroupCount = newtonReadCount(reader, "subgroup count"), resourceCount = newtonReadCount(reader, "resource count");
    if (!newtonReadBoolean(reader)) newtonError("invalid group fixed flag");
    var hasParent = newtonReadBoolean(reader), group = { id: newtonReadString(reader), type: groupType === 1 ? "composite" : "simple" };
    var parent = hasParent ? newtonReadString(reader) : undefined;
    var subgroups = [];
    for (var subgroupIndex = 0; subgroupIndex < subgroupCount; ++subgroupIndex) { var subgroupRes = newtonReadI32(reader), subgroup = { id: newtonReadString(reader) }; if (subgroupRes !== 0) subgroup.res = subgroupRes; subgroups.push(subgroup); }
    var resources = [];
    for (var resourceIndex = 0; resourceIndex < resourceCount; ++resourceIndex) {
      var resourceType = newtonReadU8(reader), typeName = NEWTON_RESOURCE_TYPE_NAMES[resourceType];
      if (!typeName) newtonError("unknown resource type index " + resourceType);
      var resource = { type: typeName, slot: newtonReadI32(reader), width: newtonReadI32(reader), height: newtonReadI32(reader), x: newtonReadI32(reader), y: newtonReadI32(reader), ax: newtonReadI32(reader), ay: newtonReadI32(reader), aw: newtonReadI32(reader), ah: newtonReadI32(reader), cols: newtonReadI32(reader), rows: newtonReadI32(reader) };
      var atlas = newtonReadBoolean(reader);
      if (!newtonReadBoolean(reader) || !newtonReadBoolean(reader)) newtonError("invalid resource fixed flag");
      var resourceHasParent = newtonReadBoolean(reader);
      resource.id = newtonReadString(reader); resource.path = newtonReadString(reader);
      if (resourceHasParent) resource.parent = newtonReadString(reader);
      if (typeName !== "Image") resource = { type: resource.type, slot: resource.slot, id: resource.id, path: resource.path };
      else if (resourceHasParent) resource = { type: resource.type, slot: resource.slot, id: resource.id, path: resource.path, parent: resource.parent, ax: resource.ax, ay: resource.ay, aw: resource.aw, ah: resource.ah, x: resource.x, y: resource.y, cols: resource.cols, rows: resource.rows };
      else resource = { type: resource.type, slot: resource.slot, id: resource.id, path: resource.path, atlas: atlas, width: resource.width, height: resource.height };
      resources.push(resource);
    }
    if (groupType === 1) { if (resourceCount !== 0) newtonError("composite group contains resources"); group.subgroups = subgroups; }
    else { if (subgroupCount !== 0) newtonError("simple group contains subgroups"); if (res !== 0) group.res = res; if (parent !== undefined) group.parent = parent; group.resources = resources; }
    definition.groups.push(group);
  }
  return definition;
}

function execute(params, id) {
  var input = params.InputFile, output = params.OutputFile;
  if (!input || !kernelx.io.isFile(input)) return { success: false, error: "InputFile is required and must exist" };
  if (!output) return { success: false, error: "OutputFile is required" };
  try {
    if (id === "newton.encode") {
      var source = JSON.parse(kernelx.io.readText(input));
      if (!kernelx.io.writeBytes(output, newtonEncode(source))) newtonError("unable to write " + output);
    } else {
      var bytes = kernelx.io.readBytes(input);
      if (!bytes.length) newtonError("input is empty or cannot be read");
      if (!kernelx.io.writeText(output, JSON.stringify(newtonDecode(bytes), null, 2))) newtonError("unable to write " + output);
    }
    return { success: true, output: output };
  } catch (error) { return { success: false, error: error && error.message ? error.message : String(error) }; }
}
