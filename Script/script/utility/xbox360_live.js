/* kernelx-manifest
{
  "id": "xbox360.live.unpack",
  "implementation": "implementation",
  "buffer_size": "128m",
  "params": [
    { "name": "InputFile", "type": "path", "required": true },
    { "name": "OutputFolder", "type": "path", "required": true, "folder": true }
  ]
}
*/

var STFS_BLOCK_SIZE = 4096;
var STFS_VOLUME_DESCRIPTOR_OFFSET = 0x379;
var STFS_HASH_ENTRIES = 170;
var STFS_HASH_ENTRY_SIZE = 0x18;
var STFS_END_OF_CHAIN = 0xffffff;

function stfsU24LE(data, offset) {
  return data[offset] + data[offset + 1] * 0x100 + data[offset + 2] * 0x10000;
}

function stfsU24BE(data, offset) {
  return data[offset] * 0x10000 + data[offset + 1] * 0x100 + data[offset + 2];
}

function stfsU32BE(data, offset) {
  return (data[offset] * 0x1000000 + data[offset + 1] * 0x10000 + data[offset + 2] * 0x100 + data[offset + 3]) >>> 0;
}

function stfsText(data, offset, length) {
  var text = "";
  for (var i = 0; i < length; ++i) {
    var value = data[offset + i];
    text += String.fromCharCode(value < 0x80 ? value : 0x3f);
  }
  return text;
}

function stfsSafeName(name) {
  var normalized = name.replace(/\\/g, "/");
  if (!normalized || normalized.charAt(0) === "/" || /^[A-Za-z]:/.test(normalized)) return null;
  var parts = normalized.split("/");
  var safe = [];
  for (var i = 0; i < parts.length; ++i) {
    if (!parts[i] || parts[i] === "." || parts[i] === "..") return null;
    safe.push(parts[i]);
  }
  return safe;
}

function stfsJoinParts(root, parts) {
  var path = root;
  for (var i = 0; i < parts.length; ++i) path = kernelx.path.join(path, parts[i]);
  return path;
}

function stfsCreateReader(bytes) {
  var headerSize = 0;
  var blocksPerHashTable = 1;
  var rootActiveIndex = false;
  var totalBlocks = 0;

  function requireRange(offset, size, message) {
    if (!Number.isSafeInteger(offset) || offset < 0 || size < 0 || offset > bytes.length - size) throw new Error(message);
  }

  function dataBase() {
    return Math.ceil(headerSize / STFS_BLOCK_SIZE) * STFS_BLOCK_SIZE;
  }

  function getOffset(cluster) {
    if (!Number.isInteger(cluster) || cluster < 0 || cluster >= totalBlocks) throw new Error("Invalid STFS block number: " + cluster);
    var base = STFS_HASH_ENTRIES;
    var block = cluster;
    for (var level = 0; level < 3; ++level) {
      block += Math.floor((cluster + base) / base) * blocksPerHashTable;
      if (cluster < base) break;
      base *= STFS_HASH_ENTRIES;
    }
    var offset = dataBase() + block * STFS_BLOCK_SIZE;
    requireRange(offset, STFS_BLOCK_SIZE, "STFS block is outside the container");
    return offset;
  }

  function hashBlockNumber(cluster, level) {
    var step0 = STFS_HASH_ENTRIES + blocksPerHashTable;
    var step1 = STFS_HASH_ENTRIES * STFS_HASH_ENTRIES + (STFS_HASH_ENTRIES + 1) * blocksPerHashTable;
    if (level === 0) {
      if (cluster < STFS_HASH_ENTRIES) return 0;
      var block = Math.floor(cluster / STFS_HASH_ENTRIES) * step0;
      block += (Math.floor(cluster / (STFS_HASH_ENTRIES * STFS_HASH_ENTRIES)) + 1) * blocksPerHashTable;
      return cluster < STFS_HASH_ENTRIES * STFS_HASH_ENTRIES ? block : block + blocksPerHashTable;
    }
    if (level === 1) {
      if (cluster < STFS_HASH_ENTRIES * STFS_HASH_ENTRIES) return step0;
      return Math.floor(cluster / (STFS_HASH_ENTRIES * STFS_HASH_ENTRIES)) * step1 + blocksPerHashTable;
    }
    return step1;
  }

  function hashTableOffset(cluster, level) {
    var offset = dataBase() + hashBlockNumber(cluster, level) * STFS_BLOCK_SIZE;
    requireRange(offset, STFS_BLOCK_SIZE, "STFS hash table is outside the container");
    return offset;
  }

  function hashEntryActive(tableOffset, record) {
    var offset = tableOffset + record * STFS_HASH_ENTRY_SIZE + 0x14;
    requireRange(offset, 4, "Invalid STFS hash entry");
    return (stfsU32BE(bytes, offset) & 0x40000000) !== 0;
  }

  function nextCluster(cluster) {
    if (!Number.isInteger(cluster) || cluster < 0 || cluster >= totalBlocks) throw new Error("Invalid STFS block chain");
    var secondary = rootActiveIndex ? STFS_BLOCK_SIZE : 0;
    if (blocksPerHashTable === 2) {
      if (totalBlocks > STFS_HASH_ENTRIES * STFS_HASH_ENTRIES) {
        var level2 = hashTableOffset(cluster, 2) + secondary;
        secondary = hashEntryActive(level2, Math.floor(cluster / (STFS_HASH_ENTRIES * STFS_HASH_ENTRIES)) % STFS_HASH_ENTRIES) ? STFS_BLOCK_SIZE : 0;
      }
      if (totalBlocks > STFS_HASH_ENTRIES) {
        var level1 = hashTableOffset(cluster, 1) + secondary;
        secondary = hashEntryActive(level1, Math.floor(cluster / STFS_HASH_ENTRIES) % STFS_HASH_ENTRIES) ? STFS_BLOCK_SIZE : 0;
      }
    }
    var table = hashTableOffset(cluster, 0) + secondary;
    var offset = table + (cluster % STFS_HASH_ENTRIES) * STFS_HASH_ENTRY_SIZE + 0x15;
    requireRange(offset, 3, "Invalid STFS block-chain entry");
    return stfsU24BE(bytes, offset);
  }

  function parseEntry(offset) {
    requireRange(offset, 0x40, "Truncated STFS file table");
    var nameLength = bytes[offset + 40] & 0x3f;
    if (!nameLength) return null;
    if (nameLength > 40) throw new Error("Invalid STFS entry name length");
    return {
      name: stfsText(bytes, offset, nameLength),
      allocatedBlocks: stfsU24LE(bytes, offset + 44),
      cluster: stfsU24LE(bytes, offset + 47),
      parent: bytes[offset + 50] * 0x100 + bytes[offset + 51],
      size: stfsU32BE(bytes, offset + 52),
      isDirectory: (bytes[offset + 40] & 0x80) !== 0
    };
  }

  function initialize() {
    if (bytes.length < STFS_VOLUME_DESCRIPTOR_OFFSET + 0x24) throw new Error("Truncated PIRS/LIVE/CON container");
    var magic = stfsText(bytes, 0, 4);
    if (magic !== "PIRS" && magic !== "LIVE" && magic !== "CON ") throw new Error("Invalid PIRS/LIVE/CON signature");
    headerSize = stfsU32BE(bytes, 0x340);
    if (!headerSize || headerSize > bytes.length) throw new Error("Invalid STFS header size");
    var descriptor = STFS_VOLUME_DESCRIPTOR_OFFSET;
    if (bytes[descriptor] !== 0x24) throw new Error("Invalid STFS volume descriptor");
    var flags = bytes[descriptor + 2];
    blocksPerHashTable = (flags & 1) ? 1 : 2;
    rootActiveIndex = (flags & 2) !== 0;
    totalBlocks = stfsU32BE(bytes, descriptor + 28);
    if (!totalBlocks) throw new Error("STFS container has no data blocks");
    return {
      fileTableBlockCount: bytes[descriptor + 3] * 0x100 + bytes[descriptor + 4],
      fileTableBlock: stfsU24LE(bytes, descriptor + 5)
    };
  }

  return { bytes: bytes, initialize: initialize, getOffset: getOffset, nextCluster: nextCluster, parseEntry: parseEntry };
}

function stfsListEntries(reader) {
  var volume = reader.initialize();
  if (!volume.fileTableBlockCount) return [];
  var entries = [];
  var block = volume.fileTableBlock;
  var visited = {};
  for (var tableIndex = 0; tableIndex < volume.fileTableBlockCount; ++tableIndex) {
    if (visited[block]) throw new Error("STFS file-table block chain contains a loop");
    visited[block] = true;
    var offset = reader.getOffset(block);
    for (var i = 0; i < STFS_BLOCK_SIZE / 0x40; ++i) {
      var entry = reader.parseEntry(offset + i * 0x40);
      if (!entry) return entries;
      entries.push(entry);
    }
    block = reader.nextCluster(block);
    if (block === STFS_END_OF_CHAIN) break;
  }
  return entries;
}

function stfsEntryParts(index, entries, cache, resolving) {
  if (cache[index]) return cache[index];
  if (resolving[index]) throw new Error("STFS directory parents contain a loop");
  var entry = entries[index];
  var name = stfsSafeName(entry.name);
  if (!name) throw new Error("Unsafe STFS entry name: " + entry.name);
  resolving[index] = true;
  var parts = name;
  if (entry.parent !== 0xffff && entry.parent >= 0 && entry.parent < entries.length && entry.parent !== index) {
    parts = stfsEntryParts(entry.parent, entries, cache, resolving).concat(name);
  }
  delete resolving[index];
  cache[index] = parts;
  return parts;
}

function stfsExtractFile(reader, entry, output) {
  var writer = kernelx.io.openWriter(output);
  if (!writer) throw new Error("Unable to create " + output);
  var remaining = entry.size;
  var cluster = entry.cluster;
  var visited = {};
  var ok = true;
  try {
    while (remaining > 0) {
      if (visited[cluster]) throw new Error("STFS file block chain contains a loop: " + entry.name);
      visited[cluster] = true;
      var offset = reader.getOffset(cluster);
      var chunkSize = Math.min(remaining, STFS_BLOCK_SIZE);
      if (!writer.write(reader.bytes.subarray(offset, offset + chunkSize))) throw new Error("Unable to write " + output);
      remaining -= chunkSize;
      if (remaining > 0) {
        cluster = reader.nextCluster(cluster);
        if (cluster === STFS_END_OF_CHAIN) throw new Error("STFS file block chain ends early: " + entry.name);
      }
    }
  } catch (error) {
    ok = false;
    throw error;
  } finally {
    if (!writer.close() || writer.failed()) ok = false;
  }
  if (!ok) throw new Error("Unable to finish " + output);
}

function xbox360LiveUnpack(input, output) {
  if (!input || !kernelx.io.isFile(input)) return { success: false, error: "InputFile is required and must exist" };
  if (!output) return { success: false, error: "OutputFolder is required" };
  try {
    var mapped = kernelx.io.mmapRead(input);
    if (!mapped || !mapped.byteLength) return { success: false, error: "Unable to read InputFile" };
    var bytes = new Uint8Array(mapped);
    var reader = stfsCreateReader(bytes);
    var entries = stfsListEntries(reader);
    if (!entries.length) return { success: false, error: "No STFS entries were found" };
    if (!kernelx.io.mkdir(output)) return { success: false, error: "Unable to create OutputFolder" };

    var cache = {};
    var resolving = {};
    var fileCount = 0;
    for (var index = 0; index < entries.length; ++index) {
      var entry = entries[index];
      var target = stfsJoinParts(output, stfsEntryParts(index, entries, cache, resolving));
      if (entry.isDirectory) {
        if (!kernelx.io.mkdir(target)) throw new Error("Unable to create " + target);
      } else {
        if (!kernelx.io.mkdir(kernelx.path.parent(target))) throw new Error("Unable to create parent folder for " + target);
        stfsExtractFile(reader, entry, target);
        ++fileCount;
      }
    }
    return { success: true, output: output, entries: entries.length, files: fileCount };
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) };
  }
}

function execute(params) {
  return xbox360LiveUnpack(params.InputFile, params.OutputFolder);
}
