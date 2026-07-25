cmake_minimum_required(VERSION 4.3.0)
project(audio LANGUAGES C CXX)

set(CMAKE_C_STANDARD 23)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS OFF)
set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)
set(BUILD_SHARED_LIBS OFF CACHE BOOL "" FORCE)
set(OPUS_BUILD_PROGRAMS OFF CACHE BOOL "" FORCE)
set(OPUS_BUILD_TESTING OFF CACHE BOOL "" FORCE)
set(OPUS_DISABLE_DOCS ON CACHE BOOL "" FORCE)
set(OPUS_DISABLE_INTRINSICS OFF CACHE BOOL "" FORCE)
set(XM_VERBOSE OFF CACHE BOOL "" FORCE)
set(XM_SAMPLE_TYPE float CACHE STRING "" FORCE)
set(BUILD_CXXLIBS OFF CACHE BOOL "" FORCE)
set(BUILD_PROGRAMS OFF CACHE BOOL "" FORCE)
set(BUILD_EXAMPLES OFF CACHE BOOL "" FORCE)
set(BUILD_TESTING OFF CACHE BOOL "" FORCE)
set(BUILD_DOCS OFF CACHE BOOL "" FORCE)
set(WITH_OGG OFF CACHE BOOL "" FORCE)
set(INSTALL_MANPAGES OFF CACHE BOOL "" FORCE)
set(INSTALL_PKGCONFIG_MODULES OFF CACHE BOOL "" FORCE)
set(INSTALL_CMAKE_CONFIG_MODULE OFF CACHE BOOL "" FORCE)
set(FAAD_BUILD_CLI OFF CACHE BOOL "" FORCE)

add_subdirectory(lib/opus ${CMAKE_CURRENT_BINARY_DIR}/opus)
add_subdirectory(lib/libxm/src ${CMAKE_CURRENT_BINARY_DIR}/libxm)
add_subdirectory(lib/flac ${CMAKE_CURRENT_BINARY_DIR}/flac)
add_subdirectory(lib/faad2 ${CMAKE_CURRENT_BINARY_DIR}/faad2)
set_target_properties(opus xm FLAC PROPERTIES POSITION_INDEPENDENT_CODE ON)

add_library(audio SHARED native-lib.cpp lib/stb/stb_vorbis.c)
file(GLOB WMA_SOURCES CONFIGURE_DEPENDS "${CMAKE_CURRENT_SOURCE_DIR}/lib/WmaDecoder/WmaDecoder/Wma_*.cpp")
list(FILTER WMA_SOURCES EXCLUDE REGEX ".*/Wma_file[.]cpp$")
list(FILTER WMA_SOURCES EXCLUDE REGEX ".*/Wma_avio[.]cpp$")
target_sources(audio PRIVATE ${WMA_SOURCES})
target_sources(audio PRIVATE
    lib/alac/codec/EndianPortable.c
    lib/alac/codec/ALACBitUtilities.c
    lib/alac/codec/ALACDecoder.cpp
    lib/alac/codec/ALACEncoder.cpp
    lib/alac/codec/ag_dec.c
    lib/alac/codec/ag_enc.c
    lib/alac/codec/dp_dec.c
    lib/alac/codec/dp_enc.c
    lib/alac/codec/matrix_dec.c
    lib/alac/codec/matrix_enc.c
)
set_target_properties(audio PROPERTIES CXX_VISIBILITY_PRESET hidden VISIBILITY_INLINES_HIDDEN YES)

target_include_directories(audio PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/lib/dr_lib
    ${CMAKE_CURRENT_SOURCE_DIR}/lib/minimp3
    ${CMAKE_CURRENT_SOURCE_DIR}/lib/opus/include
    ${CMAKE_CURRENT_SOURCE_DIR}/lib/flac/include
    ${CMAKE_CURRENT_SOURCE_DIR}/lib/WmaDecoder/WmaDecoder
    ${CMAKE_CURRENT_SOURCE_DIR}/lib/alac/codec
    ${CMAKE_CURRENT_SOURCE_DIR}/lib/faad2/include
    ${CMAKE_CURRENT_BINARY_DIR}/libxm
)

target_compile_options(audio PRIVATE
    -O3
    -DNDEBUG
    -ffunction-sections
    -fdata-sections
    -march=armv8-a+crypto+crc
)

set_source_files_properties(
    lib/alac/codec/ALACDecoder.cpp
    lib/alac/codec/ALACEncoder.cpp
    PROPERTIES COMPILE_OPTIONS "-Wno-multichar"
)

target_link_options(audio PRIVATE -Wl,--gc-sections -Wl,--icf=safe)
target_link_libraries(audio PRIVATE opus xm FLAC faad log m)
