cmake_minimum_required(VERSION 4.3.0)
project(vcdiff LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

set(OPEN_VCDIFF_SOURCE_DIR "${CMAKE_CURRENT_SOURCE_DIR}/lib/open-vcdiff/src")
set(OPEN_VCDIFF_SOURCES
    "${OPEN_VCDIFF_SOURCE_DIR}/addrcache.cc"
    "${OPEN_VCDIFF_SOURCE_DIR}/zlib/adler32.c"
    "${OPEN_VCDIFF_SOURCE_DIR}/blockhash.cc"
    "${OPEN_VCDIFF_SOURCE_DIR}/codetable.cc"
    "${OPEN_VCDIFF_SOURCE_DIR}/decodetable.cc"
    "${OPEN_VCDIFF_SOURCE_DIR}/encodetable.cc"
    "${OPEN_VCDIFF_SOURCE_DIR}/headerparser.cc"
    "${OPEN_VCDIFF_SOURCE_DIR}/instruction_map.cc"
    "${OPEN_VCDIFF_SOURCE_DIR}/jsonwriter.cc"
    "${OPEN_VCDIFF_SOURCE_DIR}/logging.cc"
    "${OPEN_VCDIFF_SOURCE_DIR}/varint_bigendian.cc"
    "${OPEN_VCDIFF_SOURCE_DIR}/vcdecoder.cc"
    "${OPEN_VCDIFF_SOURCE_DIR}/vcencoder.cc"
    "${OPEN_VCDIFF_SOURCE_DIR}/vcdiffengine.cc"
)

configure_file("${OPEN_VCDIFF_SOURCE_DIR}/config.h.in" "${CMAKE_CURRENT_BINARY_DIR}/generated/config.h")

add_library(vcdiff SHARED native-lib.cpp ${OPEN_VCDIFF_SOURCES})

target_include_directories(vcdiff PRIVATE
    "${CMAKE_CURRENT_BINARY_DIR}/generated"
    "${OPEN_VCDIFF_SOURCE_DIR}"
    "${OPEN_VCDIFF_SOURCE_DIR}/zlib"
)

target_compile_definitions(vcdiff PRIVATE
    VCDIFF_USE_BLOCK_COMPARE_WORDS=1
)

target_compile_options(vcdiff PRIVATE
    -O3
    -DNDEBUG
    -fexceptions
    -ffunction-sections
    -fdata-sections
    -march=armv8-a+crypto+crc
)

target_link_options(vcdiff PRIVATE
    -Wl,--gc-sections
    -Wl,--icf=safe
)
