cmake_minimum_required(VERSION 3.16)
project(PrivyWebViewPlugins LANGUAGES C CXX)

if(WIN32 OR CMAKE_SYSTEM_NAME STREQUAL "Windows")
    add_subdirectory(Windows)
endif()
