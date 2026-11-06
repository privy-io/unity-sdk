cmake_minimum_required(VERSION 3.16)
project(PrivyWebViewWindows LANGUAGES CXX)

set(PLUGIN_OUTPUT_DIR "${CMAKE_CURRENT_SOURCE_DIR}/x86_64")

add_library(PrivyWebView SHARED PrivyWebView.cpp)

# Ensure output is placed in the Unity plugin folder
set_target_properties(PrivyWebView PROPERTIES
    CXX_STANDARD 17
    CXX_STANDARD_REQUIRED ON
    RUNTIME_OUTPUT_DIRECTORY "${PLUGIN_OUTPUT_DIR}"
    LIBRARY_OUTPUT_DIRECTORY "${PLUGIN_OUTPUT_DIR}"
    ARCHIVE_OUTPUT_DIRECTORY "${PLUGIN_OUTPUT_DIR}"
)

# WebView2 SDK support
# User can specify WEBVIEW2_ROOT to point to the WebView2 SDK root (contains include/ and lib/)
if(NOT WEBVIEW2_ROOT)
    # ProgramFiles(x86) contains parentheses and cannot be referenced directly in CMake variables.
    # Use cmd to expand it instead.
    execute_process(COMMAND cmd /c "echo %ProgramFiles(x86)%" OUTPUT_VARIABLE _progfiles_x86 OUTPUT_STRIP_TRAILING_WHITESPACE)
    execute_process(COMMAND cmd /c "echo %ProgramFiles%" OUTPUT_VARIABLE _progfiles OUTPUT_STRIP_TRAILING_WHITESPACE)

    if(_progfiles_x86)
        set(_webview2_candidate "${_progfiles_x86}/Microsoft WebView2 SDK")
    elseif(_progfiles)
        set(_webview2_candidate "${_progfiles}/Microsoft WebView2 SDK")
    endif()

    if(_webview2_candidate)
        set(WEBVIEW2_ROOT "${_webview2_candidate}")
    endif()
endif()

# Primary (recommended) include path
find_path(WEBVIEW2_INCLUDE_DIR WebView2.h
    PATHS
        "${WEBVIEW2_ROOT}/include"
        "${WEBVIEW2_ROOT}/*/include"
    NO_DEFAULT_PATH
)

# Primary lib path
find_library(WEBVIEW2_LIB WebView2LoaderStatic
    PATHS
        "${WEBVIEW2_ROOT}/x64"
        "${WEBVIEW2_ROOT}/x86"
        "${WEBVIEW2_ROOT}/*/x64"
        "${WEBVIEW2_ROOT}/*/x86"
    NO_DEFAULT_PATH
)

# Fallback: if we still don't find it, check the expected folder layout directly.
if(NOT WEBVIEW2_INCLUDE_DIR)
    if(EXISTS "${WEBVIEW2_ROOT}/include/WebView2.h")
        set(WEBVIEW2_INCLUDE_DIR "${WEBVIEW2_ROOT}/include")
    endif()
endif()

if(NOT WEBVIEW2_LIB)
    if(EXISTS "${WEBVIEW2_ROOT}/x64/WebView2LoaderStatic.lib")
        set(WEBVIEW2_LIB "${WEBVIEW2_ROOT}/x64/WebView2LoaderStatic.lib")
    elseif(EXISTS "${WEBVIEW2_ROOT}/x86/WebView2LoaderStatic.lib")
        set(WEBVIEW2_LIB "${WEBVIEW2_ROOT}/x86/WebView2LoaderStatic.lib")
    endif()
endif()

message(STATUS "WebView2 SDK root candidate: ${WEBVIEW2_ROOT}")
message(STATUS "Searching for WebView2.h under: ${WEBVIEW2_ROOT}/include")

if(WEBVIEW2_INCLUDE_DIR AND WEBVIEW2_LIB)
    message(STATUS "Found WebView2 include: ${WEBVIEW2_INCLUDE_DIR}")
    message(STATUS "Found WebView2 lib: ${WEBVIEW2_LIB}")
    target_include_directories(PrivyWebView PRIVATE "${WEBVIEW2_INCLUDE_DIR}")
    target_link_libraries(PrivyWebView PRIVATE "${WEBVIEW2_LIB}")
else()
    message(FATAL_ERROR "WebView2 SDK not found. Please install WebView2 SDK (https://developer.microsoft.com/microsoft-edge/webview2/) and ensure WebView2.h and WebView2.lib are available. You can also set WEBVIEW2_ROOT to the SDK root folder.")
endif()
