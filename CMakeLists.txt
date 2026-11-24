
cmake_minimum_required(VERSION 3.10)

project(chiaki)

if(NOT WIN32)
	message(FATAL_ERROR "chiaki-ng is Windows-only. Configure this project with a Windows toolchain (MSYS2/MinGW or MSVC).")
endif()

# Like option(), but the value can also be AUTO
macro(tri_option name desc default)
    set("${name}" "${default}" CACHE STRING "${desc}")
    set_property(CACHE "${name}" PROPERTY STRINGS AUTO ON OFF)
endmacro()

option(CHIAKI_ENABLE_TESTS "Enable tests for Chiaki" ON)
# PP867: CHIAKI_ENABLE_GUI stood here, and gui/ is deleted. PP21 turned it OFF and PP632 retired
# the Qt client's build; what kept the source was the drift checks that read it, and PP867 recorded
# the files they read under tests/oracles/gui before deleting the tree. CHIAKI_ENABLE_SPEEX,
# CHIAKI_GUI_ENABLE_SDL_GAMECONTROLLER and CHIAKI_ENABLE_STEAM_SHORTCUT went with it - only the
# client read them - and so did cmake/FindSDL2.cmake and cmake/FindSpeexDSP.cmake.
option(CHIAKI_LIB_ENABLE_OPUS "Use Opus as part of Chiaki Lib" ON)
# PP430: CHIAKI_ENABLE_RUDP stood here and nothing read it. It appeared once in the whole build -
# its own declaration - while every other option here is referenced between three and fourteen
# times, so setting it OFF changed nothing: holepunch.c, rudp.c and rudpsendbuffer.c are in the
# unconditional source list and curl and json-c link unconditionally.
#
# It goes rather than getting wired up, and PP340 is why. Gating those three sources without
# gating their callers breaks the build - session.c drives the PSN path from nine call sites with
# no conditional compilation - so a real gate is that task's work and not a line here.
#
# This also removes PP313's second door rather than deciding it: there was never a door, because
# the option was inert.
#
# PP867: AUTO unconditionally. The default was ON only when the Qt client was built.
tri_option(CHIAKI_ENABLE_FFMPEG_DECODER "Enable FFMPEG video decoder" AUTO)
option(CHIAKI_LIB_ENABLE_MBEDTLS "Use mbedtls instead of OpenSSL as part of Chiaki Lib" OFF)
option(CHIAKI_LIB_MBEDTLS_EXTERNAL_PROJECT "Fetch Mbed TLS instead of using system-provided libs" OFF)
option(CHIAKI_LIB_OPENSSL_EXTERNAL_PROJECT "Use OpenSSL as CMake external project" OFF)
# PP30: CHIAKI_USE_SYSTEM_JERASURE stood here and is gone with the library it chose between.
# An option that selects a source for a dependency nothing links is a question with no consequence,
# and BuildOptionsAreRead would have gone on reporting it as read because the block below read it.
tri_option(CHIAKI_USE_SYSTEM_NANOPB "Use system-provided nanopb instead of submodule" AUTO)
option(CHIAKI_LIB_MINIUPNPC_EXTERNAL_PROJECT "Build miniupnpc from source via FetchContent" OFF)

# PP876: Transmiti's own release numbering, held equal to <Version> in app/Transmiti.csproj.
set(CHIAKI_VERSION_MAJOR 0)
set(CHIAKI_VERSION_MINOR 1)
set(CHIAKI_VERSION_PATCH 0)
set(CHIAKI_VERSION ${CHIAKI_VERSION_MAJOR}.${CHIAKI_VERSION_MINOR}.${CHIAKI_VERSION_PATCH})

set(CPACK_PACKAGE_NAME "chiaki-ng")
set(CPACK_PACKAGE_DESCRIPTION "Open Source PlayStation Remote Play client for Windows")
set(CPACK_PACKAGE_VERSION_MAJOR ${CHIAKI_VERSION_MAJOR})
set(CPACK_PACKAGE_VERSION_MINOR ${CHIAKI_VERSION_MINOR})
set(CPACK_PACKAGE_VERSION_PATCH ${CHIAKI_VERSION_PATCH})
include(CPack)

list(APPEND CMAKE_MODULE_PATH "${CMAKE_CURRENT_SOURCE_DIR}/cmake")
set(CMAKE_CXX_STANDARD 11)

# PP30: the jerasure probe stood here, and cmake/FindJerasure.cmake with it.
#
# It was the only consumer of that find module, so the module went in the same commit - a find
# module for a package nothing looks for is dead weight that still resolves, which is worse than
# absent because a later reader takes it as evidence the dependency is wanted.

# PP21: libplacebo is the PORT's, not the Qt client's, and it was found inside the client's branch.
#
# chiaki-render (PP9) links PkgConfig::LIBPLACEBO and is built unconditionally beside the shim, for
# the reason PP4 gives: it is the half with no Qt in it. But its dependency was discovered under
# the client's option, so the first build with the GUI genuinely off failed at
# shim/CMakeLists.txt:53 - a target that has nothing to do with Qt, broken by removing Qt.
#
# That is the tail this task was filed for, arriving somewhere nobody predicted: not QString in a
# file nobody thought of as UI, but a find_package riding along in the block being deleted.
find_package(PkgConfig REQUIRED)
pkg_check_modules(LIBPLACEBO REQUIRED libplacebo>=7.349.0 IMPORTED_TARGET)

# PP32: the speexdsp probe stood here, gated on the Qt client since PP632 because only the client's
# microphone path linked it and lib/ references speex nowhere. PP867 deleted the client, so the probe,
# its option and cmake/FindSpeexDSP.cmake went with it.

# PP431: FindPython3, because CMP0148 says FindPythonInterp is REMOVED - not deprecated. The old
# call worked only while cmake_minimum_required names 3.10 and so selects that policy's OLD
# behaviour; on a CMake where it is NEW, configure fails rather than warns.
#
# The comment that stood here said this was to stop nanopb finding Python 2.7, and it was not:
# FindPythonInterp set PYTHON_EXECUTABLE, nanopb reads Python_EXECUTABLE, and
# third-party/nanopb/CMakeLists.txt does its own find_package(Python) with the modern module.
# Nothing this sets reaches nanopb.
#
# What it IS for is lib/protobuf, which runs the nanopb generator as this interpreter and is the
# only consumer in the tree. Interpreter alone: no headers or libraries are wanted here.
find_package(Python3 3 REQUIRED COMPONENTS Interpreter)

if(CHIAKI_USE_SYSTEM_NANOPB)
	if(CHIAKI_USE_SYSTEM_NANOPB STREQUAL AUTO)
		find_package(Nanopb QUIET)
		set(CHIAKI_USE_SYSTEM_NANOPB ${Nanopb_FOUND})
	else()
		find_package(Nanopb REQUIRED)
		set(CHIAKI_USE_SYSTEM_NANOPB ON)
	endif()
endif()

if(CHIAKI_LIB_OPENSSL_EXTERNAL_PROJECT)
	include(OpenSSLExternalProject)
endif()

add_subdirectory(third-party)

add_definitions(-DCHIAKI_VERSION_MAJOR=${CHIAKI_VERSION_MAJOR} -DCHIAKI_VERSION_MINOR=${CHIAKI_VERSION_MINOR} -DCHIAKI_VERSION_PATCH=${CHIAKI_VERSION_PATCH} -DCHIAKI_VERSION=\"${CHIAKI_VERSION}\")

if(CHIAKI_LIB_ENABLE_MBEDTLS)
	add_definitions(-DCHIAKI_LIB_ENABLE_MBEDTLS)
	if(CHIAKI_LIB_MBEDTLS_EXTERNAL_PROJECT)
		set(FETCHCONTENT_QUIET CACHE BOOL FALSE)
		include(FetchContent)
		set(ENABLE_TESTING CACHE INTERNAL OFF)
		set(ENABLE_PROGRAMS CACHE INTERNAL OFF)
		set(USE_SHARED_MBEDTLS_LIBRARY CACHE INTERNAL OFF)
		FetchContent_Declare(
			mbedtls
			GIT_REPOSITORY https://github.com/Mbed-TLS/mbedtls.git
			GIT_TAG 8b3f26a5ac38d4fdccbc5c5366229f3e01dafcc0 # v2.28.0
			GIT_PROGRESS TRUE
		)
		FetchContent_MakeAvailable(mbedtls)
	endif()
endif()

if(CHIAKI_ENABLE_FFMPEG_DECODER)
	find_package(FFMPEG COMPONENTS avcodec avutil avformat)
	if(FFMPEG_FOUND)
		set(CHIAKI_ENABLE_FFMPEG_DECODER ON)
	else()
		if(NOT CHIAKI_ENABLE_FFMPEG_DECODER STREQUAL AUTO)
			message(FATAL_ERROR "CHIAKI_ENABLE_FFMPEG_DECODER is set to ON, but ffmpeg could not be found.")
		endif()
		set(CHIAKI_ENABLE_FFMPEG_DECODER OFF)
	endif()
endif()

if(CHIAKI_ENABLE_FFMPEG_DECODER)
	message(STATUS "FFMPEG Decoder enabled")
else()
	message(STATUS "FFMPEG Decoder disabled")
endif()

if(CHIAKI_LIB_MINIUPNPC_EXTERNAL_PROJECT)
	include(MiniupnpcExternalProject)
endif()

add_subdirectory(lib)

# PP4: the seam the .NET host calls through - the half of the port that has no Qt in it at all.
# PP867: the Qt client's branch stood below this, and went with gui/.
add_subdirectory(shim)

if(CHIAKI_ENABLE_TESTS)
	enable_testing()
	add_subdirectory(test)
endif()
