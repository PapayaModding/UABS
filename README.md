# UABS

👉中文版在[这里](./README_zh.md)

UABS (Unity Asset Bundle Seeker) is a modding tool for the Unity Game Engine. Tools like [UABEA](https://github.com/nesrak1/UABEA), [AssetStudio](https://github.com/Perfare/AssetStudio),
[AssetRipper](https://github.com/AssetRipper/AssetRipper) etc. are popular modding tools, each with their own strength-and the same goes for UABS. The goal of this project is to make searching game assets faster and to streamline the process of mod-making. I am also a mod maker! You can watch my videos on [Bilibili](https://space.bilibili.com/31525265) (Mandarin). I post original modding tutorials from time to time.

<p align="center">
    <img src="./readme_img/logo.png" width="300"/>
</p>

**There is wiki page for this project. Click [here](https://github.com/Kolyn090/UABS/wiki) to see.**

## Tool Features
1. Built-in folder-style browsing.
2. Manageable package system within the software.
3. Puts more emphasis on file searching compared to other tools.

## Features
This tool is still under development, so not all features are complete yet.

1. Display image textures and play audio files (similar to AssetStudio) – currently supports images only
2. Export image textures, audio files, etc. (similar to AssetStudio) – currently supports images only
3. Find asset bundle dependencies and provide quick navigation ✅
4. Tag and annotate asset files ✅
5. Quickly search for files within asset bundles ✅


## Used Libraries
| Library | License |
| --- | --- |
| [AssetsTools.NET](https://github.com/nesrak1/AssetsTools.NET) | MIT |
| [AssetsTools.NET.Texture](https://github.com/nesrak1/AssetsTools.NET/tree/main/AssetsTools.NET.Texture) | MIT |
| [AddressablesTools](https://github.com/nesrak1/AddressablesTools/releases) | MIT |
| [BCnEncoder.NET](https://github.com/Nominom/BCnEncoder.NET) | MIT |
| &nbsp;&nbsp;&nbsp;&nbsp; - [CommunityToolkit.HighPerformance](https://www.nuget.org/packages/CommunityToolkit.HighPerformance/) | MIT |
| &nbsp;&nbsp;&nbsp;&nbsp; - [ImageSharp](https://github.com/SixLabors/ImageSharp?tab=readme-ov-file) | [Six Labors Split License](https://github.com/SixLabors/ImageSharp?tab=License-1-ov-file) |
| &nbsp;&nbsp;&nbsp;&nbsp; - [System.Runtime.CompilerServices.Unsafe](https://www.nuget.org/packages/system.runtime.compilerservices.unsafe/) | MIT |
| [StandaloneFileBrowser](https://github.com/gkngkc/UnityStandaloneFileBrowser) | MIT |
| [Newtonsoft.Json-for-Unity](https://github.com/applejag/Newtonsoft.Json-for-Unity) | MIT |
| [astc-encoder](https://github.com/ARM-software/astc-encoder) | Apache-2.0 |
| [Noto Sans Simplified Chinese](https://fonts.google.com/noto/specimen/Noto+Sans+SC/license?lang=zh_Hans) | SIL Open Font License, Version 1.1 |
| [UABEA](https://github.com/nesrak1/UABEA) | MIT |
| [UABEANext](https://github.com/nesrak1/UABEANext) | MIT? |
| &nbsp;&nbsp;&nbsp;&nbsp; - [AssetsTools.NET.MonoCecil](https://www.nuget.org/packages/AssetsTools.NET.MonoCecil/1.0.0-preview2) | MIT |
| &nbsp;&nbsp;&nbsp;&nbsp; - [AssetsTools.NET.Cpp2IL](https://www.nuget.org/packages/AssetsTools.NET.Cpp2IL/) | MIT |
| &nbsp;&nbsp;&nbsp;&nbsp; - [AssetRipper.Primitives](https://www.nuget.org/packages/AssetRipper.Primitives) | MIT |
| &nbsp;&nbsp;&nbsp;&nbsp; - [Mono.Cecil](https://www.nuget.org/packages/Mono.Cecil/) | MIT |
| &nbsp;&nbsp;&nbsp;&nbsp; - [Microsoft.Bcl.HashCode](https://www.nuget.org/packages/Microsoft.Bcl.HashCode/) | MIT |
| &nbsp;&nbsp;&nbsp;&nbsp; - [LibCpp2IL](https://www.nuget.org/packages/Samboy063.LibCpp2IL/2022.1.0-pre-release.19) | MIT |
| &nbsp;&nbsp;&nbsp;&nbsp; - [WasmDisassembler](https://www.nuget.org/packages/Samboy063.WasmDisassembler/2022.1.0-pre-release.19) | MIT |


## Installation
Standalone software:
To install software, go to [Releases](https://github.com/Kolyn090/UABS/releases). Download the zip file and unzip it. Open UABS executable file to run the software.

Developmental environment:
Download Unity (recommend 2021.3.33f1). Clone or Fork this repo and open the folder 'UABS' in Unity. After that (in Unity) from the folder 'Scenes' open scene 'UABS'. I highly recommend you to use 2D view + do not apply Skybox.

## Issues
This tool is built with Unity. I understand that it can bring problems but there are obvious benefits.

---

Q1: Unity has many different versions. How can I know if this tool works with other Unity game versions?

A: I don’t think the Unity version is a major issue. This tool is heavily based on UABEA—if UABEA works for your game, then in theory, this tool should work too. If you run into version-related issues, feel free to open an issue and I’ll take a look.

---

Q2: Will you release this as a standalone app?

A: Yes, a standalone version is available on the [Releases page](https://github.com/Kolyn090/UABS/releases).
However, please note that some features require the Unity development environment.
If you're only using the tool for asset browsing, the standalone app is sufficient.

---

Q3: I noticed your tutorials are all about 2D. Will you make any 3D tutorials?

A: Sorry, I don’t have much experience with 3D. I might look into it when I have time.
If there's a specific game you're interested in, feel free to ask via an issue.

---

Q4: Some files take a long time to open. Why is that?

A: That’s expected if the file is large.
Currently, UABS is not fully optimized for reading large asset bundles, so performance may be poor in those cases.
If the file is under 10MB but still takes several minutes to open, please file an issue and I’ll investigate.

## Special Note
1. I made the logo. The font used is [HE'S DEAD Jim](https://www.dafont.com/hes-dead-jim.font). BTW I like Star Trek series.
2. If you plan to redistribute this tool, please credit the author (me) or include a link to this repository.
Thank you very much for your support!

