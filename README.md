# 🚀 DR Language Compiler

An interactive **Compiler** built from scratch using **C#** and **.NET Framework**. This project was developed as part of our **Compiler Design** course to demonstrate the full compilation pipeline—from raw source code to semantic validation.

Developed by: **Khaled Hamada** & **Khaled Mohamed**.
---

## ✨ Project Overview

The **DR Compiler** is a desktop application designed to process a custom-defined syntax (DR Language). It features a modern Dark-themed GUI that provides real-time feedback on every stage of the compilation process.

### 🛠️ The Compilation Pipeline

Our project implements the three core phases of a compiler:

1.   **Lexical Analysis (Scanner):**  
    *  Uses **Regular Expressions (Regex)** to tokenize the source code.  
    *  Identifies keywords (e.g., `plan`, `CHECK`, `REWORK`), literals, and operators.  
    *  Handles single-character symbols and multi-character operators like `++` or `>=`.  

2.   **Syntax Analysis (Parser):**  
    *  Implements a **Recursive Descent Parser**.  
    *  Validates the structure of the code against the language grammar.  
    *  Generates an **Abstract Syntax Tree (AST)** / Parse Tree for visualization.

3.   **Semantic Analysis:**  
    *  Manages a **Symbol Table** using a stack-based approach to handle nested Scopes.  
    *  Performs **Type Checking** (e.g., ensuring a `duration` (double) isn't assigned to a `status` (bool)).  
    *  Detects redeclarations and undefined identifiers.  

---

## 📝 DR Language Features

The language includes specialized data types and control structures:

| Keyword | Type / Function |
| :--- | :--- |
| `file` |  Integer Type   |
| `duration` |  Double/Floating-point Type   |
| `note` |  String Type   |
| `status` |  Boolean Type   |
| `plan MORNING_COFFEE()` |  Main Function Entry |
| `CHECK` / `REJECT` |  If / Else Logic   |
| `REWORK` |  While Loop   |
| `SHOW` |  Output Statement   |

---

## 💻 Tech Stack & Tools

*  **Language:** C#  
*  **Framework:** .NET Framework v4.7.2  
*  **GUI:** Windows Forms (WinForms)  
*  **Key Algorithms:** Recursive Descent Parsing, Tree Traversal, Stack-based Scope Management.  

---

## 🚀 Getting Started

1.   **Prerequisites:** Visual Studio 2017/2019/2022 and .NET Framework 4.7.2.  
2.   **Run:** Open the `.csproj` file, build the solution, and run the `DR_GUI` application. 
3.   **Test:** Use the "Load Example" button or write your own DR code in the editor and click **Run Compiler**.  

---

## 🤝 Collaboration
 Special thanks to **Khaled Mohamed** for the core contribution to the scanning logic and semantic validation rules.  
