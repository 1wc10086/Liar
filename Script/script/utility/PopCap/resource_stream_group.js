/* kernelx-manifest
[
  {
    "id": "popcap.resource_stream_group.pack",
    "implementation": "implementation",
    "buffer_size": "1024m",
    "params": [
      { "name": "InputFolder", "type": "path", "required": true, "folder": true, "extensions": [".rsg.bundle"], "language": "bundle_directory" },
      { "name": "OutputFile", "type": "path", "required": true, "extensions": [".rsg"], "language": "data_file" },
      { "name": "Version", "type": "list", "default": "4", "list": ["1", "3", "4"], "language": "version_number" }
    ]
  },
  {
    "id": "popcap.resource_stream_group.unpack",
    "implementation": "implementation",
    "buffer_size": "1024m",
    "params": [
      { "name": "InputFile", "type": "path", "required": true, "extensions": [".rsg"], "language": "data_file" },
      { "name": "OutputFolder", "type": "path", "required": true, "folder": true, "extensions": [".rsg.bundle"], "language": "bundle_directory" },
      { "name": "Version", "type": "list", "default": "4", "list": ["1", "3", "4"], "language": "version_number" }
    ]
  }
]
*/

function execute(params) {
  try {
    var version = Number(params.Version || 4);
    if (version !== 1 && version !== 3 && version !== 4) throw new Error("ResourceStreamGroup: Version must be 1, 3, or 4");
    if (params.InputFolder !== undefined) { ResourceStreamGroupCore.pack(params.InputFolder, params.OutputFile, version); return { success: true, output: params.OutputFile }; }
    if (params.InputFile !== undefined) { ResourceStreamGroupCore.unpack(params.InputFile, params.OutputFolder, version); return { success: true, output: params.OutputFolder }; }
    throw new Error("ResourceStreamGroup: unknown operation");
  } catch (error) { return { success: false, error: error && error.message ? error.message : String(error) }; }
}
