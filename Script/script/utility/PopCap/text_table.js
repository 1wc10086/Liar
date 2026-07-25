/* kernelx-manifest
{
  "id": "text_table.convert",
  "implementation": "implementation",
  "buffer_size": "512m",
  "params": [
    { "name": "InputFile", "type": "path", "required": true, "language": "source_file" },
    { "name": "OutputFile", "type": "path", "required": true, "language": "destination_file" },
    { "name": "SourceVersion", "type": "list", "required": false, "default": "automatic", "list": ["automatic", "text", "json_map", "json_list"], "language": "source_version" },
    { "name": "DestinationVersion", "type": "list", "required": false, "default": "text", "list": ["text", "json_map", "json_list"], "language": "destination_version" }
  ]
}
*/

function textTableError(message) { throw new Error("Text-Table: " + message); }

function textTableIsObject(value) {
  return value !== null && typeof value === "object" && !Array.isArray(value);
}

function textTableReadUtf8(path) {
  var data = kernelx.io.readBytes(path);
  if (data.length >= 2 && ((data[0] === 255 && data[1] === 254) || (data[0] === 254 && data[1] === 255))) textTableError("unsupported charset UTF-16");
  return kernelx.io.readText(path);
}

function textTableJsonValue(json) {
  var value = json && json.objects && json.objects[0] && json.objects[0].objdata && json.objects[0].objdata.LocStringValues;
  if (!textTableIsObject(value) && !Array.isArray(value)) textTableError("invalid source");
  return value;
}

function textTableParseJsonText(text) {
  var output = "";
  var quoted = false;
  var escaped = false;
  for (var index = 0; index < text.length; ++index) {
    var character = text[index];
    if (quoted) {
      output += character;
      if (escaped) escaped = false;
      else if (character === "\\") escaped = true;
      else if (character === "\"") quoted = false;
      continue;
    }
    if (character === "\"") {
      quoted = true;
      output += character;
    } else if (character === "/" && text[index + 1] === "/") {
      while (++index < text.length && text[index] !== "\n" && text[index] !== "\r") {}
      if (index < text.length) output += text[index];
    } else if (character === "/" && text[index + 1] === "*") {
      index += 2;
      while (index < text.length && (text[index] !== "*" || text[index + 1] !== "/")) ++index;
      if (index >= text.length) textTableError("invalid source");
      ++index;
    } else {
      output += character;
    }
  }
  output = output.replace(/,(\s*[\]\}])/g, "$1");
  try { return JSON.parse(output); }
  catch (error) { textTableError("invalid source"); }
}

function textTableParseText(text) {
  var strings = {};
  var keyPattern = /^\[.+\]$/gm;
  var valuePattern = /(.|[\n\r])*?(?=[\n\r]*?(\[|$))/gy;
  var key;
  while ((key = keyPattern.exec(text)) !== null) {
    valuePattern.lastIndex = keyPattern.lastIndex + 1;
    var value = valuePattern.exec(text);
    strings[key[0].slice(1, -1)] = value ? value[0] : "";
    keyPattern.lastIndex = valuePattern.lastIndex;
  }
  return strings;
}

function textTableParseJson(text, sourceVersion) {
  var source = textTableParseJsonText(text);
  var value = textTableJsonValue(source);
  var strings = {};
  var key;
  if (sourceVersion === "json_map") {
    if (!textTableIsObject(value)) textTableError("invalid source");
    for (key in value) {
      if (typeof value[key] !== "string") textTableError("invalid map element");
      strings[key] = value[key];
    }
  } else {
    if (!Array.isArray(value)) textTableError("invalid source");
    if (value.length % 2) textTableError("invalid list size");
    for (var index = 0; index < value.length; index += 2) {
      key = value[index];
      if (typeof key !== "string" || typeof value[index + 1] !== "string") textTableError("invalid list element");
      strings[key] = value[index + 1];
    }
  }
  return strings;
}

function textTableParse(path, sourceVersion) {
  var text = textTableReadUtf8(path);
  if (sourceVersion === "automatic") {
    try {
      var value = textTableJsonValue(textTableParseJsonText(text));
      sourceVersion = Array.isArray(value) ? "json_list" : "json_map";
    } catch (error) {
      sourceVersion = "text";
    }
  }
  if (sourceVersion === "text") return textTableParseText(text);
  if (sourceVersion === "json_map" || sourceVersion === "json_list") return textTableParseJson(text, sourceVersion);
  textTableError("invalid SourceVersion");
}

function textTableEncode(strings, destinationVersion) {
  var key;
  if (destinationVersion === "text") {
    var output = [];
    for (key in strings) output.push("[" + key + "]\n" + strings[key] + "\n");
    return output.join("\n");
  }
  var values = destinationVersion === "json_map" ? strings : [];
  if (destinationVersion === "json_list") for (key in strings) { values.push(key); values.push(strings[key]); }
  if (destinationVersion !== "json_map" && destinationVersion !== "json_list") textTableError("invalid DestinationVersion");
  return JSON.stringify({ version: 1, objects: [{ aliases: ["LawnStringsData"], objclass: "LawnStringsData", objdata: { LocStringValues: values } }] }, null, 2);
}

function execute(params) {
  var input = params.InputFile;
  var output = params.OutputFile;
  if (!input || !kernelx.io.isFile(input)) return { success: false, error: "InputFile is required and must exist" };
  if (!output) return { success: false, error: "OutputFile is required" };
  try {
    var sourceVersion = params.SourceVersion || "automatic";
    var destinationVersion = params.DestinationVersion || "text";
    var result = textTableEncode(textTableParse(input, sourceVersion), destinationVersion);
    if (!kernelx.io.writeText(output, result)) throw new Error("Unable to write " + output);
    return { success: true, output: output, sourceVersion: sourceVersion, destinationVersion: destinationVersion };
  } catch (error) {
    return { success: false, error: error && error.message ? error.message : String(error) };
  }
}
