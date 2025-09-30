using Banking.Capabilities;

namespace Demo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Configure CapabilityEngine to load capabilities from JSON
            var baseDir = AppContext.BaseDirectory;

            var jsonPath = System.IO.Path.Combine(baseDir, "capabilities.json");

            if (!System.IO.File.Exists(jsonPath))
            {
                // Fallback to project root when running from bin/Debug|Release
                jsonPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "capabilities.json"));
            }

            CapabilityEngine.Configure(new JsonCapabilitiesStore(jsonPath));

            var request = new CapabilityQuery();

            var eligible = CapabilityEngine.FindEligibleBanks(request).ToList();

            foreach (var e in eligible)
            {
                Console.WriteLine($"Eligible Bank: {e.BankCode}");
            }

            var capabilitiesPerBank = CapabilityEngine.GetCapabilitiesPerBank(request);

            foreach (var (bank, sameDay, charges, currencies) in capabilitiesPerBank)
            {
                Console.WriteLine($"{bank.BankCode} | SameDay: {sameDay} | Charges: {charges} | Ccy: [ {string.Join(", ", currencies)} ]");
            }

            var usdQuery = new CapabilityQuery
            {
                Currency = "USD",
                RequireSameDay = false,
                AllowedCharges = BearerChargeType.SHA | BearerChargeType.OWN
            };

            Console.WriteLine("\nCapabilities restricted to USD (SHA or OWN charges):");

            var usdCapabilities = CapabilityEngine.GetCapabilitiesPerBank(usdQuery, new[] { usdQuery.Currency });

            foreach (var (bank, sameDay, charges, currencies) in usdCapabilities)
            {
                Console.WriteLine($"{bank.BankCode} | SameDay: {sameDay} | Charges: {charges} | USD Supported: {currencies.Contains("USD")}");
            }

            var eurQuery = new CapabilityQuery
            {
                Currency = "EUR",
                RequireSameDay = true,
                AllowedCharges = BearerChargeType.SHA
            };

            Console.WriteLine("\nSame-day EUR capability snapshot (SHA charges only):");

            var eurCapabilities = CapabilityEngine.GetCapabilitiesPerBank(eurQuery, new[] { eurQuery.Currency });

            foreach (var (bank, sameDay, charges, currencies) in eurCapabilities)
            {
                Console.WriteLine($"{bank.BankCode} | SameDay: {sameDay} | Charges: {charges} | EUR Supported: {currencies.Contains("EUR")}");
            }
        }
    }
}
