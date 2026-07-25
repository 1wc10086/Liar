module;
#include <algorithm>
#include <cstddef>
#include <cstdint>
#include <memory>
#include <mutex>
#include <span>
#include <stdexcept>
#include <string_view>
#include <thread>
#include <unordered_map>
#include <vector>
#include "lib/astc-encoder/Source/astcenc.h"
export module utility.astc.astc;
import utility.binary.unified_binary_stream;
import utility.png.png;
import tool.shell.config_manager;

export {
namespace ASTCHelper {
enum class Quality { Fastest, Fast, Medium, Thorough, Exhaustive };
inline Quality GetDefaultQuality() { const auto q = ConfigManager::get().getSetting("astc_quality", "Fastest"); if (q == "Fast") return Quality::Fast; if (q == "Medium") return Quality::Medium; if (q == "Thorough") return Quality::Thorough; if (q == "Exhaustive") return Quality::Exhaustive; return Quality::Fastest; }
inline float GetQualityFloat(Quality q) noexcept { switch (q) { case Quality::Fastest: return ASTCENC_PRE_FASTEST; case Quality::Fast: return ASTCENC_PRE_FAST; case Quality::Medium: return ASTCENC_PRE_MEDIUM; case Quality::Thorough: return ASTCENC_PRE_THOROUGH; case Quality::Exhaustive: return ASTCENC_PRE_EXHAUSTIVE; default: return ASTCENC_PRE_FAST; } }
inline unsigned int GetHardwareThreads() noexcept { static const auto threads = std::max(1u, std::thread::hardware_concurrency()); return threads; }
struct ContextKey { int bw; int bh; bool is_encode; Quality quality; unsigned int num_threads; bool operator==(const ContextKey&) const = default; };
struct ContextKeyHash { std::size_t operator()(const ContextKey& k) const noexcept { std::size_t h{}; auto combine = [&h](auto v) { h ^= std::hash<decltype(v)>{}(v) + 0x9e3779b9 + (h << 6) + (h >> 2); }; combine(k.bw); combine(k.bh); combine(k.is_encode); combine(static_cast<int>(k.quality)); combine(k.num_threads); return h; } };
class AstcContextPool { struct ContextWrapper { astcenc_context* ctx{}; ~ContextWrapper() { if (ctx) astcenc_context_free(ctx); } }; std::mutex mutex_; std::unordered_multimap<ContextKey, std::unique_ptr<ContextWrapper>, ContextKeyHash> pool_; public: static AstcContextPool& Instance() { static AstcContextPool instance; return instance; } struct ReturnToPool { AstcContextPool* pool; ContextKey key; void operator()(ContextWrapper* context) const { std::lock_guard lock(pool->mutex_); pool->pool_.emplace(key, std::unique_ptr<ContextWrapper>(context)); } }; using ContextPtr = std::unique_ptr<ContextWrapper, ReturnToPool>; ContextPtr GetContext(const ContextKey& key) { { std::lock_guard lock(mutex_); if (auto it = pool_.find(key); it != pool_.end()) { auto context = std::move(it->second); pool_.erase(it); return {context.release(), {this, key}}; } } astcenc_config config{}; if (astcenc_config_init(ASTCENC_PRF_LDR, key.bw, key.bh, 1, GetQualityFloat(key.quality), key.is_encode ? 0 : ASTCENC_FLG_DECOMPRESS_ONLY, &config) != ASTCENC_SUCCESS) throw std::runtime_error("Failed to init ASTC config"); astcenc_context* context{}; if (astcenc_context_alloc(&config, key.num_threads, &context, nullptr) != ASTCENC_SUCCESS) throw std::runtime_error("Failed to alloc ASTC context"); return {new ContextWrapper{context}, {this, key}}; } };
inline void DecodeASTC(std::span<const uint8_t> input, int w, int h, int bw, int bh, ImageColor* output) { const auto threads = GetHardwareThreads(); auto context = AstcContextPool::Instance().GetContext({bw, bh, false, Quality::Fastest, threads}); astcenc_image image{}; image.dim_x = w; image.dim_y = h; image.dim_z = 1; image.data_type = ASTCENC_TYPE_U8; void* slices[]{output}; image.data = slices; const astcenc_swizzle swizzle{ASTCENC_SWZ_R, ASTCENC_SWZ_G, ASTCENC_SWZ_B, ASTCENC_SWZ_A}; astcenc_decompress_reset(context->ctx); auto worker = [&](unsigned int i) { astcenc_decompress_image(context->ctx, input.data(), input.size(), &image, &swizzle, i); }; std::vector<std::jthread> workers; for (unsigned int i = 1; i < threads; ++i) workers.emplace_back(worker, i); worker(0); }
inline void EncodeASTC(const ImageColor* input, int w, int h, int bw, int bh, std::span<uint8_t> output, Quality quality) { const auto threads = GetHardwareThreads(); auto context = AstcContextPool::Instance().GetContext({bw, bh, true, quality, threads}); astcenc_image image{}; image.dim_x = w; image.dim_y = h; image.dim_z = 1; image.data_type = ASTCENC_TYPE_U8; void* slices[]{const_cast<ImageColor*>(input)}; image.data = slices; const astcenc_swizzle swizzle{ASTCENC_SWZ_R, ASTCENC_SWZ_G, ASTCENC_SWZ_B, ASTCENC_SWZ_A}; astcenc_compress_reset(context->ctx); auto worker = [&](unsigned int i) { astcenc_compress_image(context->ctx, &image, &swizzle, output.data(), output.size(), i); }; std::vector<std::jthread> workers; for (unsigned int i = 1; i < threads; ++i) workers.emplace_back(worker, i); worker(0); }
}
template <int W, int H> struct ASTC_RGBA { static ImageBitmap* read(UnifiedBinaryStream& bs, int w, int h) { const auto size = static_cast<size_t>((w + W - 1) / W) * ((h + H - 1) / H) * 16; auto data = bs.readBytes(size); auto img = ImageBitmap::create(w, h); ASTCHelper::DecodeASTC(data, w, h, W, H, img->getPixels()); return img; } static uint32_t write(UnifiedBinaryStream& bs, const ImageBitmap* img) { const auto w = img->getWidth(), h = img->getHeight(); std::vector<uint8_t> data(static_cast<size_t>((w + W - 1) / W) * ((h + H - 1) / H) * 16); ASTCHelper::EncodeASTC(img->getPixels(), w, h, W, H, data, ASTCHelper::GetDefaultQuality()); bs.writeBytes(data); if constexpr (W == 4) return w; else if constexpr (W == 5) return w * 16 / 25; else if constexpr (W == 6) return w * 4 / 9; else return w / 4; } };
}
