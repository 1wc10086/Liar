/* kernelx-manifest
{
  "id": "wma.decode",
  "implementation": "implementation",
  "buffer_size": "512m",
  "params": [
    { "name": "InputFile", "type": "path", "required": true, "extensions": [".wma", ".WMA"] },
    { "name": "OutputFile", "type": "path", "required": true, "extensions": [".wav", ".WAV"] },
    { "name": "FrameCapacity", "type": "int", "required": false, "default": 4800000, "min": 1, "max": 4294967295 }
  ]
}
*/

var WMA_DEFAULT_FRAME_CAPACITY = 4800000;
var WMA_MAX_FRAME_CAPACITY = 4294967295;

function wmaFrameCapacity(value) {
  var capacity = Number(value);
  if (!Number.isInteger(capacity) || capacity < 1) return WMA_DEFAULT_FRAME_CAPACITY;
  return Math.min(capacity, WMA_MAX_FRAME_CAPACITY);
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
    if (!source || source.byteLength === 0) throw new Error("Unable to read InputFile or input is empty");

    var decoded = kernelx.audio.wma(new Uint8Array(source), wmaFrameCapacity(params.FrameCapacity));
    if (!decoded || !decoded.pcm || !decoded.sampleRate || !decoded.channels) {
      throw new Error("WMA decoding failed; increase FrameCapacity if the file is longer than its configured limit");
    }

    var wav = kernelx.audio.encodeWav(decoded.pcm, decoded.sampleRate, decoded.channels);
    if (!wav) throw new Error("WAV encoding failed");
    if (!kernelx.io.writeBytes(output, wav)) throw new Error("Unable to write " + output);

    return {
      success: true,
      output: output,
      sampleRate: decoded.sampleRate,
      channels: decoded.channels,
      frames: decoded.pcm.length / decoded.channels
    };
  } catch (error) {
    return { success: false, error: error && error.message ? error.message : String(error) };
  }
}
