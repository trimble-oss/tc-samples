using System.Reflection;
using System.Runtime.InteropServices;
using SQLitePCL;

namespace DataSyncSampleApp;

/// <summary>
/// SQLCipher on Android: one copy of <c>libe_sqlcipher.so</c> must ship, and the native lib must load
/// before any P/Invoke to <c>e_sqlcipher</c>. <see cref="Batteries_V2.Init"/> registers the loader;
/// optional JNI preload fixes edge cases where the first P/Invoke runs too early.
/// This also sets up a DllImportResolver to redirect tc_sqlite3 to e_sqlcipher.
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
            raw.SetProvider(new SQLite3Provider_e_sqlcipher());
            raw.FreezeProvider();
            _done = true;
        }
    }
}
