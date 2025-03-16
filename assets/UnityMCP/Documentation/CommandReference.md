# UnityMCP Command Reference

This document provides a reference for all available commands in the UnityMCP system.

## Command Structure

Commands follow a domain-based structure:

```
domain.action
```

For example:
- `object.CreatePrimitive` - Create a primitive object
- `material.SetMaterial` - Set a material on an object

## Available Commands

###  Commands

### animation Commands

#### `animation.AddAnimationCurve`

Executes the AddAnimationCurve command

**Parameters:**

- `clipName` (string): The clipName parameter
- `objectName` (string): The objectName parameter
- `propertyPath` (string): The propertyPath parameter
- `keyframes` (List<Vector2>): The keyframes parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "animation.AddAnimationCurve",
  "parameters": {
    "clipName": "example",
    "objectName": "example",
    "propertyPath": "example",
    "keyframes": [{}]
  }
}
```

#### `animation.AddAnimationToObject`

Executes the AddAnimationToObject command

**Parameters:**

- `objectName` (string): The objectName parameter
- `clipName` (string): The clipName parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "animation.AddAnimationToObject",
  "parameters": {
    "objectName": "example",
    "clipName": "example"
  }
}
```

#### `animation.CreateAnimationClip`

Executes the CreateAnimationClip command

**Parameters:**

- `name` (string): The name parameter
- `length` (float) (default: 1): The length parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "animation.CreateAnimationClip",
  "parameters": {
    "name": "example",
    "length": 3.14
  }
}
```

#### `animation.SetAnimationParameter`

Executes the SetAnimationParameter command

**Parameters:**

- `objectName` (string): The objectName parameter
- `parameterName` (string): The parameterName parameter
- `value` (Object): The value parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "animation.SetAnimationParameter",
  "parameters": {
    "objectName": "example",
    "parameterName": "example",
    "value": {}
  }
}
```

#### `animation.StopAnimation`

Executes the StopAnimation command

**Parameters:**

- `objectName` (string): The objectName parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "animation.StopAnimation",
  "parameters": {
    "objectName": "example"
  }
}
```

### assetstore Commands

#### `assetstore.DownloadTexture`

Executes the DownloadTexture command

**Parameters:**

- `url` (string): The url parameter
- `saveName` (string) (default: null): The saveName parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "assetstore.DownloadTexture",
  "parameters": {
    "url": "example",
    "saveName": "example"
  }
}
```

#### `assetstore.GetAssetCategories`

Executes the GetAssetCategories command

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "assetstore.GetAssetCategories",
  "parameters": {}
}
```

#### `assetstore.GetProjectAssets`

Executes the GetProjectAssets command

**Parameters:**

- `filter` (string) (default: null): The filter parameter
- `limit` (int) (default: 100): The limit parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "assetstore.GetProjectAssets",
  "parameters": {
    "filter": "example",
    "limit": 42
  }
}
```

#### `assetstore.ImportLocalAsset`

Executes the ImportLocalAsset command

**Parameters:**

- `path` (string): The path parameter
- `assetType` (string) (default: null): The assetType parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "assetstore.ImportLocalAsset",
  "parameters": {
    "path": "example",
    "assetType": "example"
  }
}
```

#### `assetstore.InstantiateAsset`

Executes the InstantiateAsset command

**Parameters:**

- `assetPath` (string): The assetPath parameter
- `name` (string) (default: null): The name parameter
- `position` (Vector3?) (default: null): The position parameter
- `rotation` (Vector3?) (default: null): The rotation parameter
- `scale` (Vector3?) (default: null): The scale parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "assetstore.InstantiateAsset",
  "parameters": {
    "assetPath": "example",
    "name": "example",
    "position": {},
    "rotation": {},
    "scale": {}
  }
}
```

#### `assetstore.SearchAssets`

Executes the SearchAssets command

**Parameters:**

- `query` (string): The query parameter
- `category` (string) (default: null): The category parameter
- `limit` (int) (default: 10): The limit parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "assetstore.SearchAssets",
  "parameters": {
    "query": "example",
    "category": "example",
    "limit": 42
  }
}
```

### assistant Commands

#### `assistant.AnalyzeProject`

Executes the AnalyzeProject command

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "assistant.AnalyzeProject",
  "parameters": {}
}
```

#### `assistant.GetAnalysis`

Executes the GetAnalysis command

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "assistant.GetAnalysis",
  "parameters": {}
}
```

#### `assistant.GetCreativeSuggestion`

Executes the GetCreativeSuggestion command

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "assistant.GetCreativeSuggestion",
  "parameters": {}
}
```

#### `assistant.GetInsights`

Executes the GetInsights command

**Parameters:**

- `count` (int) (default: 5): The count parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "assistant.GetInsights",
  "parameters": {
    "count": 42
  }
}
```

### camera Commands

#### `camera.CreateCamera`

Executes the CreateCamera command

**Parameters:**

- `name` (string) (default: "New Camera"): The name parameter
- `position` (Vector3?) (default: null): The position parameter
- `rotation` (Vector3?) (default: null): The rotation parameter
- `fieldOfView` (float) (default: 60): The fieldOfView parameter
- `nearClipPlane` (float) (default: 0,3): The nearClipPlane parameter
- `farClipPlane` (float) (default: 1000): The farClipPlane parameter
- `isMain` (bool) (default: false): The isMain parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "camera.CreateCamera",
  "parameters": {
    "name": "example",
    "position": {},
    "rotation": {},
    "fieldOfView": 3.14,
    "nearClipPlane": 3.14,
    "farClipPlane": 3.14,
    "isMain": true
  }
}
```

#### `camera.CreateCameraRig`

Executes the CreateCameraRig command

**Parameters:**

- `name` (string) (default: "CameraRig"): The name parameter
- `position` (Vector3?) (default: null): The position parameter
- `rotation` (Vector3?) (default: null): The rotation parameter
- `fieldOfView` (float) (default: 60): The fieldOfView parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "camera.CreateCameraRig",
  "parameters": {
    "name": "example",
    "position": {},
    "rotation": {},
    "fieldOfView": 3.14
  }
}
```

#### `camera.CreateOrbitCamera`

Executes the CreateOrbitCamera command

**Parameters:**

- `targetName` (string): The targetName parameter
- `cameraName` (string) (default: "OrbitCamera"): The cameraName parameter
- `distance` (float) (default: 10): The distance parameter
- `height` (float) (default: 5): The height parameter
- `fieldOfView` (float) (default: 60): The fieldOfView parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "camera.CreateOrbitCamera",
  "parameters": {
    "targetName": "example",
    "cameraName": "example",
    "distance": 3.14,
    "height": 3.14,
    "fieldOfView": 3.14
  }
}
```

#### `camera.GetCameraInfo`

Executes the GetCameraInfo command

**Parameters:**

- `cameraName` (string) (default: null): The cameraName parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "camera.GetCameraInfo",
  "parameters": {
    "cameraName": "example"
  }
}
```

#### `camera.LookAt`

Executes the LookAt command

**Parameters:**

- `cameraName` (string): The cameraName parameter
- `targetName` (string): The targetName parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "camera.LookAt",
  "parameters": {
    "cameraName": "example",
    "targetName": "example"
  }
}
```

#### `camera.SetCameraAsMain`

Executes the SetCameraAsMain command

**Parameters:**

- `cameraName` (string): The cameraName parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "camera.SetCameraAsMain",
  "parameters": {
    "cameraName": "example"
  }
}
```

#### `camera.SetCameraProperties`

Executes the SetCameraProperties command

**Parameters:**

- `cameraName` (string): The cameraName parameter
- `fieldOfView` (float?) (default: null): The fieldOfView parameter
- `nearClipPlane` (float?) (default: null): The nearClipPlane parameter
- `farClipPlane` (float?) (default: null): The farClipPlane parameter
- `clearFlags` (CameraClearFlags?) (default: null): The clearFlags parameter
- `backgroundColor` (Color?) (default: null): The backgroundColor parameter
- `cullingMask` (int?) (default: null): The cullingMask parameter
- `orthographic` (bool?) (default: null): The orthographic parameter
- `orthographicSize` (float?) (default: null): The orthographicSize parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "camera.SetCameraProperties",
  "parameters": {
    "cameraName": "example",
    "fieldOfView": {},
    "nearClipPlane": {},
    "farClipPlane": {},
    "clearFlags": {},
    "backgroundColor": {},
    "cullingMask": {},
    "orthographic": {},
    "orthographicSize": {}
  }
}
```

#### `camera.TakeScreenshot`

Executes the TakeScreenshot command

**Parameters:**

- `filename` (string) (default: "Screenshot"): The filename parameter
- `width` (int) (default: 1920): The width parameter
- `height` (int) (default: 1080): The height parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "camera.TakeScreenshot",
  "parameters": {
    "filename": "example",
    "width": 42,
    "height": 42
  }
}
```

### core Commands

#### `core.ExecuteCoroutine`

Executes the ExecuteCoroutine command

**Parameters:**

- `coroutineName` (string): The coroutineName parameter
- `parameters` (Dictionary`2): The parameters parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "core.ExecuteCoroutine",
  "parameters": {
    "coroutineName": "example",
    "parameters": {}
  }
}
```

#### `core.GetCommandHandlers`

Executes the GetCommandHandlers command

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "core.GetCommandHandlers",
  "parameters": {}
}
```

#### `core.GetContext`

Executes the GetContext command

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "core.GetContext",
  "parameters": {}
}
```

#### `core.GetSubsystems`

Executes the GetSubsystems command

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "core.GetSubsystems",
  "parameters": {}
}
```

#### `core.GetSystemInfo`

Executes the GetSystemInfo command

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "core.GetSystemInfo",
  "parameters": {}
}
```

#### `core.GetVariable`

Executes the GetVariable command

**Parameters:**

- `key` (string): The key parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "core.GetVariable",
  "parameters": {
    "key": "example"
  }
}
```

#### `core.RemoveVariable`

Executes the RemoveVariable command

**Parameters:**

- `key` (string): The key parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "core.RemoveVariable",
  "parameters": {
    "key": "example"
  }
}
```

#### `core.SetVariable`

Executes the SetVariable command

**Parameters:**

- `key` (string): The key parameter
- `value` (Object): The value parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "core.SetVariable",
  "parameters": {
    "key": "example",
    "value": {}
  }
}
```

### lighting Commands

#### `lighting.CreateLight`

Executes the CreateLight command

**Parameters:**

- `type` (string) (default: "Point"): The type parameter
- `name` (string) (default: null): The name parameter
- `position` (Vector3?) (default: null): The position parameter
- `rotation` (Vector3?) (default: null): The rotation parameter
- `color` (Color?) (default: null): The color parameter
- `intensity` (float) (default: 1): The intensity parameter
- `range` (float) (default: 10): The range parameter
- `spotAngle` (float) (default: 30): The spotAngle parameter
- `shadows` (LightShadows) (default: Soft): The shadows parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "lighting.CreateLight",
  "parameters": {
    "type": "example",
    "name": "example",
    "position": {},
    "rotation": {},
    "color": {},
    "intensity": 3.14,
    "range": 3.14,
    "spotAngle": 3.14,
    "shadows": "None"
  }
}
```

#### `lighting.CreateLightingPreset`

Executes the CreateLightingPreset command

**Parameters:**

- `name` (string): The name parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "lighting.CreateLightingPreset",
  "parameters": {
    "name": "example"
  }
}
```

#### `lighting.GetLightingInfo`

Executes the GetLightingInfo command

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "lighting.GetLightingInfo",
  "parameters": {}
}
```

#### `lighting.SetAmbientLight`

Executes the SetAmbientLight command

**Parameters:**

- `mode` (string) (default: "Flat"): The mode parameter
- `color` (Color?) (default: null): The color parameter
- `skyColor` (Color?) (default: null): The skyColor parameter
- `equatorColor` (Color?) (default: null): The equatorColor parameter
- `groundColor` (Color?) (default: null): The groundColor parameter
- `intensity` (float?) (default: null): The intensity parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "lighting.SetAmbientLight",
  "parameters": {
    "mode": "example",
    "color": {},
    "skyColor": {},
    "equatorColor": {},
    "groundColor": {},
    "intensity": {}
  }
}
```

#### `lighting.SetFogSettings`

Executes the SetFogSettings command

**Parameters:**

- `enabled` (bool) (default: true): The enabled parameter
- `mode` (string) (default: "Exponential"): The mode parameter
- `color` (Color?) (default: null): The color parameter
- `density` (float?) (default: null): The density parameter
- `startDistance` (float?) (default: null): The startDistance parameter
- `endDistance` (float?) (default: null): The endDistance parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "lighting.SetFogSettings",
  "parameters": {
    "enabled": true,
    "mode": "example",
    "color": {},
    "density": {},
    "startDistance": {},
    "endDistance": {}
  }
}
```

#### `lighting.SetLightProperties`

Executes the SetLightProperties command

**Parameters:**

- `lightName` (string): The lightName parameter
- `color` (Color?) (default: null): The color parameter
- `intensity` (float?) (default: null): The intensity parameter
- `range` (float?) (default: null): The range parameter
- `spotAngle` (float?) (default: null): The spotAngle parameter
- `shadows` (LightShadows?) (default: null): The shadows parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "lighting.SetLightProperties",
  "parameters": {
    "lightName": "example",
    "color": {},
    "intensity": {},
    "range": {},
    "spotAngle": {},
    "shadows": {}
  }
}
```

#### `lighting.SetSkybox`

Executes the SetSkybox command

**Parameters:**

- `skyboxName` (string): The skyboxName parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "lighting.SetSkybox",
  "parameters": {
    "skyboxName": "example"
  }
}
```

### material Commands

#### `material.CreateMaterial`

Executes the CreateMaterial command

**Parameters:**

- `name` (string): The name parameter
- `color` (Color?) (default: null): The color parameter
- `shader` (string) (default: "Standard"): The shader parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "material.CreateMaterial",
  "parameters": {
    "name": "example",
    "color": {},
    "shader": "example"
  }
}
```

#### `material.CreatePBRMaterial`

Executes the CreatePBRMaterial command

**Parameters:**

- `name` (string): The name parameter
- `albedo` (Color?) (default: null): The albedo parameter
- `metallic` (float) (default: 0): The metallic parameter
- `smoothness` (float) (default: 0,5): The smoothness parameter
- `emission` (Color?) (default: null): The emission parameter
- `albedoTexture` (string) (default: null): The albedoTexture parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "material.CreatePBRMaterial",
  "parameters": {
    "name": "example",
    "albedo": {},
    "metallic": 3.14,
    "smoothness": 3.14,
    "emission": {},
    "albedoTexture": "example"
  }
}
```

#### `material.GetMaterialList`

Executes the GetMaterialList command

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "material.GetMaterialList",
  "parameters": {}
}
```

#### `material.GetShaderList`

Executes the GetShaderList command

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "material.GetShaderList",
  "parameters": {}
}
```

#### `material.SetMaterial`

Executes the SetMaterial command

**Parameters:**

- `objectName` (string): The objectName parameter
- `materialName` (string) (default: null): The materialName parameter
- `color` (Color?) (default: null): The color parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "material.SetMaterial",
  "parameters": {
    "objectName": "example",
    "materialName": "example",
    "color": {}
  }
}
```

#### `material.SetMaterialProperty`

Executes the SetMaterialProperty command

**Parameters:**

- `objectName` (string): The objectName parameter
- `propertyName` (string): The propertyName parameter
- `value` (Object): The value parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "material.SetMaterialProperty",
  "parameters": {
    "objectName": "example",
    "propertyName": "example",
    "value": {}
  }
}
```

### object Commands

#### `object.AddComponent`

Executes the AddComponent command

**Parameters:**

- `objectName` (string): The objectName parameter
- `componentType` (string): The componentType parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "object.AddComponent",
  "parameters": {
    "objectName": "example",
    "componentType": "example"
  }
}
```

#### `object.CreateEmpty`

Executes the CreateEmpty command

**Parameters:**

- `name` (string) (default: "New GameObject"): The name parameter
- `position` (Vector3?) (default: null): The position parameter
- `rotation` (Vector3?) (default: null): The rotation parameter
- `scale` (Vector3?) (default: null): The scale parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "object.CreateEmpty",
  "parameters": {
    "name": "example",
    "position": {},
    "rotation": {},
    "scale": {}
  }
}
```

#### `object.CreatePrimitive`

Executes the CreatePrimitive command

**Parameters:**

- `type` (string) (default: "Cube"): The type parameter
- `name` (string) (default: null): The name parameter
- `position` (Vector3?) (default: null): The position parameter
- `rotation` (Vector3?) (default: null): The rotation parameter
- `scale` (Vector3?) (default: null): The scale parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "object.CreatePrimitive",
  "parameters": {
    "type": "example",
    "name": "example",
    "position": {},
    "rotation": {},
    "scale": {}
  }
}
```

#### `object.DeleteObject`

Executes the DeleteObject command

**Parameters:**

- `name` (string): The name parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "object.DeleteObject",
  "parameters": {
    "name": "example"
  }
}
```

#### `object.DuplicateObject`

Executes the DuplicateObject command

**Parameters:**

- `name` (string): The name parameter
- `newName` (string) (default: null): The newName parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "object.DuplicateObject",
  "parameters": {
    "name": "example",
    "newName": "example"
  }
}
```

#### `object.FocusObject`

Executes the FocusObject command

**Parameters:**

- `name` (string): The name parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "object.FocusObject",
  "parameters": {
    "name": "example"
  }
}
```

#### `object.GetObjectInfo`

Executes the GetObjectInfo command

**Parameters:**

- `name` (string): The name parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "object.GetObjectInfo",
  "parameters": {
    "name": "example"
  }
}
```

#### `object.GetObjectInfoById`

Executes the GetObjectInfoById command

**Parameters:**

- `id` (int): The id parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "object.GetObjectInfoById",
  "parameters": {
    "id": 42
  }
}
```

#### `object.RemoveComponent`

Executes the RemoveComponent command

**Parameters:**

- `objectName` (string): The objectName parameter
- `componentType` (string): The componentType parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "object.RemoveComponent",
  "parameters": {
    "objectName": "example",
    "componentType": "example"
  }
}
```

#### `object.SelectObject`

Executes the SelectObject command

**Parameters:**

- `name` (string): The name parameter
- `addToSelection` (bool) (default: false): The addToSelection parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "object.SelectObject",
  "parameters": {
    "name": "example",
    "addToSelection": true
  }
}
```

#### `object.SetObjectActive`

Executes the SetObjectActive command

**Parameters:**

- `name` (string): The name parameter
- `active` (bool): The active parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "object.SetObjectActive",
  "parameters": {
    "name": "example",
    "active": true
  }
}
```

#### `object.SetObjectParent`

Executes the SetObjectParent command

**Parameters:**

- `name` (string): The name parameter
- `parentName` (string) (default: null): The parentName parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "object.SetObjectParent",
  "parameters": {
    "name": "example",
    "parentName": "example"
  }
}
```

#### `object.SetObjectTransform`

Executes the SetObjectTransform command

**Parameters:**

- `name` (string): The name parameter
- `position` (Vector3?) (default: null): The position parameter
- `rotation` (Vector3?) (default: null): The rotation parameter
- `scale` (Vector3?) (default: null): The scale parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "object.SetObjectTransform",
  "parameters": {
    "name": "example",
    "position": {},
    "rotation": {},
    "scale": {}
  }
}
```

### prefab Commands

#### `prefab.ApplyPrefabChanges`

Executes the ApplyPrefabChanges command

**Parameters:**

- `objectName` (string): The objectName parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "prefab.ApplyPrefabChanges",
  "parameters": {
    "objectName": "example"
  }
}
```

#### `prefab.CreatePrefab`

Executes the CreatePrefab command

**Parameters:**

- `objectName` (string): The objectName parameter
- `prefabPath` (string) (default: null): The prefabPath parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "prefab.CreatePrefab",
  "parameters": {
    "objectName": "example",
    "prefabPath": "example"
  }
}
```

#### `prefab.GetPrefabList`

Executes the GetPrefabList command

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "prefab.GetPrefabList",
  "parameters": {}
}
```

#### `prefab.InstantiatePrefab`

Executes the InstantiatePrefab command

**Parameters:**

- `prefabPath` (string): The prefabPath parameter
- `name` (string) (default: null): The name parameter
- `position` (Vector3?) (default: null): The position parameter
- `rotation` (Vector3?) (default: null): The rotation parameter
- `scale` (Vector3?) (default: null): The scale parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "prefab.InstantiatePrefab",
  "parameters": {
    "prefabPath": "example",
    "name": "example",
    "position": {},
    "rotation": {},
    "scale": {}
  }
}
```

#### `prefab.RevertPrefabChanges`

Executes the RevertPrefabChanges command

**Parameters:**

- `objectName` (string): The objectName parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "prefab.RevertPrefabChanges",
  "parameters": {
    "objectName": "example"
  }
}
```

### scene Commands

#### `scene.CreateNewScene`

Executes the CreateNewScene command

**Parameters:**

- `sceneName` (string) (default: "New Scene"): The sceneName parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "scene.CreateNewScene",
  "parameters": {
    "sceneName": "example"
  }
}
```

#### `scene.FindObjectsOfType`

Executes the FindObjectsOfType command

**Parameters:**

- `typeName` (string): The typeName parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "scene.FindObjectsOfType",
  "parameters": {
    "typeName": "example"
  }
}
```

#### `scene.GetAllScenes`

Executes the GetAllScenes command

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "scene.GetAllScenes",
  "parameters": {}
}
```

#### `scene.GetSceneHierarchy`

Executes the GetSceneHierarchy command

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "scene.GetSceneHierarchy",
  "parameters": {}
}
```

#### `scene.GetSceneInfo`

Executes the GetSceneInfo command

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "scene.GetSceneInfo",
  "parameters": {}
}
```

#### `scene.LoadScene`

Executes the LoadScene command

**Parameters:**

- `sceneName` (string): The sceneName parameter
- `mode` (LoadSceneMode) (default: Single): The mode parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "scene.LoadScene",
  "parameters": {
    "sceneName": "example",
    "mode": "Single"
  }
}
```

#### `scene.SaveScene`

Executes the SaveScene command

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "scene.SaveScene",
  "parameters": {}
}
```

#### `scene.SetActiveScene`

Executes the SetActiveScene command

**Parameters:**

- `sceneName` (string): The sceneName parameter

**Returns:**

A JSON object containing the result of the command

**Example:**

```json
{
  "type": "scene.SetActiveScene",
  "parameters": {
    "sceneName": "example"
  }
}
```

