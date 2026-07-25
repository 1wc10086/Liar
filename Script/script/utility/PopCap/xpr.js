/* kernelx-manifest
[
  {
    "id": "xpr.unpack",
    "implementation": "implementation",
    "buffer_size": "64m",
    "params": [
      { "name": "InputFile", "type": "path", "required": true, "extensions": [".xpr"] },
      { "name": "OutputFolder", "type": "path", "required": true, "folder": true }
    ]
  },
  {
    "id": "xpr.pack",
    "implementation": "implementation",
    "buffer_size": "64m",
    "params": [
      { "name": "InputFolder", "type": "path", "required": true, "folder": true },
      { "name": "OutputFile", "type": "path", "required": true, "extensions": [".xpr"] }
    ]
  }
]
*/

var XPR_MAGIC = "XPR2";
var XPR_INDEX_ENTRY_SIZE = 16;
var XPR_FILE_ALIGNMENT = 16;
var XPR_ARCHIVE_ALIGNMENT = 2048;
var XPR_MAX_U32 = 0xffffffff;
var XPR_TX2D_TAG = "TX2D";
var XPR_TX2D_ENTRY_SIZE = 16;

function xprBytesText(bytes) {
  var text = "";
  for (var i = 0; i < bytes.length; ++i) text += String.fromCharCode(bytes[i]);
  return text;
}

function xprLatin1Bytes(text) {
  var bytes = new Uint8Array(text.length);
  for (var i = 0; i < text.length; ++i) {
    var code = text.charCodeAt(i);
    if (code > 0xff) return null;
    bytes[i] = code;
  }
  return bytes;
}

function xprAlign(value, alignment) {
  return Math.ceil(value / alignment) * alignment;
}

function xprTypeText(type) {
  if (type === null || type === undefined || type === "") return 0;
  var bytes = xprLatin1Bytes(String(type));
  if (!bytes || bytes.length < 4) return null;
  return ((bytes[0] * 0x1000000) + (bytes[1] << 16) + (bytes[2] << 8) + bytes[3]) >>> 0;
}

function xprTypeString(type) {
  if (type === 0) return null;
  return String.fromCharCode((type >>> 24) & 0xff, (type >>> 16) & 0xff, (type >>> 8) & 0xff, type & 0xff);
}

function xprReadPath(data, offset, end) {
  if (offset < 0 || offset >= end) return null;
  var limit = offset;
  while (limit < end && data[limit] !== 0) ++limit;
  if (limit === end) return null;
  return xprBytesText(data.subarray(offset, limit));
}

function xprResourcePath(resourceFolder, recordPath) {
  return kernelx.path.join(resourceFolder, recordPath.replace(/\\/g, "/"));
}

function xprWritePadding(writer, count) {
  if (!count) return true;
  return writer.write(new Uint8Array(count));
}

function xprWriteFile(path, bytes) {
  var writer = kernelx.io.openWriter(path);
  if (!writer) return false;
  var ok = writer.write(bytes) && writer.close();
  return ok && !writer.failed();
}

function xprUnpackTx2d(bytes, output, textureDataSize) {
  var textureDataEnd = 12 + textureDataSize;
  if (textureDataEnd > bytes.byteLength) return null;

  var entries = [];
  for (var offset = 0; offset + XPR_TX2D_ENTRY_SIZE <= textureDataEnd; offset += 4) {
    if (xprBytesText(bytes.subarray(offset + 4, offset + 8)) !== XPR_TX2D_TAG) continue;
    var reader = kernelx.binary.reader(bytes.subarray(offset, offset + XPR_TX2D_ENTRY_SIZE), "be");
    var texturePageOffset = reader.u32();
    reader.bytes(4);
    var descriptorOffset = reader.u32();
    var descriptorSize = reader.u32();
    if (reader.error() || descriptorSize !== 0x34) continue;
    entries.push({ HeaderOffset: offset, TexturePageOffset: texturePageOffset, DescriptorOffset: descriptorOffset, DescriptorSize: descriptorSize });
  }
  if (!entries.length) return null;

  var resourceFolder = kernelx.path.join(output, "resource");
  var infoFolder = kernelx.path.join(output, "info");
  if (!kernelx.io.mkdir(resourceFolder) || !kernelx.io.mkdir(infoFolder)) return { success: false, error: "Unable to create OutputFolder" };
  if (!xprWriteFile(kernelx.path.join(resourceFolder, "texture_data.bin"), bytes.subarray(12, textureDataEnd))) return { success: false, error: "Unable to write texture_data.bin" };

  var info = {
    Source: "XPR2 TX2D texture container (raw)",
    Version: 1,
    Content: {
      TextureDataOffset: 12,
      TextureDataSize: textureDataSize,
      Tx2dEntries: entries
    }
  };
  var infoPath = kernelx.path.join(infoFolder, "tx2d.json");
  if (!kernelx.io.writeText(infoPath, JSON.stringify(info, null, 2))) return { success: false, error: "Unable to write " + infoPath };
  return { success: true, output: output, textures: entries.length };
}

function xprUnpack(input, output) {
  if (!input || !kernelx.io.isFile(input)) return { success: false, error: "InputFile is required and must exist" };
  if (!output) return { success: false, error: "OutputFolder is required" };

  var data = kernelx.io.mmapRead(input);
  if (!data || data.byteLength < 16) return { success: false, error: "Invalid or truncated XPR file" };
  var bytes = new Uint8Array(data);

  var header = kernelx.binary.reader(bytes, "be");
  if (xprBytesText(header.bytes(4)) !== XPR_MAGIC) return { success: false, error: "Invalid XPR signature" };
  var xprDataSize = header.u32();
  var textureDataSize = header.u32();
  if (header.error() || xprDataSize < 8 || xprDataSize > bytes.byteLength - 12) return { success: false, error: "Invalid XPR data size" };

  var tx2dResult = textureDataSize ? xprUnpackTx2d(bytes, output, textureDataSize) : null;
  if (tx2dResult) return tx2dResult;

  var xprDataOffset = bytes.byteLength - xprDataSize;
  if (xprDataOffset < 12) return { success: false, error: "Invalid XPR data offset" };
  var reader = kernelx.binary.reader(bytes.subarray(xprDataOffset, xprDataOffset + xprDataSize), "be");
  var fileCount = reader.u32();
  if (reader.error() || fileCount > Math.floor((xprDataSize - 8) / XPR_INDEX_ENTRY_SIZE)) return { success: false, error: "Invalid XPR index" };

  var entries = [];
  var namesEnd = xprDataSize;
  for (var i = 0; i < fileCount; ++i) {
    var type = reader.u32();
    var fileOffset = reader.u32();
    var fileSize = reader.u32();
    var pathOffset = reader.u32();
    if (reader.error() || fileOffset > xprDataSize || fileSize > xprDataSize - fileOffset) return { success: false, error: "XPR entry " + i + " is outside the archive" };
    entries.push({ type: xprTypeString(type), fileOffset: fileOffset, fileSize: fileSize, pathOffset: pathOffset });
  }
  if (reader.u32() !== 0 || reader.error()) return { success: false, error: "Invalid XPR path pool marker" };

  var resourceFolder = kernelx.path.join(output, "resource");
  var infoFolder = kernelx.path.join(output, "info");
  if (!kernelx.io.mkdir(resourceFolder) || !kernelx.io.mkdir(infoFolder)) return { success: false, error: "Unable to create OutputFolder" };

  var records = [];
  var fileAligned = true;
  for (var j = 0; j < entries.length; ++j) {
    var entry = entries[j];
    var recordPath = xprReadPath(bytes, xprDataOffset + entry.pathOffset, xprDataOffset + namesEnd);
    if (recordPath === null) return { success: false, error: "Invalid XPR path for entry " + j };
    var filePath = xprResourcePath(resourceFolder, recordPath);
    var payload = bytes.subarray(xprDataOffset + entry.fileOffset, xprDataOffset + entry.fileOffset + entry.fileSize);
    if (!kernelx.io.mkdir(kernelx.path.parent(filePath))) return { success: false, error: "Unable to create parent directory for " + filePath };
    if (!xprWriteFile(filePath, payload)) return { success: false, error: "Unable to write " + filePath };
    if (entry.fileOffset % XPR_FILE_ALIGNMENT !== 0) fileAligned = false;
    records.push({ Type: entry.type, Path: recordPath });
  }

  var packInfo = {
    Source: "LibWindPop.Packs.Xpr.XprPackInfo",
    Author: "YingFengTingYu",
    Version: 0,
    Content: {
      XprDataOffset: xprDataOffset,
      XprDataFileAlign: fileAligned,
      RecordFiles: records
    }
  };
  var infoPath = kernelx.path.join(infoFolder, "pack_info.json");
  if (!kernelx.io.writeText(infoPath, JSON.stringify(packInfo, null, 2))) return { success: false, error: "Unable to write " + infoPath };
  return { success: true, output: output };
}

function xprPack(input, output) {
  if (!input || !kernelx.io.isDir(input)) return { success: false, error: "InputFolder is required and must exist" };
  if (!output) return { success: false, error: "OutputFile is required" };

  var infoPath = kernelx.path.join(input, "info", "pack_info.json");
  var infoText = kernelx.io.readText(infoPath);
  var shell;
  try { shell = JSON.parse(infoText); } catch (_) { return { success: false, error: "Invalid XPR pack_info.json" }; }
  if (shell && shell.Version === 1 && shell.Source === "XPR2 TX2D texture container (raw)") return { success: false, error: "Packing XPR2 TX2D containers is not supported" };
  if (!shell || shell.Version !== 0 || !shell.Content || !Array.isArray(shell.Content.RecordFiles)) return { success: false, error: "Invalid XPR pack info" };

  var content = shell.Content;
  var xprDataOffset = content.XprDataOffset;
  if (!Number.isInteger(xprDataOffset) || xprDataOffset < 12 || xprDataOffset > XPR_MAX_U32) return { success: false, error: "Invalid XprDataOffset" };

  var entries = [];
  var pathPoolSize = 0;
  for (var i = 0; i < content.RecordFiles.length; ++i) {
    var record = content.RecordFiles[i];
    if (!record || typeof record.Path !== "string") return { success: false, error: "Invalid XPR record " + i };
    var pathBytes = xprLatin1Bytes(record.Path);
    var type = xprTypeText(record.Type);
    var sourcePath = xprResourcePath(kernelx.path.join(input, "resource"), record.Path);
    var size = kernelx.io.size(sourcePath);
    if (!pathBytes || type === null || !kernelx.io.isFile(sourcePath) || !Number.isInteger(size) || size < 0 || size > XPR_MAX_U32) return { success: false, error: "Invalid XPR resource " + record.Path };
    if (pathPoolSize > XPR_MAX_U32 - pathBytes.length - 1) return { success: false, error: "XPR path pool is too large" };
    pathPoolSize += pathBytes.length + 1;
    entries.push({ type: type, path: record.Path, pathBytes: pathBytes, sourcePath: sourcePath, size: size });
  }

  if (entries.length > Math.floor((XPR_MAX_U32 - 8) / XPR_INDEX_ENTRY_SIZE)) return { success: false, error: "Too many XPR entries" };
  var headerSize = 8 + entries.length * XPR_INDEX_ENTRY_SIZE + pathPoolSize;
  headerSize = xprAlign(headerSize, XPR_FILE_ALIGNMENT);
  if (headerSize > XPR_MAX_U32) return { success: false, error: "XPR index is too large" };

  var position = headerSize;
  var alignFiles = content.XprDataFileAlign === true;
  for (var j = 0; j < entries.length; ++j) {
    if (alignFiles) position = xprAlign(position, XPR_FILE_ALIGNMENT);
    if (position > XPR_MAX_U32 - entries[j].size) return { success: false, error: "XPR data exceeds 4 GiB" };
    entries[j].offset = position;
    position += entries[j].size;
  }
  var xprDataSize = xprAlign(position, XPR_ARCHIVE_ALIGNMENT);
  if (xprDataSize > XPR_MAX_U32) return { success: false, error: "XPR archive exceeds 4 GiB" };

  var index = kernelx.binary.writer("be");
  index.u32(entries.length);
  var pathOffset = 8 + entries.length * XPR_INDEX_ENTRY_SIZE;
  for (var k = 0; k < entries.length; ++k) {
    var entry = entries[k];
    index.u32(entry.type);
    index.u32(entry.offset);
    index.u32(entry.size);
    index.u32(pathOffset);
    pathOffset += entry.pathBytes.length + 1;
  }
  index.u32(0);
  for (var n = 0; n < entries.length; ++n) {
    index.bytes(entries[n].pathBytes);
    index.u8(0);
  }
  var indexData = index.finish();
  if (index.error() || indexData.byteLength > headerSize) return { success: false, error: "Unable to build XPR index" };

  var writer = kernelx.io.openWriter(output);
  if (!writer) return { success: false, error: "Unable to create OutputFile" };
  var header = kernelx.binary.writer("be");
  header.bytes(new Uint8Array([88, 80, 82, 50]));
  header.u32(xprDataSize);
  header.u32(0);
  if (header.error() || !writer.write(header.finish()) || !xprWritePadding(writer, xprDataOffset - 12) || !writer.write(indexData) || !xprWritePadding(writer, headerSize - indexData.byteLength)) {
    writer.close();
    return { success: false, error: "Unable to write XPR header" };
  }

  for (var m = 0; m < entries.length; ++m) {
    var padding = entries[m].offset - Number(writer.position() - BigInt(xprDataOffset));
    if (padding < 0 || !xprWritePadding(writer, padding)) {
      writer.close();
      return { success: false, error: "Unable to align XPR resource data" };
    }
    var copied = kernelx.io.streamRead(entries[m].sourcePath, function (chunk) { return writer.write(chunk); }, 65536);
    if (!copied || writer.failed()) {
      writer.close();
      return { success: false, error: "Unable to read " + entries[m].sourcePath };
    }
  }
  var finalPadding = xprDataSize - Number(writer.position() - BigInt(xprDataOffset));
  if (finalPadding < 0 || !xprWritePadding(writer, finalPadding) || !writer.close() || writer.failed()) return { success: false, error: "Unable to finish OutputFile" };
  return { success: true, output: output };
}

function execute(params, id) {
  return id === "xpr.pack" || params.InputFolder ? xprPack(params.InputFolder, params.OutputFile) : xprUnpack(params.InputFile, params.OutputFolder);
}
