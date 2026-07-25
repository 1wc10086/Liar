add_library(XMemCompression STATIC
    XMemCompression.c
    ../compression/api/compress.cpp
    ../compression/api/decompress.cpp
    ../compression/api/pchxmcdapi.cpp
    ../compression/codec/lzx/decoder/decalign.cpp
    ../compression/codec/lzx/decoder/decapi.cpp
    ../compression/codec/lzx/decoder/decblk.cpp
    ../compression/codec/lzx/decoder/decin.cpp
    ../compression/codec/lzx/decoder/decinit.cpp
    ../compression/codec/lzx/decoder/decout.cpp
    ../compression/codec/lzx/decoder/dectree.cpp
    ../compression/codec/lzx/decoder/decuncmp.cpp
    ../compression/codec/lzx/decoder/decverb.cpp
    ../compression/codec/lzx/decoder/decxlat.cpp
    ../compression/codec/lzx/decoder/ldi.cpp
    ../compression/codec/lzx/decoder/ldiapi.cpp
    ../compression/codec/lzx/decoder/maketbl.cpp
    ../compression/codec/lzx/encoder/blkinit.cpp
    ../compression/codec/lzx/encoder/block.cpp
    ../compression/codec/lzx/encoder/bsearch.cpp
    ../compression/codec/lzx/encoder/encapi.cpp
    ../compression/codec/lzx/encoder/encdata.cpp
    ../compression/codec/lzx/encoder/encstats.cpp
    ../compression/codec/lzx/encoder/enctree.cpp
    ../compression/codec/lzx/encoder/init.cpp
    ../compression/codec/lzx/encoder/io.cpp
    ../compression/codec/lzx/encoder/lci.cpp
    ../compression/codec/lzx/encoder/lciapi.cpp
    ../compression/codec/lzx/encoder/optenc.cpp
    ../compression/codec/lzx/encoder/tree.cpp
    ../compression/common/xmcddebug.cpp
)

set_property(TARGET XMemCompression PROPERTY C_STANDARD 23)
set_property(TARGET XMemCompression PROPERTY CXX_STANDARD 23)
set_property(TARGET XMemCompression PROPERTY CXX_STANDARD_REQUIRED ON)

target_include_directories(
    XMemCompression
    PRIVATE
        ${CMAKE_CURRENT_SOURCE_DIR}
        ${CMAKE_CURRENT_SOURCE_DIR}/../compression/api
        ${CMAKE_CURRENT_SOURCE_DIR}/../compression/inc
)

if (WIN32)
    target_link_libraries(XMemCompression PRIVATE Advapi32)
endif()

if (MSVC)
    target_compile_options(XMemCompression PRIVATE /Od /Ob0)
    target_link_options(XMemCompression PRIVATE /OPT:NOREF /OPT:NOICF)
endif()
