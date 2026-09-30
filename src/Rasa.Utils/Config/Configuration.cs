using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Primitives;

namespace Rasa.Config
{
    /// <summary>
    /// appsettings.json (and the optional appsettings.env.json), loaded once and watched.
    ///
    /// Load used to build a new configuration root, with its own file watchers, every time it
    /// was called - and the servers called it from their reload handler, because a root's
    /// change token only fires once and the reload after the first was never heard. Every
    /// change to the file left two more watchers behind for the life of the process. A save
    /// that was half-written or not valid JSON - editors do not write atomically - threw out
    /// of the watcher's callback, on a thread pool thread with nothing above it, and ended
    /// the process.
    ///
    /// Now there is one root. ChangeToken.OnChange re-registers after every change, so every
    /// reload is heard. A file that fails to parse on a reload is reported and ignored, and
    /// the handlers are not run while appsettings.json has nothing loaded (a failed reload from
    /// the watcher empties it), so the settings already in force stay in force until a good
    /// file is saved. A save raises the change more than once; the handlers run once for each
    /// set of settings that is actually different. Loading at startup still fails loudly:
    /// there is nothing to fall back to then.
    /// </summary>
    public static class Configuration
    {
        public delegate void OnLoadDelegate();

        public static IConfiguration Config { get; private set; }

        /// <summary>Run after the first load and after every reload that parsed: bind a fresh config here.</summary>
        public static OnLoadDelegate OnLoad;

        /// <summary>Run before OnLoad on a reload (not the first load), for anything that wants to know it was one.</summary>
        public static OnLoadDelegate OnReLoad;

        private static IConfigurationRoot _root;
        private static IConfigurationProvider _main;
        private static IDisposable _watch;
        private static readonly object LoadLock = new object();
        private static bool _loaded;

        /// <summary>Every setting as last handed to the handlers, to tell a real change from a repeat.</summary>
        private static string _applied;

        /// <summary>Set by a Load() after the first: run the handlers even if nothing changed.</summary>
        private static bool _forceNext;

        /// <summary>
        /// The first call loads the files and runs OnLoad. Every call after it re-reads them (the
        /// console's reload config), which runs the handlers through the same path as a change
        /// on disk does.
        /// </summary>
        public static void Load()
        {
            lock (LoadLock)
            {
                if (_root != null)
                {
                    _forceNext = true;
                    _root.Reload();
                    _forceNext = false;
                    return;
                }

                _root = new ConfigurationBuilder()
                    .AddJsonFile(source => Watched(source, "appsettings.json", false))
                    .AddJsonFile(source => Watched(source, "appsettings.env.json", true))
                    .Build();

                Config = _root;
                _main = _root.Providers.First(p => p is JsonConfigurationProvider json && json.Source.Path == "appsettings.json");
                _applied = Snapshot();
                _loaded = true;

                _watch = ChangeToken.OnChange(() => _root.GetReloadToken(), Changed);
            }

            OnLoad?.Invoke();
        }

        private static void Watched(JsonConfigurationSource source, string path, bool optional)
        {
            source.Path = path;
            source.Optional = optional;
            source.ReloadOnChange = true;
            source.OnLoadException = LoadFailed;
            source.ResolveFileProvider();
        }

        /// <summary>
        /// A file that would not load. At startup it is thrown, as it always was; on a reload it
        /// is logged and ignored, and Changed keeps what is in force.
        /// </summary>
        private static void LoadFailed(FileLoadExceptionContext context)
        {
            lock (LoadLock)
                if (!_loaded)
                    return;

            context.Ignore = true;

            try
            {
                Logger.WriteLog(LogType.Error,
                    $"{context.Provider?.Source?.Path ?? "The configuration file"} could not be read after a change, so the settings already loaded stay in force. "
                    + $"Fix the file and save it again. {context.Exception?.GetBaseException().Message}");
            }
            catch (Exception)
            {
                // Nothing on this thread may throw.
            }
        }

        /// <summary>
        /// A change to either file, from the watcher's thread, or a reload asked for from the
        /// console, on its. Runs the handlers unless the reload failed; nothing escapes, since the
        /// watcher's thread has nothing above it.
        /// </summary>
        private static void Changed()
        {
            lock (LoadLock)
            {
                // A reload from the watcher that failed leaves appsettings.json with nothing in
                // it; binding that would zero every setting.
                if (!_main.GetChildKeys(Enumerable.Empty<string>(), null).Any())
                    return;

                var snapshot = Snapshot();
                var force = _forceNext;

                _forceNext = false;

                if (!force && snapshot == _applied)
                    return;

                _applied = snapshot;
            }

            try
            {
                OnReLoad?.Invoke();
                OnLoad?.Invoke();
            }
            catch (Exception e)
            {
                try
                {
                    Logger.WriteLog(LogType.Error, $"Applying the reloaded configuration failed: {e}");
                }
                catch (Exception)
                {
                    // Nothing on this thread may throw.
                }
            }
        }

        private static string Snapshot() =>
            string.Join("\n", _root.AsEnumerable().OrderBy(e => e.Key, StringComparer.Ordinal).Select(e => $"{e.Key}={e.Value}"));

        public static void Bind(object obj)
        {
            Config.Bind(obj);
        }
    }
}
