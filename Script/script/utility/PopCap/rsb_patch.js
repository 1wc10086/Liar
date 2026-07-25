/* kernelx-manifest
[
  {
    "id": "rsb_patch.encode",
    "implementation": "implementation",
    "buffer_size": "1024m",
    "params": [
      { "name": "AfterFile", "type": "path", "required": true, "extensions": [".rsb", ".RSB"] },
      { "name": "PatchFile", "type": "path", "required": true, "extensions": [".rsbpatch", ".RSBPATCH"] },
      { "name": "BeforeFile", "type": "path", "required": true, "extensions": [".rsb", ".RSB"] },
      { "name": "UseRawPacket", "type": "list", "default": "false", "list": ["false", "true"] },
      { "name": "Mode", "type": "list", "default": "encode", "list": ["encode"] }
    ]
  },
  {
    "id": "rsb_patch.decode",
    "implementation": "implementation",
    "buffer_size": "1024m",
    "params": [
      { "name": "PatchFile", "type": "path", "required": true, "extensions": [".rsbpatch", ".RSBPATCH"] },
      { "name": "AfterFile", "type": "path", "required": true, "extensions": [".rsb", ".RSB"] },
      { "name": "BeforeFile", "type": "path", "required": true, "extensions": [".rsb", ".RSB"] },
      { "name": "UseRawPacket", "type": "list", "default": "false", "list": ["false", "true"] },
      { "name": "Mode", "type": "list", "default": "decode", "list": ["decode"] }
    ]
  }
]
*/

var RSBP_MAGIC = 0x52534250;
var RSB_MAGIC = 0x72736231;
var RSG_MAGIC = 0x72736770;
var RSBP_VERSION = 1;
var RSB_HEADER_SIZE = 104;
var RSB_SUBGROUP_SIZE = 196;
var RSG_HEADER_SIZE = 84;
var RSBP_PACKAGE_SIZE = 40;
var RSBP_PACKET_SIZE = 152;
var RSB_ALIGNMENT = 0x1000;

function rsbpError(message) { throw new Error("RSB patch: " + message); }
function rsbpView(bytes) { return new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength); }
function rsbpU32(view, offset) { return view.getUint32(offset, true); }
function rsbpSetU32(view, offset, value) { view.setUint32(offset, value >>> 0, true); }
function rsbpAlign(size) { return (size + RSB_ALIGNMENT - 1) & ~(RSB_ALIGNMENT - 1); }
function rsbpPadding(size) { return (RSB_ALIGNMENT - (size & (RSB_ALIGNMENT - 1))) & (RSB_ALIGNMENT - 1); }
function rsbpEqual(a, b) {
  if (a.length !== b.length) return false;
  for (var i = 0; i < a.length; ++i) if (a[i] !== b[i]) return false;
  return true;
}
function rsbpConcat(parts, length) {
  var output = new Uint8Array(length), offset = 0;
  for (var i = 0; i < parts.length; ++i) { output.set(parts[i], offset); offset += parts[i].length; }
  return output;
}
function rsbpSlice(bytes, offset, size, what) {
  if (!Number.isSafeInteger(offset) || !Number.isSafeInteger(size) || offset < 0 || size < 0 || offset > bytes.length - size) rsbpError("invalid " + what + " range");
  return bytes.subarray(offset, offset + size);
}
function rsbpBool(value) { return value === true || value === 1 || value === "1" || String(value).toLowerCase() === "true"; }
function rsbpName(bytes, offset) {
  var end = offset;
  while (end < offset + 128 && bytes[end] !== 0) ++end;
  var value = "";
  for (var i = offset; i < end; ++i) value += String.fromCharCode(bytes[i]);
  return value;
}
function rsbpWriteName(bytes, offset, name) {
  if (name.length >= 128) rsbpError("subgroup identifier is too long: " + name);
  for (var i = 0; i < name.length; ++i) {
    var code = name.charCodeAt(i);
    if (code > 0x7f) rsbpError("subgroup identifier is not ASCII: " + name);
    bytes[offset + i] = code;
  }
}
function rsbpHash(bytes) {
  if (!kernelx.botan || !kernelx.botan.loaded()) rsbpError("MD5 support is unavailable");
  var hash = kernelx.botan.hash(bytes, "MD5");
  if (!hash || hash.byteLength !== 16) rsbpError("MD5 calculation failed");
  return new Uint8Array(hash);
}
function rsbpVcdiffEncode(before, after) {
  if (!kernelx.differentiation || !kernelx.differentiation.loaded()) rsbpError("VCDIFF support is unavailable");
  var patch = kernelx.differentiation.encode(before, after, 1, true);
  if (!patch) rsbpError("VCDIFF encoding failed");
  return new Uint8Array(patch);
}
function rsbpVcdiffDecode(before, patch, capacity) {
  if (!kernelx.differentiation || !kernelx.differentiation.loaded()) rsbpError("VCDIFF support is unavailable");
  if (!Number.isSafeInteger(capacity) || capacity < 1) rsbpError("invalid VCDIFF output capacity");
  var after = kernelx.differentiation.decode(before, patch, capacity, capacity, capacity, true);
  if (!after) rsbpError("VCDIFF decoding failed");
  return new Uint8Array(after);
}

function rsbpParseRsb(bytes, validatePackets) {
  if (bytes.length < 8 + RSB_HEADER_SIZE) rsbpError("RSB is truncated");
  var view = rsbpView(bytes);
  if (rsbpU32(view, 0) !== RSB_MAGIC || rsbpU32(view, 4) !== 4) rsbpError("RSB must use version 4");
  var header = 8;
  var informationSize = rsbpU32(view, header + 4);
  var subgroupCount = rsbpU32(view, header + 32);
  var subgroupOffset = rsbpU32(view, header + 36);
  var subgroupBlockSize = rsbpU32(view, header + 40);
  if (informationSize < 8 + RSB_HEADER_SIZE || informationSize > bytes.length) rsbpError("invalid RSB information section size");
  if (subgroupBlockSize !== RSB_SUBGROUP_SIZE) rsbpError("unexpected RSB subgroup record size");
  if (subgroupCount > Math.floor((informationSize - subgroupOffset) / RSB_SUBGROUP_SIZE)) rsbpError("RSB subgroup table is outside the information section");
  var groups = [], byName = Object.create(null);
  for (var i = 0; i < subgroupCount; ++i) {
    var offset = subgroupOffset + i * RSB_SUBGROUP_SIZE;
    var name = rsbpName(bytes, offset);
    if (!name) rsbpError("RSB subgroup " + i + " has no identifier");
    if (Object.prototype.hasOwnProperty.call(byName, name)) rsbpError("duplicate RSB subgroup identifier: " + name);
    var item = {
      index: i, name: name, recordOffset: offset,
      offset: rsbpU32(view, offset + 128), size: rsbpU32(view, offset + 132),
      compression: rsbpU32(view, offset + 140), informationSize: rsbpU32(view, offset + 144),
      generalOffset: rsbpU32(view, offset + 148), generalSize: rsbpU32(view, offset + 152), generalOriginalSize: rsbpU32(view, offset + 156),
      textureOffset: rsbpU32(view, offset + 164), textureSize: rsbpU32(view, offset + 168), textureOriginalSize: rsbpU32(view, offset + 172)
    };
    if (validatePackets !== false) rsbpSlice(bytes, item.offset, item.size, "RSB subgroup " + name);
    groups.push(item); byName[name] = item;
  }
  return { bytes: bytes, view: view, informationSize: informationSize, groups: groups, byName: byName };
}

function rsbpRawPacket(packet) {
  if (packet.length < 8 + RSG_HEADER_SIZE) rsbpError("RSG packet is truncated");
  var view = rsbpView(packet);
  if (rsbpU32(view, 0) !== RSG_MAGIC || rsbpU32(view, 4) !== 4) rsbpError("RSG packet must use version 4");
  var header = 8;
  var compression = rsbpU32(view, header + 8);
  var informationSize = rsbpU32(view, header + 12);
  var generalOffset = rsbpU32(view, header + 16), generalSize = rsbpU32(view, header + 20), generalOriginalSize = rsbpU32(view, header + 24);
  var textureOffset = rsbpU32(view, header + 32), textureSize = rsbpU32(view, header + 36), textureOriginalSize = rsbpU32(view, header + 40);
  if (informationSize < 8 + RSG_HEADER_SIZE || informationSize > packet.length) rsbpError("invalid RSG information section size");
  var general = rsbpSlice(packet, generalOffset, generalSize, "RSG general data");
  var texture = rsbpSlice(packet, textureOffset, textureSize, "RSG texture data");
  var generalCompressed = (compression & 2) !== 0;
  var textureCompressed = (compression & 1) !== 0;
  var generalRaw = generalCompressed ? kernelx.zlib.unzlib(general, generalOriginalSize) : general;
  var textureRaw = textureCompressed && textureOriginalSize !== 0 ? kernelx.zlib.unzlib(texture, textureOriginalSize) : texture;
  if (!generalRaw || !textureRaw) rsbpError("RSG zlib decompression failed");
  generalRaw = new Uint8Array(generalRaw); textureRaw = new Uint8Array(textureRaw);
  if (generalRaw.length !== generalOriginalSize || textureRaw.length !== textureOriginalSize) rsbpError("RSG decompressed size does not match its header");
  var parts = [packet.subarray(0, informationSize)];
  var length = informationSize;
  var padding = rsbpPadding(length); if (padding) { parts.push(new Uint8Array(padding)); length += padding; }
  parts.push(generalRaw); length += generalRaw.length;
  padding = rsbpPadding(length); if (padding) { parts.push(new Uint8Array(padding)); length += padding; }
  parts.push(textureRaw); length += textureRaw.length;
  padding = rsbpPadding(length); if (padding) { parts.push(new Uint8Array(padding)); length += padding; }
  return rsbpConcat(parts, length);
}

function rsbpRawPacketSize(item) {
  var size = rsbpAlign(item.informationSize);
  size = rsbpAlign(size + item.generalOriginalSize);
  size = rsbpAlign(size + item.textureOriginalSize);
  return size;
}

function rsbpRipePacket(raw) {
  if (raw.length < 8 + RSG_HEADER_SIZE) rsbpError("raw RSG packet is truncated");
  var source = rsbpView(raw);
  if (rsbpU32(source, 0) !== RSG_MAGIC || rsbpU32(source, 4) !== 4) rsbpError("raw RSG packet must use version 4");
  var header = 8;
  var compression = rsbpU32(source, header + 8), informationSize = rsbpU32(source, header + 12);
  var generalOffset = rsbpU32(source, header + 16), generalOriginalSize = rsbpU32(source, header + 24);
  var textureOffset = rsbpU32(source, header + 32), textureOriginalSize = rsbpU32(source, header + 40);
  if (informationSize < 8 + RSG_HEADER_SIZE || informationSize > raw.length) rsbpError("invalid raw RSG information section size");
  var general = rsbpSlice(raw, generalOffset, generalOriginalSize, "raw RSG general data");
  var texture = rsbpSlice(raw, textureOffset, textureOriginalSize, "raw RSG texture data");
  var generalCompressed = (compression & 2) !== 0, textureCompressed = (compression & 1) !== 0;
  var generalRipe = generalCompressed ? kernelx.zlib.zlib(general, 9) : general;
  var textureRipe = textureCompressed && textureOriginalSize !== 0 ? kernelx.zlib.zlib(texture, 9) : texture;
  if (!generalRipe || !textureRipe) rsbpError("RSG zlib compression failed");
  generalRipe = new Uint8Array(generalRipe); textureRipe = new Uint8Array(textureRipe);
  var information = raw.slice(0, informationSize), view = rsbpView(information);
  var parts = [information], length = information.length;
  var padding = rsbpPadding(length); if (padding) { parts.push(new Uint8Array(padding)); length += padding; }
  rsbpSetU32(view, header + 16, length); parts.push(generalRipe); length += generalRipe.length;
  if (generalCompressed) { padding = rsbpPadding(length); if (padding) { parts.push(new Uint8Array(padding)); length += padding; } }
  rsbpSetU32(view, header + 20, generalRipe.length);
  rsbpSetU32(view, header + 32, length); parts.push(textureRipe); length += textureRipe.length;
  if (textureCompressed && textureOriginalSize !== 0) { padding = rsbpPadding(length); if (padding) { parts.push(new Uint8Array(padding)); length += padding; } }
  rsbpSetU32(view, header + 36, textureRipe.length);
  var packet = rsbpConcat(parts, length);
  var dataSize = Math.max(informationSize, rsbpU32(view, header + 16) + rsbpU32(view, header + 20), rsbpU32(view, header + 32) + rsbpU32(view, header + 36));
  return { bytes: packet, dataSize: dataSize };
}

function rsbpPacketData(bundle, item, raw) {
  var packet = rsbpSlice(bundle.bytes, item.offset, item.size, "RSB subgroup " + item.name);
  return raw ? rsbpRawPacket(packet) : packet;
}
function rsbpBuildPacketInfo(name, patchExists, patchSize, beforeHash) {
  var bytes = new Uint8Array(RSBP_PACKET_SIZE), view = rsbpView(bytes);
  rsbpSetU32(view, 0, patchExists ? 1 : 0); rsbpSetU32(view, 4, patchSize);
  rsbpWriteName(bytes, 8, name); bytes.set(beforeHash, 136);
  return bytes;
}

function rsbpEncode(beforeBytes, afterBytes, raw) {
  var before = rsbpParseRsb(beforeBytes), after = rsbpParseRsb(afterBytes);
  var informationBefore = before.bytes.subarray(0, before.informationSize);
  var informationAfter = after.bytes.subarray(0, after.informationSize);
  var informationChanged = !rsbpEqual(informationBefore, informationAfter);
  var informationPatch = informationChanged ? rsbpVcdiffEncode(informationBefore, informationAfter) : new Uint8Array(0);
  var parts = [new Uint8Array(8), new Uint8Array(RSBP_PACKAGE_SIZE), informationPatch];
  var length = 8 + RSBP_PACKAGE_SIZE + informationPatch.length;
  var afterEnd = after.informationSize;
  for (var i = 0; i < after.groups.length; ++i) {
    var target = after.groups[i], source = before.byName[target.name];
    var beforePacket = source ? rsbpPacketData(before, source, raw) : new Uint8Array(0);
    var afterPacket = rsbpPacketData(after, target, raw);
    var changed = !rsbpEqual(beforePacket, afterPacket);
    var packetPatch = changed ? rsbpVcdiffEncode(beforePacket, afterPacket) : new Uint8Array(0);
    parts.push(rsbpBuildPacketInfo(target.name, changed, packetPatch.length, rsbpHash(beforePacket)), packetPatch);
    length += RSBP_PACKET_SIZE + packetPatch.length;
    afterEnd = Math.max(afterEnd, target.offset + target.size);
  }
  var output = rsbpConcat(parts, length), view = rsbpView(output);
  rsbpSetU32(view, 0, RSBP_MAGIC); rsbpSetU32(view, 4, RSBP_VERSION);
  rsbpSetU32(view, 8, 2); rsbpSetU32(view, 12, afterEnd); rsbpSetU32(view, 16, 0);
  rsbpSetU32(view, 20, informationPatch.length); output.set(rsbpHash(informationBefore), 24);
  rsbpSetU32(view, 40, after.groups.length); rsbpSetU32(view, 44, informationChanged ? 1 : 0);
  return output;
}

function rsbpDecode(beforeBytes, patchBytes, raw) {
  var before = rsbpParseRsb(beforeBytes);
  if (patchBytes.length < 8 + RSBP_PACKAGE_SIZE) rsbpError("patch is truncated");
  var patchView = rsbpView(patchBytes);
  if (rsbpU32(patchView, 0) !== RSBP_MAGIC || rsbpU32(patchView, 4) !== RSBP_VERSION) rsbpError("unsupported RSB patch version");
  if (rsbpU32(patchView, 8) !== 2 || rsbpU32(patchView, 16) !== 0) rsbpError("invalid RSB patch package header");
  var allAfterSize = rsbpU32(patchView, 12), informationPatchSize = rsbpU32(patchView, 20), packetCount = rsbpU32(patchView, 40), informationChanged = rsbpU32(patchView, 44) !== 0;
  if (allAfterSize < before.informationSize || packetCount > 0x100000) rsbpError("invalid RSB patch package metadata");
  var informationBefore = before.bytes.subarray(0, before.informationSize);
  if (!rsbpEqual(rsbpHash(informationBefore), patchBytes.subarray(24, 40))) rsbpError("before RSB information MD5 mismatch");
  var offset = 8 + RSBP_PACKAGE_SIZE;
  var informationAfter;
  if (!informationChanged) {
    if (informationPatchSize !== 0) rsbpError("unchanged RSB information has a patch payload");
    informationAfter = informationBefore;
  } else {
    var informationPatch = rsbpSlice(patchBytes, offset, informationPatchSize, "RSB information patch"); offset += informationPatchSize;
    informationAfter = rsbpVcdiffDecode(informationBefore, informationPatch, allAfterSize);
  }
  var result = rsbpParseRsb(informationAfter, false);
  if (result.groups.length !== packetCount) rsbpError("RSB patch packet count does not match its information section");
  var parts = [informationAfter], length = informationAfter.length;
  for (var i = 0; i < result.groups.length; ++i) {
    var target = result.groups[i];
    var packetInfo = rsbpSlice(patchBytes, offset, RSBP_PACKET_SIZE, "RSB packet header"); offset += RSBP_PACKET_SIZE;
    var infoView = rsbpView(packetInfo), name = rsbpName(packetInfo, 8), changed = rsbpU32(infoView, 0) !== 0, packetPatchSize = rsbpU32(infoView, 4);
    if (name !== target.name) rsbpError("RSB patch packet order does not match the information section");
    var source = before.byName[name], beforePacket = source ? rsbpPacketData(before, source, raw) : new Uint8Array(0);
    if (!rsbpEqual(rsbpHash(beforePacket), packetInfo.subarray(136, 152))) rsbpError("before packet MD5 mismatch for " + name);
    var afterPacket;
    if (!changed) {
      if (packetPatchSize !== 0) rsbpError("unchanged packet has a patch payload: " + name);
      afterPacket = beforePacket;
    } else {
      var packetPatch = rsbpSlice(patchBytes, offset, packetPatchSize, "RSB packet patch"); offset += packetPatchSize;
      afterPacket = rsbpVcdiffDecode(beforePacket, packetPatch, raw ? rsbpRawPacketSize(target) : target.size);
    }
    if (raw) {
      var ripe = rsbpRipePacket(afterPacket), record = target.recordOffset, info = rsbpView(informationAfter);
      rsbpSetU32(info, record + 128, length); rsbpSetU32(info, record + 132, ripe.dataSize);
      var ripeHeader = rsbpView(ripe.bytes), ripeBase = 8;
      rsbpSetU32(info, record + 148, rsbpU32(ripeHeader, ripeBase + 16)); rsbpSetU32(info, record + 152, rsbpU32(ripeHeader, ripeBase + 20));
      rsbpSetU32(info, record + 164, rsbpU32(ripeHeader, ripeBase + 32)); rsbpSetU32(info, record + 168, rsbpU32(ripeHeader, ripeBase + 36));
      afterPacket = ripe.bytes;
    }
    parts.push(afterPacket); length += afterPacket.length;
  }
  if (offset !== patchBytes.length) rsbpError("patch has trailing data");
  var output = rsbpConcat(parts, length);
  rsbpParseRsb(output);
  return output;
}

function execute(params, id) {
  try {
    var mode = id === "rsb_patch.encode" ? "encode" : id === "rsb_patch.decode" ? "decode" : params.Mode;
    var beforeFile = params.BeforeFile, afterFile = params.AfterFile, patchFile = params.PatchFile, raw = rsbpBool(params.UseRawPacket);
    if (!beforeFile || !kernelx.io.isFile(beforeFile)) rsbpError("BeforeFile is required and must exist");
    if (mode === "encode") {
      if (!afterFile || !kernelx.io.isFile(afterFile)) rsbpError("AfterFile is required and must exist");
      if (!patchFile) rsbpError("PatchFile is required");
      var patch = rsbpEncode(new Uint8Array(kernelx.io.mmapRead(beforeFile)), new Uint8Array(kernelx.io.mmapRead(afterFile)), raw);
      if (!kernelx.io.writeBytes(patchFile, patch)) rsbpError("unable to write " + patchFile);
      return { success: true, output: patchFile };
    }
    if (mode === "decode") {
      if (!patchFile || !kernelx.io.isFile(patchFile)) rsbpError("PatchFile is required and must exist");
      if (!afterFile) rsbpError("AfterFile is required");
      var after = rsbpDecode(new Uint8Array(kernelx.io.mmapRead(beforeFile)), new Uint8Array(kernelx.io.mmapRead(patchFile)), raw);
      if (!kernelx.io.writeBytes(afterFile, after)) rsbpError("unable to write " + afterFile);
      return { success: true, output: afterFile };
    }
    rsbpError("Mode must be encode or decode");
  } catch (error) { return { success: false, error: error && error.message ? error.message : String(error) }; }
}
