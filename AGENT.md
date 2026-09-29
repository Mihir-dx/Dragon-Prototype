# AI Agent Instructions

## 1. Workflow & Planning
* **Professional Execution:** Before writing any scripts or executing any MCP tool calls, you must explicitly outline your step-by-step plan for the current task and wait for my approval.
* **Tool Efficiency:** Do not guess the state of the project. Use your MCP tools efficiently to read the scene hierarchy, inspect component properties, and verify file paths before making modifications.
* **Rule Adherence:** You will read and abide by these rules before executing any new task.

## 2. Technical Constraints
* **Input System:** You MUST exclusively use Unity's New Input System (`UnityEngine.InputSystem`). Under no circumstances should you use the legacy `UnityEngine.Input` class (e.g., no `Input.GetKeyDown`, no `Input.GetAxis`). 
* **Simplicity & Readability:** Keep C# scripts clean and readable. Rely on straightforward enum-based state machines for AI and logic rather than over-engineered design patterns.
