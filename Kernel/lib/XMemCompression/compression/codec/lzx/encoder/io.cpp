#include "../../../api/precomp.hpp"

#include <cstring>

namespace XCOMPRESS
{
void output_bits(t_encoder_context* context, int n, unsigned int x)
{
    int bitcount = static_cast<signed char>(context->enc_bitcount);
    const int shift = bitcount - n;
    bitcount -= n;
    context->enc_bitcount = static_cast<char>(bitcount);
    context->enc_bitbuf |= x << shift;

    while (bitcount <= 16)
    {
        if (context->enc_output_buffer_curpos >= context->enc_output_buffer_end)
        {
            context->enc_output_overflow = true;
            context->enc_output_buffer_curpos = context->enc_output_buffer_start;
        }

        const unsigned int bitbuf = context->enc_bitbuf;
        *context->enc_output_buffer_curpos++ = static_cast<unsigned __int8>((bitbuf >> 16) & 0xFF);
        *context->enc_output_buffer_curpos++ = static_cast<unsigned __int8>((bitbuf >> 24) & 0xFF);
        context->enc_bitbuf <<= 16;
        bitcount += 16;
        context->enc_bitcount = static_cast<char>(bitcount);
    }
}

void output_bits(t_encoder_context* context, int n, unsigned long x)
{
    output_bits(context, n, static_cast<unsigned int>(x));
}

bool init_compressed_output_buffer(t_encoder_context* context)
{
    context->enc_output_buffer_start = static_cast<unsigned __int8*>(context->enc_malloc(0x9800u));
    if (context->enc_output_buffer_start == nullptr)
    {
        return false;
    }

    context->enc_output_buffer_curpos = context->enc_output_buffer_start;
    context->enc_output_buffer_end = context->enc_output_buffer_start + 0x97C0;
    return true;
}

void free_compressed_output_buffer(t_encoder_context* context)
{
    if (context->enc_output_buffer_start != nullptr)
    {
        context->enc_free(context->enc_output_buffer_start);
        context->enc_output_buffer_start = nullptr;
    }
}

void reset_translation(t_encoder_context* context)
{
    context->enc_instr_pos = 0;
}

int read_input_data(t_encoder_context* context, unsigned __int8* mem, int amount)
{
    const int available = context->enc_input_left;
    if (amount <= available)
    {
        ::memcpy(mem, context->enc_input_ptr, static_cast<std::size_t>(amount));
        context->enc_input_left -= amount;
        context->enc_input_ptr += amount;
        return amount;
    }

    if (available <= 0)
    {
        return 0;
    }

    ::memcpy(mem, context->enc_input_ptr, static_cast<std::size_t>(available));
    context->enc_input_ptr += available;
    context->enc_input_left = 0;
    return available;
}

void encoder_translate_e8(t_encoder_context* context, unsigned __int8* mem, int bytes)
{
    unsigned __int8* const base = mem;

    if (bytes <= 6)
    {
        context->enc_instr_pos += static_cast<unsigned int>(bytes);
        return;
    }

    unsigned __int8 tail[6];
    ::memcpy(tail, base + bytes - 6, sizeof(tail));
    ::memset(base + bytes - 6, 0xE8, sizeof(tail));

    const unsigned int end_instr_pos = context->enc_instr_pos + static_cast<unsigned int>(bytes) - 10;

    while (true)
    {
        while (*mem != 0xE8)
        {
            ++context->enc_instr_pos;
            ++mem;
        }

        const unsigned int instr_pos = context->enc_instr_pos;
        if (instr_pos >= end_instr_pos)
        {
            break;
        }

        int relative =
            static_cast<int>(static_cast<unsigned int>(mem[1]) |
                             (static_cast<unsigned int>(mem[2]) << 8) |
                             (static_cast<unsigned int>(mem[3]) << 16) |
                             (static_cast<unsigned int>(mem[4]) << 24));
        int translated = static_cast<int>(instr_pos) + relative;

        if (translated >= 0)
        {
            const int file_size = static_cast<int>(context->enc_file_size_for_translation);
            if (translated < file_size + static_cast<int>(instr_pos))
            {
                if (translated >= file_size)
                {
                    translated = relative - file_size;
                }

                mem[1] = static_cast<unsigned __int8>(translated);
                mem[2] = static_cast<unsigned __int8>(translated >> 8);
                mem[3] = static_cast<unsigned __int8>(translated >> 16);
                mem[4] = static_cast<unsigned __int8>(translated >> 24);
            }
        }

        mem += 5;
        context->enc_instr_pos += 5;
    }

    ::memcpy(base + bytes - 6, tail, sizeof(tail));
    context->enc_instr_pos = end_instr_pos + 10;
}

long comp_read_input(t_encoder_context* context, unsigned long BufPos, long Size)
{
    if (Size <= 0)
    {
        return 0;
    }

    unsigned __int8* const mem = context->enc_RealMemWindow + BufPos;
    int bytes = read_input_data(context, mem, Size);
    if (bytes < 0)
    {
        return 0;
    }

    if ((context->enc_file_size_for_translation != 0) && (context->enc_num_cfdata_frames < 0x8000))
    {
        encoder_translate_e8(context, mem, bytes);
    }

    ++context->enc_num_cfdata_frames;
    return bytes;
}
}
