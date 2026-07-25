#pragma once

#define BUILD ""
#define HIDDEN __attribute__((visibility("hidden")))
#define INLINE inline
#define THREAD_LOCAL _Thread_local
#define FALLTHROUGH __attribute__((fallthrough));
#define PACKAGE_NAME "texture"
#define VERSION "3.1.0"
#define SIZEOF_SIZE_T 8
#ifndef BITS_IN_JSAMPLE
#define BITS_IN_JSAMPLE 8
#endif
#define SIMD_ARCHITECTURE 0
