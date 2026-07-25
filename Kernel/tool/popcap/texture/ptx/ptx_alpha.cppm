module;
#include <algorithm>
#include <array>
#include <cstddef>
#include <cstdint>
#include <span>
#include <stdexcept>
#include <vector>
export module tool.popcap.texture.ptx.ptx_alpha;
import utility.png.png;
export namespace ImagePtxCodec::Alpha {
enum class Scheme : uint8_t { None, A8, A8_A1, Palette4_A1, Palette5_A1 };
struct DetectResult { Scheme scheme = Scheme::None; bool valid = false; };
inline uint8_t fourToEight(uint8_t v) noexcept { return uint8_t((v << 4) | v); }
inline uint8_t fiveToEight(uint8_t v) noexcept { return uint8_t((v << 3) | (v >> 2)); }
inline uint8_t eightToFour(uint8_t v) noexcept { return uint8_t((uint32_t(v) * 15u + 127u) / 255u); }
inline uint8_t eightToFive(uint8_t v) noexcept { return uint8_t((uint32_t(v) * 31u + 127u) / 255u); }
inline uint8_t eightToOne(uint8_t v) noexcept { return v >= 128; }
inline int paletteBitWidth(int count) noexcept { if (!count) return 1; int bits = 0, x = 1; while (x < count) { x <<= 1; ++bits; } return bits; }
class BitReaderMSB { std::span<const uint8_t> data_; size_t pos_{}; public: explicit BitReaderMSB(std::span<const uint8_t> data) : data_(data) {} uint32_t readBits(int bits) noexcept { uint32_t v{}; for(int i=0;i<bits;++i) { v<<=1; if(pos_/8<data_.size()) v|=(data_[pos_/8]>>(7-(pos_&7)))&1u; ++pos_; } return v; } };
class BitWriterMSB { std::vector<uint8_t> data_; size_t pos_{}; public: explicit BitWriterMSB(size_t bits):data_((bits+7)/8){} void writeBits(uint32_t v,int bits) noexcept { for(int i=bits-1;i>=0;--i) { if((v>>i)&1u) data_[pos_/8]|=uint8_t(1u<<(7-(pos_&7))); ++pos_; } } std::vector<uint8_t> take() && noexcept{return std::move(data_);} };
inline void decodeA8(std::span<const uint8_t> p,ImageBitmap& b){size_t n=size_t(b.getWidth())*b.getHeight();if(p.size()<n)throw std::runtime_error("PTX alpha A8 payload too small");for(size_t i=0;i<n;++i)b.getPixels()[i].a=p[i];}
inline std::vector<uint8_t> encodeA8(const ImageBitmap& b){size_t n=size_t(b.getWidth())*b.getHeight();std::vector<uint8_t> o(n);for(size_t i=0;i<n;++i)o[i]=b.getPixels()[i].a;return o;}
inline void decodeA1Bitstream(std::span<const uint8_t> p,ImageBitmap& b){BitReaderMSB r(p);for(size_t i=0,n=size_t(b.getWidth())*b.getHeight();i<n;++i)b.getPixels()[i].a=r.readBits(1)?255:0;}
inline std::vector<uint8_t> encodeA1Bitstream(const ImageBitmap& b){size_t n=size_t(b.getWidth())*b.getHeight();BitWriterMSB w(n);for(size_t i=0;i<n;++i)w.writeBits(eightToOne(b.getPixels()[i].a),1);return std::move(w).take();}
inline bool canUsePureA1(const ImageBitmap& b) noexcept {for(size_t i=0,n=size_t(b.getWidth())*b.getHeight();i<n;++i)if(b.getPixels()[i].a!=0&&b.getPixels()[i].a!=255)return false;return true;}
inline void decodeA8_A1(std::span<const uint8_t> p,ImageBitmap& b){if(p.empty())throw std::runtime_error("PTX alpha A8_A1 payload too small");if(p[0]==0)decodeA1Bitstream(p.subspan(1),b);else decodeA8(p.subspan(1),b);}
inline std::vector<uint8_t> encodeA8_A1(const ImageBitmap& b){auto body=canUsePureA1(b)?encodeA1Bitstream(b):encodeA8(b);std::vector<uint8_t> o;o.reserve(body.size()+1);o.push_back(canUsePureA1(b)?0:1);o.insert(o.end(),body.begin(),body.end());return o;}
template<int Bits> uint8_t quantFrom8(uint8_t a) noexcept {if constexpr(Bits==4)return eightToFour(a);else return eightToFive(a);}
template<int Bits> uint8_t quantTo8(uint8_t a) noexcept {if constexpr(Bits==4)return fourToEight(a);else return fiveToEight(a);}
template<int Bits> void decodePaletteA1(std::span<const uint8_t> p,ImageBitmap& b){constexpr int M=Bits==4?16:32;if(p.empty()||p[0]>M||p.size()<size_t(1+p[0]))throw std::runtime_error("PTX alpha palette payload too small");int count=p[0];if(!count){decodeA1Bitstream(p.subspan(1),b);return;}std::vector<uint8_t> pal(count);for(int i=0;i<count;++i)pal[i]=quantTo8<Bits>(p[1+i]);int bits=paletteBitWidth(count);if(!bits){for(size_t i=0,n=size_t(b.getWidth())*b.getHeight();i<n;++i)b.getPixels()[i].a=pal[0];return;}BitReaderMSB r(p.subspan(1+count));for(size_t i=0,n=size_t(b.getWidth())*b.getHeight();i<n;++i)b.getPixels()[i].a=pal[std::min<size_t>(r.readBits(bits),pal.size()-1)];}
template<int Bits> std::vector<uint8_t> encodePaletteA1(const ImageBitmap& b){constexpr int M=Bits==4?16:32;std::array<bool,M> used{};std::vector<uint8_t> pal;for(size_t i=0,n=size_t(b.getWidth())*b.getHeight();i<n;++i){auto q=quantFrom8<Bits>(b.getPixels()[i].a);if(!used[q]){used[q]=true;pal.push_back(q);}}if((pal.size()==1&&(used[0]||used[M-1]))||(pal.size()==2&&used[0]&&used[M-1]))pal.clear();int bits=paletteBitWidth(pal.size());std::vector<uint8_t> o{uint8_t(pal.size())};o.insert(o.end(),pal.begin(),pal.end());if(pal.empty()){auto a=encodeA1Bitstream(b);o.insert(o.end(),a.begin(),a.end());return o;}if(!bits)return o;std::array<uint8_t,M> lut{};for(size_t i=0;i<pal.size();++i)lut[pal[i]]=i;BitWriterMSB w(size_t(b.getWidth())*b.getHeight()*bits);for(size_t i=0,n=size_t(b.getWidth())*b.getHeight();i<n;++i)w.writeBits(lut[quantFrom8<Bits>(b.getPixels()[i].a)],bits);auto a=std::move(w).take();o.insert(o.end(),a.begin(),a.end());return o;}
inline void decodePalette4_A1(std::span<const uint8_t> p,ImageBitmap& b){decodePaletteA1<4>(p,b);} inline void decodePalette5_A1(std::span<const uint8_t> p,ImageBitmap& b){decodePaletteA1<5>(p,b);} inline std::vector<uint8_t> encodePalette4_A1(const ImageBitmap& b){return encodePaletteA1<4>(b);} inline std::vector<uint8_t> encodePalette5_A1(const ImageBitmap& b){return encodePaletteA1<5>(b);}
inline bool isValidA8Payload(size_t s,size_t n)noexcept{return s==n;} inline bool isValidA8A1Payload(std::span<const uint8_t> p,size_t n)noexcept{return !p.empty()&&p.size()==1+(p[0]==0?((n+7)>>3):n);}
template<int Bits> bool isValidPalettePayload(std::span<const uint8_t> p,size_t n)noexcept{constexpr int M=Bits==4?16:32;if(p.empty()||p[0]>M||p.size()<size_t(1+p[0]))return false;for(int i=0;i<p[0];++i)if(p[1+i]>=M)return false;return p.size()==size_t(1+p[0])+((n*paletteBitWidth(p[0])+7)>>3);}
inline DetectResult autoDetect(uint32_t id,std::span<const uint8_t> p,size_t n,uint32_t as,uint32_t af)noexcept{if(p.empty())return{Scheme::None,true};bool a8=isValidA8Payload(p.size(),n),a8a1=isValidA8A1Payload(p,n),p4=isValidPalettePayload<4>(p,n),p5=isValidPalettePayload<5>(p,n);auto pal=[&]{if(p4&&p5){bool high=false;for(int i=0;i<p[0];++i)high|=p[1+i]>15;return DetectResult{high?Scheme::Palette5_A1:Scheme::Palette4_A1,true};}return p4?DetectResult{Scheme::Palette4_A1,true}:p5?DetectResult{Scheme::Palette5_A1,true}:DetectResult{};};if(as&&as==p.size()){auto r=pal();if(r.valid)return r;if(a8a1)return{Scheme::A8_A1,true};}if(af==0x64){auto r=pal();if(r.valid)return r;}if(id==150){auto r=pal();if(r.valid&&(af==0x64||as))return r;if(a8)return{Scheme::A8,true};if(a8a1)return{Scheme::A8_A1,true};if(r.valid)return r;}if(a8&&!p4&&!p5&&!a8a1)return{Scheme::A8,true};if(a8a1&&!a8&&!p4&&!p5)return{Scheme::A8_A1,true};auto r=pal();if(r.valid)return r;if(a8)return{Scheme::A8,true};if(a8a1)return{Scheme::A8_A1,true};return{};}
inline void decodeByScheme(Scheme s,std::span<const uint8_t> p,ImageBitmap& b){switch(s){case Scheme::None:return;case Scheme::A8:decodeA8(p,b);return;case Scheme::A8_A1:decodeA8_A1(p,b);return;case Scheme::Palette4_A1:decodePalette4_A1(p,b);return;case Scheme::Palette5_A1:decodePalette5_A1(p,b);return;}}
inline std::vector<uint8_t> encodeByScheme(Scheme s,const ImageBitmap& b){switch(s){case Scheme::None:return{};case Scheme::A8:return encodeA8(b);case Scheme::A8_A1:return encodeA8_A1(b);case Scheme::Palette4_A1:return encodePalette4_A1(b);case Scheme::Palette5_A1:return encodePalette5_A1(b);}return{};}
}
