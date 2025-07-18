# UABS

👉中文版在[这里](./README_zh.md)

UABS (Unity Asset Bundle Seeker) is a modding tool for the Unity Game Engine. Tools like [UABEA](https://github.com/nesrak1/UABEA), [AssetStudio](https://github.com/Perfare/AssetStudio),
[AssetRipper](https://github.com/AssetRipper/AssetRipper) etc. are popular modding tools, each with their own strength-and the same goes for UABS. The goal of this project is to make searching game assets faster and to streamline the process of mod-making. I am also a mod maker! You can watch my videos on [Bilibili](https://space.bilibili.com/31525265) (Mandarin). I post original modding tutorials from time to time.

<p align="center">
    <img src="./readme_img/logo.png" width="300"/>
</p>

## Tool Features
1. Built-in folder-style browsing.
2. Manageable cache system within the software.
3. Puts more emphasis on file searching compared to other tools.

## Features
This tool is still under development, so not all features are complete yet.

1. Display image textures and play audio files (similar to AssetStudio) – currently supports images only
2. Export image textures, audio files, etc. (similar to AssetStudio) – currently supports images only
3. Modify Texture2D files (similar to UABEA) – not implemented yet
4. Find asset bundle dependencies and provide quick navigation ✅
5. Tag and annotate asset files ✅
6. Quickly search for files within asset bundles ✅
7. Integrate and automate small tools I’ve previously released on Bilibili, as much as possible

## Used Libraries
[UABEA](https://github.com/nesrak1/UABEA) (MIT) - AssetsTools.Net & AssetsTools.Net.Extra as well as dump file to UABEA Json format. Many UI designs were referenced from UABEA too.

[AddressablesTools](https://github.com/nesrak1/AddressablesTools/releases) (MIT) - Necessary for modding.

[BCnEncoder.NET](https://github.com/Nominom/BCnEncoder.NET) (MIT) - For image decoding.

[Newtonsoft.Json-for-Unity](https://github.com/applejag/Newtonsoft.Json-for-Unity) (MIT) - Best Json library for Unity ever existed.

[astc-encoder](https://github.com/ARM-software/astc-encoder) (Apache-2.0) - ASTC image decompression. Common image format for Android, iOS games.

[Noto Sans Simplified Chinese](https://fonts.google.com/noto/specimen/Noto+Sans+SC/license?lang=zh_Hans) (SIL Open Font License, Version 1.1)  - Chinese font.

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

## Special Notice
1. I made the logo. The font used is [HE'S DEAD Jim](https://www.dafont.com/hes-dead-jim.font). BTW I like Star Trek series.
2. If you plan to redistribute this tool, please credit the author (me) or include a link to this repository.
Thank you very much for your support!

