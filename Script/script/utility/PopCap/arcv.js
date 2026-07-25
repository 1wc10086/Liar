/* kernelx-manifest
[
  {
    "id": "arcv.unpack",
    "implementation": "implementation",
    "buffer_size": "64m",
    "params": [
      { "name": "InputFile", "type": "path", "required": true },
      { "name": "OutputFolder", "type": "path", "required": true, "folder": true }
    ]
  },
  {
    "id": "arcv.pack",
    "implementation": "implementation",
    "buffer_size": "64m",
    "params": [
      { "name": "InputFolder", "type": "path", "required": true, "folder": true },
      { "name": "OutputFile", "type": "path", "required": true, "extensions": [".arcv"] }
    ]
  }
]
*/

var ARCV_EXTENSIONS = {
  RGCN: ".NCGR",
  RCSN: ".NSCR",
  RLCN: ".NCLR",
  RNAN: ".NANR",
  RECN: ".NCER",
  RAMN: ".NMAR",
  RCMN: ".NMCR",
  RTFN: ".NFTR",
  SDAT: ".sdat",
  NARC: ".narc"
};

function bytesText(bytes) {
  var text = "";
  for (var i = 0; i < bytes.length; ++i) text += String.fromCharCode(bytes[i]);
  return text;
}

function crcFileName(crc, magic) {
  var name = String(crc);
  while (name.length < 10) name = "0" + name;
  return name + (ARCV_EXTENSIONS[magic] || ".dat");
}

function parseCSharpInt64(value) {
  value = value.trim();
  if (!/^[+-]?\d+$/.test(value)) return null;
  try {
    var parsed = BigInt(value);
    return parsed >= -9223372036854775808n && parsed <= 9223372036854775807n ? parsed : null;
  } catch (_) {
    return null;
  }
}

function unpack(input, output) {
  if (!input || !kernelx.io.isFile(input)) return { success: false, error: "InputFile is required and must exist" };
  if (!output) return { success: false, error: "OutputFolder is required" };

  var data = kernelx.io.mmapRead(input);
  if (!data || data.byteLength < 12) return { success: false, error: "Invalid or truncated ARCV file" };

  var reader = kernelx.binary.reader(data, "le");
  if (bytesText(reader.bytes(4)) !== "ARCV") return { success: false, error: "Invalid ARCV signature" };
  var count = reader.i32();
  var archiveSize = reader.i32();
  if (reader.error() || count < 0 || archiveSize < 12 || archiveSize > data.byteLength || count > Math.floor((archiveSize - 12) / 12))
    return { success: false, error: "Invalid ARCV index" };

  var entries = [];
  for (var i = 0; i < count; ++i) {
    var offset = reader.u32();
    var size = reader.i32();
    var crc = reader.u32();
    if (reader.error() || size < 0 || offset > archiveSize || size > archiveSize - offset)
      return { success: false, error: "ARCV entry " + i + " is outside the archive" };
    entries.push({ offset: offset, size: size, crc: crc });
  }

  if (!kernelx.io.mkdir(output)) return { success: false, error: "Unable to create OutputFolder" };
  for (var j = 0; j < entries.length; ++j) {
    var entry = entries[j];
    var payload = new Uint8Array(data, entry.offset, entry.size);
    var magic = entry.size >= 4 ? bytesText(payload.subarray(0, 4)) : "";
    var path = kernelx.path.join(output, crcFileName(entry.crc, magic));
    if (!kernelx.io.writeBytes(path, payload)) return { success: false, error: "Unable to write " + path };
  }
  return { success: true, output: output };
}

function pack(input, output) {
  if (!input || !kernelx.io.isDir(input)) return { success: false, error: "InputFolder is required and must exist" };
  if (!output) return { success: false, error: "OutputFile is required" };

  var paths = kernelx.io.collect(input);
  var entries = [];
  for (var i = 0; i < paths.length; ++i) {
    var crc = parseCSharpInt64(kernelx.path.stem(paths[i]));
    var size = kernelx.io.size(paths[i]);
    if (crc !== null && size >= 0 && size <= 0x7fffffff && Math.floor(size) === size)
      entries.push({ crc: crc, crcBits: Number(BigInt.asUintN(32, crc)), path: paths[i], size: size });
  }
  entries.sort(function (a, b) { return a.crc < b.crc ? -1 : a.crc > b.crc ? 1 : 0; });

  var headerSize = 12 + entries.length * 12;
  if (headerSize > 0x7fffffff) return { success: false, error: "Too many ARCV entries" };
  var position = headerSize;
  for (var j = 0; j < entries.length; ++j) {
    position = Math.ceil(position / 4) * 4;
    if (position > 0x7fffffff - entries[j].size) return { success: false, error: "ARCV file exceeds 2 GiB" };
    entries[j].offset = position;
    position += entries[j].size;
  }

  var header = kernelx.binary.writer("le");
  header.bytes(new Uint8Array([65, 82, 67, 86]));
  header.i32(entries.length);
  header.i32(position);
  for (var k = 0; k < entries.length; ++k) {
    header.i32(entries[k].offset);
    header.i32(entries[k].size);
    header.u32(entries[k].crcBits);
  }
  if (header.error()) return { success: false, error: "Unable to build ARCV index" };

  var writer = kernelx.io.openWriter(output);
  if (!writer || !writer.write(header.finish())) return { success: false, error: "Unable to create OutputFile" };
  var padding = new Uint8Array([172, 172, 172]);
  for (var n = 0; n < entries.length; ++n) {
    var pad = (4 - (Number(writer.position()) % 4)) % 4;
    if (pad && !writer.write(padding.subarray(0, pad))) return { success: false, error: "Unable to pad ARCV data" };
    var wrote = kernelx.io.streamRead(entries[n].path, function (chunk) { return writer.write(chunk); }, 65536);
    if (!wrote || writer.failed()) return { success: false, error: "Unable to read " + entries[n].path };
  }
  return writer.close() && !writer.failed()
    ? { success: true, output: output }
    : { success: false, error: "Unable to finish OutputFile" };
}

function execute(params, id) {
  return id === "arcv.pack" || params.InputFolder ? pack(params.InputFolder, params.OutputFile) : unpack(params.InputFile, params.OutputFolder);
}
