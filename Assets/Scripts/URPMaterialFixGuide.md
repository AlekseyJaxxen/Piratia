# URP Material Fix Guide

## 🎯 Проблема
Временные box монстры использовали `Shader.Find("Standard")`, который не работает в Unity URP (Universal Render Pipeline).

## ✅ Решение

### 1. **URPMaterialHelper.cs** - Утилита для URP материалов
- Автоматически находит подходящие URP шейдеры
- Fallback на Standard шейдер если URP не найден
- Специальные материалы для разных типов монстров

### 2. **Обновленные файлы:**
- `Monster.cs` - временные box монстры
- `MonsterBoxPrefabGenerator.cs` - prefab генератор
- `URPMaterialTest.cs` - тестирование материалов

---

## 🔧 Как использовать

### **Автоматическое создание материалов:**
```csharp
// Для монстров (автоматически по типу)
Material material = URPMaterialHelper.CreateMonsterBoxMaterial(color, boxType);

// Для обычных материалов
Material material = URPMaterialHelper.CreateURPMaterial(color, metallic, smoothness);
```

### **Тестирование:**
1. Добавьте `URPMaterialTest` на любой GameObject
2. **Context Menu → "Test URP Materials"** - проверка шейдеров
3. **Context Menu → "Create Test Boxes"** - создание тестовых box
4. **Context Menu → "Clean Test Boxes"** - очистка тестовых объектов

---

## 🎨 Типы монстров и их материалы

| Тип | Цвет | Metallic | Smoothness | Описание |
|-----|------|----------|------------|----------|
| **Tank** | Синий | 0.5 | 0.8 | Металлический, гладкий |
| **Fast** | Красный | 0.1 | 0.5 | Матовый, быстрый |
| **Magic** | Фиолетовый | 0.2 | 0.9 | Магический, очень гладкий |
| **Ranged** | Желтый | 0.4 | 0.6 | Средний металлик |

---

## 🔍 Диагностика

### **В логах должно быть:**
```
[URPMaterialHelper] Found URP shader: Universal Render Pipeline/Lit
[Monster] Created material with shader: Universal Render Pipeline/Lit
```

### **Если URP не найден:**
```
[URPMaterialHelper] No URP shaders found, using Standard shader as fallback
```

---

## ⚠️ Важно

1. **URP шейдеры** должны быть доступны в проекте
2. **Fallback на Standard** работает, но может выглядеть по-другому
3. **Материалы создаются динамически** - не сохраняются в проекте
4. **Тестирование** рекомендуется перед использованием в игре

---

## 🚀 Результат

Теперь временные box монстры будут:
- ✅ **Правильно отображаться** в URP
- ✅ **Иметь разные материалы** по типам
- ✅ **Работать на всех платформах**
- ✅ **Автоматически адаптироваться** к доступным шейдерам
