# UABS
UABS (Unity Asset Bundle Seeker或者‘Unity资源包查找工具’) 是一款专门应用于Unity引擎的辅助模组工具。目前市面上已经热门的工具像是有
[UABEA](https://github.com/nesrak1/UABEA), [AssetStudio](https://github.com/Perfare/AssetStudio),
[AssetRipper](https://github.com/AssetRipper/AssetRipper) 等等。它们都有各自的特点，该工具也是如此。
在这个项目里，我的目的是要让文件索取变得更方便快捷以及缩短模组的制作流程。其次我个人也是模组制作者，你可以在[B站](https://space.bilibili.com/31525265)找到我。我会不定期发布一些制作模组的原创教程。


<p align="center">
    <img src="./readme_img/logo.png" width="300"/>
</p>

## 工具特点
1. 内置文件夹式浏览。
2. 软件内的可管理缓存系统。
3. 比其他工具更注重文件的查询。

## 功能
该工具目前还处在建设阶段，所以功能尚不完全。

1. 显示图像材质，听取音频文件等（效果同AssetStudio）- 目前可以显示图像材质
2. 导出图像材质，音频文件等（效果同AssetStudio）- 目前可以导出图像材质
3. 改写材质文件（效果同UABEA）- 还没做
4. 寻找资源包的依赖项并快速引导 ✅
5. 标记，备注资源文件 ✅
6. 快速寻找资源包中的文件 ✅
7. 尽可能把我在B站发布过的小工具加进来并实现自动化

## 使用库
[UABEA](https://github.com/nesrak1/UABEA) (MIT) - AssetsTools.Net & AssetsTools.Net.Extra 以及导出文件Json化。很多UI也都是参考的UABEA。

[AddressablesTools](https://github.com/nesrak1/AddressablesTools/releases) (MIT) - 做模组必要工具。

[BCnEncoder.NET](https://github.com/Nominom/BCnEncoder.NET) (MIT) - 处理部分棘手的图像格式。

[Newtonsoft.Json-for-Unity](https://github.com/applejag/Newtonsoft.Json-for-Unity) (MIT) - 最好的Json代码库。

[Noto Sans Simplified Chinese](https://fonts.google.com/noto/specimen/Noto+Sans+SC/license?lang=zh_Hans) (SIL Open Font License, Version 1.1)  - 中文字体。


## 安装
独立软件：
下载软件请移步[Releases页面](https://github.com/Kolyn090/UABS/releases)。
下载zip文件后使用解压工具进行解压。打开UABS可执行文件即可运行。

开发环境：
下载Unity，推荐版本2021.3.33f1。Clone或者Fork该repo并在Unity中打开里面的名称为UABS的文件夹，接着去Scenes文件夹打开UABS就可以了。十分推荐切换成2D视角+不适用Skybox。


## 问题
该工具使用Unity引擎建造。我知道这会带来很多问题不过也有些显而易见的好处。

---

问题一：Unity有很多个版本，怎么知道该工具可以适用于其他Unity版本的游戏？

答：我认为游戏的版本不是很大的问题，该工具很多地方都是参考了UABEA，如果UABEA都没有问题那理论上来说该工具也没有问题。如果你遇到了与版本相关的问题，可以发issue我可以帮忙看看。
（该工具的建造版本为Unity 2021.3.33f1，你如果下载的话请确保是用的这个Unity版本）

---

问题二：你会把这个工具做成一个独立软件吗？

答：独立软件可以在[Releases](https://github.com/Kolyn090/UABS/releases)下载。不过要注意部分功能会需要用到Unity开发环境。但如果你只是将软件用于查询目的则只需要独立软件就可以。

---

问题三：我看见你发的内容都是讲2D的，有3D的教程吗？

答：不好意思，3D个人没什么研究，只能说有空的话会看看。如果你有想做的游戏可以在issue发问。

---

问题四：有些文件打开要花很长时间，这是为什么？

答：如果你打开的文件尺寸很大的话，这是正常现象。目前UABS读取资源包的方式优化不足，打开大型文件是很吃力的。如果文件小于10MB但是打开时间超过好几分钟的话可以发issue提问。

## 特注
1. 工具的logo由本人绘制，使用字体是[HE'S DEAD Jim](https://www.dafont.com/hes-dead-jim.font)。顺便一提我很爱看星际迷航系列。
2. 如果你要二次发布该工具的话请标注一下作者（我）- Kolyn090，或者附上这篇repo的链接。非常感谢你的支持！
