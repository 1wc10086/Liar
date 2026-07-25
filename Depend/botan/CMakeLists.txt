cmake_minimum_required(VERSION 4.3.0)
project(botan LANGUAGES C CXX)

set(CMAKE_C_STANDARD 17)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS OFF)
set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

file(GLOB MBEDTLS_SOURCES CONFIGURE_DEPENDS "${CMAKE_CURRENT_SOURCE_DIR}/lib/mbedtls/library/*.c")
list(FILTER MBEDTLS_SOURCES EXCLUDE REGEX ".*/ecp_curves_new[.]c$")

set(BLAKE3_SOURCES
    lib/BLAKE3/c/blake3.c
    lib/BLAKE3/c/blake3_dispatch.c
    lib/BLAKE3/c/blake3_portable.c
    lib/BLAKE3/c/blake3_neon.c
)

add_library(botan SHARED
    native-lib.cpp
    ${MBEDTLS_SOURCES}
    ${BLAKE3_SOURCES}
)

target_include_directories(botan PRIVATE
    lib/mbedtls/include
    lib/mbedtls/library
    lib/BLAKE3/c
)

target_compile_options(botan PRIVATE
    -O3
    -DNDEBUG
    -ffunction-sections
    -fdata-sections
    -march=armv8-a+crypto+crc
    $<$<COMPILE_LANGUAGE:CXX>:-fexceptions;-frtti>
)

target_link_options(botan PRIVATE -Wl,--gc-sections)
target_link_libraries(botan PRIVATE log android)
