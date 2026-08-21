# MathsGen agent guide

Scope: declarative generation of `Delta.Maths` source and
`Maths/Vectors/shader-contract.json`.

Read only what the task needs:

- [README.md](README.md) for the generator model and stable conventions.
- [TODO.md](TODO.md) for selected work.
- [IDEAS.md](IDEAS.md) only for research or task selection.
- [WORKFLOW.md](WORKFLOW.md) before generation or verification.
- [../Maths/AGENTS.md](../Maths/AGENTS.md) when generated runtime/API output
  changes; [../DeltaShader/AGENTS.md](../DeltaShader/AGENTS.md) when the shader
  contract changes.

Never edit generated Maths files directly. Preserve the column-vector,
column-major `T * R * S` convention and explicit
`Builtin`/`Helper`/`Unsupported` mappings.

Skills: use `compiler-frontend` for declaration/validation rules,
`code-generation-and-backends` for output model changes,
`abi-and-calling-conventions` for shader-contract layout, and
`performance-benchmark` only for an explicitly requested generator benchmark.
