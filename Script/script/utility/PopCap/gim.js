/* kernelx-manifest
[
  {
    "id": "gim.decode",
    "implementation": "implementation",
    "params": [
      { "name": "InputFile", "type": "path", "required": true, "extensions": [".gim", ".GIM"] },
      { "name": "OutputFile", "type": "path", "required": true, "extensions": [".png", ".PNG"] }
    ]
  },
  {
    "id": "gim.encode",
    "implementation": "implementation",
    "params": [
      { "name": "InputFile", "type": "path", "required": true, "extensions": [".png", ".PNG"] },
      { "name": "OutputFile", "type": "path", "required": true, "extensions": [".gim", ".GIM"] }
    ]
  }
]
*/

function gimU16(bytes, p) {
  return bytes[p] | (bytes[p + 1] << 8);
}

function gimU32(bytes, p) {
  return (bytes[p] | (bytes[p + 1] << 8) | (bytes[p + 2] << 16) | (bytes[p + 3] << 24)) >>> 0;
}

function gimSection(bytes, p) {
  if (p < 0 || p + 0x10 > bytes.length) throw new Error("Truncated GIM section at 0x" + p.toString(16));
  var size = gimU32(bytes, p + 4);
  var dataSize = gimU32(bytes, p + 8);
  if (size < 0x10 || p + size > bytes.length) throw new Error("Invalid GIM section size at 0x" + p.toString(16));
  return { position: p, size: size, dataSize: dataSize, data: p + 0x10 };
}

function gimSections(bytes, p, end) {
  var out = [];
  while (p + 0x10 <= end) {
    var section = gimSection(bytes, p);
    out.push(section);
    p += section.size;
  }
  return out;
}

function gimBytesPerPixel(format) {
  var bits = format & 0xf;
  if (bits === 4) return 1;
  if (bits === 5) return 1;
  if (bits === 6) return 2;
  if (bits === 7) return 4;
  if (bits === 0 || bits === 1 || bits === 2) return 2;
  if (bits === 3) return 4;
  throw new Error("Unsupported GIM pixel format " + bits);
}

function gimExpand5(value) { return (value << 3) | (value >>> 2); }
function gimExpand6(value) { return (value << 2) | (value >>> 4); }

function gimColor(bytes, p, format) {
  var bits = format & 0xf;
  var r, g, b, a;
  if (bits === 0) return [0, 0, 0, 0];
  if (bits === 3) {
    r = bytes[p]; g = bytes[p + 1]; b = bytes[p + 2]; a = bytes[p + 3];
  } else {
    var value = gimU16(bytes, p);
    if (bits === 1) {
      r = gimExpand5(value & 0x1f); g = gimExpand5((value >>> 5) & 0x1f);
      b = gimExpand5((value >>> 10) & 0x1f); a = (value & 0x8000) ? 255 : 0;
    } else if (bits === 2) {
      r = gimExpand4((value >>> 0) & 0xf); g = gimExpand4((value >>> 4) & 0xf);
      b = gimExpand4((value >>> 8) & 0xf); a = gimExpand4((value >>> 12) & 0xf);
    } else {
      r = gimExpand5((value >>> 11) & 0x1f); g = gimExpand6((value >>> 5) & 0x3f);
      b = gimExpand5(value & 0x1f); a = 255;
    }
  }
  return [r, g, b, a];
}

function gimExpand4(value) { return value * 17; }

function gimUnswizzle(indices, width, height) {
  var out = new Uint8Array(width * height);
  var rowBytes = Math.ceil(width / 16) * 16;
  var src = 0;
  for (var blockY = 0; blockY < height; blockY += 8) {
    for (var blockX = 0; blockX < rowBytes; blockX += 16) {
      for (var y = 0; y < 8; ++y) {
        for (var x = 0; x < 16; ++x) {
          if (src >= indices.length) throw new Error("Truncated GIM swizzled image data");
          if (blockY + y < height && blockX + x < width) out[(blockY + y) * width + blockX + x] = indices[src];
          ++src;
        }
      }
    }
  }
  return out;
}

function gimSwizzle(indices, width, height) {
  var blockedWidth = Math.ceil(width / 16) * 16;
  var blockedHeight = Math.ceil(height / 8) * 8;
  var out = new Uint8Array(blockedWidth * blockedHeight);
  var p = 0;
  for (var blockY = 0; blockY < blockedHeight; blockY += 8) {
    for (var blockX = 0; blockX < blockedWidth; blockX += 16) {
      for (var y = 0; y < 8; ++y) for (var x = 0; x < 16; ++x) {
        var sx = blockX + x, sy = blockY + y;
        out[p++] = sx < width && sy < height ? indices[sy * width + sx] : 0;
      }
    }
  }
  return out;
}

function gimPutU16(bytes, p, value) {
  bytes[p] = value & 0xff;
  bytes[p + 1] = (value >>> 8) & 0xff;
}

function gimPutU32(bytes, p, value) {
  bytes[p] = value & 0xff;
  bytes[p + 1] = (value >>> 8) & 0xff;
  bytes[p + 2] = (value >>> 16) & 0xff;
  bytes[p + 3] = (value >>> 24) & 0xff;
}

function gimPutSection(bytes, p, id, size) {
  gimPutU32(bytes, p, id);
  gimPutU32(bytes, p + 4, size);
  gimPutU32(bytes, p + 8, size);
  gimPutU32(bytes, p + 12, 0x10);
}

function gimColorDistance(a, r, g, b, alpha) {
  var dr = a[0] - r, dg = a[1] - g, db = a[2] - b, da = a[3] - alpha;
  return dr * dr + dg * dg + db * db + da * da;
}

function gimPalette(pixels) {
  var palette = [], lookup = new Map(), indices = new Uint8Array(pixels.length / 4);
  for (var p = 0; p < indices.length; ++p) {
    var q = p * 4, r = pixels[q], g = pixels[q + 1], b = pixels[q + 2], a = pixels[q + 3];
    var key = r + "," + g + "," + b + "," + a;
    var index = lookup.get(key);
    if (index === undefined) {
      if (palette.length < 256) {
        index = palette.length;
        palette.push([r, g, b, a]);
        lookup.set(key, index);
      } else {
        index = 0;
        var best = Infinity;
        for (var i = 0; i < palette.length; ++i) {
          var distance = gimColorDistance(palette[i], r, g, b, a);
          if (distance < best) { best = distance; index = i; }
        }
      }
    }
    indices[p] = index;
  }
  while (palette.length < 256) palette.push([0, 0, 0, 0]);
  return { palette: palette, indices: indices };
}

function gimEncode(image) {
  if (!image || !image.width || !image.height || !image.pixels) throw new Error("Invalid PNG image");
  if (image.width > 65535 || image.height > 65535) throw new Error("GIM image dimensions are too large");
  var indexed = gimPalette(image.pixels);
  var pixels = gimSwizzle(indexed.indices, image.width, image.height);
  var paletteSize = 0x450;
  var imageSize = 0x50 + pixels.length;
  var containerSize = 0x10 + paletteSize + imageSize;
  var fileSize = 0x20 + containerSize;
  var bytes = new Uint8Array(fileSize);

  bytes[0] = 0x4d; bytes[1] = 0x49; bytes[2] = 0x47; bytes[3] = 0x2e;
  bytes[4] = 0x30; bytes[5] = 0x30; bytes[6] = 0x2e; bytes[7] = 0x31;
  bytes[8] = 0x50; bytes[9] = 0x53; bytes[10] = 0x50;
  gimPutSection(bytes, 0x10, 2, fileSize - 0x10);
  gimPutSection(bytes, 0x20, 3, containerSize);

  var paletteOffset = 0x30;
  gimPutSection(bytes, paletteOffset, 5, paletteSize);
  gimPutU32(bytes, paletteOffset + 0x10, 0x30);
  gimPutU16(bytes, paletteOffset + 0x14, 3);
  gimPutU16(bytes, paletteOffset + 0x18, 256);
  gimPutU16(bytes, paletteOffset + 0x1a, 1);
  for (var i = 0; i < indexed.palette.length; ++i) {
    var color = indexed.palette[i], cp = paletteOffset + 0x50 + i * 4;
    bytes[cp] = color[0]; bytes[cp + 1] = color[1]; bytes[cp + 2] = color[2]; bytes[cp + 3] = color[3];
  }

  var imageOffset = paletteOffset + paletteSize;
  gimPutSection(bytes, imageOffset, 4, imageSize);
  gimPutU32(bytes, imageOffset + 0x10, 0x30);
  gimPutU16(bytes, imageOffset + 0x14, 5);
  gimPutU16(bytes, imageOffset + 0x16, 1);
  gimPutU16(bytes, imageOffset + 0x18, image.width);
  gimPutU16(bytes, imageOffset + 0x1a, image.height);
  gimPutU16(bytes, imageOffset + 0x1c, Math.ceil(image.width / 16) * 16);
  gimPutU16(bytes, imageOffset + 0x1e, Math.ceil(image.height / 8) * 8);
  bytes.set(pixels, imageOffset + 0x50);
  return bytes;
}

function gimDecode(bytes) {
  if (!(bytes instanceof Uint8Array) || bytes.length < 0x40) throw new Error("Invalid or truncated GIM file");
  var signature = String.fromCharCode.apply(null, bytes.subarray(0, 11));
  if (signature !== "MIG.00.1PSP") throw new Error("Invalid GIM signature");

  var root = gimSection(bytes, 0x10);
  var container = gimSection(bytes, root.data);
  var top = gimSections(bytes, container.data, Math.min(bytes.length, container.position + container.size));
  var paletteSection = null;
  var imageSection = null;
  for (var i = 0; i < top.length; ++i) {
    var child = top[i];
      var format = gimU32(bytes, child.position + 0x14);
      var dimensions = gimU32(bytes, child.position + 0x18);
      var width = dimensions & 0xffff;
      var height = dimensions >>> 16;
      if (width && height && (format & 0xf) === 3) paletteSection = { section: child, format: format, width: width, height: height };
      if (width && height && (format & 0xf) !== 0) imageSection = { section: child, format: format, width: width, height: height };
  }
  if (!imageSection) throw new Error("GIM image section not found");

  var width = imageSection.width, height = imageSection.height;
  var format = imageSection.format;
  var bpp = gimBytesPerPixel(format);
  var image = imageSection.section;
  var imageFrameOffset = gimU16(bytes, image.position + 0x10);
  var imageDataOffset = image.position + 0x20 + imageFrameOffset;
  var imageData = bytes.subarray(imageDataOffset, Math.min(bytes.length, imageDataOffset + Math.ceil(width / 16) * 16 * Math.ceil(height / 8) * 8 * bpp));
  var palette = paletteSection ? paletteSection.section : null;
  var paletteBytes = palette ? bytes.subarray(palette.position + 0x50, Math.min(bytes.length, palette.position + 0x50 + paletteSection.width * paletteSection.height * 4)) : null;
  var rgba = new Uint8Array(width * height * 4);

  if ((format & 0xf) < 4) {
    if (imageData.length < width * height * bpp) throw new Error("Truncated GIM image data");
    for (var y = 0; y < height; ++y) for (var x = 0; x < width; ++x) {
      var color = gimColor(imageData, (y * width + x) * bpp, format);
      var d = (y * width + x) * 4;
      rgba[d] = color[0]; rgba[d + 1] = color[1]; rgba[d + 2] = color[2]; rgba[d + 3] = color[3];
    }
  } else if ((format & 0xf) === 4 || (format & 0xf) === 5) {
    if (!paletteBytes) throw new Error("GIM indexed image has no palette");
    if (paletteBytes.length < 4) throw new Error("Truncated GIM palette");
    var indices = gimUnswizzle(imageData, width, height);
    if ((format & 0xf) === 4) {
      var unpacked = new Uint8Array(width * height);
      for (var nibble = 0; nibble < unpacked.length; ++nibble) {
        var packed = imageData[Math.floor(nibble / 2)];
        unpacked[nibble] = (nibble & 1) ? packed >>> 4 : packed & 0xf;
      }
      indices = gimUnswizzle(unpacked, width, height);
    }
    for (var p = 0; p < indices.length; ++p) {
      var color = gimColor(paletteBytes, indices[p] * 4, 3);
      rgba[p * 4] = color[0]; rgba[p * 4 + 1] = color[1]; rgba[p * 4 + 2] = color[2]; rgba[p * 4 + 3] = color[3];
    }
  } else throw new Error("Unsupported GIM image format " + (format & 0xf));
  return { width: width, height: height, pixels: rgba };
}

function execute(params) {
  var input = params.InputFile, output = params.OutputFile;
  if (!input || !kernelx.io.isFile(input)) return { success: false, error: "InputFile is required and must exist" };
  if (!output) return { success: false, error: "OutputFile is required" };
  try {
    if (/\.gim$/i.test(input)) {
      var image = gimDecode(new Uint8Array(kernelx.io.mmapRead(input)));
      var png = kernelx.png.encode(image.width, image.height, image.pixels);
      if (!png || !kernelx.io.writeBytes(output, png)) return { success: false, error: "Unable to write " + output };
      return { success: true, output: output, width: image.width, height: image.height, bytes: png.length };
    }
    var decoded = kernelx.png.read(input);
    if (!decoded || !decoded.width || !decoded.height || !decoded.pixels) return { success: false, error: "Unable to read PNG " + input };
    var gim = gimEncode({ width: decoded.width, height: decoded.height, pixels: new Uint8Array(decoded.pixels) });
    if (!kernelx.io.writeBytes(output, gim)) return { success: false, error: "Unable to write " + output };
    return { success: true, output: output, width: decoded.width, height: decoded.height, bytes: gim.length };
  } catch (e) {
    return { success: false, error: e && e.message ? e.message : String(e) };
  }
}
