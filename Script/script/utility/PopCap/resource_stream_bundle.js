/* kernelx-manifest
[
  {
    "id": "popcap.resource_stream_bundle.pack",
    "implementation": "implementation",
    "buffer_size": "1024m",
    "params": [
      { "name": "InputFolder", "type": "path", "required": true, "folder": true, "extensions": [".rsb.bundle"], "language": "bundle_directory" },
      { "name": "OutputFile", "type": "path", "required": true, "extensions": [".rsb"], "language": "data_file" },
      { "name": "Version", "type": "list", "default": "4", "list": ["1", "3", "4"], "language": "version_number" },
      { "name": "ExtendedTextureInformation", "type": "list", "default": "0", "list": ["0", "1", "2", "3"] },
      { "name": "Layout", "type": "list", "default": "resource", "list": ["group", "subgroup", "resource"] },
      { "name": "InputPacket", "type": "list", "default": "false", "list": ["false", "true"] },
      { "name": "OutputNewPacket", "type": "list", "default": "false", "list": ["false", "true"] }
    ]
  },
  {
    "id": "popcap.resource_stream_bundle.unpack",
    "implementation": "implementation",
    "buffer_size": "1024m",
    "params": [
      { "name": "InputFile", "type": "path", "required": true, "extensions": [".rsb"], "language": "data_file" },
      { "name": "OutputFolder", "type": "path", "required": true, "folder": true, "extensions": [".rsb.bundle"], "language": "bundle_directory" },
      { "name": "Version", "type": "list", "default": "4", "list": ["1", "3", "4"], "language": "version_number" },
      { "name": "ExtendedTextureInformation", "type": "list", "default": "0", "list": ["0", "1", "2", "3"] },
      { "name": "Layout", "type": "list", "default": "resource", "list": ["group", "subgroup", "resource"] },
      { "name": "OutputResource", "type": "list", "default": "true", "list": ["false", "true"] },
      { "name": "OutputPacket", "type": "list", "default": "false", "list": ["false", "true"] }
    ]
  }
]
*/

var RSB_MAGIC = 0x72736231,
    RSB_ALIGNMENT = 0x1000;

function rsbError(m) {
    throw new Error("ResourceStreamBundle: " + m);
}

function rsbView(b) {
    return new DataView(b.buffer, b.byteOffset, b.byteLength);
}

function rsbU32(v, p) {
    return v.getUint32(p, true);
}

function rsbU16(v, p) {
    return v.getUint16(p, true);
}

function rsbSet32(v, p, n) {
    if (!Number.isSafeInteger(n) || n < 0 || n > 0xffffffff) rsbError("32-bit field is out of range");
    v.setUint32(p, n, true);
}

function rsbSet16(v, p, n) {
    if (!Number.isSafeInteger(n) || n < 0 || n > 0xffff) rsbError("16-bit field is out of range");
    v.setUint16(p, n, true);
}

function rsbAlign(n) {
    return Math.ceil(n / RSB_ALIGNMENT) * RSB_ALIGNMENT;
}

function rsbSlice(b, p, n, d) {
    if (!Number.isSafeInteger(p) || !Number.isSafeInteger(n) || p < 0 || n < 0 || p > b.length - n) rsbError("invalid " + d + " range");
    return b.subarray(p, p + n);
}

function rsbConcat(a, n) {
    var r = new Uint8Array(n),
        p = 0;
    for (var i = 0; i < a.length; ++i) {
        r.set(a[i], p);
        p += a[i].length;
    }
    return r;
}

function rsbBool(v) {
    return v === true || v === 1 || v === "1" || String(v).toLowerCase() === "true";
}

function rsbNumber(v, d) {
    v = Number(v);
    if (!Number.isSafeInteger(v) || v < 0 || v > 0xffffffff) rsbError("invalid " + d);
    return v;
}

function rsbUtf8(s) {
    return (typeof rsgpUtf8Encode === "function" ? rsgpUtf8Encode : function(x) {
        return new TextEncoder().encode(x);
    })(s);
}

function rsbText(b) {
    return (typeof rsgpUtf8Decode === "function" ? rsgpUtf8Decode : function(x) {
        return new TextDecoder().decode(x);
    })(b);
}

function rsbKey(s) {
    return rsbUtf8(String(s).replace(/\\/g, "/").replace(/\//g, "\\").toUpperCase());
}

function rsbCmp(a, b) {
    for (var i = 0, n = Math.min(a.length, b.length); i < n; ++i)
        if (a[i] !== b[i]) return a[i] - b[i];
    return a.length - b.length;
}

function rsbCommon(a, b) {
    var i = 0,
        n = Math.min(a.length, b.length);
    while (i < n && a[i] === b[i]) ++i;
    return i;
}

function rsbName(b, p) {
    var e = p;
    while (e < p + 128 && b[e]) ++e;
    return rsbText(b.subarray(p, e));
}

function rsbPutName(b, p, s, d) {
    var x = rsbUtf8(String(s));
    if (x.length >= 128) rsbError(d + " is too long");
    b.set(x, p);
}

function rsbType(a, d) {
    var t = a && a.type;
    if (t === "general") return 0;
    if (t === "texture") return 1;
    return rsbNumber(t, d + " resource type");
}

function rsbLayout(mode, group, subgroup) {
    if (mode === "group") return {
        resource: "group/" + group + "/" + subgroup + "/resource",
        packet: "group/" + group + "/" + subgroup + "/packet.rsg"
    };
    if (mode === "subgroup") return {
        resource: "subgroup/" + subgroup + "/resource",
        packet: "subgroup/" + subgroup + "/packet.rsg"
    };
    return {
        resource: "resource",
        packet: "packet/" + subgroup + ".rsg"
    };
}

function rsbStandardGroup(id, composite) {
    return composite ? id : id + "_CompositeShell";
}

function rsbOriginalGroup(id) {
    var suffix = "_COMPOSITESHELL";
    return id.toUpperCase().slice(-suffix.length) === suffix ? {
        identifier: id.slice(0, -suffix.length),
        composite: false
    } : {
        identifier: id,
        composite: true
    };
}

function rsbEncodeMap(entries) {
    var a = [],
        seen = Object.create(null),
        i, j;
    for (i = 0; i < entries.length; ++i) {
        var k = rsbKey(entries[i].key),
            sig = Array.prototype.join.call(k, ",");
        if (seen[sig]) rsbError("duplicate map key: " + entries[i].key);
        seen[sig] = true;
        a.push({
            key: k,
            value: rsbNumber(entries[i].value, "map value")
        });
    }
    a.sort(function(x, y) {
        return -rsbCmp(x.key, y.key);
    });
    var options = new Array(a.length),
        words = [];
    if (a.length) options[0] = {
        inherit: 0,
        parent: 0
    };
    for (i = 0; i < a.length; ++i) {
        var current = options[i];
        if (!current) rsbError("invalid map ordering");
        var child = Object.create(null);
        for (j = i + 1; j < a.length; ++j)
            if (!options[j]) {
                var common = rsbCommon(a[i].key, a[j].key);
                if (!child[common] && common >= current.inherit) {
                    child[common] = true;
                    options[j] = {
                        inherit: common,
                        parent: words.length + common - current.inherit
                    };
                }
            }
        if (i) {
            var pointer = words.length;
            if (current.parent >= words.length || pointer > 0xffffff) rsbError("map is too large");
            words[current.parent] = (words[current.parent] & 0xff) | pointer << 8;
        }
        for (j = current.inherit; j < a[i].key.length; ++j) words.push(a[i].key[j]);
        words.push(0, a[i].value);
    }
    var out = new Uint8Array(words.length * 4),
        view = rsbView(out);
    for (i = 0; i < words.length; ++i) rsbSet32(view, i * 4, words[i] >>> 0);
    return out;
}

function rsbDecodeMap(b) {
    if (b.length % 4) rsbError("map is not word aligned");
    var v = rsbView(b),
        p = 0,
        parent = new Array(b.length / 4),
        result = [];
    while (p < b.length) {
        var begin = p,
            prefix = parent[begin / 4] || [],
            key = prefix.slice(),
            child = [];
        for (;;) {
            if (p + 4 > b.length) rsbError("truncated map");
            var x = rsbU32(v, p),
                c = x & 255,
                q = x >>> 8;
            p += 4;
            if (!c) break;
            if (q) child.push({
                offset: q,
                prefix: key.slice()
            });
            key.push(c);
        }
        for (var i = 0; i < child.length; ++i) {
            if (child[i].offset >= parent.length || parent[child[i].offset]) rsbError("invalid map pointer");
            parent[child[i].offset] = child[i].prefix;
        }
        if (p + 4 > b.length) rsbError("truncated map value");
        result.push({
            key: rsbText(new Uint8Array(key)),
            value: rsbU32(v, p)
        });
        p += 4;
    }
    return result;
}

function rsbHeaderSize(version) {
    return version === 4 ? 104 : 100;
}

function rsbGroupSize(version) {
    return 128 + 64 * (version === 1 ? 8 : 16) + 4;
}

function rsbSubgroupSize(version) {
    return version === 1 ? 196 : 204;
}

function rsbTextureSize(version, extended) {
    return version !== 4 || extended === 0 ? 16 : extended === 1 ? 20 : 24;
}

function rsbFourCC(s) {
    if (!s) return 0;
    s = String(s);
    if (rsbUtf8(s).length !== 4) rsbError("locale must contain four ASCII bytes");
    var b = rsbUtf8(s);
    return (b[0] << 24 | b[1] << 16 | b[2] << 8 | b[3]) >>> 0;
}

function rsbFourCCText(n) {
    if (!n) return undefined;
    return String.fromCharCode(n >>> 24 & 255, n >>> 16 & 255, n >>> 8 & 255, n & 255);
}

function rsbReadManifest(bytes, h, version) {
    if (!h.groupManifest || !h.resourceManifest || !h.stringManifest) return null;
    var strings = rsbSlice(bytes, h.stringManifest, h.informationSize - h.stringManifest, "manifest strings");

    function textAt(p) {
        if (p >= strings.length) rsbError("invalid manifest string offset");
        var e = p;
        while (e < strings.length && strings[e]) ++e;
        if (e === strings.length) rsbError("unterminated manifest string");
        return rsbText(strings.subarray(p, e));
    }
    var groupBytes = rsbSlice(bytes, h.groupManifest, h.resourceManifest - h.groupManifest, "group manifest"),
        resourceBytes = rsbSlice(bytes, h.resourceManifest, h.stringManifest - h.resourceManifest, "resource manifest"),
        gv = rsbView(groupBytes),
        rv = rsbView(resourceBytes),
        p = 0,
        groups = [];
    while (p < groupBytes.length) {
        if (p + 12 > groupBytes.length) rsbError("truncated group manifest");
        var id = rsbOriginalGroup(textAt(rsbU32(gv, p))),
            count = rsbU32(gv, p + 4),
            subSize = rsbU32(gv, p + 8);
        p += 12;
        if (subSize !== (version === 1 ? 12 : 16) || p + count * subSize > groupBytes.length) rsbError("invalid subgroup manifest");
        var g = {
            identifier: id.identifier,
            composite: id.composite,
            subgroup: []
        };
        for (var si = 0; si < count; ++si) {
            var resolution = rsbU32(gv, p),
                locale = version === 1 ? 0 : rsbU32(gv, p + 4),
                idOff = rsbU32(gv, p + subSize - 8),
                rc = rsbU32(gv, p + subSize - 4),
                sg = {
                    identifier: textAt(idOff),
                    category: {},
                    resource: []
                };
            if (resolution) sg.category.resolution = resolution;
            if (locale) sg.category.locale = rsbFourCCText(locale);
            p += subSize;
            for (var ri = 0; ri < rc; ++ri) {
                if (p + 4 > groupBytes.length) rsbError("truncated resource manifest index");
                var d = rsbU32(gv, p);
                p += 4;
                if (d + 28 > resourceBytes.length) rsbError("invalid resource manifest detail");
                var type = rsbU16(rv, d + 4),
                    propOff = rsbU32(rv, d + 8),
                    imageOff = rsbU32(rv, d + 12),
                    ident = textAt(rsbU32(rv, d + 16)),
                    path = textAt(rsbU32(rv, d + 20)),
                    pc = rsbU32(rv, d + 24),
                    prop = {};
                if (imageOff) {
                    if (imageOff + 24 > resourceBytes.length) rsbError("invalid image property");
                    var names = ["type", "flag", "x", "y", "ax", "ay", "aw", "ah", "rows", "cols"];
                    for (var ni = 0; ni < names.length; ++ni) prop[names[ni]] = String(rsbU16(rv, imageOff + ni * 2));
                    prop.parent = textAt(rsbU32(rv, imageOff + 20));
                }
                if (propOff + pc * 12 > resourceBytes.length) rsbError("invalid resource properties");
                for (var pi = 0; pi < pc; ++pi) prop[textAt(rsbU32(rv, propOff + pi * 12))] = textAt(rsbU32(rv, propOff + pi * 12 + 8));
                sg.resource.push({
                    identifier: ident,
                    path: path,
                    type: type,
                    property: prop
                });
            }
            g.subgroup.push(sg);
        }
        groups.push(g);
    }
    return {
        group: groups
    };
}

function rsbWriteManifest(manifest, version) {
    if (!manifest) return null;
    if (!Array.isArray(manifest.group)) rsbError("manifest group must be an array");
    var stringParts = [],
        strings = Object.create(null),
        stringSize = 0,
        subgroupSize = version === 1 ? 12 : 16,
        details = [],
        detailSize = 0,
        groups = [];

    function setString(s) {
        s = String(s === undefined ? "" : s);
        if (Object.prototype.hasOwnProperty.call(strings, s)) return strings[s];
        var o = stringSize,
            x = rsbUtf8(s),
            z = new Uint8Array(x.length + 1);
        z.set(x);
        strings[s] = o;
        stringParts.push(z);
        stringSize += z.length;
        return o;
    }

    function addDetail(r) {
        var prop = r.property || {},
            image = rsbNumber(r.type, "manifest resource type") === 0,
            keys = Object.keys(prop).filter(function(k) {
                return !image || ["type", "flag", "x", "y", "ax", "ay", "aw", "ah", "rows", "cols", "parent"].indexOf(k) < 0;
            }),
            size = 28 + (image ? 24 : 0) + keys.length * 12,
            b = new Uint8Array(size),
            v = rsbView(b),
            offset = detailSize,
            base = 28 + (image ? 24 : 0);
        rsbSet16(v, 4, rsbNumber(r.type, "manifest resource type"));
        rsbSet16(v, 6, 28);
        rsbSet32(v, 8, offset + base);
        rsbSet32(v, 12, image ? offset + 28 : 0);
        rsbSet32(v, 16, setString(r.identifier));
        rsbSet32(v, 20, setString(r.path));
        rsbSet32(v, 24, keys.length);
        if (image) {
            var names = ["type", "flag", "x", "y", "ax", "ay", "aw", "ah", "rows", "cols"];
            for (var ni = 0; ni < names.length; ++ni) rsbSet16(v, 28 + ni * 2, rsbNumber(prop[names[ni]], "image property " + names[ni]));
            rsbSet32(v, 48, setString(prop.parent));
        }
        for (var pi = 0; pi < keys.length; ++pi) {
            rsbSet32(v, base + pi * 12, setString(keys[pi]));
            rsbSet32(v, base + pi * 12 + 8, setString(prop[keys[pi]]));
        }
        details.push(b);
        detailSize += size;
        return offset;
    }
    setString("");
    for (var gi = 0; gi < manifest.group.length; ++gi) {
        var g = manifest.group[gi];
        if (!g || !Array.isArray(g.subgroup)) rsbError("invalid manifest group");
        var b = new Uint8Array(12),
            v = rsbView(b),
            parts = [b],
            size = b.length;
        rsbSet32(v, 0, setString(rsbStandardGroup(g.identifier, g.composite === true)));
        rsbSet32(v, 4, g.subgroup.length);
        rsbSet32(v, 8, subgroupSize);
        for (var si = 0; si < g.subgroup.length; ++si) {
            var sg = g.subgroup[si],
                cat = sg.category || {},
                sb = new Uint8Array(subgroupSize),
                sv = rsbView(sb);
            if (!sg || !Array.isArray(sg.resource)) rsbError("invalid manifest subgroup");
            rsbSet32(sv, 0, cat.resolution === undefined ? 0 : rsbNumber(cat.resolution, "manifest resolution"));
            if (version !== 1) rsbSet32(sv, 4, rsbFourCC(cat.locale));
            rsbSet32(sv, subgroupSize - 8, setString(sg.identifier));
            rsbSet32(sv, subgroupSize - 4, sg.resource.length);
            parts.push(sb);
            size += sb.length;
            for (var ri = 0; ri < sg.resource.length; ++ri) {
                var index = new Uint8Array(4);
                rsbSet32(rsbView(index), 0, addDetail(sg.resource[ri]));
                parts.push(index);
                size += 4;
            }
        }
        groups.push(rsbConcat(parts, size));
    }
    var groupParts = [],
        groupSize = 0;
    for (gi = 0; gi < groups.length; ++gi) {
        groupParts.push(groups[gi]);
        groupSize += groups[gi].length;
    }
    return {
        group: rsbConcat(groupParts, groupSize),
        detail: rsbConcat(details, detailSize),
        strings: rsbConcat(stringParts, stringSize)
    };
}

function rsbReadJson(path, what) {
    try {
        return JSON.parse(kernelx.io.readText(path));
    } catch (e) {
        rsbError("invalid " + what);
    }
}

function rsbWriteJson(path, value) {
    if (!kernelx.io.writeText(path, JSON.stringify(value, null, 4))) rsbError("unable to write " + path);
}

function rsbDefinition(definition, version, extended) {
    if (!definition || !Array.isArray(definition.group)) rsbError("definition group must be an array");
    var groups = [],
        subgroupCount = 0,
        textureCount = 0;
    for (var gi = 0; gi < definition.group.length; ++gi) {
        var g = definition.group[gi];
        if (!g || typeof g.identifier !== "string" || !Array.isArray(g.subgroup)) rsbError("invalid definition group " + gi);
        var group = {
            identifier: g.identifier,
            composite: g.composite === true,
            subgroup: []
        };
        for (var si = 0; si < g.subgroup.length; ++si) {
            var sg = g.subgroup[si];
            if (!sg || typeof sg.identifier !== "string" || !sg.compression || !Array.isArray(sg.resource)) rsbError("invalid definition subgroup " + gi + "/" + si);
            var subgroup = {
                identifier: sg.identifier,
                category: sg.category || {},
                compression: {
                    general: sg.compression.general === true,
                    texture: sg.compression.texture === true
                },
                resource: []
            };
            for (var ri = 0; ri < sg.resource.length; ++ri) {
                var r = sg.resource[ri],
                    type = rsbType(r.additional, "definition");
                if (!r || typeof r.path !== "string" || !r.additional || (type !== 0 && type !== 1)) rsbError("invalid definition resource " + gi + "/" + si + "/" + ri);
                if (type === 1) {
                    var a = r.additional.value;
                    if (!a || !Array.isArray(a.size) || a.size.length !== 2) rsbError("invalid texture definition");
                    rsbNumber(a.size[0], "texture width");
                    rsbNumber(a.size[1], "texture height");
                    rsbNumber(a.format, "texture format");
                    rsbNumber(a.pitch, "texture pitch");
                    if (version === 4 && extended >= 1) rsbNumber(a.additional_byte_count, "texture additional byte count");
                    if (version === 4 && extended >= 2) rsbNumber(a.scale, "texture scale");
                    ++textureCount;
                }
                subgroup.resource.push(r);
            }
            group.subgroup.push(subgroup);
            ++subgroupCount;
        }
        groups.push(group);
    }
    return {
        group: groups,
        subgroupCount: subgroupCount,
        textureCount: textureCount
    };
}

function rsbMakePacket(folder, layout, group, subgroup, version, usePacket, writePacket) {
    var packetPath = kernelx.path.join(folder, layout.packet),
        resources = [],
        definition = {
            compression: subgroup.compression,
            resource: []
        },
        textureIndex = 0;
    if (usePacket && kernelx.io.isFile(packetPath)) return new Uint8Array(kernelx.io.readBytes(packetPath));
    for (var i = 0; i < subgroup.resource.length; ++i) {
        var r = subgroup.resource[i],
            path = String(r.path).replace(/\\/g, "/"),
            file = kernelx.path.join(kernelx.path.join(folder, layout.resource), path),
            type = rsbType(r.additional, "definition"),
            value = r.additional.value || {};
        if (!kernelx.io.isFile(file)) rsbError("resource does not exist: " + path);
        if (type === 1) {
            value = {
                index: textureIndex++,
                size: value.size
            };
        }
        definition.resource.push({
            path: path,
            additional: {
                type: type,
                value: value
            }
        });
        resources.push(new Uint8Array(kernelx.io.readBytes(file)));
    }
    var packet = ResourceStreamGroupCore.encode(definition, resources, version);
    if (writePacket) {
        if (!kernelx.io.mkdir(kernelx.path.dir(packetPath)) || !kernelx.io.writeBytes(packetPath, packet)) rsbError("unable to write packet: " + packetPath);
    }
    return packet;
}

function rsbPack(folder, output, version, extended, layoutMode, inputPacket, outputPacket) {
    var definitionPath = kernelx.path.join(folder, "definition.json");
    if (!kernelx.io.isFile(definitionPath)) rsbError("InputFolder must contain definition.json");
    var definition = rsbDefinition(rsbReadJson(definitionPath, "definition.json"), version, extended),
        manifestPath = kernelx.path.join(folder, "manifest.json"),
        manifest = kernelx.io.isFile(manifestPath) ? rsbReadJson(manifestPath, "manifest.json") : null,
        manifestData = rsbWriteManifest(manifest, version),
        headerSize = rsbHeaderSize(version),
        groupSize = rsbGroupSize(version),
        subgroupSize = rsbSubgroupSize(version),
        textureSize = rsbTextureSize(version, extended),
        groupEntries = [],
        subgroupEntries = [],
        resourceEntries = [],
        packets = [],
        globalSubgroup = 0,
        globalTexture = 0;
    for (var gi = 0; gi < definition.group.length; ++gi) {
        var g = definition.group[gi];
        groupEntries.push({
            key: rsbStandardGroup(g.identifier, g.composite),
            value: gi
        });
        for (var si = 0; si < g.subgroup.length; ++si) {
            var sg = g.subgroup[si],
                layout = rsbLayout(layoutMode, g.identifier, sg.identifier),
                packet = rsbMakePacket(folder, layout, g, sg, version, inputPacket, outputPacket);
            subgroupEntries.push({
                key: sg.identifier,
                value: globalSubgroup
            });
            for (var ri = 0; ri < sg.resource.length; ++ri) resourceEntries.push({
                key: sg.resource[ri].path,
                value: globalSubgroup
            });
            packets.push(packet);
            ++globalSubgroup;
        }
    }
    var groupMap = rsbEncodeMap(groupEntries),
        subgroupMap = rsbEncodeMap(subgroupEntries),
        resourceMap = rsbEncodeMap(resourceEntries),
        offset = 8 + headerSize,
        resourceMapOffset = offset;
    offset += resourceMap.length;
    var subgroupMapOffset = offset;
    offset += subgroupMap.length;
    var groupInfoOffset = offset;
    offset += definition.group.length * groupSize;
    var groupMapOffset = offset;
    offset += groupMap.length;
    var subgroupInfoOffset = offset;
    offset += definition.subgroupCount * subgroupSize;
    var poolOffset = offset;
    offset += definition.subgroupCount * 152;
    var textureOffset = offset;
    offset += definition.textureCount * textureSize;
    var informationWithoutManifest = version === 4 ? rsbAlign(offset) : 0;
    if (version === 4) offset = informationWithoutManifest;
    var groupManifestOffset = 0,
        resourceManifestOffset = 0,
        stringManifestOffset = 0;
    if (manifestData) {
        groupManifestOffset = offset;
        offset += manifestData.group.length;
        resourceManifestOffset = offset;
        offset += manifestData.detail.length;
        stringManifestOffset = offset;
        offset += manifestData.strings.length;
    }
    var informationSize = rsbAlign(offset),
        packetOffset = informationSize,
        packetOffsets = [];
    for (var pi = 0; pi < packets.length; ++pi) {
        packetOffsets.push(packetOffset);
        packetOffset += packets[pi].length;
    }
    var out = new Uint8Array(packetOffset),
        v = rsbView(out),
        h = 8;
    rsbSet32(v, 0, RSB_MAGIC);
    rsbSet32(v, 4, version);
    rsbSet32(v, h, version < 3 ? 1 : 0);
    rsbSet32(v, h + 4, informationSize);
    rsbSet32(v, h + 8, resourceMap.length);
    rsbSet32(v, h + 12, resourceMapOffset);
    rsbSet32(v, h + 24, subgroupMap.length);
    rsbSet32(v, h + 28, subgroupMapOffset);
    rsbSet32(v, h + 32, definition.subgroupCount);
    rsbSet32(v, h + 36, subgroupInfoOffset);
    rsbSet32(v, h + 40, subgroupSize);
    rsbSet32(v, h + 44, definition.group.length);
    rsbSet32(v, h + 48, groupInfoOffset);
    rsbSet32(v, h + 52, groupSize);
    rsbSet32(v, h + 56, groupMap.length);
    rsbSet32(v, h + 60, groupMapOffset);
    rsbSet32(v, h + 64, definition.subgroupCount);
    rsbSet32(v, h + 68, poolOffset);
    rsbSet32(v, h + 72, 152);
    rsbSet32(v, h + 76, definition.textureCount);
    rsbSet32(v, h + 80, textureOffset);
    rsbSet32(v, h + 84, textureSize);
    rsbSet32(v, h + 88, groupManifestOffset);
    rsbSet32(v, h + 92, resourceManifestOffset);
    rsbSet32(v, h + 96, stringManifestOffset);
    if (version === 4) rsbSet32(v, h + 100, informationWithoutManifest);
    out.set(resourceMap, resourceMapOffset);
    out.set(subgroupMap, subgroupMapOffset);
    out.set(groupMap, groupMapOffset);
    if (manifestData) {
        out.set(manifestData.group, groupManifestOffset);
        out.set(manifestData.detail, resourceManifestOffset);
        out.set(manifestData.strings, stringManifestOffset);
    }
    globalSubgroup = 0;
    globalTexture = 0;
    for (gi = 0; gi < definition.group.length; ++gi) {
        g = definition.group[gi];
        var go = groupInfoOffset + gi * groupSize;
        rsbPutName(out, go, rsbStandardGroup(g.identifier, g.composite), "group identifier");
        rsbSet32(v, go + 128 + 64 * (version === 1 ? 8 : 16), g.subgroup.length);
        for (si = 0; si < g.subgroup.length; ++si) {
            sg = g.subgroup[si];
            var simple = go + 128 + si * (version === 1 ? 8 : 16),
                so = subgroupInfoOffset + globalSubgroup * subgroupSize,
                po = poolOffset + globalSubgroup * 152,
                packet = packets[globalSubgroup],
                pv = rsbView(packet),
                ph = 8,
                textureBegin = globalTexture,
                textureCount = 0;
            rsbSet32(v, simple, globalSubgroup);
            rsbSet32(v, simple + 4, sg.category.resolution === undefined ? 0 : rsbNumber(sg.category.resolution, "resolution"));
            if (version !== 1) rsbSet32(v, simple + 8, rsbFourCC(sg.category.locale));
            rsbPutName(out, so, sg.identifier, "subgroup identifier");
            rsbSet32(v, so + 128, packetOffsets[globalSubgroup]);
            rsbSet32(v, so + 132, packet.length);
            rsbSet32(v, so + 136, globalSubgroup);
            rsbPutName(out, po, sg.identifier + "_AutoPool", "pool identifier");
            rsbSet32(v, po + 128, rsbU32(pv, ph + 16) + rsbU32(pv, ph + 24));
            rsbSet32(v, po + 132, rsbU32(pv, ph + 40));
            rsbSet32(v, po + 136, 1);
            rsbSet32(v, so + 140, rsbU32(pv, ph + 8));
            rsbSet32(v, so + 144, rsbU32(pv, ph + 12));
            rsbSet32(v, so + 148, rsbU32(pv, ph + 16));
            rsbSet32(v, so + 152, rsbU32(pv, ph + 20));
            rsbSet32(v, so + 156, rsbU32(pv, ph + 24));
            rsbSet32(v, so + 160, rsbU32(pv, ph + 24));
            rsbSet32(v, so + 164, rsbU32(pv, ph + 32));
            rsbSet32(v, so + 168, rsbU32(pv, ph + 36));
            rsbSet32(v, so + 172, rsbU32(pv, ph + 40));
            for (ri = 0; ri < sg.resource.length; ++ri)
                if (rsbType(sg.resource[ri].additional, "definition") === 1) {
                    var a = sg.resource[ri].additional.value,
                        to = textureOffset + globalTexture * textureSize;
                    rsbSet32(v, to, a.size[0]);
                    rsbSet32(v, to + 4, a.size[1]);
                    rsbSet32(v, to + 8, a.pitch);
                    rsbSet32(v, to + 12, a.format);
                    if (version === 4 && extended >= 1) rsbSet32(v, to + 16, a.additional_byte_count);
                    if (version === 4 && extended >= 2) {
                        if (extended === 3) {
                            rsbSet32(v, to + 16, a.scale);
                            rsbSet32(v, to + 20, a.additional_byte_count);
                        } else rsbSet32(v, to + 20, a.scale);
                    }++globalTexture;
                    ++textureCount;
                }
            if (version < 3) {
                rsbSet32(v, po + 144, textureCount);
                rsbSet32(v, po + 148, textureBegin);
            } else {
                rsbSet32(v, so + 196, textureCount);
                rsbSet32(v, so + 200, textureBegin);
            }
            out.set(packet, packetOffsets[globalSubgroup]);
            ++globalSubgroup;
        }
    }
    if (!kernelx.io.writeBytes(output, out)) rsbError("unable to write " + output);
}

function rsbParse(bytes, version, extended) {
    if (bytes.length < 8 + rsbHeaderSize(version)) rsbError("file is truncated");
    var v = rsbView(bytes),
        h = 8;
    if (rsbU32(v, 0) !== RSB_MAGIC || rsbU32(v, 4) !== version) rsbError("magic marker or version does not match");
    if ((version < 3 && rsbU32(v, h) !== 1) || (version >= 3 && rsbU32(v, h) !== 0)) rsbError("unexpected header value");
    var header = {
        informationSize: rsbU32(v, h + 4),
        resourceSize: rsbU32(v, h + 8),
        resourceOffset: rsbU32(v, h + 12),
        subgroupSize: rsbU32(v, h + 24),
        subgroupOffset: rsbU32(v, h + 28),
        subgroupCount: rsbU32(v, h + 32),
        subgroupInfoOffset: rsbU32(v, h + 36),
        subgroupInfoSize: rsbU32(v, h + 40),
        groupCount: rsbU32(v, h + 44),
        groupInfoOffset: rsbU32(v, h + 48),
        groupInfoSize: rsbU32(v, h + 52),
        groupSize: rsbU32(v, h + 56),
        groupOffset: rsbU32(v, h + 60),
        poolCount: rsbU32(v, h + 64),
        poolOffset: rsbU32(v, h + 68),
        poolSize: rsbU32(v, h + 72),
        textureCount: rsbU32(v, h + 76),
        textureOffset: rsbU32(v, h + 80),
        textureSize: rsbU32(v, h + 84),
        groupManifest: rsbU32(v, h + 88),
        resourceManifest: rsbU32(v, h + 92),
        stringManifest: rsbU32(v, h + 96)
    };
    if (header.informationSize < 8 + rsbHeaderSize(version) || header.informationSize > bytes.length) rsbError("invalid information section size");
    if (header.subgroupInfoSize !== rsbSubgroupSize(version) || header.groupInfoSize !== rsbGroupSize(version) || header.poolSize !== 152 || header.textureSize !== rsbTextureSize(version, extended)) rsbError("unexpected RSB record size");

    function area(p, n, name) {
        return rsbSlice(bytes, p, n, name);
    }
    area(header.resourceOffset, header.resourceSize, "resource map");
    area(header.subgroupOffset, header.subgroupSize, "subgroup map");
    area(header.groupOffset, header.groupSize, "group map");
    area(header.groupInfoOffset, header.groupCount * header.groupInfoSize, "group information");
    area(header.subgroupInfoOffset, header.subgroupCount * header.subgroupInfoSize, "subgroup information");
    area(header.poolOffset, header.poolCount * header.poolSize, "pool information");
    area(header.textureOffset, header.textureCount * header.textureSize, "texture information");
    return {
        bytes: bytes,
        view: v,
        header: header,
        manifest: rsbReadManifest(bytes, header, version)
    };
}

function rsbUnpack(input, folder, version, extended, layoutMode, outputResource, outputPacket) {
    if (!kernelx.io.isFile(input)) rsbError("InputFile must exist");
    var package = rsbParse(new Uint8Array(kernelx.io.mmapRead(input)), version, extended),
        h = package.header,
        v = package.view,
        definition = {
            group: []
        },
        resourceMap = rsbDecodeMap(rsbSlice(package.bytes, h.resourceOffset, h.resourceSize, "resource map"));
    if (!kernelx.io.mkdir(folder)) rsbError("unable to create output directory");
    for (var gi = 0; gi < h.groupCount; ++gi) {
        var go = h.groupInfoOffset + gi * h.groupInfoSize,
            original = rsbOriginalGroup(rsbName(package.bytes, go)),
            count = rsbU32(v, go + 128 + 64 * (version === 1 ? 8 : 16)),
            group = {
                identifier: original.identifier,
                composite: original.composite,
                subgroup: []
            };
        if (count > 64) rsbError("group has too many subgroups");
        for (var si = 0; si < count; ++si) {
            var simple = go + 128 + si * (version === 1 ? 8 : 16),
                index = rsbU32(v, simple),
                so = h.subgroupInfoOffset + index * h.subgroupInfoSize;
            if (index >= h.subgroupCount) rsbError("invalid subgroup index");
            var identifier = rsbName(package.bytes, so),
                category = {},
                resolution = rsbU32(v, simple + 4);
            if (resolution) category.resolution = resolution;
            if (version !== 1) {
                var locale = rsbFourCCText(rsbU32(v, simple + 8));
                if (locale) category.locale = locale;
            }
            var packetOffset = rsbU32(v, so + 128),
                packetSize = rsbU32(v, so + 132),
                packet = rsbSlice(package.bytes, packetOffset, packetSize, "subgroup packet " + identifier),
                decoded = ResourceStreamGroupCore.decode(packet, version),
                subgroup = {
                    identifier: identifier,
                    category: category,
                    compression: decoded.compression,
                    resource: []
                },
                textureBegin, textureCount;
            if (version === 1) {
                var pool = rsbU32(v, so + 136),
                    po = h.poolOffset + pool * h.poolSize;
                if (pool >= h.poolCount) rsbError("invalid pool index");
                textureBegin = rsbU32(v, po + 148);
                textureCount = rsbU32(v, po + 144);
            } else {
                textureBegin = rsbU32(v, so + 200);
                textureCount = rsbU32(v, so + 196);
            }
            for (var ri = 0; ri < decoded.resources.length; ++ri) {
                var r = decoded.resources[ri],
                    type = r.additional.type,
                    additional;
                if (type === 0) additional = {
                    type: 0,
                    value: {}
                };
                else {
                    var ti = textureBegin + r.additional.value.index;
                    if (r.additional.value.index >= textureCount || ti >= h.textureCount) rsbError("invalid texture index");
                    var to = h.textureOffset + ti * h.textureSize;
                    var value = {
                        size: r.additional.value.size,
                        format: rsbU32(v, to + 12),
                        pitch: rsbU32(v, to + 8)
                    };
                    if (version === 4 && extended >= 1) value.additional_byte_count = extended === 3 ? rsbU32(v, to + 20) : rsbU32(v, to + 16);
                    if (version === 4 && extended >= 2) value.scale = extended === 3 ? rsbU32(v, to + 16) : rsbU32(v, to + 20);
                    additional = {
                        type: 1,
                        value: value
                    };
                }
                subgroup.resource.push({
                    path: r.path,
                    additional: additional
                });
                if (outputResource) {
                    var path = String(r.path).replace(/\\/g, "/"),
                        target = kernelx.path.join(kernelx.path.join(folder, rsbLayout(layoutMode, group.identifier, identifier).resource), path),
                        source = type === 0 ? decoded.general : decoded.texture;
                    if (!kernelx.io.mkdir(kernelx.path.dir(target)) || !kernelx.io.writeBytes(target, rsbSlice(source, r.offset, r.size, "resource " + path))) rsbError("unable to write resource: " + path);
                }
            }
            if (outputPacket) {
                var packetPath = kernelx.path.join(folder, rsbLayout(layoutMode, group.identifier, identifier).packet);
                if (!kernelx.io.mkdir(kernelx.path.dir(packetPath)) || !kernelx.io.writeBytes(packetPath, packet)) rsbError("unable to write packet: " + packetPath);
            }
            group.subgroup.push(subgroup);
        }
        definition.group.push(group);
    }
    rsbWriteJson(kernelx.path.join(folder, "definition.json"), definition);
    if (package.manifest) rsbWriteJson(kernelx.path.join(folder, "manifest.json"), package.manifest);
}

function execute(params) {
    try {
        var version = Number(params.Version || 4),
            extended = Number(params.ExtendedTextureInformation || 0),
            layout = params.Layout || "resource";
        if (version !== 1 && version !== 3 && version !== 4) rsbError("Version must be 1, 3, or 4");
        if (extended < 0 || extended > 3 || version !== 4 && extended !== 0) rsbError("invalid ExtendedTextureInformation");
        if (layout !== "group" && layout !== "subgroup" && layout !== "resource") rsbError("invalid Layout");
        if (params.InputFolder !== undefined) {
            rsbPack(params.InputFolder, params.OutputFile, version, extended, layout, rsbBool(params.InputPacket), rsbBool(params.OutputNewPacket));
            return {
                success: true,
                output: params.OutputFile
            };
        }
        if (params.InputFile !== undefined) {
            rsbUnpack(params.InputFile, params.OutputFolder, version, extended, layout, params.OutputResource === undefined || rsbBool(params.OutputResource), rsbBool(params.OutputPacket));
            return {
                success: true,
                output: params.OutputFolder
            };
        }
        rsbError("unknown operation");
    } catch (e) {
        return {
            success: false,
            error: e && e.message ? e.message : String(e)
        };
    }
}