cmake_minimum_required(VERSION 3.22.1)

project(zstd_native LANGUAGES C CXX)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

find_library(log-lib log)
find_library(android-lib android)

set(ZSTD_ROOT "${CMAKE_CURRENT_SOURCE_DIR}/lib/zstd/lib")

file(GLOB ZSTD_TOP_SOURCES CONFIGURE_DEPENDS
    "${ZSTD_ROOT}/*.c"
)

file(GLOB_RECURSE ZSTD_SOURCES CONFIGURE_DEPENDS
    "${ZSTD_ROOT}/common/*.c"
    "${ZSTD_ROOT}/compress/*.c"
    "${ZSTD_ROOT}/decompress/*.c"
    "${ZSTD_ROOT}/dictBuilder/*.c"
    "${ZSTD_ROOT}/legacy/*.c"
    "${ZSTD_ROOT}/deprecated/*.c"
)

list(APPEND ZSTD_SOURCES ${ZSTD_TOP_SOURCES})
list(REMOVE_DUPLICATES ZSTD_SOURCES)

add_library(zstd SHARED
    native-lib.cpp
    ${ZSTD_SOURCES}
)

target_include_directories(zstd PRIVATE
    "${CMAKE_CURRENT_SOURCE_DIR}"
    "${ZSTD_ROOT}"
)

target_compile_definitions(zstd PRIVATE
    ZSTD_LEGACY_SUPPORT=1
)

target_compile_options(zstd PRIVATE
    -O3
    -DNDEBUG
    -ffunction-sections
    -fdata-sections
    -fvisibility=hidden
    -Wno-unused-parameter
    -Wno-unused-function
    -Wno-deprecated-declarations
)

target_link_options(zstd PRIVATE
    -Wl,--gc-sections
)

target_link_libraries(zstd
    ${android-lib}
    ${log-lib}
)