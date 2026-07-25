cmake_minimum_required(VERSION 3.22.1)
project(xz LANGUAGES C CXX)

set(CMAKE_C_STANDARD 17)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

set(LIBLZMA_ROOT "${CMAKE_CURRENT_SOURCE_DIR}/lib/liblzma")

file(GLOB_RECURSE LIBLZMA_SOURCES CONFIGURE_DEPENDS
    "${LIBLZMA_ROOT}/*.c"
)

list(FILTER LIBLZMA_SOURCES EXCLUDE REGEX ".*/(crc32_tablegen|crc64_tablegen|crc_clmul_consts_gen|fastpos_tablegen|price_tablegen|crc32_small|crc64_small|stream_encoder_mt|stream_decoder_mt)[.]c$")

add_library(liblzma STATIC
    ${LIBLZMA_SOURCES}
    "${CMAKE_CURRENT_SOURCE_DIR}/lib/common/tuklib_physmem.c"
    "${CMAKE_CURRENT_SOURCE_DIR}/lib/common/tuklib_cpucores.c"
)

target_include_directories(liblzma PRIVATE
    "${CMAKE_CURRENT_SOURCE_DIR}/lib/liblzma/api"
    "${CMAKE_CURRENT_SOURCE_DIR}/lib/liblzma/common"
    "${CMAKE_CURRENT_SOURCE_DIR}/lib/liblzma/check"
    "${CMAKE_CURRENT_SOURCE_DIR}/lib/liblzma/delta"
    "${CMAKE_CURRENT_SOURCE_DIR}/lib/liblzma/lz"
    "${CMAKE_CURRENT_SOURCE_DIR}/lib/liblzma/lzma"
    "${CMAKE_CURRENT_SOURCE_DIR}/lib/liblzma/rangecoder"
    "${CMAKE_CURRENT_SOURCE_DIR}/lib/liblzma/simple"
    "${CMAKE_CURRENT_SOURCE_DIR}/lib/common"
)

target_compile_definitions(liblzma PRIVATE
    HAVE_INTTYPES_H
    HAVE_STDINT_H
    HAVE_STDBOOL_H
    HAVE_VISIBILITY=1
    HAVE___BUILTIN_BSWAPXX
    HAVE___BUILTIN_ASSUME_ALIGNED
    TUKLIB_FAST_UNALIGNED_ACCESS
    HAVE_MF_HC3
    HAVE_MF_HC4
    HAVE_MF_BT2
    HAVE_MF_BT3
    HAVE_MF_BT4
    HAVE_ENCODER_LZMA1
    HAVE_DECODER_LZMA1
    HAVE_ENCODER_LZMA2
    HAVE_DECODER_LZMA2
    HAVE_ENCODER_DELTA
    HAVE_DECODER_DELTA
    HAVE_ENCODER_X86
    HAVE_DECODER_X86
    HAVE_ENCODER_POWERPC
    HAVE_DECODER_POWERPC
    HAVE_ENCODER_IA64
    HAVE_DECODER_IA64
    HAVE_ENCODER_ARM
    HAVE_DECODER_ARM
    HAVE_ENCODER_ARMTHUMB
    HAVE_DECODER_ARMTHUMB
    HAVE_ENCODER_ARM64
    HAVE_DECODER_ARM64
    HAVE_ENCODER_SPARC
    HAVE_DECODER_SPARC
    HAVE_ENCODER_RISCV
    HAVE_DECODER_RISCV
    HAVE_CHECK_CRC64
    HAVE_CHECK_SHA256
    HAVE_INTERNAL_SHA256
)

set_target_properties(liblzma PROPERTIES
    POSITION_INDEPENDENT_CODE ON
    C_VISIBILITY_PRESET hidden
)

add_library(xz SHARED native-lib.cpp)

target_include_directories(xz PRIVATE
    "${CMAKE_CURRENT_SOURCE_DIR}/lib/liblzma/api"
)

target_compile_options(xz PRIVATE
    -O3
    -DNDEBUG
    -ffunction-sections
    -fdata-sections
)

target_link_options(xz PRIVATE
    -Wl,--gc-sections
    -Wl,--exclude-libs,ALL
)

target_link_libraries(xz PRIVATE
    liblzma
    android
    log
)
