# Baton Pass — Save System Git-Like

## Resumo

Sistema de save para Wayward Son com duas camadas:
- **Runtime (jogador):** auto-save (120s, F5 manual) + load por perfil
- **Dev/Debug:** commits snapshots, workspace com delta, checkout/revert via janela Editor
- **Editor (Edit mode):** visualizacao de snapshots salvos em disco, Apply de dados em objetos `ISaveable` sem Play Mode

## Arquivos

### Runtime (`Assets/Scripts/SaveSystem/`)

| Arquivo | Descricao |
|---------|-----------|
| `ISaveable.cs` | Interface `SaveID`, `CollectData()`, `ApplyData()` |
| `GameSaveData.cs` | Container central. `components` dictionary + `componentsJson` string para serializar via `MiniJSON`. Metodos `BeforeSerialize()`/`AfterDeserialize()` |
| `SaveSnapshot.cs` | Commit: `id (GUID)`, `timestamp`, `description`, `parentID`, `GameSaveData` |
| `SaveDelta.cs` | `SaveDelta` + `FieldChange` com `componentID`, `fieldPath`, `oldValue`, `newValue`, `ChangeType` |
| `SaveProfile.cs` | Perfil: `name`, `createdAt`, `commitIDs[]`, `workspaceID` |
| `SaveManager.cs` | Singleton: profiles, commits, checkout/stash, revert, delta, debug JSON. Inclui `MiniJSON` interno (Serialize + Deserialize). Expoe `ApplySnapshot(snapshotID)` |
| `SaveCommands.cs` | No player: auto-save timer + F5 manual save |
| `SaveDebugUI.cs` | HUD `OnGUI()` no Game View: botoes **Save (Commit)** e **Load (Apply)**, mais lista de snapshots com botao de **Checkout** (verde = ativo, clicar re-aplica se ja for o atual). Toggle visibilidade com F8 |

### Editor (`Assets/Scripts/SaveSystem/Editor/`)

| Arquivo | Descricao |
|---------|-----------|
| `SaveEditorWindow.cs` | Janela (Window > Wayward Son > Save System). Dois modos: **Full** (Play + SaveManager) com commits, workspace delta, checkout/revert; **Browse** (Editor) leitura de disco com Apply. Botoes "Apply" por commit + geral |
| `SaveDebugger.cs` | Cria `Assets/Debug/SaveDeltas/`, garante SaveManager na cena. Menu items: "Add SaveDebugUI to Scene", "Open Save Browser (Editor)" |
| `EditorSaveUtils.cs` | Utilitario standalone para leitura de saves do disco em Edit mode: lista perfis, carrega snapshots, le JSON raw, aplica dados em objetos `ISaveable` da cena (com Undo/SetDirty) |

### Componentes que implementam `ISaveable`

| Componente | SaveID | Dados salvos |
|------------|--------|-------------|
| `PlayerController` | `"Player"` | position, rotationY, isAiming, speed, autoAimRange |
| `PlayerHealth` | `"Player"` | health, maxHealth |
| `FlashlightController` | `"Flashlight"` | isOn, currentBattery, maxBattery |
| `WeaponHandler` | `"Weapon"` | activeWeaponName, currentAmmo |
| `Inventory` | `"Inventory"` | showInventory, items (lista com nome/pos), equippedItemName |

### SetupPrototypeScene Alterado

- Cria `SaveManager` (DontDestroyOnLoad) + `SaveCommands` no player

## Fluxo de Uso

### Jogador
- Auto-save a cada 120s (configuravel no `SaveCommands`)
- F5 para save manual
- Dados salvos em `<persistentDataPath>/WaywardSon/Saves/<ProfileName>/`

### Desenvolvedor (Play Mode)
1. Abrir `Wayward Son > Save System`
2. Criar/Selecionar perfil
3. Clicar "Commit Snapshot" para gerar snapshot + JSON debug em `Assets/Debug/SaveDeltas/`. O snapshot e automaticamente aplicado (checkout) aos objetos `ISaveable` da cena, deixando o workspace limpo (delta zero).
4. Ver workspace com mudancas em relacao ao ultimo commit
5. "Revert" descarta mudancas do workspace
6. "Checkout" em commit antigo → estado atual stashado → testa estado antigo
7. "Return to Workspace" volta ao estado antes do checkout
8. "Apply" aplica dados de um snapshot nos objetos `ISaveable` da cena (sem stash)

### Desenvolvedor (Edit Mode)
1. Abrir `Wayward Son > Save System` (ou `Wayward Son > Open Save Browser`)
2. Selecionar um perfil existente
3. Navegar pelos snapshots salvos em disco
4. Clicar em um snapshot para ver o JSON raw do arquivo
5. Clicar "Apply" para aplicar os dados do snapshot nos objetos `ISaveable` da cena atual (com Undo)

### Debug HUD
1. `Wayward Son > Add SaveDebugUI to Scene`
2. Durante Play, usar os botoes no canto superior esquerdo do Game View:
   - **Save (Commit):** cria um novo snapshot e faz checkout automatico
   - **Load (Apply):** aplica o ultimo commit (Revert)
   - **Lista de Snapshots:** abaixo dos botoes, lista todos os commits. Cada linha e um botao — clicar faz **Checkout** para aquele snapshot. Se ja for o atual (destacado em verde), clicar **re-aplica** os dados nos objetos `ISaveable` para garantir sincronia
3. F8 para toggle visibilidade

## Armazenamento

```
<persistentDataPath>/WaywardSon/Saves/<ProfileName>/
├── profile.json
├── snapshots/
│   ├── <guid>.json      # cada snapshot (commit) — metadata via JsonUtility + componentsJson via MiniJSON
│   └── ...
```

Em desenvolvimento, JSONs de delta em:
```
Assets/Debug/SaveDeltas/
├── commit_<guid>.json
├── profiles.json
└── ...
```

## Correcao: Serializacao do Dictionary

`JsonUtility` nao serializa `Dictionary<string, object>`. O schema antigo perdia os dados de components ao salvar em disco. Corrigido com:

1. `GameSaveData.components` marcado como `[NonSerialized]` e mantido em memoria
2. Novo campo `GameSaveData.componentsJson` — string serializada por `JsonUtility` que armazena o resultado de `MiniJSON.Serialize(components)`
3. `BeforeSerialize()`: `componentsJson = MiniJSON.Serialize(components)`
4. `AfterDeserialize()`: faz parse de `componentsJson` de volta para `Dictionary<string, object>`
5. `MiniJSON.Deserialize()` adicionado ao `SaveManager.cs` — parser JSON recursivo completo

Snapshots antigos (sem `componentsJson`) carregam com dictionary vazio e podem ser recriados.

## Pontos de Atencao

1. **Nova cena:** Ao criar nova cena, rodar `Wayward Son > Ensure SaveManager in Scene`.
2. **Para adicionar novos componentes salvareis:** Implementar `ISaveable` e registrar automaticamente via `DiscoverSaveables()`.
3. **ScriptableObjects:** Referencias (WeaponData, ItemDefinition) sao resolvidas por nome. Manter nomes unicos entre assets.
4. **Apply em Edit mode:** Usa `Resources.FindObjectsOfTypeAll<MonoBehaviour>()` filtrado por `scene.IsValid()`. Requer que a cena tenha objetos com `ISaveable`. Usa `Undo.RecordObject` para permitir Ctrl+Z.
5. **MiniJSON:** Mantido como classe `internal static` dentro de `SaveManager.cs`. Tanto `Serialize` quanto `Deserialize` assumem input bem-formado (não ha validacao).
6. **Auto-checkout apos commit:** `CreateSnapshot()` agora chama `ApplySnapshotData(snapshot.data)` ao final. Isso garante que o workspace fique sincronizado com o commit recem-criado (delta zero) e que os objetos `ISaveable` reflitam exatamente o estado salvo.
7. **Checkout no Debug HUD:** O `SaveDebugUI` exibe a lista de snapshots com botoes de checkout. Se o snapshot ja esta ativo (workspaceID), clicar nele re-aplica os dados (ApplySnapshot) para forcar sincronia dos `ISaveable`.
