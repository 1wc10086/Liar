/* kernelx-manifest
[
  {
    "id": "xmem.unpack",
    "implementation": "implementation",
    "buffer_size": "512m",
    "params": [
      { "name": "InputFile", "type": "path", "required": true },
      { "name": "OutputFile", "type": "path", "required": true }
    ]
  },
  {
    "id": "xmem.pack",
    "implementation": "implementation",
    "buffer_size": "512m",
    "params": [
      { "name": "InputFile", "type": "path", "required": true },
      { "name": "OutputFile", "type": "path", "required": true }
    ]
  }
]
*/

var XMEM_MAGIC_LE = 0xed12f50f;

function xmemMagic(bytes) {
  return bytes.length >= 4
    && (((bytes[0]) | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24)) >>> 0) === XMEM_MAGIC_LE;
}

function xmemWriteFile(path, bytes) {
  var writer = kernelx.io.openWriter(path);
  return !!writer && writer.write(bytes) && writer.close() && !writer.failed();
}

function xmemUnpack(input, output) {
  if (!input || !kernelx.io.isFile(input)) return { success: false, error: "InputFile is required and must exist" };
  if (!output) return { success: false, error: "OutputFile is required" };

  var source = kernelx.io.mmapRead(input);
  if (!source || !source.byteLength) return { success: false, error: "InputFile is empty or cannot be read" };
  var bytes = new Uint8Array(source);
  if (!xmemMagic(bytes)) return { success: false, error: "Invalid XMem LZX TD signature" };

  var decoded = kernelx.xmem.decompress(bytes);
  if (!decoded || !decoded.byteLength) return { success: false, error: "XMem decompression failed" };
  return xmemWriteFile(output, new Uint8Array(decoded))
    ? { success: true, output: output }
    : { success: false, error: "Unable to write " + output };
}

function xmemPack(input, output) {
  if (!input || !kernelx.io.isFile(input)) return { success: false, error: "InputFile is required and must exist" };
  if (!output) return { success: false, error: "OutputFile is required" };

  var source = kernelx.io.mmapRead(input);
  if (!source || !source.byteLength) return { success: false, error: "InputFile is empty or cannot be read" };
  var encoded = kernelx.xmem.compress(new Uint8Array(source));
  if (!encoded || !encoded.byteLength || !xmemMagic(new Uint8Array(encoded))) return { success: false, error: "XMem compression failed" };
  return xmemWriteFile(output, new Uint8Array(encoded))
    ? { success: true, output: output }
    : { success: false, error: "Unable to write " + output };
}

function execute(params, id) {
  return id === "xmem.pack" ? xmemPack(params.InputFile, params.OutputFile) : xmemUnpack(params.InputFile, params.OutputFile);
}
