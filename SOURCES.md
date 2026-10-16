# 音标依据与数据说明

数据按国际语音协会（International Phonetic Association）的官方主图人工核对，并重新整理为适于查询和复制的 UTF-8 JSON。未嵌入网页、图表图片、在线字体或语音服务。

- 官方图表和修订说明： https://www.internationalphoneticassociation.org/node/1061
- 官方完整主图： https://www.internationalphoneticassociation.org/node/55
- 官方图表存档： https://www.internationalphoneticassociation.org/content/ipa-chart-archive
- 官方交互图： https://www.internationalphoneticassociation.org/IPAcharts/IPA_charts_EI/IPA_charts_EI.html

协会页面说明，2025年起按年重发图表并更新版权年份，主图注明最近的符号修订版本。这里参考主图的现行主要分类；图表年份不代表每年新增字母。

归属：International Phonetic Alphabet / IPA Chart，International Phonetic Association，官方当前主图采用 CC BY-SA 4.0： https://creativecommons.org/licenses/by-sa/4.0/ 。本项目整理的 IPA 数据 `src/Data/catalog.json` 亦按 CC BY-SA 4.0 提供；新增中文释义、搜索关键词、复制文本和占位显示方式为本项目整理。界面布局并非官方图表的复刻。

英语示例词和分类用于教学检索，不能替代特定口音的语音分析。`ts/dz/tr/dr` 单独标作教学辅音组合。`ɡ` 使用 U+0261；长音用 U+02D0；鼻化 U+0303；送气 U+02B0；挤喉标记 U+02BC，不混用 ASCII 冒号、引号或图片。

组合附加符号以独立码点保存，显示的 U+25CC 虚线圆圈与复制文本分离；连音弧同样不复制圆圈。不对复制文本执行会改变码点序列的自动替换或 Unicode 归一化。

发布方式依据微软官方文档： https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/ 。SelfContained 内嵌对应 Windows 运行时；IncludeNativeLibrariesForSelfExtract 将本机运行库也纳入单一 EXE。
