#pragma once

#include <cstddef>
#include <cstdlib>

namespace XCOMPRESS
{
inline void* XMCDMemAlloc(std::size_t Size)
{
    // Liar : change - replace the Windows process heap with the C++ runtime allocator.
    return std::malloc(Size);
}

inline void XMCDMemFree(void* pMemory)
{
    // Liar : change - paired with the portable allocation above.
    std::free(pMemory);
}
}
