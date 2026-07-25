module;
#include <string>
#include <string_view>
export module tool.shell.shell_processor;
import tool.shell.plugin_base;

export struct BatchResult {
    int successCount = 0;
    int failCount = 0;
    double elapsed = 0.0;
};

export class ShellProcessor {
public:
    static bool executeFunction(const FunctionConfig& cfg, const PluginParamMap& params, double& elapsed);
    static BatchResult executeBatch(const FunctionConfig& cfg, std::string_view inputFolder, std::string_view outputFolder, const PluginParamMap& extraParams);
};
