/* kernelx-manifest
[
  {
    "id": "soe.unpack",
    "implementation": "implementation",
    "buffer_size": "512m",
    "params": [
      { "name": "InputFile", "type": "path", "required": true, "extensions": [".soe", ".SOE"] },
      { "name": "OutputFile", "type": "path", "required": true }
    ]
  },
  {
    "id": "soe.pack",
    "implementation": "implementation",
    "buffer_size": "512m",
    "params": [
      { "name": "InputFile", "type": "path", "required": true },
      { "name": "OutputFile", "type": "path", "required": true, "extensions": [".soe", ".SOE"] },
      { "name": "Level", "type": "int", "required": false, "default": 6, "min": 0, "max": 9 }
    ]
  }
]
*/

var SOE_HEADER_SIZE = 20;
var SOE_MAGIC = 0x00454f53;
var SOE_COMPRESSION_ZLB = 0x00424c5a;
var SOE_VERSION = 1;
var SOE_MAX_SIZE = 0xffffffff;

function soeWriteFile(path, bytes) {
  var writer = kernelx.io.openWriter(path);
  return !!writer && writer.write(bytes) && writer.close() && !writer.failed();
}

function soeHeader(bytes) {
  if (bytes.length < SOE_HEADER_SIZE) throw new Error("Invalid or truncated SOE header");
  var view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
  var magic = view.getUint32(0, true);
  var compression = view.getUint32(4, true);
  var uncompressedSize = view.getUint32(8, true);
  var compressedSize = view.getUint32(12, true);
  var version = view.getUint32(16, true);
  if (magic !== SOE_MAGIC) throw new Error("Invalid SOE signature");
  if (compression !== SOE_COMPRESSION_ZLB) throw new Error("Unsupported SOE compression");
  if (version !== SOE_VERSION) throw new Error("Unsupported SOE version " + version);
  if (compressedSize !== bytes.length - SOE_HEADER_SIZE) throw new Error("SOE compressed size does not match file length");
  return { uncompressedSize: uncompressedSize, compressedSize: compressedSize };
}

function soeCompressEmpty(level) {
  var stream = kernelx.zlib.stream.compressor(level);
  if (!stream) return null;
  return stream.finish();
}

function soeUnpack(input, output) {
  if (!input || !kernelx.io.isFile(input)) return { success: false, error: "InputFile is required and must exist" };
  if (!output) return { success: false, error: "OutputFile is required" };
  try {
    var source = kernelx.io.mmapRead(input);
    if (!source) throw new Error("Unable to read InputFile");
    var bytes = new Uint8Array(source);
    var header = soeHeader(bytes);
    var payload = bytes.subarray(SOE_HEADER_SIZE);
    var decoded = header.uncompressedSize === 0
      ? kernelx.zlib.decompress(payload)
      : kernelx.zlib.decompress(payload, header.uncompressedSize);
    if (!decoded) throw new Error("SOE zlib decompression failed");
    var outputBytes = new Uint8Array(decoded);
    if (outputBytes.length !== header.uncompressedSize) throw new Error("SOE uncompressed size does not match header");
    if (!soeWriteFile(output, outputBytes)) throw new Error("Unable to write " + output);
    return { success: true, output: output, uncompressedSize: outputBytes.length, compressedSize: header.compressedSize };
  } catch (error) {
    return { success: false, error: error && error.message ? error.message : String(error) };
  }
}

function soePack(input, output, level) {
  if (!input || !kernelx.io.isFile(input)) return { success: false, error: "InputFile is required and must exist" };
  if (!output) return { success: false, error: "OutputFile is required" };
  try {
    var source = kernelx.io.mmapRead(input);
    if (!source) throw new Error("Unable to read InputFile");
    var raw = new Uint8Array(source);
    if (raw.length > SOE_MAX_SIZE) throw new Error("InputFile exceeds the SOE 4 GiB size limit");
    var compressionLevel = Number(level);
    if (!Number.isInteger(compressionLevel) || compressionLevel < 0 || compressionLevel > 9) compressionLevel = 6;
    var compressed = raw.length === 0
      ? soeCompressEmpty(compressionLevel)
      : kernelx.zlib.compress(raw, compressionLevel);
    if (!compressed) throw new Error("SOE zlib compression failed");
    var payload = new Uint8Array(compressed);
    if (payload.length > SOE_MAX_SIZE) throw new Error("Compressed SOE data exceeds the 4 GiB size limit");
    var header = new Uint8Array(SOE_HEADER_SIZE);
    var view = new DataView(header.buffer);
    view.setUint32(0, SOE_MAGIC, true);
    view.setUint32(4, SOE_COMPRESSION_ZLB, true);
    view.setUint32(8, raw.length, true);
    view.setUint32(12, payload.length, true);
    view.setUint32(16, SOE_VERSION, true);
    var archive = new Uint8Array(SOE_HEADER_SIZE + payload.length);
    archive.set(header);
    archive.set(payload, SOE_HEADER_SIZE);
    if (!soeWriteFile(output, archive)) throw new Error("Unable to write " + output);
    return { success: true, output: output, uncompressedSize: raw.length, compressedSize: payload.length };
  } catch (error) {
    return { success: false, error: error && error.message ? error.message : String(error) };
  }
}

function execute(params, id) {
  return id === "soe.pack" ? soePack(params.InputFile, params.OutputFile, params.Level) : soeUnpack(params.InputFile, params.OutputFile);
}
