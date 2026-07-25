cmake_minimum_required(VERSION 4.3.0)
project(ncomp LANGUAGES C CXX)

set(CMAKE_C_STANDARD 23)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

set(NCOMP_ROOT ${CMAKE_CURRENT_SOURCE_DIR})

add_library(ncomp SHARED
    native-lib.cpp
    lib/FastLZ/fastlz.c
    lib/heatshrink/heatshrink_decoder.c
    lib/heatshrink/heatshrink_encoder.c
    lib/lzf/lzf_c.c
    lib/lzf/lzf_d.c
    lib/lzw/lzwencode.c
    lib/lzw/lzwdecode.c
    lib/lzw/bitfile/bitfile.c
    lib/microtar/src/microtar.c
    lib/lzfse/src/lzfse_decode.c
    lib/lzfse/src/lzfse_decode_base.c
    lib/lzfse/src/lzfse_encode.c
    lib/lzfse/src/lzfse_encode_base.c
    lib/lzfse/src/lzvn_decode_base.c
    lib/lzfse/src/lzvn_encode_base.c
    lib/lzfse/src/lzfse_fse.c
    lib/libbsc/adler32/adler32.cpp
    lib/libbsc/bwt/bwt.cpp
    lib/libbsc/bwt/libsais/libsais.c
    lib/libbsc/coder/coder.cpp
    lib/libbsc/coder/qlfc/qlfc.cpp
    lib/libbsc/coder/qlfc/qlfc_model.cpp
    lib/libbsc/filters/detectors.cpp
    lib/libbsc/filters/preprocessing.cpp
    lib/libbsc/libbsc/libbsc.cpp
    lib/libbsc/lzp/lzp.cpp
    lib/libbsc/platform/platform.cpp
    lib/libbsc/st/st.cpp
    lib/zpaq/libzpaq.cpp
)

target_include_directories(ncomp PRIVATE
    ${NCOMP_ROOT}/lib/FastLZ
    ${NCOMP_ROOT}/lib/heatshrink
    ${NCOMP_ROOT}/lib/lzf
    ${NCOMP_ROOT}/lib/libbsc
    ${NCOMP_ROOT}/lib/libbsc/bwt
    ${NCOMP_ROOT}/lib/lzfse/src
    ${NCOMP_ROOT}/lib/lzw
    ${NCOMP_ROOT}/lib/microtar/src
    ${NCOMP_ROOT}/lib/zpaq
)

target_compile_definitions(ncomp PRIVATE NOJIT)

target_compile_options(ncomp PRIVATE
    -O3
    -DNDEBUG
    -fexceptions
    -ffunction-sections
    -fdata-sections
)

target_link_options(ncomp PRIVATE -Wl,--gc-sections)
find_library(dl-lib dl)
target_link_libraries(ncomp PRIVATE android log ${dl-lib})
