# MachineGate.NG.Velox v2.0.1

Zero-allocation, high-performance G-code processing pipeline for CNC kinematics, trajectory planning, and real-time interpolation.

`NG.Velox` is a systems-level .NET library designed to parse, interpret, plan, and interpolate G-code with native C/C++ performance. It is built for mission-critical environments where Garbage Collection (GC) pauses are unacceptable, such as real-time CNC controllers, edge computing, and high-throughput CAM simulations.

## 🚀 Key Features

- **Absolute Zero-Allocation (GC-Free)**: The *entire* pipeline, including Simulation DTOs and Hardware binary streams, is now 100% allocation-free on the managed heap. Powered by a custom unmanaged `MemoryArena` and raw pointer arithmetic.
- **Industrial Kinematics**: Implements Look-Ahead planning, Junction Deviation, and S-Curve (jerk-limited) acceleration profiles.
- **High-Throughput Pipeline**: Processes ~400,000 lines of G-code per second on mid-tier hardware.
- **Dual Output Targets**:
  - **Hardware**: Packs trajectory into dense binary structures directly in unmanaged memory for zero-copy UART/Ethernet transmission.
  - **Simulation**: Generates unmanaged `SimulationFrame` structs pointing directly to arena memory, eliminating managed DTO materialization overhead.
- **UVW Plane Normalization**: Unified mathematical path for G17/G18/G19 planes, eliminating redundant code branches.
- **Accurate Diagnostics**: `IndexMap` tracks preprocessed characters back to original source lines for precise error reporting.

## 🏗 Architecture & Pipeline

The library processes data through a strictly sequential, allocation-free pipeline using a shared `MemoryArena`:

1. **Preprocessor**: Strips comments, N-codes, and whitespace. Builds an `IndexMap` for error tracking. Writes directly to unmanaged memory.
2. **Lexer**: O(1) character classification via expanded `ushort` bitmask lookup tables (`CharRegistry`).
3. **Parser**: Generates a dense 16-byte AST `Node` using explicit struct unions. Utilizes a high-performance `TokenReader` cursor.
4. **Interpreter**: Emulates the CNC VM (modal groups, planes, absolute/relative modes) allocating frames directly in the arena.
5. **Planner**: Look-Ahead algorithm calculating S-curve entry/exit/cruise velocities in-place.
6. **Interpolator**: Generates adaptive-step trajectory points using `ArenaList<T>`.
7. **Postprocessor**: 
   - *Hardware*: Pre-calculates exact binary size and uses `Buffer.MemoryCopy` for zero-copy blitting.
   - *Simulation*: Outputs unmanaged structs referencing arena pointers.

## 📊 Benchmarks

Tested on `Intel Core i5-10400F`, `.NET 10.0.10`, `X64 RyuJIT AVX2`, `BenchmarkDotNet v0.15.2`.

| Line Count | Mean Time | Allocated |
| :--- | :--- | :--- |
| **10** | 8.12 μs | **0 B** |
| **100** | 147.38 μs | **0 B** |
| **1,000** | 1.81 ms | **0 B** |
| **10,000** | 24.88 ms | **0 B** |
| **100,000** | 251.55 ms | **0 B** |

*Note: 100,000 lines of G-code represents a massive, highly detailed 3D toolpath. The entire end-to-end pipeline processes it in ~251 milliseconds with absolutely zero GC pressure.*

## 💻 Usage Example

In 2.0.0 the API relies on a single `MemoryArena` for all internal allocations. The `PipelineResult` is a lightweight `ref struct` and does not require disposal.

### Hardware Pipeline (Binary Output for Microcontrollers)

```csharp
using NG.Velox.Factories;
using NG.Velox.Memory.Core;
using NG.Velox.Pipeline.Extensions;
using NG.Velox.Diagnostic.Core;

var pipeline = VeloxPipelineFactory.CreateHardware();

// 1. Allocate a single unmanaged memory arena for the pipeline execution
var arena = new MemoryArena(capacity: 1024 * 1024 * 50); // e.g., 50 MB
var diag = new DiagnosticBag();

try 
{
    ReadOnlyMemory<char> gcode = File.ReadAllText("toolpath.gcode").AsMemory();

    // 2. Process returns an unmanaged ref struct view. No intermediate allocations!
    var result = pipeline.Process(gcode, ref arena, ref diag);

    if (diag.HasErrors)
    {
        Console.WriteLine($"Error {diag.Diagnostics[0].Code} at index {diag.Diagnostics[0].Start}");
        return;
    }

    // 3. Cross the managed/unmanaged boundary explicitly when needed
    // Option A: Materialize to a managed array (allocates only here, at the boundary)
    byte[] managedBytes = result.ToArray(); 

    // Option B: Zero-copy copy to an existing managed buffer/span
    // result.CopyTo(mySpan);
}
finally
{
    // 4. Dispose arena and diagnostics if needed
    arena.Dispose();
    diag.Dispose();
}
```

### Simulation Pipeline (DTOs for Visualization/JSON)

```csharp
using NG.Velox.Factories;
using NG.Velox.Memory.Core;
using NG.Velox.Pipeline.Extensions;
using NG.Velox.Diagnostic.Core;
using System.Text.Json;

var pipeline = VeloxPipelineFactory.CreateSimulation();

// 1. Allocate a single unmanaged memory arena for the pipeline execution
var arena = new MemoryArena(capacity: 1024 * 1024 * 50); // e.g., 50 MB
var diag = new DiagnosticBag();

try 
{
    ReadOnlyMemory<char> gcode = File.ReadAllText("toolpath.gcode").AsMemory();

    // 2. Process returns an unmanaged ref struct view. No intermediate allocations!
    var result = pipeline.Process(gcode, ref arena, ref diag);

    if (diag.HasErrors)
    {
        Console.WriteLine($"Error {diag.Diagnostics[0].Code} at index {diag.Diagnostics[0].Start}");
        return;
    }

    // 3. SimulationFrames are unmanaged structs pointing to arena memory.
    // To serialize to JSON, explicitly marshal to managed arrays:
    var managedFrames = result.ToArray();

    // 4. Map managed frames to Dtos if needed or write custom serializer
    var dtos = managedFrames.MapToDtos();
    string json = JsonSerializer.Serialize(dtos);
    File.WriteAllText("trajectory.json", json);
}
finally
{
    // 5. Dispose arena and diagnostics if needed
    arena.Dispose();
    diag.Dispose();
}
```

## 📸 Visual Validation

The mathematical correctness of the kinematics, S-curves, and arc interpolation is validated against industry-standard software.

**1. Source G-Code Snippet:**
```gcode
(Complex arc and linear moves)
G00 X0.000 Y0.000 Z0.000 F100.000
G00 Z200.000
G00 X100.000 Y100.000 Z200.000
G00 X100.000 Y100.000 Z100.000
F100.000
G17
G03 X120.000 Y80.000 R20.000
G03 X100.000 Y60.000 R20.000
G03 X80.000 Y80.000 R20.000
G03 X100.000 Y100.000 R20.000
G03 X120.000 Y80.000 R20.000
G02 X80.000 Y80.000 R20
G02 X120.000 Y80.000 R20
G18
G03 X100.000 Z120.000 R20.000
G03 X80.000 Z100.000 R20.000
G03 X100.000 Z80.000 R20.000
G03 X120.000 Z100.000 R20.000
G02 X80.000 Z100.000 R20.000
G02 X120.000 Z100.000 R20.000
G17
G03 X100 Y100 R20
G19
G02 Y80 Z120 R20
G02 Y60 Z100 R20
G02 Y80 Z80 R20
G02 Y100 Z100 R20
G03 Y60 Z100 R20
G03 Y100 Z100 R20
G00 X100.000 Y100.000 Z200.0
```

**2. NG.Velox Interpolation Output (NumPy/Matplotlib):**

![NumPy Validation](numpy_validation.png)

**3. Ethalon Validation (CIMCO Edit):**

![CIMCO Ethalon](cimco_ethalon.png)

## ⚙️ Technical Highlights for Systems Devs

- **Custom Arena Allocator**: `MemoryArena` provides O(1) linear allocation with strict alignment guarantees via `NativeMemory.Alloc`, bypassing the .NET GC entirely.
- **Raw Pointer Arithmetic**: Hot paths (Lexing, Parsing, Interpolation) use direct `T*` pointer math, eliminating array index bounds-checking overhead.
- **Expanded CharMask**: Upgraded from `byte` to `ushort` to natively classify brackets, semicolons, whitespace, newlines, and labels during preprocessing.
- **TokenReader**: A high-performance, cursor-based token consumption engine that replaces standard index-based loops.
- **Struct Unions**: `Node` uses `[StructLayout(LayoutKind.Explicit, Size = 16)]` to overlap enums, reducing AST memory footprint by ~40% and maximizing L1/L2 cache locality.
- **Branchless Lexing**: `CharRegistry` uses bitwise masks for O(1) character classification without `if/switch` branching.
- **No Reflection / No Virtual Calls**: Interfaces are used for architecture, but concrete `sealed` classes are invoked directly in the pipeline to allow aggressive JIT inlining.

## 📦 Installation

```bash
dotnet add package MachineGate.NG.Velox --version 2.0.1
```

## 🤝 Contributing

This is a systems-level project. Contributions regarding mathematical edge-cases, SIMD vectorization for the interpolator, or NativeAOT compatibility tests are highly welcome. Please open an issue first to discuss the architectural impact.

## ⚖️ License

Distributed under the MIT License. See `LICENSE` for more information.
