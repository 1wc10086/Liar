/* kernelx-manifest
[
  {
    "id": "font_widget_dat.decode",
    "implementation": "implementation",
    "buffer_size": "64m",
    "params": [
      { "name": "InputFile", "type": "path", "required": true, "extensions": [".dat", ".DAT"] },
      { "name": "OutputFile", "type": "path", "required": true, "extensions": [".json", ".JSON"] }
    ]
  },
  {
    "id": "font_widget_dat.encode",
    "implementation": "implementation",
    "buffer_size": "64m",
    "params": [
      { "name": "InputFile", "type": "path", "required": true, "extensions": [".json", ".JSON"] },
      { "name": "OutputFile", "type": "path", "required": true, "extensions": [".dat", ".DAT"] }
    ]
  }
]
*/

function fontWidgetDatError(message) { throw new Error("FontWidget DAT: " + message); }
function fontWidgetDatReader(bytes) { return { bytes: bytes, offset: 0, view: new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength) }; }
function fontWidgetDatRequire(reader, size) { if (size < 0 || size > reader.bytes.length - reader.offset) fontWidgetDatError("unexpected end of file at 0x" + reader.offset.toString(16)); }
function fontWidgetDatU8(reader) { fontWidgetDatRequire(reader, 1); return reader.bytes[reader.offset++]; }
function fontWidgetDatU16(reader) { fontWidgetDatRequire(reader, 2); var value = reader.view.getUint16(reader.offset, true); reader.offset += 2; return value; }
function fontWidgetDatI32(reader) { fontWidgetDatRequire(reader, 4); var value = reader.view.getInt32(reader.offset, true); reader.offset += 4; return value; }
function fontWidgetDatIsObject(value) { return value !== null && typeof value === "object" && !Array.isArray(value); }
function fontWidgetDatInteger(value, name, minimum, maximum) { if (typeof value !== "number" || !isFinite(value) || Math.floor(value) !== value || value < minimum || value > maximum) fontWidgetDatError(name + " must be an integer from " + minimum + " to " + maximum); return value; }
function fontWidgetDatI16(value, name) { return fontWidgetDatInteger(value, name, -32768, 32767); }
function fontWidgetDatU16Value(value, name) { return fontWidgetDatInteger(value, name, 0, 65535); }
function fontWidgetDatI32Value(value, name) { return fontWidgetDatInteger(value, name, -2147483648, 2147483647); }
function fontWidgetDatWriter() { return { chunks: [], length: 0 }; }
function fontWidgetDatWrite(writer, bytes) { writer.chunks.push(bytes); writer.length += bytes.length; }
function fontWidgetDatWriteU8(writer, value) { fontWidgetDatWrite(writer, new Uint8Array([fontWidgetDatInteger(value, "byte", 0, 255)])); }
function fontWidgetDatWriteU16(writer, value, name) { var bytes = new Uint8Array(2); new DataView(bytes.buffer).setUint16(0, fontWidgetDatU16Value(value, name), true); fontWidgetDatWrite(writer, bytes); }
function fontWidgetDatWriteI16(writer, value, name) { var bytes = new Uint8Array(2); new DataView(bytes.buffer).setInt16(0, fontWidgetDatI16(value, name), true); fontWidgetDatWrite(writer, bytes); }
function fontWidgetDatWriteI32(writer, value, name) { var bytes = new Uint8Array(4); new DataView(bytes.buffer).setInt32(0, fontWidgetDatI32Value(value, name), true); fontWidgetDatWrite(writer, bytes); }
function fontWidgetDatFinish(writer) { var output = new Uint8Array(writer.length), offset = 0; for (var index = 0; index < writer.chunks.length; ++index) { output.set(writer.chunks[index], offset); offset += writer.chunks[index].length; } return output; }
function fontWidgetDatString(reader, label) {
  var length = fontWidgetDatU16(reader);
  fontWidgetDatRequire(reader, length);
  var bytes = reader.bytes.subarray(reader.offset, reader.offset + length), value = "";
  for (var index = 0; index < bytes.length; ++index) {
    if (bytes[index] < 32 || bytes[index] > 126) fontWidgetDatError("invalid " + label + " string");
    value += String.fromCharCode(bytes[index]);
  }
  reader.offset += length;
  return value;
}
function fontWidgetDatWriteString(writer, value, name) {
  if (typeof value !== "string" || value.length > 65535) fontWidgetDatError(name + " must be an ASCII string no longer than 65535 characters");
  var bytes = new Uint8Array(value.length);
  for (var index = 0; index < value.length; ++index) { var code = value.charCodeAt(index); if (code < 32 || code > 126) fontWidgetDatError(name + " must be ASCII printable text"); bytes[index] = code; }
  fontWidgetDatWriteU16(writer, bytes.length, name + " length");
  fontWidgetDatWrite(writer, bytes);
}
function fontWidgetDatCharacter(codePoint) { return codePoint >= 0xd800 && codePoint <= 0xdfff ? null : String.fromCharCode(codePoint); }
function fontWidgetDatDecode(bytes) {
  if (bytes.length < 16) fontWidgetDatError("file is too short");
  var reader = fontWidgetDatReader(bytes), magic = fontWidgetDatI32(reader);
  if ((magic >>> 0) !== 0xff0000bb) fontWidgetDatError("not a supported FontWidget DAT file");
  var definition = { format: "popcap.font_widget.dat", header: { magic: "0xff0000bb", point_size: fontWidgetDatI32(reader), unknown: fontWidgetDatI32(reader), layer_count: fontWidgetDatU16(reader) }, layers: [] };
  for (var layerIndex = 0; layerIndex < definition.header.layer_count; ++layerIndex) {
    var layer = { name: fontWidgetDatString(reader, "layer name"), reserved_32: fontWidgetDatI32(reader), glyph_slots: fontWidgetDatU16(reader), reserved_16: fontWidgetDatU16(reader), reserved_header_32: [], glyphs: [], kerning: [] };
    for (var valueIndex = 0; valueIndex < 8; ++valueIndex) layer.reserved_header_32.push(fontWidgetDatI32(reader));
    layer.reserved_before_glyph_table_16 = fontWidgetDatU16(reader);
    if (!layer.glyph_slots) fontWidgetDatError("layer has no glyph slots");
    for (var glyphIndex = 0; glyphIndex < layer.glyph_slots - 1; ++glyphIndex) {
      fontWidgetDatRequire(reader, 36);
      var codePoint = fontWidgetDatU16(reader);
      layer.glyphs.push({
        code_point: codePoint,
        character: fontWidgetDatCharacter(codePoint),
        atlas_marker: fontWidgetDatU16(reader),
        image_rect_x: fontWidgetDatI32(reader),
        image_rect_y: fontWidgetDatI32(reader),
        image_rect_width: fontWidgetDatI32(reader),
        image_rect_height: fontWidgetDatI32(reader),
        image_offset_x: fontWidgetDatI32(reader),
        image_offset_y: fontWidgetDatI32(reader),
        width: fontWidgetDatI32(reader),
        order: fontWidgetDatI32(reader)
      });
    }
    layer.kerning_count = fontWidgetDatU16(reader);
    for (var kerningIndex = 0; kerningIndex < layer.kerning_count; ++kerningIndex) {
      var leftCodePoint = fontWidgetDatU16(reader), rightCodePoint = fontWidgetDatU16(reader);
      fontWidgetDatRequire(reader, 4);
      var offset = reader.view.getInt16(reader.offset, true), unknown = reader.view.getUint16(reader.offset + 2, true); reader.offset += 4;
      layer.kerning.push({ left_code_point: leftCodePoint, left: fontWidgetDatCharacter(leftCodePoint), right_code_point: rightCodePoint, right: fontWidgetDatCharacter(rightCodePoint), offset: offset, unknown_16: unknown });
    }
    for (var glyphMapIndex = 0; glyphMapIndex < layer.glyphs.length; ++glyphMapIndex) {
      var glyph = layer.glyphs[glyphMapIndex];
      glyph.kerning = [];
      for (var pairIndex = 0; pairIndex < layer.kerning.length; ++pairIndex) if (layer.kerning[pairIndex].left_code_point === glyph.code_point) glyph.kerning.push(layer.kerning[pairIndex]);
    }
    layer.multiply = { red: fontWidgetDatI32(reader), green: fontWidgetDatI32(reader), blue: fontWidgetDatI32(reader), alpha: fontWidgetDatI32(reader) };
    layer.add = { red: fontWidgetDatI32(reader), green: fontWidgetDatI32(reader), blue: fontWidgetDatI32(reader), alpha: fontWidgetDatI32(reader) };
    layer.image_file = fontWidgetDatString(reader, "image file");
    layer.draw_mode = fontWidgetDatI32(reader);
    layer.offset_x = fontWidgetDatI32(reader);
    layer.offset_y = fontWidgetDatI32(reader);
    layer.spacing = fontWidgetDatI32(reader);
    layer.minimum_point_size = fontWidgetDatI32(reader);
    layer.maximum_point_size = fontWidgetDatI32(reader);
    layer.point_size = fontWidgetDatI32(reader);
    layer.ascent = fontWidgetDatI32(reader);
    layer.ascent_padding = fontWidgetDatI32(reader);
    layer.height = fontWidgetDatI32(reader);
    layer.default_height = fontWidgetDatI32(reader);
    layer.line_spacing_offset = fontWidgetDatI32(reader);
    layer.base_order = fontWidgetDatI32(reader);
    layer.initialized = fontWidgetDatU8(reader);
    definition.layers.push(layer);
  }
  fontWidgetDatRequire(reader, 4);
  if (reader.bytes[reader.offset] !== 0xbb || reader.bytes[reader.offset + 1] !== 0 || reader.bytes[reader.offset + 2] !== 0 || reader.bytes[reader.offset + 3] !== 0xff) fontWidgetDatError("invalid file trailer");
  reader.offset += 4;
  if (reader.offset !== bytes.length) fontWidgetDatError("unexpected data after file trailer");
  return definition;
}

function fontWidgetDatArray(value, name) { if (!Array.isArray(value)) fontWidgetDatError(name + " must be an array"); return value; }
function fontWidgetDatObject(value, name) { if (!fontWidgetDatIsObject(value)) fontWidgetDatError(name + " must be an object"); return value; }
function fontWidgetDatWriteColor(writer, value, name) {
  value = fontWidgetDatObject(value, name);
  fontWidgetDatWriteI32(writer, value.red, name + ".red");
  fontWidgetDatWriteI32(writer, value.green, name + ".green");
  fontWidgetDatWriteI32(writer, value.blue, name + ".blue");
  fontWidgetDatWriteI32(writer, value.alpha, name + ".alpha");
}
function fontWidgetDatEncode(definition) {
  definition = fontWidgetDatObject(definition, "definition");
  var header = fontWidgetDatObject(definition.header, "header"), layers = fontWidgetDatArray(definition.layers, "layers"), writer = fontWidgetDatWriter();
  if (definition.format !== "popcap.font_widget.dat") fontWidgetDatError("unsupported format");
  if (header.magic !== "0xff0000bb") fontWidgetDatError("header.magic must be 0xff0000bb");
  if (layers.length !== fontWidgetDatU16Value(header.layer_count, "header.layer_count")) fontWidgetDatError("header.layer_count does not match layers length");
  fontWidgetDatWriteI32(writer, -16777029, "magic");
  fontWidgetDatWriteI32(writer, header.point_size, "header.point_size");
  fontWidgetDatWriteI32(writer, header.unknown, "header.unknown");
  fontWidgetDatWriteU16(writer, layers.length, "header.layer_count");
  for (var layerIndex = 0; layerIndex < layers.length; ++layerIndex) {
    var layerName = "layers[" + layerIndex + "]", layer = fontWidgetDatObject(layers[layerIndex], layerName), glyphs = fontWidgetDatArray(layer.glyphs, layerName + ".glyphs"), kerning = fontWidgetDatArray(layer.kerning, layerName + ".kerning"), reserved = fontWidgetDatArray(layer.reserved_header_32, layerName + ".reserved_header_32");
    if (reserved.length !== 8) fontWidgetDatError(layerName + ".reserved_header_32 must contain 8 values");
    if (glyphs.length + 1 !== fontWidgetDatU16Value(layer.glyph_slots, layerName + ".glyph_slots")) fontWidgetDatError(layerName + ".glyph_slots must equal glyph count plus one");
    if (kerning.length !== fontWidgetDatU16Value(layer.kerning_count, layerName + ".kerning_count")) fontWidgetDatError(layerName + ".kerning_count does not match kerning length");
    fontWidgetDatWriteString(writer, layer.name, layerName + ".name");
    fontWidgetDatWriteI32(writer, layer.reserved_32, layerName + ".reserved_32");
    fontWidgetDatWriteU16(writer, layer.glyph_slots, layerName + ".glyph_slots");
    fontWidgetDatWriteU16(writer, layer.reserved_16, layerName + ".reserved_16");
    for (var reservedIndex = 0; reservedIndex < 8; ++reservedIndex) fontWidgetDatWriteI32(writer, reserved[reservedIndex], layerName + ".reserved_header_32[" + reservedIndex + "]");
    fontWidgetDatWriteU16(writer, layer.reserved_before_glyph_table_16, layerName + ".reserved_before_glyph_table_16");
    for (var glyphIndex = 0; glyphIndex < glyphs.length; ++glyphIndex) {
      var glyphName = layerName + ".glyphs[" + glyphIndex + "]", glyph = fontWidgetDatObject(glyphs[glyphIndex], glyphName);
      fontWidgetDatWriteU16(writer, glyph.code_point, glyphName + ".code_point");
      fontWidgetDatWriteU16(writer, glyph.atlas_marker, glyphName + ".atlas_marker");
      fontWidgetDatWriteI32(writer, glyph.image_rect_x, glyphName + ".image_rect_x");
      fontWidgetDatWriteI32(writer, glyph.image_rect_y, glyphName + ".image_rect_y");
      fontWidgetDatWriteI32(writer, glyph.image_rect_width, glyphName + ".image_rect_width");
      fontWidgetDatWriteI32(writer, glyph.image_rect_height, glyphName + ".image_rect_height");
      fontWidgetDatWriteI32(writer, glyph.image_offset_x, glyphName + ".image_offset_x");
      fontWidgetDatWriteI32(writer, glyph.image_offset_y, glyphName + ".image_offset_y");
      fontWidgetDatWriteI32(writer, glyph.width, glyphName + ".width");
      fontWidgetDatWriteI32(writer, glyph.order, glyphName + ".order");
    }
    fontWidgetDatWriteU16(writer, kerning.length, layerName + ".kerning_count");
    for (var kerningIndex = 0; kerningIndex < kerning.length; ++kerningIndex) {
      var kerningName = layerName + ".kerning[" + kerningIndex + "]", pair = fontWidgetDatObject(kerning[kerningIndex], kerningName);
      fontWidgetDatWriteU16(writer, pair.left_code_point, kerningName + ".left_code_point");
      fontWidgetDatWriteU16(writer, pair.right_code_point, kerningName + ".right_code_point");
      fontWidgetDatWriteI16(writer, pair.offset, kerningName + ".offset");
      fontWidgetDatWriteU16(writer, pair.unknown_16, kerningName + ".unknown_16");
    }
    fontWidgetDatWriteColor(writer, layer.multiply, layerName + ".multiply");
    fontWidgetDatWriteColor(writer, layer.add, layerName + ".add");
    fontWidgetDatWriteString(writer, layer.image_file, layerName + ".image_file");
    fontWidgetDatWriteI32(writer, layer.draw_mode, layerName + ".draw_mode");
    fontWidgetDatWriteI32(writer, layer.offset_x, layerName + ".offset_x");
    fontWidgetDatWriteI32(writer, layer.offset_y, layerName + ".offset_y");
    fontWidgetDatWriteI32(writer, layer.spacing, layerName + ".spacing");
    fontWidgetDatWriteI32(writer, layer.minimum_point_size, layerName + ".minimum_point_size");
    fontWidgetDatWriteI32(writer, layer.maximum_point_size, layerName + ".maximum_point_size");
    fontWidgetDatWriteI32(writer, layer.point_size, layerName + ".point_size");
    fontWidgetDatWriteI32(writer, layer.ascent, layerName + ".ascent");
    fontWidgetDatWriteI32(writer, layer.ascent_padding, layerName + ".ascent_padding");
    fontWidgetDatWriteI32(writer, layer.height, layerName + ".height");
    fontWidgetDatWriteI32(writer, layer.default_height, layerName + ".default_height");
    fontWidgetDatWriteI32(writer, layer.line_spacing_offset, layerName + ".line_spacing_offset");
    fontWidgetDatWriteI32(writer, layer.base_order, layerName + ".base_order");
    fontWidgetDatWriteU8(writer, layer.initialized);
  }
  fontWidgetDatWrite(writer, new Uint8Array([0xbb, 0, 0, 0xff]));
  return fontWidgetDatFinish(writer);
}

function execute(params, id) {
  var input = params.InputFile, output = params.OutputFile;
  if (!input || !kernelx.io.isFile(input)) return { success: false, error: "InputFile is required and must exist" };
  if (!output) return { success: false, error: "OutputFile is required" };
  try {
    var encode = id === "font_widget_dat.encode" || (!id && /\.json$/i.test(input));
    if (encode) {
      if (!kernelx.io.writeBytes(output, fontWidgetDatEncode(JSON.parse(kernelx.io.readText(input))))) fontWidgetDatError("unable to write " + output);
    } else {
      var bytes = kernelx.io.readBytes(input);
      if (!bytes.length) fontWidgetDatError("input is empty or cannot be read");
      if (!kernelx.io.writeText(output, JSON.stringify(fontWidgetDatDecode(bytes), null, 2))) fontWidgetDatError("unable to write " + output);
    }
    return { success: true, output: output };
  } catch (error) { return { success: false, error: error && error.message ? error.message : String(error) }; }
}
