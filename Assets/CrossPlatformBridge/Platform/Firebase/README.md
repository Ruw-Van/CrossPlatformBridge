# Firebase — CrossPlatformBridge

## Overview
Google Firebase integration using Authentication and Firestore.

**Supported services:** Account, CloudStorage

## Prerequisites
- CrossPlatformBridge package
- UniTask
- [Firebase Unity SDK](https://firebase.google.com/docs/unity/setup) (Firebase.Auth, Firebase.Firestore, Firebase.App DLLs)
- Firebase project configured

## Installation
1. Download Firebase Unity SDK and import `FirebaseAuth.unitypackage` and `FirebaseFirestore.unitypackage`.
2. Add Scripting Define Symbol: `USE_CROSSPLATFORMBRIDGE_FIREBASE`
3. Place `google-services.json` (Android) or `GoogleService-Info.plist` (iOS) in `Assets/StreamingAssets/`.

## Platform Configuration
Firebase dependencies are checked automatically via `FirebaseApp.CheckAndFixDependenciesAsync()` during initialization.

---

## Services

### Account
Firebase Authentication — anonymous sign-in (UID-based account management).

#### Handler Registration
```csharp
var account = new CrossPlatformBridge.Platform.Firebase.Account.FirebaseAccountHandler();
bool ok = await account.InitializeAsync(); // Anonymous sign-in
string uid = account.AccountId;
```

#### Test Operations (ServiceTestUI)
- Initialize (anonymous sign-in)
- Shutdown

---

### CloudStorage
Firebase Firestore cloud save (`users/{uid}/data/{key}` document structure).

#### Configuration
Call `FirebaseAccountHandler.InitializeAsync()` before using cloud storage to obtain a valid UID.

#### Handler Registration
```csharp
// Initialize account first
var account = new FirebaseAccountHandler();
await account.InitializeAsync();
// Then use cloud storage
var storage = new CrossPlatformBridge.Platform.Firebase.CloudStorage.FirebaseCloudStorageHandler();
await storage.SaveData("firebase_save", "hello");
string value = await storage.LoadData("firebase_save");
```

#### Test Operations (ServiceTestUI)
- Save Data / Load Data / Load All Data / Delete Data
