@echo off

echo --- Проверка существующего удаленного репозитория 'origin' ---
git remote -v
if errorlevel 1 (
    echo Удаленный репозиторий 'origin' не найден. Добавляем его.
    git remote add origin https://github.com/AlekseyJaxxen/Piratia.git
) else (
    echo Удаленный репозиторий 'origin' уже существует.
)

echo --- Добавление всех файлов и коммит ---
git add .
git commit -m "Initial commit"

echo --- Отправка файлов на GitHub ---
git push -u origin main

echo --- Готово! Все файлы отправлены на GitHub. ---
pause