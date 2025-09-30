using System;
using System.Collections.Generic;
using System.Linq;

namespace Banking.Capabilities
{
    [Flags]
    public enum BearerChargeType
    {
        None = 0,
        SHA = 1,   // shared
        OWN = 2,   // our (aka OUR)
        BEN = 4    // beneficiary
    }

    /// <summary>
    /// Capability matrix per correspondent bank.
    /// </summary>
    public sealed class BankCapabilities
    {
        public string BankCode { get; init; } = default!; // e.g., BIC or internal code

        public HashSet<string> Currencies { get; init; } = new(StringComparer.OrdinalIgnoreCase); // ISO 4217 codes

        public bool SameDayTransfer { get; init; }

        public BearerChargeType BearerChargeTypes { get; init; } = BearerChargeType.None;

        public bool SupportsCurrency(string iso4217) => !string.IsNullOrWhiteSpace(iso4217) && Currencies.Contains(iso4217);

        public bool SupportsAllCurrencies(IEnumerable<string> iso4217s) =>  iso4217s != null && iso4217s.All(Currencies.Contains);

        public bool SupportsSameDay(bool requireSameDay) => !requireSameDay || SameDayTransfer;

        public bool SupportsCharge(BearerChargeType charge) => charge != BearerChargeType.None && (BearerChargeTypes & charge) == charge;

        public bool SupportsAnyCharge(BearerChargeType charges) => (BearerChargeTypes & charges) != BearerChargeType.None;
    }

    /// <summary>
    /// A filter describing a desired payment route.
    /// </summary>
    public sealed class CapabilityQuery
    {
        public string Currency { get; init; } = "USD"; // single currency
        public bool RequireSameDay { get; init; } = false;  // yes/no
        public BearerChargeType AllowedCharges { get; init; } = BearerChargeType.SHA | BearerChargeType.OWN | BearerChargeType.BEN; // one or more
    }

    public static class CapabilityEngine
    {
        private static ICapabilitiesStore? _store;

        public static void Configure(ICapabilitiesStore store) => _store = store ?? throw new ArgumentNullException(nameof(store));

        private static IEnumerable<BankCapabilities> EnsureConfiguredAndGetAll()
        {
            if (_store is null)
                throw new InvalidOperationException("CapabilityEngine is not configured.");
            return _store.GetAll();
        }

        /// <summary>
        /// Returns banks that satisfy the requested capability set.
        /// - Currency: must include the requested currency
        /// - SameDay: must be true if requested
        /// - Bearer Charge: bank must support at least one of the allowed charges
        /// </summary>
        public static IEnumerable<BankCapabilities> FindEligibleBanks(CapabilityQuery request)
        {
            var source = EnsureConfiguredAndGetAll();

            return source.Where(b =>
                b.SupportsCurrency(request.Currency) &&
                b.SupportsSameDay(request.RequireSameDay) &&
                b.SupportsAnyCharge(request.AllowedCharges)
            );
        }

        /// <summary>
        /// Returns eligible banks scored (example: prefer same-day, prefer SHA if allowed).
        /// Customize the scoring logic as needed.
        /// </summary>
        public static IEnumerable<(BankCapabilities Bank, int Score)> RankEligibleBanks(CapabilityQuery request)
        {
            var eligible = FindEligibleBanks(request);

            return eligible.Select(b =>
            {
                int score = 0;
                if (b.SameDayTransfer && request.RequireSameDay) score += 10;
                if ((b.BearerChargeTypes & request.AlledCharges & BearerChargeType.SHA) != 0) score += 5;
                return (Bank: b, Score: score);
            });
        }

        /// <summary>
        /// For each bank that matches non-currency constraints (same-day, charges),
        /// returns the list of currencies supported by that bank.
        /// Optionally restrict the currencies to the provided set.
        /// </summary>
        public static IEnumerable<(BankCapabilities Bank, List<string> Currencies)> GetEligibleCurrenciesPerBank(CapabilityQuery request, IEnumerable<string>? restrictToCurrencies = null)
        {
            var source = EnsureConfiguredAndGetAll();

            HashSet<string>? filter = restrictToCurrencies is not null
                ? new HashSet<string>(restrictToCurrencies, StringComparer.OrdinalIgnoreCase)
                : null;

            return source
                .Where(b =>
                    b.SupportsSameDay(request.RequireSameDay) &&
                    b.SupportsAnyCharge(request.AllowedCharges))
                .Select(b =>
                {
                    IEnumerable<string> pool = filter is null
                        ? b.Currencies
                        : b.Currencies.Where(c => filter.Contains(c));

                    var list = pool
                        .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    return (Bank: b, Currencies: list);
                })
                .Where(x => x.Currencies.Count > 0)
                .Select(x => (x.Bank, x.Currencies));
        }

        /// <summary>
        /// Aggregates bank capabilities (same-day flag, bearer charge types) and currencies
        /// for banks matching non-currency constraints. Optionally restrict currencies to a provided set.
        /// </summary>
        public static IEnumerable<(BankCapabilities Bank, bool SameDay, BearerChargeType Charges, List<string> Currencies)> GetCapabilitiesPerBank(CapabilityQuery request, IEnumerable<string>? restrictToCurrencies = null)
        {
            var source = EnsureConfiguredAndGetAll();

            HashSet<string>? filter = restrictToCurrencies is not null
                ? new HashSet<string>(restrictToCurrencies, StringComparer.OrdinalIgnoreCase)
                : null;

            return source
                .Where(b =>
                    b.SupportsSameDay(request.RequireSameDay) &&
                    b.SupportsAnyCharge(request.AllowedCharges))
                .Select(b =>
                {
                    IEnumerable<string> pool = filter is null
                        ? b.Currencies
                        : b.Currencies.Where(c => filter.Contains(c));

                    var list = pool
                        .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    return (Bank: b, SameDay: b.SameDayTransfer, Charges: b.BearerChargeTypes, Currencies: list);
                })
                .Where(x => x.Currencies.Count > 0)
                .Select(x => (x.Bank, x.SameDay, x.Charges, x.Currencies));
        }
    }
}
