var RSGP_MAGIC = 0x72736770;
var RSGP_HEADER_SIZE = 84;
var RSGP_ALIGNMENT = 0x1000;
var RSGP_GENERAL = 0;
var RSGP_TEXTURE = 1;

function rsgpError(message) { throw new Error("ResourceStreamGroup: " + message); }
function rsgpView(bytes) { return new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength); }
function rsgpU32(view, offset) { return view.getUint32(offset, true); }
function rsgpSetU32(view, offset, value) {
  if (!Number.isSafeInteger(value) || value < 0 || value > 0xffffffff) rsgpError("32-bit field is out of range");
  view.setUint32(offset, value, true);
}
function rsgpAlign(size) { return Math.ceil(size / RSGP_ALIGNMENT) * RSGP_ALIGNMENT; }
function rsgpPadding(size) { return (RSGP_ALIGNMENT - size % RSGP_ALIGNMENT) % RSGP_ALIGNMENT; }
function rsgpSlice(bytes, offset, size, description) {
  if (!Number.isSafeInteger(offset) || !Number.isSafeInteger(size) || offset < 0 || size < 0 || offset > bytes.length - size) rsgpError("invalid " + description + " range");
  return bytes.subarray(offset, offset + size);
}
function rsgpConcat(parts, size) {
  var output = new Uint8Array(size), offset = 0;
  for (var i = 0; i < parts.length; ++i) { output.set(parts[i], offset); offset += parts[i].length; }
  return output;
}
function rsgpUpper(bytes) {
  var output = new Uint8Array(bytes.length);
  for (var i = 0; i < bytes.length; ++i) output[i] = bytes[i] >= 97 && bytes[i] <= 122 ? bytes[i] - 32 : bytes[i];
  return output;
}
function rsgpCompare(a, b) {
  var size = Math.min(a.length, b.length);
  for (var i = 0; i < size; ++i) if (a[i] !== b[i]) return a[i] - b[i];
  return a.length - b.length;
}
function rsgpCommonSize(a, b) {
  var size = Math.min(a.length, b.length), index = 0;
  while (index < size && a[index] === b[index]) ++index;
  return index;
}
function rsgpUtf8Encode(value) {
  var output = [];
  for (var i = 0; i < value.length; ++i) {
    var code = value.charCodeAt(i);
    if (code >= 0xd800 && code <= 0xdbff && i + 1 < value.length) {
      var low = value.charCodeAt(i + 1);
      if (low >= 0xdc00 && low <= 0xdfff) { code = 0x10000 + ((code - 0xd800) << 10) + low - 0xdc00; ++i; }
    }
    if (code < 0x80) output.push(code);
    else if (code < 0x800) output.push(0xc0 | code >> 6, 0x80 | code & 0x3f);
    else if (code < 0x10000) output.push(0xe0 | code >> 12, 0x80 | code >> 6 & 0x3f, 0x80 | code & 0x3f);
    else output.push(0xf0 | code >> 18, 0x80 | code >> 12 & 0x3f, 0x80 | code >> 6 & 0x3f, 0x80 | code & 0x3f);
  }
  return new Uint8Array(output);
}
function rsgpUtf8Decode(bytes) {
  var output = "";
  for (var i = 0; i < bytes.length;) {
    var a = bytes[i++], code;
    if (a < 0x80) code = a;
    else if (a >= 0xc2 && a <= 0xdf && i < bytes.length) code = (a & 0x1f) << 6 | bytes[i++] & 0x3f;
    else if (a >= 0xe0 && a <= 0xef && i + 1 < bytes.length) { code = (a & 0x0f) << 12 | (bytes[i++] & 0x3f) << 6 | bytes[i++] & 0x3f; }
    else if (a >= 0xf0 && a <= 0xf4 && i + 2 < bytes.length) { code = (a & 0x07) << 18 | (bytes[i++] & 0x3f) << 12 | (bytes[i++] & 0x3f) << 6 | bytes[i++] & 0x3f; }
    else rsgpError("invalid UTF-8 resource path");
    if (code > 0xffff) { code -= 0x10000; output += String.fromCharCode(0xd800 + (code >> 10), 0xdc00 + (code & 0x3ff)); }
    else output += String.fromCharCode(code);
  }
  return output;
}
function rsgpPathKey(path) {
  if (typeof path !== "string" || !path.length || path.charAt(0) === "/" || path.charAt(0) === "\\" || /^[A-Za-z]:/.test(path)) rsgpError("resource path must be relative");
  var normalized = path.replace(/\\/g, "/");
  if (normalized.split("/").some(function (part) { return !part || part === "." || part === ".."; })) rsgpError("invalid resource path: " + path);
  return { path: normalized, key: rsgpUpper(rsgpUtf8Encode(normalized.replace(/\//g, "\\"))) };
}
function rsgpNumber(value, description) {
  value = Number(value);
  if (!Number.isSafeInteger(value) || value < 0 || value > 0xffffffff) rsgpError("invalid " + description);
  return value;
}
function rsgpCompression(value) {
  if (!value || typeof value !== "object") rsgpError("definition compression is missing");
  return { general: value.general === true, texture: value.texture === true };
}
function rsgpAdditional(resource, index) {
  var additional = resource && resource.additional;
  if (!additional || typeof additional !== "object") rsgpError("resource " + index + " has no additional data");
  var type = rsgpNumber(additional.type, "resource " + index + " type"), value = additional.value;
  if (type === RSGP_GENERAL) return { type: type };
  if (type !== RSGP_TEXTURE || !value || typeof value !== "object" || !Array.isArray(value.size) || value.size.length !== 2) rsgpError("invalid texture resource " + index);
  return { type: type, index: rsgpNumber(value.index, "texture index"), width: rsgpNumber(value.size[0], "texture width"), height: rsgpNumber(value.size[1], "texture height") };
}

function rsgpDecodeMap(bytes) {
  if (bytes.length % 4) rsgpError("resource information section is not word aligned");
  var view = rsgpView(bytes), offset = 0, parent = new Array(bytes.length / 4), resources = [];
  while (offset < bytes.length) {
    var begin = offset, prefix = parent[begin / 4] || [], key = prefix.slice(), childOffsets = [];
    for (;;) {
      if (offset + 4 > bytes.length) rsgpError("truncated resource path");
      var composite = rsgpU32(view, offset), character = composite & 0xff, child = composite >>> 8;
      offset += 4;
      if (character === 0) break;
      if (child) childOffsets.push({ offset: child, prefix: key.slice() });
      key.push(character);
    }
    for (var c = 0; c < childOffsets.length; ++c) {
      if (childOffsets[c].offset >= parent.length || parent[childOffsets[c].offset]) rsgpError("invalid resource path tree pointer");
      parent[childOffsets[c].offset] = childOffsets[c].prefix;
    }
    if (offset + 12 > bytes.length) rsgpError("truncated resource information");
    var type = rsgpU32(view, offset), resource = { path: rsgpUtf8Decode(new Uint8Array(key)), type: type, offset: rsgpU32(view, offset + 4), size: rsgpU32(view, offset + 8) };
    offset += 12;
    if (type === RSGP_GENERAL) resource.additional = { type: type, value: {} };
    else if (type === RSGP_TEXTURE) {
      if (offset + 20 > bytes.length) rsgpError("truncated texture information");
      if (rsgpU32(view, offset + 4) !== 0 || rsgpU32(view, offset + 8) !== 0) rsgpError("unsupported texture information");
      resource.additional = { type: type, value: { index: rsgpU32(view, offset), size: [rsgpU32(view, offset + 12), rsgpU32(view, offset + 16)] } };
      offset += 20;
    } else rsgpError("unknown resource type");
    resources.push(resource);
  }
  return resources;
}

function rsgpEncodeMap(resources) {
  var items = [], seen = Object.create(null);
  for (var i = 0; i < resources.length; ++i) {
    var info = rsgpPathKey(resources[i].path), additional = rsgpAdditional(resources[i], i), signature = Array.prototype.join.call(info.key, ",");
    if (seen[signature]) rsgpError("duplicate resource path after upper-casing: " + resources[i].path);
    seen[signature] = true;
    items.push({ path: info.path, key: info.key, additional: additional, data: resources[i].data, offset: resources[i].offset, size: resources[i].size });
  }
  items.sort(function (a, b) { return -rsgpCompare(a.key, b.key); });
  var options = new Array(items.length), words = [];
  if (items.length) options[0] = { inherit: 0, parent: 0 };
  for (i = 0; i < items.length; ++i) {
    var current = options[i];
    if (!current) rsgpError("invalid resource path ordering");
    var hasChild = Object.create(null);
    for (var j = i + 1; j < items.length; ++j) if (!options[j]) {
      var common = rsgpCommonSize(items[i].key, items[j].key);
      if (!hasChild[common] && common >= current.inherit) { hasChild[common] = true; options[j] = { inherit: common, parent: words.length + common - current.inherit }; }
    }
    if (i !== 0) {
      var pointer = words.length >>> 0;
      if (current.parent >= words.length || pointer > 0xffffff) rsgpError("resource path tree is too large");
      words[current.parent] = (words[current.parent] & 0xff) | pointer << 8;
    }
    for (j = current.inherit; j < items[i].key.length; ++j) words.push(items[i].key[j]);
    words.push(0);
    words.push(items[i].additional.type, items[i].offset, items[i].size);
    if (items[i].additional.type === RSGP_TEXTURE) words.push(items[i].additional.index, 0, 0, items[i].additional.width, items[i].additional.height);
  }
  var output = new Uint8Array(words.length * 4), view = rsgpView(output);
  for (i = 0; i < words.length; ++i) rsgpSetU32(view, i * 4, words[i] >>> 0);
  return { bytes: output, items: items };
}

function rsgpDecode(input, version) {
  if (input.length < 8 + RSGP_HEADER_SIZE) rsgpError("file is truncated");
  var view = rsgpView(input);
  if (rsgpU32(view, 0) !== RSGP_MAGIC || rsgpU32(view, 4) !== version) rsgpError("magic marker or version does not match");
  var base = 8, unknown = rsgpU32(view, base), compression = rsgpU32(view, base + 8), informationSize = rsgpU32(view, base + 12);
  if (version === 1 && unknown !== 1 || version !== 1 && unknown !== 0) rsgpError("unexpected header value");
  if (informationSize < 8 + RSGP_HEADER_SIZE || informationSize > input.length) rsgpError("invalid information section size");
  var generalOffset = rsgpU32(view, base + 16), generalSize = rsgpU32(view, base + 20), generalOriginal = rsgpU32(view, base + 24);
  var textureOffset = rsgpU32(view, base + 32), textureSize = rsgpU32(view, base + 36), textureOriginal = rsgpU32(view, base + 40);
  var mapSize = rsgpU32(view, base + 64), mapOffset = rsgpU32(view, base + 68);
  var map = rsgpDecodeMap(rsgpSlice(input, mapOffset, mapSize, "resource information"));
  var generalCompressed = (compression & 2) !== 0, textureCompressed = (compression & 1) !== 0;
  var general = generalCompressed ? kernelx.zlib.decompress(rsgpSlice(input, generalOffset, generalSize, "general data"), generalOriginal) : rsgpSlice(input, generalOffset, generalSize, "general data");
  var texture = textureCompressed && textureOriginal !== 0 ? kernelx.zlib.decompress(rsgpSlice(input, textureOffset, textureSize, "texture data"), textureOriginal) : rsgpSlice(input, textureOffset, textureSize, "texture data");
  if (!general || !texture) rsgpError("zlib decompression failed");
  general = new Uint8Array(general); texture = new Uint8Array(texture);
  if (general.length !== generalOriginal || texture.length !== textureOriginal) rsgpError("decompressed size does not match header");
  return { compression: { general: generalCompressed, texture: textureCompressed }, resources: map, general: general, texture: texture };
}

function rsgpEncode(definition, resources, version) {
  if (!definition || !Array.isArray(definition.resource) || !Array.isArray(resources) || resources.length !== definition.resource.length) rsgpError("invalid package definition");
  var compression = rsgpCompression(definition.compression);
  for (var i = 0; i < resources.length; ++i) {
    resources[i] = { path: rsgpPathKey(definition.resource[i].path).path, additional: definition.resource[i].additional, data: new Uint8Array(resources[i]) };
  }
  var groups = [[], []];
  for (i = 0; i < resources.length; ++i) groups[rsgpAdditional(resources[i], i).type].push(resources[i]);
  var sections = [];
  for (var type = 0; type < 2; ++type) {
    var parts = [], length = 0;
    for (i = 0; i < groups[type].length; ++i) { parts.push(groups[type][i].data); length += groups[type][i].data.length; var padding = rsgpPadding(length); if (padding) { parts.push(new Uint8Array(padding)); length += padding; } }
    var raw = rsgpConcat(parts, length), useCompression = type === RSGP_GENERAL ? compression.general : compression.texture;
    var stored = useCompression && !(type === RSGP_TEXTURE && !raw.length) ? kernelx.zlib.compress(raw, 9) : raw;
    if (!stored) rsgpError("zlib compression failed");
    sections[type] = { raw: raw, stored: new Uint8Array(stored), compressed: useCompression && !(type === RSGP_TEXTURE && !raw.length) };
  }
  for (type = 0; type < 2; ++type) {
    var position = 0;
    for (i = 0; i < groups[type].length; ++i) { groups[type][i].offset = position; groups[type][i].size = groups[type][i].data.length; position += groups[type][i].data.length + rsgpPadding(position + groups[type][i].data.length); }
  }
  var map = rsgpEncodeMap(resources), resourceInformationOffset = 8 + RSGP_HEADER_SIZE;
  var informationSize = rsgpAlign(resourceInformationOffset + map.bytes.length), sectionOffset = informationSize;
  var generalOffset = sectionOffset, generalSize = sections[0].stored.length, textureOffset = generalOffset + generalSize + (sections[0].compressed ? rsgpPadding(generalOffset + generalSize) : 0);
  var textureSize = sections[1].stored.length, total = textureOffset + textureSize + (sections[1].compressed ? rsgpPadding(textureOffset + textureSize) : 0);
  var outputBytes = new Uint8Array(total), view = rsgpView(outputBytes), base = 8;
  rsgpSetU32(view, 0, RSGP_MAGIC); rsgpSetU32(view, 4, version); rsgpSetU32(view, base, version === 1 ? 1 : 0);
  rsgpSetU32(view, base + 8, (compression.general ? 2 : 0) | (compression.texture ? 1 : 0)); rsgpSetU32(view, base + 12, informationSize);
  rsgpSetU32(view, base + 16, generalOffset); rsgpSetU32(view, base + 20, generalSize); rsgpSetU32(view, base + 24, sections[0].raw.length);
  rsgpSetU32(view, base + 32, textureOffset); rsgpSetU32(view, base + 36, textureSize); rsgpSetU32(view, base + 40, sections[1].raw.length);
  rsgpSetU32(view, base + 64, map.bytes.length); rsgpSetU32(view, base + 68, resourceInformationOffset);
  outputBytes.set(map.bytes, resourceInformationOffset); outputBytes.set(sections[0].stored, generalOffset); outputBytes.set(sections[1].stored, textureOffset);
  return outputBytes;
}

function rsgpPack(folder, output, version) {
  var definitionPath = kernelx.path.join(folder, "definition.json"), resourceFolder = kernelx.path.join(folder, "resource");
  if (!kernelx.io.isFile(definitionPath) || !kernelx.io.isDir(resourceFolder)) rsgpError("InputFolder must contain definition.json and resource/");
  var definition;
  try { definition = JSON.parse(kernelx.io.readText(definitionPath)); } catch (error) { rsgpError("invalid definition.json"); }
  if (!definition || !Array.isArray(definition.resource)) rsgpError("definition resource must be an array");
  var resources = [];
  for (var i = 0; i < definition.resource.length; ++i) {
    var path = rsgpPathKey(definition.resource[i].path).path, fullPath = kernelx.path.join(resourceFolder, path);
    if (!kernelx.io.isFile(fullPath)) rsgpError("resource does not exist: " + path);
    resources.push(new Uint8Array(kernelx.io.readBytes(fullPath)));
  }
  var outputBytes = rsgpEncode(definition, resources, version);
  if (!kernelx.io.writeBytes(output, outputBytes)) rsgpError("unable to write " + output);
}

function rsgpUnpack(inputPath, folder, version) {
  if (!kernelx.io.isFile(inputPath)) rsgpError("InputFile must exist");
  var package = rsgpDecode(new Uint8Array(kernelx.io.mmapRead(inputPath)), version), resourceFolder = kernelx.path.join(folder, "resource");
  if (!kernelx.io.mkdir(folder) || !kernelx.io.mkdir(resourceFolder)) rsgpError("unable to create output directory");
  for (var i = 0; i < package.resources.length; ++i) {
    var resource = package.resources[i], data = resource.type === RSGP_GENERAL ? package.general : package.texture;
    var outputPath = kernelx.path.join(resourceFolder, resource.path.replace(/\\/g, "/"));
    if (!kernelx.io.mkdir(kernelx.path.dir(outputPath))) rsgpError("unable to create resource directory: " + resource.path);
    if (!kernelx.io.writeBytes(outputPath, rsgpSlice(data, resource.offset, resource.size, "resource " + resource.path))) rsgpError("unable to write resource: " + resource.path);
    delete resource.type; delete resource.offset; delete resource.size;
  }
  if (!kernelx.io.writeText(kernelx.path.join(folder, "definition.json"), JSON.stringify({ compression: package.compression, resource: package.resources }, null, 2))) rsgpError("unable to write definition.json");
}

var ResourceStreamGroupCore = {
  decode: rsgpDecode,
  encode: rsgpEncode,
  encodeMap: rsgpEncodeMap,
  pack: rsgpPack,
  unpack: rsgpUnpack,
};
