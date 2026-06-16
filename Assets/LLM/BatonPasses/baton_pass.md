# Baton Pass — Save System Git-Like

## Resumo

Sistema de save para Wayward Son com duas camadas:
- **Runtime (jogador):** auto-save (120s, F5 manual) + load por perfil
- **Dev/Debug:** commits snapshots, workspace com delta, checkout/revert via janela Editor

## Arquivos Criados

### Runtime (`Assets/Scripts/SaveSystem/`)

| Arquivo | Descricao |
|---------|-----------|
| `ISaveable.cs` | Interface `SaveID`, `CollectData()`, `ApplyData()` |
| `GameSaveData.cs` | Container central: `sceneName`, `timestamp`, `Dictionary<string, object> components` |
| `SaveSnapshot.cs` | Commit: `id (GUID)`, `timestamp`, `description`, `parentID`, `GameSaveData` |
| `SaveDelta.cs` | `SaveDelta` + `FieldChange` com `componentID`, `fieldPath`, `oldValue`, `newValue`, `ChangeType` |
| `SaveProfile.cs` | Perfil: `name`, `createdAt`, `commitIDs[]`, `workspaceID` |
| `SaveManager.cs` | Singleton: profiles, commits, checkout/stash, revert, delta, debug JSON |
| `SaveCommands.cs` | No player: auto-save timer + F5 manual save |

### Editor (`Assets/Scripts/SaveSystem/Editor/`)

| Arquivo | Descricao |
|---------|-----------|
| `SaveEditorWindow.cs` | Janela (Window > Wayward Son > Save System): profiles, commits, workspace delta, checkout/revert |
| `SaveDebugger.cs` | Cria `Assets/Debug/SaveDeltas/`, garante SaveManager na cena |

### Componentes Modificados (implementam `ISaveable`)

| Componente | SaveID | Dados salvos |
|------------|--------|-------------|
| `PlayerController` | `"Player"` | position, rotationY, isAiming, speed, autoAimRange |
| `PlayerHealth` | `"Player"` | health, maxHealth |
| `FlashlightController` | `"Flashlight"` | isOn, currentBattery, maxBattery |
| `WeaponHandler` | `"Weapon"` | activeWeaponName, currentAmmo |
| `Inventory` | `"Inventory"` | showInventory, items (lista com nome/pos), equippedItemName |

## Fluxo de Uso

### Jogador
- Auto-save a cada 120s (configuravel no `SaveCommands`)
- F5 para save manual
- Dados salvos em `<persistentDataPath>/WaywardSon/Saves/<ProfileName>/`

### Desenvolvedor
1. Abrir `Window > Wayward Son > Save System`
2. Criar/Selecionar perfil
3. Clicar "Commit Snapshot" para gerar snapshot + JSON debug em `Assets/Debug/SaveDeltas/`
4. Ver workspace com mudancas em relacao ao ultimo commit
5. "Revert" descarta mudancas do workspace
6. "Checkout" em commit antigo → estado atual stashado → testa estado antigo
7. "Return to Workspace" volta ao estado antes do checkout

## Armazenamento

```
<persistentDataPath>/WaywardSon/Saves/<ProfileName>/
├── profile.json
├── snapshots/
│   ├── <guid>.json      # cada snapshot (commit)
│   └── ...
```

Em desenvolvimento, JSONs de delta em:
```
Assets/Debug/SaveDeltas/
├── commit_<guid>.json
├── profiles.json
└── ...
```

## Pontos de Atencao

1. **Dictionary serialization:** `JsonUtility` nao serializa `Dictionary<string, object>`. O sistema usa `MiniJSON` interno para debug JSON. Para JSON preview no editor, campos complexos aparecem limitados.
2. **Nova cena:** Ao criar nova cena, rodar `Wayward Son > Ensure SaveManager in Scene`.
3. **Para adicionar novos componentes salvaveis:** Implementar `ISaveable` e registrar automaticamente via `DiscoverSaveables()`.
4. **ScriptableObjects:** Referencias (WeaponData, ItemDefinition) sao resolvidas por nome. Manter nomes unicos entre assets.
