#pragma once

#ifndef _SAMPLE_DEBUG_OPTIONS_
#define _SAMPLE_DEBUG_OPTIONS_

#include "CreateOptions.h"
FT_CREATE_OPTIONS_V1 _g_val(FT_CREATE_OPTIONS_FLAGS_DEBUG_DEPTH_MASK);
PVOID _opt = &_g_val;  

#endif