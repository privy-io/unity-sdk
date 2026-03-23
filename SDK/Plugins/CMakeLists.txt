cmake_minimum_required(VERSION 3.16)
project(PrivyWebViewPlugins LANGUAGES C CXX)

option(BUILD_WINDOWS_PLUGIN "Build Windows WebView2 plugin" ON)
option(BUILD_LINUX_PLUGIN "Build Linux WebKitGTK plugin" ON)

if(BUILD_WINDOWS_PLUGIN AND (WIN32 OR CMAKE_SYSTEM_NAME STREQUAL "Windows"))
    add_subdirectory(Windows)
endif()

if(BUILD_LINUX_PLUGIN AND (UNIX AND NOT APPLE))
    add_subdirectory(Linux)
endif()
