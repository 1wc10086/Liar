cmake_minimum_required(VERSION 4.3.0)
project(iconv LANGUAGES C CXX)

set(CMAKE_C_STANDARD 23)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS OFF)
set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

set(ICONV_SOURCE_DIR "${CMAKE_CURRENT_SOURCE_DIR}/lib/libiconv/source")

add_library(iconv SHARED
    native-lib.cpp
    "${ICONV_SOURCE_DIR}/lib/iconv.c"
    "${ICONV_SOURCE_DIR}/lib/compat.c"
    "${ICONV_SOURCE_DIR}/libcharset/lib/localcharset.c"
)

target_include_directories(iconv PRIVATE
    "${CMAKE_CURRENT_SOURCE_DIR}/config"
    "${ICONV_SOURCE_DIR}"
    "${ICONV_SOURCE_DIR}/include"
    "${ICONV_SOURCE_DIR}/lib"
    "${ICONV_SOURCE_DIR}/libcharset/include"
)

target_compile_options(iconv PRIVATE
    -O3
    -DNDEBUG
    -ffunction-sections
    -fdata-sections
    -fvisibility=hidden
    -march=armv8-a+crypto+crc
)

target_compile_definitions(iconv PRIVATE
    INT_PTR=long
)

target_link_options(iconv PRIVATE
    -Wl,--gc-sections
)
