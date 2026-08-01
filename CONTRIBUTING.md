# Contributing to NG.Velox

Thank you for your interest in contributing to **NG.Velox**! 

NG.Velox is a systems-level .NET library designed for mission-critical CNC control. Because we operate in an environment where microsecond latencies and Garbage Collection (GC) pauses are unacceptable, this project has a very specific architectural philosophy. 

Before you submit a PR, please read this guide carefully to ensure your contributions align with our core performance constraints.

## 🧠 The Philosophy: Systems-Level .NET
We treat C# as a high-level assembly language. Our goal is to achieve native C/C++ performance while leveraging the .NET ecosystem. 
* **Nanoseconds matter.** We care about branch prediction, CPU cache locality (L1/L2), and memory alignment.
* **Zero-GC is mandatory.** The core pipeline (from Preprocessing to Postprocessing) must generate **exactly 0 bytes** of managed heap allocations.
* **Determinism.** Execution time must be highly predictable. We avoid algorithms with unpredictable branching or hidden allocations.

## 🛑 The Golden Rules of NG.Velox

If you are modifying the core pipeline (`Preprocessing`, `Lexing`, `Parsing`, `Interpretation`, `Planning`, `Interpolation`, `Postprocessing`), you **must** adhere to these rules:

1. **No Managed Heap Allocations:** 
   * Do not use `new` for reference types.
   * Do not use LINQ, `string.Split()`, `List<T>`, or `ArrayPool<T>` in the hot paths.
   * All intermediate data must be allocated inside the shared `MemoryArena` using raw pointers (`T*`) or `ArenaList<T>`.
2. **Use `ref struct` and `unsafe`:** 
   * Intermediate results (like `LexingResult`, `ParsingResult`) are `readonly unsafe ref struct` wrappers over unmanaged pointers.
   * Embrace pointer arithmetic (`char*`, `int*`) to eliminate array bounds-checking overhead.
3. **Strict Memory Layouts:**
   * Use `[StructLayout(LayoutKind.Sequential, Pack = X)]` or `[StructLayout(LayoutKind.Explicit)]` to control padding and ensure dense memory packing.
   * Keep structs small to maximize CPU cache line utilization.
4. **No Virtual Calls or Reflection in Hot Loops:**
   * Use generic type constraints (`where TContext : struct, IInterpolationContext, allows ref struct`) to allow the JIT compiler to aggressively inline interface calls.
   * Do not use reflection to discover G-codes or machine states.

## 🚀 Areas Where We Need Help

We welcome contributions in the following areas:

### 1. Industrial Kinematics & Mathematics
* Implementing missing G-code/M-code standards (e.g., canned cycles G81-G89, tool radius compensation G41/G42).
* Optimizing the Look-Ahead planner or S-Curve (jerk-limited) calculations.
* Adding support for 4th/5th axis kinematics (RTCP - Rotary Tool Center Point).

### 2. Extreme Performance Optimization
* **SIMD / Vectorization:** Replacing scalar math in the `Interpolator` or `Planner` with `System.Numerics.Vector<T>` or hardware intrinsics (AVX2/AVX-512).
* **Branchless Algorithms:** Refactoring `if/switch` statements in the Lexer/Parser into bitwise operations and lookup tables.
* **Memory Prefetching:** Optimizing memory access patterns for the `MemoryArena`.

### 3. Infrastructure & Tooling
* **NativeAOT:** Ensuring 100% compatibility with NativeAOT compilation for embedded edge devices.
* **Cross-Platform Edge Cases:** Testing and fixing alignment/endianness issues on ARM64 (e.g., Raspberry Pi, Apple Silicon).
* **Fuzzing:** Writing fuzzers for the Lexer/Parser to guarantee memory safety when processing malformed G-code.

## 🛠 Development Setup

1. **Prerequisites:**
   * .NET 10 SDK (or the latest preview/release).
   * An IDE that supports `unsafe` code and `.editorconfig` (Rider or Visual Studio 2022).
2. **Clone & Build:**
   ```bash
   git clone https://github.com/MachineGate/NG.Velox.git
   cd NG.Velox
   dotnet build -c Release
   ```
3. **Running Benchmarks:**
   We use `BenchmarkDotNet`. To verify your performance impact:
   ```bash
   cd NG.Velox.Benchmarks
   dotnet run -c Release --filter *PipelineBenchmarks*
   ```
   *Note: Always run benchmarks in `Release` mode without a debugger attached.*

## 🐛 Reporting Bugs

When reporting bugs, especially related to kinematics or parsing:
1. Provide the exact **G-code snippet** that causes the issue.
2. Specify the **machine configuration** (e.g., `VeloxPipelineOptions` used).
3. If it's a crash/memory corruption, provide the **stack trace** and the exact hardware/OS architecture (x64/ARM64).

## 📝 Pull Request Process

1. Fork the repo and create a branch from `main`.
2. Write your code, ensuring it passes all existing unit tests.
3. **Crucial:** If you modified the core pipeline, run the benchmarks and include the `Before` and `After` BenchmarkDotNet markdown tables in your PR description. We will reject PRs that introduce GC allocations or regress performance without a compelling mathematical reason.
4. Ensure all public and complex internal APIs have XML documentation.
5. Submit the PR and tag the maintainers for review.

## 🤝 Code of Conduct

We are a community of engineers, machinists, and systems programmers. Be respectful, constructive, and focus on the math and the metrics. 

---

*Thank you for helping us push the boundaries of what .NET can achieve in real-time manufacturing!*