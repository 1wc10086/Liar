module;
#include <cstdint>
export module utility.rijndael.core;

export enum class RijndaelMode : int { ECB = 0, CBC = 1, CFB = 2 };
export enum class PaddingType : int { PKCS7 = 0, ZERO = 1 };
