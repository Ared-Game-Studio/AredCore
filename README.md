# Ared Core Package

AredCore is a Unity package that provides a set of reusable core modules and embedded third‑party utilities commonly used across Ared Game Studio projects.


## Modules

### 1) AutoSheetData
> **AutoSheetData** helps you fetch and manage structured data (google sheet and excel) and make it usable inside Unity.



### 2) AudioManager
> **AudioManager** provides a simple way to play and control audio in your game.



### 3) LocalNotification
> **LocalNotification** enables scheduling and managing local notifications on Android and IOS.



## External Packages & Libraries

AredCore may include or depend on the following external utilities and SDKs (embedded or integrated as dependencies):

### External Dependency Manager for Unity (EDM4U)
> https://github.com/googlesamples/unity-jar-resolver

### Facebook SDK
> https://github.com/facebook/facebook-sdk-for-unity

### GameAnalytics SDK

> https://github.com/GameAnalytics/GA-SDK-UNITY

### Vibration 
> https://github.com/BenoitFreslon/Vibration

### NaughtyAttributes
> https://github.com/dbrizov/NaughtyAttributes



## Installation



### Option A: Install via Git URL (recommended)

1. Open **Unity** → **Window** → **Package Manager**
2. Click the **+** button (top left)
3. Select **Add package from git URL...**
4. Paste:

```text
https://github.com/Ared-Game-Studio/AredCore.git
```

If you need a specific version:

```text
https://github.com/Ared-Game-Studio/AredCore.git#v1.0.0
```




### Option B: Add to `Packages/manifest.json`

In your Unity project, edit `Packages/manifest.json` and add:

```json
{
  "dependencies": {
    "co.aredstudio.core": "https://github.com/Ared-Game-Studio/AredCore.git"
  }
}
```


### Option C: Git clone

1. Clone the repository:
```bash
git clone https://github.com/Ared-Game-Studio/AredCore.git
```

2. Embed into your project** by copying into `Packages/` folder.

