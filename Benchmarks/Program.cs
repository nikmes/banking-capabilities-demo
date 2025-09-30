using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Banking.Capabilities;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

[MemoryDiagnoser]
public class CapabilityEngineBenchmarks
{
    private readonly CapabilityQuery _usdSameDayShaQuery = new()
    {
        Currency = "USD",
        RequireSameDay = true,
        AllowedCharges = BearerChargeType.SHA
    };

    private readonly CapabilityQuery _eurAnyDayOwnShaQuery = new()
    {
        Currency = "EUR",
        RequireSameDay = false,
        AllowedCharges = BearerChargeType.SHA | BearerChargeType.OWN
    };

    private IReadOnlyList<BankCapabilities> _banks = default!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _banks = CreateSampleBanks();
        CapabilityEngine.Configure(new InMemoryCapabilitiesStore(_banks));
    }

    [Benchmark]
    public List<BankCapabilities> FindEligibleUsdSameDaySha()
        => CapabilityEngine.FindEligibleBanks(_usdSameDayShaQuery).ToList();

    [Benchmark]
    public List<(BankCapabilities Bank, bool SameDay, BearerChargeType Charges, List<string> Currencies)> GetCapabilitiesForEuro()
        => CapabilityEngine.GetCapabilitiesPerBank(_eurAnyDayOwnShaQuery, new[] { "EUR" }).ToList();

    private static List<BankCapabilities> CreateSampleBanks()
    {
        return new List<BankCapabilities>
        {
            new()
            {
                BankCode = "BANKA-GB2L",
                Currencies = new HashSet<string>(new[] { "USD", "EUR", "GBP" }, StringComparer.OrdinalIgnoreCase),
                SameDayTransfer = true,
                BearerChargeTypes = BearerChargeType.SHA | BearerChargeType.OWN
            },
            new()
            {
                BankCode = "BANKB-DEFF",
                Currencies = new HashSet<string>(new[] { "EUR", "USD" }, StringComparer.OrdinalIgnoreCase),
                SameDayTransfer = false,
                BearerChargeTypes = BearerChargeType.SHA | BearerChargeType.BEN
            },
            new()
            {
                BankCode = "BANKC-US33",
                Currencies = new HashSet<string>(new[] { "USD", "CAD" }, StringComparer.OrdinalIgnoreCase),
                SameDayTransfer = true,
                BearerChargeTypes = BearerChargeType.OWN | BearerChargeType.BEN
            },
            new()
            {
                BankCode = "BANKF-CHZZ",
                Currencies = new HashSet<string>(new[] { "CHF", "EUR", "USD" }, StringComparer.OrdinalIgnoreCase),
                SameDayTransfer = true,
                BearerChargeTypes = BearerChargeType.SHA | BearerChargeType.OWN | BearerChargeType.BEN
            },
            new()
            {
                BankCode = "BANKH-CA11",
                Currencies = new HashSet<string>(new[] { "CAD", "USD", "GBP" }, StringComparer.OrdinalIgnoreCase),
                SameDayTransfer = true,
                BearerChargeTypes = BearerChargeType.OWN | BearerChargeType.BEN
            }
        };
    }

    private sealed class InMemoryCapabilitiesStore : ICapabilitiesStore
    {
        private readonly IReadOnlyList<BankCapabilities> _banks;

        public InMemoryCapabilitiesStore(IEnumerable<BankCapabilities> banks)
        {
            _banks = banks.Select(Clone).ToList();
        }

        public IEnumerable<BankCapabilities> GetAll() => _banks;

        private static BankCapabilities Clone(BankCapabilities original) => new()
        {
            BankCode = original.BankCode,
            Currencies = new HashSet<string>(original.Currencies, StringComparer.OrdinalIgnoreCase),
            SameDayTransfer = original.SameDayTransfer,
            BearerChargeTypes = original.BearerChargeTypes
        };
    }
}
