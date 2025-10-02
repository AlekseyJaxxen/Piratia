# 🐛 Руководство по отладке анимаций монстров

## 🚨 Частые проблемы и решения

### ❌ **Ошибка: "Animation clip '' not found"**

**Причина**: Система пытается воспроизвести анимацию с пустым именем.

**Диагностика**:
```
[Monster] Found animation with empty name at index 0 for Moshroom
[Monster] Animation at ID 0 has empty name for Moshroom. Skipping playback.
```

**Решения**:

#### 🔧 **Решение 1: Проверьте Animation Component**
1. Выберите префаб монстра в Unity
2. Найдите компонент `Animation`
3. Убедитесь, что все анимации имеют имена:
   ```
   Animation Component:
   ├── ✅ Mushroom_attack (не пустое)
   ├── ❌ [Empty] (пустое имя!)
   └── ✅ Mushroom_idle (не пустое)
   ```

#### 🔧 **Решение 2: Переименуйте анимации**
1. В Animation Component нажмите на анимацию с пустым именем
2. В Inspector найдите поле "Name"
3. Введите корректное имя: `"Mushroom_attack"`

#### 🔧 **Решение 3: Удалите пустые анимации**
1. В Animation Component выберите анимацию с пустым именем
2. Нажмите кнопку "Remove Clip"
3. Добавьте правильную анимацию через "Add Clip"

### ❌ **Ошибка: "No animations available"**

**Причина**: Animation Component пуст или не найден.

**Диагностика**:
```
[Monster] Animation component has 0 clips for Moshroom
[Monster] No animations available for Moshroom
```

**Решения**:

#### 🔧 **Решение 1: Добавьте анимации**
1. Выберите префаб монстра
2. В Animation Component нажмите "Add Clip"
3. Выберите файл анимации (.anim)
4. Задайте имя: `"Mushroom_attack"`

#### 🔧 **Решение 2: Проверьте структуру префаба**
```
MonsterPrefab
├── Model (MeshRenderer) ✅
├── Animation Component ✅
│   ├── Mushroom_attack ✅
│   └── Mushroom_idle ✅
└── Monster Script ✅
```

### ❌ **Ошибка: "Animation component not found"**

**Причина**: У не-гуманоидного монстра нет Animation Component.

**Диагностика**:
```
[Monster] Found MeshRenderer on Moshroom (non-humanoid)
[Monster] Animation component not found for non-humanoid monster Moshroom
```

**Решения**:

#### 🔧 **Решение: Добавьте Animation Component**
1. Выберите префаб монстра
2. Нажмите "Add Component"
3. Найдите "Animation"
4. Добавьте анимации в компонент

## 🎯 Правильная настройка для гриба

### ✅ **Корректная структура:**
```
MushroomMonster Prefab:
├── 📦 Model GameObject
│   ├── 🎨 MeshRenderer (для не-гуманоида)
│   └── 🎬 Animation Component
│       ├── [0] Mushroom_attack ← ID 0 (атака)
│       ├── [1] Mushroom_idle   ← ID 1 (покой)
│       └── [2] Mushroom_move   ← ID 2 (движение)
└── 🧠 Monster Script
```

### ✅ **MonsterBasicAttackSkill настройки:**
```
Animation Settings:
├── ✅ Use Universal Animation Ids: true
├── 📝 Custom Attack Animation Name: "" (пусто)
└── 🔢 Custom Attack Animation Id: -1 (авто)
```

## 📊 Логи для диагностики

### ✅ **Успешная инициализация:**
```
[Monster] Found MeshRenderer on MushroomMonster (non-humanoid)
[Monster] Found Animation component on MushroomMonster
[Monster] Animation component has 3 clips for MushroomMonster
[Monster] Cached animation 0: 'Mushroom_attack' for MushroomMonster
[Monster] Cached animation 1: 'Mushroom_idle' for MushroomMonster
[Monster] Cached animation 2: 'Mushroom_move' for MushroomMonster
[Monster] Available animations for MushroomMonster: 0:Mushroom_attack, 1:Mushroom_idle, 2:Mushroom_move
```

### ✅ **Успешная атака:**
```
[MonsterBasicAttackSkill] Playing universal Attack animation (ID: 0): 'Mushroom_attack' for MushroomMonster
[Monster] Playing animation by ID 0: 'Mushroom_attack' on MushroomMonster
```

### ❌ **Проблемная инициализация:**
```
[Monster] Found MeshRenderer on MushroomMonster (non-humanoid)
[Monster] Animation component not found for non-humanoid monster MushroomMonster
[Monster] Cannot initialize animation cache - no valid animation system found for MushroomMonster
[Monster] No animations available for MushroomMonster
```

## 🛠️ Пошаговая диагностика

### 1️⃣ **Проверьте тип монстра:**
- **MeshRenderer** = не-гуманоид → нужен **Animation**
- **SkinnedMeshRenderer** = гуманоид → нужен **Animator**

### 2️⃣ **Проверьте компоненты:**
- Для не-гуманоидов: `Animation Component` должен быть на том же GameObject или дочернем
- Для гуманоидов: `Animator` должен быть на том же GameObject или дочернем

### 3️⃣ **Проверьте анимации:**
- Все анимации должны иметь **непустые имена**
- **ID 0** должен быть анимацией атаки
- Анимации должны быть **совместимы** с моделью

### 4️⃣ **Проверьте настройки скилла:**
- `useUniversalAnimationIds = true` (рекомендуется)
- `customAttackAnimationName = ""` (пусто для авто-определения)
- `customAttackAnimationId = -1` (авто)

## 🎉 Результат

После исправления вы должны увидеть:
```
✅ [Monster] Playing animation by ID 0: 'Mushroom_attack' on MushroomMonster
✅ Анимация атаки воспроизводится корректно
✅ Никаких ошибок в консоли
```
