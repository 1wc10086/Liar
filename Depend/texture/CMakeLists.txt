cmake_minimum_required(VERSION 4.3.0)
project(texture LANGUAGES C CXX)

set(CMAKE_C_STANDARD 23)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

set(JPEG_LIB_VERSION 80)
set(VERSION 3.1.0)
set(LIBJPEG_TURBO_VERSION_NUMBER 3010000)
configure_file(lib/libjpeg-turbo/src/jconfig.h.in ${CMAKE_CURRENT_BINARY_DIR}/generated/jconfig.h @ONLY)
configure_file(jconfigint.h ${CMAKE_CURRENT_BINARY_DIR}/generated/jconfigint.h COPYONLY)
configure_file(jversion.h ${CMAKE_CURRENT_BINARY_DIR}/generated/jversion.h COPYONLY)
file(MAKE_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR}/generated/src/webp)
configure_file(webp_config.h ${CMAKE_CURRENT_BINARY_DIR}/generated/src/webp/config.h COPYONLY)

file(GLOB_RECURSE WEBP_SOURCES CONFIGURE_DEPENDS lib/libwebp/src/*.c)
file(GLOB_RECURSE SHARPYUV_SOURCES CONFIGURE_DEPENDS lib/libwebp/sharpyuv/*.c)
list(FILTER WEBP_SOURCES EXCLUDE REGEX ".*/(examples|tests|dwebp|cwebp|webpmux|vwebp|gif2webp|img2webp|webpinfo|webp_quality|webp_to_sdl)[.]c$")
list(FILTER WEBP_SOURCES EXCLUDE REGEX ".*/.*_(sse2|sse41|avx2|mips|msa|vsx)[.]c$")
list(FILTER SHARPYUV_SOURCES EXCLUDE REGEX ".*/.*_(sse2|avx2)[.]c$")

file(GLOB JPEG_SOURCES CONFIGURE_DEPENDS lib/libjpeg-turbo/src/*.c)
list(FILTER JPEG_SOURCES EXCLUDE REGEX ".*/(cjpeg|djpeg|jpegtran|rdjpgcom|wrjpgcom|example|tjbench|tjunittest|strtest|bmpsizetest|jcstest|tjcomp|tjdecomp|tjtran|cdjpeg|rdbmp|rdcolmap|rdgif|rdpng|rdppm|rdtarga|rdswitch|wrbmp|wrgif|wrpng|wrppm|wrtarga)[.]c$")
list(FILTER JPEG_SOURCES EXCLUDE REGEX ".*/turbojpeg-mp[.]c$")
list(FILTER JPEG_SOURCES EXCLUDE REGEX ".*/(jccolext|jdcolext|jdcol565|jdmrgext|jdmrg565|jstdhuff)[.]c$")
file(GLOB JPEG_PRECISION_SOURCES CONFIGURE_DEPENDS
    lib/libjpeg-turbo/src/wrapper/j*-12.c
    lib/libjpeg-turbo/src/wrapper/j*-16.c
)

add_library(texture SHARED native-lib.cpp ${WEBP_SOURCES} ${SHARPYUV_SOURCES} ${JPEG_SOURCES} ${JPEG_PRECISION_SOURCES})

target_include_directories(texture PRIVATE
    ${CMAKE_CURRENT_BINARY_DIR}/generated
    ${CMAKE_CURRENT_SOURCE_DIR}/lib/libwebp
    ${CMAKE_CURRENT_SOURCE_DIR}/lib/libwebp/src
    ${CMAKE_CURRENT_SOURCE_DIR}/lib/libjpeg-turbo/src
    ${CMAKE_CURRENT_BINARY_DIR}/generated
)

target_compile_definitions(texture PRIVATE STBI_NO_PSD STBIW_NO_STDIO HAVE_CONFIG_H)
target_compile_options(texture PRIVATE -O3 -DNDEBUG -ffunction-sections -fdata-sections -fvisibility=hidden -march=armv8-a+crypto+crc)
target_link_options(texture PRIVATE -Wl,--gc-sections -Wl,--exclude-libs,ALL)
target_link_libraries(texture PRIVATE android log)
