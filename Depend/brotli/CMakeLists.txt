cmake_minimum_required(VERSION 4.3.0)
project(brotli LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

set(BUILD_SHARED_LIBS OFF CACHE BOOL "" FORCE)

file(GLOB_RECURSE BROTLI_C_SOURCES CONFIGURE_DEPENDS
        "${CMAKE_CURRENT_SOURCE_DIR}/lib/brotli/c/common/*.c"
        "${CMAKE_CURRENT_SOURCE_DIR}/lib/brotli/c/dec/*.c"
        "${CMAKE_CURRENT_SOURCE_DIR}/lib/brotli/c/enc/*.c"
)

add_library(brotli SHARED
        native-lib.cpp
        ${BROTLI_C_SOURCES}
)

target_include_directories(brotli PRIVATE
        ${CMAKE_CURRENT_SOURCE_DIR}/lib/brotli/c/include
        ${CMAKE_CURRENT_SOURCE_DIR}/lib/brotli/c
)

target_compile_definitions(brotli PRIVATE BROTLI_SHARED_COMPILATION)

target_compile_options(brotli PRIVATE
        -O3
        -DNDEBUG
        -ffunction-sections
        -fdata-sections
)

target_link_libraries(brotli
        android
        log)
