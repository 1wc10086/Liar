cmake_minimum_required(VERSION 4.3.0)
project(kernel LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

find_library(log-lib log)
find_library(android-lib android)

set(BUILD_TESTING OFF CACHE BOOL "" FORCE)
set(WITH_GTEST OFF CACHE BOOL "" FORCE)
set(WITH_BENCHMARKS OFF CACHE BOOL "" FORCE)
set(WITH_FUZZERS OFF CACHE BOOL "" FORCE)
set(BUILD_SHARED_LIBS OFF CACHE BOOL "" FORCE)
set(ZLIB_COMPAT ON CACHE BOOL "" FORCE)

add_subdirectory(${CMAKE_CURRENT_SOURCE_DIR}/lib/zlib-ng ${CMAKE_CURRENT_BINARY_DIR}/zlib-ng-build)

file(GLOB_RECURSE KERNEL_LIB_CPP CONFIGURE_DEPENDS
    "${CMAKE_CURRENT_SOURCE_DIR}/lib/*.cpp"
    "${CMAKE_CURRENT_SOURCE_DIR}/lib/*.cc"
)

file(GLOB_RECURSE KERNEL_LIB_C CONFIGURE_DEPENDS
    "${CMAKE_CURRENT_SOURCE_DIR}/lib/*.c"
)

foreach(p IN LISTS KERNEL_LIB_CPP KERNEL_LIB_C)
    if(p MATCHES "/lib/zlib-ng/")
        list(REMOVE_ITEM KERNEL_LIB_CPP "${p}")
        list(REMOVE_ITEM KERNEL_LIB_C "${p}")
    endif()
endforeach()

list(REMOVE_DUPLICATES KERNEL_LIB_CPP)
list(REMOVE_DUPLICATES KERNEL_LIB_C)
list(SORT KERNEL_LIB_CPP)
list(SORT KERNEL_LIB_C)

file(GLOB_RECURSE XMEM_LZX_ENCODER_SOURCES CONFIGURE_DEPENDS
    "${CMAKE_CURRENT_SOURCE_DIR}/lib/XMemCompression/compression/codec/lzx/encoder/*.cpp"
)

set_source_files_properties(${XMEM_LZX_ENCODER_SOURCES} PROPERTIES
    COMPILE_OPTIONS "-O0;-fno-inline;-fno-strict-aliasing"
)

add_library(kernel_libs STATIC
    ${KERNEL_LIB_CPP}
    ${KERNEL_LIB_C}
)

set_target_properties(kernel_libs PROPERTIES
    POSITION_INDEPENDENT_CODE ON
    CXX_SCAN_FOR_MODULES OFF
)

target_include_directories(kernel_libs PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/lib
    ${CMAKE_CURRENT_SOURCE_DIR}/third
    ${CMAKE_CURRENT_SOURCE_DIR}/utility
    ${CMAKE_CURRENT_SOURCE_DIR}/utility/io
)

target_compile_options(kernel_libs PRIVATE
    -O3
    -DNDEBUG
    -frtti
    -fexceptions
    -ffunction-sections
    -fdata-sections
    -Wno-c++11-narrowing
    -Wno-deprecated-declarations
    -march=armv8-a+crypto+crc
)

set(CMAKE_CXX_SCAN_FOR_MODULES ON)

file(GLOB_RECURSE KERNEL_MODULES CONFIGURE_DEPENDS
    "${CMAKE_CURRENT_SOURCE_DIR}/utility/*.cppm"
    "${CMAKE_CURRENT_SOURCE_DIR}/tool/shell/*.cppm"
    "${CMAKE_CURRENT_SOURCE_DIR}/tool/popcap/*.cppm"
)

file(GLOB_RECURSE KERNEL_SOURCES CONFIGURE_DEPENDS
    "${CMAKE_CURRENT_SOURCE_DIR}/utility/*.cpp"
    "${CMAKE_CURRENT_SOURCE_DIR}/tool/shell/*.cpp"
    "${CMAKE_CURRENT_SOURCE_DIR}/tool/popcap/*.cpp"
)

list(REMOVE_DUPLICATES KERNEL_MODULES)
list(REMOVE_DUPLICATES KERNEL_SOURCES)
list(SORT KERNEL_MODULES)
list(SORT KERNEL_SOURCES)

add_library(kernel SHARED
    ${KERNEL_SOURCES}
)

target_sources(kernel
    PUBLIC
        FILE_SET cxx_modules
        TYPE CXX_MODULES
        FILES ${KERNEL_MODULES}
)

target_include_directories(kernel PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/lib
    ${CMAKE_CURRENT_SOURCE_DIR}/tool
    ${CMAKE_CURRENT_SOURCE_DIR}/third
    ${CMAKE_CURRENT_SOURCE_DIR}/utility
    ${CMAKE_CURRENT_SOURCE_DIR}/utility/io
)

target_compile_options(kernel PRIVATE
    -O3
    -DNDEBUG
    -frtti
    -fexceptions
    -ffunction-sections
    -fdata-sections
    -Wno-c++11-narrowing
    -Wno-deprecated-declarations
    -march=armv8-a+crypto+crc
)

target_link_options(kernel PRIVATE
    -Wl,--gc-sections
    -Wl,--icf=safe
)

target_link_libraries(kernel PRIVATE
    kernel_libs
    ${log-lib}
    ${android-lib}
    zlib-ng::zlibstatic
)