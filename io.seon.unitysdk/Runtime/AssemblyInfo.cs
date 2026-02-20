using System.Runtime.CompilerServices;

// Allow test assemblies to access internal members (e.g., SetPlatform, ISeonPlatform)
[assembly: InternalsVisibleTo("io.seon.unitysdk.editor.tests")]
[assembly: InternalsVisibleTo("io.seon.unitysdk.tests")]
