/* kernelx-manifest
{
  "id": "ps3.pkg.unpack",
  "implementation": "implementation",
  "buffer_size": "16m",
  "params": [
    { "name": "InputFile", "type": "path", "required": true, "extensions": [".pkg"] },
    { "name": "OutputFolder", "type": "path", "required": true, "folder": true }
  ]
}
*/

var PS3_PKG_MAGIC = [0x7f, 0x50, 0x4b, 0x47];
var PS3_PKG_TYPE = 0x01;
var PS3_RETAIL_FINALIZED = 0x80;
var PS3_AES_KEY = new Uint8Array([0x2e, 0x7b, 0x71, 0xd7, 0xc9, 0xc9, 0xa1, 0x4e, 0xa3, 0x22, 0x1f, 0x18, 0x88, 0x28, 0xb8, 0xf8]);
var PS3_BLOCK_SIZE = 16;
var PS3_COPY_CHUNK_SIZE = 1024 * 1024;
var PS3_MAX_FILE_COUNT = 100000;

function ps3U32BE(bytes, offset) {
  return ((bytes[offset] * 0x1000000) + (bytes[offset + 1] << 16) + (bytes[offset + 2] << 8) + bytes[offset + 3]) >>> 0;
}

function ps3Matches(bytes, offset, expected) {
  for (var i = 0; i < expected.length; ++i) if (bytes[offset + i] !== expected[i]) return false;
  return true;
}

function ps3RequireRange(offset, size, limit, message) {
  if (!Number.isSafeInteger(offset) || !Number.isSafeInteger(size) || offset < 0 || size < 0 || offset > limit - size) throw new Error(message);
}

function ps3ReadAt(input, offset, size) {
  var bytes = new Uint8Array(size);
  if (!kernelx.io.posixReadInto(input, bytes, size, offset)) throw new Error("PKG file is truncated.");
  return bytes;
}

function ps3CounterAt(fileKey, blocks) {
  var counter = new Uint8Array(fileKey);
  for (var index = 15; index >= 0 && blocks > 0; --index) {
    var add = blocks % 256;
    var value = counter[index] + add;
    counter[index] = value & 0xff;
    blocks = Math.floor(blocks / 256) + (value > 0xff ? 1 : 0);
  }
  return counter;
}

function ps3DecodeUtf8(bytes) {
  var text = "";
  for (var index = 0; index < bytes.length && bytes[index] !== 0; ++index) {
    var first = bytes[index];
    if (first < 0x80) {
      text += String.fromCharCode(first);
      continue;
    }
    var count = first >= 0xf0 && first <= 0xf4 ? 4 : first >= 0xe0 && first <= 0xef ? 3 : first >= 0xc2 && first <= 0xdf ? 2 : 0;
    if (!count || index + count > bytes.length) {
      text += "?";
      continue;
    }
    var code = first & ((1 << (7 - count)) - 1);
    var valid = true;
    for (var part = 1; part < count; ++part) {
      var next = bytes[index + part];
      if ((next & 0xc0) !== 0x80) valid = false;
      code = (code << 6) | (next & 0x3f);
    }
    if (!valid || code > 0x10ffff || (code >= 0xd800 && code <= 0xdfff)) {
      text += "?";
      continue;
    }
    if (code <= 0xffff) text += String.fromCharCode(code);
    else {
      code -= 0x10000;
      text += String.fromCharCode(0xd800 + (code >> 10), 0xdc00 + (code & 0x3ff));
    }
    index += count - 1;
  }
  return text;
}

function ps3SafePath(root, name) {
  var normalized = name.replace(/\\/g, "/");
  if (!normalized || normalized.charAt(0) === "/" || /^[A-Za-z]:/.test(normalized)) throw new Error("Unsafe path in PKG: " + name);
  var parts = normalized.split("/");
  var output = root;
  for (var index = 0; index < parts.length; ++index) {
    if (!parts[index] || parts[index] === "." || parts[index] === "..") throw new Error("Unsafe path in PKG: " + name);
    output = kernelx.path.join(output, parts[index]);
  }
  return output;
}

function ps3CreateReader(input) {
  var fileSize = Number(kernelx.io.posixSize(input));
  if (!Number.isSafeInteger(fileSize) || fileSize < 0x80) throw new Error("File is too small to be a PS3 PKG.");
  var header = ps3ReadAt(input, 0, 0x80);
  if (!ps3Matches(header, 0, PS3_PKG_MAGIC)) throw new Error("Invalid PKG magic. Expected 7F 50 4B 47.");

  var packageType = header[0x07];
  var dataOffset = ps3U32BE(header, 0x24);
  var dataSize = ps3U32BE(header, 0x2c);
  if (packageType !== PS3_PKG_TYPE) throw new Error("Unsupported package type 0x" + packageType.toString(16) + "; this script extracts PS3 PKG files only.");
  ps3RequireRange(dataOffset, dataSize, fileSize, "Invalid encrypted-data range.");
  if (!dataSize || dataOffset < 0x80) throw new Error("Invalid encrypted-data range.");

  var retail = header[0x04] === PS3_RETAIL_FINALIZED;
  if (retail && (!kernelx.botan.loaded || !kernelx.botan.loaded())) throw new Error("Retail PKG extraction requires the bundled Botan crypto library: " + kernelx.botan.error());
  var fileKey = header.slice(0x70, 0x80);

  function decryptAt(relativeOffset, size) {
    ps3RequireRange(relativeOffset, size, dataSize, "File-table entry points outside the package data area.");
    if (!size) return new Uint8Array(0);
    if (!retail) return ps3ReadAt(input, dataOffset + relativeOffset, size);

    var blockOffset = Math.floor(relativeOffset / PS3_BLOCK_SIZE);
    var withinBlock = relativeOffset % PS3_BLOCK_SIZE;
    var encryptedSize = Math.ceil((withinBlock + size) / PS3_BLOCK_SIZE) * PS3_BLOCK_SIZE;
    ps3RequireRange(blockOffset * PS3_BLOCK_SIZE, encryptedSize, dataSize, "Encrypted retail data is not AES block-aligned.");
    var encrypted = ps3ReadAt(input, dataOffset + blockOffset * PS3_BLOCK_SIZE, encryptedSize);
    // CTR decrypts by XORing the same AES-encrypted counter stream as encryption.
    var decryptedData = kernelx.botan.encrypt(encrypted, PS3_AES_KEY, ps3CounterAt(fileKey, blockOffset), "AES-128-CTR");
    if (!decryptedData || decryptedData.byteLength !== encryptedSize) throw new Error("Unable to decrypt retail PKG data: " + kernelx.botan.error());
    var decrypted = new Uint8Array(decryptedData);
    return decrypted.slice(withinBlock, withinBlock + size);
  }

  return { fileSize: fileSize, dataSize: dataSize, retail: retail, decryptAt: decryptAt };
}

function ps3WriteFile(reader, relativeOffset, size, destination) {
  ps3RequireRange(relativeOffset, size, reader.dataSize, "File entry points outside the package data area.");
  var writer = kernelx.io.openWriter(destination);
  if (!writer) throw new Error("Unable to create " + destination);
  var ok = true;
  try {
    for (var offset = 0; offset < size; offset += PS3_COPY_CHUNK_SIZE) {
      var amount = Math.min(PS3_COPY_CHUNK_SIZE, size - offset);
      if (!writer.write(reader.decryptAt(relativeOffset + offset, amount))) throw new Error("Unable to write " + destination);
    }
  } catch (error) {
    ok = false;
    throw error;
  } finally {
    if (!writer.close() || writer.failed()) ok = false;
  }
  if (!ok) throw new Error("Unable to finish " + destination);
}

function ps3PkgUnpack(input, output) {
  if (!input || !kernelx.io.isFile(input)) return { success: false, error: "InputFile is required and must exist" };
  if (!output) return { success: false, error: "OutputFolder is required" };
  try {
    var reader = ps3CreateReader(input);
    var prefix = reader.decryptAt(0, 32);
    var nameTableOffset = ps3U32BE(prefix, 0);
    var firstFileOffset = ps3U32BE(prefix, 12);
    if (!nameTableOffset || nameTableOffset % 32 || nameTableOffset > firstFileOffset) throw new Error("Invalid PS3 PKG file table.");
    var fileCount = nameTableOffset / 32;
    if (fileCount > PS3_MAX_FILE_COUNT || firstFileOffset > reader.dataSize) throw new Error("Unreasonable PS3 PKG file table size.");

    var table = reader.decryptAt(0, firstFileOffset);
    if (!kernelx.io.mkdir(output)) throw new Error("Unable to create OutputFolder");
    var extracted = 0;
    for (var index = 0; index < fileCount; ++index) {
      var entryOffset = index * 32;
      var nameOffset = ps3U32BE(table, entryOffset);
      var nameSize = ps3U32BE(table, entryOffset + 4);
      var dataOffset = ps3U32BE(table, entryOffset + 12);
      var dataSize = ps3U32BE(table, entryOffset + 20);
      var contentType = table[entryOffset + 24];
      var fileType = table[entryOffset + 27];
      ps3RequireRange(nameOffset, nameSize, reader.dataSize, "Invalid name entry at index " + index + ".");
      if (!nameSize) throw new Error("Invalid name entry at index " + index + ".");

      var nameBytes;
      if (contentType === 0x90) {
        ps3RequireRange(nameOffset, nameSize, table.length, "Invalid inline name entry at index " + index + ".");
        nameBytes = table.subarray(nameOffset, nameOffset + nameSize);
      } else nameBytes = reader.decryptAt(nameOffset, nameSize);
      var destination = ps3SafePath(output, ps3DecodeUtf8(nameBytes));
      if (fileType === 0x04 && dataSize === 0) {
        if (!kernelx.io.mkdir(destination)) throw new Error("Unable to create " + destination);
        continue;
      }
      ps3WriteFile(reader, dataOffset, dataSize, destination);
      ++extracted;
    }
    return { success: true, output: output, files: extracted, entries: fileCount, retail: reader.retail };
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) };
  }
}

function execute(params) {
  return ps3PkgUnpack(params.InputFile, params.OutputFolder);
}
