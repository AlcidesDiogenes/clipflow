@echo off
chcp 65001 >nul
title ClipFlow - Modo Desenvolvimento (Hot Reload Ao Vivo)
echo ========================================================
echo   CLIPFLOW - MODO DESENVOLVIMENTO (HOT RELOAD ATIVO)
echo   Qualquer alteracao salva nos arquivos .xaml ou .cs
echo   sera aplicada automaticamente na janela aberta!
echo ========================================================
echo.
taskkill /F /IM ClipFlow.exe >nul 2>&1
cd /d "%~dp0"
dotnet watch --project src/ClipFlow.App
pause
