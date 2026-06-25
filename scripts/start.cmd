@echo off
setlocal
cd /d "%~dp0.."
"C:\Users\Mardiles 2025\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe" "node_modules\next\dist\bin\next" start -p 3000
