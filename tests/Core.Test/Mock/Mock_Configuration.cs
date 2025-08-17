namespace Core.Test.Mock
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Primitives;
    using System;
    using System.Collections.Generic;

    public sealed class Mock_Configuration : IConfiguration
    {
        private readonly Dictionary<string, string> _defaults = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _overrides = new(StringComparer.OrdinalIgnoreCase);
        private IConfigurationRoot _root = new ConfigurationBuilder().Build();

        public Mock_Configuration() => Rebuild();
        public Mock_Configuration(IDictionary<string, string> defaults)
        {
            if (defaults != null)
                foreach (var kv in defaults) _defaults[kv.Key] = kv.Value;
            Rebuild();
        }

        // ---- IConfiguration ----
        public string this[string key]
        {
            get => _root[key];
            set
            {
                // Az indexer a felülírás rétegbe ír
                _overrides[key] = value;
                Rebuild();
            }
        }

        public IEnumerable<IConfigurationSection> GetChildren() => _root.GetChildren();
        public IChangeToken GetReloadToken() => _root.GetReloadToken();
        public IConfigurationSection GetSection(string key) => _root.GetSection(key);

        // ---- Kényelmi API tesztekhez ----

        /// <summary>Alapértelmezett érték felvétele (defaults réteg).</summary>
        public Mock_Configuration SetDefault(string key, string value)
        {
            _defaults[key] = value;
            Rebuild();
            return this;
        }

        /// <summary>Tömeges alapértelmezett feltöltés.</summary>
        public Mock_Configuration SetDefaults(IDictionary<string, string> values)
        {
            foreach (var kv in values) _defaults[kv.Key] = kv.Value;
            Rebuild();
            return this;
        }

        /// <summary>Felülírás beállítása (overrides réteg).</summary>
        public Mock_Configuration SetOverride(string key, string value)
        {
            _overrides[key] = value;
            Rebuild();
            return this;
        }

        /// <summary>Tömeges felülírás.</summary>
        public Mock_Configuration SetOverrides(IDictionary<string, string> values)
        {
            foreach (var kv in values) _overrides[kv.Key] = kv.Value;
            Rebuild();
            return this;
        }

        /// <summary>Felülírások törlése.</summary>
        public Mock_Configuration ClearOverrides()
        {
            _overrides.Clear();
            Rebuild();
            return this;
        }

        /// <summary>
        /// Ideiglenes felülírás scope: using (...) { ... } végén automatikusan visszaáll.
        /// </summary>
        public IDisposable WithOverrides(IDictionary<string, string> values)
        {
            var snapshot = new Dictionary<string, string>(_overrides, StringComparer.OrdinalIgnoreCase);
            SetOverrides(values);
            return new Scope(() =>
            {
                _overrides.Clear();
                foreach (var kv in snapshot) _overrides[kv.Key] = kv.Value;
                Rebuild();
            });
        }

        private void Rebuild()
        {
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(_defaults)   // alap
                .AddInMemoryCollection(_overrides); // felülírások
            _root = builder.Build();
        }

        private sealed class Scope(Action onDispose) : IDisposable
        {
            private Action _onDispose = onDispose;

            public void Dispose()
            {
                _onDispose?.Invoke();
                _onDispose = null;
            }
        }


    }

}
