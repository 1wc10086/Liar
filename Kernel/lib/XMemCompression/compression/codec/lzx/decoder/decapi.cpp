#include "../../../api/precomp.hpp"

#include <cstring>

namespace XCOMPRESS
{
void LZX_DecodeFree(t_decoder_context* context)
{
    free_decompression_memory(context);
}

void LZX_DecodeNewGroup(t_decoder_context* context)
{
    reset_decoder_trees(context);
    decoder_misc_init(context);
    init_decoder_translation(context);
    context->dec_num_cfdata_frames = 0;
}

int LZX_Decode(
    t_decoder_context* context,
    long bytes_to_decode,
    unsigned __int8* compressed_input_buffer,
    long compressed_input_size,
    unsigned __int8* uncompressed_output_buffer,
    long uncompressed_output_size,
    long* bytes_decoded)
{
    (void)uncompressed_output_size;

    context->dec_input_curpos = compressed_input_buffer;
    context->dec_output_buffer = uncompressed_output_buffer;
    context->dec_end_input_pos = compressed_input_buffer + compressed_input_size + 4;

    init_decoder_input(context);

    const long decoded = decode_data(context, bytes_to_decode);
    ++context->dec_num_cfdata_frames;

    if (decoded < 0)
    {
        *bytes_decoded = 0;
        return 1;
    }

    *bytes_decoded = decoded;
    context->dec_position_at_start += decoded;
    return 0;
}

bool LZX_DecodeInsertDictionary(
    t_decoder_context* context,
    const unsigned __int8* data,
    unsigned long data_size)
{
    const unsigned int window_size = context->dec_window_size;
    if (data_size > window_size)
    {
        return false;
    }

    ::memcpy(context->dec_mem_window + (window_size - data_size), data, data_size);

    if (data_size < window_size)
    {
        ::memset(context->dec_mem_window, 0, window_size - data_size);
    }

    return true;
}

void build_global_tables(t_decoder_context* context)
{
    unsigned int* const dec_extra_bits_table = reinterpret_cast<unsigned int*>(context->dec_extra_bits_table);

    dec_extra_bits_table[0] = 0x00000000;
    dec_extra_bits_table[1] = 0x02020101;
    dec_extra_bits_table[2] = 0x04040303;
    dec_extra_bits_table[3] = 0x06060505;
    dec_extra_bits_table[4] = 0x08080707;
    dec_extra_bits_table[5] = 0x0A0A0909;
    dec_extra_bits_table[6] = 0x0C0C0B0B;
    dec_extra_bits_table[7] = 0x0E0E0D0D;
    dec_extra_bits_table[8] = 0x10100F0F;
    dec_extra_bits_table[9] = 0x11111111;
    dec_extra_bits_table[10] = 0x11111111;
    dec_extra_bits_table[11] = 0x11111111;
    dec_extra_bits_table[12] = 0x11111111;

    context->MP_POS_minus2_table[0] = -2;
    context->MP_POS_minus2_table[1] = -1;
    context->MP_POS_minus2_table[2] = 0;
    context->MP_POS_minus2_table[3] = 1;
    context->MP_POS_minus2_table[4] = 2;
    context->MP_POS_minus2_table[5] = 4;
    context->MP_POS_minus2_table[6] = 6;
    context->MP_POS_minus2_table[7] = 10;
    context->MP_POS_minus2_table[8] = 14;
    context->MP_POS_minus2_table[9] = 22;
    context->MP_POS_minus2_table[10] = 30;
    context->MP_POS_minus2_table[11] = 46;
    context->MP_POS_minus2_table[12] = 62;
    context->MP_POS_minus2_table[13] = 94;
    context->MP_POS_minus2_table[14] = 126;
    context->MP_POS_minus2_table[15] = 190;
    context->MP_POS_minus2_table[16] = 254;
    context->MP_POS_minus2_table[17] = 382;
    context->MP_POS_minus2_table[18] = 510;
    context->MP_POS_minus2_table[19] = 766;
    context->MP_POS_minus2_table[20] = 1022;
    context->MP_POS_minus2_table[21] = 1534;
    context->MP_POS_minus2_table[22] = 2046;
    context->MP_POS_minus2_table[23] = 3070;
    context->MP_POS_minus2_table[24] = 4094;
    context->MP_POS_minus2_table[25] = 6142;
    context->MP_POS_minus2_table[26] = 8190;
    context->MP_POS_minus2_table[27] = 12286;
    context->MP_POS_minus2_table[28] = 16382;
    context->MP_POS_minus2_table[29] = 24574;
    context->MP_POS_minus2_table[30] = 32766;
    context->MP_POS_minus2_table[31] = 49150;
    context->MP_POS_minus2_table[32] = 65534;
    context->MP_POS_minus2_table[33] = 98302;
    context->MP_POS_minus2_table[34] = 131070;
    context->MP_POS_minus2_table[35] = 196606;
    context->MP_POS_minus2_table[36] = 262142;
    context->MP_POS_minus2_table[37] = 393214;
    context->MP_POS_minus2_table[38] = 524286;
    context->MP_POS_minus2_table[39] = 655358;
    context->MP_POS_minus2_table[40] = 786430;
    context->MP_POS_minus2_table[41] = 917502;
    context->MP_POS_minus2_table[42] = 1048574;
    context->MP_POS_minus2_table[43] = 1179646;
    context->MP_POS_minus2_table[44] = 1310718;
    context->MP_POS_minus2_table[45] = 1441790;
    context->MP_POS_minus2_table[46] = 1572862;
    context->MP_POS_minus2_table[47] = 1703934;
    context->MP_POS_minus2_table[48] = 1835006;
    context->MP_POS_minus2_table[49] = 1966078;
    context->MP_POS_minus2_table[50] = 2097150;
}

void* dec_malloc(t_decoder_context* context, unsigned long cb)
{
    if (context->dec_malloc != nullptr)
    {
        return context->dec_malloc(cb);
    }

    void* const memory = context->dec_memory;
    context->dec_memory = static_cast<unsigned __int8*>(memory) + cb;
    return memory;
}

void dec_free(t_decoder_context* context, void* pv)
{
    if (context->dec_free != nullptr)
    {
        context->dec_free(pv);
    }
}

bool LZX_DecodeInit(t_decoder_context* context, long compression_window_size)
{
    build_global_tables(context);

    const unsigned int window_size = static_cast<unsigned int>(compression_window_size);
    context->dec_window_size = window_size;
    context->dec_window_mask = window_size - 1;

    if ((window_size & context->dec_window_mask) != 0)
    {
        return false;
    }

    if (!allocate_decompression_memory(context))
    {
        return false;
    }

    LZX_DecodeNewGroup(context);
    return true;
}
}
