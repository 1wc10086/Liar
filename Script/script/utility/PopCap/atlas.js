/* kernelx-manifest
[
  {
    "id": "atlas.cut",
    "implementation": "implementation",
    "buffer_size": "512m",
    "params": [
      { "name": "InputFile", "type": "path", "required": true, "extensions": [".png"], "language": "raw_file" },
      { "name": "OutputFolder", "type": "path", "required": true, "folder": true, "language": "bundle_directory" },
      { "name": "InfoFile", "type": "path", "required": true, "extensions": [".xml", ".plist", ".json", ".dat"], "language": "data_file" },
      { "name": "ItemName", "type": "string", "required": false, "language": "item_name" },
      { "name": "Format", "type": "list", "default": "newxml", "list": ["newxml", "oldxml", "ancientxml", "plist", "imagedat", "tvatlasxml", "resrton"], "language": "atlas_format" }
    ]
  },
  {
    "id": "atlas.splice",
    "implementation": "implementation",
    "buffer_size": "512m",
    "params": [
      { "name": "InputFolder", "type": "path", "required": true, "folder": true, "language": "bundle_directory" },
      { "name": "OutputFile", "type": "path", "required": true, "extensions": [".png"], "language": "ripe_file" },
      { "name": "InfoFile", "type": "path", "required": true, "extensions": [".xml", ".plist", ".json", ".dat"], "language": "data_file" },
      { "name": "ItemName", "type": "string", "required": false, "language": "item_name" },
      { "name": "Format", "type": "list", "default": "newxml", "list": ["newxml", "oldxml", "ancientxml", "plist", "imagedat", "tvatlasxml", "resrton"], "language": "atlas_format" },
      { "name": "MaxWidth", "type": "int", "default": 2048, "language": "max_width" },
      { "name": "MaxHeight", "type": "int", "default": 2048, "language": "max_height" }
    ]
  }
]
*/

function atlasError(message) {
    throw new Error("Atlas: " + message);
}

function atlasStem(path) {
    return kernelx.path.stem(path).toLowerCase();
}

function atlasKey(name) {
    return String(name || "").replace(/\\/g, "/").split("/").pop().replace(/\.[^.]*$/, "").toLowerCase();
}

function atlasAttr(text, name) {
    var m = new RegExp("\\s" + name + "\\s*=\\s*([\"'])(.*?)\\1", "i").exec(text);
    return m ? m[2] : null;
}

function atlasSetAttr(text, name, value) {
    var re = new RegExp("(\\s" + name + "\\s*=\\s*)([\"'])(.*?)\\2", "i");
    return re.test(text) ? text.replace(re, "$1\"" + value + "\"") : text.replace(/\s*\/?>(\s*)$/, " " + name + "=\"" + value + "\"$&");
}

function atlasRemoveAttr(text, name) {
    return text.replace(new RegExp("\\s" + name + "\\s*=\\s*([\"']).*?\\1", "ig"), "");
}

function atlasInt(value, what) {
    var n = Number(value);
    if (!Number.isInteger(n) || n < 0) atlasError("invalid " + what);
    return n;
}

function atlasIdFile(folder) {
    return kernelx.path.join(folder, "AtlasID.txt");
}

function atlasReadId(folder) {
    var path = atlasIdFile(folder);
    return kernelx.io.isFile(path) ? kernelx.io.readText(path).replace(/[\r\n]/g, "") : "";
}

function atlasWriteId(folder, id) {
    if (id) kernelx.io.writeText(atlasIdFile(folder), id);
}

function atlasCopyRect(source, sourceWidth, sx, sy, width, height, target, targetWidth, tx, ty) {
    for (var y = 0; y < height; ++y) target.set(source.subarray(((sy + y) * sourceWidth + sx) * 4, ((sy + y) * sourceWidth + sx + width) * 4), ((ty + y) * targetWidth + tx) * 4);
}

function atlasRotate270(source, width, height) {
    var target = new Uint8Array(source.length),
        targetWidth = height;
    for (var y = 0; y < height; ++y)
        for (var x = 0; x < width; ++x) target.set(source.subarray((y * width + x) * 4, (y * width + x + 1) * 4), ((width - 1 - x) * targetWidth + y) * 4);
    return {
        pixels: target,
        width: height,
        height: width
    };
}

function atlasCut(input, folder, entries) {
    var image = kernelx.png.read(input);
    if (!image) atlasError("cannot decode input PNG");
    kernelx.io.mkdir(folder);
    for (var i = 0; i < entries.length; ++i) {
        var e = entries[i];
        if (e.x + e.width > image.width || e.y + e.height > image.height || !e.width || !e.height) atlasError("sprite outside atlas: " + e.id);
        var w = e.rotated ? e.height : e.width,
            h = e.rotated ? e.width : e.height;
        var pixels = new Uint8Array(w * h * 4);
        atlasCopyRect(image.pixels, image.width, e.x, e.y, w, h, pixels, w, 0, 0);
        if (e.rotated) {
            var rotated = atlasRotate270(pixels, w, h);
            pixels = rotated.pixels;
            w = rotated.width;
            h = rotated.height;
        }
        if (!kernelx.png.write(kernelx.path.join(folder, e.id.toLowerCase() + ".png"), w, h, pixels, 6)) atlasError("cannot write sprite: " + e.id);
    }
}

function atlasIntersect(a, b) {
    return !(a.x >= b.x + b.width || a.x + a.width <= b.x || a.y >= b.y + b.height || a.y + a.height <= b.y);
}

function atlasContains(a, b) {
    return b.x >= a.x && b.y >= a.y && b.x + b.width <= a.x + a.width && b.y + b.height <= a.y + a.height;
}

function atlasSplitFree(free, used) {
    if (!atlasIntersect(free, used)) return [free];
    var result = [];
    if (used.x > free.x && used.x < free.x + free.width) result.push({
        x: free.x,
        y: free.y,
        width: used.x - free.x,
        height: free.height
    });
    if (used.x + used.width < free.x + free.width) result.push({
        x: used.x + used.width,
        y: free.y,
        width: free.x + free.width - used.x - used.width,
        height: free.height
    });
    if (used.y > free.y && used.y < free.y + free.height) result.push({
        x: free.x,
        y: free.y,
        width: free.width,
        height: used.y - free.y
    });
    if (used.y + used.height < free.y + free.height) result.push({
        x: free.x,
        y: used.y + used.height,
        width: free.width,
        height: free.y + free.height - used.y - used.height
    });
    return result;
}

function atlasPack(images, width, height) {
    var free = [{
            x: 0,
            y: 0,
            width: width,
            height: height
        }],
        placed = {};
    images.sort(function(a, b) {
        return b.width * b.height - a.width * a.height;
    });
    for (var i = 0; i < images.length; ++i) {
        var image = images[i],
            best = null,
            score = Infinity,
            shortSide = Infinity;
        for (var j = 0; j < free.length; ++j)
            if (image.width <= free[j].width && image.height <= free[j].height) {
                var area = free[j].width * free[j].height - image.width * image.height,
                    side = Math.min(free[j].width - image.width, free[j].height - image.height);
                if (area < score || area === score && side < shortSide) {
                    best = {
                        x: free[j].x,
                        y: free[j].y,
                        width: image.width,
                        height: image.height
                    };
                    score = area;
                    shortSide = side;
                }
            }
        if (!best) atlasError("images do not fit within MaxWidth and MaxHeight: " + image.id);
        var next = [];
        for (j = 0; j < free.length; ++j) next = next.concat(atlasSplitFree(free[j], best));
        free = next.filter(function(a, index, all) {
            if (!a.width || !a.height) return false;
            for (var k = 0; k < all.length; ++k)
                if (k !== index && atlasContains(all[k], a)) return false;
            return true;
        });
        image.x = best.x;
        image.y = best.y;
        placed[image.id.toLowerCase()] = image;
    }
    return placed;
}

function atlasSplice(folder, output, width, height) {
    if (!kernelx.io.isDir(folder)) atlasError("InputFolder must exist");
    width = atlasInt(width, "MaxWidth");
    height = atlasInt(height, "MaxHeight");
    if (!width || !height) atlasError("atlas size must be positive");
    var paths = kernelx.io.list(folder).filter(function(p) {
        return kernelx.path.ext(p).toLowerCase() === ".png";
    });
    if (!paths.length) atlasError("InputFolder contains no PNG files");
    var images = [];
    for (var i = 0; i < paths.length; ++i) {
        var image = kernelx.png.read(paths[i]);
        if (!image) atlasError("cannot decode PNG: " + paths[i]);
        images.push({
            id: atlasStem(paths[i]),
            width: image.width,
            height: image.height,
            pixels: image.pixels
        });
    }
    var packed = atlasPack(images, width, height),
        pixels = kernelx.png.createBuffer(width, height);
    for (i = 0; i < images.length; ++i) atlasCopyRect(images[i].pixels, images[i].width, 0, 0, images[i].width, images[i].height, pixels, width, images[i].x, images[i].y);
    if (!kernelx.png.write(output, width, height, pixels, 6)) atlasError("cannot write output PNG");
    return packed;
}

function atlasNodes(xml, tag) {
    return xml.match(new RegExp("<" + tag + "\\b[^>]*?(?:/>|>[^<]*</" + tag + ">)", "gi")) || [];
}

function atlasReplaceNodes(xml, tag, update) {
    return xml.replace(new RegExp("<" + tag + "\\b[^>]*?(?:/>|>[^<]*</" + tag + ">)", "gi"), function(node) {
        return update(node);
    });
}

function atlasOldNodes(xml, item, atlasName) {
    var matches = xml.match(/<atlas\b[^>]*>[\s\S]*?<\/atlas>/gi) || [];
    for (var i = 0; i < matches.length; ++i)
        if (item ? atlasAttr(matches[i], "id") === item : atlasStem(atlasAttr(matches[i], "path") || "") === atlasName) return matches[i];
    return null;
}

function atlasOldCut(input, folder, info, item) {
    var xml = kernelx.io.readText(info),
        block = atlasOldNodes(xml, item, atlasStem(input));
    if (!block) atlasError("atlas item not found");
    var id = item || atlasAttr(block, "id");
    var entries = atlasNodes(block, "image").map(function(n) {
        return {
            id: atlasAttr(n, "id"),
            x: atlasInt(atlasAttr(n, "x"), "x"),
            y: atlasInt(atlasAttr(n, "y"), "y"),
            width: atlasInt(atlasAttr(n, "width"), "width"),
            height: atlasInt(atlasAttr(n, "height"), "height")
        };
    });
    atlasCut(input, folder, entries);
    atlasWriteId(folder, id);
}

function atlasOldSplice(folder, output, info, item, w, h) {
    var packed = atlasSplice(folder, output, w, h),
        xml = kernelx.io.readText(info),
        id = item || atlasReadId(folder),
        block = atlasOldNodes(xml, id, atlasStem(output));
    if (!block) atlasError("atlas item not found");
    var changed = atlasReplaceNodes(block, "image", function(n) {
        var e = packed[atlasKey(atlasAttr(n, "id"))];
        if (!e) atlasError("missing input image: " + atlasAttr(n, "id"));
        return atlasSetAttr(atlasSetAttr(atlasSetAttr(atlasSetAttr(n, "x", e.x), "y", e.y), "width", e.width), "height", e.height);
    });
    kernelx.io.writeText(info, xml.replace(block, changed));
}

function atlasNewBlock(xml, item, name) {
    var groups = xml.match(/<Resources\b[^>]*>[\s\S]*?<\/Resources>/gi) || [];
    for (var i = 0; i < groups.length; ++i) {
        var nodes = atlasNodes(groups[i], "Image");
        for (var j = 0; j < nodes.length; ++j)
            if (atlasAttr(nodes[j], "type") === "0" && atlasAttr(nodes[j], "imagetype") === "2" && (item ? atlasAttr(nodes[j], "id") === item : atlasStem(atlasAttr(nodes[j], "path") || "") === name)) return {
                block: groups[i],
                atlas: nodes[j]
            };
    }
    return null;
}

function atlasNewCut(input, folder, info, item) {
    var xml = kernelx.io.readText(info),
        found = atlasNewBlock(xml, item, atlasStem(input));
    if (!found) atlasError("atlas item not found");
    var id = item || atlasAttr(found.atlas, "id"),
        parent = id.split("|")[0],
        entries = atlasNodes(found.block, "Image").filter(function(n) {
            return atlasAttr(n, "type") === "0" && atlasAttr(n, "imagetype") === "4" && atlasAttr(n, "parent") === parent;
        }).map(function(n) {
            return {
                id: atlasAttr(n, "id").split("|")[0],
                x: atlasInt(atlasAttr(n, "ax") || 0, "ax"),
                y: atlasInt(atlasAttr(n, "ay") || 0, "ay"),
                width: atlasInt(atlasAttr(n, "aw") || 0, "aw"),
                height: atlasInt(atlasAttr(n, "ah") || 0, "ah")
            };
        });
    if (!entries.length) atlasError("atlas has no sprites");
    atlasCut(input, folder, entries);
    atlasWriteId(folder, id);
}

function atlasNewSplice(folder, output, info, item, w, h) {
    var packed = atlasSplice(folder, output, w, h),
        xml = kernelx.io.readText(info),
        found = atlasNewBlock(xml, item || atlasReadId(folder), atlasStem(output));
    if (!found) atlasError("atlas item not found");
    var parent = atlasAttr(found.atlas, "id").split("|")[0],
        changed = atlasSetAttr(atlasSetAttr(found.atlas, "aw", w), "ah", h);
    var block = found.block.replace(found.atlas, changed);
    block = atlasReplaceNodes(block, "Image", function(n) {
        if (atlasAttr(n, "type") !== "0" || atlasAttr(n, "imagetype") !== "4" || atlasAttr(n, "parent") !== parent) return n;
        var e = packed[atlasAttr(n, "id").split("|")[0].toLowerCase()];
        if (!e) atlasError("missing input image: " + atlasAttr(n, "id"));
        n = e.x ? atlasSetAttr(n, "ax", e.x) : atlasRemoveAttr(n, "ax");
        n = e.y ? atlasSetAttr(n, "ay", e.y) : atlasRemoveAttr(n, "ay");
        return atlasSetAttr(atlasSetAttr(n, "aw", e.width), "ah", e.height);
    });
    kernelx.io.writeText(info, xml.replace(found.block, block));
}

function atlasAncientBlock(xml, item, name) {
    var blocks = xml.match(/<Atlas\b[^>]*>[\s\S]*?<\/Atlas>/gi) || [];
    for (var i = 0; i < blocks.length; ++i)
        if (item ? atlasAttr(blocks[i], "id") === item || item.slice(-String(atlasAttr(blocks[i], "id")).length) === atlasAttr(blocks[i], "id") : atlasStem(atlasAttr(blocks[i], "path") || "") === name) return blocks[i];
    return null;
}

function atlasAncientCut(input, folder, info, item) {
    var xml = kernelx.io.readText(info),
        block = atlasAncientBlock(xml, item, atlasStem(input));
    if (!block) atlasError("atlas item not found");
    var id = item || atlasAttr(block, "id"),
        prefix = "",
        entries = [];
    block.replace(/<(SetDefaults|Image)\b[^>]*\/?>(?:[^<]*<\/(?:SetDefaults|Image)>)?/gi, function(node, tag) {
        if (tag.toLowerCase() === "setdefaults") prefix = atlasAttr(node, "idprefix") || "";
        else entries.push({
            id: prefix + atlasAttr(node, "id"),
            x: atlasInt(atlasAttr(node, "x"), "x"),
            y: atlasInt(atlasAttr(node, "y"), "y"),
            width: atlasInt(atlasAttr(node, "width"), "width"),
            height: atlasInt(atlasAttr(node, "height"), "height")
        });
        return node;
    });
    if (!entries.length) atlasError("atlas has no sprites");
    atlasCut(input, folder, entries);
    atlasWriteId(folder, id);
}

function atlasAncientSplice(folder, output, info, item, w, h) {
    var packed = atlasSplice(folder, output, w, h),
        xml = kernelx.io.readText(info),
        block = atlasAncientBlock(xml, item || atlasReadId(folder), atlasStem(output)),
        prefix = "";
    if (!block) atlasError("atlas item not found");
    var changed = block.replace(/<(SetDefaults|Image)\b[^>]*\/?>(?:[^<]*<\/(?:SetDefaults|Image)>)?/gi, function(node, tag) {
        if (tag.toLowerCase() === "setdefaults") {
            prefix = atlasAttr(node, "idprefix") || "";
            return node;
        }
        var e = packed[(prefix + atlasAttr(node, "id")).toLowerCase()];
        if (!e) atlasError("missing input image: " + prefix + atlasAttr(node, "id"));
        return atlasSetAttr(atlasSetAttr(atlasSetAttr(atlasSetAttr(node, "x", e.x), "y", e.y), "width", e.width), "height", e.height);
    });
    kernelx.io.writeText(info, xml.replace(block, changed));
}

function atlasTvNodes(xml) {
    return xml.match(/<[A-Za-z_][\w:.-]*\b[^>]*\/?>(?:[^<]*<\/[A-Za-z_][\w:.-]*>)?/g) || [];
}

function atlasTvSprite(node) {
    return atlasAttr(node, "name") !== null && atlasAttr(node, "x") !== null && atlasAttr(node, "y") !== null && atlasAttr(node, "w") !== null && atlasAttr(node, "h") !== null;
}

function atlasTvCut(input, folder, info) {
    var xml = kernelx.io.readText(info),
        block = (xml.match(/<atlas\b[^>]*>[\s\S]*?<\/atlas>/i) || [])[0];
    if (!block) atlasError("atlas item not found");
    var entries = atlasTvNodes(block).filter(atlasTvSprite).map(function(n) {
        return {
            id: atlasKey(atlasAttr(n, "name")),
            x: atlasInt(atlasAttr(n, "x"), "x"),
            y: atlasInt(atlasAttr(n, "y"), "y"),
            width: atlasInt(atlasAttr(n, "w"), "w"),
            height: atlasInt(atlasAttr(n, "h"), "h")
        };
    });
    if (!entries.length) atlasError("atlas has no sprites");
    atlasCut(input, folder, entries);
}

function atlasTvSplice(folder, output, info, item, w, h) {
    var packed = atlasSplice(folder, output, w, h),
        xml = kernelx.io.readText(info);
    xml = xml.replace(/<[A-Za-z_][\w:.-]*\b[^>]*\/?>(?:[^<]*<\/[A-Za-z_][\w:.-]*>)?/g, function(n) {
        if (!atlasTvSprite(n)) return n;
        var e = packed[atlasKey(atlasAttr(n, "name"))];
        if (!e) atlasError("missing input image: " + atlasAttr(n, "name"));
        return atlasSetAttr(atlasSetAttr(atlasSetAttr(atlasSetAttr(n, "x", e.x), "y", e.y), "w", e.width), "h", e.height);
    });
    kernelx.io.writeText(info, xml);
}

function atlasPlistFrames(xml) {
    var marker = /<key>\s*frames\s*<\/key>\s*<dict>/i.exec(xml);
    if (!marker) atlasError("plist frames dictionary not found");
    var start = marker.index + marker[0].length,
        depth = 1,
        token = /<\/?dict\b[^>]*>/ig,
        match;
    token.lastIndex = start;
    while ((match = token.exec(xml))) {
        if (match[0].slice(1, 2) === "/") --depth;
        else ++depth;
        if (!depth) return {
            start: start,
            end: match.index,
            text: xml.slice(start, match.index)
        };
    }
    atlasError("invalid plist frames dictionary");
}

function atlasPlistValue(block, key) {
    var m = new RegExp("<key>\\s*" + key + "\\s*<\\/key>\\s*<[^>]+>([\\s\\S]*?)<\\/[^>]+>", "i").exec(block);
    return m ? m[1].trim() : "";
}

function atlasPlistSetValue(block, key, value, tag) {
    var re = new RegExp("(<key>\\s*" + key + "\\s*<\\/key>\\s*<)([^ >]+)([^>]*>)[\\s\\S]*?(<\\/\\2>)", "i");
    return re.test(block) ? block.replace(re, "$1$2$3" + value + "$4") : block;
}

function atlasPlistEntries(xml) {
    var text = atlasPlistFrames(xml).text,
        re = /<key>([\s\S]*?)<\/key>\s*<dict>([\s\S]*?)<\/dict>/ig,
        entries = [],
        m;
    while ((m = re.exec(text))) {
        var rect = atlasPlistValue(m[2], "textureRect"),
            nums = rect.match(/-?\d+/g);
        if (!nums || nums.length < 4) atlasError("invalid plist textureRect: " + m[1]);
        entries.push({
            id: atlasKey(m[1].trim()),
            name: m[1].trim(),
            x: atlasInt(nums[0], "x"),
            y: atlasInt(nums[1], "y"),
            width: atlasInt(nums[2], "width"),
            height: atlasInt(nums[3], "height"),
            rotated: /<key>\s*textureRotated\s*<\/key>\s*<true\s*\/>/i.test(m[2])
        });
    }
    return entries;
}

function atlasPlistCut(input, folder, info) {
    var entries = atlasPlistEntries(kernelx.io.readText(info));
    if (!entries.length) atlasError("atlas has no sprites");
    atlasCut(input, folder, entries);
}

function atlasPlistSplice(folder, output, info, item, w, h) {
    var packed = atlasSplice(folder, output, w, h),
        xml = kernelx.io.readText(info),
        frame = atlasPlistFrames(xml),
        re = /(<key>([\s\S]*?)<\/key>\s*<dict>)([\s\S]*?)(<\/dict>)/ig;
    var changed = frame.text.replace(re, function(all, begin, name, body, end) {
        var e = packed[atlasKey(name.trim())];
        if (!e) atlasError("missing input image: " + name.trim());
        body = atlasPlistSetValue(body, "spriteOffset", "{0,0}");
        body = atlasPlistSetValue(body, "spriteSize", "{" + e.width + "," + e.height + "}");
        body = atlasPlistSetValue(body, "spriteSourceSize", "{" + e.width + "," + e.height + "}");
        body = atlasPlistSetValue(body, "textureRect", "{{" + e.x + "," + e.y + "},{" + e.width + "," + e.height + "}}");
        body = body.replace(/(<key>\s*textureRotated\s*<\/key>\s*)<true\s*\/>/i, "$1<false/>");
        return begin + body + end;
    });
    kernelx.io.writeText(info, xml.slice(0, frame.start) + changed + xml.slice(frame.end));
}

function atlasUtf8(bytes, start, length) {
    var result = "",
        end = start + length;
    while (start < end) {
        var a = bytes[start++];
        if (a < 128) result += String.fromCharCode(a);
        else if (a >= 192 && a < 224 && start < end) result += String.fromCharCode((a & 31) << 6 | bytes[start++] & 63);
        else if (a >= 224 && a < 240 && start + 1 < end) {
            var b = bytes[start++],
                c = bytes[start++];
            result += String.fromCharCode((a & 15) << 12 | (b & 63) << 6 | c & 63);
        } else if (a >= 240 && a < 248 && start + 2 < end) {
            var d = ((a & 7) << 18 | (bytes[start++] & 63) << 12 | (bytes[start++] & 63) << 6 | bytes[start++] & 63) - 65536;
            result += String.fromCharCode(55296 + (d >> 10), 56320 + (d & 1023));
        } else atlasError("invalid UTF-8 string in image.dat");
    }
    return result;
}

function atlasDatEntries(bytes, item) {
    var view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength),
        offset = 0,
        count = view.getInt32(offset, true);
    offset += 4, entries = [];

    function str() {
        if (offset + 2 > bytes.length) atlasError("truncated image.dat");
        var n = view.getUint16(offset, true);
        offset += 2;
        if (offset + n > bytes.length) atlasError("truncated image.dat string");
        var result = atlasUtf8(bytes, offset, n);
        offset += n;
        return result;
    }
    for (var i = 0; i < count; ++i) {
        if (offset + 4 > bytes.length || view.getInt32(offset, true) !== 4) atlasError("invalid image.dat");
        offset += 4;
        var id = str();
        str();
        str();
        if (offset + 49 > bytes.length) atlasError("truncated image.dat entry");
        offset++;
        var pos = offset;
        var x = view.getInt32(offset, true),
            y = view.getInt32(offset + 4, true),
            width = view.getInt32(offset + 8, true),
            height = view.getInt32(offset + 12, true);
        offset += 48;
        var parent = str();
        if (offset + 4 > bytes.length) atlasError("truncated image.dat entry");
        offset += 4;
        if (parent === item) entries.push({
            id: id,
            x: x,
            y: y,
            width: width,
            height: height,
            pos: pos
        });
    }
    return entries;
}

function atlasDatCut(input, folder, info, item) {
    if (!item) atlasError("ItemName is required for imagedat cut");
    var entries = atlasDatEntries(kernelx.io.readBytes(info), item);
    if (!entries.length) atlasError("atlas has no sprites");
    atlasCut(input, folder, entries);
    atlasWriteId(folder, item);
}

function atlasDatSplice(folder, output, info, item, w, h) {
    var packed = atlasSplice(folder, output, w, h),
        id = item || atlasReadId(folder);
    if (!id) atlasError("ItemName or AtlasID.txt is required");
    var bytes = kernelx.io.readBytes(info),
        entries = atlasDatEntries(bytes, id),
        view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
    for (var i = 0; i < entries.length; ++i) {
        var e = packed[entries[i].id.toLowerCase()];
        if (!e) atlasError("missing input image: " + entries[i].id);
        view.setInt32(entries[i].pos, e.x, true);
        view.setInt32(entries[i].pos + 4, e.y, true);
        view.setInt32(entries[i].pos + 8, e.width, true);
        view.setInt32(entries[i].pos + 12, e.height, true);
    }
    kernelx.io.writeBytes(info, bytes);
}

function atlasJsonFind(json, item, name) {
    var groups = json.groups || [];
    for (var i = 0; i < groups.length; ++i)
        if (groups[i].type === "simple" && groups[i].resources)
            for (var j = 0; j < groups[i].resources.length; ++j) {
                var r = groups[i].resources[j];
                if (r.type === "Image" && r.atlas === true && (item ? r.id === item : r.path && r.path[r.path.length - 1] && String(r.path[r.path.length - 1]).toLowerCase() === name)) return {
                    resources: groups[i].resources,
                    atlas: r
                };
            }
    return null;
}

function atlasJsonCut(input, folder, info, item) {
    var json = JSON.parse(kernelx.io.readText(info)),
        found = atlasJsonFind(json, item, atlasStem(input));
    if (!found) atlasError("atlas item not found");
    var entries = found.resources.filter(function(r) {
        return r.type === "Image" && r.parent === found.atlas.id;
    }).map(function(r) {
        return {
            id: r.id,
            x: r.ax || 0,
            y: r.ay || 0,
            width: r.aw || 0,
            height: r.ah || 0
        };
    });
    if (!entries.length) atlasError("atlas has no sprites");
    atlasCut(input, folder, entries);
    atlasWriteId(folder, found.atlas.id);
}

function atlasJsonSplice(folder, output, info, item, w, h) {
    var packed = atlasSplice(folder, output, w, h),
        json = JSON.parse(kernelx.io.readText(info)),
        found = atlasJsonFind(json, item || atlasReadId(folder), atlasStem(output));
    if (!found) atlasError("atlas item not found");
    found.atlas.width = w;
    found.atlas.height = h;
    for (var i = 0; i < found.resources.length; ++i) {
        var r = found.resources[i];
        if (r.type === "Image" && r.parent === found.atlas.id) {
            var e = packed[String(r.id).toLowerCase()];
            if (!e) atlasError("missing input image: " + r.id);
            r.ax = e.x;
            r.ay = e.y;
            r.aw = e.width;
            r.ah = e.height;
        }
    }
    kernelx.io.writeText(info, JSON.stringify(json, null, 2));
}

function execute(params) {
    try {
        var format = params.Format || "newxml",
            cut = !!params.InputFile;
        if (cut) {
            if (!params.OutputFolder || !params.InfoFile) atlasError("OutputFolder and InfoFile are required");
            if (format === "newxml") atlasNewCut(params.InputFile, params.OutputFolder, params.InfoFile, params.ItemName);
            else if (format === "oldxml") atlasOldCut(params.InputFile, params.OutputFolder, params.InfoFile, params.ItemName);
            else if (format === "ancientxml") atlasAncientCut(params.InputFile, params.OutputFolder, params.InfoFile, params.ItemName);
            else if (format === "tvatlasxml") atlasTvCut(params.InputFile, params.OutputFolder, params.InfoFile);
            else if (format === "imagedat") atlasDatCut(params.InputFile, params.OutputFolder, params.InfoFile, params.ItemName);
            else if (format === "resrton") atlasJsonCut(params.InputFile, params.OutputFolder, params.InfoFile, params.ItemName);
            else if (format === "plist") atlasPlistCut(params.InputFile, params.OutputFolder, params.InfoFile);
            return {
                success: true,
                output: params.OutputFolder,
                format: format
            };
        }
        if (!params.InputFolder || !params.OutputFile || !params.InfoFile) atlasError("InputFolder, OutputFile and InfoFile are required");
        if (format === "newxml") atlasNewSplice(params.InputFolder, params.OutputFile, params.InfoFile, params.ItemName, params.MaxWidth || 2048, params.MaxHeight || 2048);
        else if (format === "oldxml") atlasOldSplice(params.InputFolder, params.OutputFile, params.InfoFile, params.ItemName, params.MaxWidth || 2048, params.MaxHeight || 2048);
        else if (format === "ancientxml") atlasAncientSplice(params.InputFolder, params.OutputFile, params.InfoFile, params.ItemName, params.MaxWidth || 2048, params.MaxHeight || 2048);
        else if (format === "tvatlasxml") atlasTvSplice(params.InputFolder, params.OutputFile, params.InfoFile, params.ItemName, params.MaxWidth || 2048, params.MaxHeight || 2048);
        else if (format === "imagedat") atlasDatSplice(params.InputFolder, params.OutputFile, params.InfoFile, params.ItemName, params.MaxWidth || 2048, params.MaxHeight || 2048);
        else if (format === "resrton") atlasJsonSplice(params.InputFolder, params.OutputFile, params.InfoFile, params.ItemName, params.MaxWidth || 2048, params.MaxHeight || 2048);
        else if (format === "plist") atlasPlistSplice(params.InputFolder, params.OutputFile, params.InfoFile, params.ItemName, params.MaxWidth || 2048, params.MaxHeight || 2048);
        return {
            success: true,
            output: params.OutputFile,
            format: format
        };
    } catch (error) {
        return {
            success: false,
            error: error && error.message ? error.message : String(error)
        };
    }
}