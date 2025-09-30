using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Banking.Capabilities
{
    public interface ICapabilitiesStore
    {
        IEnumerable<BankCapabilities> GetAll();
    }

    /// <summary>
    /// Loads bank capabilities from a JSON file.
    /// JSON supports enum strings (e.g., "SHA") or numeric values.
    /// Extra properties in JSON are ignored.
    /// </summary>
    public sealed class JsonCapabilitiesStore : ICapabilitiesStore
    {
        private readonly string _path;
        private readonly JsonSerializerOptions _options;
        private List<BankCapabilities>? _cache;
        private readonly bool _reloadOnEachCall;

        public JsonCapabilitiesStore(string path, bool reloadOnEachCall = false, JsonSerializerOptions? options = null)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _reloadOnEachCall = reloadOnEachCall;
            _options = options ?? new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            _options.Converters.Add(new JsonStringEnumConverter());
        }

        public IEnumerable<BankCapabilities> GetAll()
        {
            if (_reloadOnEachCall)
            {
                _cache = null;
            }

            if (_cache is null)
            {
                if (!File.Exists(_path))
                {
                    throw new FileNotFoundException($"Capabilities JSON file not found: {_path}");
                }

                string json = File.ReadAllText(_path);
                var banks = JsonSerializer.Deserialize<List<BankCapabilities>>(json, _options)
                            ?? new List<BankCapabilities>();

                // Normalize currencies comparer to OrdinalIgnoreCase
                _cache = banks.Select(b => new BankCapabilities
                {
                    BankCode = b.BankCode,
                    Currencies = new HashSet<string>(b.Currencies ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase),
                    SameDayTransfer = b.SameDayTransfer,
                    BearerChargeTypes = b.BearerChargeTypes
                }).ToList();
            }

            return _cache;
        }

        public void Refresh() => _cache = null;
    }
}
