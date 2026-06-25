// Disable legacy runtime marshalling so that [LibraryImport] source generation can handle
// structs with explicit layout (e.g. INPUT_RECORD) using safe, AOT-compatible pinning.
[assembly: System.Runtime.CompilerServices.DisableRuntimeMarshalling]

// Expose internals to the Console test project.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Mked.Console.Tests")]
