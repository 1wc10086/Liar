/* kernelx-manifest
{
  "id": "caf.decode",
  "implementation": "implementation",
  "buffer_size": "512m",
  "params": [
    { "name": "InputFile", "type": "path", "required": true, "extensions": [".caf", ".CAF"] },
    { "name": "OutputFile", "type": "path", "required": true, "extensions": [".wav", ".WAV"] },
    { "name": "FrameCapacity", "type": "int", "required": false, "default": 4800000, "min": 1, "max": 4294967295 }
  ]
}
*/

var CAF_DEFAULT_FRAME_CAPACITY = 4800000;
var CAF_MAX_FRAME_CAPACITY = 4294967295;
var CAF_LPCM_FLOAT = 1;
var CAF_LPCM_BIG_ENDIAN = 2;
var CAF_LPCM_SIGNED_INTEGER = 4;
var CAF_LPCM_PACKED = 8;
var CAF_LPCM_ALIGNED_HIGH = 16;
var CAF_LPCM_NON_INTERLEAVED = 32;

function cafError(message) { throw new Error("CAF: " + message); }

function cafFourCC(bytes, offset) {
  return String.fromCharCode(bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3]);
}

function cafU32(view, offset) { return view.getUint32(offset, false); }

function cafI64(view, offset) {
  var high = view.getInt32(offset, false);
  var low = view.getUint32(offset + 4, false);
  if (high < 0) return -1;
  if (high > 0x1fffff) cafError("chunk is too large for JavaScript");
  return high * 4294967296 + low;
}

function cafU64(view, offset) {
  var high = view.getUint32(offset, false);
  var low = view.getUint32(offset + 4, false);
  if (high > 0x1fffff) cafError("packet table value is too large for JavaScript");
  return high * 4294967296 + low;
}

function cafSlice(bytes, start, end) {
  if (start < 0 || end < start || end > bytes.length) cafError("truncated chunk");
  return bytes.slice(start, end);
}

function cafParse(bytes) {
  if (bytes.length < 8 || cafFourCC(bytes, 0) !== "caff") cafError("not a CAF file");
  var view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
  if (view.getUint16(4, false) !== 1) cafError("unsupported CAF version");
  var offset = 8;
  var chunks = {};
  while (offset < bytes.length) {
    if (bytes.length - offset < 12) cafError("truncated chunk header");
    var type = cafFourCC(bytes, offset);
    var size = cafI64(view, offset + 4);
    var dataOffset = offset + 12;
    if (size < 0) {
      if (type !== "data") cafError("indeterminate size is only valid for data");
      size = bytes.length - dataOffset;
    }
    if (size > bytes.length - dataOffset) cafError("chunk exceeds file size");
    if (!chunks[type]) chunks[type] = cafSlice(bytes, dataOffset, dataOffset + size);
    offset = dataOffset + size;
  }
  if (!chunks.desc || !chunks.data) cafError("missing desc or data chunk");
  if (chunks.desc.length !== 32) cafError("invalid desc chunk length");
  if (chunks.data.length < 4) cafError("invalid data chunk");

  var desc = new DataView(chunks.desc.buffer, chunks.desc.byteOffset, chunks.desc.byteLength);
  var sampleRate = desc.getFloat64(0, false);
  var format = cafFourCC(chunks.desc, 8);
  var info = {
    sampleRate: sampleRate,
    format: format,
    flags: cafU32(desc, 12),
    bytesPerPacket: cafU32(desc, 16),
    framesPerPacket: cafU32(desc, 20),
    channels: cafU32(desc, 24),
    bits: cafU32(desc, 28),
    data: chunks.data.slice(4),
    cookie: chunks.kuki || new Uint8Array(0),
    packetTable: chunks.pakt || new Uint8Array(0)
  };
  if (!Number.isFinite(sampleRate) || sampleRate <= 0 || sampleRate > 192000) cafError("invalid sample rate");
  if (!info.channels || info.channels > 8) cafError("unsupported channel count");
  return info;
}

function cafFrameCapacity(value) {
  var capacity = Number(value);
  if (!Number.isInteger(capacity) || capacity < 1) return CAF_DEFAULT_FRAME_CAPACITY;
  return Math.min(capacity, CAF_MAX_FRAME_CAPACITY);
}

function cafWriteWavPcm(data, sampleRate, channels, bits, format) {
  var blockAlign = channels * Math.ceil(bits / 8);
  if (!data.length || data.length > 0xffffffff - 44 || blockAlign > 0xffff) cafError("WAV output is too large or invalid");
  var output = new Uint8Array(44 + data.length);
  var view = new DataView(output.buffer);
  output.set([82, 73, 70, 70], 0);
  view.setUint32(4, output.length - 8, true);
  output.set([87, 65, 86, 69, 102, 109, 116, 32], 8);
  view.setUint32(16, 16, true);
  view.setUint16(20, format, true);
  view.setUint16(22, channels, true);
  view.setUint32(24, Math.round(sampleRate), true);
  view.setUint32(28, Math.round(sampleRate) * blockAlign, true);
  view.setUint16(32, blockAlign, true);
  view.setUint16(34, bits, true);
  output.set([100, 97, 116, 97], 36);
  view.setUint32(40, data.length, true);
  output.set(data, 44);
  return output;
}

function cafDecodeLpcm(info) {
  var bytesPerSample = Math.ceil(info.bits / 8);
  if (!bytesPerSample || info.bits > 64) cafError("invalid LPCM sample layout");
  if (info.flags & CAF_LPCM_NON_INTERLEAVED) cafError("non-interleaved LPCM CAF is not supported");
  if (info.bytesPerPacket && info.framesPerPacket) {
    var samplesPerPacket = info.framesPerPacket * info.channels;
    if (info.bytesPerPacket % samplesPerPacket) cafError("unsupported LPCM packet layout");
    bytesPerSample = info.bytesPerPacket / samplesPerPacket;
  }
  if (bytesPerSample !== Math.ceil(info.bits / 8) || !(info.flags & CAF_LPCM_PACKED) && info.bits % 8 || info.flags & CAF_LPCM_ALIGNED_HIGH) cafError("padded LPCM samples are not supported");
  if (info.data.length % (bytesPerSample * info.channels)) cafError("LPCM data does not contain complete frames");
  var bigEndian = (info.flags & CAF_LPCM_BIG_ENDIAN) !== 0;
  var floating = (info.flags & CAF_LPCM_FLOAT) !== 0;
  var signed = (info.flags & CAF_LPCM_SIGNED_INTEGER) !== 0;
  var source = new DataView(info.data.buffer, info.data.byteOffset, info.data.byteLength);
  var samples = info.data.length / bytesPerSample;
  var pcm = new Float32Array(samples);
  var i;
  for (i = 0; i < samples; ++i) {
    var offset = i * bytesPerSample;
    var value;
    if (floating) {
      if (info.bits === 32) value = source.getFloat32(offset, !bigEndian);
      else if (info.bits === 64) value = source.getFloat64(offset, !bigEndian);
      else cafError("unsupported LPCM floating-point bit depth");
    } else if (info.bits === 8) {
      value = signed ? source.getInt8(offset) / 128 : (source.getUint8(offset) - 128) / 128;
    } else if (info.bits === 16) {
      value = (signed ? source.getInt16(offset, !bigEndian) : source.getUint16(offset, !bigEndian) - 32768) / 32768;
    } else if (info.bits === 24) {
      var a = bigEndian ? source.getUint8(offset) : source.getUint8(offset + 2);
      var b = source.getUint8(offset + 1);
      var c = bigEndian ? source.getUint8(offset + 2) : source.getUint8(offset);
      var raw = a * 65536 + b * 256 + c;
      if (signed && raw >= 0x800000) raw -= 0x1000000;
      value = signed ? raw / 8388608 : (raw - 8388608) / 8388608;
    } else if (info.bits === 32) {
      value = (signed ? source.getInt32(offset, !bigEndian) : source.getUint32(offset, !bigEndian) - 2147483648) / 2147483648;
    } else cafError("unsupported LPCM integer bit depth");
    pcm[i] = value;
  }
  var wav = kernelx.audio.encodeWav(pcm, Math.round(info.sampleRate), info.channels);
  if (!wav) cafError("WAV encoding failed");
  return wav;
}

function cafMuLaw(value) {
  value = ~value & 255;
  var magnitude = ((value & 15) << 3) + 132;
  magnitude <<= (value >> 4) & 7;
  return (value & 128 ? 132 - magnitude : magnitude - 132) / 32768;
}

function cafALaw(value) {
  value ^= 85;
  var magnitude = (value & 15) << 4;
  var exponent = (value >> 4) & 7;
  magnitude += exponent ? 264 : 8;
  if (exponent > 1) magnitude <<= exponent - 1;
  return (value & 128 ? magnitude : -magnitude) / 32768;
}

function cafDecodeG711(info, decode) {
  if (info.data.length % info.channels) cafError("G.711 data does not contain complete frames");
  var pcm = new Float32Array(info.data.length);
  for (var i = 0; i < pcm.length; ++i) pcm[i] = decode(info.data[i]);
  var wav = kernelx.audio.encodeWav(pcm, Math.round(info.sampleRate), info.channels);
  if (!wav) cafError("WAV encoding failed");
  return wav;
}

function cafDecodeIma4(info) {
  if (info.bytesPerPacket && info.bytesPerPacket !== 34 * info.channels) cafError("unsupported ima4 packet layout");
  if (info.data.length % (34 * info.channels)) cafError("truncated ima4 packet");
  var indexTable = [-1, -1, -1, -1, 2, 4, 6, 8, -1, -1, -1, -1, 2, 4, 6, 8];
  var stepTable = [7, 8, 9, 10, 11, 12, 13, 14, 16, 17, 19, 21, 23, 25, 28, 31, 34, 37, 41, 45, 50, 55, 60, 66, 73, 80, 88, 97, 107, 118, 130, 143, 157, 173, 190, 209, 230, 253, 279, 307, 337, 371, 408, 449, 494, 544, 598, 658, 724, 796, 876, 963, 1060, 1166, 1282, 1411, 1552, 1707, 1878, 2066, 2272, 2499, 2749, 3024, 3327, 3660, 4026, 4428, 4871, 5358, 5894, 6484, 7132, 7845, 8630, 9493, 10442, 11487, 12635, 13899, 15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794, 32767];
  var packets = info.data.length / (34 * info.channels);
  var pcm = new Float32Array(packets * 64 * info.channels);
  for (var packet = 0; packet < packets; ++packet) {
    for (var channel = 0; channel < info.channels; ++channel) {
      var base = (packet * info.channels + channel) * 34;
      var predictor = ((info.data[base] << 8) | info.data[base + 1]) & 0xff80;
      if (predictor & 0x8000) predictor -= 0x10000;
      var index = info.data[base + 1] & 127;
      if (index > 88) index = 88;
      for (var sample = 0; sample < 64; ++sample) {
        var packed = info.data[base + 2 + (sample >> 1)];
        var code = sample & 1 ? packed & 15 : packed >> 4;
        var step = stepTable[index];
        var delta = step >> 3;
        if (code & 4) delta += step;
        if (code & 2) delta += step >> 1;
        if (code & 1) delta += step >> 2;
        predictor += code & 8 ? -delta : delta;
        if (predictor < -32768) predictor = -32768;
        if (predictor > 32767) predictor = 32767;
        index += indexTable[code];
        if (index < 0) index = 0;
        if (index > 88) index = 88;
        pcm[(packet * 64 + sample) * info.channels + channel] = predictor / 32768;
      }
    }
  }
  var wav = kernelx.audio.encodeWav(pcm, Math.round(info.sampleRate), info.channels);
  if (!wav) cafError("WAV encoding failed");
  return wav;
}

function cafPacketSizes(info) {
  if (info.bytesPerPacket) {
    if (info.data.length % info.bytesPerPacket) cafError("compressed data does not contain complete packets");
    var fixed = [];
    for (var p = 0; p < info.data.length; p += info.bytesPerPacket) fixed.push(info.bytesPerPacket);
    return fixed;
  }
  if (info.packetTable.length < 24) cafError("variable-size compressed CAF requires a pakt chunk");
  var table = new DataView(info.packetTable.buffer, info.packetTable.byteOffset, info.packetTable.byteLength);
  var count = cafU64(table, 0);
  if (count > 10000000) cafError("too many packets");
  var sizes = [];
  var offset = 24;
  for (var i = 0; i < count; ++i) {
    var value = 0;
    var groups = 0;
    while (true) {
      if (offset >= info.packetTable.length || groups++ === 10) cafError("invalid pakt packet size");
      var byte = info.packetTable[offset++];
      value = value * 128 + (byte & 127);
      if (value > 0xffffffff) cafError("packet is too large");
      if (!(byte & 128)) break;
    }
    sizes.push(value);
  }
  var total = 0;
  for (var j = 0; j < sizes.length; ++j) total += sizes[j];
  if (total !== info.data.length) cafError("pakt packet sizes do not match data length");
  return sizes;
}

function cafAdtsHeader(payloadSize, sampleRate, channels, cookie) {
  var rates = [96000, 88200, 64000, 48000, 44100, 32000, 24000, 22050, 16000, 12000, 11025, 8000, 7350];
  var index = rates.indexOf(Math.round(sampleRate));
  if (index < 0 || channels < 1 || channels > 7 || payloadSize > 8191) cafError("cannot build ADTS header for CAF AAC stream");
  var objectType = 2;
  if (cookie.length >= 2) objectType = ((cookie[0] >> 3) & 31) || 2;
  var length = payloadSize + 7;
  return new Uint8Array([255, 241, (objectType - 1) << 6 | index << 2 | channels >> 2, (channels & 3) << 6 | length >> 11, length >> 3 & 255, (length & 7) << 5 | 31, 252]);
}

function cafMakeAdts(info) {
  var sizes = cafPacketSizes(info);
  var total = info.data.length + sizes.length * 7;
  var output = new Uint8Array(total);
  var inputOffset = 0;
  var outputOffset = 0;
  for (var i = 0; i < sizes.length; ++i) {
    var header = cafAdtsHeader(sizes[i], info.sampleRate, info.channels, info.cookie);
    output.set(header, outputOffset);
    outputOffset += header.length;
    output.set(info.data.subarray(inputOffset, inputOffset + sizes[i]), outputOffset);
    inputOffset += sizes[i];
    outputOffset += sizes[i];
  }
  return output;
}

function cafDecodeNative(info, capacity) {
  var decoded;
  if (info.format === "alac") {
    decoded = kernelx.audio.alac(info.cookie, info.data, capacity);
    if (!decoded || !decoded.pcm || !decoded.sampleRate || !decoded.channels) cafError("ALAC decoding failed");
    return cafWriteWavPcm(new Uint8Array(decoded.pcm.buffer, decoded.pcm.byteOffset, decoded.pcm.byteLength), decoded.sampleRate, decoded.channels, 16, 1);
  }
  if (info.format === "aac ") decoded = kernelx.audio.aac(cafMakeAdts(info), capacity);
  else if (info.format === ".mp3" || info.format === "mp3 ") decoded = kernelx.audio.mp3(info.data, capacity, Math.round(info.sampleRate));
  else if (info.format === "flac") decoded = kernelx.audio.flac(info.data, capacity);
  else if (info.format === "vorb") decoded = kernelx.audio.vorbis(info.data, capacity, Math.round(info.sampleRate));
  else cafError("unsupported CAF audio format '" + info.format + "'");
  if (!decoded || !decoded.pcm || !decoded.sampleRate || !decoded.channels) cafError(info.format + " decoding failed; this CAF codec may require an external decoder");
  var wav = kernelx.audio.encodeWav(decoded.pcm, decoded.sampleRate, decoded.channels);
  if (!wav) cafError("WAV encoding failed");
  return wav;
}

function cafDecode(info, capacity) {
  if (info.format === "lpcm") return cafDecodeLpcm(info);
  if (info.format === "ulaw") return cafDecodeG711(info, cafMuLaw);
  if (info.format === "alaw") return cafDecodeG711(info, cafALaw);
  if (info.format === "ima4") return cafDecodeIma4(info);
  return cafDecodeNative(info, capacity);
}

function execute(params) {
  var input = params.InputFile;
  var output = params.OutputFile;
  if (!input || !kernelx.io.isFile(input)) return { success: false, error: "InputFile is required and must exist" };
  if (!output) return { success: false, error: "OutputFile is required" };
  try {
    if (!kernelx.audio.loaded()) {
      var status = kernelx.audio.status();
      throw new Error("Audio decoder is unavailable" + (status.error ? ": " + status.error : ""));
    }
    var source = kernelx.io.mmapRead(input);
    if (!source || !source.byteLength) cafError("input is empty or cannot be read");
    var info = cafParse(new Uint8Array(source));
    var wav = cafDecode(info, cafFrameCapacity(params.FrameCapacity));
    if (!kernelx.io.writeBytes(output, wav)) throw new Error("Unable to write " + output);
    return { success: true, output: output, format: info.format, sampleRate: Math.round(info.sampleRate), channels: info.channels };
  } catch (error) {
    return { success: false, error: error && error.message ? error.message : String(error) };
  }
}
