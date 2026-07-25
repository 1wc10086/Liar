/* kernelx-manifest
[
  {
    "id": "sps.encode",
    "implementation": "implementation",
    "buffer_size": "512m",
    "params": [
      { "name": "InputFile", "type": "path", "required": true, "extensions": [".wav", ".WAV"] },
      { "name": "OutputFile", "type": "path", "required": true, "extensions": [".sps", ".SPS"] }
    ]
  },
  {
    "id": "sps.decode",
    "implementation": "implementation",
    "buffer_size": "512m",
    "params": [
      { "name": "InputFile", "type": "path", "required": true, "extensions": [".sps", ".SPS"] },
      { "name": "OutputFile", "type": "path", "required": true, "extensions": [".wav", ".WAV"] }
    ]
  },
  {
    "id": "snr.encode",
    "implementation": "implementation",
    "buffer_size": "512m",
    "params": [
      { "name": "InputFile", "type": "path", "required": true, "extensions": [".wav", ".WAV"] },
      { "name": "OutputFile", "type": "path", "required": true, "extensions": [".snr", ".SNR"] }
    ]
  },
  {
    "id": "snr.decode",
    "implementation": "implementation",
    "buffer_size": "512m",
    "params": [
      { "name": "InputFile", "type": "path", "required": true, "extensions": [".snr", ".SNR"] },
      { "name": "OutputFile", "type": "path", "required": true, "extensions": [".wav", ".WAV"] }
    ]
  }
]
*/

var XAS_COEFFICIENTS = [[0, 0], [240, 0], [460, -208], [392, -220]];

function xasError(message) { throw new Error("SPS/SNR: " + message); }
function xasClip16(value) { return value >= 32767 ? 32767 : value <= -32768 ? -32768 : value; }
function xasClip4(value) { return value >= 7 ? 7 : value <= -8 ? -8 : value; }
function xasSign4(value) { value &= 15; return value & 8 ? value - 16 : value; }
function xasSign12(value) { return value & 2048 ? value - 4096 : value; }
function xasFourCC(bytes, offset) { return String.fromCharCode(bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3]); }
function xasU32BE(view, offset) { return view.getUint32(offset, false); }
function xasHasExtension(path, extension) { return typeof path === "string" && path.toLowerCase().slice(-extension.length) === extension; }

function xasConcat(parts, length) {
  var output = new Uint8Array(length);
  var offset = 0;
  for (var i = 0; i < parts.length; ++i) { output.set(parts[i], offset); offset += parts[i].length; }
  return output;
}

function xasBitReader(bytes, offset) { return { bytes: bytes, bit: offset * 8 }; }
function xasReadBits(reader, count) {
  var value = 0;
  while (count--) value = value * 2 + ((reader.bytes[reader.bit >> 3] >> (7 - (reader.bit & 7))) & 1), ++reader.bit;
  return value;
}
function xasBitWriter(size) { return { bytes: new Uint8Array(size), bit: 0 }; }
function xasWriteBits(writer, count, value) {
  for (var bit = count - 1; bit >= 0; --bit) {
    if (Math.floor(value / Math.pow(2, bit)) & 1) writer.bytes[writer.bit >> 3] |= 1 << (7 - (writer.bit & 7));
    ++writer.bit;
  }
}
function xasCopyBits(writer, reader, count) { while (count--) xasWriteBits(writer, 1, xasReadBits(reader, 1)); }

function xasParseLayer3Frame(data, offset) {
  if (offset + 2 > data.length) xasError("truncated EALayer3 frame");
  var view = new DataView(data.buffer, data.byteOffset, data.byteLength), first = view.getUint16(offset, false);
  var frameSize = first & 4095, extended = (first & 32768) !== 0, stereoFlag = (first >> 14) & 1, preSize = extended ? 6 : 2;
  if (!frameSize || offset + frameSize > data.length || frameSize < preSize + 1) xasError("invalid EALayer3 frame size");
  var pcmSamples = 0, commonSizeHint = 0, offsetMode = 0, offsetSamples = 0;
  if (extended) {
    var extension = view.getUint32(offset + 2, false);
    offsetMode = extension >>> 30; offsetSamples = (extension >>> 20) & 1023; pcmSamples = (extension >>> 10) & 1023; commonSizeHint = extension & 1023;
  }
  var reader = xasBitReader(data, offset + preSize), versionIndex = xasReadBits(reader, 2), rateIndex = xasReadBits(reader, 2);
  var channelMode = xasReadBits(reader, 2), modeExtension = xasReadBits(reader, 2), granule = xasReadBits(reader, 1);
  var versions = [3, -1, 2, 1], rates = [[11025, 12000, 8000], [], [22050, 24000, 16000], [44100, 48000, 32000]];
  var version = versions[versionIndex], sampleRate = rates[versionIndex][rateIndex], channels = channelMode === 3 ? 1 : 2, mpeg1 = version === 1;
  if (!sampleRate || version < 0) xasError("invalid EALayer3 MPEG parameters");
  var scfsi = [];
  if (mpeg1 && granule === 1) for (var c = 0; c < channels; ++c) scfsi[c] = xasReadBits(reader, 4);
  var mainBits = [], others1 = [], others2 = [], otherBits = mpeg1 ? 15 : 19, dataBits = 0;
  for (var channel = 0; channel < channels; ++channel) {
    mainBits[channel] = xasReadBits(reader, 12); others1[channel] = xasReadBits(reader, 32); others2[channel] = xasReadBits(reader, otherBits); dataBits += mainBits[channel];
  }
  var dataBit = reader.bit, paddingBits = (8 - ((reader.bit - (offset + preSize) * 8 + dataBits) & 7)) & 7;
  reader.bit += dataBits + paddingBits;
  var commonSize = (reader.bit - (offset + preSize) * 8) / 8, pcmSize = pcmSamples * channels * 2;
  if (commonSizeHint && commonSizeHint !== commonSize || preSize + commonSize + pcmSize !== frameSize) xasError("invalid EALayer3 frame layout");
  return { data: data, offset: offset, frameSize: frameSize, preSize: preSize, pcmSamples: pcmSamples, pcmOffset: offset + preSize + commonSize, offsetMode: offsetMode, offsetSamples: offsetSamples, versionIndex: versionIndex, sampleRate: sampleRate, rateIndex: rateIndex, channelMode: channelMode, modeExtension: modeExtension, granule: granule, channels: channels, mpeg1: mpeg1, scfsi: scfsi, mainBits: mainBits, others1: others1, others2: others2, otherBits: otherBits, dataBit: dataBit };
}

function xasRebuildLayer3Frame(first, second) {
  if (first.mpeg1 && (!second || first.granule === second.granule || first.channels !== second.channels || first.sampleRate !== second.sampleRate)) xasError("unpaired EALayer3 granules");
  var frameSize = Math.floor((first.mpeg1 ? 144 * 640000 : 72 * 320000) / first.sampleRate), writer = xasBitWriter(frameSize);
  xasWriteBits(writer, 11, 2047); xasWriteBits(writer, 2, first.versionIndex); xasWriteBits(writer, 2, 1); xasWriteBits(writer, 1, 1);
  xasWriteBits(writer, 4, 0); xasWriteBits(writer, 2, first.rateIndex); xasWriteBits(writer, 1, 0); xasWriteBits(writer, 1, 0);
  xasWriteBits(writer, 2, first.channelMode); xasWriteBits(writer, 2, first.modeExtension); xasWriteBits(writer, 1, 1); xasWriteBits(writer, 1, 1); xasWriteBits(writer, 2, 0);
  if (first.mpeg1) {
    xasWriteBits(writer, 9, 0); xasWriteBits(writer, first.channels === 1 ? 5 : 3, 0);
    for (var c = 0; c < second.channels; ++c) xasWriteBits(writer, 4, second.scfsi[c]);
  } else { xasWriteBits(writer, 8, 0); xasWriteBits(writer, first.channels === 1 ? 1 : 2, 0); }
  var frames = first.mpeg1 ? [first, second] : [first];
  for (var f = 0; f < frames.length; ++f) for (var channel = 0; channel < frames[f].channels; ++channel) {
    xasWriteBits(writer, 12, frames[f].mainBits[channel]); xasWriteBits(writer, 32, frames[f].others1[channel]); xasWriteBits(writer, frames[f].otherBits, frames[f].others2[channel]);
  }
  for (var d = 0; d < frames.length; ++d) {
    var reader = { bytes: frames[d].data, bit: frames[d].dataBit };
    for (var ch = 0; ch < frames[d].channels; ++ch) xasCopyBits(writer, reader, frames[d].mainBits[ch]);
  }
  if (writer.bit > frameSize * 8) xasError("EALayer3 frame exceeds free-format MPEG capacity");
  return writer.bytes;
}

function xasDecodeLayer3(data, header) {
  if (!kernelx.audio || !kernelx.audio.loaded()) xasError("EALayer3 decoding requires the audio decoder");
  var frames = [], offset = 0, parts = [], length = 0;
  while (offset < data.length) { var frame = xasParseLayer3Frame(data, offset); frames.push(frame); offset += frame.frameSize; }
  for (var i = 0; i < frames.length;) {
    var second = frames[i].mpeg1 ? frames[i + 1] : null;
    if (frames[i].mpeg1 && !second) {
      second = Object.assign({}, frames[i]); second.granule = frames[i].granule ^ 1; second.scfsi = frames[i].scfsi.slice();
    }
    var rebuilt = xasRebuildLayer3Frame(frames[i], second);
    parts.push(rebuilt); length += rebuilt.length; i += frames[i].mpeg1 ? 2 : 1;
  }
  var decoded = kernelx.audio.mp3(xasConcat(parts, length), header.samples + 2304, header.sampleRate);
  if (!decoded || !decoded.pcm || decoded.channels !== header.channelConfig + 1) xasError("EALayer3 MPEG decoding failed");
  var channels = decoded.channels, output = new Float32Array(header.samples * channels), sourceFrames = decoded.pcm.length / channels;
  var copyFrames = Math.min(header.samples, sourceFrames);
  output.set(decoded.pcm.subarray(0, copyFrames * channels));
  for (var f = 0; f < frames.length; ++f) if (frames[f].pcmSamples) {
    if (frames[f].channels !== channels || frames[f].offsetMode !== 0) xasError("unsupported EALayer3 PCM patch layout");
    var destinationFrame = f * 576, pcmView = new DataView(data.buffer, data.byteOffset + frames[f].pcmOffset, frames[f].pcmSamples * channels * 2);
    for (var sample = 0; sample < frames[f].pcmSamples && destinationFrame + sample < header.samples; ++sample) {
      for (var channel = 0; channel < channels; ++channel) output[(destinationFrame + sample) * channels + channel] = pcmView.getInt16((sample * channels + channel) * 2, false) / 32768;
    }
  }
  var wav = kernelx.audio.encodeWav(output, header.sampleRate, channels);
  if (!wav) xasError("EALayer3 WAV encoding failed");
  return wav;
}

function xasParseWav(bytes) {
  if (bytes.length < 12 || xasFourCC(bytes, 0) !== "RIFF" || xasFourCC(bytes, 8) !== "WAVE") xasError("input is not a RIFF/WAVE file");
  var view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
  var offset = 12;
  var format = null;
  var data = null;
  while (offset + 8 <= bytes.length) {
    var id = xasFourCC(bytes, offset);
    var size = view.getUint32(offset + 4, true);
    var start = offset + 8;
    if (size > bytes.length - start) xasError("truncated WAV chunk");
    if (id === "fmt " && format === null) format = bytes.slice(start, start + size);
    else if (id === "data" && data === null) data = bytes.slice(start, start + size);
    offset = start + size + (size & 1);
  }
  if (!format || format.length < 16 || !data) xasError("WAV must contain fmt and data chunks");
  var fmt = new DataView(format.buffer, format.byteOffset, format.byteLength);
  var audioFormat = fmt.getUint16(0, true);
  var channels = fmt.getUint16(2, true);
  var sampleRate = fmt.getUint32(4, true);
  var bits = fmt.getUint16(14, true);
  if (audioFormat !== 1 || bits !== 16) xasError("input must be 16-bit PCM WAV");
  if (!channels || channels > 16 || !sampleRate || sampleRate > 262143 || data.length % (channels * 2)) xasError("invalid PCM WAV layout");
  var sampleCount = data.length / 2;
  var pcm = new Int16Array(sampleCount);
  for (var i = 0; i < sampleCount; ++i) pcm[i] = new DataView(data.buffer, data.byteOffset + i * 2, 2).getInt16(0, true);
  return { channels: channels, sampleRate: sampleRate, samples: data.length / (channels * 2), pcm: pcm };
}

function xasMakeWav(pcm, sampleRate, channels) {
  if (!channels || !sampleRate || pcm.length % channels) xasError("invalid decoded audio metadata");
  var output = new Uint8Array(44 + pcm.byteLength);
  var view = new DataView(output.buffer);
  output.set([82, 73, 70, 70], 0); view.setUint32(4, output.length - 8, true);
  output.set([87, 65, 86, 69, 102, 109, 116, 32], 8); view.setUint32(16, 16, true);
  view.setUint16(20, 1, true); view.setUint16(22, channels, true); view.setUint32(24, sampleRate, true);
  view.setUint32(28, sampleRate * channels * 2, true); view.setUint16(32, channels * 2, true); view.setUint16(34, 16, true);
  output.set([100, 97, 116, 97], 36); view.setUint32(40, pcm.byteLength, true);
  output.set(new Uint8Array(pcm.buffer, pcm.byteOffset, pcm.byteLength), 44);
  return output;
}

function xasEncodeSample(previous, coefficients, sample, shift) {
  var prediction = previous[1] * coefficients[0] + previous[0] * coefficients[1];
  var encoded = xasClip4(((((sample << 8) - prediction) + (1 << (shift - 1))) >> shift));
  var predecoded = ((encoded << shift) + prediction + 128) >> 8;
  var decoded = xasClip16(predecoded);
  var term = 1 << (shift - 8);
  var alternative = xasClip16(predecoded + term);
  if (encoded !== 7 && Math.abs(decoded - sample) > Math.abs(alternative - sample)) { ++encoded; decoded = alternative; }
  else {
    alternative = xasClip16(predecoded - term);
    if (encoded !== -8 && Math.abs(decoded - sample) > Math.abs(alternative - sample)) { --encoded; decoded = alternative; }
  }
  return { encoded: encoded, decoded: decoded };
}

function xasChooseParameters(samples, previous) {
  var best = 0, minimum = 2147483647, signedError = 0;
  for (var coefficient = 0; coefficient < 4; ++coefficient) {
    var old = previous[0], recent = previous[1], maximum = 0, sign = 0, coef = XAS_COEFFICIENTS[coefficient];
    for (var i = 0; i < 30; ++i) {
      var error = (samples[i + 2] << 8) - coef[0] * recent - coef[1] * old;
      if (Math.abs(error) > maximum) { maximum = Math.abs(error); sign = error; }
      old = recent; recent = samples[i + 2];
    }
    if (maximum < minimum) { minimum = maximum; best = coefficient; signedError = sign; }
  }
  var clipped = xasClip16(minimum >> 8), mask = 16384, exponent;
  for (exponent = 0; exponent < 12; ++exponent) { if ((((mask >> 3) + clipped) & mask) !== 0) break; mask >>= 1; }
  return { coefficient: best, exponent: exponent };
}

function xasEncodeChunk(samples) {
  var chunk = new Uint8Array(76), view = new DataView(chunk.buffer);
  for (var subchunk = 0; subchunk < 4; ++subchunk) {
    var base = subchunk * 32, first = (samples[base] + 7) >> 4, second = (samples[base + 1] + 7) >> 4;
    var parameters = xasChooseParameters(samples.slice(base, base + 32), [first << 4, second << 4]);
    view.setUint16(subchunk * 4, ((first & 4095) << 4) | parameters.coefficient, true);
    view.setUint16(subchunk * 4 + 2, ((second & 4095) << 4) | parameters.exponent, true);
    var previous = [first << 4, second << 4], coef = XAS_COEFFICIENTS[parameters.coefficient], shift = 20 - parameters.exponent;
    for (var pair = 0; pair < 15; ++pair) {
      var one = xasEncodeSample(previous, coef, samples[base + 2 + pair * 2], shift); previous = [previous[1], one.decoded];
      var two = xasEncodeSample(previous, coef, samples[base + 3 + pair * 2], shift); previous = [previous[1], two.decoded];
      chunk[16 + pair * 4 + subchunk] = ((one.encoded & 15) << 4) | (two.encoded & 15);
    }
  }
  return chunk;
}

function xasEncode(pcm, samples, channels) {
  var chunks = Math.ceil(samples / 128), output = new Uint8Array(chunks * channels * 76), block = new Int16Array(128), offset = 0;
  for (var chunk = 0; chunk < chunks; ++chunk) for (var channel = 0; channel < channels; ++channel) {
    block.fill(0);
    var count = Math.min(128, samples - chunk * 128);
    for (var sample = 0; sample < count; ++sample) block[sample] = pcm[(chunk * 128 + sample) * channels + channel];
    output.set(xasEncodeChunk(block), offset); offset += 76;
  }
  return output;
}

function xasDecodeChunk(bytes, offset, destination, destinationOffset, channel, channels, count) {
  var view = new DataView(bytes.buffer, bytes.byteOffset + offset, 76);
  for (var subchunk = 0; subchunk < 4; ++subchunk) {
    var data1 = view.getUint16(subchunk * 4, true), data2 = view.getUint16(subchunk * 4 + 2, true);
    var previous = [xasSign12(data1 >> 4) << 4, xasSign12(data2 >> 4) << 4], coef = XAS_COEFFICIENTS[data1 & 3], shift = 20 - (data2 & 15);
    var base = subchunk * 32;
    if (base < count) destination[(destinationOffset + base) * channels + channel] = previous[0];
    if (base + 1 < count) destination[(destinationOffset + base + 1) * channels + channel] = previous[1];
    for (var pair = 0; pair < 15; ++pair) {
      var packed = bytes[offset + 16 + pair * 4 + subchunk];
      for (var nibble = 0; nibble < 2; ++nibble) {
        var correction = xasSign4(nibble ? packed : packed >> 4) << shift;
        var decoded = xasClip16((previous[1] * coef[0] + previous[0] * coef[1] + correction + 128) >> 8);
        var index = base + 2 + pair * 2 + nibble;
        if (index < count) destination[(destinationOffset + index) * channels + channel] = decoded;
        previous = [previous[1], decoded];
      }
    }
  }
}

function xasDecode(data, samples, channels) {
  var chunks = Math.ceil(samples / 128), required = chunks * channels * 76;
  if (data.length < required) xasError("XAS data is truncated");
  var pcm = new Int16Array(samples * channels), offset = 0;
  for (var chunk = 0; chunk < chunks; ++chunk) for (var channel = 0; channel < channels; ++channel) {
    xasDecodeChunk(data, offset, pcm, chunk * 128, channel, channels, Math.min(128, samples - chunk * 128)); offset += 76;
  }
  return pcm;
}

function xasHeader(view, offset) {
  var first = xasU32BE(view, offset), second = xasU32BE(view, offset + 4);
  return {
    codec: (first >>> 24) & 15,
    channelConfig: (first >>> 18) & 63,
    sampleRate: first & 262143,
    samples: second & 536870911
  };
}

function xasChannels(header, payloadLength) {
  var bytesPerChannel = Math.ceil(header.samples / 128) * 76;
  if (!bytesPerChannel || payloadLength % bytesPerChannel) xasError("XAS data length does not match the EAAC sample count");
  var channels = payloadLength / bytesPerChannel;
  if (!channels || channels > 16) xasError("unsupported EAAC channel count");
  return channels;
}

function xasEncodeSnr(wav) {
  var xas = xasEncode(wav.pcm, wav.samples, wav.channels), output = new Uint8Array(16 + xas.length), view = new DataView(output.buffer);
  view.setUint32(0, (4 << 24) | ((wav.channels - 1) << 18) | wav.sampleRate, false);
  view.setUint32(4, wav.samples, false); view.setUint32(8, xas.length + 8, false); view.setUint32(12, wav.samples, false); output.set(xas, 16);
  return output;
}

function xasEncodeSps(wav) {
  var xas = xasEncode(wav.pcm, wav.samples, wav.channels), parts = [], length = 12, offset = 0, remaining = wav.samples, chunkSize = 2736 * wav.channels;
  var header = new Uint8Array(12), view = new DataView(header.buffer);
  view.setUint32(0, 0x4800000c, false); view.setUint32(4, (0x14 << 24) | ((wav.channels - 1) << 18) | wav.sampleRate, false); view.setUint32(8, (wav.samples | 0x40000000) >>> 0, false); parts.push(header);
  while (offset < xas.length) {
    var size = Math.min(chunkSize, xas.length - offset), block = new Uint8Array(size + 8), blockView = new DataView(block.buffer);
    blockView.setUint32(0, (0x44000000 | (size + 8)) >>> 0, false); blockView.setUint32(4, Math.min(4608, remaining), false); block.set(xas.subarray(offset, offset + size), 8);
    parts.push(block); length += block.length; offset += size; remaining -= 4608;
  }
  parts.push(new Uint8Array([0x45, 0, 0, 4]));
  return xasConcat(parts, length + 4);
}

function xasDecodeSnr(bytes) {
  if (bytes.length < 16) xasError("truncated SNR header");
  var view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength), header = xasHeader(view, 0), size = xasU32BE(view, 8);
  if (size < 8 || size - 8 > bytes.length - 16 || xasU32BE(view, 12) !== header.samples) xasError("invalid SNR data block");
  var data = bytes.subarray(16, 16 + size - 8), channels = xasChannels(header, data.length);
  return xasMakeWav(xasDecode(data, header.samples, channels), header.sampleRate, channels);
}

function xasDecodeSps(bytes) {
  if (bytes.length < 16) xasError("truncated SPS header");
  var view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
  if ((xasU32BE(view, 0) >>> 24) !== 0x48) xasError("SPS block id mismatch");
  var header = xasHeader(view, 4), offset = 12, parts = [], length = 0;
  while (offset + 4 <= bytes.length) {
    var magic = xasU32BE(view, offset), id = magic >>> 24, size = magic & 0xffffff; offset += 4;
    if (id === 0x45) break;
    if (id !== 0x44 || size < 8 || size - 4 > bytes.length - offset) xasError("invalid SPS data block");
    offset += 4; var payload = size - 8;
    if (payload > bytes.length - offset) xasError("truncated SPS data block");
    parts.push(bytes.slice(offset, offset + payload)); length += payload; offset += payload;
  }
  var data = xasConcat(parts, length);
  if (header.codec === 6) return xasDecodeLayer3(data, header);
  if (header.codec !== 4) xasError("unsupported SPS codec " + header.codec);
  var channels = xasChannels(header, data.length);
  return xasMakeWav(xasDecode(data, header.samples, channels), header.sampleRate, channels);
}

function execute(params, id) {
  var input = params.InputFile, output = params.OutputFile;
  if (!input || !kernelx.io.isFile(input)) return { success: false, error: "InputFile is required and must exist" };
  if (!output) return { success: false, error: "OutputFile is required" };
  try {
    var source = kernelx.io.mmapRead(input);
    if (!source || !source.byteLength) xasError("input is empty or cannot be read");
    var bytes = new Uint8Array(source), result, decoded;
    var isWav = bytes.length >= 12 && xasFourCC(bytes, 0) === "RIFF" && xasFourCC(bytes, 8) === "WAVE";
    if (isWav) {
      decoded = xasParseWav(bytes);
      result = id === "snr.encode" || id !== "sps.encode" && xasHasExtension(output, ".snr") ? xasEncodeSnr(decoded) : xasEncodeSps(decoded);
    } else {
      result = bytes.length >= 4 && xasU32BE(new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength), 0) >>> 24 === 0x48 ? xasDecodeSps(bytes) : xasDecodeSnr(bytes);
    }
    if (!kernelx.io.writeBytes(output, result)) xasError("unable to write " + output);
    return { success: true, output: output, sampleRate: decoded ? decoded.sampleRate : undefined, channels: decoded ? decoded.channels : undefined };
  } catch (error) { return { success: false, error: error && error.message ? error.message : String(error) }; }
}
