module;
#include <cstdint>
#include <optional>
#include <string>
#include <vector>
export module tool.popcap.wwise.bnk.core;

export namespace WwiseSoundBank {

struct BankHeader {
    uint32_t version{};
    uint32_t id{};
    uint32_t language{};
    std::string headExpand;
};

struct InitEntry {
    uint32_t id{};
    std::string name;
};

struct StageGroupData {
    std::string defaultTransitionTime;
    std::vector<std::string> customTransition;
};

struct StageGroup {
    uint32_t id{};
    StageGroupData data;
};

struct SwitchGroupData {
    uint32_t parameter{};
    uint8_t parameterCategory{};
    std::vector<std::string> point;
};

struct SwitchGroup {
    uint32_t id{};
    SwitchGroupData data;
};

struct GameParameter {
    uint32_t id{};
    std::string data;
};

struct GameSync {
    std::string volumeThreshold;
    std::string maxVoiceInstances;
    uint16_t unknownType1{};
    std::vector<StageGroup> stageGroup;
    std::vector<SwitchGroup> switchGroup;
    std::vector<GameParameter> gameParameter;
    uint32_t unknownType2{};
};

struct EnvironmentVolume {
    std::string volumeValue;
    std::vector<std::string> volumePoint;
};

struct EnvironmentFilter {
    std::string value;
    std::vector<std::string> point;
};

struct EnvironmentItem {
    EnvironmentVolume volume;
    EnvironmentFilter lowPassFilter;
    std::optional<EnvironmentFilter> highPassFilter;
};

struct Environments {
    EnvironmentItem obstruction;
    EnvironmentItem occlusion;
};

struct HircObject {
    uint8_t objType{};
    uint32_t id{};
    std::string data;
};

struct ReferenceEntry {
    uint32_t id{};
    std::string name;
};

struct Reference {
    std::vector<ReferenceEntry> entries;
    uint32_t unknownType{};
};

struct PlatformSetting {
    std::string platform;
};

struct DidxEntry {
    uint32_t id{};
    uint32_t offset{};
    uint32_t size{};
};

struct Bank {
    BankHeader header;
    std::vector<uint32_t> embeddedMedia;
    std::optional<std::vector<InitEntry>> initialization;
    std::optional<GameSync> gameSync;
    std::optional<Environments> environments;
    std::vector<HircObject> hierarchy;
    std::optional<Reference> reference;
    std::optional<PlatformSetting> platform;
    std::vector<DidxEntry> dataIndex;
    std::optional<uint64_t> dataChunkOffset;
};

} 