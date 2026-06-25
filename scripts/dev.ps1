$ErrorActionPreference = "Stop"

$node = "C:\Users\Mardiles 2025\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe"
& $node "node_modules\next\dist\bin\next" dev -p 3000
