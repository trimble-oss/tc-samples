using System.Reflection;
using System.Runtime.InteropServices;
using SQLitePCL;

namespace DataSyncSampleApp;

/// <summary>
/// SQLCipher: <see cref="Batteries_V2.Init"/> loads the e_sqlcipher bundle. On Android, JNI preload and an
/// explicit <c>SQLite3Provider_e_sqlcipher</c> ensure <c>libe_sqlcipher.so</c> is ready before early P/Invoke.
/// iOS relies on bundle init only (provider type is not available in the iOS compile graph the same way).
/// DllImportResolver redirects Trimble.SQLite <c>tc_sqlite3</c> / <c>sqlite3</c> to <c>e_sqlcipher</c>.
/// </summary>
internal static class SqlCipherBootstrap
{
    private static readonly object Gate = new();
    private static bool _done;

    public static void EnsureInitialized()
    {
        lock (Gate)
        {
            if (_done)
                return;

#if ANDROID
            // Ensures libe_sqlcipher.so is mapped before SQLitePCLRaw's first DllImport (device-specific ordering).
            try
            {
                Java.Lang.JavaSystem.LoadLibrary("e_sqlcipher");
            }
            catch
            {
                /* Batteries_V2.Init below will load via alternate path */
            }
#endif

            // Intercept Trimble.SQLite.dll's DllImports that look for "tc_sqlite3" and redirect to "e_sqlcipher"
            Assembly trimbleSqliteAssembly = typeof(Trimble.SQLite.Sqlite).Assembly;
            NativeLibrary.SetDllImportResolver(trimbleSqliteAssembly, (libraryName, assembly, searchPath) =>
            {
                if (libraryName == "tc_sqlite3" || libraryName == "sqlite3")
                {
                    return NativeLibrary.Load("e_sqlcipher", assembly, searchPath);
                }
                return IntPtr.Zero;
            });

            Batteries_V2.Init();
#if ANDROID
            // Reinforces e_sqlcipher provider after bundle init; required for some device orderings with Trimble.SQLite.
            raw.SetProvider(new SQLite3Provider_e_sqlcipher());
#endif
            raw.FreezeProvider();
            _done = true;
        }
    }
}
