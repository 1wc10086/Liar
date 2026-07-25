/* kernelx-manifest
{
  "id": "audio.xm.to_wav",
  "name": "audio.xm.to_wav",
  "implementation": "implementation",
  "buffer_size": "256.0m",
  "params": [
      { "name": "InputFile", "type": "file", "required": true, "extensions": ["xm"] },
    { "name": "OutputFile", "type": "file", "required": false, "default": "", "extensions": ["wav"] },
    { "name": "SampleRate", "type": "string", "required": false, "default": "48000" },
    { "name": "MaxDurationSeconds", "type": "string", "required": false, "default": "180" }
  ]
}
*/

function boundedInteger(value, fallback, minimum, maximum) {
  var number = Number(value);
  if (!Number.isFinite(number)) return fallback;
  return Math.max(minimum, Math.min(maximum, Math.floor(number)));
}

function execute(params) {
  var input = params.InputFile || params.input || params.in;
  if (!input || !kernelx.io.isFile(input)) {
    return { success: false, error: "InputFile is required and must exist" };
  }
  if (!kernelx.audio.loaded()) {
    return { success: false, error: kernelx.audio.status().error || "libaudio.so is unavailable" };
  }

  try {
    var output = params.OutputFile || params.output || input + ".wav";
    var sampleRate = boundedInteger(params.SampleRate, 48000, 8000, 96000);
    var requestedSeconds = boundedInteger(params.MaxDurationSeconds, 180, 1, 300);
    var maxFrames = 6000000;
    var frames = Math.min(sampleRate * requestedSeconds, maxFrames);
    var source = kernelx.io.readBytes(input);
    if (!source || !source.byteLength) {
      return { success: false, error: "Failed to read: " + input };
    }

    var decoded = kernelx.audio.xm(source, frames, sampleRate);
    if (!decoded || !decoded.pcm || !decoded.sampleRate || !decoded.channels ||
        !decoded.pcm.length || decoded.pcm.length % decoded.channels) {
      return { success: false, error: "XM decode failed or MaxDurationSeconds is too small" };
    }

    var wav = kernelx.audio.encodeWav(decoded.pcm, decoded.sampleRate, decoded.channels);
    if (!wav || !wav.byteLength) return { success: false, error: "WAV encoding failed" };
    if (!kernelx.io.writeBytes(output, wav)) {
      return { success: false, error: "Failed to write: " + output };
    }

    var renderedFrames = decoded.pcm.length / decoded.channels;
    return {
      success: true,
      input: input,
      output: output,
      sampleRate: decoded.sampleRate,
      channels: decoded.channels,
      frames: renderedFrames,
      durationSeconds: renderedFrames / decoded.sampleRate,
      maxDurationSeconds: frames / sampleRate,
      inputSize: source.byteLength,
      outputSize: wav.byteLength
    };
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) };
  }
}
