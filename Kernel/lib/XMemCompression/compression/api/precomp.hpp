#pragma once

#include <cstdlib>
#include <cstring>

#if !defined(_MSC_VER)
// Liar : change - compatibility aliases for the original MSVC-oriented sources.
#define __int8 char
#define __int16 short
#define __int32 int
#define __int64 long long
#define __fastcall
#endif

#include "../inc/xcompress.h"
#include "../inc/xmcdcodec_lzx.hpp"
#include "../inc/xmcdutil.hpp"
