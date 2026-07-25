#include "../api/precomp.hpp"

#include <cstdarg>
#include <cstdio>

namespace XCOMPRESS
{
void XMCDPRINT(char* Format, ...)
{
    char string[260]{};
    va_list ArgList;
    va_start(ArgList, Format);
    // Liar : change - use a portable bounded formatter instead of _vsnprintf.
    std::vsnprintf(string, sizeof(string), Format, ArgList);
    va_end(ArgList);
}
}
