cmake_minimum_required(VERSION 4.3.0)
project(lexbor LANGUAGES C CXX)

set(CMAKE_C_STANDARD 17)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

file(GLOB_RECURSE LEXBOR_SOURCES CONFIGURE_DEPENDS
    "${CMAKE_CURRENT_SOURCE_DIR}/lib/lexbor/source/lexbor/*.c"
)
list(FILTER LEXBOR_SOURCES EXCLUDE REGEX ".*/ports/windows_nt/.*")

add_library(lexbor SHARED native-lib.cpp ${LEXBOR_SOURCES})
set_target_properties(lexbor PROPERTIES OUTPUT_NAME lexbor POSITION_INDEPENDENT_CODE ON)

target_include_directories(lexbor PRIVATE
    "${CMAKE_CURRENT_SOURCE_DIR}/lib/lexbor/source"
)

target_compile_options(lexbor PRIVATE
    -O3
    -DNDEBUG
    -fvisibility=hidden
    -ffunction-sections
    -fdata-sections
)

target_link_options(lexbor PRIVATE -Wl,--gc-sections)
