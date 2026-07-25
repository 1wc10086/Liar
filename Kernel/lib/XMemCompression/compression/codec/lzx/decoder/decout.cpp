#include "../../../api/precomp.hpp"

#include <cstring>

namespace XCOMPRESS
{
void copy_data_to_output(t_decoder_context* context, long amount, const unsigned __int8* data)
{
    if (context->dec_output_buffer == nullptr)
    {
        return;
    }

    const bool translate_e8 =
        (context->dec_current_file_size != 0) &&
        (context->dec_num_cfdata_frames < 0x8000);

    ::memcpy(context->dec_output_buffer, data, static_cast<std::size_t>(amount));

    if (translate_e8)
    {
        decoder_translate_e8(context, context->dec_output_buffer, static_cast<int>(amount));
    }
}
}
