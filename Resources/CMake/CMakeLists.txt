cmake_minimum_required(VERSION 3.31)

include("${CMAKE_CURRENT_LIST_DIR}/Project.cmake")

project("${john_project_name}")

include("${CMAKE_CURRENT_LIST_DIR}/John.cmake")

add_subdirectory("${john_project_root}" "${CMAKE_BINARY_DIR}/Project")
