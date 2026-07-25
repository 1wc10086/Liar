/* kernelx-manifest
[
  {
    "id": "peggle_level_dat.decode",
    "implementation": "implementation",
    "params": [
      { "name": "InputFile", "type": "path", "required": true, "extensions": [".dat", ".DAT"] },
      { "name": "OutputFile", "type": "path", "required": true, "extensions": [".json"] }
    ]
  },
  {
    "id": "peggle_level_dat.encode",
    "implementation": "implementation",
    "params": [
      { "name": "InputFile", "type": "path", "required": true, "extensions": [".json"] },
      { "name": "OutputFile", "type": "path", "required": true, "extensions": [".dat", ".DAT"] }
    ]
  }
]
*/

var PEGGLE_TYPES = {
    2: "Rod",
    3: "Polygon",
    5: "Circle",
    6: "Brick",
    8: "Teleport",
    9: "Emitter"
};
var PEGGLE_MOVEMENTS = ["NoMovement", "VerticalCycle", "HorizontalCycle", "Circle", "HorizontalInfinity", "VerticalInfinity", "HorizontalArc", "VerticalArc", "Rotate", "RotateBackAndForth", "Unused", "VerticalWrap", "HorizontalWrap", "RotateAroundCircle", "RetraceCircle", "WeirdShape"];

function peggleHex(bytes) {
    var s = "";
    for (var i = 0; i < bytes.length; i++) s += (bytes[i] < 16 ? "0" : "") + bytes[i].toString(16);
    return s;
}

function peggleHexBytes(hex, field) {
    if (typeof hex !== "string" || hex.length % 2 || /[^0-9a-f]/i.test(hex)) throw new Error("Invalid " + field);
    var out = new Uint8Array(hex.length / 2);
    for (var i = 0; i < out.length; i++) out[i] = parseInt(hex.substr(i * 2, 2), 16);
    return out;
}

function peggleUtf8Decode(bytes) {
    return typeof TextDecoder !== "undefined" ? new TextDecoder("utf-8").decode(bytes) : String.fromCharCode.apply(null, bytes);
}

function peggleUtf8Encode(text) {
    return typeof TextEncoder !== "undefined" ? new TextEncoder().encode(text) : new Uint8Array(String(text).split("").map(function(c) {
        return c.charCodeAt(0);
    }));
}

function peggleReader(bytes) {
    var view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength),
        p = 0;

    function need(n, f) {
        if (n < 0 || n > bytes.length - p) throw new Error("Truncated Peggle DAT " + (f || "data") + " at 0x" + p.toString(16));
    }
    return {
        position: function() {
            return p;
        },
        remaining: function() {
            return bytes.length - p;
        },
        u8: function(f) {
            need(1, f);
            return bytes[p++];
        },
        i8: function(f) {
            var v = this.u8(f);
            return v > 127 ? v - 256 : v;
        },
        u16: function(f) {
            need(2, f);
            var v = view.getUint16(p, true);
            p += 2;
            return v;
        },
        i16: function(f) {
            need(2, f);
            var v = view.getInt16(p, true);
            p += 2;
            return v;
        },
        u24: function(f) {
            return this.u16(f) | (this.u8(f) << 16);
        },
        u32: function(f) {
            need(4, f);
            var v = view.getUint32(p, true);
            p += 4;
            return v;
        },
        i32: function(f) {
            need(4, f);
            var v = view.getInt32(p, true);
            p += 4;
            return v;
        },
        f32: function(f) {
            need(4, f);
            var v = view.getFloat32(p, true);
            p += 4;
            return v;
        },
        bytes: function(n, f) {
            need(n, f);
            var v = bytes.slice(p, p + n);
            p += n;
            return v;
        },
        string: function(f) {
            var n = this.i16((f || "string") + " length");
            if (n <= 0) return "";
            return peggleUtf8Decode(this.bytes(n, f));
        }
    };
}

function peggleWriter() {
    var chunks = [],
        size = 0;

    function put(a) {
        chunks.push(a);
        size += a.length;
    }

    function dv(n, method, value) {
        var a = new Uint8Array(n),
            v = new DataView(a.buffer);
        v[method](0, value, true);
        put(a);
    }
    return {
        u8: function(v) {
            put(new Uint8Array([v & 255]));
        },
        i8: function(v) {
            put(new Uint8Array([v & 255]));
        },
        u16: function(v) {
            dv(2, "setUint16", v);
        },
        i16: function(v) {
            dv(2, "setInt16", v);
        },
        u24: function(v) {
            this.u16(v);
            this.u8(v >>> 16);
        },
        u32: function(v) {
            dv(4, "setUint32", v);
        },
        i32: function(v) {
            dv(4, "setInt32", v);
        },
        f32: function(v) {
            dv(4, "setFloat32", v);
        },
        bytes: put,
        string: function(v) {
            var a = peggleUtf8Encode(v || "");
            if (a.length > 32767) throw new Error("Peggle string is too long");
            this.i16(a.length);
            put(a);
        },
        finish: function() {
            var out = new Uint8Array(size),
                p = 0;
            for (var i = 0; i < chunks.length; i++) {
                out.set(chunks[i], p);
                p += chunks[i].length;
            }
            return out;
        }
    };
}

function peggleBit(v, n) {
    return (v & (1 << n)) !== 0;
}

function peggleColor(r) {
    var v = r.u32("colour");
    return [(v >>> 24) & 255, (v >>> 16) & 255, (v >>> 8) & 255, v & 255];
}

function peggleWriteColor(w, c) {
    w.u32(((c[0] & 255) * 0x1000000) + ((c[1] & 255) << 16) + ((c[2] & 255) << 8) + (c[3] & 255));
}

function peggleReadPegInfo(r) {
    var type = r.u8("PegInfo type"),
        flags = r.u8("PegInfo flags"),
        d = {
            type: type,
            rawFlags: flags,
            canBeOrange: peggleBit(flags, 1),
            crumble: peggleBit(flags, 3)
        };
    if (peggleBit(flags, 2)) d.unknownInt32_1 = r.i32("PegInfo unknown 1");
    if (peggleBit(flags, 4)) d.unknownInt32_2 = r.i32("PegInfo unknown 2");
    if (peggleBit(flags, 5)) d.unknownByte_1 = r.u8("PegInfo unknown byte 1");
    if (peggleBit(flags, 7)) d.unknownByte_2 = r.u8("PegInfo unknown byte 2");
    return d;
}

function peggleWritePegInfo(w, d) {
    var f = d.rawFlags >>> 0;
    w.u8(d.type);
    w.u8(f);
    if (peggleBit(f, 2)) w.i32(d.unknownInt32_1);
    if (peggleBit(f, 4)) w.i32(d.unknownInt32_2);
    if (peggleBit(f, 5)) w.u8(d.unknownByte_1);
    if (peggleBit(f, 7)) w.u8(d.unknownByte_2);
}

function peggleReadMovementLink(r) {
    var linkId = r.i32("movement link");
    return {
        linkId: linkId,
        movement: linkId === 1 ? peggleReadMovement(r) : null
    };
}

function peggleWriteMovementLink(w, d) {
    w.i32(d.linkId);
    if (d.linkId === 1) peggleWriteMovement(w, d.movement);
}

function peggleReadMovement(r) {
    var shape = r.i8("movement shape"),
        anchorX = r.f32("movement anchor x"),
        anchorY = r.f32("movement anchor y"),
        timePeriod = r.i16("movement time"),
        flags = r.u16("movement flags"),
        d = {
            rawShape: shape,
            type: Math.abs(shape),
            typeName: PEGGLE_MOVEMENTS[Math.abs(shape)] || "Unknown",
            reverse: shape < 0,
            anchorX: anchorX,
            anchorY: anchorY,
            timePeriod: timePeriod,
            rawFlags: flags
        };
    if (peggleBit(flags, 0)) d.offset = r.i16("movement offset");
    if (peggleBit(flags, 1)) d.radius1 = r.i16("movement radius 1");
    if (peggleBit(flags, 2)) d.startPhase = r.f32("movement start phase");
    if (peggleBit(flags, 3)) d.moveRotationRadians = r.f32("movement rotation");
    if (peggleBit(flags, 4)) d.radius2 = r.i16("movement radius 2");
    if (peggleBit(flags, 5)) d.pause1 = r.i16("movement pause 1");
    if (peggleBit(flags, 6)) d.pause2 = r.i16("movement pause 2");
    if (peggleBit(flags, 7)) d.phase1 = r.u8("movement phase 1");
    if (peggleBit(flags, 8)) d.phase2 = r.u8("movement phase 2");
    if (peggleBit(flags, 9)) d.postDelayPhase = r.f32("movement post delay");
    if (peggleBit(flags, 10)) d.maxAngle = r.f32("movement max angle");
    if (peggleBit(flags, 11)) d.unknown8 = r.f32("movement unknown 8");
    if (peggleBit(flags, 14)) d.rotationRadians = r.f32("movement rotation 2");
    if (peggleBit(flags, 12)) {
        d.subOffsetX = r.f32("movement sub x");
        d.subOffsetY = r.f32("movement sub y");
        d.subMovement = peggleReadMovementLink(r);
    }
    if (peggleBit(flags, 13)) {
        d.objectX = r.f32("movement object x");
        d.objectY = r.f32("movement object y");
    }
    return d;
}

function peggleWriteMovement(w, d) {
    var f = d.rawFlags >>> 0;
    w.i8(d.rawShape);
    w.f32(d.anchorX);
    w.f32(d.anchorY);
    w.i16(d.timePeriod);
    w.u16(f);
    if (peggleBit(f, 0)) w.i16(d.offset);
    if (peggleBit(f, 1)) w.i16(d.radius1);
    if (peggleBit(f, 2)) w.f32(d.startPhase);
    if (peggleBit(f, 3)) w.f32(d.moveRotationRadians);
    if (peggleBit(f, 4)) w.i16(d.radius2);
    if (peggleBit(f, 5)) w.i16(d.pause1);
    if (peggleBit(f, 6)) w.i16(d.pause2);
    if (peggleBit(f, 7)) w.u8(d.phase1);
    if (peggleBit(f, 8)) w.u8(d.phase2);
    if (peggleBit(f, 9)) w.f32(d.postDelayPhase);
    if (peggleBit(f, 10)) w.f32(d.maxAngle);
    if (peggleBit(f, 11)) w.f32(d.unknown8);
    if (peggleBit(f, 14)) w.f32(d.rotationRadians);
    if (peggleBit(f, 12)) {
        w.f32(d.subOffsetX);
        w.f32(d.subOffsetY);
        peggleWriteMovementLink(w, d.subMovement);
    }
    if (peggleBit(f, 13)) {
        w.f32(d.objectX);
        w.f32(d.objectY);
    }
}

function peggleReadGeneric(r, version) {
    var f = version < 15 ? r.u24("generic flags") : r.u32("generic flags"),
        d = {
            rawFlags: f,
            flags: {}
        },
        x = d.flags;
    x.rollyPresent = peggleBit(f, 0);
    x.bouncyPresent = peggleBit(f, 1);
    x.pegInfoPresent = peggleBit(f, 2);
    x.movementPresent = peggleBit(f, 3);
    x.unknown4Present = peggleBit(f, 4);
    x.collision = peggleBit(f, 5);
    x.visible = peggleBit(f, 6);
    x.canMove = peggleBit(f, 7);
    x.solidColourPresent = peggleBit(f, 8);
    x.outlineColourPresent = peggleBit(f, 9);
    x.imageFilenamePresent = peggleBit(f, 10);
    x.imageDXPresent = peggleBit(f, 11);
    x.imageDYPresent = peggleBit(f, 12);
    x.imageRotationPresent = peggleBit(f, 13);
    x.background = peggleBit(f, 14);
    x.baseObject = peggleBit(f, 15);
    x.unknown16Present = peggleBit(f, 16);
    x.idPresent = peggleBit(f, 17);
    x.unknown18Present = peggleBit(f, 18);
    x.soundPresent = peggleBit(f, 19);
    x.ballStopReset = peggleBit(f, 20);
    x.logicPresent = peggleBit(f, 21);
    x.foreground = peggleBit(f, 22);
    x.maxBounceVelocityPresent = peggleBit(f, 23);
    x.drawSort = peggleBit(f, 24);
    x.foreground2 = peggleBit(f, 25);
    x.subIdPresent = peggleBit(f, 26);
    x.flipperFlagsPresent = peggleBit(f, 27);
    x.drawFloat = peggleBit(f, 28);
    x.shadow = version >= 80 ? peggleBit(f, 30) : true;
    if (x.rollyPresent) d.rolly = r.f32("rolly");
    if (x.bouncyPresent) d.bouncy = r.f32("bouncy");
    if (x.unknown4Present) d.unknownInt32_1 = r.i32("unknown 4");
    if (x.solidColourPresent) d.solidColour = peggleColor(r);
    if (x.outlineColourPresent) d.outlineColour = peggleColor(r);
    if (x.imageFilenamePresent) d.imageFilename = r.string("image filename");
    if (x.imageDXPresent) d.imageDX = r.f32("image dx");
    if (x.imageDYPresent) d.imageDY = r.f32("image dy");
    if (x.imageRotationPresent) d.imageRotationRadians = r.f32("image rotation");
    if (x.unknown16Present) d.unknownInt32_2 = r.i32("unknown 16");
    if (x.idPresent) d.id = r.string("id");
    if (x.unknown18Present) d.unknownInt32_3 = r.i32("unknown 18");
    if (x.soundPresent) d.sound = r.u8("sound");
    if (x.logicPresent) d.logic = r.string("logic");
    if (x.maxBounceVelocityPresent) d.maxBounceVelocity = r.f32("max bounce velocity");
    if (x.subIdPresent) d.subId = r.i32("sub id");
    if (x.flipperFlagsPresent) d.flipperFlags = r.u8("flipper flags");
    if (x.pegInfoPresent) d.pegInfo = peggleReadPegInfo(r);
    if (x.movementPresent) d.movementLink = peggleReadMovementLink(r);
    return d;
}

function peggleWriteGeneric(w, version, d) {
    var f = d.rawFlags >>> 0;
    if (version < 15) w.u24(f);
    else w.u32(f);
    if (peggleBit(f, 0)) w.f32(d.rolly);
    if (peggleBit(f, 1)) w.f32(d.bouncy);
    if (peggleBit(f, 4)) w.i32(d.unknownInt32_1);
    if (peggleBit(f, 8)) peggleWriteColor(w, d.solidColour);
    if (peggleBit(f, 9)) peggleWriteColor(w, d.outlineColour);
    if (peggleBit(f, 10)) w.string(d.imageFilename);
    if (peggleBit(f, 11)) w.f32(d.imageDX);
    if (peggleBit(f, 12)) w.f32(d.imageDY);
    if (peggleBit(f, 13)) w.f32(d.imageRotationRadians);
    if (peggleBit(f, 16)) w.i32(d.unknownInt32_2);
    if (peggleBit(f, 17)) w.string(d.id);
    if (peggleBit(f, 18)) w.i32(d.unknownInt32_3);
    if (peggleBit(f, 19)) w.u8(d.sound);
    if (peggleBit(f, 21)) w.string(d.logic);
    if (peggleBit(f, 23)) w.f32(d.maxBounceVelocity);
    if (peggleBit(f, 26)) w.i32(d.subId);
    if (peggleBit(f, 27)) w.u8(d.flipperFlags);
    if (peggleBit(f, 2)) peggleWritePegInfo(w, d.pegInfo);
    if (peggleBit(f, 3)) peggleWriteMovementLink(w, d.movementLink);
}

function peggleReadVariableFloat(r) {
    var kind = r.u8("variable float kind");
    if (kind > 0) return {
        kind: kind,
        isVariable: false,
        value: r.f32("variable float value")
    };
    return {
        kind: kind,
        isVariable: true,
        value: r.string("variable float name")
    };
}

function peggleWriteVariableFloat(w, d) {
    d = d || {};
    var kind = d.kind === undefined ? (d.isVariable ? 0 : 1) : d.kind;
    w.u8(kind);
    if (kind > 0) w.f32(d.value === undefined ? d.staticValue : d.value);
    else w.string(d.value === undefined ? d.variableValue : d.value);
}

function peggleReadSpecific(r, version, entry) {
    var fA, fB, fC, d;
    if (entry.type === 2) {
        fA = r.u8("rod flags");
        d = {
            rawFlagsA: fA,
            pointA: [r.f32("rod ax"), r.f32("rod ay")],
            pointB: [r.f32("rod bx"), r.f32("rod by")]
        };
        if (peggleBit(fA, 0)) d.extraFloat1 = r.f32("rod extra 1");
        if (peggleBit(fA, 1)) d.extraFloat2 = r.f32("rod extra 2");
        return d;
    }
    if (entry.type === 3) {
        fA = r.u8("polygon flags A");
        fB = version >= 35 ? r.u8("polygon flags B") : 0;
        d = {
            rawFlagsA: fA,
            rawFlagsB: fB
        };
        if (peggleBit(fA, 2)) d.rotation = r.f32("polygon rotation");
        if (peggleBit(fA, 3)) d.unknownFloat = r.f32("polygon unknown");
        if (peggleBit(fA, 5)) d.scale = r.f32("polygon scale");
        if (peggleBit(fA, 1)) d.normalDir = r.u8("polygon normal");
        if (peggleBit(fA, 4)) {
            entry.x = r.f32("polygon x");
            entry.y = r.f32("polygon y");
        }
        var n = r.i32("polygon points");
        if (n < 0 || n > 1000000) throw new Error("Invalid polygon point count");
        d.points = [];
        for (var i = 0; i < n; i++) d.points.push([r.f32("polygon x"), r.f32("polygon y")]);
        if (peggleBit(fB, 0)) d.unknownByteB = r.u8("polygon B byte");
        if (peggleBit(fB, 1)) d.growType = r.i32("polygon grow type");
        return d;
    }
    if (entry.type === 5) {
        fA = r.u8("circle flags A");
        fB = version >= 82 ? r.u8("circle flags B") : 0;
        d = {
            rawFlagsA: fA
        };
        if (version >= 82) d.rawFlagsB = fB;
        if (peggleBit(fA, 1)) {
            entry.x = r.f32("circle x");
            entry.y = r.f32("circle y");
        }
        d.radius = r.f32("circle radius");
        return d;
    }
    if (entry.type === 6) {
        fA = r.u8("brick flags A");
        fB = version >= 35 ? r.u8("brick flags B") : 0;
        d = {
            rawFlagsA: fA,
            rawFlagsB: fB
        };
        if (peggleBit(fA, 2)) d.floatA2 = r.f32("brick A2");
        if (peggleBit(fA, 3)) d.floatA3 = r.f32("brick A3");
        if (peggleBit(fA, 5)) d.floatA5 = r.f32("brick A5");
        if (peggleBit(fA, 1)) d.byteA1 = r.u8("brick A1");
        if (peggleBit(fA, 4)) {
            entry.x = r.f32("brick x");
            entry.y = r.f32("brick y");
        }
        if (peggleBit(fB, 0)) d.byteB0 = r.u8("brick B0");
        if (peggleBit(fB, 1)) d.intB1 = r.i32("brick B1");
        if (peggleBit(fB, 2)) d.intB2 = r.i16("brick B2");
        fC = r.u16("brick flags C");
        d.rawFlagsC = fC;
        if (peggleBit(fC, 8)) d.floatC8 = r.f32("brick C8");
        if (peggleBit(fC, 9)) d.floatC9 = r.f32("brick C9");
        if (peggleBit(fC, 2)) d.brickType = r.u8("brick type");
        if (peggleBit(fC, 3)) d.curvePoints = r.u8("brick curve points") + 2;
        if (peggleBit(fC, 5)) d.leftAngle = r.f32("brick left angle");
        if (peggleBit(fC, 6)) {
            d.rightAngle = r.f32("brick right angle");
            d.rightAngle2 = r.f32("brick right angle 2");
        }
        if (peggleBit(fC, 4)) d.sectorAngle = r.f32("brick sector angle");
        if (peggleBit(fC, 7)) d.width = r.f32("brick width");
        d.textureFlip = peggleBit(fC, 10);
        d.length = r.f32("brick length");
        d.angle = r.f32("brick angle");
        d.unknownFinal = r.u32("brick final");
        return d;
    }
    if (entry.type === 8) {
        fA = r.u8("teleport flags");
        d = {
            rawFlagsA: fA,
            width: r.i32("teleport width"),
            height: r.i32("teleport height")
        };
        if (peggleBit(fA, 1)) d.legacyInt16 = r.i16("teleport legacy");
        if (peggleBit(fA, 3)) d.legacyInt32_1 = r.i32("teleport legacy 1");
        if (peggleBit(fA, 5)) d.legacyInt32_2 = r.i32("teleport legacy 2");
        if (peggleBit(fA, 4)) d.nestedEntry = peggleReadEntry(r, version);
        if (peggleBit(fA, 2)) {
            entry.x = r.f32("teleport x");
            entry.y = r.f32("teleport y");
        }
        if (peggleBit(fA, 6)) {
            d.extraFloat1 = r.f32("teleport extra 1");
            d.extraFloat2 = r.f32("teleport extra 2");
        }
        return d;
    }
    if (entry.type === 9) {
        fA = r.u16("emitter flags");
        d = {
            rawFlagsA: fA,
            mainVar: r.i32("emitter main var"),
            changeColour: peggleBit(fA, 8),
            transparancy: peggleBit(fA, 2),
            randomStartPosition: peggleBit(fA, 4),
            changeUnknown: peggleBit(fA, 6),
            changeScale: peggleBit(fA, 7),
            changeOpacity: peggleBit(fA, 9),
            changeVelocity: peggleBit(fA, 10),
            changeDirection: peggleBit(fA, 11),
            changeRotation: peggleBit(fA, 12)
        };
        d.image = r.string("emitter image");
        d.width = r.i32("emitter width");
        d.height = r.i32("emitter height");
        if (d.mainVar === 2) {
            d.mainVar0 = r.i32("emitter main var 0");
            d.mainVar1 = r.f32("emitter main var 1");
            d.mainVar2 = r.string("emitter main var 2");
            d.mainVar3 = r.u8("emitter main var 3");
            if (peggleBit(fA, 13)) {
                d.unknownVF0 = peggleReadVariableFloat(r);
                d.unknownVF1 = peggleReadVariableFloat(r);
            }
        }
        if (peggleBit(fA, 5)) {
            entry.x = r.f32("emitter x");
            entry.y = r.f32("emitter y");
        }
        d.emitImage = r.string("emitter emit image");
        d.unknownEmitRate = r.f32("emitter unknown emit rate");
        d.unknown2 = r.f32("emitter unknown 2");
        d.rotation = r.f32("emitter rotation");
        d.maxQuantity = r.i32("emitter max quantity");
        d.timeBeforeFadeOut = r.f32("emitter time before fade out");
        d.fadeInTime = r.f32("emitter fade in time");
        d.lifeDuration = r.f32("emitter life duration");
        d.emitRate = peggleReadVariableFloat(r);
        d.emitAreaMultiplier = peggleReadVariableFloat(r);
        if (peggleBit(fA, 12)) {
            d.initialRotation = peggleReadVariableFloat(r);
            d.rotationVelocity = peggleReadVariableFloat(r);
            d.rotationUnknown = r.f32("emitter rotation unknown");
        }
        if (peggleBit(fA, 7)) {
            d.minScale = peggleReadVariableFloat(r);
            d.scaleVelocity = peggleReadVariableFloat(r);
            d.maxRandScale = r.f32("emitter max random scale");
        }
        if (peggleBit(fA, 8)) {
            d.colourRed = peggleReadVariableFloat(r);
            d.colourGreen = peggleReadVariableFloat(r);
            d.colourBlue = peggleReadVariableFloat(r);
        }
        if (peggleBit(fA, 9)) d.opacity = peggleReadVariableFloat(r);
        if (peggleBit(fA, 10)) {
            d.minVelocityX = peggleReadVariableFloat(r);
            d.minVelocityY = peggleReadVariableFloat(r);
            d.maxVelocityX = r.f32("emitter max velocity x");
            d.maxVelocityY = r.f32("emitter max velocity y");
            d.accelerationX = r.f32("emitter acceleration x");
            d.accelerationY = r.f32("emitter acceleration y");
        }
        if (peggleBit(fA, 11)) {
            d.directionSpeed = r.f32("emitter direction speed");
            d.directionRandomSpeed = r.f32("emitter direction random speed");
            d.directionAcceleration = r.f32("emitter direction acceleration");
            d.directionAngle = r.f32("emitter direction angle");
            d.directionRandomAngle = r.f32("emitter direction random angle");
        }
        if (peggleBit(fA, 6)) {
            d.unknownA = r.f32("emitter unknown a");
            d.unknownB = r.f32("emitter unknown b");
        }
        return d;
    }
    throw new Error("Unsupported Peggle entry type " + entry.type + " cannot be boundary-decoded");
}

function peggleWriteSpecific(w, version, e) {
    var d = e.specificData,
        f;
    if (e.type === 2) {
        f = d.rawFlagsA;
        w.u8(f);
        w.f32(d.pointA[0]);
        w.f32(d.pointA[1]);
        w.f32(d.pointB[0]);
        w.f32(d.pointB[1]);
        if (peggleBit(f, 0)) w.f32(d.extraFloat1);
        if (peggleBit(f, 1)) w.f32(d.extraFloat2);
        return;
    }
    if (e.type === 3) {
        f = d.rawFlagsA;
        w.u8(f);
        if (version >= 35) w.u8(d.rawFlagsB);
        if (peggleBit(f, 2)) w.f32(d.rotation);
        if (peggleBit(f, 3)) w.f32(d.unknownFloat);
        if (peggleBit(f, 5)) w.f32(d.scale);
        if (peggleBit(f, 1)) w.u8(d.normalDir);
        if (peggleBit(f, 4)) {
            w.f32(e.x);
            w.f32(e.y);
        }
        w.i32(d.points.length);
        for (var i = 0; i < d.points.length; i++) {
            w.f32(d.points[i][0]);
            w.f32(d.points[i][1]);
        }
        if (peggleBit(d.rawFlagsB, 0)) w.u8(d.unknownByteB);
        if (peggleBit(d.rawFlagsB, 1)) w.i32(d.growType);
        return;
    }
    if (e.type === 5) {
        f = d.rawFlagsA;
        w.u8(f);
        if (version >= 82) w.u8(d.rawFlagsB);
        if (peggleBit(f, 1)) {
            w.f32(e.x);
            w.f32(e.y);
        }
        w.f32(d.radius);
        return;
    }
    if (e.type === 6) {
        f = d.rawFlagsA;
        w.u8(f);
        if (version >= 35) w.u8(d.rawFlagsB);
        if (peggleBit(f, 2)) w.f32(d.floatA2);
        if (peggleBit(f, 3)) w.f32(d.floatA3);
        if (peggleBit(f, 5)) w.f32(d.floatA5);
        if (peggleBit(f, 1)) w.u8(d.byteA1);
        if (peggleBit(f, 4)) {
            w.f32(e.x);
            w.f32(e.y);
        }
        if (peggleBit(d.rawFlagsB, 0)) w.u8(d.byteB0);
        if (peggleBit(d.rawFlagsB, 1)) w.i32(d.intB1);
        if (peggleBit(d.rawFlagsB, 2)) w.i16(d.intB2);
        f = d.rawFlagsC;
        w.u16(f);
        if (peggleBit(f, 8)) w.f32(d.floatC8);
        if (peggleBit(f, 9)) w.f32(d.floatC9);
        if (peggleBit(f, 2)) w.u8(d.brickType);
        if (peggleBit(f, 3)) w.u8(d.curvePoints - 2);
        if (peggleBit(f, 5)) w.f32(d.leftAngle);
        if (peggleBit(f, 6)) {
            w.f32(d.rightAngle);
            w.f32(d.rightAngle2);
        }
        if (peggleBit(f, 4)) w.f32(d.sectorAngle);
        if (peggleBit(f, 7)) w.f32(d.width);
        w.f32(d.length);
        w.f32(d.angle);
        w.u32(d.unknownFinal);
        return;
    }
    if (e.type === 8) {
        f = d.rawFlagsA;
        w.u8(f);
        w.i32(d.width);
        w.i32(d.height);
        if (peggleBit(f, 1)) w.i16(d.legacyInt16);
        if (peggleBit(f, 3)) w.i32(d.legacyInt32_1);
        if (peggleBit(f, 5)) w.i32(d.legacyInt32_2);
        if (peggleBit(f, 4)) peggleWriteEntry(w, version, d.nestedEntry);
        if (peggleBit(f, 2)) {
            w.f32(e.x);
            w.f32(e.y);
        }
        if (peggleBit(f, 6)) {
            w.f32(d.extraFloat1);
            w.f32(d.extraFloat2);
        }
        return;
    }
    throw new Error("Unsupported Peggle entry type " + e.type);
}

function peggleReadEmitter(r, entry) {
    var mainVar = r.i32("emitter main var"),
        f = r.u16("emitter flags"),
        d = {
            rawFlagsA: f,
            mainVar: mainVar
        };
    d.image = r.string("emitter image");
    d.width = r.i32("emitter width");
    d.height = r.i32("emitter height");
    if (mainVar === 2) {
        d.mainVar0 = r.i32("emitter main var 0");
        d.mainVar1 = r.f32("emitter main var 1");
        d.mainVar2 = r.string("emitter main var 2");
        d.mainVar3 = r.u8("emitter main var 3");
        if (peggleBit(f, 13)) {
            d.unknownVF0 = peggleReadVariableFloat(r);
            d.unknownVF1 = peggleReadVariableFloat(r);
        }
    }
    if (peggleBit(f, 5)) {
        entry.x = r.f32("emitter x");
        entry.y = r.f32("emitter y");
    }
    d.emitImage = r.string("emitter emit image");
    d.unknownEmitRate = r.f32("emitter unknown emit rate");
    d.unknown2 = r.f32("emitter unknown 2");
    d.rotation = r.f32("emitter rotation");
    d.maxQuantity = r.i32("emitter max quantity");
    d.timeBeforeFadeOut = r.f32("emitter time before fade out");
    d.fadeInTime = r.f32("emitter fade in time");
    d.lifeDuration = r.f32("emitter life duration");
    d.emitRate = peggleReadVariableFloat(r);
    d.emitAreaMultiplier = peggleReadVariableFloat(r);
    if (peggleBit(f, 12)) {
        d.initialRotation = peggleReadVariableFloat(r);
        d.rotationVelocity = peggleReadVariableFloat(r);
        d.rotationUnknown = r.f32("emitter rotation unknown");
    }
    if (peggleBit(f, 7)) {
        d.minScale = peggleReadVariableFloat(r);
        d.scaleVelocity = peggleReadVariableFloat(r);
        d.maxRandScale = r.f32("emitter max random scale");
    }
    if (peggleBit(f, 8)) {
        d.colourRed = peggleReadVariableFloat(r);
        d.colourGreen = peggleReadVariableFloat(r);
        d.colourBlue = peggleReadVariableFloat(r);
    }
    if (peggleBit(f, 9)) d.opacity = peggleReadVariableFloat(r);
    if (peggleBit(f, 10)) {
        d.minVelocityX = peggleReadVariableFloat(r);
        d.minVelocityY = peggleReadVariableFloat(r);
        d.maxVelocityX = r.f32("emitter max velocity x");
        d.maxVelocityY = r.f32("emitter max velocity y");
        d.accelerationX = r.f32("emitter acceleration x");
        d.accelerationY = r.f32("emitter acceleration y");
    }
    if (peggleBit(f, 11)) {
        d.directionSpeed = r.f32("emitter direction speed");
        d.directionRandomSpeed = r.f32("emitter direction random speed");
        d.directionAcceleration = r.f32("emitter direction acceleration");
        d.directionAngle = r.f32("emitter direction angle");
        d.directionRandomAngle = r.f32("emitter direction random angle");
    }
    if (peggleBit(f, 6)) {
        d.unknownA = r.f32("emitter unknown a");
        d.unknownB = r.f32("emitter unknown b");
    }
    return d;
}

function peggleWriteEmitter(w, e) {
    var d = e.specificData || {},
        f = d.rawFlagsA >>> 0;
    w.i32(d.mainVar);
    w.u16(f);
    w.string(d.image);
    w.i32(d.width);
    w.i32(d.height);
    if (d.mainVar === 2) {
        w.i32(d.mainVar0);
        w.f32(d.mainVar1);
        w.string(d.mainVar2);
        w.u8(d.mainVar3);
        if (peggleBit(f, 13)) {
            peggleWriteVariableFloat(w, d.unknownVF0);
            peggleWriteVariableFloat(w, d.unknownVF1);
        }
    }
    if (peggleBit(f, 5)) {
        w.f32(e.x);
        w.f32(e.y);
    }
    w.string(d.emitImage);
    w.f32(d.unknownEmitRate);
    w.f32(d.unknown2);
    w.f32(d.rotation);
    w.i32(d.maxQuantity);
    w.f32(d.timeBeforeFadeOut);
    w.f32(d.fadeInTime);
    w.f32(d.lifeDuration);
    peggleWriteVariableFloat(w, d.emitRate);
    peggleWriteVariableFloat(w, d.emitAreaMultiplier);
    if (peggleBit(f, 12)) {
        peggleWriteVariableFloat(w, d.initialRotation);
        peggleWriteVariableFloat(w, d.rotationVelocity);
        w.f32(d.rotationUnknown);
    }
    if (peggleBit(f, 7)) {
        peggleWriteVariableFloat(w, d.minScale);
        peggleWriteVariableFloat(w, d.scaleVelocity);
        w.f32(d.maxRandScale);
    }
    if (peggleBit(f, 8)) {
        peggleWriteVariableFloat(w, d.colourRed);
        peggleWriteVariableFloat(w, d.colourGreen);
        peggleWriteVariableFloat(w, d.colourBlue);
    }
    if (peggleBit(f, 9)) peggleWriteVariableFloat(w, d.opacity);
    if (peggleBit(f, 10)) {
        peggleWriteVariableFloat(w, d.minVelocityX);
        peggleWriteVariableFloat(w, d.minVelocityY);
        w.f32(d.maxVelocityX);
        w.f32(d.maxVelocityY);
        w.f32(d.accelerationX);
        w.f32(d.accelerationY);
    }
    if (peggleBit(f, 11)) {
        w.f32(d.directionSpeed);
        w.f32(d.directionRandomSpeed);
        w.f32(d.directionAcceleration);
        w.f32(d.directionAngle);
        w.f32(d.directionRandomAngle);
    }
    if (peggleBit(f, 6)) {
        w.f32(d.unknownA);
        w.f32(d.unknownB);
    }
}

function peggleReadEntry(r, version) {
    var offset = r.position(),
        magic = r.i32("entry magic");
    if (magic !== 1) return {
        kind: magic === 0 ? "null" : "reference",
        offset: offset,
        referenceMarker: magic
    };
    var e = {
        kind: "inline",
        offset: offset,
        magic: magic,
        type: r.i32("entry type"),
        x: 0,
        y: 0
    };
    e.typeName = PEGGLE_TYPES[e.type] || ("Unknown(" + e.type + ")");
    e.generic = peggleReadGeneric(r, version);
    e.specificData = e.type === 9 ? peggleReadEmitter(r, e) : peggleReadSpecific(r, version, e);
    return e;
}

function peggleWriteEntry(w, version, e) {
    if (e.kind !== "inline") {
        w.i32(e.referenceMarker);
        return;
    }
    w.i32(e.magic === undefined ? 1 : e.magic);
    w.i32(e.type);
    peggleWriteGeneric(w, version, e.generic);
    if (e.type === 9) peggleWriteEmitter(w, e);
    else peggleWriteSpecific(w, version, e);
}

function peggleDecode(bytes) {
    if (!(bytes instanceof Uint8Array) || bytes.length < 9) throw new Error("Invalid or truncated Peggle DAT file");
    var r = peggleReader(bytes),
        d = {
            format: "Peggle level DAT (LevelEntry/DataSync)",
            endianness: "little",
            version: r.i32("version"),
            unknownByte: r.u8("unknown byte"),
            entries: []
        },
        n = r.i32("entry count");
    if (n < 0 || n > 1000000) throw new Error("Invalid Peggle entry count " + n);
    d.entryCount = n;
    for (var i = 0; i < n; i++) d.entries.push(peggleReadEntry(r, d.version));
    if (r.remaining()) d.trailingDataHex = peggleHex(r.bytes(r.remaining(), "trailing data"));
    return d;
}

function peggleEncode(d) {
    if (!d || typeof d !== "object") throw new Error("Peggle JSON object is required");
    var w = peggleWriter(),
        entries = d.entries || [],
        version = d.version;
    w.i32(version);
    w.u8(d.unknownByte);
    w.i32(entries.length);
    for (var i = 0; i < entries.length; i++) peggleWriteEntry(w, version, entries[i]);
    if (d.trailingDataHex) w.bytes(peggleHexBytes(d.trailingDataHex, "trailingDataHex"));
    return w.finish();
}

function peggleDecodeFile(input, output) {
    if (!input || !kernelx.io.isFile(input)) return {
        success: false,
        error: "InputFile is required and must exist"
    };
    if (!output) return {
        success: false,
        error: "OutputFile is required"
    };
    try {
        var d = peggleDecode(new Uint8Array(kernelx.io.mmapRead(input)));
        if (!kernelx.io.writeText(output, JSON.stringify(d, null, 2))) return {
            success: false,
            error: "Unable to write " + output
        };
        return {
            success: true,
            output: output,
            entries: d.entries.length
        };
    } catch (e) {
        return {
            success: false,
            error: e && e.message ? e.message : String(e)
        };
    }
}

function peggleEncodeFile(input, output) {
    if (!input || !kernelx.io.isFile(input)) return {
        success: false,
        error: "InputFile is required and must exist"
    };
    if (!output) return {
        success: false,
        error: "OutputFile is required"
    };
    try {
        var b = peggleEncode(JSON.parse(kernelx.io.readText(input)));
        if (!kernelx.io.writeBytes(output, b)) return {
            success: false,
            error: "Unable to write " + output
        };
        return {
            success: true,
            output: output,
            bytes: b.length
        };
    } catch (e) {
        return {
            success: false,
            error: e && e.message ? e.message : String(e)
        };
    }
}

function execute(params, id) {
    return id === "peggle_level_dat.encode" || (!id && typeof params.InputFile === "string" && /\.json$/i.test(params.InputFile)) ? peggleEncodeFile(params.InputFile, params.OutputFile) : peggleDecodeFile(params.InputFile, params.OutputFile);
}