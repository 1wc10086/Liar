cmake_minimum_required(VERSION 4.3.0,)
project(serdes LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

file(GLOB_RECURSE C4CORE_SOURCES CONFIGURE_DEPENDS
    "${CMAKE_CURRENT_SOURCE_DIR}/lib/rapidyaml/ext/c4core.src/c4/*.cpp"
)
file(GLOB_RECURSE RAPIDYAML_SOURCES CONFIGURE_DEPENDS
    "${CMAKE_CURRENT_SOURCE_DIR}/lib/rapidyaml/src/c4/yml/*.cpp"
)

add_library(serdes SHARED native-lib.cpp ${C4CORE_SOURCES} ${RAPIDYAML_SOURCES})

target_include_directories(serdes PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/lib
    ${CMAKE_CURRENT_SOURCE_DIR}/lib/rapidyaml/src
    ${CMAKE_CURRENT_SOURCE_DIR}/lib/rapidyaml/ext/c4core.src
)

target_compile_options(serdes PRIVATE
    -O3
    -DNDEBUG
    -fvisibility=hidden
    -fexceptions
    -ffunction-sections
    -fdata-sections
)

target_link_options(serdes PRIVATE -Wl,--gc-sections)
